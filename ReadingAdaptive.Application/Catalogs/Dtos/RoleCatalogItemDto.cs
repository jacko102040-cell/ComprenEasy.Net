namespace ReadingAdaptive.Application.Catalogs.Dtos;

public sealed record RoleCatalogItemDto(
    int RoleId,
    string Name,
    bool IsActive);
