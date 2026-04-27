using System.ComponentModel.DataAnnotations;

namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed class SaveReadingPhaseProgressRequestDto
{
    [Range(0, int.MaxValue)]
    public int TimeSpentSeconds { get; set; }

    [StringLength(200)]
    public string? ProgressNote { get; set; }

    public IReadOnlyCollection<SaveReadingPhaseAnswerItemDto> Answers { get; set; } = [];
}
