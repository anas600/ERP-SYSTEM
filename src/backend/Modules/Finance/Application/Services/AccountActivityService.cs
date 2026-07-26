using Dapper;
using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IAccountActivityService
{
    Task<FinanceResult<AccountActivityResponse>> GetActivityAsync(
        Guid companyId, Guid accountId, DateTime? from, DateTime? to, CancellationToken ct);
}

public sealed class AccountActivityService : IAccountActivityService
{
    private readonly IDbConnectionFactory _db;
    private readonly IAccountRepository _accounts;

    public AccountActivityService(IDbConnectionFactory db, IAccountRepository accounts)
    {
        _db = db; _accounts = accounts;
    }

    public async Task<FinanceResult<AccountActivityResponse>> GetActivityAsync(
        Guid companyId, Guid accountId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.CompanyId != companyId)
            return FinanceResult<AccountActivityResponse>.Fail("الحساب غير موجود.", FinanceErrorCode.NotFound);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Opening balance
        decimal openingDr = 0, openingCr = 0;
        if (from.HasValue)
        {
            const string openingSql = @"
                SELECT COALESCE(SUM(jl.debit), 0), COALESCE(SUM(jl.credit), 0)
                FROM journal_lines jl
                INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                WHERE jl.account_id = @AccountId AND je.company_id = @CompanyId
                  AND je.status = 2 AND je.entry_date < @From";
            var op = await conn.QueryFirstAsync<(decimal dr, decimal cr)>(new CommandDefinition(openingSql,
                new { AccountId = accountId, CompanyId = companyId, From = from.Value }, cancellationToken: ct));
            openingDr = op.dr; openingCr = op.cr;
        }

        // Period transactions
        const string txSql = @"
            SELECT jl.id AS journal_line_id, je.entry_date, je.entry_number, je.reference, je.description,
                   jl.debit, jl.credit
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            WHERE jl.account_id = @AccountId AND je.company_id = @CompanyId
              AND je.status = 2
              AND (@From::timestamptz IS NULL OR je.entry_date >= @From)
              AND (@To::timestamptz IS NULL OR je.entry_date <= @To)
            ORDER BY je.entry_date, jl.id";

        var tx = (await conn.QueryAsync<AccountActivityRow>(new CommandDefinition(txSql,
            new { AccountId = accountId, CompanyId = companyId, From = from, To = to }, cancellationToken: ct))).AsList();

        var periodDr = tx.Sum(t => t.debit);
        var periodCr = tx.Sum(t => t.credit);

        // Compute running balance based on NormalBalance
        // NormalBalance: 1=Debit, 2=Credit
        int nb = (int)account.NormalBalance;
        decimal running = nb == 1
            ? (openingDr - openingCr)
            : (openingCr - openingDr);
        var transactions = tx.Select(t =>
        {
            running += nb == 1 ? (t.debit - t.credit) : (t.credit - t.debit);
            return new AccountActivityTransaction
            {
                JournalLineId = t.journal_line_id,
                EntryDate = t.entry_date,
                EntryNumber = t.entry_number,
                Reference = t.reference,
                Description = t.description,
                Debit = t.debit,
                Credit = t.credit
            };
        }).ToList();

        decimal closing = nb == 1
            ? (openingDr + periodDr - openingCr - periodCr)
            : (openingCr + periodCr - openingDr - periodDr);

        return FinanceResult<AccountActivityResponse>.Ok(new AccountActivityResponse
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            NormalBalance = nb,
            From = from,
            To = to,
            OpeningBalance = nb == 1 ? (openingDr - openingCr) : (openingCr - openingDr),
            PeriodDebit = periodDr,
            PeriodCredit = periodCr,
            ClosingBalance = closing,
            Transactions = transactions
        });
    }

    private sealed class AccountActivityRow
    {
        public Guid journal_line_id { get; set; }
        public DateTime entry_date { get; set; }
        public string entry_number { get; set; } = string.Empty;
        public string reference { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal debit { get; set; }
        public decimal credit { get; set; }
    }
}
