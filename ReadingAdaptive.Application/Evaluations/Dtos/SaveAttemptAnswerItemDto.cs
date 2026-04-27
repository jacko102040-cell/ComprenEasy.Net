using System.ComponentModel.DataAnnotations;

namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed class SaveAttemptAnswerItemDto
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(1, int.MaxValue)]
    public int SelectedOptionId { get; set; }

    [Range(0, int.MaxValue)]
    public int? AnswerTimeSeconds { get; set; }
}
