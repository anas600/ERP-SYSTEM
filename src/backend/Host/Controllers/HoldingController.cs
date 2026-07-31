// Sprint 11 T2 (BE Jimi) — HoldingController.
//
// New controller for the demo Holding dashboard. The existing
// GetHoldingBySlug endpoint on `CompaniesController` returns a single
// Holding's detail (the `/api/holdings/{slug}` FE page); this controller
// returns the **consolidated KPIs** across the entire Holding (all
// sub-companies).
//
// Routes:
//   GET /api/holdings/dashboard       — Holding-level consolidated KPIs
//   GET /api/dashboard/holding        — alias (the FE's fallback path)
//
// Auth: ReadAccess policy — the dashboard is the demo landing page
// for every role. Per DashboardChartService / DashboardSummaryService
// precedent (Sprint 1 / Sprint 5).
//
// Empty-state contract: when the Holding is not seeded yet, the service
// returns a zero-filled DTO; we forward it as 200 OK. The FE renders the
// empty state cleanly.

using ERPSystem.Modules.Finance.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class HoldingController : ControllerBase
{
    private readonly IFinanceService _finance;
    public HoldingController(IFinanceService finance) => _finance = finance;

    // GET /api/holdings/dashboard — consolidated KPIs (revenue, expenses,
    // net profit, company count, employee count, treasury balance, recent
    // transactions). Backed by FinanceService.GetConsolidatedKpisAsync.
    //
    // The FE contract: HoldingDashboard (api-types.ts).
    [HttpGet("api/holdings/dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var r = await _finance.GetConsolidatedKpisAsync(ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // GET /api/dashboard/holding — alternative route. The FE's
    // getHoldingDashboard() tries /api/holdings/dashboard first and falls
    // back to /api/dashboard/holding on 404. Both must work; we return the
    // same payload so the FE can pick either.
    [HttpGet("api/dashboard/holding")]
    public async Task<IActionResult> GetDashboardAlias(CancellationToken ct)
    {
        var r = await _finance.GetConsolidatedKpisAsync(ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Holding Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
