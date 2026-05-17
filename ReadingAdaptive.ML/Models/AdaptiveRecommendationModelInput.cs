namespace ReadingAdaptive.ML.Models;

public sealed class AdaptiveRecommendationModelInput
{
    public float TotalScore { get; set; }

    public float LiteralScore { get; set; }

    public float InferentialScore { get; set; }

    public float CriticalScore { get; set; }

    public float TotalCorrect { get; set; }

    public float TotalErrors { get; set; }

    public float AnsweredQuestions { get; set; }

    public float TotalQuestions { get; set; }

    public float CompletionPercentage { get; set; }

    public float TotalTimeSeconds { get; set; }

    public float AverageTimeSeconds { get; set; }

    public float CurrentDifficultyRank { get; set; }

    public float PreviousProgressDelta { get; set; }

    public float CompletedReadingSessions { get; set; }

    public string PredictedAction { get; set; } = string.Empty;
}
