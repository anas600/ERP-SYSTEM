// DEC-053: Simple audit logger — records entity CRUD actions to audit_log table.
// Schema: id, tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at
//
// Usage: in any service, inject IAuditLogger and call LogAsync(...).
//
// Failure-safe: audit failures are swallowed and do NOT break business logic.
// The reasoning: in an ERP system, losing a transaction is worse than losing an audit log entry.

using System.Text.Json;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace ERPSystem.Host.Audit;

/// <summary>
/// Records entity CRUD actions to the audit_log table for compliance and forensics.
/// </summary>
/// <remarks>
/// <para>
/// <b>Threading:</b> Implementation is thread-safe. All DB operations go through
/// the injected <see cref="IDbConnectionFactory"/> which is also thread-safe.
/// </para>
/// <para>
/// <b>Failure handling:</b> All exceptions are caught and swallowed. The rationale:
/// audit logging is a secondary concern — it must NEVER break the business operation.
/// If audit fails, the business operation succeeds and the log entry is silently dropped.
/// </para>
/// <para>
/// <b>Schema:</b> audit_log table is created via FluentMigrator. See
/// <c>20260706_120000_AddAuditLog.cs</c>.
/// </para>
/// </remarks>
public interface IAuditLogger
{
    /// <summary>
    /// Records a single audit log entry.
    /// </summary>
    /// <param name="entityType">The entity name (e.g., "Vendor", "JournalEntry").</param>
    /// <param name="entityId">The entity's primary key (nullable for bulk operations).</param>
    /// <param name="action">
    /// The action verb. Common values: "CREATE", "UPDATE", "DELETE", "READ",
    /// "APPROVE", "POST", "REVERSE".
    /// </param>
    /// <param name="changes">
    /// Optional diff/payload (before/after values). Serialized as JSONB.
    /// Pass null for read-only operations.
    /// </param>
    /// <param name="ct">Cancellation token (default: none).</param>
    /// <returns>Task that completes when the log entry is written (or silently fails).</returns>
    Task LogAsync(string entityType, Guid? entityId, string action, object? changes, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IAuditLogger"/> implementation using Dapper + PostgreSQL.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly IDbConnectionFactory _db;
    private readonly IHttpContextAccessor _http;

    /// <summary>
    /// Creates a new AuditLogger instance.
    /// </summary>
    /// <param name="db">Database connection factory (required).</param>
    /// <param name="http">HTTP context accessor (required for IP/claim extraction).</param>
    public AuditLogger(IDbConnectionFactory db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    /// <inheritdoc />
    public async Task LogAsync(string entityType, Guid? entityId, string action, object? changes, CancellationToken ct = default)
    {
        try
        {
            // Extract tenant_id + user_id from current claims (best-effort)
            Guid? tenantId = null;
            Guid? userId = null;
            var http = _http.HttpContext;
            if (http != null)
            {
                var tenantClaim = http.User?.FindFirst("tenant_id")?.Value;
                if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tid))
                    tenantId = tid;
                var subClaim = http.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? http.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var uid))
                    userId = uid;
            }

            var ip = http?.Connection?.RemoteIpAddress?.ToString();
            var changesJson = changes == null ? null : JsonSerializer.Serialize(changes);

            using var conn = await _db.CreateOltpConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO audit_log (tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at)
                VALUES (@TenantId, @EntityType, @EntityId, @Action, @UserId, @Changes::jsonb, @IpAddress, NOW())",
                new
                {
                    TenantId = tenantId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    UserId = userId,
                    Changes = changesJson,
                    IpAddress = ip
                }, cancellationToken: ct));
        }
        catch (Exception)
        {
            // Audit failure should NOT break the calling operation.
            // Swallow exception — the calling business logic continues.
            // In production, consider logging to a fallback file or Sentry.
        }
    }
}
