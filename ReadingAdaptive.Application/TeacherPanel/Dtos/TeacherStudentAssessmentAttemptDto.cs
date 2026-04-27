namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherStudentAssessmentAttemptDto(
    long AttemptId,
    int AssessmentId,
    string AssessmentTitle,
    string AssessmentType,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal? TotalScore,
    decimal? CompletionPercentage,
    int? TotalTimeSeconds);
