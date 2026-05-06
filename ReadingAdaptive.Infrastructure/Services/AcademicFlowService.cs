using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReadingAdaptive.Application.AcademicFlow.Dtos;
using ReadingAdaptive.Application.AcademicFlow.Exceptions;
using ReadingAdaptive.Application.AcademicFlow.Interfaces;
using ReadingAdaptive.Application.Evaluations.Constants;
using ReadingAdaptive.Application.Readings.Constants;
using ReadingAdaptive.Infrastructure.Configuration;
using ReadingAdaptive.Infrastructure.Persistence;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class AcademicFlowService : IAcademicFlowService
{
    private const string CompletedStatus = "Completed";
    private const string ReadingsStage = "Readings";

    private readonly ReadingAdaptiveDbContext _dbContext;
    private readonly int _minimumReadingSessionsRequired;

    public AcademicFlowService(
        ReadingAdaptiveDbContext dbContext,
        IOptions<AcademicFlowOptions> options)
    {
        _dbContext = dbContext;
        _minimumReadingSessionsRequired = Math.Max(1, options.Value.MinimumReadingSessionsRequired);
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

        var completedReadings = attempts
            .Where(attempt => attempt.Assessment.AssessmentType == ReadingAssessmentTypes.ReadingPractice)
            .ToList();

        var latestReadingFinishedAt = completedReadings.Count == 0
            ? (DateTime?)null
            : completedReadings
                .Select(attempt => attempt.FinishedAt ?? attempt.StartedAt)
                .Max();

        var activeReadings = await _dbContext.Readings
            .AsNoTracking()
            .Where(reading => reading.IsActive)
            .OrderBy(reading => reading.Title)
            .ThenBy(reading => reading.ReadingId)
            .Select(reading => new ActiveReadingItem(reading.ReadingId, reading.Title))
            .ToListAsync(cancellationToken);

        var completedReadingIds = completedReadings
            .Where(attempt => attempt.Assessment.ReadingId.HasValue)
            .Select(attempt => attempt.Assessment.ReadingId!.Value)
            .Distinct()
            .ToHashSet();

        var nextReading = activeReadings.FirstOrDefault(reading => !completedReadingIds.Contains(reading.ReadingId));

        // Keep these flags friendly to the current frontend while making readings the true entry point.
        var hasCompletedPretest = true;
        var hasCompletedPosttest = false;
        var hasCompletedMinimumReadingIntervention = completedReadings.Count >= _minimumReadingSessionsRequired;
        var canAccessReadings = true;
        var canAccessPosttest = false;
        var canAccessFinalComparison = false;
        var currentStage = ReadingsStage;

        var (recommendedRoute, recommendedMessage) = ResolveRecommendedStep(
            nextReading,
            activeReadings.Count,
            completedReadingIds.Count);

        return new AcademicFlowSummaryDto(
            hasCompletedPretest,
            null,
            completedReadings.Count,
            _minimumReadingSessionsRequired,
            hasCompletedMinimumReadingIntervention,
            latestReadingFinishedAt,
            canAccessReadings,
            canAccessPosttest,
            hasCompletedPosttest,
            null,
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

    private static (string Route, string Message) ResolveRecommendedStep(
        ActiveReadingItem? nextReading,
        int activeReadingsCount,
        int completedReadingIdsCount)
    {
        if (nextReading is not null)
        {
            return (
                $"/readings/{nextReading.ReadingId}",
                $"Continua con el refuerzo lector en \"{nextReading.Title}\".");
        }

        if (activeReadingsCount == 0)
        {
            return (
                "/dashboard",
                "No hay lecturas activas disponibles en este momento.");
        }

        if (completedReadingIdsCount >= activeReadingsCount)
        {
            return (
                "/dashboard",
                "Has completado las lecturas activas de refuerzo disponibles.");
        }

        return (
            "/readings",
            "Continua con tu practica de comprension lectora desde la lista de lecturas.");
    }

    private sealed record ActiveReadingItem(int ReadingId, string Title);
}
