namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ReadingDetailDto(
    int ReadingId,
    string Title,
    string? Summary,
    string Content,
    string? ImageUrl,
    byte DifficultyLevelId,
    string DifficultyLevelName,
    int? EstimatedMinutes);
