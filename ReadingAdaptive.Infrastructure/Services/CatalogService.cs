using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Application.Catalogs.Dtos;
using ReadingAdaptive.Application.Catalogs.Interfaces;
using ReadingAdaptive.Infrastructure.Persistence;

namespace ReadingAdaptive.Infrastructure.Services;

public sealed class CatalogService : ICatalogService
{
    private readonly ReadingAdaptiveDbContext _dbContext;

    public CatalogService(ReadingAdaptiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RoleCatalogItemDto>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleCatalogItemDto(
                role.RoleId,
                role.Name,
                role.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DifficultyLevelCatalogItemDto>> GetDifficultyLevelsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DifficultyLevels
            .AsNoTracking()
            .OrderBy(level => level.RankOrder)
            .Select(level => new DifficultyLevelCatalogItemDto(
                level.DifficultyLevelId,
                level.Name,
                level.RankOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DimensionCatalogItemDto>> GetDimensionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Dimensions
            .AsNoTracking()
            .OrderBy(dimension => dimension.DimensionId)
            .Select(dimension => new DimensionCatalogItemDto(
                dimension.DimensionId,
                dimension.Name,
                dimension.Description))
            .ToListAsync(cancellationToken);
    }
}
