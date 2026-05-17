using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using ReadingAdaptive.Application.Adaptive.Constants;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;
using ReadingAdaptive.ML.Models;
using ReadingAdaptive.ML.Training;

internal static class Program
{
    private const int MinimumRows = 30;
    private const string ConnectionStringArgument = "--connection-string";
    private const string OutputArgument = "--output";
    private const string ConnectionStringEnvironmentVariable = "COMPRENEASY_TRAINING_CONNECTION_STRING";
    private const string DefaultOutputPath = "ReadingAdaptive.Api/App_Data/ml/adaptive-recommendation-model.zip";
    private const string SeedCsvPath = "ReadingAdaptive.ML.TrainerApp/Data/adaptive-training-seed.csv";
    private const string CompletedStatus = "Completed";

    private static readonly string[] RequiredActions =
    [
        AdaptivePredictedActions.Reinforce,
        AdaptivePredictedActions.AdvanceWithSupport,
        AdaptivePredictedActions.Advance
    ];

    public static async Task<int> Main(string[] args)
    {
        var options = TrainerOptions.Parse(args);
        var connectionString = options.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Training connection string was not provided.");
            Console.WriteLine($"Set {ConnectionStringEnvironmentVariable} or pass {ConnectionStringArgument}.");
            return 1;
        }

        var outputPath = ResolvePath(options.OutputPath ?? DefaultOutputPath);
        var seedPath = ResolvePath(SeedCsvPath);

