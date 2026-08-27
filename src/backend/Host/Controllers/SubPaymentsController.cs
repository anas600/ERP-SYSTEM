using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 64 (DEC-224) — Sub-Payment REST API.
///
/// <para>Routes (5 endpoints):</para>
/// <list type="bullet">
///   <item>GET  /api/sub-contracts/{subContractId}/payments                  — list by sub-contract</item>
///   <item>GET  /api/sub-payments/{id}                                       — details</item>
///   <item>POST /api/sub-contracts/{subContractId}/billings/{billingId}/payments — create (regular payment)</item>
///   <item>POST /api/sub-contracts/{subContractId}/release-retention         — release retention (creates a release payment)</item>
///   <item>GET  /api/sub-contracts/{subContractId}/balance                   — outstanding balance (Sprint 64 statement lite)</item>
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
[RequirePermission("projects.sub_payments.view")]
public sealed class SubPaymentsController : ControllerBase
{
    private readonly ISubPaymentService _service;

    public SubPaymentsController(ISubPaymentService service)
    {
        _service = service;
    }

    /// <summary>
    /// L19 / DEC-095: read the userId from the JWT context. NEVER trust a request
    /// DTO for userId (see L186 / Sprint 61 fix).
    /// </summary>
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [HttpGet("api/sub-contracts/{subContractId:guid}/payments")]
    [RequirePermission("projects.sub_payments.view")]
    public async Task<IActionResult> ListBySubContract(Guid subContractId, CancellationToken ct)
    {
        var r = await _service.ListBySubContractAsync(subContractId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/sub-payments/{id:guid}")]
    [RequirePermission("projects.sub_payments.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/sub-contracts/{subContractId:guid}/billings/{billingId:guid}/payments")]
    [RequirePermission("projects.sub_payments.create")]
    public async Task<IActionResult> Create(
        Guid subContractId, Guid billingId,
        [FromBody] CreateSubPaymentRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(UserId, subContractId, billingId, req, ct);
        if (r.Succeeded)
            return CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value);

        return r.ErrorCode switch
        {
            SubPaymentErrorCode.AlreadyExists => Conflict(Problem(r)),
            SubPaymentErrorCode.NotFound => NotFound(Problem(r)),
            SubPaymentErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpPost("api/sub-contracts/{subContractId:guid}/release-retention")]
    [RequirePermission("projects.sub_payments.create")]
    public async Task<IActionResult> ReleaseRetention(
        Guid subContractId, [FromBody] ReleaseRetentionRequest req, CancellationToken ct)
    {
        var r = await _service.ReleaseRetentionAsync(UserId, subContractId, req, ct);
        if (r.Succeeded)
            return CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value);

        return r.ErrorCode switch
        {
            SubPaymentErrorCode.NotFound => NotFound(Problem(r)),
            SubPaymentErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpGet("api/sub-contracts/{subContractId:guid}/balance")]
    [RequirePermission("projects.sub_payments.view")]
    public async Task<IActionResult> GetBalance(Guid subContractId, CancellationToken ct)
    {
        var r = await _service.GetBalanceAsync(subContractId, ct);
        return r.Succeeded ? Ok(r.Value) : r.ErrorCode switch
        {
            SubPaymentErrorCode.NotFound => NotFound(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    private static ProblemDetails Problem<T>(SubPaymentResult<T> r) => new()
    {
        Title = "SubPayment Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
