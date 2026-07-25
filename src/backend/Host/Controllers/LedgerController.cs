using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/finance/ledger")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class LedgerController : ControllerBase
{
    private readonly IGeneralLedgerService _ledger;

    public LedgerController(IGeneralLedgerService ledger)
    {
        _ledger = ledger;
    }

    /// <summary>Trial Balance — كل الحسابات وأرصدتها</summary>
    [HttpGet("trial-balance")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountBalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrialBalance([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var r = await _ledger.GetTrialBalanceAsync(asOf, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>دفتر أستاذ حساب معين</summary>
    [HttpGet("accounts/{accountId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerLineResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AccountLedger(Guid accountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var r = await _ledger.GetAccountLedgerAsync(accountId, from, to, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Ledger Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
