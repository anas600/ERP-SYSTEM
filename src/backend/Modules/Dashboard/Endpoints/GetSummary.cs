// Sprint 1 (T1 / Block A) — GET /api/dashboard/summary
// Sprint 5 (T1-T3 / Phase 4) — /api/dashboard/charts/* endpoints
//
// GET /api/dashboard/summary       — 4-KPI summary payload (Sprint 1)
// GET /api/dashboard/charts/revenue       — T1: revenue vs expense per month
// GET /api/dashboard/charts/expenses-by-category — T2: pie / donut chart
// GET /api/dashboard/charts/top-customers — T3: top customers bar chart
//
// Auth: any authenticated user (ReadAccess policy).
// Why ReadAccess: the dashboard is the default landing page after login for
// every role. Restricting it to Admin/Accountant would hide the page from
// ProjectManager + Viewer roles, which is the wrong UX for a demo.
//
// The response shapes are intentionally flat (lists of small DTOs) so the FE
// can drop them straight into Recharts without any further transformation.
// Field names are camelCase to match the existing JSON contract from
// FinanceReportService and the other reports endpoints.

using ERPSystem.Modules.Dashboard.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Modules.Dashboard.Endpoints;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardSummaryService _summary;
    private readonly IDashboardChartService _charts;

    public DashboardController(
        IDashboardSummaryService summary,
        IDashboardChartService charts)
    {
        _summary = summary;
        _charts = charts;
    }

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
        var summary = await _summary.GetSummaryAsync(ct);
        return Ok(summary);
    }

    /// <summary>
    /// GET /api/dashboard/charts/revenue?months=6 — revenue vs expense per
    /// month for the line chart. Returns at most one row per month, even
    /// when both sides are zero (so the FE chart has stable x-axis labels).
    /// </summary>
    [HttpGet("charts/revenue")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int? months, CancellationToken ct)
    {
        var points = await _charts.GetRevenueVsExpenseAsync(months ?? 6, ct);
        return Ok(points);
    }

    /// <summary>
    /// GET /api/dashboard/charts/expenses-by-category?months=3 — pie / donut
    /// chart data. One slice per Expense-type account (AccountType.Expense = 5).
    /// Empty list when no expenses or no company context.
    /// </summary>
    [HttpGet("charts/expenses-by-category")]
    public async Task<IActionResult> GetExpensesByCategory([FromQuery] int? months, CancellationToken ct)
    {
        var slices = await _charts.GetExpensesByCategoryAsync(months ?? 3, ct);
        return Ok(slices);
    }

    /// <summary>
    /// GET /api/dashboard/charts/top-customers?limit=5 — top customers by
    /// posted invoice total, all-time within the current company.
    /// </summary>
    [HttpGet("charts/top-customers")]
    public async Task<IActionResult> GetTopCustomers([FromQuery] int? limit, CancellationToken ct)
    {
        var rows = await _charts.GetTopCustomersAsync(limit ?? 5, ct);
        return Ok(rows);
    }
}
