using ReadingAdaptive.Application.Adaptive.Dtos;

namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherStudentDetailDto(
    int StudentId,
    string FullName,
    string Username,
    byte Grade,
    string? Section,
    bool IsEnabledForTest,
    bool IsActive,
    TeacherStudentProgressSummaryDto ProgressSummary,
    IReadOnlyCollection<TeacherStudentAssessmentAttemptDto> EvaluationAttempts,
    IReadOnlyCollection<TeacherStudentReadingSessionDto> ReadingSessions,
    IReadOnlyCollection<AdaptiveRecommendationDto> RecentRecommendations,
    IReadOnlyCollection<TeacherStudentCommentDto> Comments,
    IReadOnlyCollection<TeacherCommentTagDto> AvailableCommentTags);
