using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Shared.MultiTenancy;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/finance/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IChartOfAccountsService _service;
    private readonly ITenantContext _tenantContext;
    private readonly IValidator<CreateAccountRequest> _validator;

    public AccountsController(
        IChartOfAccountsService service,
        ITenantContext tenantContext,
        IValidator<CreateAccountRequest> validator)
    {
        _service = service;
        _tenantContext = tenantContext;
        _validator = validator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        if (!_tenantContext.IsResolved) return Unauthorized();
        var r = await _service.ListAsync(_tenantContext.TenantId!.Value, includeInactive, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!_tenantContext.IsResolved) return Unauthorized();
        var r = await _service.GetByIdAsync(_tenantContext.TenantId!.Value, id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        if (!_tenantContext.IsResolved) return Unauthorized();
        var r = await _service.GetByCodeAsync(_tenantContext.TenantId!.Value, code, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        if (!_tenantContext.IsResolved) return Unauthorized();
        var v = await _validator.ValidateAsync(request, ct);
        if (!v.IsValid) return ValidationProblem(new ValidationProblemDetails(
            v.Errors.GroupBy(e => e.PropertyName).ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray())));

        var r = await _service.CreateAsync(_tenantContext.TenantId!.Value, request, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!_tenantContext.IsResolved) return Unauthorized();
        var r = await _service.DeleteAsync(_tenantContext.TenantId!.Value, id, ct);
        return r.Succeeded ? NoContent() : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Finance Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
