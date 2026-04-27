namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ActiveReadingDto(
    int ReadingId,
    string Title,
    string? Summary,
    string? ImageUrl,
    byte DifficultyLevelId,
    string DifficultyLevelName,
    int? EstimatedMinutes,
    int ActivePhaseCount);
