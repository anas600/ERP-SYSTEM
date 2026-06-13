using System;

namespace ERPSystem.Modules.Inventory.Entities;

/// <summary>
/// Warehouse - represents a physical storage location
/// Phase 2.3: Inventory Module
/// </summary>
public class Warehouse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
