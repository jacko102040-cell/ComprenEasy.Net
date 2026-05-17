namespace ReadingAdaptive.Application.Adaptive.Dtos;

public sealed record AdaptiveMlPredictionRequest(
    float TotalScore,
    float LiteralScore,
    float InferentialScore,
    float CriticalScore,
    float TotalCorrect,
    float TotalErrors,
    float AnsweredQuestions,
    float TotalQuestions,
    float CompletionPercentage,
    float TotalTimeSeconds,
    float AverageTimeSeconds,
    float CurrentDifficultyRank,
    float PreviousProgressDelta,
    float CompletedReadingSessions);
