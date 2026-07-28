// Sprint 1 (T1 / Block A) — GET /api/dashboard/summary
// Returns the 4-KPI summary payload consumed by app/admin/dashboard/page.tsx.
//
// Auth: any authenticated user (ReadAccess policy).
// Why ReadAccess: the dashboard is the default landing page after login for
// every role. Restricting it to Admin/Accountant would hide the page from
// ProjectManager + Viewer roles, which is the wrong UX for a demo.
//
// The response shape is intentionally flat (4 numbers) so the FE can render
// it as KPI cards with no further transformation. Field names are camelCase
// to match the existing JSON contract from the FinanceReportService and the
// other reports endpoints.

using ERPSystem.Modules.Dashboard.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Modules.Dashboard.Endpoints;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardSummaryService _service;
    public DashboardController(IDashboardSummaryService service) { _service = service; }

    /// <summary>
    /// GET /api/dashboard/summary — 4-KPI payload for the dashboard page.
    /// Returns 200 even when the company context is unresolved (the FE
    /// renders an empty state instead of an error); never returns 401 here
    /// because the [Authorize] attribute on the controller already handles
    /// the no-token case upstream.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _service.GetSummaryAsync(ct);
        return Ok(summary);
    }
}
