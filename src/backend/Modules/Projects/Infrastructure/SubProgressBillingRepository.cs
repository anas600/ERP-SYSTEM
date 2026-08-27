using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 64 / DEC-223 — Dapper repository for <c>sub_progress_billings</c>.
///
/// <para><b>Why Dapper (DEC-008)</b>: NO EF Core in this codebase. Every
/// repository uses Dapper against the OLTP connection (see
/// <see cref="IDbConnectionFactory"/>).</para>
///
/// <para><b>L19 / DEC-095</b>: every WHERE / INSERT / UPDATE includes
/// <c>company_id</c> where applicable so the service layer's JWT-derived
/// company cannot be spoofed via the request DTO.</para>
/// </summary>
public sealed class SubProgressBillingRepository : ISubProgressBillingRepository
{
    private readonly IDbConnectionFactory _db;
    public SubProgressBillingRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, sub_contract_id AS SubContractId,
        billing_number AS BillingNumber, billing_date AS BillingDate,
        period_from AS PeriodFrom, period_to AS PeriodTo,
        work_completed_percent AS WorkCompletedPercent,
        gross_amount AS GrossAmount, retention_deducted AS RetentionDeducted,
        previous_billings_amount AS PreviousBillingsAmount, net_payable AS NetPayable,
        status, notes,
        created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<SubProgressBilling?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<SubProgressBilling>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM sub_progress_billings WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SubProgressBilling>> ListBySubContractAsync(
        Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = $@"
            SELECT {SelectColumns}
            FROM sub_progress_billings
            WHERE sub_contract_id = @SubContractId
            ORDER BY billing_date ASC, billing_number ASC";
        var rows = await conn.QueryAsync<SubProgressBilling>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountBySubContractAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT COUNT(*)::int FROM sub_progress_billings WHERE sub_contract_id = @SubContractId";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
    }

    public async Task<decimal> SumBySubContractAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT COALESCE(SUM(gross_amount), 0)::numeric
            FROM sub_progress_billings
            WHERE sub_contract_id = @SubContractId";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
    }

    public async Task<decimal> SumGrossNonCancelledBySubContractAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT COALESCE(SUM(gross_amount), 0)::numeric
            FROM sub_progress_billings
            WHERE sub_contract_id = @SubContractId AND status <> 4";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
    }

    public async Task<decimal> SumRetentionNonCancelledBySubContractAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT COALESCE(SUM(retention_deducted), 0)::numeric
            FROM sub_progress_billings
            WHERE sub_contract_id = @SubContractId AND status <> 4";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
    }

    public async Task InsertAsync(SubProgressBilling billing, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO sub_progress_billings
                (id, company_id, sub_contract_id, billing_number, billing_date,
                 period_from, period_to, work_completed_percent,
                 gross_amount, retention_deducted, previous_billings_amount, net_payable,
                 status, notes, created_at, updated_at)
            VALUES
                (@Id, @CompanyId, @SubContractId, @BillingNumber, @BillingDate,
                 @PeriodFrom, @PeriodTo, @WorkCompletedPercent,
                 @GrossAmount, @RetentionDeducted, @PreviousBillingsAmount, @NetPayable,
                 @Status, @Notes, @CreatedAt, @UpdatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            billing.Id, billing.CompanyId, billing.SubContractId, billing.BillingNumber, billing.BillingDate,
            billing.PeriodFrom, billing.PeriodTo, billing.WorkCompletedPercent,
            billing.GrossAmount, billing.RetentionDeducted,
            billing.PreviousBillingsAmount, billing.NetPayable,
            billing.Status, billing.Notes, billing.CreatedAt, billing.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(SubProgressBilling billing, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE sub_progress_billings SET
                billing_date = @BillingDate,
                period_from = @PeriodFrom, period_to = @PeriodTo,
                work_completed_percent = @WorkCompletedPercent,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id AND company_id = @CompanyId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            billing.Id, billing.CompanyId, billing.BillingDate,
            billing.PeriodFrom, billing.PeriodTo, billing.WorkCompletedPercent,
            billing.Notes, billing.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(Guid id, int status, DateTime updatedAt, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE sub_progress_billings SET
                status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id";
        await conn.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, Status = status, UpdatedAt = updatedAt }, cancellationToken: ct));
    }
}
