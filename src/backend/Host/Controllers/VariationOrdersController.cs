using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/variations")]
[Authorize]
public sealed class VariationOrdersController : ControllerBase
{
    private readonly IVariationOrderService _service;
    private readonly ICompanyContext _ctx;

    public VariationOrdersController(IVariationOrderService service, ICompanyContext ctx)
    {
        _service = service; _ctx = ctx;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
        => Ok(await _service.ListAsync(projectId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid projectId, Guid id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateVariationOrderRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        var id = await _service.CreateAsync(companyId.Value, userId.Value, req, ct);
        return Created($"/api/projects/{projectId}/variations/{id}", new { id });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid projectId, Guid id, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        await _service.ApproveAsync(companyId.Value, id, userId.Value, ct);
        return NoContent();
    }

    [HttpPost("lines")]
    public async Task<IActionResult> AddLine(Guid projectId, [FromBody] CreateVariationOrderLineRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        var id = await _service.AddLineAsync(companyId.Value, userId.Value, req, ct);
        return Created($"/api/projects/{projectId}/variations/lines/{id}", new { id });
    }
}
