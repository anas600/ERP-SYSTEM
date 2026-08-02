using ERPSystem.Modules.Inventory.Application;
using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Modules.Inventory.Infrastructure;
using ERPSystem.Shared.CompanyContext;

namespace ERPSystem.Modules.Inventory.Application.Services;

public sealed class InventoryResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public InventoryErrorCode? ErrorCode { get; init; }
    public static InventoryResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static InventoryResult<T> Fail(string e, InventoryErrorCode c) => new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum InventoryErrorCode { NotFound, AlreadyExists, ValidationError, Internal }

public interface IItemService
{
    Task<InventoryResult<ItemResponse>> CreateAsync(Guid userId, CreateItemRequest req, CancellationToken ct);
    Task<InventoryResult<ItemResponse>> UpdateAsync(Guid userId, Guid id, UpdateItemRequest req, CancellationToken ct);
    Task<InventoryResult<ItemResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InventoryResult<IReadOnlyList<ItemResponse>>> ListAsync(Guid? companyId, Guid? categoryId, bool includeInactive, int skip, int take, CancellationToken ct);
    Task<InventoryResult<bool>> DeactivateAsync(Guid userId, Guid id, CancellationToken ct);
}

public sealed class ItemService : IItemService
{
    private readonly IItemRepository _repo;
    private readonly ICompanyContext _companyContext;
    public ItemService(IItemRepository r, ICompanyContext companyContext) { _repo = r; _companyContext = companyContext; }

    public async Task<InventoryResult<ItemResponse>> CreateAsync(Guid userId, CreateItemRequest req, CancellationToken ct)
    {
        if (await _repo.GetBySkuAsync(req.Sku, ct) != null)
            return InventoryResult<ItemResponse>.Fail("SKU مستخدم.", InventoryErrorCode.AlreadyExists);
        if (!string.IsNullOrEmpty(req.Barcode) && await _repo.GetByBarcodeAsync(req.Barcode, ct) != null)
            return InventoryResult<ItemResponse>.Fail("الباركود مستخدم.", InventoryErrorCode.AlreadyExists);

        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved — cannot create item without company_id (Constitution Article 3).");
        var now = DateTime.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(), CompanyId = companyId,  // Sprint 25 fix (DEC-085 audit) — was req.CompanyId (client-supplied)
            Sku = req.Sku.Trim(), Barcode = req.Barcode, Name = req.Name.Trim(),
            Description = req.Description, CategoryId = req.CategoryId, UnitOfMeasureId = req.UnitOfMeasureId,
            ItemType = req.ItemType, CostingMethod = req.CostingMethod,
            AverageCost = 0, StandardCost = req.StandardCost,
            InventoryAccountId = req.InventoryAccountId, CogsAccountId = req.CogsAccountId,
            SalesAccountId = req.SalesAccountId, ReorderLevel = req.ReorderLevel,
            ReorderQuantity = req.ReorderQuantity, IsActive = true,
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId
        };
        await _repo.InsertAsync(item, ct);
        return InventoryResult<ItemResponse>.Ok(MapToResponse(item));
    }

    public async Task<InventoryResult<ItemResponse>> UpdateAsync(Guid userId, Guid id, UpdateItemRequest req, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item == null) return InventoryResult<ItemResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        item.Barcode = req.Barcode;
        item.Name = req.Name.Trim();
        item.Description = req.Description;
        item.CategoryId = req.CategoryId;
        item.UnitOfMeasureId = req.UnitOfMeasureId;
        item.CostingMethod = req.CostingMethod;
        item.StandardCost = req.StandardCost;
        item.InventoryAccountId = req.InventoryAccountId;
        item.CogsAccountId = req.CogsAccountId;
        item.SalesAccountId = req.SalesAccountId;
        item.ReorderLevel = req.ReorderLevel;
        item.ReorderQuantity = req.ReorderQuantity;
        item.IsActive = req.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;
        await _repo.UpdateAsync(item, ct);
        return InventoryResult<ItemResponse>.Ok(MapToResponse(item));
    }

    public async Task<InventoryResult<ItemResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item == null) return InventoryResult<ItemResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        return InventoryResult<ItemResponse>.Ok(MapToResponse(item));
    }

    public async Task<InventoryResult<IReadOnlyList<ItemResponse>>> ListAsync(Guid? companyId, Guid? categoryId, bool includeInactive, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _repo.ListAsync(companyId, categoryId, includeInactive, skip, take, ct);
        return InventoryResult<IReadOnlyList<ItemResponse>>.Ok(list.Select(MapToResponse).ToList());
    }

    public async Task<InventoryResult<bool>> DeactivateAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item == null) return InventoryResult<bool>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;
        await _repo.UpdateAsync(item, ct);
        return InventoryResult<bool>.Ok(true);
    }

    private static ItemResponse MapToResponse(Item i) => new()
    {
        Id = i.Id, CompanyId = i.CompanyId, Sku = i.Sku, Barcode = i.Barcode,
        Name = i.Name, Description = i.Description, CategoryId = i.CategoryId, UnitOfMeasureId = i.UnitOfMeasureId,
        ItemType = i.ItemType, CostingMethod = i.CostingMethod, AverageCost = i.AverageCost, StandardCost = i.StandardCost,
        InventoryAccountId = i.InventoryAccountId, CogsAccountId = i.CogsAccountId, SalesAccountId = i.SalesAccountId,
        ReorderLevel = i.ReorderLevel, ReorderQuantity = i.ReorderQuantity, IsActive = i.IsActive
    };
}

