using ERPSystem.Modules.Companies.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteMasterData)]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _service;
    public CompaniesController(ICompanyService service)
    { _service = service; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var r = await _service.ListAsync(includeInactive, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("tree")]
    public async Task<IActionResult> Tree(CancellationToken ct = default)
    {
        var r = await _service.GetTreeAsync(ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound();
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
        var r = await _service.CreateHoldingAsync(req.Code, req.Name, req.LegalName ?? req.Name, req.BaseCurrency, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPost("subsidiary")]
    public async Task<IActionResult> AddSubsidiary([FromBody] AddSubsidiaryRequest req, CancellationToken ct)
    {
        var r = await _service.AddSubsidiaryAsync(req.ParentCompanyId, req.Code, req.Name, req.LegalName, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var r = await _service.DeactivateAsync(id, ct);
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
