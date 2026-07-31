using Dapper;
using ERPSystem.Modules.Payments.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Payments.Infrastructure;

/// <summary>تنفيذ IPaymentRepository عبر Dapper.</summary>
public sealed class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _db;
    public PaymentRepository(IDbConnectionFactory db) => _db = db;

    private const string SelPayment = @"
        id, company_id AS CompanyId,
        party_type AS PartyType, party_id AS PartyId,
        payment_number AS PaymentNumber, payment_date AS PaymentDate, amount,
        currency_code AS CurrencyCode, payment_method AS PaymentMethod,
        bank_account_id AS BankAccountId, notes,
        status, posted_at AS PostedAt, posted_by AS PostedBy,
        journal_entry_id AS JournalEntryId,
        created_at AS CreatedAt, created_by AS CreatedBy,
        updated_at AS UpdatedAt, updated_by AS UpdatedBy";

    private const string SelAlloc = @"
        id, payment_id AS PaymentId,
        ref_type AS RefType, ref_id AS RefId, amount_applied AS AmountApplied";

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var p = await conn.QueryFirstOrDefaultAsync<Payment>(new CommandDefinition(
            $"SELECT {SelPayment} FROM payments WHERE id = @Id LIMIT 1",
            new { Id = id }, cancellationToken: ct));
        if (p != null) p.Allocations = (await GetAllocationsAsync(p.Id, ct)).ToList();
        return p;
    }

    public async Task<Payment?> GetByPaymentNumberAsync(string paymentNumber, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Payment>(new CommandDefinition(
            $"SELECT {SelPayment} FROM payments WHERE payment_number = @PaymentNumber LIMIT 1",
            new { PaymentNumber = paymentNumber }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Payment>> ListAsync(
        string? partyType, Guid? partyId, PaymentStatus? status,
        int skip, int take, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT {SelPayment} FROM payments WHERE 1=1";
        var p = new DynamicParameters();
        if (!string.IsNullOrEmpty(partyType)) { sql += " AND party_type = @PartyType"; p.Add("PartyType", partyType); }
        if (partyId.HasValue) { sql += " AND party_id = @PartyId"; p.Add("PartyId", partyId.Value); }
        if (status.HasValue) { sql += " AND status = @Status"; p.Add("Status", status.Value.ToString()); }
        sql += " ORDER BY payment_date DESC, created_at DESC OFFSET @Skip LIMIT @Take";
        p.Add("Skip", skip); p.Add("Take", take);

        var rows = await conn.QueryAsync<Payment>(new CommandDefinition(sql, p, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(Payment payment, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO payments (id, company_id, party_type, party_id, payment_number,
                                  payment_date, amount, currency_code, payment_method, bank_account_id, notes,
                                  status, posted_at, posted_by, journal_entry_id,
                                  created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @CompanyId, @PartyType, @PartyId, @PaymentNumber,
                    @PaymentDate, @Amount, @CurrencyCode, @PaymentMethod, @BankAccountId, @Notes,
                    @Status, @PostedAt, @PostedBy, @JournalEntryId,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy)", new
        {
            payment.Id, payment.CompanyId,
            payment.PartyType, payment.PartyId, payment.PaymentNumber,
            payment.PaymentDate, payment.Amount, payment.CurrencyCode, payment.PaymentMethod,
            payment.BankAccountId, payment.Notes,
            Status = payment.Status.ToString(),
            payment.PostedAt, payment.PostedBy, payment.JournalEntryId,
            payment.CreatedAt, payment.CreatedBy, payment.UpdatedAt, payment.UpdatedBy
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE payments
               SET amount = @Amount, payment_date = @PaymentDate, payment_method = @PaymentMethod,
                   notes = @Notes, status = @Status, posted_at = @PostedAt, posted_by = @PostedBy,
                   journal_entry_id = @JournalEntryId,
                   updated_at = @UpdatedAt, updated_by = @UpdatedBy
             WHERE id = @Id", new
        {
            payment.Id, payment.Amount, payment.PaymentDate, payment.PaymentMethod,
            payment.Notes, Status = payment.Status.ToString(),
            payment.PostedAt, payment.PostedBy, payment.JournalEntryId,
            payment.UpdatedAt, payment.UpdatedBy
        }, cancellationToken: ct));
    }

    public async Task InsertAllocationsAsync(Guid paymentId, IEnumerable<PaymentAllocation> allocations, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        foreach (var a in allocations)
        {
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO payment_allocations (id, payment_id, ref_type, ref_id, amount_applied)
                VALUES (@Id, @PaymentId, @RefType, @RefId, @AmountApplied)",
                new
                {
                    a.Id, PaymentId = paymentId,
                    a.RefType, a.RefId, a.AmountApplied
                }, cancellationToken: ct));
        }
    }

    public async Task<IReadOnlyList<PaymentAllocation>> GetAllocationsAsync(Guid paymentId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<PaymentAllocation>(new CommandDefinition(
            $"SELECT {SelAlloc} FROM payment_allocations WHERE payment_id = @PaymentId ORDER BY id",
            new { PaymentId = paymentId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<decimal> SumAllocationsForRefAsync(string refType, Guid refId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var sum = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(pa.amount_applied), 0)
            FROM payment_allocations pa
            INNER JOIN payments p ON p.id = pa.payment_id
            WHERE p.status = 'Posted'
              AND pa.ref_type = @RefType
              AND pa.ref_id = @RefId",
            new { RefType = refType, RefId = refId }, cancellationToken: ct));
        return sum ?? 0m;
    }
}
