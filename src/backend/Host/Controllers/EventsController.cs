using ERPSystem.Shared.Events.Application.Services;
using ERPSystem.Shared.Events.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/events")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class EventsController : ControllerBase
{
    private readonly IOutboxRepository _outbox;
    private readonly ICompanyContext _companyContext;
    public EventsController(IOutboxRepository outbox, ICompanyContext companyContext) { _outbox = outbox; _companyContext = companyContext; }
    private Guid CompanyId => _companyContext.CompanyId ?? throw new UnauthorizedAccessException();

    /// <summary>Admin: list pending (unprocessed) events for the company</summary>
    [HttpGet("outbox")]
    public async Task<IActionResult> ListPending([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _outbox.ListAllAsync(CompanyId, unprocessedOnly: true, skip, take, ct);
        return Ok(new { count = list.Count, items = list });
    }

    /// <summary>Admin: list processed events (audit trail)</summary>
    [HttpGet("processed")]
    public async Task<IActionResult> ListProcessed([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _outbox.ListAllAsync(CompanyId, unprocessedOnly: false, skip, take, ct);
        return Ok(new { count = list.Count, items = list });
    }

    /// <summary>Admin: count of unprocessed events for the company</summary>
    [HttpGet("pending-count")]
    public async Task<IActionResult> PendingCount(CancellationToken ct)
    {
        var c = await _outbox.CountPendingAsync(CompanyId, ct);
        return Ok(new { count = c });
    }

    /// <summary>Admin: manual retry — resets retry_count so the processor picks it up again</summary>
    [HttpPost("retry/{id:guid}")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        var evt = await _outbox.GetByIdAsync(id, ct);
        if (evt == null || evt.CompanyId != CompanyId) return NotFound();
        await _outbox.ResetForRetryAsync(id, ct);
        return NoContent();
    }
}
