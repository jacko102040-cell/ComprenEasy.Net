namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ReadingProgressSummaryDto(
    int TotalActiveReadings,
    int CompletedReadings,
    int InProgressReadings,
    decimal CompletionPercentage,
    decimal? AverageScore,
    decimal? AverageLiteralScore,
    decimal? AverageInferentialScore,
    decimal? AverageCriticalScore,
    int TotalTimeSeconds,
    IReadOnlyCollection<ReadingProgressItemDto> Readings);
