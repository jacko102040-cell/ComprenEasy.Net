namespace ReadingAdaptive.Application.Adaptive.Dtos;

public sealed record AdaptiveRecommendationDto(
    long RecommendationId,
    long SourceAttemptId,
    int SourceAssessmentId,
    string SourceAssessmentType,
    string SourceAssessmentTitle,
    byte CurrentDifficultyLevelId,
    string CurrentDifficultyLevelName,
    byte RecommendedDifficultyLevelId,
    string RecommendedDifficultyLevelName,
    string PredictedAction,
    int? RecommendedAssessmentId,
    string? RecommendedAssessmentTitle,
    string EngineType,
    decimal? ConfidenceScore,
    DateTime CreatedAt);
