namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record AssessmentDetailDto(
    int AssessmentId,
    string AssessmentType,
    string Title,
    string? Description,
    byte? DifficultyLevelId,
    IReadOnlyCollection<AssessmentQuestionDto> Questions);
