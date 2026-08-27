using System.Security.Claims;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 61 (DEC-192, DEC-193, DEC-194) — Engineer Report REST API.
///
/// <para>Routes (8 total; 7 live here + 1 in <see cref="EngineerReportPhotosController"/>):</para>
/// <list type="bullet">
///   <item>GET    /api/projects/{id}/engineer-reports — list (with from/to/status filters)</item>
///   <item>GET    /api/engineer-reports/{id} — details</item>
///   <item>POST   /api/projects/{id}/engineer-reports — create Draft</item>
///   <item>PUT    /api/engineer-reports/{id} — update Draft only</item>
///   <item>POST   /api/engineer-reports/{id}/submit — Draft → Submitted</item>
///   <item>GET    /api/engineer-reports/{id}/photos — list photos</item>
///   <item>POST   /api/engineer-reports/{id}/signoff — Approve / Reject (PM/Client)</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: companyId always comes from <c>ICompanyContext</c>, never
/// from the request DTO.</para>
/// </summary>
[ApiController]
[Authorize]
public sealed class EngineerReportsController : ControllerBase
{
    private readonly IEngineerReportService _service;

    public EngineerReportsController(IEngineerReportService service)
    {
        _service = service;
    }

    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [HttpGet("api/projects/{projectId:guid}/engineer-reports")]
    public async Task<IActionResult> ListByProject(
        Guid projectId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        EngineerReportStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EngineerReportStatus>(status, ignoreCase: true, out var parsed))
                return BadRequest($"Invalid status: {status}. Valid: Draft, Submitted, Approved, Rejected.");
            statusEnum = parsed;
        }
        var r = await _service.ListByProjectAsync(projectId, from, to, statusEnum, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/engineer-reports/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _service.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/projects/{projectId:guid}/engineer-reports")]
    public async Task<IActionResult> Create(
        Guid projectId, [FromBody] CreateEngineerReportRequest req, CancellationToken ct)
    {
        var r = await _service.CreateAsync(UserId, projectId, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : r.ErrorCode == EngineerReportErrorCode.AlreadyExists
                ? Conflict(Problem(r))
                : BadRequest(Problem(r));
    }

    [HttpPut("api/engineer-reports/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateEngineerReportRequest req, CancellationToken ct)
    {
        var r = await _service.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPost("api/engineer-reports/{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var r = await _service.SubmitAsync(UserId, id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/engineer-reports/{id:guid}/photos")]
    public async Task<IActionResult> ListPhotos(Guid id, CancellationToken ct)
    {
        var r = await _service.ListPhotosAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/engineer-reports/{id:guid}/signoff")]
    public async Task<IActionResult> Signoff(
        Guid id, [FromBody] SignoffRequest req, CancellationToken ct)
    {
        var r = await _service.SignoffAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    private static ProblemDetails Problem<T>(EngineerReportResult<T> r) => new()
    {
        Title = "EngineerReport Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
