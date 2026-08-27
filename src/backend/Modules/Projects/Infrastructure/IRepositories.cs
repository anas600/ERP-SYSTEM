using ERPSystem.Modules.Projects.Entities;

namespace ERPSystem.Modules.Projects.Infrastructure;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Project?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Project>> ListAsync(Guid? companyId, ProjectStatus? status, bool includeInactive, int skip, int take, CancellationToken ct);
    Task InsertAsync(Project project, CancellationToken ct);
    Task UpdateAsync(Project project, CancellationToken ct);
}

public interface ITaskRepository
{
    Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ProjectTask>> ListByProjectAsync(Guid projectId, CancellationToken ct);
    Task InsertAsync(ProjectTask task, CancellationToken ct);
    Task UpdateAsync(ProjectTask task, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Resource?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Resource>> ListAsync(bool includeInactive, CancellationToken ct);
    Task InsertAsync(Resource resource, CancellationToken ct);
    Task UpdateAsync(Resource resource, CancellationToken ct);
}

public interface IProjectBudgetRepository
{
    Task<ProjectBudget?> GetByProjectAsync(Guid projectId, CancellationToken ct);
    Task<ProjectBudget?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(ProjectBudget budget, CancellationToken ct);
    Task UpdateAsync(ProjectBudget budget, CancellationToken ct);
    /// <summary>إعادة حساب SpentAmount من journal_lines (JOIN على cost_center_id)</summary>
    Task<decimal> RecalculateSpentAsync(Guid projectId, Guid costCenterId, CancellationToken ct);
}

public interface IResourceAssignmentRepository
{
    Task<ResourceAssignment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ResourceAssignment>> ListByProjectAsync(Guid projectId, CancellationToken ct);
    Task InsertAsync(ResourceAssignment assignment, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

/// <summary>Sprint 58 / DEC-163: Project Contract (one per project).</summary>
public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Contract?> GetByProjectAsync(Guid projectId, CancellationToken ct);
    Task<int> CountBillingsAsync(Guid contractId, CancellationToken ct);
    Task InsertAsync(Contract contract, CancellationToken ct);
    Task UpdateAsync(Contract contract, CancellationToken ct);
    /// <summary>Soft delete — يرجع true لو فعلاً اتحذف، false لو عنده billings.</summary>
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct);
}

/// <summary>Sprint 58 / DEC-164: Progress Billing.</summary>
public interface IBillingRepository
{
    Task<ProgressBilling?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ProgressBilling>> ListByProjectAsync(Guid projectId, CancellationToken ct);
    Task<bool> BillingNumberExistsAsync(string billingNumber, Guid companyId, CancellationToken ct);
    Task<decimal> SumAdvanceDeductedAsync(Guid contractId, CancellationToken ct);
    Task<int> CountNonCancelledAsync(Guid contractId, CancellationToken ct);
    Task<decimal> MaxPercentAsync(Guid contractId, CancellationToken ct);
    Task InsertAsync(ProgressBilling billing, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, BillingStatus status, Guid? invoiceId, Guid? journalEntryId, Guid updatedBy, CancellationToken ct);
}

// =============================================================================
// Sprint 61 (DEC-192, DEC-193, DEC-194) — Engineer's Daily Report repositories.
// =============================================================================

/// <summary>Sprint 61 / DEC-192 — EngineerReport persistence.</summary>
public interface IEngineerReportRepository
{
    Task<EngineerReport?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<EngineerReport>> ListByProjectAsync(
        Guid projectId, Guid companyId, DateTime? from, DateTime? to,
        EngineerReportStatus? status, int skip, int take, CancellationToken ct);
    Task<int> CountByProjectAndDateAsync(Guid projectId, DateTime reportDate, CancellationToken ct);
    Task InsertAsync(EngineerReport report, CancellationToken ct);
    Task UpdateAsync(EngineerReport report, CancellationToken ct);
}

/// <summary>Sprint 61 / DEC-193 — EngineerReportPhoto persistence.</summary>
public interface IEngineerReportPhotoRepository
{
    Task<EngineerReportPhoto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<EngineerReportPhoto>> ListByReportAsync(Guid reportId, CancellationToken ct);
    Task<int> CountByReportAsync(Guid reportId, CancellationToken ct);
    Task InsertAsync(EngineerReportPhoto photo, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

/// <summary>Sprint 61 / DEC-194 — EngineerReportSignoff persistence.</summary>
public interface IEngineerReportSignoffRepository
{
    Task<EngineerReportSignoff?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<EngineerReportSignoff>> ListByReportAsync(Guid reportId, CancellationToken ct);
    Task InsertAsync(EngineerReportSignoff signoff, CancellationToken ct);
}
