using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Finance.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/finance/journal-entries")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteFinance)]
[RequirePermission("finance.journal.view")]
public class JournalEntriesController : ControllerBase
{
    private readonly IJournalEntryService _service;
    private readonly IValidator<PostJournalEntryRequest> _validator;

    public JournalEntriesController(
        IJournalEntryService service,
        IValidator<PostJournalEntryRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    private Guid? CurrentUserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(v, out var g) ? g : null;
    }

    [HttpGet]
    [RequirePermission("finance.journal.view")]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] JournalEntryStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (take is < 1 or > 200) take = 50;
        var r = await _service.ListAsync(from, to, status, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("finance.journal.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    /// <summary>إنشاء قيد (Draft). يجب أن يكون متوازن (Σ debit = Σ credit).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(JournalEntryResponse), StatusCodes.Status201Created)]
    [RequirePermission("finance.journal.create")]
    public async Task<IActionResult> CreateDraft([FromBody] PostJournalEntryRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var v = await _validator.ValidateAsync(request, ct);
        if (!v.IsValid) return ValidationProblem(new ValidationProblemDetails(
            v.Errors.GroupBy(e => e.PropertyName).ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray())));

        var r = await _service.CreateDraftAsync(userId.Value, request, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    /// <summary>ترحيل قيد (Draft → Posted). يجعله يؤثر على General Ledger.</summary>
    [HttpPost("{id:guid}/post")]
    [RequirePermission("finance.journal.post")]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var r = await _service.PostAsync(userId.Value, id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(FinanceResult<T> r) => new()
    {
        Title = "Finance Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
