using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Finance Reports API — بديل للـ Reports module القديم.
/// كل الـ endpoints تحت /api/finance/reports/* و [Authorize].
/// يغطي الـ 20 تقرير محاسبي إلزامي:
///   1. Trial Balance
///   2. Income Statement
///   3. Balance Sheet
///   4. Cash Flow
///   5. General Ledger
///   6. Journal Entries
///   7. Account Activity / Cardex
///  10. AP Aging
///  11. Collections
///  16. Cost Center Performance
///  19. VAT Report
/// </summary>
[ApiController]
[Route("api/finance/reports")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class FinanceReportsController : ControllerBase
{
    private readonly IFinanceReportService _finance;
    private readonly IGeneralLedgerReportService _gl;
    private readonly IBalanceSheetService _bs;
    private readonly ICashFlowService _cf;
    private readonly IAPAgingService _ap;
    private readonly IJournalEntryReportService _je;
    private readonly IAccountActivityService _accountActivity;
    private readonly ICollectionsService _collections;
    private readonly ICostCenterReportService _costCenters;
    private readonly IVatReportService _vat;
    private readonly ICompanyContext _companyContext;

    public FinanceReportsController(
        IFinanceReportService finance,
        IGeneralLedgerReportService gl, IBalanceSheetService bs, ICashFlowService cf, IAPAgingService ap,
        IJournalEntryReportService je, IAccountActivityService accountActivity,
        ICollectionsService collections, ICostCenterReportService costCenters, IVatReportService vat,
        ICompanyContext companyContext)
    {
        _finance = finance; _gl = gl; _bs = bs; _cf = cf; _ap = ap;
        _je = je; _accountActivity = accountActivity;
        _collections = collections; _costCenters = costCenters; _vat = vat;
        _companyContext = companyContext;
    }

    private Guid CompanyId => _companyContext.CompanyId ?? throw new UnauthorizedAccessException();

    // ===== Report 1: Trial Balance =====
    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateTime asOf, CancellationToken ct)
    {
        var r = await _finance.GetTrialBalanceAsync(CompanyId, null, asOf, ct);
        return Ok(r);
    }

    // ===== Report 2: Income Statement =====
    [HttpGet("income-statement")]
    public async Task<IActionResult> IncomeStatement(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (to < from) return BadRequest("to must be >= from.");
        var r = await _finance.GetIncomeStatementAsync(CompanyId, null, from, to, ct);
        return Ok(r);
    }

    // ===== Report 3: Balance Sheet =====
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateTime asOf, CancellationToken ct)
    {
        var r = await _bs.GetAsync(CompanyId, asOf, ct);
        return Ok(r);
    }

    // ===== Report 4: Cash Flow =====
    [HttpGet("cash-flow")]
    public async Task<IActionResult> CashFlow(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (to < from) return BadRequest("to must be >= from.");
        var r = await _cf.GetAsync(CompanyId, from, to, ct);
        return Ok(r);
    }

    // ===== Report 5: General Ledger =====
    [HttpGet("general-ledger")]
    public async Task<IActionResult> GeneralLedger(
        [FromQuery] Guid accountId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (accountId == Guid.Empty) return BadRequest("accountId is required.");
        var r = await _gl.GetAccountLedgerAsync(accountId, from, to, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(new { error = r.Error });
    }

    // ===== Report 6: Journal Entries Register =====
    [HttpGet("journal-entries")]
    public async Task<IActionResult> JournalEntries(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int? status, [FromQuery] int skip, [FromQuery] int take,
        CancellationToken ct)
    {
        if (take <= 0) take = 100;
        if (take > 500) take = 500;
        if (skip < 0) skip = 0;
        var r = await _je.GetAsync(CompanyId, from, to, status, skip, take, ct);
        return Ok(r);
    }

    // ===== Report 7: Account Activity / Cardex =====
    [HttpGet("account-activity")]
    public async Task<IActionResult> AccountActivity(
        [FromQuery] Guid accountId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (accountId == Guid.Empty) return BadRequest("accountId is required.");
        var r = await _accountActivity.GetActivityAsync(CompanyId, accountId, from, to, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(new { error = r.Error });
    }

    // ===== Report 10: AP Aging =====
    [HttpGet("ap-aging")]
    public async Task<IActionResult> APAging([FromQuery] DateTime asOf, CancellationToken ct)
    {
        var r = await _ap.GetAsync(CompanyId, asOf, ct);
        return Ok(r);
    }

    // ===== Report 11: Collections =====
    [HttpGet("collections")]
    public async Task<IActionResult> Collections(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var r = await _collections.GetAsync(CompanyId, from, to, ct);
        return Ok(r);
    }

    // ===== Report 16: Cost Center Performance =====
    [HttpGet("cost-center-performance")]
    public async Task<IActionResult> CostCenterPerformance(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var r = await _costCenters.GetAsync(CompanyId, from, to, ct);
        return Ok(r);
    }

    // ===== Report 19: VAT Report =====
    [HttpGet("vat")]
    public async Task<IActionResult> VatReport(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (to < from) return BadRequest("to must be >= from.");
        var r = await _vat.GetAsync(CompanyId, from, to, ct);
        return Ok(r);
    }
}
