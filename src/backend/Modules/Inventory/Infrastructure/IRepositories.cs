using System.Data;
using ERPSystem.Modules.Inventory.Entities;

namespace ERPSystem.Modules.Inventory.Infrastructure;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Item?> GetBySkuAsync(string sku, CancellationToken ct);
    Task<Item?> GetByBarcodeAsync(string barcode, CancellationToken ct);
    Task<IReadOnlyList<Item>> ListAsync(Guid? companyId, Guid? categoryId, bool includeInactive, int skip, int take, CancellationToken ct);
    Task InsertAsync(Item item, CancellationToken ct);
    Task UpdateAsync(Item item, CancellationToken ct);
}

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Warehouse>> ListAsync(Guid? companyId, bool includeInactive, CancellationToken ct);
    Task<IReadOnlyList<Warehouse>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct); // DEC-031
    Task InsertAsync(Warehouse warehouse, CancellationToken ct);
    Task UpdateAsync(Warehouse warehouse, CancellationToken ct);
}

public interface IUnitOfMeasureRepository
{
    Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UnitOfMeasure?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<UnitOfMeasure>> ListAsync(bool includeInactive, CancellationToken ct);
    Task InsertAsync(UnitOfMeasure uom, CancellationToken ct);
    Task InsertAsync(UnitOfMeasure uom, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task UpdateAsync(UnitOfMeasure uom, CancellationToken ct);
}

public interface IItemCategoryRepository
{
    Task<ItemCategory?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ItemCategory?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<ItemCategory>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<IReadOnlyList<ItemCategory>> ListChildrenAsync(Guid parentId, CancellationToken ct);
    Task InsertAsync(ItemCategory category, CancellationToken ct);
    Task InsertAsync(ItemCategory category, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task UpdateAsync(ItemCategory category, CancellationToken ct);
}
