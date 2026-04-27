using ReadingAdaptive.Application.Catalogs.Dtos;

namespace ReadingAdaptive.Application.Catalogs.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyCollection<RoleCatalogItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DifficultyLevelCatalogItemDto>> GetDifficultyLevelsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DimensionCatalogItemDto>> GetDimensionsAsync(
        CancellationToken cancellationToken = default);
}
