using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

// =============================================================================
// Sprint 64 / DEC-222 — Sub-Contract service.
//
// Manages CRUD on sub_contracts. CompanyId always comes from the JWT context
// (L19 / DEC-095), never from the request DTO. The project + subcontractor
// must both exist within the same company (defense in depth).
// =============================================================================

/// <summary>Result envelope for SubContract service calls.</summary>
public sealed class SubContractResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public SubContractErrorCode? ErrorCode { get; init; }
    public static SubContractResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static SubContractResult<T> Fail(string e, SubContractErrorCode c) =>
        new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum SubContractErrorCode
{
    NotFound, AlreadyExists, ValidationError, Internal
}

public interface ISubContractService
{
    Task<SubContractResult<SubContractResponse>> CreateAsync(
        Guid userId, Guid projectId, CreateSubContractRequest req, CancellationToken ct);

    Task<SubContractResult<SubContractResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateSubContractRequest req, CancellationToken ct);

    Task<SubContractResult<SubContractResponse>> GetByIdAsync(
        Guid id, CancellationToken ct);

    Task<SubContractResult<IReadOnlyList<SubContractResponse>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct);

    Task<SubContractResult<bool>> SoftDeleteAsync(
        Guid userId, Guid id, CancellationToken ct);
}

/// <summary>
/// Sprint 64 / DEC-222 — Sub-Contract service.
///
/// <para><b>Validation rules</b>:</para>
/// <list type="bullet">
///   <item>Project + Subcontractor must exist (and belong to the same company).</item>
///   <item>ContractValue must be &gt;= 0.</item>
///   <item>RetentionPercent must be in [0, 100].</item>
///   <item>RetentionReleaseBilling must be &gt;= 1.</item>
///   <item>Status must be 1 (Active), 2 (Completed) or 3 (Cancelled).</item>
///   <item>UNIQUE (project_id, contract_number) — no duplicate sub-contract numbers per project.</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: CompanyId comes from <see cref="ICompanyContext"/>,
/// never from the request DTO. List/get queries are scoped by project (and the
/// caller already knows the project is in their JWT-scoped company).</para>
/// </summary>
public sealed class SubContractService : ISubContractService
{
    private readonly ISubContractRepository _subContracts;
    private readonly IProjectRepository _projects;
    private readonly ISubcontractorRepository _subcontractors;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<SubContractService> _logger;

