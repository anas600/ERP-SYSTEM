using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/price-lists")]
[Authorize]
public sealed class PriceListsController : ControllerBase
{
    private readonly IPriceListService _service;
    private readonly ICompanyContext _ctx;

    public PriceListsController(IPriceListService service, ICompanyContext ctx)
    {
        _service = service; _ctx = ctx;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        if (companyId == null || companyId == Guid.Empty) return BadRequest("Company context missing");
        var list = await _service.ListAsync(companyId.Value, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePriceListRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        var userId = _ctx.UserId;
        if (companyId == null || companyId == Guid.Empty || userId == null || userId == Guid.Empty) return BadRequest("Context missing");
        var id = await _service.CreateAsync(companyId.Value, userId.Value, req, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{priceListId:guid}/items")]
    public async Task<IActionResult> ListItems(Guid priceListId, CancellationToken ct)
    {
        var items = await _service.ListItemsAsync(priceListId, ct);
        return Ok(items);
    }

    [HttpPost("{priceListId:guid}/items")]
    public async Task<IActionResult> CreateItem(Guid priceListId, [FromBody] CreatePriceListItemRequest req, CancellationToken ct)
    {
        var companyId = _ctx.CompanyId;
        if (companyId == null || companyId == Guid.Empty) return BadRequest("Company context missing");
        var id = await _service.CreateItemAsync(companyId.Value, priceListId, req, ct);
        return Created($"/api/price-lists/{priceListId}/items/{id}", new { id });
    }
}
