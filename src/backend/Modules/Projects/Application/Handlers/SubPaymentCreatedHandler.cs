using System.Data.Common;
using Dapper;
using ERPSystem.Modules.Projects.Application.Events;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Handlers;

public interface ISubPaymentCreatedHandler
{
    Task HandleAsync(SubPaymentCreatedEvent evt, CancellationToken ct);
}

/// <summary>
/// Sprint 65 / DEC-232 (Wave 1A): Subscribes to <see cref="SubPaymentCreatedEvent"/>.
///
/// <para>Responsibility: when a sub-payment is recorded, create the AP Vendor Bill + Journal
/// Entry (Dr Subcontractor Cost / Cr Cash). When the payment also releases retention, a
/// separate bill is created for the retention amount.</para>
///
/// <para><b>No real Subcontractor / SubPayment table yet:</b> the Subcontractor module is being
/// landed in parallel (Sprint 64 branch). Until that schema merges, this handler uses the
/// <c>SubcontractorId</c> from the event as the AP vendor's id (1:1 mapping by convention) and
/// skips creation cleanly if no matching vendor exists. The handler is a self-contained Finance
/// integration point — it does not depend on a SubPayment DB row.</para>
///
/// <para><b>L19 / DEC-095 compliance:</b> <c>CompanyId</c> and <c>UserId</c> come from the event
/// payload, which the firing service populated from <c>ICompanyContext</c> and the JWT.</para>
/// </summary>
public sealed class SubPaymentCreatedHandler : ISubPaymentCreatedHandler
{
    // Account codes — match the professional 4-level CoA.
    // 5100 = Cost of Services (Subcontractor Cost)
    // 1101 = Cash on Hand
    private const string SubcontractorCostAccountCode = "5100";
    private const string CashAccountCode = "1101";

    private readonly IDbConnectionFactory _db;
    private readonly ILogger<SubPaymentCreatedHandler> _logger;

