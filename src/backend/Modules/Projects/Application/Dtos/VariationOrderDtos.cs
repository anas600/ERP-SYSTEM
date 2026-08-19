namespace ERPSystem.Modules.Projects.Application.Dtos;

public record VariationOrderDto(
    Guid Id,
    Guid ProjectId,
    Guid? ContractId,
    string OrderNumber,
    DateTime IssuedAt,
    string? Reason,
    string Status,
    decimal OriginalContractValue,
    decimal VariationAmount,
    decimal NewContractValue,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    string? Notes,
    int LinesCount
);

public record VariationOrderLineDto(
    Guid Id,
    Guid VariationOrderId,
    Guid? BoqLineId,
    string LineType,
    string Description,
    decimal QtyChange,
    decimal PriceChange,
    decimal NetChange,
    int SortOrder
);

public record CreateVariationOrderRequest(
    Guid ProjectId,
    Guid? ContractId,
    string OrderNumber,
    DateTime IssuedAt,
    string? Reason,
    string? Notes,
    decimal OriginalContractValue
);

public record CreateVariationOrderLineRequest(
    Guid VariationOrderId,
    Guid? BoqLineId,
    string LineType,
    string Description,
    decimal QtyChange,
    decimal PriceChange,
    int? SortOrder
);
