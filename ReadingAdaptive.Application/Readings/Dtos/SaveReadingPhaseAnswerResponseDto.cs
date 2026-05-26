namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record SaveReadingPhaseAnswerResponseDto(
    long AttemptId,
    byte PhaseId,
    int SavedAnswers,
    int PhaseAnsweredQuestions,
    int PhaseTotalQuestions,
    int TotalAnsweredQuestions,
    int TotalQuestions,
    decimal CompletionPercentage);
