// DEC-053 P1.5: Audit log viewer endpoint
// Allows Admin + Accountant to query audit_log with filters + pagination.

using System.Security.Claims;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.AuditRead)]
public class AuditController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;

    public AuditController(IDbConnectionFactory db, ICompanyContext companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    private Guid? CompanyIdFilter => _companyContext.CompanyId;

    /// <summary>
    /// List audit log entries with optional filters and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (take is < 1 or > 500) take = 50;

        var sql = @"SELECT id, company_id AS CompanyId, entity_type AS EntityType, entity_id AS EntityId,
                    action, user_id AS UserId, changes, ip_address::text AS IpAddress, created_at AS CreatedAt
                    FROM audit_log
                    WHERE 1=1";
        var p = new DynamicParameters();

        var cid = CompanyIdFilter;
        if (cid.HasValue)
        {
            sql += " AND company_id = @Cid";
            p.Add("Cid", cid.Value);
        }
        if (fromDate.HasValue)
        {
            sql += " AND created_at >= @FromDate";
            p.Add("FromDate", fromDate.Value);
        }
        if (toDate.HasValue)
        {
            sql += " AND created_at <= @ToDate";
            p.Add("ToDate", toDate.Value);
        }
        if (userId.HasValue)
        {
            sql += " AND user_id = @UserId";
            p.Add("UserId", userId.Value);
        }
        if (!string.IsNullOrEmpty(entityType))
        {
            sql += " AND entity_type = @EntityType";
            p.Add("EntityType", entityType);
        }
        if (!string.IsNullOrEmpty(action))
        {
            sql += " AND action = @Action";
            p.Add("Action", action);
        }

        sql += " ORDER BY created_at DESC OFFSET @Skip LIMIT @Take";
        p.Add("Skip", skip);
        p.Add("Take", take);

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<AuditEntry>(new CommandDefinition(sql, p, cancellationToken: ct));
        return Ok(rows);
    }

    /// <summary>
    /// Get a single audit log entry by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<AuditEntry>(new CommandDefinition(@"
            SELECT id, company_id AS CompanyId, entity_type AS EntityType, entity_id AS EntityId,
                   action, user_id AS UserId, changes, ip_address::text AS IpAddress, created_at AS CreatedAt
            FROM audit_log WHERE id = @Id", new { Id = id }, cancellationToken: ct));
        if (row == null) return NotFound();
        return Ok(row);
    }

    /// <summary>
    /// Summary counts grouped by entity_type (for dashboard).
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct = default)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var cid = CompanyIdFilter;
        var sql = @"SELECT entity_type, COUNT(*) AS cnt
                    FROM audit_log
                    WHERE 1=1" + (cid.HasValue ? " AND company_id = @Cid" : "") +
                    @" GROUP BY entity_type ORDER BY cnt DESC";
        var p = new DynamicParameters();
        if (cid.HasValue) p.Add("Cid", cid.Value);
        var rows = await conn.QueryAsync<AuditSummary>(new CommandDefinition(sql, p, cancellationToken: ct));
        return Ok(rows);
    }
}

public sealed class AuditEntry
{
    public long Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = "";
    public Guid? UserId { get; set; }
    public string? Changes { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AuditSummary
{
    public string EntityType { get; set; } = "";
    public long Cnt { get; set; }
}
