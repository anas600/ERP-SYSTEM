// Sprint 11 T2 (BE Jimi) — TransactionsController.
//
// New controller for the demo /transactions page. Returns recent
// journal lines as `TransactionDto` (per the FE contract in api-types.ts).
//
// Routes:
//   GET /api/transactions/recent?limit=N — most recent journal lines
//   GET /api/transactions?limit=N        — alias (the task spec asks
//                                          for this shape; the FE uses
//                                          /recent)
//
// Auth: ReadAccess policy. The transactions page is part of the demo
// landing surface, all roles can view it.

using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Finance.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
[RequirePermission("finance.reports.view")]
public class TransactionsController : ControllerBase
{
    private readonly IFinanceService _finance;
    public TransactionsController(IFinanceService finance) => _finance = finance;

    // GET /api/transactions/recent?limit=N — most recent journal lines
    // (per the FE's `getRecentTransactions()` contract).
    [HttpGet("api/transactions/recent")]
    public async Task<IActionResult> GetRecent(
        [FromQuery] int? limit,
        CancellationToken ct = default)
    {
        var r = await _finance.GetRecentTransactionsAsync(limit ?? 20, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // GET /api/transactions?limit=N — alias route, per the task spec.
    [HttpGet("api/transactions")]
    public async Task<IActionResult> Get(
        [FromQuery] int? limit,
        CancellationToken ct = default)
    {
        var r = await _finance.GetRecentTransactionsAsync(limit ?? 20, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Transactions Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
