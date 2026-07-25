using System.Data;
using ERPSystem.Modules.Companies.Entities;

namespace ERPSystem.Modules.Companies.Infrastructure;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Company?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Company>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<IReadOnlyList<Company>> ListSubsidiariesAsync(Guid parentCompanyId, CancellationToken ct);
    // Phase 6.0b (P6-0b): scope-less lookup — the default Holding in the new
    // Multi-Company schema is global (no per-user isolation). Returns the id
    // of the row where is_group = true AND parent_company_id IS NULL AND code = '000'
    // (the seed holding), or null if none exists. Used by DefaultHoldingBootstrapHostedService
    // at app startup for the idempotency check.
    Task<Guid?> GetHoldingCompanyIdAsync(CancellationToken ct);
    Task InsertAsync(Company company, CancellationToken ct);
    Task InsertAsync(Company company, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task UpdateAsync(Company company, CancellationToken ct);
}

public interface ICostCenterRepository
{
    Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CostCenter?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<CostCenter>> ListAsync(Guid? companyId, CostCenterType? type, bool includeInactive, CancellationToken ct);
    Task<IReadOnlyList<CostCenter>> ListChildrenAsync(Guid parentId, CancellationToken ct);
    Task InsertAsync(CostCenter cc, CancellationToken ct);
    Task UpdateAsync(CostCenter cc, CancellationToken ct);
}