    public SubContractService(
        ISubContractRepository subContracts,
        IProjectRepository projects,
        ISubcontractorRepository subcontractors,
        ICompanyContext companyContext,
        ILogger<SubContractService> logger)
    {
        _subContracts = subContracts;
        _projects = projects;
        _subcontractors = subcontractors;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<SubContractResult<SubContractResponse>> CreateAsync(
        Guid userId, Guid projectId, CreateSubContractRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        // Validate scope of work + contract number early.
        if (string.IsNullOrWhiteSpace(req.ContractNumber))
            return SubContractResult<SubContractResponse>.Fail(
                "رقم العقد مطلوب.", SubContractErrorCode.ValidationError);
        if (string.IsNullOrWhiteSpace(req.ScopeOfWork))
            return SubContractResult<SubContractResponse>.Fail(
                "نطاق العمل مطلوب.", SubContractErrorCode.ValidationError);

        // Project must exist and belong to the same company.
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return SubContractResult<SubContractResponse>.Fail(
                "المشروع غير موجود.", SubContractErrorCode.NotFound);
        if (project.CompanyId != companyId)
            return SubContractResult<SubContractResponse>.Fail(
                "المشروع لا ينتمي لشركتك.", SubContractErrorCode.ValidationError);

        // Subcontractor must exist and belong to the same company.
        var sub = await _subcontractors.GetByIdAsync(req.SubcontractorId, ct);
        if (sub == null)
            return SubContractResult<SubContractResponse>.Fail(
                "المقاول الباطن غير موجود.", SubContractErrorCode.NotFound);
        if (sub.CompanyId != companyId)
            return SubContractResult<SubContractResponse>.Fail(
                "المقاول الباطن لا ينتمي لشركتك.", SubContractErrorCode.ValidationError);
        if (!sub.IsActive)
            return SubContractResult<SubContractResponse>.Fail(
                "المقاول الباطن معطّل. فعّله أولاً.", SubContractErrorCode.ValidationError);

        // Validate monetary / retention fields.
        if (req.ContractValue < 0)
            return SubContractResult<SubContractResponse>.Fail(
                "قيمة العقد يجب أن تكون >= 0.", SubContractErrorCode.ValidationError);
        if (req.RetentionPercent < 0m || req.RetentionPercent > 100m)
            return SubContractResult<SubContractResponse>.Fail(
                "نسبة الاحتجاز يجب أن تكون بين 0 و 100.", SubContractErrorCode.ValidationError);
        if (req.RetentionReleaseBilling < 1)
            return SubContractResult<SubContractResponse>.Fail(
                "رقم المستخلص الذي يبدأ عنده تحرير الاحتجاز يجب أن يكون >= 1.",
                SubContractErrorCode.ValidationError);

        // UNIQUE (project_id, contract_number) — check first.
        var existing = await _subContracts.ListByProjectAsync(projectId, ct);
        if (existing.Any(sc => string.Equals(sc.ContractNumber, req.ContractNumber.Trim(),
                StringComparison.OrdinalIgnoreCase)))
            return SubContractResult<SubContractResponse>.Fail(
                $"يوجد بالفعل عقد باطن برقم ({req.ContractNumber}) على هذا المشروع.",
                SubContractErrorCode.AlreadyExists);

        var now = DateTime.UtcNow;
        var sc = new SubContract
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = projectId,
            SubcontractorId = req.SubcontractorId,
            ContractNumber = req.ContractNumber.Trim(),
            ScopeOfWork = req.ScopeOfWork.Trim(),
            ContractValue = req.ContractValue,
            RetentionPercent = req.RetentionPercent,
            RetentionReleaseBilling = req.RetentionReleaseBilling,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Status = (int)SubContractStatus.Active,
            Notes = req.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _subContracts.InsertAsync(sc, ct);
        _logger.LogInformation("SubContract created {Id} project={ProjectId} sub={SubId} by {UserId}",
            sc.Id, projectId, req.SubcontractorId, userId);
        return SubContractResult<SubContractResponse>.Ok(MapToResponse(sc));
    }

    public async Task<SubContractResult<SubContractResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateSubContractRequest req, CancellationToken ct)
    {
        var sc = await _subContracts.GetByIdAsync(id, ct);
        if (sc == null)
            return SubContractResult<SubContractResponse>.Fail(
                "العقد الباطن غير موجود.", SubContractErrorCode.NotFound);

        if (string.IsNullOrWhiteSpace(req.ScopeOfWork))
            return SubContractResult<SubContractResponse>.Fail(
                "نطاق العمل مطلوب.", SubContractErrorCode.ValidationError);
        if (req.ContractValue < 0)
            return SubContractResult<SubContractResponse>.Fail(
                "قيمة العقد يجب أن تكون >= 0.", SubContractErrorCode.ValidationError);
        if (req.RetentionPercent < 0m || req.RetentionPercent > 100m)
            return SubContractResult<SubContractResponse>.Fail(
                "نسبة الاحتجاز يجب أن تكون بين 0 و 100.", SubContractErrorCode.ValidationError);
        if (req.RetentionReleaseBilling < 1)
            return SubContractResult<SubContractResponse>.Fail(
                "رقم المستخلص الذي يبدأ عنده تحرير الاحتجاز يجب أن يكون >= 1.",
                SubContractErrorCode.ValidationError);
        if (req.Status < 1 || req.Status > 3)
            return SubContractResult<SubContractResponse>.Fail(
                "حالة العقد يجب أن تكون 1 (نشط) أو 2 (مكتمل) أو 3 (ملغى).",
                SubContractErrorCode.ValidationError);

        sc.ScopeOfWork = req.ScopeOfWork.Trim();
        sc.ContractValue = req.ContractValue;
        sc.RetentionPercent = req.RetentionPercent;
        sc.RetentionReleaseBilling = req.RetentionReleaseBilling;
        sc.StartDate = req.StartDate;
        sc.EndDate = req.EndDate;
        sc.Status = req.Status;
        sc.Notes = req.Notes?.Trim();
        sc.UpdatedAt = DateTime.UtcNow;
        await _subContracts.UpdateAsync(sc, ct);
        _logger.LogInformation("SubContract updated {Id} by {UserId}", sc.Id, userId);
        return SubContractResult<SubContractResponse>.Ok(MapToResponse(sc));
    }

    public async Task<SubContractResult<SubContractResponse>> GetByIdAsync(
        Guid id, CancellationToken ct)
    {
        var sc = await _subContracts.GetByIdAsync(id, ct);
        if (sc == null)
            return SubContractResult<SubContractResponse>.Fail(
                "العقد الباطن غير موجود.", SubContractErrorCode.NotFound);
        return SubContractResult<SubContractResponse>.Ok(MapToResponse(sc));
    }

    public async Task<SubContractResult<IReadOnlyList<SubContractResponse>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct)
    {
        var rows = await _subContracts.ListByProjectAsync(projectId, ct);
        var list = rows.Select(MapToResponse).ToList();
        return SubContractResult<IReadOnlyList<SubContractResponse>>.Ok(list);
    }

    public async Task<SubContractResult<bool>> SoftDeleteAsync(
        Guid userId, Guid id, CancellationToken ct)
    {
        var sc = await _subContracts.GetByIdAsync(id, ct);
        if (sc == null)
            return SubContractResult<bool>.Fail(
                "العقد الباطن غير موجود.", SubContractErrorCode.NotFound);

        var ok = await _subContracts.SoftDeleteAsync(id, ct);
        if (!ok)
            return SubContractResult<bool>.Fail(
                "لا يمكن الحذف — توجد مستخلصات مرتبطة بهذا العقد الباطن.",
                SubContractErrorCode.ValidationError);

        _logger.LogInformation("SubContract soft-deleted {Id} by {UserId}", id, userId);
        return SubContractResult<bool>.Ok(true);
    }

    // ===== Helpers =====

    private static SubContractResponse MapToResponse(SubContract sc) => new(
        sc.Id, sc.CompanyId, sc.ProjectId, sc.SubcontractorId,
        sc.ContractNumber, sc.ScopeOfWork,
        sc.ContractValue, sc.RetentionPercent, sc.RetentionReleaseBilling,
        sc.StartDate, sc.EndDate, sc.Status, StatusName(sc.Status), sc.Notes,
        sc.CreatedAt, sc.UpdatedAt);

    private static string StatusName(int status) => status switch
    {
        1 => "نشط",
        2 => "مكتمل",
        3 => "ملغى",
        _ => "غير معروف",
    };
}