public interface IWarehouseService
{
    Task<InventoryResult<WarehouseResponse>> CreateAsync(Guid userId, CreateWarehouseRequest req, CancellationToken ct);
    Task<InventoryResult<WarehouseResponse>> UpdateAsync(Guid userId, Guid id, UpdateWarehouseRequest req, CancellationToken ct);
    Task<InventoryResult<WarehouseResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InventoryResult<IReadOnlyList<WarehouseResponse>>> ListAsync(Guid? companyId, bool includeInactive, CancellationToken ct);
    Task<InventoryResult<bool>> DeactivateAsync(Guid userId, Guid id, CancellationToken ct);
}

public sealed class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repo;
    private readonly ICompanyContext _companyContext;
    public WarehouseService(IWarehouseRepository r, ICompanyContext companyContext) { _repo = r; _companyContext = companyContext; }
    public async Task<InventoryResult<WarehouseResponse>> CreateAsync(Guid userId, CreateWarehouseRequest req, CancellationToken ct)
    {
        if (await _repo.GetByCodeAsync(req.Code, ct) != null)
            return InventoryResult<WarehouseResponse>.Fail("كود المخزن مستخدم.", InventoryErrorCode.AlreadyExists);
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved — cannot create warehouse without company_id (Constitution Article 3).");
        var now = DateTime.UtcNow;
        var w = new Warehouse
        {
            Id = Guid.NewGuid(), CompanyId = companyId,  // Sprint 25 fix (DEC-085 audit)
            Code = req.Code.Trim(), Name = req.Name.Trim(), Location = req.Location, ManagerUserId = req.ManagerUserId,
            IsActive = true, CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId
        };
        await _repo.InsertAsync(w, ct);
        return InventoryResult<WarehouseResponse>.Ok(MapToResponse(w));
    }
    public async Task<InventoryResult<WarehouseResponse>> UpdateAsync(Guid userId, Guid id, UpdateWarehouseRequest req, CancellationToken ct)
    {
        var w = await _repo.GetByIdAsync(id, ct);
        if (w == null) return InventoryResult<WarehouseResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        w.Name = req.Name.Trim();
        w.Location = req.Location;
        w.ManagerUserId = req.ManagerUserId;
        w.IsActive = req.IsActive;
        w.UpdatedAt = DateTime.UtcNow;
        w.UpdatedBy = userId;
        await _repo.UpdateAsync(w, ct);
        return InventoryResult<WarehouseResponse>.Ok(MapToResponse(w));
    }
    public async Task<InventoryResult<WarehouseResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var w = await _repo.GetByIdAsync(id, ct);
        if (w == null) return InventoryResult<WarehouseResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        return InventoryResult<WarehouseResponse>.Ok(MapToResponse(w));
    }
    public async Task<InventoryResult<IReadOnlyList<WarehouseResponse>>> ListAsync(Guid? companyId, bool includeInactive, CancellationToken ct)
    {
        var list = await _repo.ListAsync(companyId, includeInactive, ct);
        return InventoryResult<IReadOnlyList<WarehouseResponse>>.Ok(list.Select(MapToResponse).ToList());
    }
    public async Task<InventoryResult<bool>> DeactivateAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var w = await _repo.GetByIdAsync(id, ct);
        if (w == null) return InventoryResult<bool>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        w.IsActive = false;
        w.UpdatedAt = DateTime.UtcNow;
        w.UpdatedBy = userId;
        await _repo.UpdateAsync(w, ct);
        return InventoryResult<bool>.Ok(true);
    }
    private static WarehouseResponse MapToResponse(Warehouse w) => new()
    {
        Id = w.Id, CompanyId = w.CompanyId, Code = w.Code, Name = w.Name,
        Location = w.Location, ManagerUserId = w.ManagerUserId, IsActive = w.IsActive
    };
}

public interface IUnitOfMeasureService
{
    Task<InventoryResult<UnitOfMeasureResponse>> CreateAsync(CreateUnitOfMeasureRequest req, CancellationToken ct);
    Task<InventoryResult<UnitOfMeasureResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InventoryResult<IReadOnlyList<UnitOfMeasureResponse>>> ListAsync(bool includeInactive, CancellationToken ct);
}

