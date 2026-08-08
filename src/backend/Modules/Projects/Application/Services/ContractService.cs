using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IContractService
{
    Task<ProjectResult<ContractResponse>> GetByProjectAsync(Guid projectId, CancellationToken ct);
    Task<ProjectResult<ContractResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ProjectResult<ContractResponse>> CreateAsync(Guid userId, Guid projectId, CreateContractRequest req, CancellationToken ct);
    Task<ProjectResult<ContractResponse>> UpdateAsync(Guid userId, Guid id, UpdateContractRequest req, CancellationToken ct);
    Task<ProjectResult<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
}

/// <summary>
/// Sprint 58 / DEC-163: Contract service — CRUD for project contracts.
///
/// قاعدة UNIQUE (company_id, project_id) تضمن عقد واحد لكل مشروع.
/// Soft-delete فقط (deleted_at) — ولا يُحذف لو عنده billings (DEC-164).
/// </summary>
public sealed class ContractService : IContractService
{
    private readonly IContractRepository _contracts;
    private readonly IProjectRepository _projects;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<ContractService> _logger;

    public ContractService(
        IContractRepository contracts,
        IProjectRepository projects,
        ICompanyContext companyContext,
        ILogger<ContractService> logger)
    {
        _contracts = contracts; _projects = projects; _companyContext = companyContext; _logger = logger;
    }

    public async Task<ProjectResult<ContractResponse>> GetByProjectAsync(Guid projectId, CancellationToken ct)
    {
        var c = await _contracts.GetByProjectAsync(projectId, ct);
        if (c == null)
            return ProjectResult<ContractResponse>.Fail("لا يوجد عقد لهذا المشروع.", ProjectErrorCode.NotFound);
        return ProjectResult<ContractResponse>.Ok(MapToResponse(c));
    }

