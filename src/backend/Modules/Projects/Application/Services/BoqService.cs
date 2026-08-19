using Dapper;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IBoqService
{
    Task<List<BoqSectionDto>> ListSectionsAsync(Guid projectId, CancellationToken ct);
    Task<Guid> CreateSectionAsync(Guid companyId, Guid projectId, Guid userId, CreateBoqSectionRequest req, CancellationToken ct);

    Task<List<BoqLineDto>> ListLinesAsync(Guid projectId, CancellationToken ct);
    Task<Guid> CreateLineAsync(Guid companyId, Guid projectId, Guid userId, CreateBoqLineRequest req, CancellationToken ct);
    Task RecalculateLineAsync(Guid companyId, Guid lineId, CancellationToken ct);

    Task<List<BoqSubitemDto>> ListSubitemsAsync(Guid lineId, CancellationToken ct);
    Task<Guid> CreateSubitemAsync(Guid companyId, Guid userId, CreateBoqSubitemRequest req, CancellationToken ct);
}

public sealed class BoqService : IBoqService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<BoqService> _logger;

    public BoqService(IDbConnectionFactory db, ILogger<BoqService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<List<BoqSectionDto>> ListSectionsAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT s.id, s.project_id, s.code, s.name, s.sort_order,
       (SELECT COUNT(*) FROM boq_lines WHERE section_id = s.id AND is_active = true) AS lines_count
FROM boq_sections s
WHERE s.project_id = @ProjectId AND s.is_active = true
ORDER BY s.sort_order, s.code;";
        var rows = await conn.QueryAsync(sql, new { ProjectId = projectId });
        return rows.Select(r => new BoqSectionDto(
            (Guid)r.id, (Guid)r.project_id, (string)r.code, (string)r.name, (int)r.sort_order, (int)(long)r.lines_count
        )).ToList();
    }

    public async Task<Guid> CreateSectionAsync(Guid companyId, Guid projectId, Guid userId, CreateBoqSectionRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        const string sql = @"
INSERT INTO boq_sections (id, company_id, project_id, code, name, sort_order, is_active, created_at)
VALUES (@Id, @CompanyId, @ProjectId, @Code, @Name, @SortOrder, true, now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId, ProjectId = projectId,
            req.Code, req.Name, SortOrder = req.SortOrder ?? 0
        });
        return id;
    }

    public async Task<List<BoqLineDto>> ListLinesAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT bl.id, bl.section_id, bl.price_list_item_id, bl.code, bl.description,
       bl.unit_id, u.code AS unit_code, bl.contract_qty, bl.executed_qty,
       bl.unit_price, bl.regional_premium_pct, bl.final_unit_price, bl.total_amount,
       bl.is_measurable, bl.is_active, bl.sort_order
FROM boq_lines bl
LEFT JOIN units_of_measure u ON u.id = bl.unit_id
WHERE bl.project_id = @ProjectId AND bl.is_active = true
ORDER BY bl.sort_order, bl.code;";
        var rows = await conn.QueryAsync(sql, new { ProjectId = projectId });
        return rows.Select(r => new BoqLineDto(
            (Guid)r.id, (Guid)r.section_id, (Guid?)r.price_list_item_id, (string)r.code, (string)r.description,
            (Guid)r.unit_id, (string?)r.unit_code, (decimal)r.contract_qty, (decimal)r.executed_qty,
            (decimal)r.unit_price, (decimal)r.regional_premium_pct, (decimal)r.final_unit_price, (decimal)r.total_amount,
            (bool)r.is_measurable, (bool)r.is_active, (int)r.sort_order
        )).ToList();
    }

    public async Task<Guid> CreateLineAsync(Guid companyId, Guid projectId, Guid userId, CreateBoqLineRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        var finalUnitPrice = req.UnitPrice * (1 + req.RegionalPremiumPct / 100m);
        var totalAmount = req.ContractQty * finalUnitPrice;
        const string sql = @"
INSERT INTO boq_lines (id, company_id, project_id, section_id, price_list_item_id, code, description,
                      unit_id, contract_qty, executed_qty, unit_price, regional_premium_pct,
                      final_unit_price, total_amount, is_measurable, is_active, sort_order,
                      created_at, updated_at)
VALUES (@Id, @CompanyId, @ProjectId, @SectionId, @PriceListItemId, @Code, @Description,
        @UnitId, @ContractQty, 0, @UnitPrice, @RegionalPremiumPct,
        @FinalUnitPrice, @TotalAmount, @IsMeasurable, true, @SortOrder,
        now(), now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId, ProjectId = projectId,
            req.SectionId, req.PriceListItemId, req.Code, req.Description, req.UnitId,
            req.ContractQty, req.UnitPrice, req.RegionalPremiumPct,
            FinalUnitPrice = finalUnitPrice, TotalAmount = totalAmount,
            req.IsMeasurable, SortOrder = req.SortOrder ?? 0
        });
        return id;
    }

    public async Task RecalculateLineAsync(Guid companyId, Guid lineId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Recompute contract_qty from subitems, then total
        const string sql = @"
UPDATE boq_lines SET
  contract_qty = COALESCE((SELECT SUM(final_qty) FROM boq_subitems WHERE boq_line_id = boq_lines.id), 0),
  final_unit_price = unit_price * (1 + regional_premium_pct / 100),
  total_amount = contract_qty * (unit_price * (1 + regional_premium_pct / 100)),
  updated_at = now()
WHERE id = @LineId;";
        await conn.ExecuteAsync(sql, new { LineId = lineId });
    }

    public async Task<List<BoqSubitemDto>> ListSubitemsAsync(Guid lineId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT id, boq_line_id, description, count, length_m, width_m, height_m,
       initial_qty, deductions, final_qty, sort_order
FROM boq_subitems
WHERE boq_line_id = @LineId
ORDER BY sort_order;";
        var rows = await conn.QueryAsync(sql, new { LineId = lineId });
        return rows.Select(r => new BoqSubitemDto(
            (Guid)r.id, (Guid)r.boq_line_id, (string)r.description, (int)r.count,
            (decimal)r.length_m, (decimal)r.width_m, (decimal)r.height_m,
            (decimal)r.initial_qty, (decimal)r.deductions, (decimal)r.final_qty, (int)r.sort_order
        )).ToList();
    }

    public async Task<Guid> CreateSubitemAsync(Guid companyId, Guid userId, CreateBoqSubitemRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        var initialQty = req.Count * req.LengthM * req.WidthM * req.HeightM;
        var finalQty = initialQty - req.Deductions;
        const string sql = @"
INSERT INTO boq_subitems (id, company_id, boq_line_id, description, count, length_m, width_m, height_m,
                       initial_qty, deductions, final_qty, sort_order, created_at)
VALUES (@Id, @CompanyId, @BoqLineId, @Description, @Count, @LengthM, @WidthM, @HeightM,
        @InitialQty, @Deductions, @FinalQty, @SortOrder, now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId, req.BoqLineId, req.Description, req.Count,
            req.LengthM, req.WidthM, req.HeightM,
            InitialQty = initialQty, Deductions = req.Deductions, FinalQty = finalQty,
            SortOrder = req.SortOrder ?? 0
        });
        // Recalculate the parent line
        await RecalculateLineAsync(companyId, req.BoqLineId, ct);
        return id;
    }
}
