// DEC-052 P3: Generic soft-delete + restore endpoints
// Provides /soft-delete/{table}/{id} for soft delete
// and /soft-delete/{table}/{id}/restore for admin restore.
//
// Why a separate controller? Centralized soft-delete logic = single audit trail,
// consistent behavior, no need to modify every controller.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/soft-delete")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteMasterData)]
public class SoftDeleteController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ITenantContext _tenant;

    // Whitelist: only allow soft delete on these tables (security: prevent arbitrary SQL)
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "sales_invoices", "payments", "journal_entries", "users"
    };

    public SoftDeleteController(IDbConnectionFactory db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid TenantId => _tenant.TenantId ?? throw new UnauthorizedAccessException();
    private Guid UserId => Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Soft delete a record. Sets is_deleted = true, deleted_at = NOW, deleted_by = current user.
    /// </summary>
    [HttpDelete("{table}/{id:guid}")]
    public async Task<IActionResult> SoftDelete(string table, Guid id, CancellationToken ct = default)
    {
        if (!AllowedTables.Contains(table))
            return BadRequest(new { error = "Table not allowed for soft delete" });

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {table}
            SET is_deleted = TRUE,
                deleted_at = NOW(),
                deleted_by = @UserId,
                updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE",
            new { Id = id, TenantId = TenantId, UserId = UserId }, cancellationToken: ct));

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
        var affected = await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {table}
            SET is_deleted = FALSE,
                deleted_at = NULL,
                deleted_by = NULL,
                updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = TRUE",
            new { Id = id, TenantId = TenantId }, cancellationToken: ct));

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
        var rows = await conn.QueryAsync(new CommandDefinition($@"
            SELECT id, deleted_at, deleted_by
            FROM {table}
            WHERE tenant_id = @TenantId AND is_deleted = TRUE
            ORDER BY deleted_at DESC
            OFFSET @Skip LIMIT @Take",
            new { TenantId = TenantId, Skip = skip, Take = take }, cancellationToken: ct));

        return Ok(rows);
    }
}
