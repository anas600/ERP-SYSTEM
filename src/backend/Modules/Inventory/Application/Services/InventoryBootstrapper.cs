using System.Data;
using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Modules.Inventory.Infrastructure;
using ERPSystem.Shared.SeedData;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Inventory.Application.Services;

/// <summary>
/// Phase 6.1c: Multi-Company model. UoMs and Categories are global reference
/// data — they don't need a tenantId. Called once at startup (or by tests
/// / re-seed scripts).
/// </summary>
public interface IInventoryBootstrapper
{
    Task EnsureDefaultUoMsAndCategoriesAsync(CancellationToken ct);
    Task EnsureDefaultUoMsAndCategoriesAsync(IDbConnection conn, IDbTransaction? tx, CancellationToken ct);
}

public sealed class InventoryBootstrapper : IInventoryBootstrapper
{
    private readonly IUnitOfMeasureRepository _uoms;
    private readonly IItemCategoryRepository _categories;
    private readonly ILogger<InventoryBootstrapper> _logger;

    public InventoryBootstrapper(IUnitOfMeasureRepository uoms, IItemCategoryRepository categories, ILogger<InventoryBootstrapper> logger)
    {
        _uoms = uoms; _categories = categories; _logger = logger;
    }

    public async Task EnsureDefaultUoMsAndCategoriesAsync(CancellationToken ct)
    {
        if (await _uoms.GetByCodeAsync("pcs", ct) == null)
        {
            foreach (var (code, name, symbol) in DefaultInventorySeed.DefaultUoMs)
            {
                if (await _uoms.GetByCodeAsync(code, ct) == null)
                {
                    await _uoms.InsertAsync(new UnitOfMeasure
                    {
                        Id = Guid.NewGuid(), Code = code, Name = name, Symbol = symbol,
                        IsActive = true, CreatedAt = DateTime.UtcNow
                    }, ct);
                }
            }
            _logger.LogInformation("تم زرع 6 UoMs افتراضية (global reference data)");
        }

        if (await _categories.GetByCodeAsync("RM", ct) == null)
        {
            var idByCode = new Dictionary<string, Guid>();
            foreach (var (code, name, _, parentCode) in DefaultInventorySeed.DefaultCategories.Where(c => c.ParentCode == null))
            {
                var id = Guid.NewGuid();
                await _categories.InsertAsync(new ItemCategory
                {
                    Id = id, Code = code, Name = name, ParentId = null,
                    IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                }, ct);
                idByCode[code] = id;
            }
            _logger.LogInformation("تم زرع 5 تصنيفات افتراضية (global reference data)");
        }
    }

    public async Task EnsureDefaultUoMsAndCategoriesAsync(IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        if (await _uoms.GetByCodeAsync("pcs", ct) == null)
        {
            foreach (var (code, name, symbol) in DefaultInventorySeed.DefaultUoMs)
            {
                if (await _uoms.GetByCodeAsync(code, ct) == null)
                {
                    await _uoms.InsertAsync(new UnitOfMeasure
                    {
                        Id = Guid.NewGuid(), Code = code, Name = name, Symbol = symbol,
                        IsActive = true, CreatedAt = DateTime.UtcNow
                    }, conn, tx, ct);
                }
            }
            _logger.LogInformation("تم زرع 6 UoMs افتراضية (global reference data)");
        }

        if (await _categories.GetByCodeAsync("RM", ct) == null)
        {
            var idByCode = new Dictionary<string, Guid>();
            foreach (var (code, name, _, parentCode) in DefaultInventorySeed.DefaultCategories.Where(c => c.ParentCode == null))
            {
                var id = Guid.NewGuid();
                await _categories.InsertAsync(new ItemCategory
                {
                    Id = id, Code = code, Name = name, ParentId = null,
                    IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                }, conn, tx, ct);
                idByCode[code] = id;
            }
            _logger.LogInformation("تم زرع 5 تصنيفات افتراضية (global reference data)");
        }
    }
}
