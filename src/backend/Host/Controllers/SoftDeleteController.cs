// DEC-052 P3: Generic soft-delete + restore endpoints
// Provides /soft-delete/{table}/{id} for soft delete
// and /soft-delete/{table}/{id}/restore for admin restore.
//
// Why a separate controller? Centralized soft-delete logic = single audit trail,
// consistent behavior, no need to modify every controller.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/soft-delete")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteMasterData)]
public class SoftDeleteController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;

    // Whitelist: only allow soft delete on these tables (security: prevent arbitrary SQL)
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "sales_invoices", "payments", "journal_entries", "users"
    };

    public SoftDeleteController(IDbConnectionFactory db, ICompanyContext companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    private Guid UserId => Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Soft delete a record. Sets is_deleted = true, deleted_at = NOW, deleted_by = current user.
    /// Phase 6.1b: tenant_id column removed — companies are now global, scoped via user_companies.
    /// </summary>
    [HttpDelete("{table}/{id:guid}")]
    public async Task<IActionResult> SoftDelete(string table, Guid id, CancellationToken ct = default)
    {
        if (!AllowedTables.Contains(table))
            return BadRequest(new { error = "Table not allowed for soft delete" });

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // CodeQL cs/sql-injection fix: avoid {table} interpolation; emit one hardcoded SQL literal per allowed table.
        // Phase 6.1b: tenant_id filter removed (multi-company model — soft-delete is per-company now but no column to filter on at table level).
        string? sql = table.ToLowerInvariant() switch
        {
            "sales_invoices" => "UPDATE sales_invoices SET is_deleted = TRUE, deleted_at = NOW(), deleted_by = @UserId, updated_at = NOW() WHERE id = @Id AND is_deleted = FALSE",
            "payments" => "UPDATE payments SET is_deleted = TRUE, deleted_at = NOW(), deleted_by = @UserId, updated_at = NOW() WHERE id = @Id AND is_deleted = FALSE",
            "journal_entries" => "UPDATE journal_entries SET is_deleted = TRUE, deleted_at = NOW(), deleted_by = @UserId, updated_at = NOW() WHERE id = @Id AND is_deleted = FALSE",
            "users" => "UPDATE users SET is_deleted = TRUE, deleted_at = NOW(), deleted_by = @UserId, updated_at = NOW() WHERE id = @Id AND is_deleted = FALSE",
            _ => null
        };
        if (sql is null) return BadRequest(new { error = "Table not allowed for soft delete" });
        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, UserId = UserId }, cancellationToken: ct));

        if (affected == 0)
            return NotFound(new { error = "Not found or already deleted" });

        return Ok(new { id, table, deleted_at = DateTime.UtcNow, deleted_by = UserId });
    }

    /// <summary>
    /// Restore a soft-deleted record. Sets is_deleted = false.
    /// </summary>
    [HttpPost("{table}/{id:guid}/restore")]
    public async Task<IActionResult> Restore(string table, Guid id, CancellationToken ct = default)
    {
        if (!AllowedTables.Contains(table))
            return BadRequest(new { error = "Table not allowed" });

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // CodeQL cs/sql-injection fix: avoid {table} interpolation; emit one hardcoded SQL literal per allowed table.
        // Phase 6.1b: tenant_id filter removed.
        string? sql = table.ToLowerInvariant() switch
        {
            "sales_invoices" => "UPDATE sales_invoices SET is_deleted = FALSE, deleted_at = NULL, deleted_by = NULL, updated_at = NOW() WHERE id = @Id AND is_deleted = TRUE",
            "payments" => "UPDATE payments SET is_deleted = FALSE, deleted_at = NULL, deleted_by = NULL, updated_at = NOW() WHERE id = @Id AND is_deleted = TRUE",
            "journal_entries" => "UPDATE journal_entries SET is_deleted = FALSE, deleted_at = NULL, deleted_by = NULL, updated_at = NOW() WHERE id = @Id AND is_deleted = TRUE",
            "users" => "UPDATE users SET is_deleted = FALSE, deleted_at = NULL, deleted_by = NULL, updated_at = NOW() WHERE id = @Id AND is_deleted = TRUE",
            _ => null
        };
        if (sql is null) return BadRequest(new { error = "Table not allowed" });
        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        if (affected == 0)
            return NotFound(new { error = "Not found or not deleted" });

        return Ok(new { id, table, restored_at = DateTime.UtcNow });
    }

    /// <summary>
    /// List soft-deleted records (admin only).
    /// </summary>
    [HttpGet("{table}/deleted")]
    public async Task<IActionResult> ListDeleted(
        string table,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (!AllowedTables.Contains(table))
            return BadRequest(new { error = "Table not allowed" });
        if (take is < 1 or > 200) take = 50;

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // CodeQL cs/sql-injection fix: avoid {table} interpolation; emit one hardcoded SQL literal per allowed table.
        // Phase 6.1b: tenant_id filter removed.
        string? sql = table.ToLowerInvariant() switch
        {
            "sales_invoices" => "SELECT id, deleted_at, deleted_by FROM sales_invoices WHERE is_deleted = TRUE ORDER BY deleted_at DESC OFFSET @Skip LIMIT @Take",
            "payments" => "SELECT id, deleted_at, deleted_by FROM payments WHERE is_deleted = TRUE ORDER BY deleted_at DESC OFFSET @Skip LIMIT @Take",
            "journal_entries" => "SELECT id, deleted_at, deleted_by FROM journal_entries WHERE is_deleted = TRUE ORDER BY deleted_at DESC OFFSET @Skip LIMIT @Take",
            "users" => "SELECT id, deleted_at, deleted_by FROM users WHERE is_deleted = TRUE ORDER BY deleted_at DESC OFFSET @Skip LIMIT @Take",
            _ => null
        };
        if (sql is null) return BadRequest(new { error = "Table not allowed" });
        var rows = await conn.QueryAsync(new CommandDefinition(sql, new { Skip = skip, Take = take }, cancellationToken: ct));

        return Ok(rows);
    }
}
