namespace ERPSystem.Modules.Projects.Application.Dtos;

public record PriceListDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? IssuedBy,
    DateTime? IssuedAt,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    int ItemCount
);

public record PriceListItemDto(
    Guid Id,
    Guid PriceListId,
    string Code,
    string? ParentCode,
    string Description,
    Guid UnitId,
    string? UnitCode,
    decimal UnitPrice,
    string? Section,
    string? Category,
    int Level
);

public record CreatePriceListRequest(
    string Code,
    string Name,
    string? Description,
    string? IssuedBy,
    DateTime? IssuedAt,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo
);

public record CreatePriceListItemRequest(
    string Code,
    string? ParentCode,
    string Description,
    Guid UnitId,
    decimal UnitPrice,
    string? Section,
    string? Category,
    int Level
);
