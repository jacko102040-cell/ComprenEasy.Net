namespace ReadingAdaptive.Application.Evaluations.Dtos;

/// <summary>
/// Result snapshot for an assessment attempt.
/// TotalScore, LiteralScore, InferentialScore and CriticalScore are handled as percentages in the 0-100 range.
/// </summary>
public sealed record AssessmentAttemptResultDto(
    long AttemptId,
    int AssessmentId,
    string AssessmentType,
    string AssessmentTitle,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal TotalScore,
    decimal LiteralScore,
    decimal InferentialScore,
    decimal CriticalScore,
    int TotalCorrect,
    int TotalErrors,
    int TotalTimeSeconds,
    decimal CompletionPercentage,
    IReadOnlyCollection<AttemptAnswerResultDto> Answers);
