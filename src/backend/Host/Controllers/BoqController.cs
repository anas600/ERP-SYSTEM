using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/boq")]
[Authorize]
public sealed class BoqController : ControllerBase
{
    private readonly IBoqService _service;
    private readonly ICompanyContext _ctx;

    public BoqController(IBoqService service, ICompanyContext ctx)
    {
        _service = service; _ctx = ctx;
    }

    [HttpGet("sections")]
    public async Task<IActionResult> ListSections(Guid projectId, CancellationToken ct)
        => Ok(await _service.ListSectionsAsync(projectId, ct));

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection(Guid projectId, [FromBody] CreateBoqSectionRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        var id = await _service.CreateSectionAsync(companyId.Value, projectId, userId.Value, req, ct);
        return Created($"/api/projects/{projectId}/boq/sections/{id}", new { id });
    }

    [HttpGet("lines")]
    public async Task<IActionResult> ListLines(Guid projectId, CancellationToken ct)
        => Ok(await _service.ListLinesAsync(projectId, ct));

    [HttpPost("lines")]
    public async Task<IActionResult> CreateLine(Guid projectId, [FromBody] CreateBoqLineRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        var id = await _service.CreateLineAsync(companyId.Value, projectId, userId.Value, req, ct);
        return Created($"/api/projects/{projectId}/boq/lines/{id}", new { id });
    }

    [HttpGet("lines/{lineId:guid}/subitems")]
    public async Task<IActionResult> ListSubitems(Guid projectId, Guid lineId, CancellationToken ct)
        => Ok(await _service.ListSubitemsAsync(lineId, ct));

    [HttpPost("lines/subitems")]
    public async Task<IActionResult> CreateSubitem(Guid projectId, [FromBody] CreateBoqSubitemRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        var id = await _service.CreateSubitemAsync(companyId.Value, userId.Value, req, ct);
        return Created($"/api/projects/{projectId}/boq/lines/{req.BoqLineId}/subitems/{id}", new { id });
    }
}
