// Sprint 11 T2 (BE Jimi) — AccountsController (refactored).
//
// The legacy /api/finance/accounts route is kept for the existing FE
// (legacyAccount shape with numeric enums), and the new /api/accounts
// route is added for the demo pages (string enums, flat shape, per
// the FE contract in api-types.ts).
//
// Both routes are served by the same controller so the DI graph stays
// clean. The legacy methods delegate to IChartOfAccountsService; the
// new methods delegate to IFinanceService.ListAccountsAsync.

using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteFinance)]
public class AccountsController : ControllerBase
{
    private readonly IChartOfAccountsService _legacy;
    private readonly IFinanceService _finance;
    private readonly IValidator<CreateAccountRequest> _validator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IChartOfAccountsService legacy,
        IFinanceService finance,
        IValidator<CreateAccountRequest> validator,
        ILogger<AccountsController> logger)
    {
        _legacy = legacy;
        _finance = finance;
        _validator = validator;
        _logger = logger;
    }

    // ============ Legacy routes (/api/finance/accounts) — kept for
    // the existing FE pages. The legacy AccountResponse uses numeric
    // enums (type: 1..5, normalBalance: 1..2) so the FE doesn't break. ============

    [HttpGet("api/finance/accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLegacy([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var r = await _legacy.ListAsync(includeInactive, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(ProblemLegacy(r));
    }

    [HttpGet("api/finance/accounts/{id:guid}")]
    public async Task<IActionResult> GetByIdLegacy(Guid id, CancellationToken ct)
    {
        var r = await _legacy.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound();
    }

    // Sprint 52a (Phase 4): tree view of the CoA. Returns L1 roots with nested children.
    [HttpGet("api/finance/accounts/tree")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountTreeNode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var r = await _legacy.ListAsync(includeInactive, ct);
        if (!r.Succeeded) return BadRequest(ProblemLegacy(r));

        // Build a lookup by Id and a children list per parent.
        // Then walk roots (no parent) and assemble the tree in-memory.
        var nodes = r.Value!.Select(a => new AccountTreeNode
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            Type = a.Type,
            NormalBalance = a.NormalBalance,
            Level = a.Level ?? (short)99,
            IsPostable = a.IsPostable,
        }).ToList();

        var byId = nodes.ToDictionary(n => n.Id);
        var roots = new List<AccountTreeNode>();
        foreach (var n in nodes.OrderBy(x => x.Code, StringComparer.Ordinal))
        {
            // We need parent info — use the source account list.
            var src = r.Value!.First(a => a.Id == n.Id);
            if (src.ParentAccountId.HasValue && byId.TryGetValue(src.ParentAccountId.Value, out var parent))
            {
                parent.Children.Add(n);
            }
            else
            {
                roots.Add(n);
            }
        }
        return Ok(roots);
    }

    [HttpGet("api/finance/accounts/by-code/{code}")]
    public async Task<IActionResult> GetByCodeLegacy(string code, CancellationToken ct)
    {
        var r = await _legacy.GetByCodeAsync(code, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(ProblemLegacy(r));
    }

    [HttpPost("api/finance/accounts")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLegacy([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var v = await _validator.ValidateAsync(request, ct);
        if (!v.IsValid) return ValidationProblem(new ValidationProblemDetails(
            v.Errors.GroupBy(e => e.PropertyName).ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray())));

        FinanceResult<AccountResponse> r;
        try
        {
            r = await _legacy.CreateAsync(request, ct);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503" || ex.SqlState == "23502")
        {
            // Sprint 41 (DEC-128): never leak the raw FK / NOT NULL message to the user.
            // Map to a friendly Arabic explanation; the real cause is logged for ops.
            _logger.LogWarning(ex, "FK/NOT NULL violation on account create: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "بيانات مرفوضة",
                Status = StatusCodes.Status400BadRequest,
                Detail = "لا يمكن إنشاء الحساب — تحقق من اختيار الشركة النشطة ومن صحة بيانات الحساب الأب.",
            });
        }

        if (r.Succeeded)
        {
            return CreatedAtAction(nameof(GetByIdLegacy), new { id = r.Value!.Id }, r.Value);
        }
        return BadRequest(ProblemLegacy(r));
    }

    [HttpDelete("api/finance/accounts/{id:guid}")]
    public async Task<IActionResult> DeleteLegacy(Guid id, CancellationToken ct)
    {
        var r = await _legacy.DeleteAsync(id, ct);
        return r.Succeeded ? NoContent() : BadRequest(ProblemLegacy(r));
    }

    // ============ New routes (/api/accounts) — demo-grade flat DTO
    // with string enums (per the FE's `AccountType` / `NormalBalance`
    // unions). Per the FE's `getAccounts()` contract. ============

    // GET /api/accounts — flat list of accounts for the active company.
    // String enums (Asset/Liability/Equity/Revenue/Expense, Debit/Credit).
    [HttpGet("api/accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var r = await _finance.ListAccountsAsync(includeInactive, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // GET /api/accounts/{id} — single account.
    [HttpGet("api/accounts/{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _finance.GetAccountByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    private static ProblemDetails ProblemLegacy<T>(FinanceResult<T> r) => new()
    {
        Title = "Finance Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Finance Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
