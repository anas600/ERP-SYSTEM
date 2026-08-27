using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 64 (DEC-222) — Sub-Contract REST API.
///
/// <para>Routes (5 endpoints):</para>
/// <list type="bullet">
///   <item>GET    /api/projects/{projectId}/sub-contracts  — list by project</item>
///   <item>GET    /api/sub-contracts/{id}                  — details</item>
///   <item>POST   /api/projects/{projectId}/sub-contracts  — create (UNIQUE on (project, number))</item>
///   <item>PUT    /api/sub-contracts/{id}                  — update</item>
///   <item>DELETE /api/sub-contracts/{id}                  — soft delete (refuses if billings exist)</item>
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
[Authorize]
[RequirePermission("projects.sub_contracts.view")]
public sealed class SubContractsController : ControllerBase
{
    private readonly ISubContractService _service;

    public SubContractsController(ISubContractService service)
    {
        _service = service;
    }

    /// <summary>
    /// L19 / DEC-095: read the userId from the JWT context. NEVER trust a request
    /// DTO for userId (see L186 / Sprint 61 fix).
    /// </summary>
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [HttpGet("api/projects/{projectId:guid}/sub-contracts")]
    [RequirePermission("projects.sub_contracts.view")]
    public async Task<IActionResult> ListByProject(Guid projectId, CancellationToken ct)
    {
        var r = await _service.ListByProjectAsync(projectId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/sub-contracts/{id:guid}")]
    [RequirePermission("projects.sub_contracts.view")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/projects/{projectId:guid}/sub-contracts")]
    [RequirePermission("projects.sub_contracts.create")]
    public async Task<IActionResult> Create(
        Guid projectId, [FromBody] CreateSubContractRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(UserId, projectId, req, ct);
        if (r.Succeeded)
            return CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value);

        return r.ErrorCode switch
        {
            SubContractErrorCode.AlreadyExists => Conflict(Problem(r)),
            SubContractErrorCode.NotFound => NotFound(Problem(r)),
            SubContractErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpPut("api/sub-contracts/{id:guid}")]
    [RequirePermission("projects.sub_contracts.update")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSubContractRequest req, CancellationToken ct)
    {
        var r = await _service.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : r.ErrorCode switch
        {
            SubContractErrorCode.NotFound => NotFound(Problem(r)),
            SubContractErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpDelete("api/sub-contracts/{id:guid}")]
    [RequirePermission("projects.sub_contracts.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _service.SoftDeleteAsync(UserId, id, ct);
        return r.Succeeded ? NoContent() : r.ErrorCode switch
        {
            SubContractErrorCode.NotFound => NotFound(Problem(r)),
            SubContractErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    private static ProblemDetails Problem<T>(SubContractResult<T> r) => new()
    {
        Title = "SubContract Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
