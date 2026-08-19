using System;

namespace ERPSystem.Modules.Projects.Entities;

public enum VariationOrderStatus
{
    Draft = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4
}

public class VariationOrder
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ContractId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;   // "VO-2026-001"
    public DateTime IssuedAt { get; set; }
    public string? Reason { get; set; }
    public VariationOrderStatus Status { get; set; } = VariationOrderStatus.Draft;
    public decimal OriginalContractValue { get; set; }
    public decimal VariationAmount { get; set; }              // can be + or -
    public decimal NewContractValue { get; set; }              // = Original + Variation
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VariationOrderLine
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid VariationOrderId { get; set; }
    public Guid? BoqLineId { get; set; }
    public string LineType { get; set; } = "ADD";            // "ADD" | "MODIFY" | "DELETE"
    public string Description { get; set; } = string.Empty;
    public decimal QtyChange { get; set; }
    public decimal PriceChange { get; set; }
    public decimal NetChange { get; set; }                   // = QtyChange × PriceChange
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
