using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 64 (DEC-221) — Subcontractor REST API.
///
/// <para>Routes (5 endpoints):</para>
/// <list type="bullet">
///   <item>GET    /api/subcontractors            — list (filter by isActive, tradeSpecialty)</item>
///   <item>GET    /api/subcontractors/{id}        — details</item>
///   <item>POST   /api/subcontractors            — create (UNIQUE on (company_id, code))</item>
///   <item>PUT    /api/subcontractors/{id}        — update</item>
///   <item>DELETE /api/subcontractors/{id}        — soft delete (sets is_active=false)</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: <c>companyId</c> always comes from
/// <c>ICompanyContext</c>, never from the request DTO. <c>userId</c> comes from
/// the JWT <c>NameIdentifier</c> claim, not the request body (L186 lesson —
/// Sprint 61 EngineerId pattern).</para>
///
/// <para><b>Sprint 63 RBAC (DEC-215, DEC-216)</b>: each endpoint requires the
/// corresponding permission code from <c>RbacSeedData.json</c> via
/// <see cref="RequirePermissionAttribute"/>. Admin role bypasses the check.</para>
/// </summary>
[ApiController]
[Route("api/subcontractors")]
[Authorize]
[RequirePermission("projects.subcontractors.view")]
public sealed class SubcontractorsController : ControllerBase
{
    private readonly ISubcontractorService _service;

    public SubcontractorsController(ISubcontractorService service)
    {
        _service = service;
    }

    /// <summary>
    /// L19 / DEC-095: read the userId from the JWT context. NEVER trust a request
    /// DTO for userId (see L186 / Sprint 61 fix).
    /// </summary>
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [HttpGet]
    [RequirePermission("projects.subcontractors.view")]
    public async Task<IActionResult> List(
        [FromQuery] bool? isActive,
        [FromQuery] string? tradeSpecialty,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (take < 1 || take > 200) take = 50;
        if (skip < 0) skip = 0;
        var r = await _service.ListAsync(isActive, tradeSpecialty, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("projects.subcontractors.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost]
    [RequirePermission("projects.subcontractors.create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubcontractorRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(UserId, req, ct);
        if (r.Succeeded)
            return CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value);

        return r.ErrorCode switch
        {
            SubcontractorErrorCode.AlreadyExists => Conflict(Problem(r)),
            SubcontractorErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("projects.subcontractors.update")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSubcontractorRequest req, CancellationToken ct)
    {
        var r = await _service.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : r.ErrorCode switch
        {
            SubcontractorErrorCode.NotFound => NotFound(Problem(r)),
            SubcontractorErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("projects.subcontractors.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _service.SoftDeleteAsync(UserId, id, ct);
        return r.Succeeded ? NoContent() : r.ErrorCode switch
        {
            SubcontractorErrorCode.NotFound => NotFound(Problem(r)),
            SubcontractorErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    private static ProblemDetails Problem<T>(SubcontractorResult<T> r) => new()
    {
        Title = "Subcontractor Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
