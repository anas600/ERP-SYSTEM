// DEC-053: Simple audit logger — records entity CRUD actions to audit_log table.
// Schema: id, company_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at
//
// Usage: in any service, inject IAuditLogger and call LogAsync(...).
//
// Failure-safe: audit failures are LOGGED and do NOT break business logic.
// The reasoning: in an ERP system, losing a transaction is worse than losing an audit log entry.

using System.Text.Json;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
/// <b>Failure handling:</b> All exceptions are caught and LOGGED. The rationale:
/// audit logging is a secondary concern — it must NEVER break the business operation.
/// If audit fails, the business operation succeeds and the log entry is dropped (but the
/// failure is recorded in the application log so operators can detect it).
/// </para>
/// <para>
/// <b>Schema:</b> audit_log table is created via the JSON DataTypeRegistry
/// (<c>Host/data-types/audit_log.json</c>). Phase 6.1b: company_id (not tenant_id).
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
    /// <returns>Task that completes when the log entry is written (or logged-on-failure).</returns>
    Task LogAsync(string entityType, Guid? entityId, string action, object? changes, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IAuditLogger"/> implementation using Dapper + PostgreSQL.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly IDbConnectionFactory _db;
    private readonly IHttpContextAccessor _http;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<AuditLogger> _logger;

    /// <summary>
    /// Creates a new AuditLogger instance.
    /// </summary>
    public AuditLogger(
        IDbConnectionFactory db,
        IHttpContextAccessor http,
        ICompanyContext companyContext,
        ILogger<AuditLogger> logger)
    {
        _db = db;
        _http = http;
        _companyContext = companyContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(string entityType, Guid? entityId, string action, object? changes, CancellationToken ct = default)
    {
        try
        {
            // Phase 6.1b: extract company_id from ICompanyContext first, then fall back to JWT claim.
            // The audit_log table has company_id (not tenant_id).
            Guid? companyId = _companyContext.CompanyId;
            Guid? userId = _companyContext.UserId;
            var http = _http.HttpContext;
            if (http != null)
            {
                if (companyId == null || companyId == Guid.Empty)
                {
                    var companyClaim = http.User?.FindFirst("default_company_id")?.Value;
                    if (string.IsNullOrEmpty(companyClaim))
                        companyClaim = http.User?.FindFirst("company_id")?.Value;
                    if (!string.IsNullOrEmpty(companyClaim) && Guid.TryParse(companyClaim, out var cid))
                        companyId = cid;
                }
                if (userId == null || userId == Guid.Empty)
                {
                    var subClaim = http.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? http.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var uid))
                        userId = uid;
                }
            }

            if (companyId == null || companyId == Guid.Empty)
            {
                _logger.LogWarning("AuditLog skipped: company context empty (entity: {EntityType}/{EntityId})", entityType, entityId);
                return;
            }

            var ip = http?.Connection?.RemoteIpAddress?.ToString();
            var changesJson = changes == null ? null : JsonSerializer.Serialize(changes);

            using var conn = await _db.CreateOltpConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO audit_log (company_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at)
                VALUES (@CompanyId, @EntityType, @EntityId, @Action, @UserId, @Changes::jsonb, @IpAddress, NOW())",
                new
                {
                    CompanyId = companyId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    UserId = userId,
                    Changes = changesJson,
                    IpAddress = ip
                }, cancellationToken: ct));
        }
        catch (Exception ex)
        {
            // Phase 6.1b: log the failure instead of silently swallowing.
            // Audit failure should NOT break the calling operation, but operators
            // MUST be able to detect it.
            _logger.LogError(ex,
                "Failed to write audit log for {EntityType}/{EntityId} action={Action}",
                entityType, entityId, action);
        }
    }
}
