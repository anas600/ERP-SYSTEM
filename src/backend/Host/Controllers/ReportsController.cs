using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class ReportsController : ControllerBase
{
    private readonly IProjectReportService _projects;
    private readonly IInventoryReportService _inventory;
    private readonly IFinanceReportService _finance;
    private readonly IBudgetVsActualService _budgetVsActual;
    private readonly ICompanyContext _companyContext;

    public ReportsController(
        IProjectReportService p, IInventoryReportService i, IFinanceReportService f,
        IBudgetVsActualService budgetVsActual, ICompanyContext c)
    {
        _projects = p; _inventory = i; _finance = f; _budgetVsActual = budgetVsActual; _companyContext = c;
    }
    private Guid CompanyId => _companyContext.CompanyId ?? throw new UnauthorizedAccessException();

    // ===== Project Reports =====
    [HttpGet("projects/{id:guid}/pnl")]
    public async Task<IActionResult> ProjectPnL(Guid id, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var r = await _projects.GetProjectPnLAsync(id, from, to, ct);
        return Ok(r);
    }
    [HttpGet("projects/{id:guid}/budget-vs-actual")]
    public async Task<IActionResult> BudgetVsActual(Guid id, CancellationToken ct)
    {
        var r = await _projects.GetBudgetVsActualAsync(id, ct);
        return Ok(r);
    }
    [HttpGet("projects/summary")]
    public async Task<IActionResult> ProjectsSummary([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var r = await _projects.GetProjectsSummaryAsync(companyId, ct);
        return Ok(new { count = r.Count, items = r });
    }

    // Report 18: Budget vs Actual (all projects)
    [HttpGet("projects/budget-vs-actual")]
    public async Task<IActionResult> BudgetVsActualAll(
        [FromQuery] Guid? projectId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var r = await _budgetVsActual.GetAsync(CompanyId, projectId, from, to, ct);
        return Ok(r);
    }

    // ===== Inventory Reports =====
    [HttpGet("inventory/valuation")]
    public async Task<IActionResult> InventoryValuation([FromQuery] Guid? companyId, [FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var r = await _inventory.GetStockValuationAsync(CompanyId, companyId, warehouseId, ct);
        return Ok(new { count = r.Count, totalValue = r.Sum(x => x.TotalValue), items = r });
    }
    [HttpGet("inventory/movements")]
    public async Task<IActionResult> MovementHistory(
        [FromQuery] Guid? itemId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var r = await _inventory.GetMovementHistoryAsync(CompanyId, itemId, from, to, skip, take, ct);
        return Ok(new { count = r.Count, items = r });
    }
    [HttpGet("inventory/low-stock")]
    public async Task<IActionResult> LowStock([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var r = await _inventory.GetLowStockAsync(CompanyId, companyId, ct);
        return Ok(new { count = r.Count, items = r });
    }
    [HttpGet("inventory/aging")]
    public async Task<IActionResult> StockAging([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var r = await _inventory.GetStockAgingAsync(CompanyId, companyId, ct);
        return Ok(new { count = r.Count, items = r });
    }

    // ===== Finance Reports =====
    [HttpGet("finance/trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] Guid? companyId, [FromQuery] DateTime asOf, CancellationToken ct)
    {
        var r = await _finance.GetTrialBalanceAsync(CompanyId, companyId, asOf, ct);
        return Ok(r);
    }
    [HttpGet("finance/income-statement")]
    public async Task<IActionResult> IncomeStatement([FromQuery] Guid? companyId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var r = await _finance.GetIncomeStatementAsync(CompanyId, companyId, from, to, ct);
        return Ok(r);
    }
    [HttpGet("finance/balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] Guid? companyId, [FromQuery] DateTime asOf, CancellationToken ct)
    {
        var r = await _finance.GetBalanceSheetAsync(CompanyId, companyId, asOf, ct);
        return Ok(r);
    }

    // ===== Combined Reports (convenience) =====
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] Guid? companyId, CancellationToken ct)
    {
        // Quick overview: project count, low stock count, pending notifications
        var projects = await _projects.GetProjectsSummaryAsync(companyId, ct);
        var lowStock = await _inventory.GetLowStockAsync(CompanyId, companyId, ct);
        var valuation = await _inventory.GetStockValuationAsync(CompanyId, companyId, null, ct);
        return Ok(new
        {
            projects = new { count = projects.Count, totalBudget = projects.Sum(p => p.Budget), totalSpent = projects.Sum(p => p.Spent) },
            inventory = new { lowStockCount = lowStock.Count, totalStockValue = valuation.Sum(v => v.TotalValue) },
        });
    }
}
