using System.Security.Claims;
using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteProjects)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;
    private readonly IProjectPnLService _pnl; // Sprint 57 / DEC-161
    private readonly IContractService _contracts; // Sprint 58 / DEC-163
    private readonly IBillingService _billings; // Sprint 58 / DEC-164
    private readonly ITaskService _tasks;
    private readonly IBudgetService _budgets;
    private readonly IResourceAssignmentService _assignments;
    private readonly IValidator<CreateProjectRequest> _createV;
    private readonly IValidator<UpdateProjectRequest> _updateV;

    public ProjectsController(
        IProjectService projects,
        IProjectPnLService pnl,
        IContractService contracts,
        IBillingService billings,
        ITaskService tasks,
        IBudgetService budgets,
        IResourceAssignmentService assignments,
        IValidator<CreateProjectRequest> createV,
        IValidator<UpdateProjectRequest> updateV)
    {
        _projects = projects; _pnl = pnl; _contracts = contracts; _billings = billings;
        _tasks = tasks; _budgets = budgets; _assignments = assignments;
        _createV = createV; _updateV = updateV;
    }

    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? companyId,
        [FromQuery] ProjectStatus? status,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var r = await _projects.ListAsync(companyId, status, includeInactive, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _projects.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var v = await _createV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _projects.CreateAsync(UserId, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var v = await _updateV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _projects.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest req, CancellationToken ct)
    {
        var r = await _projects.ChangeStatusAsync(UserId, id, req.Status, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var r = await _projects.DeactivateAsync(UserId, id, ct);
        return r.Succeeded ? NoContent() : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}/tasks")]
    public async Task<IActionResult> GetTasks(Guid id, CancellationToken ct)
    {
        var r = await _tasks.ListByProjectAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("{id:guid}/budget")]
    public async Task<IActionResult> GetBudget(Guid id, CancellationToken ct)
    {
        var r = await _budgets.GetByProjectAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    /// <summary>الأرباح والخسائر حسب المشروع (Sprint 57 / DEC-161). يقبل نطاق زمني اختياري.</summary>
    [HttpGet("{id:guid}/pnl")]
    public async Task<IActionResult> GetPnL(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var r = await _pnl.GetPnLAsync(id, from, to, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    // ===== Sprint 58 / DEC-163: Project Contract endpoints =====

    /// <summary>جلب العقد المرتبط بالمشروع (عقد واحد فقط لكل مشروع).</summary>
    [HttpGet("{id:guid}/contract")]
    public async Task<IActionResult> GetContract(Guid id, CancellationToken ct)
    {
        var r = await _contracts.GetByProjectAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("{id:guid}/contract")]
    public async Task<IActionResult> CreateContract(Guid id, [FromBody] CreateContractRequest req, CancellationToken ct)
    {
        var r = await _contracts.CreateAsync(UserId, id, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetContract), new { id = id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPut("/api/contracts/{contractId:guid}")]
    public async Task<IActionResult> UpdateContract(Guid contractId, [FromBody] UpdateContractRequest req, CancellationToken ct)
    {
        var r = await _contracts.UpdateAsync(UserId, contractId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpDelete("/api/contracts/{contractId:guid}")]
    public async Task<IActionResult> DeleteContract(Guid contractId, CancellationToken ct)
    {
        var r = await _contracts.DeleteAsync(UserId, contractId, ct);
        return r.Succeeded ? NoContent() : BadRequest(Problem(r));
    }

    // ===== Sprint 58 / DEC-164: Progress Billing endpoints =====

    [HttpGet("{id:guid}/billings")]
    public async Task<IActionResult> GetBillings(Guid id, CancellationToken ct)
    {
        var r = await _billings.ListByProjectAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPost("{id:guid}/billings")]
    public async Task<IActionResult> CreateBilling(Guid id, [FromBody] CreateBillingRequest req, CancellationToken ct)
    {
        var r = await _billings.CreateAsync(UserId, id, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetBillings), new { id = id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpGet("/api/billings/{billingId:guid}")]
    public async Task<IActionResult> GetBilling(Guid billingId, CancellationToken ct)
    {
        var r = await _billings.GetByIdAsync(billingId, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    /// <summary>معاينة المستخلص قبل الإنشاء — يحسب الأرقام المتوقعة (live preview في الـ UI).</summary>
    [HttpGet("/api/contracts/{contractId:guid}/billing-preview")]
    public async Task<IActionResult> PreviewBilling(Guid contractId, [FromQuery] decimal percent, CancellationToken ct)
    {
        var r = await _billings.PreviewAsync(contractId, percent, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    /// <summary>ترحيل المستخلص (DRAFT → INVOICED). عملية ذرية: ينشئ sales_invoice + journal_entry ويحدّث الـ billing.</summary>
    [HttpPost("/api/billings/{billingId:guid}/approve")]
    public async Task<IActionResult> ApproveBilling(Guid billingId, CancellationToken ct)
    {
        var r = await _billings.ApproveAsync(UserId, billingId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>إلغاء مسودة المستخلص (DRAFT → CANCELLED).</summary>
    [HttpPost("/api/billings/{billingId:guid}/cancel")]
    public async Task<IActionResult> CancelBilling(Guid billingId, CancellationToken ct)
    {
        var r = await _billings.CancelAsync(UserId, billingId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    /// <summary>WIP (Work in Progress) — التكاليف الجارية غير المفوترة (Sprint 58 / DEC-165).</summary>
    [HttpGet("{id:guid}/wip")]
    public async Task<IActionResult> GetWip(Guid id, CancellationToken ct)
    {
        var r = await _billings.GetWipAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("{id:guid}/budget/recalculate")]
    public async Task<IActionResult> RecalculateBudget(Guid id, CancellationToken ct)
    {
        var r = await _budgets.RecalculateSpentAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpGet("{id:guid}/assignments")]
    public async Task<IActionResult> GetAssignments(Guid id, CancellationToken ct)
    {
        var r = await _assignments.ListByProjectAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPost("{id:guid}/assignments")]
    public async Task<IActionResult> AddAssignment(Guid id, [FromBody] CreateAssignmentRequest req, CancellationToken ct)
    {
        req.ProjectId = id; // override from route
        var r = await _assignments.CreateAsync(req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetAssignments), new { id = r.Value!.ProjectId }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpDelete("{id:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> RemoveAssignment(Guid id, Guid assignmentId, CancellationToken ct)
    {
        var r = await _assignments.DeleteAsync(assignmentId, ct);
        return r.Succeeded ? NoContent() : BadRequest(Problem(r));
    }

    private static ValidationProblemDetails ValidationProblem(FluentValidation.Results.ValidationResult v) =>
        new(v.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
    private static ProblemDetails Problem<T>(ProjectResult<T> r) => new()
    {
        Title = "Project Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}

public sealed class ChangeStatusRequest
{
    public ProjectStatus Status { get; set; }
}
