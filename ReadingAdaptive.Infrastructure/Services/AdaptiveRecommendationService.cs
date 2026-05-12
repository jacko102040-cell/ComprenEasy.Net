using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Adaptive.Constants;
using ReadingAdaptive.Application.Adaptive.Dtos;
using ReadingAdaptive.Application.Adaptive.Exceptions;
using ReadingAdaptive.Application.Adaptive.Interfaces;
using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Infrastructure.Persistence;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class AdaptiveRecommendationService : IAdaptiveRecommendationService
{
    private const string CompletedStatus = "Completed";
    private const string ModelVersion = "rules-v1";

    private readonly ReadingAdaptiveDbContext _dbContext;
    private readonly IAcademicFlowService _academicFlowService;

    public AdaptiveRecommendationService(
        ReadingAdaptiveDbContext dbContext,
        IAcademicFlowService academicFlowService)
    {
        _dbContext = dbContext;
        _academicFlowService = academicFlowService;
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

        return await MapRecommendationAsync(recommendation, studentId, cancellationToken);
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
            return await MapRecommendationAsync(existingRecommendation, studentId, cancellationToken);
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

        var suggestedActivity = await ResolveSuggestedActivityAsync(
            studentId,
            attempt.Assessment.ReadingId,
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
            RecommendedAssessmentId = suggestedActivity.RecommendedAssessmentId,
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

        return await MapRecommendationAsync(savedRecommendation, studentId, cancellationToken);
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

    private async Task<AdaptiveRecommendationDto> MapRecommendationAsync(
        AdaptiveRecommendation recommendation,
        int studentId,
        CancellationToken cancellationToken)
    {
        var suggestedActivity = await ResolveSuggestedActivityAsync(
            studentId,
            recommendation.SourceAttempt.Assessment.ReadingId,
            cancellationToken);

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
            suggestedActivity.RecommendedActivityType,
            suggestedActivity.RecommendedRoute,
            suggestedActivity.RecommendedReadingId,
            suggestedActivity.RecommendedAssessmentId,
            suggestedActivity.RecommendedAssessmentTitle,
            recommendation.EngineType,
            recommendation.ConfidenceScore,
            recommendation.CreatedAt);
    }

    private async Task<SuggestedActivity> ResolveSuggestedActivityAsync(
        int studentId,
        int? preferredReadingId,
        CancellationToken cancellationToken)
    {
        var summary = await _academicFlowService.GetCurrentSummaryAsync(studentId, cancellationToken);

        if (summary.CanAccessPosttest && !summary.HasCompletedPosttest)
        {
            var posttestAssessment = await _dbContext.Assessments
                .AsNoTracking()
                .Where(item =>
                    item.IsActive &&
                    item.AssessmentType == AssessmentTypes.Posttest)
                .OrderBy(item => item.Title)
                .ThenBy(item => item.AssessmentId)
                .Select(item => new
                {
                    item.AssessmentId,
                    item.Title
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new SuggestedActivity(
                AdaptiveActivityTypes.Posttest,
                "/posttests",
                null,
                posttestAssessment?.AssessmentId,
                posttestAssessment?.Title);
        }

        return await BuildReadingSuggestionAsync(studentId, preferredReadingId, cancellationToken);
    }

    private async Task<SuggestedActivity> BuildReadingSuggestionAsync(
        int studentId,
        int? preferredReadingId,
        CancellationToken cancellationToken)
    {
        var reading = await ResolveRecommendedReadingAsync(studentId, preferredReadingId, cancellationToken);

        if (reading is null)
        {
            return new SuggestedActivity(
                null,
                "/dashboard",
                null,
                null,
                null);
        }

        return new SuggestedActivity(
            AdaptiveActivityTypes.Reading,
            $"/readings/{reading.ReadingId}",
            reading.ReadingId,
            null,
            null);
    }

    private async Task<RecommendedReading?> ResolveRecommendedReadingAsync(
        int studentId,
        int? preferredReadingId,
        CancellationToken cancellationToken)
    {
        var completedReadingIds = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Where(item =>
                item.StudentId == studentId &&
                item.Status == CompletedStatus &&
                item.Assessment.AssessmentType == ReadingAssessmentTypes.ReadingPractice &&
                item.Assessment.ReadingId != null)
            .Select(item => item.Assessment.ReadingId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var completedReadingIdSet = completedReadingIds.ToHashSet();

        if (preferredReadingId.HasValue)
        {
            var preferredReading = await _dbContext.Readings
                .AsNoTracking()
                .Where(item =>
                    item.ReadingId == preferredReadingId.Value &&
                    item.IsActive &&
                    !completedReadingIdSet.Contains(item.ReadingId))
                .Select(item => new RecommendedReading(item.ReadingId))
                .FirstOrDefaultAsync(cancellationToken);

            if (preferredReading is not null)
            {
                return preferredReading;
            }
        }

        return await _dbContext.Readings
            .AsNoTracking()
            .Where(item => item.IsActive && !completedReadingIdSet.Contains(item.ReadingId))
            .OrderBy(item => item.Title)
            .ThenBy(item => item.ReadingId)
            .Select(item => new RecommendedReading(item.ReadingId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record RuleOutcome(
        string PredictedAction,
        byte RecommendedDifficultyLevelId);

    private sealed record SuggestedActivity(
        string? RecommendedActivityType,
        string RecommendedRoute,
        int? RecommendedReadingId,
        int? RecommendedAssessmentId,
        string? RecommendedAssessmentTitle);

    private sealed record RecommendedReading(int ReadingId);
}