    public async Task<ProjectResult<ContractResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var c = await _contracts.GetByIdAsync(id, ct);
        if (c == null)
            return ProjectResult<ContractResponse>.Fail("العقد غير موجود.", ProjectErrorCode.NotFound);
        return ProjectResult<ContractResponse>.Ok(MapToResponse(c));
    }

    public async Task<ProjectResult<ContractResponse>> CreateAsync(Guid userId, Guid projectId, CreateContractRequest req, CancellationToken ct)
    {
        // 0) company_id من السياق
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        // 1) المشروع لازم يكون موجود ونفس الـ company
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return ProjectResult<ContractResponse>.Fail("المشروع غير موجود.", ProjectErrorCode.NotFound);
        if (project.CompanyId != companyId)
            return ProjectResult<ContractResponse>.Fail("المشروع لا ينتمي لشركتك.", ProjectErrorCode.ValidationError);

        // 2) validation للحقول
        if (req.ContractValue <= 0)
            return ProjectResult<ContractResponse>.Fail("قيمة العقد يجب أن تكون أكبر من صفر.", ProjectErrorCode.ValidationError);
        if (req.AdvancePercent < 0 || req.AdvancePercent > 100)
            return ProjectResult<ContractResponse>.Fail("نسبة الدفعة المقدمة يجب أن تكون بين 0 و 100.", ProjectErrorCode.ValidationError);
        if (req.RetentionPercent < 0 || req.RetentionPercent > 100)
            return ProjectResult<ContractResponse>.Fail("نسبة الاحتجاز يجب أن تكون بين 0 و 100.", ProjectErrorCode.ValidationError);
        if (req.RetentionStartBilling < 1)
            return ProjectResult<ContractResponse>.Fail("رقم مستخلص بداية الاحتجاز يجب أن يكون >= 1.", ProjectErrorCode.ValidationError);

        // 3) لا يوجد عقد سابق على هذا المشروع
        var existing = await _contracts.GetByProjectAsync(projectId, ct);
        if (existing != null)
            return ProjectResult<ContractResponse>.Fail("يوجد عقد سابق على هذا المشروع. استخدم PUT للتحديث.", ProjectErrorCode.AlreadyExists);

        // 4) إنشاء العقد
        var now = DateTime.UtcNow;
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = projectId,
            ContractNumber = req.ContractNumber?.Trim(),
            ContractValue = req.ContractValue,
            AdvancePercent = req.AdvancePercent,
            RetentionPercent = req.RetentionPercent,
            RetentionStartBilling = req.RetentionStartBilling,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Notes = req.Notes?.Trim(),
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId,
            IsActive = true
        };
        await _contracts.InsertAsync(contract, ct);
        _logger.LogInformation("تم إنشاء عقد جديد للمشروع {ProjectId}: قيمة={Value}, advance={Adv}%, retention={Ret}%",
            projectId, contract.ContractValue, contract.AdvancePercent, contract.RetentionPercent);
        return ProjectResult<ContractResponse>.Ok(MapToResponse(contract));
    }

    public async Task<ProjectResult<ContractResponse>> UpdateAsync(Guid userId, Guid id, UpdateContractRequest req, CancellationToken ct)
    {
        var c = await _contracts.GetByIdAsync(id, ct);
        if (c == null)
            return ProjectResult<ContractResponse>.Fail("العقد غير موجود.", ProjectErrorCode.NotFound);

        // لا نسمح بتعديل العقد بعد وجود billings (لتجنّب تعقيد الحسابات)
        var billingCount = await _contracts.CountBillingsAsync(id, ct);
        if (billingCount > 0)
            return ProjectResult<ContractResponse>.Fail("لا يمكن تعديل العقد بعد وجود مستخلصات. ألغِ المستخلصات أولاً.", ProjectErrorCode.ValidationError);

        if (req.ContractValue <= 0)
            return ProjectResult<ContractResponse>.Fail("قيمة العقد يجب أن تكون أكبر من صفر.", ProjectErrorCode.ValidationError);
        if (req.AdvancePercent < 0 || req.AdvancePercent > 100)
            return ProjectResult<ContractResponse>.Fail("نسبة الدفعة المقدمة يجب أن تكون بين 0 و 100.", ProjectErrorCode.ValidationError);
        if (req.RetentionPercent < 0 || req.RetentionPercent > 100)
            return ProjectResult<ContractResponse>.Fail("نسبة الاحتجاز يجب أن تكون بين 0 و 100.", ProjectErrorCode.ValidationError);

        c.ContractNumber = req.ContractNumber?.Trim();
        c.ContractValue = req.ContractValue;
        c.AdvancePercent = req.AdvancePercent;
        c.RetentionPercent = req.RetentionPercent;
        c.RetentionStartBilling = req.RetentionStartBilling;
        c.StartDate = req.StartDate;
        c.EndDate = req.EndDate;
        c.Notes = req.Notes?.Trim();
        c.UpdatedAt = DateTime.UtcNow;
        c.UpdatedBy = userId;
        await _contracts.UpdateAsync(c, ct);
        return ProjectResult<ContractResponse>.Ok(MapToResponse(c));
    }

    public async Task<ProjectResult<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var deleted = await _contracts.SoftDeleteAsync(id, ct);
        if (!deleted)
            return ProjectResult<bool>.Fail("لا يمكن الحذف — يوجد مستخلصات على هذا العقد.", ProjectErrorCode.ValidationError);
        _logger.LogInformation("تم حذف العقد {Id} بواسطة {UserId}", id, userId);
        return ProjectResult<bool>.Ok(true);
    }

    private static ContractResponse MapToResponse(Contract c) => new()
    {
        Id = c.Id, CompanyId = c.CompanyId, ProjectId = c.ProjectId,
        ContractNumber = c.ContractNumber, ContractValue = c.ContractValue,
        AdvancePercent = c.AdvancePercent, RetentionPercent = c.RetentionPercent,
        RetentionStartBilling = c.RetentionStartBilling,
        StartDate = c.StartDate, EndDate = c.EndDate, Notes = c.Notes,
        CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt, IsActive = c.IsActive,
    };
}
