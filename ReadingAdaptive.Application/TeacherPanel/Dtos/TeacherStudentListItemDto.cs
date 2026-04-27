namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherStudentListItemDto(
    int StudentId,
    string FullName,
    string Username,
    byte Grade,
    string? Section,
    bool IsEnabledForTest,
    bool IsActive,
    DateTime? LastActivityAt,
    decimal? LatestScore,
    string? LatestRecommendationAction);
