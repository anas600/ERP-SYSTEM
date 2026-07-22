// DEC-053: Simple audit logger — records entity CRUD actions to audit_log table.
// Schema: id, tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at
//
// Usage: in any service, inject IAuditLogger and call LogAsync(...).

using System.Text.Json;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace ERPSystem.Host.Audit;

public interface IAuditLogger
{
    Task LogAsync(string entityType, Guid? entityId, string action, object? changes, CancellationToken ct = default);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly IDbConnectionFactory _db;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(IDbConnectionFactory db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

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
        }
    }
}
