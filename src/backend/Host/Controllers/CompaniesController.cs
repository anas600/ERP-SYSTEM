using ERPSystem.Modules.Companies.Application.Services;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteMasterData)]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _service;
    private readonly ICompanyContext _companyContext;
    public CompaniesController(ICompanyService service, ICompanyContext companyContext)
    {
        _service = service;
        _companyContext = companyContext;
    }

    // Sprint 2 (T1 / Block A): paged list. Defaults: page=1, pageSize=20; max
    // pageSize=100 (clamped by the service). The multi-company scope is enforced
    // via user_companies when ICompanyContext has a resolved user — admin users
    // without a user_id see ALL companies (the call goes through without the
    // user filter).
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var userId = _companyContext.UserId;
        var r = await _service.ListPagedAsync(page, pageSize, includeInactive, userId, ct);
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

    // Sprint 2 (T3 / Block A): top-level create. Idempotent on `code` (returns
    // 200 with the existing company if a row with the same code already exists).
    // On a fresh create, returns 201 with a Location header pointing to /api/companies/{id}.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest req, CancellationToken ct)
    {
        if (req == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Company Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "الطلب فارغ.",
            });
        }

        var r = await _service.CreateAsync(req, ct);
        if (!r.Succeeded) return BadRequest(Problem(r));

        var company = r.Value!.Company;
        if (r.Value.WasCreated)
        {
            return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
        }
        // Idempotent path: a row with the same code already exists. The FE gets
        // the existing company (so it can navigate to /admin/companies/{id}).
        return Ok(company);
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
