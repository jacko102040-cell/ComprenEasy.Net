namespace ReadingAdaptive.Application.TeacherPanel.Dtos;

public sealed record TeacherStudentReadingSessionDto(
    long AttemptId,
    int AssessmentId,
    int? ReadingId,
    string ReadingTitle,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal? CompletionPercentage,
    int? TotalTimeSeconds);
