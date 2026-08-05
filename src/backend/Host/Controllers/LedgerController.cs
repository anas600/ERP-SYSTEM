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
    private readonly ICompanyContext _companyContext;

    public LedgerController(IGeneralLedgerService ledger, IGeneralLedgerReportService report, ICompanyContext companyContext)
    {
        _ledger = ledger;
        _report = report;
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

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Ledger Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
