using ERPSystem.Modules.Companies.Application.Services;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _service;
    private readonly ITenantContext _tenant;
    public CompaniesController(ICompanyService service, ITenantContext tenant) { _service = service; _tenant = tenant; }
    private Guid TenantId => _tenant.TenantId ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var r = await _service.ListAsync(TenantId, includeInactive, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("tree")]
    public async Task<IActionResult> Tree(CancellationToken ct)
    {
        var r = await _service.GetTreeAsync(TenantId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(TenantId, id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpGet("{id:guid}/subsidiaries")]
    public async Task<IActionResult> GetSubsidiaries(Guid id, CancellationToken ct)
    {
        var r = await _service.GetSubsidiariesAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPost("holding")]
    public async Task<IActionResult> CreateHolding([FromBody] CreateHoldingRequest req, CancellationToken ct)
    {
        var r = await _service.CreateHoldingAsync(TenantId, req.Code, req.Name, req.LegalName ?? req.Name, req.BaseCurrency, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPost("subsidiary")]
    public async Task<IActionResult> AddSubsidiary([FromBody] AddSubsidiaryRequest req, CancellationToken ct)
    {
        var r = await _service.AddSubsidiaryAsync(TenantId, req.ParentCompanyId, req.Code, req.Name, req.LegalName, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var r = await _service.DeactivateAsync(TenantId, id, ct);
        return r.Succeeded ? NoContent() : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(CompanyResult<T> r) => new()
    {
        Title = "Company Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}

public sealed class CreateHoldingRequest
{
    public string Code { get; set; } = "000";
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string BaseCurrency { get; set; } = "LYD";
}

public sealed class AddSubsidiaryRequest
{
    public Guid ParentCompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
}
