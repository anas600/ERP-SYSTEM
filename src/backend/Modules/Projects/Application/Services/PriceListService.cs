using Dapper;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IPriceListService
{
    Task<List<PriceListDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<PriceListDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Guid> CreateAsync(Guid companyId, Guid userId, CreatePriceListRequest req, CancellationToken ct);
    Task<List<PriceListItemDto>> ListItemsAsync(Guid priceListId, CancellationToken ct);
    Task<Guid> CreateItemAsync(Guid companyId, Guid priceListId, CreatePriceListItemRequest req, CancellationToken ct);
}

public sealed class PriceListService : IPriceListService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PriceListService> _logger;

    public PriceListService(IDbConnectionFactory db, ILogger<PriceListService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<List<PriceListDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT pl.id, pl.code, pl.name, pl.description, pl.issued_by, pl.issued_at,
       pl.effective_from, pl.effective_to, pl.is_active,
       (SELECT COUNT(*) FROM price_list_items WHERE price_list_id = pl.id) AS item_count
FROM price_lists pl
WHERE pl.company_id = @CompanyId AND pl.is_active = true
ORDER BY pl.created_at DESC;";
        var rows = await conn.QueryAsync(sql, new { CompanyId = companyId });
        return rows.Select(r => new PriceListDto(
            (Guid)r.id, (string)r.code, (string)r.name, (string?)r.description,
            (string?)r.issued_by, (DateTime?)r.issued_at, (DateTime?)r.effective_from,
            (DateTime?)r.effective_to, (bool)r.is_active, (int)(long)r.item_count
        )).ToList();
    }

    public async Task<PriceListDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT pl.id, pl.code, pl.name, pl.description, pl.issued_by, pl.issued_at,
       pl.effective_from, pl.effective_to, pl.is_active,
       (SELECT COUNT(*) FROM price_list_items WHERE price_list_id = pl.id) AS item_count
FROM price_lists pl WHERE pl.id = @Id;";
        var r = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });
        if (r == null) return null;
        return new PriceListDto(
            (Guid)r.id, (string)r.code, (string)r.name, (string?)r.description,
            (string?)r.issued_by, (DateTime?)r.issued_at, (DateTime?)r.effective_from,
            (DateTime?)r.effective_to, (bool)r.is_active, (int)(long)r.item_count
        );
    }

    public async Task<Guid> CreateAsync(Guid companyId, Guid userId, CreatePriceListRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        const string sql = @"
INSERT INTO price_lists (id, company_id, code, name, description, issued_by, issued_at,
                         effective_from, effective_to, is_active, created_at, created_by, updated_at)
VALUES (@Id, @CompanyId, @Code, @Name, @Description, @IssuedBy, @IssuedAt,
        @EffectiveFrom, @EffectiveTo, true, now(), @CreatedBy, now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId,
            req.Code, req.Name, req.Description, req.IssuedBy, req.IssuedAt,
            req.EffectiveFrom, req.EffectiveTo,
            CreatedBy = userId
        });
        return id;
    }

    public async Task<List<PriceListItemDto>> ListItemsAsync(Guid priceListId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT pli.id, pli.price_list_id, pli.code, pli.parent_code, pli.description,
       pli.unit_id, u.code AS unit_code, pli.unit_price, pli.section, pli.category, pli.level
FROM price_list_items pli
LEFT JOIN units_of_measure u ON u.id = pli.unit_id
WHERE pli.price_list_id = @PriceListId AND pli.is_active = true
ORDER BY pli.code;";
        var rows = await conn.QueryAsync(sql, new { PriceListId = priceListId });
        return rows.Select(r => new PriceListItemDto(
            (Guid)r.id, (Guid)r.price_list_id, (string)r.code, (string?)r.parent_code,
            (string)r.description, (Guid)r.unit_id, (string?)r.unit_code,
            (decimal)r.unit_price, (string?)r.section, (string?)r.category, (int)r.level
        )).ToList();
    }

    public async Task<Guid> CreateItemAsync(Guid companyId, Guid priceListId, CreatePriceListItemRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        const string sql = @"
INSERT INTO price_list_items (id, company_id, price_list_id, code, parent_code, description,
                              unit_id, unit_price, section, category, level, is_active, created_at)
VALUES (@Id, @CompanyId, @PriceListId, @Code, @ParentCode, @Description,
        @UnitId, @UnitPrice, @Section, @Category, @Level, true, now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId, PriceListId = priceListId,
            req.Code, req.ParentCode, req.Description, req.UnitId, req.UnitPrice,
            req.Section, req.Category, req.Level
        });
        return id;
    }
}
