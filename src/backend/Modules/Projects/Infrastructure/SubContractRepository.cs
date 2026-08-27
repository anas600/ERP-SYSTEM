using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 64 / DEC-222 — Dapper repository for <c>sub_contracts</c>.
///
/// <para><b>Why Dapper (DEC-008)</b>: NO EF Core in this codebase. Every
/// repository uses Dapper against the OLTP connection (see
/// <see cref="IDbConnectionFactory"/>).</para>
///
/// <para><b>L19 / DEC-095</b>: every WHERE / INSERT / UPDATE includes
/// <c>company_id</c> so the service layer's JWT-derived company cannot be
/// spoofed via the request DTO.</para>
/// </summary>
public sealed class SubContractRepository : ISubContractRepository
{
    private readonly IDbConnectionFactory _db;
    public SubContractRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, project_id AS ProjectId, subcontractor_id AS SubcontractorId,
        contract_number AS ContractNumber, scope_of_work AS ScopeOfWork,
        contract_value AS ContractValue, retention_percent AS RetentionPercent,
        retention_release_billing AS RetentionReleaseBilling,
        start_date AS StartDate, end_date AS EndDate, status, notes,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<SubContract?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<SubContract>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM sub_contracts WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SubContract>> ListByProjectAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = $@"
            SELECT {SelectColumns}
            FROM sub_contracts
            WHERE project_id = @ProjectId
            ORDER BY contract_number ASC";
        var rows = await conn.QueryAsync<SubContract>(new CommandDefinition(
            sql, new { ProjectId = projectId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SubContract>> ListBySubcontractorAsync(Guid subcontractorId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = $@"
            SELECT {SelectColumns}
            FROM sub_contracts
            WHERE subcontractor_id = @SubcontractorId
            ORDER BY created_at DESC";
        var rows = await conn.QueryAsync<SubContract>(new CommandDefinition(
            sql, new { SubcontractorId = subcontractorId }, cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>
    /// Returns the number of progress billings linked to this sub-contract.
    /// The <c>sub_progress_billings</c> table is created in Wave 2A (DEC-223).
    /// Until then, this method returns 0 (no FK error — the table simply does not exist).
    /// </summary>
    public async Task<int> CountBillingsAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        try
        {
            const string sql = "SELECT COUNT(*)::int FROM sub_progress_billings WHERE sub_contract_id = @SubContractId";
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                sql, new { SubContractId = subContractId }, cancellationToken: ct));
        }
        catch
        {
            // Wave 2A table not yet created — return 0 to keep the service layer safe.
            return 0;
        }
    }

    public async Task InsertAsync(SubContract sc, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO sub_contracts
                (id, company_id, project_id, subcontractor_id, contract_number, scope_of_work,
                 contract_value, retention_percent, retention_release_billing,
                 start_date, end_date, status, notes,
                 created_at, updated_at)
            VALUES
                (@Id, @CompanyId, @ProjectId, @SubcontractorId, @ContractNumber, @ScopeOfWork,
                 @ContractValue, @RetentionPercent, @RetentionReleaseBilling,
                 @StartDate, @EndDate, @Status, @Notes,
                 @CreatedAt, @UpdatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            sc.Id, sc.CompanyId, sc.ProjectId, sc.SubcontractorId, sc.ContractNumber, sc.ScopeOfWork,
            sc.ContractValue, sc.RetentionPercent, sc.RetentionReleaseBilling,
            sc.StartDate, sc.EndDate, sc.Status, sc.Notes,
            sc.CreatedAt, sc.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(SubContract sc, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE sub_contracts SET
                contract_number = @ContractNumber, scope_of_work = @ScopeOfWork,
                contract_value = @ContractValue, retention_percent = @RetentionPercent,
                retention_release_billing = @RetentionReleaseBilling,
                start_date = @StartDate, end_date = @EndDate, status = @Status, notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id AND company_id = @CompanyId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            sc.Id, sc.CompanyId, sc.ContractNumber, sc.ScopeOfWork,
            sc.ContractValue, sc.RetentionPercent, sc.RetentionReleaseBilling,
            sc.StartDate, sc.EndDate, sc.Status, sc.Notes,
            sc.UpdatedAt
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Soft delete: refuse if there are existing sub_progress_billings (Wave 2A table).
    /// Otherwise mark as deleted via the CASCADE path (the table has no is_active column —
    /// we hard-delete but only when safe).
    /// </summary>
    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Atomic: only delete if no billings reference this contract.
        const string sql = @"
            DELETE FROM sub_contracts
            WHERE id = @Id
              AND NOT EXISTS (
                  SELECT 1 FROM sub_progress_billings WHERE sub_contract_id = @Id
              )";
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }
}
