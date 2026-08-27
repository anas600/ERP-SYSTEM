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

// =============================================================================
// Sprint 62 (DEC-197) — Regional Premium repository.
// Used by the RegionalPremiumService for CRUD + the BillingService for
// calculation lookups (CalculateDeductionAsync).
// =============================================================================

/// <summary>Sprint 62 / DEC-197 — Regional Premium persistence.</summary>
public interface IRegionalPremiumRepository
{
    Task<RegionalPremium?> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>List all premiums (active + inactive) for a project. The service filters
    /// on <c>IsActive</c> as part of the calculation logic.</summary>
    Task<IReadOnlyList<RegionalPremium>> ListByProjectAsync(Guid projectId, CancellationToken ct);
    Task InsertAsync(RegionalPremium premium, CancellationToken ct);
    Task UpdateAsync(RegionalPremium premium, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

/// <summary>Sprint 64 / DEC-223: Sub-ProgressBilling (work done, % complete, retention).</summary>
public interface ISubProgressBillingRepository
{
    Task<SubProgressBilling?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<SubProgressBilling>> ListBySubContractAsync(Guid subContractId, CancellationToken ct);
    /// <summary>Count of billings for this sub-contract (any status). Used to compute the retention-billing ordinal.</summary>
    Task<int> CountBySubContractAsync(Guid subContractId, CancellationToken ct);
    /// <summary>Sum of gross_amount for this sub-contract (across all statuses). Used to populate PreviousBillingsAmount.</summary>
    Task<decimal> SumBySubContractAsync(Guid subContractId, CancellationToken ct);
    /// <summary>Sum of gross_amount for this sub-contract EXCLUDING status=4 (Cancelled). Used by SubPaymentService.GetBalanceAsync.</summary>
    Task<decimal> SumGrossNonCancelledBySubContractAsync(Guid subContractId, CancellationToken ct);
    /// <summary>Sum of retention_deducted for this sub-contract EXCLUDING status=4 (Cancelled). Used by SubPaymentService.GetBalanceAsync.</summary>
    Task<decimal> SumRetentionNonCancelledBySubContractAsync(Guid subContractId, CancellationToken ct);
    Task InsertAsync(SubProgressBilling billing, CancellationToken ct);
    Task UpdateAsync(SubProgressBilling billing, CancellationToken ct);
    /// <summary>Update only the status field (used by ApproveAsync).</summary>
    Task UpdateStatusAsync(Guid id, int status, DateTime updatedAt, CancellationToken ct);
}

/// <summary>Sprint 64 / DEC-224: Sub-Payment (allocation + retention release).</summary>
public interface ISubPaymentRepository
{
    Task<SubPayment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<SubPayment>> ListBySubContractAsync(Guid subContractId, CancellationToken ct);
    Task<IReadOnlyList<SubPayment>> ListBySubProgressBillingAsync(Guid subProgressBillingId, CancellationToken ct);
    /// <summary>Sum of amount + retention_released for this sub-contract. Used by SubPaymentService.GetBalanceAsync.</summary>
    Task<decimal> SumPaidBySubContractAsync(Guid subContractId, CancellationToken ct);
    /// <summary>Sum of retention_released only — used to validate the ReleaseRetentionAsync cap (cannot release more than already released).</summary>
    Task<decimal> SumRetentionReleasedBySubContractAsync(Guid subContractId, CancellationToken ct);
    Task InsertAsync(SubPayment payment, CancellationToken ct);
}

/// <summary>Sprint 64 / DEC-221: Subcontractor master (code, name, contact, trade, tax_id).</summary>
public interface ISubcontractorRepository
{
    Task<Subcontractor?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Subcontractor?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct);
    Task<IReadOnlyList<Subcontractor>> ListAsync(
        Guid companyId, bool? isActive, string? tradeSpecialty, int skip, int take, CancellationToken ct);
    Task InsertAsync(Subcontractor sub, CancellationToken ct);
    Task UpdateAsync(Subcontractor sub, CancellationToken ct);
    /// <summary>Soft delete — sets is_active = FALSE. Returns true if a row was actually updated.</summary>
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct);
}

/// <summary>Sprint 64 / DEC-222: Sub-Contract (subcontractor ↔ project + scope + value + retention).</summary>
public interface ISubContractRepository
{
    Task<SubContract?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<SubContract>> ListByProjectAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<SubContract>> ListBySubcontractorAsync(Guid subcontractorId, CancellationToken ct);
    /// <summary>Count of sub-progress-billings linked to this sub-contract. Returns 0 if the Wave 2A table is missing (defensive).</summary>
    Task<int> CountBillingsAsync(Guid subContractId, CancellationToken ct);
    Task InsertAsync(SubContract sc, CancellationToken ct);
    Task UpdateAsync(SubContract sc, CancellationToken ct);
    /// <summary>Hard delete — refuses (returns false) if any sub_progress_billings reference this contract.</summary>
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct);
}
