// Sprint 3 (T1 / Block A) — Activity feed reader service.
//
// Reads the recent activity_log rows for the current company, joined with the
// users table to enrich each row with the actor's full_name. This is the
// read-side companion to ActivityLogService (which only writes via
// IActivityLogger).
//
// Why a separate service and not a method on ActivityLogService:
//   - ActivityLogService implements IActivityLogger (the write side) and is
//     failure-safe (per DEC-053 + DEC-073) — a DB error in LogAsync must NOT
//     break business operations. Mixing in a read query would risk silent
//     semantic drift in callers expecting only LogAsync.
//   - A new service keeps the read path's dependencies (ICompanyContext for
//     security scoping) clean and unit-testable via the FakeDbConnectionFactory
//     pattern already used by DashboardSummaryService.
//
// Security: every row is filtered by company_id from ICompanyContext. There is
// no admin override here — a user only sees activity in the company they are
// currently scoped to (Constitution Article 3: company_id only, no tenant_id).
//
// entityType / entityId: the activity_log table has no entity_* columns —
// those live on audit_log. We return them as null so the response shape
// matches the unified activity-feed contract documented in
// docs/workflow/sprint-3.md (frontend uses the same shape for both audit and
// activity rows). FE renders null entity fields with an "Activity" badge
// rather than an entity name.

using System.Text.Json;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Activity.Application;

public interface IActivityFeedService
{
    /// <summary>
    /// Returns the most-recent <paramref name="limit"/> activity_log rows for
    /// the current company, DESC by created_at. Returns an empty list if the
    /// company context is unresolved (anonymous or no X-Company-Id header).
    /// </summary>
    Task<IReadOnlyList<ActivityFeedItem>> GetRecentAsync(int limit, CancellationToken ct);
}

public sealed class ActivityFeedItem
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = "";
    /// <summary>Always null for activity_log rows (audit_log only). Reserved for shape parity.</summary>
    public string? EntityType { get; set; }
    /// <summary>Always null for activity_log rows (audit_log only). Reserved for shape parity.</summary>
    public Guid? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    /// <summary>Raw JSON text from the metadata jsonb column. FE parses client-side.</summary>
    public string? Metadata { get; set; }
}

public sealed class ActivityFeedService : IActivityFeedService
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _company;
    private readonly ILogger<ActivityFeedService> _logger;

    public ActivityFeedService(
        IDbConnectionFactory db,
        ICompanyContext company,
        ILogger<ActivityFeedService> logger)
    {
        _db = db;
        _company = company;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ActivityFeedItem>> GetRecentAsync(int limit, CancellationToken ct)
    {
        // Defensive: cap limit to a reasonable upper bound. 200 covers the
        // largest "show more" infinite-scroll case without risking a runaway
        // payload. 0 or negative falls back to the controller's default.
        if (limit <= 0) limit = 20;
        if (limit > 200) limit = 200;

        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            _logger.LogDebug("ActivityFeed.GetRecent called with no resolved company (returning empty)");
            return Array.Empty<ActivityFeedItem>();
        }

        // LEFT JOIN: activity_log.user_id is nullable (DEC-073 — pre-login
        // LOGIN_FAILED rows have no resolved user). We still want to surface
        // those rows; UserName comes back as null and the FE renders "system".
        //
        // metadata::text — cast jsonb to text so Dapper maps it to string
        // without needing a TypeHandler. The FakeDbDataReader returns the raw
        // column value (whatever the test put there), so this cast is purely
        // a runtime instruction and doesn't affect unit tests.
        const string sql = @"
            SELECT
                a.id           AS Id,
                a.user_id      AS UserId,
                a.action       AS Action,
                a.created_at   AS Timestamp,
                a.metadata::text AS Metadata,
                u.full_name    AS UserName
            FROM activity_log a
            LEFT JOIN users u ON u.id = a.user_id
            WHERE a.company_id = @CompanyId
            ORDER BY a.created_at DESC
            LIMIT @Limit";

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<ActivityFeedItem>(new CommandDefinition(
            sql,
            new { CompanyId = companyId.Value, Limit = limit },
            cancellationToken: ct));

        return rows.AsList();
    }
}
