namespace ReadingAdaptive.Application.Readings.Dtos;

public sealed record ReadingPhaseDto(
    byte PhaseId,
    string Code,
    string DisplayName,
    byte DisplayOrder,
    bool IsEnabled,
    bool IsRequired,
    string? GuidanceText,
    byte? MinQuestionsToUnlockNext);
