// Sprint 53 (DEC-140 + DEC-141) — Year-End Closing + Retained Earnings Roll
//
// الـ "إقفال السنة المالية" في ERP: نقل أرصدة الإيرادات والمصروفات إلى حساب
// "ملخّص الدخل" (3210)، ثم ترحيل صافي السنة إلى "أرباح محتجزة" (3200).
//
// السبب: بدون إقفال، حسابات 4xxx (الإيرادات) و 5xxx (المصروفات) تحمل أرصدة
// في الميزانية العمومية (تختلط مع الأصول/الخصوم). المعادلة المحاسبية تفترض
// أن السنة قد أُقفلت — هذا ما يجعل "صافي دخل السنة" صفًّا افتراضيًّا في Sprint 52a
// حلًّا مؤقتًا. الـ closing entry يجعل الحل دائمًا.
//
// idempotency: لو الـ entry موجود بالفعل (entry_number='YE-{year}-CLOSING')، نرجع true
// بدون إعادة الإنشاء.

using Dapper;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IYearEndClosingService
{
    Task<YearEndClosingResult> CloseYearAsync(Guid companyId, int year, CancellationToken ct);
    Task<YearEndClosingStatus> GetStatusAsync(Guid companyId, int year, CancellationToken ct);
}

public sealed class YearEndClosingResult
{
    public bool Success { get; set; }
    public int Year { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal NetIncome { get; set; }
    public Guid? ClosingEntryId { get; set; }
    public Guid? RollEntryId { get; set; }
    public bool WasAlreadyClosed { get; set; }
}

public sealed class YearEndClosingStatus
{
    public bool IsClosed { get; set; }
    public Guid? ClosingEntryId { get; set; }
    public Guid? RollEntryId { get; set; }
    public DateTime? ClosingDate { get; set; }
    public decimal NetIncome { get; set; }
}

public sealed class YearEndClosingService : IYearEndClosingService
{
    private readonly IDbConnectionFactory _db;
    private readonly IAccountRepository _accounts;
    private readonly IJournalEntryRepository _entries;
    private readonly ILogger<YearEndClosingService> _logger;

    // Accounts we use for closing
    private const string ClosingEntryPrefix = "YE-";
    private const string ClosingEntrySuffix = "-CLOSING";
    private const string RollEntrySuffix = "-ROLL";

    public YearEndClosingService(
        IDbConnectionFactory db,
        IAccountRepository accounts,
        IJournalEntryRepository entries,
        ILogger<YearEndClosingService> logger)
    {
        _db = db;
        _accounts = accounts;
        _entries = entries;
        _logger = logger;
    }

    public async Task<YearEndClosingStatus> GetStatusAsync(Guid companyId, int year, CancellationToken ct)
    {
        using var conn = (NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);
        var closingNumber = $"{ClosingEntryPrefix}{year}{ClosingEntrySuffix}";
        var rollNumber = $"{ClosingEntryPrefix}{year}{RollEntrySuffix}";

        var closingEntry = await conn.QueryFirstOrDefaultAsync<(Guid id, DateTime entry_date)?>(new CommandDefinition(@"
            SELECT id, entry_date FROM journal_entries
            WHERE company_id = @CompanyId AND entry_number = @EntryNumber LIMIT 1",
            new { CompanyId = companyId, EntryNumber = closingNumber }, cancellationToken: ct));

        var rollEntry = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(@"
            SELECT id FROM journal_entries
            WHERE company_id = @CompanyId AND entry_number = @EntryNumber LIMIT 1",
            new { CompanyId = companyId, EntryNumber = rollNumber }, cancellationToken: ct));

        return new YearEndClosingStatus
        {
            IsClosed = closingEntry.HasValue,
            ClosingEntryId = closingEntry?.id,
            RollEntryId = rollEntry,
            ClosingDate = closingEntry?.entry_date,
        };
    }

