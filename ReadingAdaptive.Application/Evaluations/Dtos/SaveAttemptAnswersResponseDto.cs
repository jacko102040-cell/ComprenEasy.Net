namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record SaveAttemptAnswersResponseDto(
    long AttemptId,
    int SavedAnswers,
    int AnsweredQuestions,
    int TotalQuestions,
    decimal CompletionPercentage);
