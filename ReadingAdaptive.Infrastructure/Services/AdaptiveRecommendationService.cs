using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.Adaptive.Constants;
using ReadingAdaptive.Application.Adaptive.Dtos;
using ReadingAdaptive.Application.Adaptive.Exceptions;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class AdaptiveRecommendationService : IAdaptiveRecommendationService
{
    private const string CompletedStatus = "Completed";
    private const string ModelVersion = "rules-v1";

    private readonly ReadingAdaptiveDbContext _dbContext;

    public AdaptiveRecommendationService(ReadingAdaptiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdaptiveRecommendationDto> GetLatestRecommendationAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var recommendation = await _dbContext.AdaptiveRecommendations
            .AsNoTracking()
            .Include(item => item.CurrentDifficultyLevel)
            .Include(item => item.RecommendedDifficultyLevel)
            .Include(item => item.RecommendedAssessment)
            .Include(item => item.SourceAttempt)
                .ThenInclude(attempt => attempt.Assessment)
            .Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.RecommendationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (recommendation is null)
        {
            throw new AdaptiveNotFoundException("No adaptive recommendation was found for the authenticated student.");
        }

        return MapRecommendation(recommendation);
    }

    public async Task<AdaptiveRecommendationDto> GenerateRecommendationAsync(
        long attemptId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var existingRecommendation = await _dbContext.AdaptiveRecommendations
            .AsNoTracking()
            .Include(item => item.CurrentDifficultyLevel)
            .Include(item => item.RecommendedDifficultyLevel)
            .Include(item => item.RecommendedAssessment)
            .Include(item => item.SourceAttempt)
                .ThenInclude(attempt => attempt.Assessment)
            .Where(item => item.StudentId == studentId && item.SourceAttemptId == attemptId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.RecommendationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingRecommendation is not null)
        {
            return MapRecommendation(existingRecommendation);
        }

        var attempt = await _dbContext.AssessmentAttempts
            .Include(item => item.Assessment)
                .ThenInclude(assessment => assessment.DifficultyLevel)
            .Include(item => item.Assessment)
                .ThenInclude(assessment => assessment.Reading)
                    .ThenInclude(reading => reading!.DifficultyLevel)
            .Include(item => item.AttemptAnswers)
            .SingleOrDefaultAsync(item => item.AttemptId == attemptId, cancellationToken);

        if (attempt is null)
        {
            throw new AdaptiveNotFoundException("The selected attempt was not found.");
        }

        if (attempt.StudentId != studentId)
        {
            throw new AdaptiveAccessDeniedException("You cannot generate a recommendation from another student's attempt.");
        }

        if (!string.Equals(attempt.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new AdaptiveValidationException("Only completed attempts can generate adaptive recommendations.");
        }

        var currentDifficulty = await ResolveCurrentDifficultyAsync(attempt, cancellationToken);
        var allDifficulties = await _dbContext.DifficultyLevels
            .AsNoTracking()
            .OrderBy(level => level.RankOrder)
            .ToListAsync(cancellationToken);

        var previousCompletedAttempt = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(item =>
                item.StudentId == studentId &&
                item.AttemptId != attempt.AttemptId &&
                item.FinishedAt != null &&
                item.Status == CompletedStatus)
            .OrderByDescending(item => item.FinishedAt)
            .ThenByDescending(item => item.AttemptId)
            .FirstOrDefaultAsync(cancellationToken);

        var literalScore = attempt.LiteralScore ?? 0m;
        var inferentialScore = attempt.InferentialScore ?? 0m;
        var criticalScore = attempt.CriticalScore ?? 0m;
        var totalScore = attempt.TotalScore ?? 0m;
        var totalErrors = attempt.TotalErrors ?? 0;
        var totalTimeSeconds = attempt.TotalTimeSeconds ?? 0;
        var completionPercentage = attempt.CompletionPercentage ?? 0m;
        var answeredCount = attempt.AttemptAnswers.Count;
        var divisor = Math.Max(answeredCount, 1);
        var averageResponseTimeSeconds = Math.Round(
            totalTimeSeconds / (decimal)divisor,
            2,
            MidpointRounding.AwayFromZero);
        var previousProgressDelta = previousCompletedAttempt?.TotalScore is decimal previousTotalScore
            ? totalScore - previousTotalScore
            : (decimal?)null;

        var ruleOutcome = EvaluateRules(
            totalScore,
            literalScore,
            inferentialScore,
            criticalScore,
            totalErrors,
            totalTimeSeconds,
            completionPercentage,
            currentDifficulty,
            allDifficulties);

        var recommendedAssessmentId = await ResolveRecommendedAssessmentIdAsync(
            attempt.AssessmentId,
            attempt.Assessment.AssessmentType,
            ruleOutcome.RecommendedDifficultyLevelId,
            cancellationToken);

        var confidenceScore = CalculateConfidenceScore(
            totalScore,
            completionPercentage,
            totalErrors,
            previousProgressDelta);

        var recommendation = new AdaptiveRecommendation
        {
            StudentId = studentId,
            SourceAttemptId = attempt.AttemptId,
            CurrentDifficultyLevelId = currentDifficulty.DifficultyLevelId,
            RecommendedDifficultyLevelId = ruleOutcome.RecommendedDifficultyLevelId,
            PredictedAction = ruleOutcome.PredictedAction,
            RecommendedAssessmentId = recommendedAssessmentId,
            EngineType = AdaptiveEngineTypes.Rules,
            ConfidenceScore = confidenceScore,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AdaptiveRecommendations.Add(recommendation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.MlPredictions.Add(new MlPrediction
        {
            RecommendationId = recommendation.RecommendationId,
            LiteralScore = literalScore,
            InferentialScore = inferentialScore,
            CriticalScore = criticalScore,
            ErrorCount = totalErrors,
            AvgResponseTimeSeconds = averageResponseTimeSeconds,
            CurrentDifficultyRank = currentDifficulty.RankOrder,
            PreviousProgressDelta = previousProgressDelta,
            ModelVersion = ModelVersion,
            RawOutput = BuildRawOutput(
                ruleOutcome.PredictedAction,
                ruleOutcome.RecommendedDifficultyLevelId,
                totalScore,
                completionPercentage,
                totalErrors,
                averageResponseTimeSeconds),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedRecommendation = await _dbContext.AdaptiveRecommendations
            .AsNoTracking()
            .Include(item => item.CurrentDifficultyLevel)
            .Include(item => item.RecommendedDifficultyLevel)
            .Include(item => item.RecommendedAssessment)
            .Include(item => item.SourceAttempt)
                .ThenInclude(sourceAttempt => sourceAttempt.Assessment)
            .SingleAsync(item => item.RecommendationId == recommendation.RecommendationId, cancellationToken);

        return MapRecommendation(savedRecommendation);
    }

    private async Task EnsureStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Students
            .AsNoTracking()
            .AnyAsync(student => student.StudentId == studentId, cancellationToken);

        if (!exists)
        {
            throw new AdaptiveAccessDeniedException("Authenticated user is not registered as a student.");
        }
    }

    private async Task<DifficultyLevel> ResolveCurrentDifficultyAsync(
        AssessmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (attempt.Assessment.DifficultyLevel is not null)
        {
            return attempt.Assessment.DifficultyLevel;
        }

        if (attempt.Assessment.Reading?.DifficultyLevel is not null)
        {
            return attempt.Assessment.Reading.DifficultyLevel;
        }

        var fallbackDifficulty = await _dbContext.DifficultyLevels
            .AsNoTracking()
            .OrderBy(level => level.RankOrder)
            .FirstOrDefaultAsync(cancellationToken);

        return fallbackDifficulty
            ?? throw new AdaptiveValidationException("No difficulty levels are configured for adaptive recommendations.");
    }

    private static RuleOutcome EvaluateRules(
        decimal totalScore,
        decimal literalScore,
        decimal inferentialScore,
        decimal criticalScore,
        int totalErrors,
        int totalTimeSeconds,
        decimal completionPercentage,
        DifficultyLevel currentDifficulty,
        IReadOnlyList<DifficultyLevel> allDifficulties)
    {
        var lowerDifficulty = allDifficulties
            .Where(level => level.RankOrder < currentDifficulty.RankOrder)
            .OrderByDescending(level => level.RankOrder)
            .FirstOrDefault();

        var higherDifficulty = allDifficulties
            .Where(level => level.RankOrder > currentDifficulty.RankOrder)
            .OrderBy(level => level.RankOrder)
            .FirstOrDefault();

        if (completionPercentage < 60m ||
            totalScore < 50m ||
            literalScore < 45m ||
            inferentialScore < 45m ||
            criticalScore < 45m ||
            totalErrors >= 6)
        {
            return new RuleOutcome(
                AdaptivePredictedActions.Reinforce,
                lowerDifficulty?.DifficultyLevelId ?? currentDifficulty.DifficultyLevelId);
        }

        if (completionPercentage >= 90m &&
            totalScore >= 85m &&
            literalScore >= 80m &&
            inferentialScore >= 80m &&
            criticalScore >= 75m &&
            totalErrors <= 2 &&
            totalTimeSeconds > 0)
        {
            return new RuleOutcome(
                AdaptivePredictedActions.Advance,
                higherDifficulty?.DifficultyLevelId ?? currentDifficulty.DifficultyLevelId);
        }

        return new RuleOutcome(
            AdaptivePredictedActions.AdvanceWithSupport,
            higherDifficulty?.DifficultyLevelId ?? currentDifficulty.DifficultyLevelId);
    }

    private async Task<int?> ResolveRecommendedAssessmentIdAsync(
        int sourceAssessmentId,
        string sourceAssessmentType,
        byte recommendedDifficultyLevelId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Assessments
            .AsNoTracking()
            .Where(assessment =>
                assessment.IsActive &&
                assessment.AssessmentId != sourceAssessmentId &&
                assessment.AssessmentType == sourceAssessmentType &&
                assessment.DifficultyLevelId == recommendedDifficultyLevelId)
            .OrderBy(assessment => assessment.AssessmentId)
            .Select(assessment => (int?)assessment.AssessmentId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static decimal CalculateConfidenceScore(
        decimal totalScore,
        decimal completionPercentage,
        int totalErrors,
        decimal? previousProgressDelta)
    {
        var score = 55m;
        score += completionPercentage >= 90m ? 20m : completionPercentage >= 75m ? 12m : 5m;
        score += totalScore >= 85m ? 15m : totalScore >= 65m ? 10m : 4m;
        score += totalErrors <= 2 ? 7m : totalErrors <= 5 ? 3m : 0m;
        score += previousProgressDelta is > 0 ? 3m : 0m;

        return Math.Round(Math.Clamp(score, 0m, 100m), 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildRawOutput(
        string predictedAction,
        byte recommendedDifficultyLevelId,
        decimal totalScore,
        decimal completionPercentage,
        int totalErrors,
        decimal avgResponseTimeSeconds)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"action={predictedAction};difficulty={recommendedDifficultyLevelId};score={totalScore:0.##};completion={completionPercentage:0.##};errors={totalErrors};avg={avgResponseTimeSeconds:0.##}");
    }

    private static AdaptiveRecommendationDto MapRecommendation(AdaptiveRecommendation recommendation)
    {
        return new AdaptiveRecommendationDto(
            recommendation.RecommendationId,
            recommendation.SourceAttemptId,
            recommendation.SourceAttempt.AssessmentId,
            recommendation.SourceAttempt.Assessment.AssessmentType,
            recommendation.SourceAttempt.Assessment.Title,
            recommendation.CurrentDifficultyLevelId,
            recommendation.CurrentDifficultyLevel.Name,
            recommendation.RecommendedDifficultyLevelId,
            recommendation.RecommendedDifficultyLevel.Name,
            recommendation.PredictedAction,
            recommendation.RecommendedAssessmentId,
            recommendation.RecommendedAssessment?.Title,
            recommendation.EngineType,
            recommendation.ConfidenceScore,
            recommendation.CreatedAt);
    }

    private sealed record RuleOutcome(
        string PredictedAction,
        byte RecommendedDifficultyLevelId);
}
