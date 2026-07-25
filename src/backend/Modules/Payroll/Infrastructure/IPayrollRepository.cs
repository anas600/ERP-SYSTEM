using System;
using System.Collections.Generic;
using ERPSystem.Modules.Payroll.Domain.Entities;

namespace ERPSystem.Modules.Payroll.Infrastructure;

/// <summary>
/// عقد المستودع لهيكل الرواتب (SalaryStructure + Lines).
/// Phase 6.1b: لا يوجد عزل بين المستخدمين — الهياكل مشتركة.
/// </summary>
public interface ISalaryStructureRepository
{
    Task<SalaryStructure?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<SalaryStructure?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<SalaryStructure>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<IReadOnlyList<SalaryStructureLine>> GetLinesAsync(Guid salaryStructureId, CancellationToken ct);
    Task InsertAsync(SalaryStructure structure, IEnumerable<SalaryStructureLine> lines, CancellationToken ct);
}

/// <summary>
/// عقد المستودع لدورة الرواتب (PayrollRun aggregate).
/// </summary>
public interface IPayrollRepository
{
    // ============ PayrollRun ============
    Task<PayrollRun?> GetRunByIdAsync(Guid id, CancellationToken ct);
    Task<PayrollRun?> GetRunByIdForTenantAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PayrollRun>> ListRunsAsync(PayrollRunStatus? status, int skip, int take, CancellationToken ct);
    Task InsertRunAsync(PayrollRun run, CancellationToken ct);
    Task UpdateRunAsync(PayrollRun run, CancellationToken ct);

    // ============ PayrollItem ============
    Task<PayrollItem?> GetItemByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PayrollItem>> GetItemsByRunAsync(Guid payrollRunId, CancellationToken ct);
    Task AddItemAsync(PayrollItem item, IEnumerable<PayslipComponent> components, CancellationToken ct);

    // ============ PayslipComponent ============
    Task<IReadOnlyList<PayslipComponent>> GetComponentsByItemAsync(Guid payrollItemId, CancellationToken ct);

    // ============ SalaryStructure (passthrough for PayrollService) ============
    Task<SalaryStructure?> GetStructureByCodeAsync(string code, CancellationToken ct);
}