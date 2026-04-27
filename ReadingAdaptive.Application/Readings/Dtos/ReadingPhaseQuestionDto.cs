namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ReadingPhaseQuestionDto(
    int QuestionId,
    byte DisplayOrder,
    byte DimensionId,
    string DimensionName,
    string Stem,
    decimal Points,
    int? SelectedOptionId,
    bool? IsCorrect,
    decimal ScoreObtained,
    int AnswerTimeSeconds,
    IReadOnlyCollection<ReadingPhaseQuestionOptionDto> Options);
