using System;

namespace ERPSystem.Modules.Inventory.Entities;

/// <summary>
/// Item - represents a product or raw material
/// Phase 2.3: Inventory Module
/// </summary>
public class Item
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public ItemType ItemType { get; set; }
    public CostingMethod CostingMethod { get; set; } = CostingMethod.Average;
    public decimal AverageCost { get; set; }
    public Guid? InventoryAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public enum ItemType
{
    RawMaterial = 1,
    FinishedGood = 2,
    Service = 3,
    Consumable = 4
}

public enum CostingMethod
{
    FIFO = 1,
    LIFO = 2,
    Average = 3
}
