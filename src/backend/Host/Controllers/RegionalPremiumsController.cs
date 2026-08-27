using System.Security.Claims;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 62 (DEC-197) — Regional Premium REST API.
///
/// <para>Routes (4 endpoints):</para>
/// <list type="bullet">
///   <item>GET    /api/projects/{projectId}/regional-premiums — list all premiums for a project</item>
///   <item>POST   /api/projects/{projectId}/regional-premiums — create a new premium (UNIQUE on (project, region))</item>
///   <item>PUT    /api/projects/{projectId}/regional-premiums/{id} — update an existing premium</item>
///   <item>DELETE /api/projects/{projectId}/regional-premiums/{id} — soft-style hard delete</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: companyId always comes from <c>ICompanyContext</c>, never
/// from the request DTO. <c>userId</c> comes from the JWT <c>NameIdentifier</c> claim,
/// not the request body (L186 lesson — Sprint 61 EngineerId pattern).</para>
/// </summary>
[ApiController]
[Authorize]
public sealed class RegionalPremiumsController : ControllerBase
{
    private readonly IRegionalPremiumService _service;

    public RegionalPremiumsController(IRegionalPremiumService service)
    {
        _service = service;
    }

    /// <summary>
    /// L19 / DEC-095: read the userId from the JWT context. NEVER trust a request
    /// DTO for userId (see L186 / Sprint 61 fix).
    /// </summary>
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [HttpGet("api/projects/{projectId:guid}/regional-premiums")]
    public async Task<IActionResult> ListByProject(Guid projectId, CancellationToken ct)
    {
        var r = await _service.ListByProjectAsync(projectId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPost("api/projects/{projectId:guid}/regional-premiums")]
    public async Task<IActionResult> Create(
        Guid projectId, [FromBody] CreateRegionalPremiumRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(UserId, projectId, req, ct);
        if (r.Succeeded)
            return CreatedAtAction(nameof(ListByProject), new { projectId }, r.Value);

        return r.ErrorCode switch
        {
            RegionalPremiumErrorCode.AlreadyExists => Conflict(Problem(r)),
            RegionalPremiumErrorCode.NotFound => NotFound(Problem(r)),
            RegionalPremiumErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpPut("api/projects/{projectId:guid}/regional-premiums/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid projectId, Guid id, [FromBody] UpdateRegionalPremiumRequest req, CancellationToken ct)
    {
        var r = await _service.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : r.ErrorCode == RegionalPremiumErrorCode.NotFound
            ? NotFound(Problem(r))
            : BadRequest(Problem(r));
    }

    [HttpDelete("api/projects/{projectId:guid}/regional-premiums/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid projectId, Guid id, CancellationToken ct)
    {
        var r = await _service.DeleteAsync(UserId, id, ct);
        return r.Succeeded ? NoContent()
            : r.ErrorCode == RegionalPremiumErrorCode.NotFound
                ? NotFound(Problem(r))
                : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(RegionalPremiumResult<T> r) => new()
    {
        Title = "RegionalPremium Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
