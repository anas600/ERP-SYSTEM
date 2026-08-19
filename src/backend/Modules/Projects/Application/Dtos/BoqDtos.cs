namespace ERPSystem.Modules.Projects.Application.Dtos;

public record BoqSectionDto(
    Guid Id,
    Guid ProjectId,
    string Code,
    string Name,
    int SortOrder,
    int LinesCount
);

public record BoqLineDto(
    Guid Id,
    Guid SectionId,
    Guid? PriceListItemId,
    string Code,
    string Description,
    Guid UnitId,
    string? UnitCode,
    decimal ContractQty,
    decimal ExecutedQty,
    decimal UnitPrice,
    decimal RegionalPremiumPct,
    decimal FinalUnitPrice,
    decimal TotalAmount,
    bool IsMeasurable,
    bool IsActive,
    int SortOrder
);

public record BoqSubitemDto(
    Guid Id,
    Guid BoqLineId,
    string Description,
    int Count,
    decimal LengthM,
    decimal WidthM,
    decimal HeightM,
    decimal InitialQty,
    decimal Deductions,
    decimal FinalQty,
    int SortOrder
);

public record CreateBoqSectionRequest(
    string Code,
    string Name,
    int? SortOrder
);

public record CreateBoqLineRequest(
    Guid SectionId,
    Guid? PriceListItemId,
    string Code,
    string Description,
    Guid UnitId,
    decimal ContractQty,
    decimal UnitPrice,
    decimal RegionalPremiumPct,
    bool IsMeasurable,
    int? SortOrder
);

public record CreateBoqSubitemRequest(
    Guid BoqLineId,
    string Description,
    int Count,
    decimal LengthM,
    decimal WidthM,
    decimal HeightM,
    decimal Deductions,
    int? SortOrder
);
