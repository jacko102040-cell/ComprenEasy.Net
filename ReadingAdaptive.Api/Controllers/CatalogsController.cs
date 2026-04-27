using Microsoft.AspNetCore.Mvc;
using ReadingAdaptive.Application.Catalogs.Dtos;
using ReadingAdaptive.Application.Catalogs.Interfaces;

namespace ReadingAdaptive.Api.Controllers;

[ApiController]
[Route("api/catalogs")]
public class CatalogsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleCatalogItemDto>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var roles = await _catalogService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("difficulty-levels")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DifficultyLevelCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DifficultyLevelCatalogItemDto>>> GetDifficultyLevels(
        CancellationToken cancellationToken)
    {
        var levels = await _catalogService.GetDifficultyLevelsAsync(cancellationToken);
        return Ok(levels);
    }

    [HttpGet("dimensions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DimensionCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DimensionCatalogItemDto>>> GetDimensions(
        CancellationToken cancellationToken)
    {
        var dimensions = await _catalogService.GetDimensionsAsync(cancellationToken);
        return Ok(dimensions);
    }
}
