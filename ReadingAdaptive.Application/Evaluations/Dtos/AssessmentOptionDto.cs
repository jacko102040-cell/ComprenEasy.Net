namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record AssessmentOptionDto(
    int OptionId,
    string OptionText,
    byte DisplayOrder);
