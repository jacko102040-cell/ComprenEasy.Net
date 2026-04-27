namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record AttemptAnswerResultDto(
    int QuestionId,
    string Stem,
    string DimensionName,
    int? SelectedOptionId,
    bool? IsCorrect,
    decimal ScoreObtained,
    int AnswerTimeSeconds);
