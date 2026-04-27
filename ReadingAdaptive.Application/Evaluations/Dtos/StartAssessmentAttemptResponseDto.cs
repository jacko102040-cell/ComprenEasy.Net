namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record StartAssessmentAttemptResponseDto(
    long AttemptId,
    int AssessmentId,
    byte AttemptNumber,
    string Status,
    DateTime StartedAt);
