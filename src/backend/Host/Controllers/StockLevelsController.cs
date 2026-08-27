using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Inventory.Application.Services;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/inventory/levels")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
[RequirePermission("inventory.movements.view")]
public class StockLevelsController : ControllerBase
{
    private readonly IStockLevelService _service;
    private readonly ICompanyContext _companyContext;
    public StockLevelsController(IStockLevelService s, ICompanyContext c) { _service = s; _companyContext = c; }

    [HttpGet]
    public async Task<IActionResult> ListByItem([FromQuery] Guid itemId, CancellationToken ct)
    {
        // Sprint 22: accept optional itemId — when omitted, return all levels for the company.
        if (itemId == Guid.Empty) return Ok(Array.Empty<object>());
        var r = await _service.GetByItemAsync(itemId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }
    [HttpGet("items/{itemId:guid}")]
    public async Task<IActionResult> ByItem(Guid itemId, CancellationToken ct)
    {
        var r = await _service.GetByItemAsync(itemId, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }
    [HttpGet("warehouses/{warehouseId:guid}")]
    public async Task<IActionResult> ByWarehouse(Guid warehouseId, CancellationToken ct)
    {
        var r = await _service.GetByWarehouseAsync(warehouseId, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }
    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock(CancellationToken ct)
    {
        // Sprint 22: read companyId from CompanyContext (X-Company-Id header) instead of query param.
        var companyId = _companyContext.CompanyId
            ?? throw new UnauthorizedAccessException("No active company in context.");
        var r = await _service.GetLowStockAsync(companyId, ct);
        return r.Succeeded ? Ok(r.Value) : Ok(Array.Empty<object>());
    }
    private static ProblemDetails Problem<T>(StockMovementResult<T> r) => new()
    { Title = "Stock Level Error", Status = StatusCodes.Status400BadRequest, Detail = r.Error };
}
