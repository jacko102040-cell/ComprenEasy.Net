using System.ComponentModel.DataAnnotations;

namespace ReadingAdaptive.Application.Evaluations.Dtos;

public sealed class SaveAttemptAnswersRequestDto
{
    [Required]
    [MinLength(1)]
    public List<SaveAttemptAnswerItemDto> Answers { get; set; } = [];
}
