using Dapper;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IVariationOrderService
{
    Task<List<VariationOrderDto>> ListAsync(Guid projectId, CancellationToken ct);
    Task<VariationOrderDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Guid> CreateAsync(Guid companyId, Guid userId, CreateVariationOrderRequest req, CancellationToken ct);
    Task ApproveAsync(Guid companyId, Guid id, Guid approverId, CancellationToken ct);
    Task<Guid> AddLineAsync(Guid companyId, Guid userId, CreateVariationOrderLineRequest req, CancellationToken ct);
}

public sealed class VariationOrderService : IVariationOrderService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<VariationOrderService> _logger;

    public VariationOrderService(IDbConnectionFactory db, ILogger<VariationOrderService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<List<VariationOrderDto>> ListAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT vo.id, vo.project_id, vo.contract_id, vo.order_number, vo.issued_at, vo.reason, vo.status,
       vo.original_contract_value, vo.variation_amount, vo.new_contract_value,
       vo.approved_at, vo.approved_by, vo.notes,
       (SELECT COUNT(*) FROM variation_order_lines WHERE variation_order_id = vo.id) AS lines_count
FROM variation_orders vo
WHERE vo.project_id = @ProjectId
ORDER BY vo.issued_at DESC;";
        var rows = await conn.QueryAsync(sql, new { ProjectId = projectId });
        return rows.Select(r => new VariationOrderDto(
            (Guid)r.id, (Guid)r.project_id, (Guid?)r.contract_id, (string)r.order_number, (DateTime)r.issued_at,
            (string?)r.reason, (string)r.status,
            (decimal)r.original_contract_value, (decimal)r.variation_amount, (decimal)r.new_contract_value,
            (DateTime?)r.approved_at, (Guid?)r.approved_by, (string?)r.notes, (int)(long)r.lines_count
        )).ToList();
    }

    public async Task<VariationOrderDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
SELECT vo.id, vo.project_id, vo.contract_id, vo.order_number, vo.issued_at, vo.reason, vo.status,
       vo.original_contract_value, vo.variation_amount, vo.new_contract_value,
       vo.approved_at, vo.approved_by, vo.notes,
       (SELECT COUNT(*) FROM variation_order_lines WHERE variation_order_id = vo.id) AS lines_count
FROM variation_orders vo WHERE vo.id = @Id;";
        var r = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });
        if (r == null) return null;
        return new VariationOrderDto(
            (Guid)r.id, (Guid)r.project_id, (Guid?)r.contract_id, (string)r.order_number, (DateTime)r.issued_at,
            (string?)r.reason, (string)r.status,
            (decimal)r.original_contract_value, (decimal)r.variation_amount, (decimal)r.new_contract_value,
            (DateTime?)r.approved_at, (Guid?)r.approved_by, (string?)r.notes, (int)(long)r.lines_count
        );
    }

    public async Task<Guid> CreateAsync(Guid companyId, Guid userId, CreateVariationOrderRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        const string sql = @"
INSERT INTO variation_orders (id, company_id, project_id, contract_id, order_number, issued_at,
                            reason, status, original_contract_value, variation_amount, new_contract_value,
                            approved_at, approved_by, notes, created_at, created_by, updated_at)
VALUES (@Id, @CompanyId, @ProjectId, @ContractId, @OrderNumber, @IssuedAt,
        @Reason, 'DRAFT', @OriginalContractValue, 0, @OriginalContractValue,
        NULL, NULL, @Notes, now(), @CreatedBy, now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId, req.ProjectId, req.ContractId,
            req.OrderNumber, req.IssuedAt, req.Reason, req.Notes,
            req.OriginalContractValue, CreatedBy = userId
        });
        return id;
    }

    public async Task ApproveAsync(Guid companyId, Guid id, Guid approverId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
UPDATE variation_orders SET
  status = 'APPROVED',
  approved_at = now(),
  approved_by = @ApproverId,
  updated_at = now()
WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id, ApproverId = approverId });
    }

    public async Task<Guid> AddLineAsync(Guid companyId, Guid userId, CreateVariationOrderLineRequest req, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var id = Guid.NewGuid();
        var netChange = req.QtyChange * req.PriceChange;
        const string sql = @"
INSERT INTO variation_order_lines (id, company_id, variation_order_id, boq_line_id, line_type, description,
                                qty_change, price_change, net_change, sort_order, created_at)
VALUES (@Id, @CompanyId, @VariationOrderId, @BoqLineId, @LineType, @Description,
        @QtyChange, @PriceChange, @NetChange, @SortOrder, now());";
        await conn.ExecuteAsync(sql, new
        {
            Id = id, CompanyId = companyId, req.VariationOrderId, req.BoqLineId, req.LineType,
            req.Description, req.QtyChange, req.PriceChange, NetChange = netChange,
            SortOrder = req.SortOrder ?? 0
        });

        // Update variation_amount and new_contract_value on the parent VO
        const string updateSql = @"
UPDATE variation_orders SET
  variation_amount = (SELECT COALESCE(SUM(net_change), 0) FROM variation_order_lines WHERE variation_order_id = @VariationOrderId),
  new_contract_value = original_contract_value + (SELECT COALESCE(SUM(net_change), 0) FROM variation_order_lines WHERE variation_order_id = @VariationOrderId),
  updated_at = now()
WHERE id = @VariationOrderId;";
        await conn.ExecuteAsync(updateSql, new { req.VariationOrderId });
        return id;
    }
}
