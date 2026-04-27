namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record AssessmentQuestionDto(
    int QuestionId,
    byte DisplayOrder,
    byte DimensionId,
    string DimensionName,
    string Stem,
    decimal Points,
    IReadOnlyCollection<AssessmentOptionDto> Options);
