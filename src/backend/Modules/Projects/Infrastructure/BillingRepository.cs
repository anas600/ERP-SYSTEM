using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

public sealed class BillingRepository : IBillingRepository
{
    private readonly IDbConnectionFactory _db;

    public BillingRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ProgressBilling?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, company_id AS CompanyId, project_id AS ProjectId, contract_id AS ContractId,
                   billing_number AS BillingNumber, billing_date AS BillingDate,
                   period_from AS PeriodFrom, period_to AS PeriodTo,
                   work_completed_percent AS WorkCompletedPercent,
                   gross_amount AS GrossAmount, advance_deducted AS AdvanceDeducted,
                   retention_deducted AS RetentionDeducted, net_amount AS NetAmount,
                   regional_premium_deducted AS RegionalPremiumDeducted,
                   net_amount_after_premium AS NetAmountAfterPremium,
                   status, invoice_id AS InvoiceId, journal_entry_id AS JournalEntryId, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM progress_billings WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<ProgressBilling>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ProgressBilling>> ListByProjectAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, company_id AS CompanyId, project_id AS ProjectId, contract_id AS ContractId,
                   billing_number AS BillingNumber, billing_date AS BillingDate,
                   period_from AS PeriodFrom, period_to AS PeriodTo,
                   work_completed_percent AS WorkCompletedPercent,
                   gross_amount AS GrossAmount, advance_deducted AS AdvanceDeducted,
                   retention_deducted AS RetentionDeducted, net_amount AS NetAmount,
                   regional_premium_deducted AS RegionalPremiumDeducted,
                   net_amount_after_premium AS NetAmountAfterPremium,
                   status, invoice_id AS InvoiceId, journal_entry_id AS JournalEntryId, notes,
                   created_at AS CreatedAt, created_by AS CreatedBy,
                   updated_at AS UpdatedAt, updated_by AS UpdatedBy
            FROM progress_billings
            WHERE project_id = @ProjectId
            ORDER BY billing_date DESC, billing_number DESC";
        var rows = await conn.QueryAsync<ProgressBilling>(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<bool> BillingNumberExistsAsync(string billingNumber, Guid companyId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT 1 FROM progress_billings WHERE billing_number = @BillingNumber AND company_id = @CompanyId LIMIT 1";
        var hit = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { BillingNumber = billingNumber, CompanyId = companyId }, cancellationToken: ct));
        return hit.HasValue;
    }

    public async Task<decimal> SumAdvanceDeductedAsync(Guid contractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT COALESCE(SUM(advance_deducted), 0) FROM progress_billings WHERE contract_id = @ContractId AND status != 'CANCELLED'";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, new { ContractId = contractId }, cancellationToken: ct));
    }

    public async Task<int> CountNonCancelledAsync(Guid contractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT COUNT(*)::int FROM progress_billings WHERE contract_id = @ContractId AND status != 'CANCELLED'";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { ContractId = contractId }, cancellationToken: ct));
    }

    public async Task<decimal> MaxPercentAsync(Guid contractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT COALESCE(MAX(work_completed_percent), 0) FROM progress_billings WHERE contract_id = @ContractId AND status != 'CANCELLED'";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(sql, new { ContractId = contractId }, cancellationToken: ct));
    }

    public async Task InsertAsync(ProgressBilling billing, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO progress_billings (id, company_id, project_id, contract_id, billing_number,
                                          billing_date, period_from, period_to,
                                          work_completed_percent, gross_amount, advance_deducted,
                                          retention_deducted, net_amount,
                                          regional_premium_deducted, net_amount_after_premium,
                                          status,
                                          invoice_id, journal_entry_id, notes,
                                          created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @CompanyId, @ProjectId, @ContractId, @BillingNumber,
                    @BillingDate, @PeriodFrom, @PeriodTo,
                    @WorkCompletedPercent, @GrossAmount, @AdvanceDeducted,
                    @RetentionDeducted, @NetAmount,
                    @RegionalPremiumDeducted, @NetAmountAfterPremium,
                    @Status,
                    @InvoiceId, @JournalEntryId, @Notes,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy)";
        // status column is varchar(20) — convert enum to string explicitly
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            billing.Id, billing.CompanyId, billing.ProjectId, billing.ContractId, billing.BillingNumber,
            billing.BillingDate, billing.PeriodFrom, billing.PeriodTo,
            billing.WorkCompletedPercent, billing.GrossAmount, billing.AdvanceDeducted,
            billing.RetentionDeducted, billing.NetAmount,
            billing.RegionalPremiumDeducted, billing.NetAmountAfterPremium,
            Status = StatusToString(billing.Status),
            billing.InvoiceId, billing.JournalEntryId, billing.Notes,
            billing.CreatedAt, billing.CreatedBy, billing.UpdatedAt, billing.UpdatedBy,
        }, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(Guid id, BillingStatus status, Guid? invoiceId, Guid? journalEntryId, Guid updatedBy, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE progress_billings
            SET status = @Status, invoice_id = @InvoiceId, journal_entry_id = @JournalEntryId,
                updated_at = NOW(), updated_by = @UpdatedBy
            WHERE id = @Id";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id, Status = StatusToString(status), InvoiceId = invoiceId, JournalEntryId = journalEntryId, UpdatedBy = updatedBy
        }, cancellationToken: ct));
    }

    private static string StatusToString(BillingStatus s) => s switch
    {
        BillingStatus.Draft => "DRAFT",
        BillingStatus.Invoiced => "INVOICED",
        BillingStatus.Cancelled => "CANCELLED",
        _ => "DRAFT",
    };
}
