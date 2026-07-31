// Sprint 3 (T1 / Block A) — GET /api/activity/recent
//
// Returns the most-recent activity_log rows for the current company (joined
// with users for the actor's full_name). Consumed by the activity feed page
// (Block B frontend) and the recent-activity card on the dashboard.
//
// Auth: ReadAccess (any authenticated user can see activity in their company).
// Mirrors the dashboard pattern (DashboardController.GetSummary) — the
// activity feed is a generic "what happened recently" surface, not a
// privileged audit log viewer. For the audit-log query surface see
// AuditController which uses AuditRead.
//
// Security: IActivityFeedService filters by company_id from the
// CompanyContextMiddleware. There is no bypass — a user cannot pass a
// different company_id via query string and have it apply.

using ERPSystem.Modules.Activity.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/activity")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class ActivityController : ControllerBase
{
    private readonly IActivityFeedService _service;

    public ActivityController(IActivityFeedService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/activity/recent?limit=20
    /// Returns the latest N activity_log rows for the active company,
    /// sorted DESC by timestamp. Returns 200 with an empty array when the
    /// company context is unresolved (FE renders the empty state, not an error).
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> Recent(
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var items = await _service.GetRecentAsync(limit, ct);
        return Ok(items);
    }
}
