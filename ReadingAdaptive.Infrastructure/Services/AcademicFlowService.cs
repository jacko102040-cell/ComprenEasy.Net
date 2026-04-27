using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.AcademicFlow.Dtos;
using ReadingAdaptive.Application.AcademicFlow.Exceptions;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Infrastructure.Persistence;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class AcademicFlowService : IAcademicFlowService
{
    private const string CompletedStatus = "Completed";
    private const int MinimumReadingSessionsRequired = 1;
    private const string PretestStage = "Pretest";
    private const string ReadingsStage = "Readings";
    private const string PosttestStage = "Posttest";
    private const string CompletedStage = "Completed";

    private readonly ReadingAdaptiveDbContext _dbContext;

    public AcademicFlowService(ReadingAdaptiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AcademicFlowSummaryDto> GetCurrentSummaryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStudentAsync(studentId, cancellationToken);

        var attempts = await _dbContext.AssessmentAttempts
            .AsNoTracking()
            .Include(attempt => attempt.Assessment)
            .Where(attempt =>
                attempt.StudentId == studentId &&
                attempt.Status == CompletedStatus &&
                (AssessmentTypes.SupportedTypes.Contains(attempt.Assessment.AssessmentType) ||
                    attempt.Assessment.AssessmentType == ReadingAssessmentTypes.ReadingPractice))
            .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
            .ThenByDescending(attempt => attempt.AttemptId)
            .ToListAsync(cancellationToken);

        var latestPretest = attempts.FirstOrDefault(attempt =>
            attempt.Assessment.AssessmentType == AssessmentTypes.Pretest);
        var latestPosttest = attempts.FirstOrDefault(attempt =>
            attempt.Assessment.AssessmentType == AssessmentTypes.Posttest);

        var latestPretestFinishedAt = latestPretest is null
            ? (DateTime?)null
            : latestPretest.FinishedAt ?? latestPretest.StartedAt;
        var latestPosttestFinishedAt = latestPosttest is null
            ? (DateTime?)null
            : latestPosttest.FinishedAt ?? latestPosttest.StartedAt;

        var completedReadings = latestPretestFinishedAt.HasValue
            ? attempts
                .Where(attempt =>
                    attempt.Assessment.AssessmentType == ReadingAssessmentTypes.ReadingPractice &&
                    (attempt.FinishedAt ?? attempt.StartedAt) >= latestPretestFinishedAt.Value &&
                    (!latestPosttestFinishedAt.HasValue ||
                        (attempt.FinishedAt ?? attempt.StartedAt) <= latestPosttestFinishedAt.Value))
                .ToList()
            : [];

        var latestReadingFinishedAt = completedReadings.Count == 0
            ? (DateTime?)null
            : completedReadings
                .Select(attempt => attempt.FinishedAt ?? attempt.StartedAt)
                .Max();

        var hasCompletedPretest = latestPretest is not null;
        var hasCompletedPosttest = latestPosttest is not null;
        var hasCompletedMinimumReadingIntervention = completedReadings.Count >= MinimumReadingSessionsRequired;
        var canAccessReadings = hasCompletedPretest && !hasCompletedPosttest;
        var canAccessPosttest = hasCompletedPretest && hasCompletedMinimumReadingIntervention && !hasCompletedPosttest;
        var canAccessFinalComparison = hasCompletedPosttest;

        var currentStage = ResolveCurrentStage(
            hasCompletedPretest,
            hasCompletedMinimumReadingIntervention,
            hasCompletedPosttest);

        var (recommendedRoute, recommendedMessage) = ResolveRecommendedStep(currentStage);

        return new AcademicFlowSummaryDto(
            hasCompletedPretest,
            latestPretestFinishedAt,
            completedReadings.Count,
            MinimumReadingSessionsRequired,
            hasCompletedMinimumReadingIntervention,
            latestReadingFinishedAt,
            canAccessReadings,
            canAccessPosttest,
            hasCompletedPosttest,
            latestPosttestFinishedAt,
            canAccessFinalComparison,
            currentStage,
            recommendedRoute,
            recommendedMessage);
    }

    private async Task EnsureStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.StudentId == studentId, cancellationToken);

        if (student is null)
        {
            throw new AcademicFlowAccessDeniedException("Authenticated user is not registered as a student.");
        }

        if (!student.IsEnabledForTest)
        {
            throw new AcademicFlowAccessDeniedException("Student is not enabled for the academic flow.");
        }
    }

    private static string ResolveCurrentStage(
        bool hasCompletedPretest,
        bool hasCompletedMinimumReadingIntervention,
        bool hasCompletedPosttest)
    {
        if (!hasCompletedPretest)
        {
            return PretestStage;
        }

        if (hasCompletedPosttest)
        {
            return CompletedStage;
        }

        if (!hasCompletedMinimumReadingIntervention)
        {
            return ReadingsStage;
        }

        return PosttestStage;
    }

    private static (string Route, string Message) ResolveRecommendedStep(string currentStage)
    {
        return currentStage switch
        {
            PretestStage => ("/pretests", "Tu siguiente paso academico es completar el pretest."),
            ReadingsStage => ("/readings", "Continua con la intervencion de lecturas PQ4R."),
            PosttestStage => ("/posttests", "Ya puedes pasar al posttest para medir tu avance."),
            _ => ("/pre-post-comparison", "Tu cierre principal esta en la comparacion final pretest vs posttest.")
        };
    }
}