    public async Task<YearEndClosingResult> CloseYearAsync(Guid companyId, int year, CancellationToken ct)
    {
        var closingNumber = $"{ClosingEntryPrefix}{year}{ClosingEntrySuffix}";
        var rollNumber = $"{ClosingEntryPrefix}{year}{RollEntrySuffix}";

        // 1) Idempotency check — if closing entry exists, skip
        var status = await GetStatusAsync(companyId, year, ct);
        if (status.IsClosed)
        {
            return new YearEndClosingResult
            {
                Success = true,
                Year = year,
                Message = $"السنة {year} مقفلة بالفعل ({closingNumber}) — لا حاجة لإعادة الإقفال.",
                ClosingEntryId = status.ClosingEntryId,
                RollEntryId = status.RollEntryId,
                WasAlreadyClosed = true,
            };
        }

        // 2) Get all Revenue (4) + Expense (5) accounts with non-zero balance up to year-end
        var yearEnd = new DateTime(year, 12, 31, 23, 59, 59);
        var balances = await GetRevenueExpenseBalancesAsync(companyId, yearEnd, ct);

        if (balances.Count == 0)
        {
            return new YearEndClosingResult
            {
                Success = false,
                Year = year,
                Message = $"لا توجد أرصدة إيرادات/مصروفات للسنة {year} — لا يمكن إنشاء قيد إقفال.",
            };
        }

        // 3) Compute NetIncome = Σ(Revenue credit - debit) - Σ(Expense debit - credit)
        decimal totalRevenue = 0, totalExpense = 0;
        foreach (var (account, balance) in balances)
        {
            if (account.Type == AccountType.Revenue)
                totalRevenue += balance; // balance is already positive (Cr - Dr)
            else if (account.Type == AccountType.Expense)
                totalExpense += balance; // balance is positive (Dr - Cr)
        }
        var netIncome = totalRevenue - totalExpense;

        _logger.LogInformation(
            "[Sprint53] Year-end closing for {Year}: Revenue={Rev:N2}, Expense={Exp:N2}, NetIncome={NI:N2}",
            year, totalRevenue, totalExpense, netIncome);

        // 4) Ensure account 3210 (Current Year P&L) exists; create if missing
        var pnlAccount = await EnsurePnLAccountAsync(companyId, ct);

        // 5) Build the closing entry
        var closingDate = new DateTime(year, 12, 31);
        var closingEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = closingNumber,
            CompanyId = companyId,
            EntryDate = closingDate,
            Description = $"إقفال السنة المالية {year} — تحويل الإيرادات والمصروفات إلى ملخّص الدخل",
            Reference = $"YEAR-END-{year}",
            Status = JournalEntryStatus.Posted, // Posted directly (no draft)
            CreatedByUserId = Guid.Empty, // System-generated
            PostedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var lineNum = 1;
        var totalDr = 0m;
        var totalCr = 0m;
        // DR each Revenue (zero it out)
        foreach (var (account, balance) in balances.Where(b => b.account.Type == AccountType.Revenue && b.balance > 0))
        {
            closingEntry.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = closingEntry.Id,
                CompanyId = companyId,
                AccountId = account.Id,
                Debit = balance,  // DR to zero out the Cr balance
                Credit = 0,
                Description = $"إقفال حساب الإيرادات: {account.Code} — {account.Name}",
                LineNumber = lineNum++,
            });
            totalDr += balance;
        }
        // CR each Expense (zero it out)
        foreach (var (account, balance) in balances.Where(b => b.account.Type == AccountType.Expense && b.balance > 0))
        {
            closingEntry.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = closingEntry.Id,
                CompanyId = companyId,
                AccountId = account.Id,
                Debit = 0,
                Credit = balance,  // CR to zero out the Dr balance
                Description = $"إقفال حساب المصروفات: {account.Code} — {account.Name}",
                LineNumber = lineNum++,
            });
            totalCr += balance;
        }
        // The balancing line: 3210 (Current Year P&L)
        // If NetIncome > 0 (revenue > expense), then 3210 gets the CR side (profit)
        // If NetIncome < 0 (loss), then 3210 gets the DR side
        if (netIncome >= 0)
        {
            // So far: DR Revenue = totalRevenue, CR Expense = totalExpense
            // Need: total Dr = total Cr = totalRevenue + totalExpense (gross)
            // NetIncome = totalRevenue - totalExpense
            // The P&L account must balance: (totalRevenue + ?) - (totalExpense + NetIncome) = 0
            // 3210 CR NetIncome → total Cr = totalExpense + NetIncome = totalExpense + totalRevenue - totalExpense = totalRevenue ✓
            closingEntry.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = closingEntry.Id,
                CompanyId = companyId,
                AccountId = pnlAccount.Id,
                Debit = 0,
                Credit = netIncome,
                Description = $"صافي دخل السنة {year} → ملخّص الدخل (3210)",
                LineNumber = lineNum++,
            });
            totalCr += netIncome;
        }
        else
        {
            // Loss: 3210 DR |netIncome|, no CR
            closingEntry.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = closingEntry.Id,
                CompanyId = companyId,
                AccountId = pnlAccount.Id,
                Debit = -netIncome,  // positive amount
                Credit = 0,
                Description = $"خسارة السنة {year} → ملخّص الدخل (3210)",
                LineNumber = lineNum++,
            });
            totalDr += -netIncome;
        }

        // Validate balance
        if (totalDr != totalCr)
        {
            _logger.LogError("[Sprint53] Closing entry unbalanced: DR={Dr:N2} CR={Cr:N2}", totalDr, totalCr);
            return new YearEndClosingResult
            {
                Success = false,
                Year = year,
                Message = $"القيد غير متوازن: مدين {totalDr:N2} ≠ دائن {totalCr:N2}",
            };
        }

        // 6) Insert the closing entry (with Status=Posted)
        await _entries.InsertAsync(closingEntry, ct);
        _logger.LogInformation("[Sprint53] Closing entry inserted: {Number} ({Lines} lines, {Lines} lines posted)",
            closingNumber, closingEntry.Lines.Count, closingEntry.Lines.Count);

        // 7) Roll entry: move 3210 balance → 3200 on day 1 of next year
        Guid? rollEntryId = null;
        if (netIncome != 0)
        {
            var retainedEarnings = await GetRetainedEarningsAccountAsync(companyId, ct);
            var rollDate = new DateTime(year + 1, 1, 1);
            var rollEntry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                EntryNumber = rollNumber,
                CompanyId = companyId,
                EntryDate = rollDate,
                Description = $"ترحيل صافي دخل {year} إلى الأرباح المحتجزة",
                Reference = $"YEAR-END-{year}-ROLL",
                Status = JournalEntryStatus.Posted,
                CreatedByUserId = Guid.Empty,
                PostedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            if (netIncome > 0)
            {
                // DR 3210 / CR 3200 (profit → retained earnings)
                rollEntry.Lines.Add(new JournalLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = rollEntry.Id,
                    CompanyId = companyId,
                    AccountId = pnlAccount.Id,
                    Debit = netIncome,
                    Credit = 0,
                    Description = $"إقفال ملخّص الدخل {year}",
                    LineNumber = 1,
                });
                rollEntry.Lines.Add(new JournalLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = rollEntry.Id,
                    CompanyId = companyId,
                    AccountId = retainedEarnings.Id,
                    Debit = 0,
                    Credit = netIncome,
                    Description = $"إضافة إلى الأرباح المحتجزة",
                    LineNumber = 2,
                });
            }
            else
            {
                // Loss: DR 3200 / CR 3210
                rollEntry.Lines.Add(new JournalLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = rollEntry.Id,
                    CompanyId = companyId,
                    AccountId = retainedEarnings.Id,
                    Debit = -netIncome,
                    Credit = 0,
                    Description = $"خصم خسارة {year} من الأرباح المحتجزة",
                    LineNumber = 1,
                });
                rollEntry.Lines.Add(new JournalLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = rollEntry.Id,
                    CompanyId = companyId,
                    AccountId = pnlAccount.Id,
                    Debit = 0,
                    Credit = -netIncome,
                    Description = $"إقفال ملخّص الدخل {year}",
                    LineNumber = 2,
                });
            }
            await _entries.InsertAsync(rollEntry, ct);
            rollEntryId = rollEntry.Id;
            _logger.LogInformation("[Sprint53] Roll entry inserted: {Number}", rollNumber);
        }

        return new YearEndClosingResult
        {
            Success = true,
            Year = year,
            Message = $"تم إقفال السنة {year} بنجاح. صافي الدخل: {netIncome:N2} LYD",
            NetIncome = netIncome,
            ClosingEntryId = closingEntry.Id,
            RollEntryId = rollEntryId,
            WasAlreadyClosed = false,
        };
    }

    /// <summary>
    /// Returns balances for all Revenue (4) and Expense (5) accounts with non-zero balance as of asOfDate.
    /// Balance convention: positive for both (Revenue = credit, Expense = debit).
    /// </summary>
    private async Task<List<(Account account, decimal balance)>> GetRevenueExpenseBalancesAsync(
        Guid companyId, DateTime asOfDate, CancellationToken ct)
    {
        using var conn = (NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT a.id, a.company_id AS CompanyId, a.code, a.name, a.description, a.type,
                   a.normal_balance AS NormalBalance, a.parent_account_id AS ParentAccountId,
                   a.is_intercompany AS IsIntercompany, a.is_postable AS IsPostable,
                   a.is_active AS IsActive, a.level, a.created_at AS CreatedAt, a.updated_at AS UpdatedAt,
                   COALESCE(SUM(jl.debit), 0) AS TotalDebit,
                   COALESCE(SUM(jl.credit), 0) AS TotalCredit
            FROM accounts a
            LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
            LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = a.company_id
                AND je.status = 2
                AND je.entry_date <= @AsOfDate
            WHERE a.company_id = @CompanyId
              AND a.is_postable = true
              AND a.is_active = true
              AND a.type IN (4, 5)
            GROUP BY a.id
            ORDER BY a.type, a.code";

        var rows = (await conn.QueryAsync<AccountBalRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, AsOfDate = asOfDate.Date },
            cancellationToken: ct))).ToList();

        var result = new List<(Account, decimal)>();
        foreach (var r in rows)
        {
            // Convert to positive balance: Revenue (normal Cr) = Cr - Dr, Expense (normal Dr) = Dr - Cr
            var balance = r.NormalBalance == (int)NormalBalance.Credit
                ? (r.TotalCredit - r.TotalDebit)   // Revenue: positive = credit
                : (r.TotalDebit - r.TotalCredit);  // Expense: positive = debit
            if (balance > 0.005m)
            {
                var acc = new Account
                {
                    Id = r.Id,
                    CompanyId = r.CompanyId,
                    Code = r.Code,
                    Name = r.Name,
                    Type = (AccountType)r.Type,
                    NormalBalance = (NormalBalance)r.NormalBalance,
                    ParentAccountId = r.ParentAccountId,
                    IsPostable = r.IsPostable,
                    IsActive = r.IsActive,
                    Level = r.Level,
                };
                result.Add((acc, balance));
            }
        }
        return result;
    }

    private async Task<Account> EnsurePnLAccountAsync(Guid companyId, CancellationToken ct)
    {
        // Try to find 3210 (Current Year P&L)
        var existing = await _accounts.GetByCodeAsync("3210", companyId, ct);
        if (existing != null) return existing;

        // Create it as a child of 3200 (Retained Earnings) so the hierarchy is preserved
        var parent = await _accounts.GetByCodeAsync("3200", companyId, ct);
        if (parent == null)
            throw new InvalidOperationException("Cannot find 3200 (Retained Earnings) to use as parent for 3210.");

        var newAccount = new Account
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = "3210",
            Name = "ملخّص الدخل (Current Year P&L)",
            Type = AccountType.Equity,
            NormalBalance = NormalBalance.Credit,
            ParentAccountId = parent.Id,
            IsPostable = true,
            IsActive = true,
            Level = 3,
        };
        await _accounts.InsertAsync(newAccount, ct);
        _logger.LogInformation("[Sprint53] Created account 3210 (Current Year P&L) for company {CompanyId}", companyId);
        return newAccount;
    }

    private async Task<Account> GetRetainedEarningsAccountAsync(Guid companyId, CancellationToken ct)
    {
        var acc = await _accounts.GetByCodeAsync("3200", companyId, ct);
        if (acc == null)
            throw new InvalidOperationException("Cannot find 3200 (Retained Earnings).");
        return acc;
    }

    private sealed class AccountBalRow
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public int NormalBalance { get; set; }
        public Guid? ParentAccountId { get; set; }
        public bool IsIntercompany { get; set; }
        public bool IsPostable { get; set; }
        public bool IsActive { get; set; }
        public short? Level { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }
}
