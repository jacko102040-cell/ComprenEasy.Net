namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ReadingSessionProgressDto(
    long AttemptId,
    int ReadingId,
    int AssessmentId,
    string ReadingTitle,
    string SessionStatus,
    byte AttemptNumber,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal CompletionPercentage,
    decimal TotalScore,
    decimal LiteralScore,
    decimal InferentialScore,
    decimal CriticalScore,
    int TotalCorrect,
    int TotalErrors,
    int TotalTimeSeconds,
    byte? CurrentPhaseId,
    string? CurrentPhaseCode,
    IReadOnlyCollection<PhaseProgressDto> Phases);
