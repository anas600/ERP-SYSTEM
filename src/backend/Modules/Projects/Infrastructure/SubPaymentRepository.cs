using Dapper;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Infrastructure;

/// <summary>
/// Sprint 64 / DEC-224 — Dapper repository for <c>sub_payments</c>.
///
/// <para><b>Why Dapper (DEC-008)</b>: NO EF Core in this codebase. Every
/// repository uses Dapper against the OLTP connection (see
/// <see cref="IDbConnectionFactory"/>).</para>
///
/// <para><b>L19 / DEC-095</b>: every WHERE / INSERT includes
/// <c>company_id</c> where applicable so the service layer's JWT-derived
/// company cannot be spoofed via the request DTO.</para>
/// </summary>
public sealed class SubPaymentRepository : ISubPaymentRepository
{
    private readonly IDbConnectionFactory _db;
    public SubPaymentRepository(IDbConnectionFactory db) => _db = db;

    private const string SelectColumns = @"
        id, company_id AS CompanyId, sub_contract_id AS SubContractId,
        sub_progress_billing_id AS SubProgressBillingId,
        payment_number AS PaymentNumber, payment_date AS PaymentDate,
        amount, retention_released AS RetentionReleased,
        payment_method AS PaymentMethod, reference_number AS ReferenceNumber,
        notes, created_at AS CreatedAt";

    public async Task<SubPayment?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<SubPayment>(new CommandDefinition(
            $"SELECT {SelectColumns} FROM sub_payments WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SubPayment>> ListBySubContractAsync(
        Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = $@"
            SELECT {SelectColumns}
            FROM sub_payments
            WHERE sub_contract_id = @SubContractId
            ORDER BY payment_date ASC, payment_number ASC";
        var rows = await conn.QueryAsync<SubPayment>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SubPayment>> ListBySubProgressBillingAsync(
        Guid subProgressBillingId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = $@"
            SELECT {SelectColumns}
            FROM sub_payments
            WHERE sub_progress_billing_id = @SubProgressBillingId
            ORDER BY payment_date ASC, payment_number ASC";
        var rows = await conn.QueryAsync<SubPayment>(new CommandDefinition(
            sql, new { SubProgressBillingId = subProgressBillingId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<decimal> SumPaidBySubContractAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // total paid = SUM(amount + retention_released) — both reduce outstanding balance.
        const string sql = @"
            SELECT COALESCE(SUM(amount + retention_released), 0)::numeric
            FROM sub_payments
            WHERE sub_contract_id = @SubContractId";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
    }

    public async Task<decimal> SumRetentionReleasedBySubContractAsync(Guid subContractId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT COALESCE(SUM(retention_released), 0)::numeric
            FROM sub_payments
            WHERE sub_contract_id = @SubContractId";
        return await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            sql, new { SubContractId = subContractId }, cancellationToken: ct));
    }

    public async Task InsertAsync(SubPayment payment, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            INSERT INTO sub_payments
                (id, company_id, sub_contract_id, sub_progress_billing_id,
                 payment_number, payment_date, amount, retention_released,
                 payment_method, reference_number, notes, created_at)
            VALUES
                (@Id, @CompanyId, @SubContractId, @SubProgressBillingId,
                 @PaymentNumber, @PaymentDate, @Amount, @RetentionReleased,
                 @PaymentMethod, @ReferenceNumber, @Notes, @CreatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            payment.Id, payment.CompanyId, payment.SubContractId, payment.SubProgressBillingId,
            payment.PaymentNumber, payment.PaymentDate, payment.Amount, payment.RetentionReleased,
            payment.PaymentMethod, payment.ReferenceNumber, payment.Notes, payment.CreatedAt
        }, cancellationToken: ct));
    }
}
