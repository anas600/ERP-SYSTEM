using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/finance/ledger")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class LedgerController : ControllerBase
{
    private readonly IGeneralLedgerService _ledger;
    private readonly IGeneralLedgerReportService _report;
    private readonly IYearEndClosingService _yearEnd;
    // Sprint 57 (DEC-152): Executive Dashboard service (KPIs + chart data)
    private readonly IExecutiveDashboardService _dashboard;
    private readonly ICompanyContext _companyContext;

    public LedgerController(
        IGeneralLedgerService ledger,
        IGeneralLedgerReportService report,
        IYearEndClosingService yearEnd,
        IExecutiveDashboardService dashboard,
        ICompanyContext companyContext)
    {
        _ledger = ledger;
        _report = report;
        _yearEnd = yearEnd;
        _dashboard = dashboard;
        _companyContext = companyContext;
    }

    /// <summary>Trial Balance — كل الحسابات وأرصدتها (Sprint 38: L19 filtered by current tenant)</summary>
    [HttpGet("trial-balance")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountBalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrialBalance([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var r = await _ledger.GetTrialBalanceAsync(companyId, asOf, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>Sprint 54 (DEC-142): ميزان المراجعة الهرمي — يعرض L4 (Detail) مع L3 (Control) parent.
    /// يدعم الـ drill-down من الحسابات L3 إلى L4.
    /// Sprint 60 (DEC-191): يدعم فلتر costCenterId + projectId.</summary>
    [HttpGet("trial-balance-v2")]
    [ProducesResponseType(typeof(TrialBalanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrialBalanceV2(
        [FromQuery] DateTime? asOf,
        [FromQuery] Guid? costCenterId,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var asOfDate = (asOf ?? DateTime.UtcNow).Date;
        var r = await _report.GetTrialBalanceAsync(companyId, asOfDate, costCenterId, projectId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>دفتر أستاذ حساب معين (Sprint 38: L19 filtered by current tenant)</summary>
    [HttpGet("accounts/{accountId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerLineResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AccountLedger(Guid accountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var r = await _ledger.GetAccountLedgerAsync(companyId, accountId, from, to, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    /// <summary>الميزانية العمومية — Sprint 48 (DEC-130). Σ Assets = Σ Liab + Σ Equity.
    /// Sprint 60 (DEC-191): يدعم فلتر costCenterId + projectId.</summary>
    [HttpGet("balance-sheet")]
    [ProducesResponseType(typeof(BalanceSheetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BalanceSheet(
        [FromQuery] DateTime? asOf,
        [FromQuery] Guid? costCenterId,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var asOfDate = (asOf ?? DateTime.UtcNow).Date;
        var r = await _report.GetBalanceSheetAsync(companyId, asOfDate, costCenterId, projectId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>قائمة الدخل — Sprint 48 (DEC-131). Revenue − Expenses = Net Income.
    /// Sprint 60 (DEC-191): يدعم فلتر costCenterId + projectId.</summary>
    [HttpGet("income-statement")]
    [ProducesResponseType(typeof(IncomeStatementResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> IncomeStatement(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? costCenterId,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? new DateTime(toDate.Year, 1, 1)).Date;
        var r = await _report.GetIncomeStatementAsync(companyId, fromDate, toDate, costCenterId, projectId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>التدفقات النقدية (الطريقة غير المباشرة) — Sprint 48 (DEC-132).</summary>
    [HttpGet("cash-flow")]
    [ProducesResponseType(typeof(CashFlowResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CashFlow([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? new DateTime(toDate.Year, 1, 1)).Date;
        var r = await _report.GetCashFlowAsync(companyId, fromDate, toDate, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // ============ Sprint 53 (DEC-140 + DEC-141) — Year-End Closing ============

    /// <summary>إقفال السنة المالية — يحول أرصدة الإيرادات/المصروفات إلى 3210 ثم 3200.</summary>
    [HttpPost("year-end-closing")]
    [Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteFinance)]
    public async Task<IActionResult> CloseYear([FromQuery] int year, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        if (year < 2000 || year > 2100)
            return BadRequest(new ProblemDetails { Title = "سنة غير صالحة", Status = 400, Detail = "السنة يجب أن تكون بين 2000 و 2100." });

        var r = await _yearEnd.CloseYearAsync(companyId, year, ct);
        if (r.Success)
            return Ok(r);
        return BadRequest(new ProblemDetails { Title = "Year-End Closing Failed", Status = 400, Detail = r.Message });
    }

    /// <summary>حالة إقفال السنة — هل تم إقفالها؟</summary>
    [HttpGet("year-end-closing/status")]
    public async Task<IActionResult> GetCloseStatus([FromQuery] int year, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var status = await _yearEnd.GetStatusAsync(companyId, year, ct);
        return Ok(status);
    }

    // ============ Sprint 57 (DEC-152) — Executive Dashboard ============

    /// <summary>لوحة Executive Dashboard — KPIs + chart data (revenue trend, top customers, expense breakdown, AR/AP aging).</summary>
    [HttpGet("dashboard/executive")]
    public async Task<IActionResult> GetExecutiveDashboard(CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("No active company in context");
        var r = await _dashboard.GetAsync(companyId, ct);
        return Ok(r);
    }

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Ledger Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
