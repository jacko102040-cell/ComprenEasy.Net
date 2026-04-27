namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record PhaseProgressDto(
    byte PhaseId,
    string Code,
    string DisplayName,
    byte SequenceOrder,
    bool IsRequired,
    byte? MinQuestionsToUnlockNext,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int TimeSpentSeconds,
    string? GuidanceText,
    int AnsweredQuestions,
    int TotalQuestions,
    IReadOnlyCollection<ReadingPhaseQuestionDto> Questions);
