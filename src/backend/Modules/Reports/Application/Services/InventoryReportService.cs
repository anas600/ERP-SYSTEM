using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Modules.Reports.Application;

namespace ERPSystem.Modules.Reports.Application.Services;

public interface IInventoryReportService
{
    Task<List<StockValuation>> GetStockValuationAsync(Guid companyId, Guid? subCompanyId, Guid? warehouseId, CancellationToken ct);
    Task<List<StockMovementHistory>> GetMovementHistoryAsync(Guid companyId, Guid? itemId, DateTime? from, DateTime? to, int skip, int take, CancellationToken ct);
    Task<List<LowStockItem>> GetLowStockAsync(Guid companyId, Guid? subCompanyId, CancellationToken ct);
    Task<List<StockAging>> GetStockAgingAsync(Guid companyId, Guid? subCompanyId, CancellationToken ct);
}

public sealed class InventoryReportService : IInventoryReportService
{
    private readonly IDbConnectionFactory _db;
    public InventoryReportService(IDbConnectionFactory db) => _db = db;

    public async Task<List<StockValuation>> GetStockValuationAsync(Guid companyId, Guid? subCompanyId, Guid? warehouseId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = @"
            SELECT sl.item_id AS ItemId, i.sku AS ItemSku, i.name AS ItemName,
                   sl.warehouse_id AS WarehouseId, w.name AS WarehouseName,
                   sl.quantity_on_hand AS QuantityOnHand, sl.average_cost AS AverageCost
            FROM stock_levels sl
            INNER JOIN items i ON i.id = sl.item_id
            INNER JOIN warehouses w ON w.id = sl.warehouse_id
            WHERE sl.company_id = @CompanyId AND sl.quantity_on_hand > 0"
            + (subCompanyId.HasValue ? " AND sl.company_id = @SubCompanyId" : "")
            + (warehouseId.HasValue ? " AND sl.warehouse_id = @WarehouseId" : "")
            + " ORDER BY i.sku, w.code";
        var rows = await conn.QueryAsync<StockValuation>(new CommandDefinition(sql,
            new { CompanyId = companyId, SubCompanyId = subCompanyId, WarehouseId = warehouseId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<List<StockMovementHistory>> GetMovementHistoryAsync(Guid companyId, Guid? itemId, DateTime? from, DateTime? to, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = @"
            SELECT sm.id AS MovementId, sm.reference AS Reference, sm.type AS Type,
                   sm.movement_date AS MovementDate, sm.quantity AS Quantity, sm.unit_cost AS UnitCost,
                   w.code AS WarehouseCode, sm.notes AS Notes, sm.created_at AS CreatedAt
            FROM stock_movements sm
            INNER JOIN warehouses w ON w.id = sm.warehouse_id
            WHERE sm.company_id = @CompanyId"
            + (itemId.HasValue ? " AND sm.item_id = @ItemId" : "")
            + (from.HasValue ? " AND sm.movement_date >= @From" : "")
            + (to.HasValue ? " AND sm.movement_date <= @To" : "")
            + " ORDER BY sm.movement_date DESC, sm.created_at DESC OFFSET @Skip LIMIT @Take";
        var rows = await conn.QueryAsync<StockMovementHistory>(new CommandDefinition(sql,
            new { CompanyId = companyId, ItemId = itemId, From = from, To = to, Skip = skip, Take = take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<List<LowStockItem>> GetLowStockAsync(Guid companyId, Guid? subCompanyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = @"
            SELECT i.id AS ItemId, i.sku AS ItemSku, i.name AS ItemName,
                   sl.warehouse_id AS WarehouseId, w.name AS WarehouseName,
                   sl.quantity_on_hand AS QuantityOnHand, sl.quantity_reserved AS QuantityReserved,
                   i.reorder_level AS ReorderLevel, i.reorder_quantity AS ReorderQuantity
            FROM stock_levels sl
            INNER JOIN items i ON i.id = sl.item_id
            INNER JOIN warehouses w ON w.id = sl.warehouse_id
            WHERE sl.company_id = @CompanyId
              AND i.is_active = true
              AND i.reorder_level > 0
              AND (sl.quantity_on_hand - sl.quantity_reserved) < i.reorder_level"
            + (subCompanyId.HasValue ? " AND sl.company_id = @SubCompanyId" : "")
            + " ORDER BY (i.reorder_level - (sl.quantity_on_hand - sl.quantity_reserved)) DESC";
        var rows = await conn.QueryAsync<LowStockRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, SubCompanyId = subCompanyId }, cancellationToken: ct));
        return rows.Select(r => new LowStockItem
        {
            ItemId = r.ItemId, ItemSku = r.ItemSku, ItemName = r.ItemName,
            WarehouseId = r.WarehouseId, WarehouseName = r.WarehouseName,
            QuantityOnHand = r.QuantityOnHand, QuantityReserved = r.QuantityReserved,
            ReorderLevel = r.ReorderLevel, ReorderQuantity = r.ReorderQuantity
        }).ToList();
    }

    public async Task<List<StockAging>> GetStockAgingAsync(Guid companyId, Guid? subCompanyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = @"
            SELECT i.id AS ItemId, i.sku AS Sku, i.name AS Name,
                   sl.warehouse_id AS WarehouseId, sl.quantity_on_hand AS QuantityOnHand,
                   sl.last_movement_at AS LastMovementAt,
                   EXTRACT(DAY FROM (NOW() - sl.last_movement_at))::int AS DaysInStock
            FROM stock_levels sl
            INNER JOIN items i ON i.id = sl.item_id
            WHERE sl.company_id = @CompanyId AND sl.quantity_on_hand > 0"
            + (subCompanyId.HasValue ? " AND sl.company_id = @SubCompanyId" : "")
            + " ORDER BY DaysInStock DESC";
        var rows = await conn.QueryAsync<StockAgingRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, SubCompanyId = subCompanyId }, cancellationToken: ct));
        return rows.Select(r => new StockAging
        {
            ItemId = r.ItemId, Sku = r.Sku, Name = r.Name, WarehouseId = r.WarehouseId,
            QuantityOnHand = r.QuantityOnHand, LastMovementAt = r.LastMovementAt,
            DaysInStock = r.DaysInStock
        }).ToList();
    }

    private sealed class LowStockRow
    {
        public Guid ItemId { get; set; }
        public string ItemSku { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal ReorderQuantity { get; set; }
    }

    private sealed class StockAgingRow
    {
        public Guid ItemId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public DateTime LastMovementAt { get; set; }
        public int? DaysInStock { get; set; }
    }
}
