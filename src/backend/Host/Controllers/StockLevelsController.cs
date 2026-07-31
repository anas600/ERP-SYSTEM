using ERPSystem.Modules.Inventory.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/inventory/levels")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class StockLevelsController : ControllerBase
{
    private readonly IStockLevelService _service;
    public StockLevelsController(IStockLevelService s) { _service = s; }

    [HttpGet]
    public async Task<IActionResult> ListByItem([FromQuery] Guid itemId, CancellationToken ct)
    {
        if (itemId == Guid.Empty) return BadRequest("itemId required");
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
    public async Task<IActionResult> LowStock([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (companyId == Guid.Empty) return BadRequest("companyId required");
        var r = await _service.GetLowStockAsync(companyId, ct);
        return r.Succeeded ? Ok(r.Value) : Ok(Array.Empty<object>());
    }
    private static ProblemDetails Problem<T>(StockMovementResult<T> r) => new()
    { Title = "Stock Level Error", Status = StatusCodes.Status400BadRequest, Detail = r.Error };
}
