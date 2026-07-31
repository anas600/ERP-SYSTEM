// Activity log service (Cycle 6 / DEC-073) — Dapper implementation of
// IActivityLogger. Mirrors the AuditLogger pattern (Host/Audit/AuditLogger.cs)
// but targets the activity_log table (created via JSON data-type
// Host/data-types/activity_log.json).
//
// Failure handling: any exception is caught and logged. The reasoning
// (DEC-053): a missed activity log entry is much less expensive than a
// failed business operation. The AuthService.LoginAsync should NEVER fail
// just because activity_log is unreachable.

using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Activity.Application;

/// <summary>
/// Default <see cref="IActivityLogger"/> implementation using Dapper + PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// <b>Threading:</b> thread-safe. كل عمليات الـ DB تمر عبر
/// <see cref="IDbConnectionFactory"/> الذي هو thread-safe.
/// </para>
/// <para>
/// <b>Failure handling:</b> كل الـ exceptions تُمسَك وتُسجَّل. الـ activity
/// log failures لا يجب أن تكسر الـ business operation (DEC-053 rationale).
/// </para>
/// <para>
/// <b>Schema:</b> activity_log table is created via the JSON DataTypeRegistry
/// (<c>Host/data-types/activity_log.json</c>). Phase 6.1b: company_id (not
/// tenant_id).
/// </para>
/// </remarks>
public sealed class ActivityLogService : IActivityLogger
{
    private readonly IDbConnectionFactory _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(
        IDbConnectionFactory db,
        IHttpContextAccessor http,
        ILogger<ActivityLogService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid? userId,
        Guid? companyId,
        string action,
        object? metadata = null,
        CancellationToken ct = default)
    {
        // Defensive: action is required. Skip + warn if empty (programmer error).
        if (string.IsNullOrWhiteSpace(action))
        {
            _logger.LogWarning("ActivityLog skipped: action is empty (userId={UserId})", userId);
            return;
        }

        try
        {
            var http = _http.HttpContext;

            // IP: prefer the X-Forwarded-For header (HF Space proxy), then
            // RemoteIpAddress. Truncate to 45 chars (IPv6 max).
            string? ip = null;
            if (http != null)
            {
                var xff = http.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrEmpty(xff))
                {
                    ip = xff.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                }
                ip ??= http.Connection.RemoteIpAddress?.ToString();
                if (ip != null && ip.Length > 45) ip = ip.Substring(0, 45);
            }

            // User-Agent: optional, truncated to 255 chars (column constraint).
            string? userAgent = null;
            if (http != null)
            {
                var ua = http.Request.Headers.UserAgent.ToString();
                if (!string.IsNullOrEmpty(ua))
                {
                    userAgent = ua.Length > 255 ? ua.Substring(0, 255) : ua;
                }
            }

            var metadataJson = metadata == null ? null : JsonSerializer.Serialize(metadata);

            using var conn = await _db.CreateOltpConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO activity_log
                    (company_id, user_id, action, ip_address, user_agent, metadata, created_at)
                VALUES
                    (@CompanyId, @UserId, @Action, @IpAddress, @UserAgent, @Metadata::jsonb, NOW())",
                new
                {
                    CompanyId = companyId,
                    UserId = userId,
                    Action = action,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    Metadata = metadataJson
                },
                cancellationToken: ct));
        }
        catch (Exception ex)
        {
            // DEC-053 + DEC-073: activity failures must NOT break the calling
            // operation. We log the failure so operators can detect it, but
            // the caller (e.g. AuthService.LoginAsync) continues normally.
            _logger.LogError(ex,
                "Failed to write activity log for action={Action} userId={UserId}",
                action, userId);
        }
    }
}
