namespace ReadingAdaptive.Application.Catalogs.Dtos;

public sealed record DimensionCatalogItemDto(
    int DimensionId,
    string Name,
    string? Description);
