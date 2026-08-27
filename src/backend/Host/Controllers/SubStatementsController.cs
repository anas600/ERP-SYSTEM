using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 64 (DEC-225) — Sub-Statement REST API.
///
/// <para>Routes (2 endpoints):</para>
/// <list type="bullet">
///   <item>GET /api/sub-contracts/{subContractId}/statement                          — full P&amp;L per sub-contract</item>
///   <item>GET /api/subcontractors/{subcontractorId}/projects/{projectId}/summary     — aggregated summary</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: companyId comes from <c>ICompanyContext</c>, never
/// from the request DTO. The service additionally validates that the loaded
/// sub-contract / subcontractor / project all belong to the active company.</para>
///
/// <para><b>Sprint 63 RBAC (DEC-215, DEC-216)</b>: the class-level
/// <see cref="RequirePermissionAttribute"/> uses the same temporary marker as
/// the other Sprint 64 controllers. When Sprint 63 merges, the attribute is
/// upgraded to the full <c>IAsyncAuthorizationFilter</c> — no controller
/// change required post-merge.</para>
/// </summary>
[ApiController]
[Authorize]
[RequirePermission("projects.sub_statements.view")]
public sealed class SubStatementsController : ControllerBase
{
    private readonly ISubStatementService _service;

    public SubStatementsController(ISubStatementService service)
    {
        _service = service;
    }

    [HttpGet("api/sub-contracts/{subContractId:guid}/statement")]
    [RequirePermission("projects.sub_statements.view")]
    public async Task<IActionResult> GetBySubContract(
        Guid subContractId, CancellationToken ct)
    {
        var r = await _service.GetBySubContractAsync(subContractId, ct);
        if (r.Succeeded)
            return Ok(r.Value);

        return r.ErrorCode switch
        {
            SubStatementErrorCode.NotFound => NotFound(Problem(r)),
            SubStatementErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    [HttpGet("api/subcontractors/{subcontractorId:guid}/projects/{projectId:guid}/summary")]
    [RequirePermission("projects.sub_statements.view")]
    public async Task<IActionResult> GetBySubcontractorAndProject(
        Guid subcontractorId, Guid projectId, CancellationToken ct)
    {
        var r = await _service.GetBySubcontractorAndProjectAsync(subcontractorId, projectId, ct);
        if (r.Succeeded)
            return Ok(r.Value);

        return r.ErrorCode switch
        {
            SubStatementErrorCode.NotFound => NotFound(Problem(r)),
            SubStatementErrorCode.ValidationError => BadRequest(Problem(r)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, Problem(r)),
        };
    }

    private static ProblemDetails Problem<T>(SubStatementResult<T> r) => new()
    {
        Title = "SubStatement Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
