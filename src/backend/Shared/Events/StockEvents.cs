using System;

namespace ERPSystem.Shared.Events;

/// <summary>
/// Integration event contracts for cross-module communication
/// </summary>
public interface IIntegrationEvent
{
    Guid TenantId { get; }
    DateTime OccurredAt { get; }
}

/// <summary>
/// Published by Inventory when stock is received
/// Consumed by Finance to create Journal Entry (DR Inventory Asset, CR A/P)
/// </summary>
public record StockReceivedEvent(
    Guid TenantId,
    Guid StockMovementId,
    Guid ItemId,
    Guid WarehouseId,
    decimal Quantity,
    decimal UnitCost,
    string PurchaseOrderRef
) : IIntegrationEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Published by Inventory when stock is issued (consumed or sold)
/// Consumed by Finance to post cost of goods
/// </summary>
public record StockIssuedEvent(
    Guid TenantId,
    Guid StockMovementId,
    Guid ItemId,
    Guid WarehouseId,
    decimal Quantity,
    string ReferenceType,
    Guid? ReferenceId,
    DateTime IssuedAt
) : IIntegrationEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Published by Finance when a Journal Entry is posted
/// Consumed by other modules to update balances
/// </summary>
public record JournalEntryPostedEvent(
    Guid TenantId,
    Guid JournalEntryId,
    string Reference,
    DateTime PostedAt
) : IIntegrationEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Published by Projects when materials are requested
/// Consumed by Inventory to reserve stock
/// </summary>
public record ProjectMaterialRequestedEvent(
    Guid TenantId,
    Guid ProjectId,
    Guid ItemId,
    decimal Quantity,
    DateTime NeededBy
) : IIntegrationEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
