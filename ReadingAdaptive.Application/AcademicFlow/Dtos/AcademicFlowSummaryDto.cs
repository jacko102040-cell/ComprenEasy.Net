namespace ReadingAdaptive.Application.AcademicFlow.Dtos;

public sealed record AcademicFlowSummaryDto(
    bool HasCompletedPretest,
    DateTime? LatestPretestFinishedAt,
    int CompletedReadingSessions,
    int MinimumReadingSessionsRequired,
    bool HasCompletedMinimumReadingIntervention,
    DateTime? LatestReadingFinishedAt,
    bool CanAccessReadings,
    bool CanAccessPosttest,
    bool HasCompletedPosttest,
    DateTime? LatestPosttestFinishedAt,
    bool CanAccessFinalComparison,
    string CurrentStage,
    string RecommendedRoute,
    string RecommendedMessage);
