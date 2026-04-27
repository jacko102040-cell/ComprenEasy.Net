namespace ReadingAdaptive.Application.Evaluations.Dtos;

/// <summary>
/// Comparative summary between the latest completed pretest and posttest for a student.
/// Score properties are handled as percentages in the 0-100 range.
/// </summary>
public sealed record PrePostComparisonSummaryDto(
    bool IsAvailable,
    string Message,
    DateTime? PretestFinishedAt,
    DateTime? PosttestFinishedAt,
    decimal? PretestTotalScore,
    decimal? PosttestTotalScore,
    decimal? ImprovementTotalScore,
    decimal? PretestLiteralScore,
    decimal? PosttestLiteralScore,
    decimal? ImprovementLiteralScore,
    decimal? PretestInferentialScore,
    decimal? PosttestInferentialScore,
    decimal? ImprovementInferentialScore,
    decimal? PretestCriticalScore,
    decimal? PosttestCriticalScore,
    decimal? ImprovementCriticalScore);
