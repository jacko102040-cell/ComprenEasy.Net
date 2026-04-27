namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed record ActiveAssessmentDto(
    int AssessmentId,
    string AssessmentType,
    string Title,
    string? Description,
    byte? DifficultyLevelId,
    int QuestionCount);
