using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 64 (DEC-223) — Sub-ProgressBilling REST API.
///
/// <para>Routes (5 endpoints):</para>
/// <list type="bullet">
///   <item>GET  /api/sub-contracts/{subContractId}/billings    — list by sub-contract</item>
///   <item>GET  /api/sub-progress-billings/{id}                — details</item>
///   <item>POST /api/sub-contracts/{subContractId}/billings    — create (computes gross/retention/net)</item>
///   <item>PUT  /api/sub-progress-billings/{id}                — update (Draft only)</item>
///   <item>POST /api/sub-progress-billings/{id}/approve        — Draft → Approved</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: <c>companyId</c> always comes from
/// <c>ICompanyContext</c>, never from the request DTO. <c>userId</c> comes from
/// the JWT <c>NameIdentifier</c> claim, not the request body (L186 lesson —
/// Sprint 61 EngineerId pattern).</para>
///
/// <para><b>Sprint 63 RBAC (DEC-215, DEC-216)</b>: each endpoint requires the
/// corresponding permission code via <see cref="RequirePermissionAttribute"/>.
/// Admin role bypasses the check.</para>
/// </summary>
[ApiController]
[Authorize]
[RequirePermission("projects.sub_progress_billings.view")]
public sealed class SubProgressBillingsController : ControllerBase
{
    private readonly ISubProgressBillingService _service;

    public SubProgressBillingsController(ISubProgressBillingService service)
    {
        _service = service;
    }

    /// <summary>
    /// L19 / DEC-095: read the userId from the JWT context. NEVER trust a request
    /// DTO for userId (see L186 / Sprint 61 fix).
    /// </summary>
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [HttpGet("api/sub-contracts/{subContractId:guid}/billings")]
    [RequirePermission("projects.sub_progress_billings.view")]
    public async Task<IActionResult> ListBySubContract(Guid subContractId, CancellationToken ct)
    {
        var r = await _service.ListBySubContractAsync(subContractId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/sub-progress-billings/{id:guid}")]
    [RequirePermission("projects.sub_progress_billings.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/sub-contracts/{subContractId:guid}/billings")]
    [RequirePermission("projects.sub_progress_billings.create")]
    public async Task<IActionResult> Create(
        Guid subContractId, [FromBody] CreateSubProgressBillingRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(UserId, subContractId, req, ct);
        if (r.Succeeded)
            return CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value);

        return r.ErrorCode switch
        {
            SubProgressBillingErrorCode.AlreadyExists => Conflict(Problem(r)),
            SubProgressBillingErrorCode.NotFound => NotFound(Problem(r)),
            SubProgressBillingErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpPut("api/sub-progress-billings/{id:guid}")]
    [RequirePermission("projects.sub_progress_billings.update")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSubProgressBillingRequest req, CancellationToken ct)
    {
        var r = await _service.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : r.ErrorCode switch
        {
            SubProgressBillingErrorCode.NotFound => NotFound(Problem(r)),
            SubProgressBillingErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpPost("api/sub-progress-billings/{id:guid}/approve")]
    [RequirePermission("projects.sub_progress_billings.approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var r = await _service.ApproveAsync(UserId, id, ct);
        return r.Succeeded ? Ok(r.Value) : r.ErrorCode switch
        {
            SubProgressBillingErrorCode.NotFound => NotFound(Problem(r)),
            SubProgressBillingErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    private static ProblemDetails Problem<T>(SubProgressBillingResult<T> r) => new()
    {
        Title = "SubProgressBilling Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
