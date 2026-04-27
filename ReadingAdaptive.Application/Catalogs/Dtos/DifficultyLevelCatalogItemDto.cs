namespace ReadingAdaptive.Application.Catalogs.Dtos;

public sealed record DifficultyLevelCatalogItemDto(
    int DifficultyLevelId,
    string Name,
    byte RankOrder);
