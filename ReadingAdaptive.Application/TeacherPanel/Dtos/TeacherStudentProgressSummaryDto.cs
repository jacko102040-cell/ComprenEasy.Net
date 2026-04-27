namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherStudentProgressSummaryDto(
    int CompletedEvaluationAttempts,
    int CompletedReadingSessions,
    decimal? LatestCompletedScore,
    string? LatestRecommendationAction,
    DateTime? LastActivityAt);