    public SubPaymentCreatedHandler(
        IDbConnectionFactory db,
        ILogger<SubPaymentCreatedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(SubPaymentCreatedEvent evt, CancellationToken ct)
    {
        // Guard: ignore non-positive amounts
        if (evt.Amount <= 0 && evt.RetentionReleased <= 0)
        {
            _logger.LogDebug("Finance trigger: sub-payment {Id} has no amount and no retention — no-op",
                evt.SubPaymentId);
            return;
        }

        var conn = await _db.CreateOltpConnectionAsync(ct);
        try
        {
            // Resolve the two accounts we need. If missing, log and bail — we cannot post a JE
            // without a valid debit + credit account.
            var accounts = (await conn.QueryAsync<(string Code, Guid Id)>(new CommandDefinition(
                "SELECT code, id FROM accounts WHERE code IN (@Cost, @Cash) AND company_id = @CompanyId",
                new { Cost = SubcontractorCostAccountCode, Cash = CashAccountCode, evt.CompanyId },
                cancellationToken: ct))).ToList();

            var costAcct = accounts.FirstOrDefault(a => a.Code == SubcontractorCostAccountCode);
            var cashAcct = accounts.FirstOrDefault(a => a.Code == CashAccountCode);
            if (costAcct.Id == Guid.Empty || cashAcct.Id == Guid.Empty)
            {
                _logger.LogError(
                    "Finance trigger: missing CoA accounts (5100={CostOk}, 1101={CashOk}) for company {CompanyId} — cannot post JE for sub-payment {Id}",
                    costAcct.Id != Guid.Empty, cashAcct.Id != Guid.Empty, evt.CompanyId, evt.SubPaymentId);
                return;
            }

            // Resolve vendor (subcontractorId treated as vendorId by MVP convention).
            var vendorExists = await conn.ExecuteScalarAsync<bool?>(new CommandDefinition(
                "SELECT TRUE FROM vendors WHERE id = @Id AND company_id = @CompanyId LIMIT 1",
                new { Id = evt.SubcontractorId, evt.CompanyId },
                cancellationToken: ct)) ?? false;
            if (!vendorExists)
            {
                _logger.LogWarning(
                    "Finance trigger: no vendor for subcontractor {SubId} in company {CompanyId} — cannot create bill for sub-payment {PayId}",
                    evt.SubcontractorId, evt.CompanyId, evt.SubPaymentId);
                return;
            }

            // Process each amount: base payment + (optional) retention release.
            if (evt.Amount > 0)
            {
                await CreateBillAndEntryAsync(
                    conn, ct,
                    billSuffix: $"PAY-{evt.SubPaymentId:N}".Substring(0, 20),
                    vendorId: evt.SubcontractorId,
                    amount: evt.Amount,
                    description: $"Subcontractor payment {evt.SubPaymentId} (sub-contract {evt.SubContractId})",
                    reference: $"SUB-PAY-{evt.SubPaymentId}",
                    costAcctId: costAcct.Id,
                    cashAcctId: cashAcct.Id,
                    companyId: evt.CompanyId,
                    userId: evt.UserId);
            }

            if (evt.RetentionReleased > 0)
            {
                await CreateBillAndEntryAsync(
                    conn, ct,
                    billSuffix: $"RET-{evt.SubPaymentId:N}".Substring(0, 20),
                    vendorId: evt.SubcontractorId,
                    amount: evt.RetentionReleased,
                    description: $"Retention release for sub-payment {evt.SubPaymentId}",
                    reference: $"SUB-RET-{evt.SubPaymentId}",
                    costAcctId: costAcct.Id,
                    cashAcctId: cashAcct.Id,
                    companyId: evt.CompanyId,
                    userId: evt.UserId);
            }

            _logger.LogInformation(
                "Finance trigger: SubPayment {Id} → base bill amount={Amount}, retention bill amount={Ret}",
                evt.SubPaymentId, evt.Amount, evt.RetentionReleased);
        }
        finally
        {
            if (conn is IAsyncDisposable iad) await iad.DisposeAsync();
            else if (conn is not null) conn.Dispose();
        }
    }

    /// <summary>
    /// Creates a Vendor Bill (Draft) and a balanced Journal Entry (Dr Cost / Cr Cash).
    /// Wrapped in a single transaction so the two sides either both commit or both roll back.
    /// </summary>
    private static async Task CreateBillAndEntryAsync(
        System.Data.IDbConnection conn,
        CancellationToken ct,
        string billSuffix,
        Guid vendorId,
        decimal amount,
        string description,
        string reference,
        Guid costAcctId,
        Guid cashAcctId,
        Guid companyId,
        Guid userId)
    {
        // Open a transaction. The connection is whatever IDbConnectionFactory returned; for
        // Npgsql it's an NpgsqlConnection (concrete BeginTransactionAsync), for the in-memory
        // test double it's a DbConnection (abstract BeginTransactionAsync). We use the
        // DbConnection-level API which is implemented by both.
        DbConnection dbConn = (DbConnection)conn;
        await using var tx = await dbConn.BeginTransactionAsync(ct);
        try
        {
            // 1) Insert vendor_bill (Draft)
            var billId = Guid.NewGuid();
            var billNumber = $"AP-{billSuffix}-{DateTime.UtcNow:HHmmss}";
            const string billSql = @"
                INSERT INTO vendor_bills (id, company_id, bill_number, vendor_id, status,
                                          bill_date, due_date, currency, sub_total, tax_amount, total_amount,
                                          paid_amount, notes, created_at, created_by, updated_at, updated_by)
                VALUES (@Id, @CompanyId, @BillNumber, @VendorId, 'Draft',
                        NOW(), NOW(), 'LYD', @SubTotal, 0, @TotalAmount,
                        0, @Notes, NOW(), @CreatedBy, NOW(), @UpdatedBy)";
            await conn.ExecuteAsync(new CommandDefinition(billSql, new
            {
                Id = billId,
                CompanyId = companyId,
                BillNumber = billNumber,
                VendorId = vendorId,
                SubTotal = amount,
                TotalAmount = amount,
                Notes = description,
                CreatedBy = userId,
                UpdatedBy = userId,
            }, transaction: tx, cancellationToken: ct));

            // 2) Generate next journal entry number (best-effort: try a sequence-like pattern;
            //    fall back to a UUID-based number if no helper exists).
            var jeNumber = await GetNextJournalEntryNumberAsync(conn, tx, ct);

            // 3) Insert journal_entry header
            var jeId = Guid.NewGuid();
            const string jeHeaderSql = @"
                INSERT INTO journal_entries (id, entry_number, company_id, entry_date, description, reference,
                                            status, created_by_user_id, posted_at, created_at, updated_at)
                VALUES (@Id, @EntryNumber, @CompanyId, NOW(), @Description, @Reference,
                        2, @CreatedBy, NOW(), NOW(), NOW())";
            await conn.ExecuteAsync(new CommandDefinition(jeHeaderSql, new
            {
                Id = jeId,
                EntryNumber = jeNumber,
                CompanyId = companyId,
                Description = description,
                Reference = reference,
                CreatedBy = userId,
            }, transaction: tx, cancellationToken: ct));

            // 4) Insert the two journal lines (Dr Cost / Cr Cash)
            const string lineSql = @"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, company_id, debit, credit, description, line_number)
                VALUES (@Id, @JournalEntryId, @AccountId, @CompanyId, @Debit, @Credit, @Description, @LineNumber)";
            await conn.ExecuteAsync(new CommandDefinition(lineSql, new
            {
                Id = Guid.NewGuid(),
                JournalEntryId = jeId,
                AccountId = costAcctId,
                CompanyId = companyId,
                Debit = amount,
                Credit = 0m,
                Description = $"Subcontractor cost — {description}",
                LineNumber = 1,
            }, transaction: tx, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(lineSql, new
            {
                Id = Guid.NewGuid(),
                JournalEntryId = jeId,
                AccountId = cashAcctId,
                CompanyId = companyId,
                Debit = 0m,
                Credit = amount,
                Description = $"Cash paid — {description}",
                LineNumber = 2,
            }, transaction: tx, cancellationToken: ct));

            // 5) Back-link the bill to the JE
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE vendor_bills SET journal_entry_id = @JeId WHERE id = @BillId",
                new { JeId = jeId, BillId = billId },
                transaction: tx,
                cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<string> GetNextJournalEntryNumberAsync(
        System.Data.IDbConnection conn,
        DbTransaction tx,
        CancellationToken ct)
    {
        // Reuse the same pattern BillingService.ApproveAsync uses: count + 1 with a JE prefix.
        const string sql = @"
            SELECT 'JE-' || TO_CHAR(NOW(), 'YYYY') || '-' ||
                   LPAD((COUNT(*) + 1)::text, 6, '0') AS next_number
            FROM journal_entries
            WHERE company_id = (SELECT company_id FROM journal_entries LIMIT 1)";
        var n = await conn.ExecuteScalarAsync<string>(new CommandDefinition(sql, transaction: tx, cancellationToken: ct));
        return string.IsNullOrEmpty(n) ? $"JE-{DateTime.UtcNow:yyyyMMddHHmmssfff}" : n;
    }
}
