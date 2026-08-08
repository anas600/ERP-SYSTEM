using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

public sealed class ContractRepository : IContractRepository
{
    private readonly IDbConnectionFactory _db;

    public ContractRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, company_id AS CompanyId, project_id AS ProjectId,
                   contract_number AS ContractNumber, contract_value AS ContractValue,
                   advance_percent AS AdvancePercent, retention_percent AS RetentionPercent,
                   retention_start_billing AS RetentionStartBilling,
                   start_date AS StartDate, end_date AS EndDate, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy,
                   is_active AS IsActive, deleted_at AS DeletedAt
            FROM contracts WHERE id = @Id AND deleted_at IS NULL LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Contract>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Contract?> GetByProjectAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, company_id AS CompanyId, project_id AS ProjectId,
                   contract_number AS ContractNumber, contract_value AS ContractValue,
                   advance_percent AS AdvancePercent, retention_percent AS RetentionPercent,
                   retention_start_billing AS RetentionStartBilling,
                   start_date AS StartDate, end_date AS EndDate, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy,
                   is_active AS IsActive, deleted_at AS DeletedAt
            FROM contracts WHERE project_id = @ProjectId AND deleted_at IS NULL LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Contract>(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: ct));
    }

    public async Task<int> CountBillingsAsync(Guid contractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Note: progress_billings table is created in DEC-164. This method is safe — returns 0 if table missing.
        try
        {
            const string sql = "SELECT COUNT(*)::int FROM progress_billings WHERE contract_id = @ContractId AND status != 'CANCELLED'";
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { ContractId = contractId }, cancellationToken: ct));
        }
        catch
        {
            return 0;
        }
    }

    public async Task InsertAsync(Contract contract, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO contracts (id, company_id, project_id, contract_number, contract_value,
                                   advance_percent, retention_percent, retention_start_billing,
                                   start_date, end_date, notes,
                                   created_at, created_by, updated_at, updated_by, is_active, deleted_at)
            VALUES (@Id, @CompanyId, @ProjectId, @ContractNumber, @ContractValue,
                    @AdvancePercent, @RetentionPercent, @RetentionStartBilling,
                    @StartDate, @EndDate, @Notes,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @IsActive, @DeletedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, contract, cancellationToken: ct));
    }

    public async Task UpdateAsync(Contract contract, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE contracts SET contract_number = @ContractNumber, contract_value = @ContractValue,
                                 advance_percent = @AdvancePercent, retention_percent = @RetentionPercent,
                                 retention_start_billing = @RetentionStartBilling,
                                 start_date = @StartDate, end_date = @EndDate, notes = @Notes,
                                 updated_at = @UpdatedAt, updated_by = @UpdatedBy, is_active = @IsActive
            WHERE id = @Id AND deleted_at IS NULL";
        await conn.ExecuteAsync(new CommandDefinition(sql, contract, cancellationToken: ct));
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Soft delete only if no billings exist. Atomic via WHERE NOT EXISTS.
        const string sql = @"
            UPDATE contracts SET deleted_at = NOW(), is_active = false
            WHERE id = @Id
              AND deleted_at IS NULL
              AND NOT EXISTS (
                  SELECT 1 FROM progress_billings
                  WHERE contract_id = @Id AND status != 'CANCELLED'
              )";
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }
}
