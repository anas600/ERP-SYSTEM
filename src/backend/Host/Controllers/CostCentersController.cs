using ERPSystem.Modules.Companies.Application.Services;
using ERPSystem.Modules.Companies.Entities;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/cost-centers")]
[Authorize]
public class CostCentersController : ControllerBase
{
    private readonly ICostCenterService _service;
    private readonly ITenantContext _tenant;
    public CostCentersController(ICostCenterService service, ITenantContext tenant) { _service = service; _tenant = tenant; }
    private Guid TenantId => _tenant.TenantId ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? companyId,
        [FromQuery] CostCenterType? type,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var r = await _service.ListAsync(TenantId, companyId, type, includeInactive, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(TenantId, id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpGet("{id:guid}/children")]
    public async Task<IActionResult> GetChildren(Guid id, CancellationToken ct)
    {
        var r = await _service.GetChildrenAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}/budget-status")]
    public async Task<IActionResult> BudgetStatus(Guid id, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var r = await _service.GetBudgetStatusAsync(TenantId, id, asOf, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCostCenterRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(TenantId, req, ct);
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

    private static ProblemDetails Problem<T>(CostCenterResult<T> r) => new()
    {
        Title = "CostCenter Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
