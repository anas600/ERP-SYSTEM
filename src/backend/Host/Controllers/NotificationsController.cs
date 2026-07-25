using System.Security.Claims;
using ERPSystem.Modules.Notifications.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/inventory/notifications")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    public NotificationsController(INotificationService s) { _service = s; }
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var list = await _service.ListAsync(UserId, unreadOnly, skip, take, ct);
        return Ok(list);
    }
    [HttpGet("unread")]
    public async Task<IActionResult> Unread([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var list = await _service.ListAsync(UserId, true, skip, take, ct);
        return Ok(new { count = list.Count, items = list });
    }
    [HttpPost("{id:guid}/mark-read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await _service.MarkReadAsync(UserId, id, ct);
        return NoContent();
    }
}
