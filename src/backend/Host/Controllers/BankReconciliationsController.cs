// =====================================================================================
// Sprint 65 / Wave 3A (DEC-235 + DEC-237): BankReconciliationsController
// =====================================================================================
//
// Bank reconciliation endpoints. The controller is a thin shell over
// `IBankReconciliationService`; the matching algorithm and all DTO shapes live in
// the service so the unit tests can exercise the algorithm without a web stack.
//
// Routes (all require the WriteFinance policy — same as the existing AR endpoints
// because reconciliation is a financial-write operation that mutates the matched
// state of receipts and sub-payments):
//
//   GET  /api/receipts/{id}/suggest-matches?max=5
//   POST /api/receipts/{id}/confirm-match/{subPaymentId}
//   GET  /api/reconciliation/queue?skip=0&take=50
//
// L19 / DEC-095:
//   - CompanyId is read from `ICompanyContext.CompanyId` (set by the
//     CompanyContextMiddleware from the X-Company-Id header) inside the service.
//     The controller never reads companyId from a DTO.
//   - UserId is read from the JWT `sub`/`NameIdentifier` claim in the controller
//     and passed explicitly to `ConfirmMatchAsync`. The service does not extract
//     userId from any request DTO.
// =====================================================================================

using System.Security.Claims;
using ERPSystem.Modules.Finance.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteFinance)]
public class BankReconciliationsController : ControllerBase
{
    private readonly IBankReconciliationService _service;

    public BankReconciliationsController(IBankReconciliationService service)
    {
        _service = service;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? throw new InvalidOperationException("User identity not resolved (L19 / DEC-095)."));

    /// <summary>
    /// GET /api/receipts/{id}/suggest-matches?max=5
    /// Returns the top N candidate sub-payments for a single receipt, sorted by
    /// score desc. Score algorithm: amount ±5% (50/30/10) + date ±30 days (20/10/5).
    /// </summary>
    [HttpGet("receipts/{id:guid}/suggest-matches")]
    public async Task<IActionResult> SuggestMatches(
        Guid id, [FromQuery] int max = 5, CancellationToken ct = default)
    {
        var r = await _service.SuggestMatchesAsync(id, max, ct);
        if (!r.Succeeded)
        {
            return r.ErrorCode == "NOT_FOUND" ? NotFound(Problem(r)) : BadRequest(Problem(r));
        }
        return Ok(r.Value);
    }

    /// <summary>
    /// POST /api/receipts/{id}/confirm-match/{subPaymentId}
    /// Atomically links a receipt to a sub-payment and marks the sub-payment as
    /// "matched". 404 if the receipt does not exist; 409 if the sub-payment is
    /// already matched.
    /// </summary>
    [HttpPost("receipts/{id:guid}/confirm-match/{subPaymentId:guid}")]
    public async Task<IActionResult> ConfirmMatch(
        Guid id, Guid subPaymentId, CancellationToken ct = default)
    {
        var userId = UserId;
        var r = await _service.ConfirmMatchAsync(userId, id, subPaymentId, ct);
        if (!r.Succeeded)
        {
            return r.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(Problem(r)),
                "CONFLICT" => Conflict(Problem(r)),
                "VALIDATION" => BadRequest(Problem(r)),
                _ => BadRequest(Problem(r)),
            };
        }
        return Ok(r.Value);
    }

    /// <summary>
    /// GET /api/reconciliation/queue?skip=0&take=50
    /// Returns the page of posted receipts that have not been matched to a
    /// sub-payment. Used by the FE to render the reconciliation queue.
    /// </summary>
    [HttpGet("reconciliation/queue")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var r = await _service.GetQueueAsync(skip, take, ct);
        if (!r.Succeeded)
        {
            return BadRequest(Problem(r));
        }
        return Ok(r.Value);
    }

    private static ProblemDetails Problem<T>(BankReconciliationResult<T> r) => new()
    {
        Title = r.Error,
        Status = r.ErrorCode switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "VALIDATION" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        },
        Detail = r.Error,
    };
}