public sealed class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly IUnitOfMeasureRepository _repo;
    public UnitOfMeasureService(IUnitOfMeasureRepository r) => _repo = r;
    public async Task<InventoryResult<UnitOfMeasureResponse>> CreateAsync(CreateUnitOfMeasureRequest req, CancellationToken ct)
    {
        if (await _repo.GetByCodeAsync(req.Code, ct) != null)
            return InventoryResult<UnitOfMeasureResponse>.Fail("كود UoM مستخدم.", InventoryErrorCode.AlreadyExists);
        var u = new UnitOfMeasure
        {
            Id = Guid.NewGuid(), Code = req.Code.Trim(), Name = req.Name.Trim(),
            Symbol = req.Symbol, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        await _repo.InsertAsync(u, ct);
        return InventoryResult<UnitOfMeasureResponse>.Ok(MapToResponse(u));
    }
    public async Task<InventoryResult<UnitOfMeasureResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(id, ct);
        if (u == null) return InventoryResult<UnitOfMeasureResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        return InventoryResult<UnitOfMeasureResponse>.Ok(MapToResponse(u));
    }
    public async Task<InventoryResult<IReadOnlyList<UnitOfMeasureResponse>>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var list = await _repo.ListAsync(includeInactive, ct);
        return InventoryResult<IReadOnlyList<UnitOfMeasureResponse>>.Ok(list.Select(MapToResponse).ToList());
    }
    private static UnitOfMeasureResponse MapToResponse(UnitOfMeasure u) =>
        new() { Id = u.Id, Code = u.Code, Name = u.Name, Symbol = u.Symbol, IsActive = u.IsActive };
}

public interface IItemCategoryService
{
    Task<InventoryResult<ItemCategoryResponse>> CreateAsync(CreateItemCategoryRequest req, CancellationToken ct);
    Task<InventoryResult<ItemCategoryResponse>> UpdateAsync(Guid id, UpdateItemCategoryRequest req, CancellationToken ct);
    Task<InventoryResult<ItemCategoryResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InventoryResult<IReadOnlyList<ItemCategoryResponse>>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<InventoryResult<IReadOnlyList<ItemCategoryResponse>>> GetChildrenAsync(Guid parentId, CancellationToken ct);
}

public sealed class ItemCategoryService : IItemCategoryService
{
    private readonly IItemCategoryRepository _repo;
    public ItemCategoryService(IItemCategoryRepository r) => _repo = r;
    public async Task<InventoryResult<ItemCategoryResponse>> CreateAsync(CreateItemCategoryRequest req, CancellationToken ct)
    {
        if (await _repo.GetByCodeAsync(req.Code, ct) != null)
            return InventoryResult<ItemCategoryResponse>.Fail("كود التصنيف مستخدم.", InventoryErrorCode.AlreadyExists);
        if (req.ParentId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(req.ParentId.Value, ct);
            if (parent == null)
                return InventoryResult<ItemCategoryResponse>.Fail("التصنيف الأب غير موجود.", InventoryErrorCode.NotFound);
        }
        var now = DateTime.UtcNow;
        var c = new ItemCategory
        {
            Id = Guid.NewGuid(), Code = req.Code.Trim(), Name = req.Name.Trim(),
            Description = req.Description, ParentId = req.ParentId, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        };
        await _repo.InsertAsync(c, ct);
        return InventoryResult<ItemCategoryResponse>.Ok(MapToResponse(c));
    }
    public async Task<InventoryResult<ItemCategoryResponse>> UpdateAsync(Guid id, UpdateItemCategoryRequest req, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct);
        if (c == null) return InventoryResult<ItemCategoryResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        c.Name = req.Name.Trim();
        c.Description = req.Description;
        c.ParentId = req.ParentId;
        c.IsActive = req.IsActive;
        c.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(c, ct);
        return InventoryResult<ItemCategoryResponse>.Ok(MapToResponse(c));
    }
    public async Task<InventoryResult<ItemCategoryResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct);
        if (c == null) return InventoryResult<ItemCategoryResponse>.Fail("غير موجود.", InventoryErrorCode.NotFound);
        return InventoryResult<ItemCategoryResponse>.Ok(MapToResponse(c));
    }
    public async Task<InventoryResult<IReadOnlyList<ItemCategoryResponse>>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var list = await _repo.ListAsync(includeInactive, ct);
        return InventoryResult<IReadOnlyList<ItemCategoryResponse>>.Ok(list.Select(MapToResponse).ToList());
    }
    public async Task<InventoryResult<IReadOnlyList<ItemCategoryResponse>>> GetChildrenAsync(Guid parentId, CancellationToken ct)
    {
        var list = await _repo.ListChildrenAsync(parentId, ct);
        return InventoryResult<IReadOnlyList<ItemCategoryResponse>>.Ok(list.Select(MapToResponse).ToList());
    }
    private static ItemCategoryResponse MapToResponse(ItemCategory c) =>
        new() { Id = c.Id, Code = c.Code, Name = c.Name, Description = c.Description, ParentId = c.ParentId, IsActive = c.IsActive };
}
