namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ReadingPhaseQuestionOptionDto(
    int OptionId,
    string OptionText,
    byte DisplayOrder);
