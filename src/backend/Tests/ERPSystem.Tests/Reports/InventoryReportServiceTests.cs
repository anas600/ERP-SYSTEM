using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Tests.Common;
using FluentAssertions;

namespace ERPSystem.Tests.Reports;

/// <summary>اختبارات خدمة تقارير Inventory — تعتمد على FakeDbConnectionFactory</summary>
public class InventoryReportServiceTests
{
    private static (InventoryReportService svc, FakeDbConnectionFactory db, Guid tenantId) Build()
    {
        var tenant = Guid.NewGuid();
        var db = new FakeDbConnectionFactory();
        return (new InventoryReportService(db), db, tenant);
    }

    [Fact]
    public async Task GetStockValuation_PositiveQuantity_ReturnsItemsWithTotalValue()
    {
        var (svc, db, tenant) = Build();
        var itemId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        db.AddRow("items", "id", itemId, "tenant_id", tenant, "company_id", companyId, "sku", "ITM-001", "name", "صنف 1");
        db.AddRow("warehouses", "id", warehouseId, "tenant_id", tenant, "company_id", companyId, "code", "WH-01", "name", "مخزن 1");
        db.AddRow("stock_levels", "tenant_id", tenant, "company_id", companyId, "item_id", itemId,
            "warehouse_id", warehouseId, "quantity_on_hand", 100m, "average_cost", 5.5m);

        var rows = await svc.GetStockValuationAsync(tenant, null, null, CancellationToken.None);

        rows.Should().HaveCount(1);
        rows[0].QuantityOnHand.Should().Be(100);
        rows[0].AverageCost.Should().Be(5.5m);
    }

    [Fact]
    public async Task GetStockValuation_FilteredByWarehouse_RespectsFilter()
    {
        var (svc, db, tenant) = Build();
        var item1 = Guid.NewGuid();
        var wh1 = Guid.NewGuid();
        var wh2 = Guid.NewGuid();

        db.AddRow("items", "id", item1, "tenant_id", tenant, "sku", "A", "name", "صنف A");
        db.AddRow("warehouses", "id", wh1, "tenant_id", tenant, "code", "W1", "name", "مخزن 1");
        db.AddRow("warehouses", "id", wh2, "tenant_id", tenant, "code", "W2", "name", "مخزن 2");
        db.AddRow("stock_levels", "tenant_id", tenant, "item_id", item1, "warehouse_id", wh1,
            "quantity_on_hand", 50m, "average_cost", 10m);
        db.AddRow("stock_levels", "tenant_id", tenant, "item_id", item1, "warehouse_id", wh2,
            "quantity_on_hand", 30m, "average_cost", 12m);

        var wh1Only = await svc.GetStockValuationAsync(tenant, null, wh1, CancellationToken.None);

        wh1Only.Should().HaveCount(1, "لازم يرجع فقط stock في المخزن المطلوب");
        wh1Only[0].WarehouseId.Should().Be(wh1);
        wh1Only[0].QuantityOnHand.Should().Be(50);
    }

    [Fact]
    public async Task GetLowStock_BelowReorderLevel_ReturnsItemWithCorrectStatus()
    {
        var (svc, db, tenant) = Build();
        var itemId = Guid.NewGuid();
        var whId = Guid.NewGuid();

        db.AddRow("items", "id", itemId, "tenant_id", tenant, "sku", "X", "name", "صنف X",
            "is_active", true, "reorder_level", 50m, "reorder_quantity", 100m);
        db.AddRow("warehouses", "id", whId, "tenant_id", tenant, "code", "WH", "name", "مخزن");
        db.AddRow("stock_levels", "tenant_id", tenant, "item_id", itemId, "warehouse_id", whId,
            "quantity_on_hand", 10m, "quantity_reserved", 0m);

        var low = await svc.GetLowStockAsync(tenant, null, CancellationToken.None);

        low.Should().HaveCount(1);
        low[0].ItemSku.Should().Be("X");
        low[0].ReorderLevel.Should().Be(50);
        low[0].Status.Should().Be("Critical", "الكمية المتاحة 10 أقل من 25 (نصف الـ reorder)");
    }

    [Fact]
    public async Task GetLowStock_ZeroQuantity_CriticalStatus()
    {
        var (svc, db, tenant) = Build();
        var itemId = Guid.NewGuid();
        var whId = Guid.NewGuid();

        db.AddRow("items", "id", itemId, "tenant_id", tenant, "sku", "Y", "name", "صنف Y",
            "is_active", true, "reorder_level", 10m, "reorder_quantity", 50m);
        db.AddRow("warehouses", "id", whId, "tenant_id", tenant, "code", "WH", "name", "مخزن");
        db.AddRow("stock_levels", "tenant_id", tenant, "item_id", itemId, "warehouse_id", whId,
            "quantity_on_hand", 0m, "quantity_reserved", 0m);

        var low = await svc.GetLowStockAsync(tenant, null, CancellationToken.None);

        low.Should().HaveCount(1);
        low[0].Status.Should().Be("Critical", "الكمية 0 = Critical");
    }

    [Fact]
    public async Task GetStockAging_DaysMapping_CategorizesCorrectly()
    {
        var (svc, db, tenant) = Build();
        var itemId = Guid.NewGuid();
        var whId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.AddRow("items", "id", itemId, "tenant_id", tenant, "sku", "Z", "name", "صنف Z");
        db.AddRow("stock_levels", "tenant_id", tenant, "item_id", itemId, "warehouse_id", whId,
            "quantity_on_hand", 100m, "last_movement_at", now.AddDays(-45));

        var aging = await svc.GetStockAgingAsync(tenant, null, CancellationToken.None);

        aging.Should().HaveCount(1);
        aging[0].Sku.Should().Be("Z");
        aging[0].AgeBucket.Should().Be("31-60", "45 يوم يقع في الفترة 31-60");
    }

    [Fact]
    public void StockValuation_Dto_TotalValue_CalculatesCorrectly()
    {
        var v = new StockValuation { QuantityOnHand = 10, AverageCost = 7.5m };
        v.TotalValue.Should().Be(75m, "10 × 7.5 = 75");
    }

    [Fact]
    public void LowStockItem_Dto_Shortfall_CalculatesCorrectly()
    {
        var item = new LowStockItem
        {
            ReorderLevel = 100,
            QuantityOnHand = 30,
            QuantityReserved = 5
        };
        item.QuantityAvailable.Should().Be(25, "30 - 5 = 25");
        item.Shortfall.Should().Be(75, "100 - 25 = 75");
    }
}