        try
        {
            await using var dbContext = CreateDbContext(connectionString);
            var databaseRows = await LoadDatabaseRowsAsync(dbContext);
            var seedRows = LoadSeedRows(seedPath);
            var trainingRows = BuildTrainingRows(databaseRows, seedRows);
            var counts = CountByClass(trainingRows);

            Console.WriteLine($"Historical rows: {databaseRows.Count}");
            Console.WriteLine($"Seed rows used: {trainingRows.Count - databaseRows.Count}");
            Console.WriteLine($"Training rows: {trainingRows.Count}");
            PrintClassCounts(counts);

            var missingActions = RequiredActions
                .Where(action => !counts.ContainsKey(action))
                .ToList();

            if (trainingRows.Count < MinimumRows)
            {
                Console.WriteLine($"Training aborted: at least {MinimumRows} rows are required after mixing database and seed data.");
                return 1;
            }

            if (missingActions.Count > 0)
            {
                Console.WriteLine($"Training aborted: missing classes: {string.Join(", ", missingActions)}.");
                return 1;
            }

            var trainer = new AdaptiveRecommendationModelTrainer();
            trainer.TrainAndSave(trainingRows, outputPath);

            ValidateModel(outputPath, trainingRows[0]);

            Console.WriteLine("Model generated successfully.");
            Console.WriteLine($"Model path: {outputPath}");
            Console.WriteLine($"Rows used: {trainingRows.Count}");
            PrintClassCounts(counts);

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Training failed: {exception.GetType().Name}. {exception.Message}");
            return 1;
        }
    }

    private static ReadingAdaptiveDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ReadingAdaptiveDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ReadingAdaptiveDbContext(options);
    }

    private static async Task<List<AdaptiveRecommendationModelInput>> LoadDatabaseRowsAsync(
        ReadingAdaptiveDbContext dbContext)
    {
        var recommendations = await dbContext.AdaptiveRecommendations
            .AsNoTracking()
            .Include(recommendation => recommendation.SourceAttempt)
                .ThenInclude(attempt => attempt.Assessment)
            .Include(recommendation => recommendation.SourceAttempt)
                .ThenInclude(attempt => attempt.AttemptAnswers)
            .Include(recommendation => recommendation.CurrentDifficultyLevel)
            .Include(recommendation => recommendation.MlPrediction)
            .Where(recommendation => RequiredActions.Contains(recommendation.PredictedAction))
            .OrderBy(recommendation => recommendation.CreatedAt)
            .ToListAsync();

        if (recommendations.Count == 0)
        {
            return [];
        }

        var assessmentIds = recommendations
            .Select(recommendation => recommendation.SourceAttempt.AssessmentId)
            .Distinct()
            .ToList();
        var studentIds = recommendations
            .Select(recommendation => recommendation.StudentId)
            .Distinct()
            .ToList();

        var totalQuestionsByAssessment = await dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(question => assessmentIds.Contains(question.AssessmentId) && question.IsActive)
            .GroupBy(question => question.AssessmentId)
            .Select(group => new
            {
                AssessmentId = group.Key,
                TotalQuestions = group.Count()
            })
            .ToDictionaryAsync(item => item.AssessmentId, item => item.TotalQuestions);

        var completedAttempts = await dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(attempt =>
                studentIds.Contains(attempt.StudentId) &&
                attempt.Status == CompletedStatus &&
                attempt.FinishedAt != null)
            .Select(attempt => new CompletedAttemptSnapshot(
                attempt.AttemptId,
                attempt.StudentId,
                attempt.FinishedAt!.Value,
                attempt.TotalScore,
                attempt.Assessment.AssessmentType))
            .ToListAsync();

        var rows = new List<AdaptiveRecommendationModelInput>();

        foreach (var recommendation in recommendations)
        {
            var attempt = recommendation.SourceAttempt;
            var answeredQuestions = attempt.AttemptAnswers.Count;
            var totalTimeSeconds = attempt.TotalTimeSeconds ?? 0;
            var averageTimeSeconds = answeredQuestions == 0
                ? 0f
                : totalTimeSeconds / (float)answeredQuestions;
            var previousProgressDelta = recommendation.MlPrediction?.PreviousProgressDelta ??
                ResolvePreviousProgressDelta(completedAttempts, attempt);
            var completedReadingSessions = completedAttempts.Count(completedAttempt =>
                completedAttempt.StudentId == recommendation.StudentId &&
                completedAttempt.FinishedAt <= recommendation.CreatedAt &&
                completedAttempt.AssessmentType == ReadingAssessmentTypes.ReadingPractice);

            rows.Add(new AdaptiveRecommendationModelInput
            {
                TotalScore = ToFloat(attempt.TotalScore),
                LiteralScore = ToFloat(attempt.LiteralScore),
                InferentialScore = ToFloat(attempt.InferentialScore),
                CriticalScore = ToFloat(attempt.CriticalScore),
                TotalCorrect = attempt.TotalCorrect ?? 0,
                TotalErrors = attempt.TotalErrors ?? 0,
                AnsweredQuestions = answeredQuestions,
                TotalQuestions = totalQuestionsByAssessment.TryGetValue(attempt.AssessmentId, out var totalQuestions)
                    ? totalQuestions
                    : answeredQuestions,
                CompletionPercentage = ToFloat(attempt.CompletionPercentage),
                TotalTimeSeconds = totalTimeSeconds,
                AverageTimeSeconds = averageTimeSeconds,
                CurrentDifficultyRank = recommendation.CurrentDifficultyLevel.RankOrder,
                PreviousProgressDelta = ToFloat(previousProgressDelta),
                CompletedReadingSessions = completedReadingSessions,
                PredictedAction = recommendation.PredictedAction
            });
        }

        return rows;
    }

    private static decimal ResolvePreviousProgressDelta(
        IReadOnlyCollection<CompletedAttemptSnapshot> completedAttempts,
        AssessmentAttempt attempt)
    {
        if (attempt.TotalScore is null || attempt.FinishedAt is null)
        {
            return 0m;
        }

        var previousAttempt = completedAttempts
            .Where(item =>
                item.StudentId == attempt.StudentId &&
                item.AttemptId != attempt.AttemptId &&
                item.FinishedAt < attempt.FinishedAt.Value &&
                item.TotalScore.HasValue)
            .OrderByDescending(item => item.FinishedAt)
            .FirstOrDefault();

        return previousAttempt?.TotalScore is decimal previousScore
            ? attempt.TotalScore.Value - previousScore
            : 0m;
    }

    private static List<AdaptiveRecommendationModelInput> BuildTrainingRows(
        IReadOnlyCollection<AdaptiveRecommendationModelInput> databaseRows,
        IReadOnlyCollection<AdaptiveRecommendationModelInput> seedRows)
    {
        var rows = databaseRows.ToList();
        var counts = CountByClass(rows);
        var needsSeed = rows.Count < MinimumRows || RequiredActions.Any(action => !counts.ContainsKey(action));

        if (!needsSeed)
        {
            return rows;
        }

        rows.AddRange(seedRows);
        return rows;
    }

    private static List<AdaptiveRecommendationModelInput> LoadSeedRows(string seedPath)
    {
        if (!File.Exists(seedPath))
        {
            return [];
        }

        return File.ReadLines(seedPath)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseSeedRow)
            .ToList();
    }

    private static AdaptiveRecommendationModelInput ParseSeedRow(string line)
    {
        var columns = line.Split(',');

        if (columns.Length != 15)
        {
            throw new InvalidOperationException("Invalid seed CSV format.");
        }

        return new AdaptiveRecommendationModelInput
        {
            TotalScore = ParseFloat(columns[0]),
            LiteralScore = ParseFloat(columns[1]),
            InferentialScore = ParseFloat(columns[2]),
            CriticalScore = ParseFloat(columns[3]),
            TotalCorrect = ParseFloat(columns[4]),
            TotalErrors = ParseFloat(columns[5]),
            AnsweredQuestions = ParseFloat(columns[6]),
            TotalQuestions = ParseFloat(columns[7]),
            CompletionPercentage = ParseFloat(columns[8]),
            TotalTimeSeconds = ParseFloat(columns[9]),
            AverageTimeSeconds = ParseFloat(columns[10]),
            CurrentDifficultyRank = ParseFloat(columns[11]),
            PreviousProgressDelta = ParseFloat(columns[12]),
            CompletedReadingSessions = ParseFloat(columns[13]),
            PredictedAction = columns[14].Trim()
        };
    }

    private static void ValidateModel(string outputPath, AdaptiveRecommendationModelInput sample)
    {
        var mlContext = new MLContext(seed: 42);
        var model = mlContext.Model.Load(outputPath, out _);
        var predictionEngine = mlContext.Model
            .CreatePredictionEngine<AdaptiveRecommendationModelInput, AdaptiveRecommendationModelOutput>(model);
        var prediction = predictionEngine.Predict(sample);

        if (!RequiredActions.Contains(prediction.PredictedAction))
        {
            throw new InvalidOperationException("The generated model returned an invalid action during validation.");
        }

        if (prediction.Score.Length == 0)
        {
            throw new InvalidOperationException("The generated model did not return scores during validation.");
        }

        Console.WriteLine($"Validation prediction: {prediction.PredictedAction}; scores={prediction.Score.Length}");
    }

    private static Dictionary<string, int> CountByClass(IEnumerable<AdaptiveRecommendationModelInput> rows)
    {
        return rows
            .GroupBy(row => row.PredictedAction)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    private static void PrintClassCounts(IReadOnlyDictionary<string, int> counts)
    {
        foreach (var action in RequiredActions)
        {
            counts.TryGetValue(action, out var count);
            Console.WriteLine($"{action}: {count}");
        }
    }

    private static string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", path));
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float ToFloat(decimal? value)
    {
        return value.HasValue
            ? Convert.ToSingle(value.Value, CultureInfo.InvariantCulture)
            : 0f;
    }

    private sealed record CompletedAttemptSnapshot(
        long AttemptId,
        int StudentId,
        DateTime FinishedAt,
        decimal? TotalScore,
        string AssessmentType);

    private sealed record TrainerOptions(
        string? ConnectionString,
        string? OutputPath)
    {
        public static TrainerOptions Parse(string[] args)
        {
            string? connectionString = null;
            string? outputPath = null;

            for (var index = 0; index < args.Length; index++)
            {
                var current = args[index];

                if (string.Equals(current, ConnectionStringArgument, StringComparison.OrdinalIgnoreCase) &&
                    index + 1 < args.Length)
                {
                    connectionString = args[++index];
                    continue;
                }

                if (string.Equals(current, OutputArgument, StringComparison.OrdinalIgnoreCase) &&
                    index + 1 < args.Length)
                {
                    outputPath = args[++index];
                }
            }

            return new TrainerOptions(connectionString, outputPath);
        }
    }
}
