using Dapper;
using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IGeneralLedgerReportService
{
    Task<FinanceResult<GeneralLedgerReportResponse>> GetAccountLedgerAsync(
        Guid companyId, Guid accountId, DateTime? from, DateTime? to, CancellationToken ct);

    // Sprint 48 (DEC-130): Balance Sheet — Σ Assets = Σ Liab + Σ Equity
    Task<FinanceResult<BalanceSheetResponse>> GetBalanceSheetAsync(
        Guid companyId, DateTime asOfDate, CancellationToken ct);

    // Sprint 48 (DEC-131): Income Statement — Revenue − Expenses = Net Income
    Task<FinanceResult<IncomeStatementResponse>> GetIncomeStatementAsync(
        Guid companyId, DateTime from, DateTime to, CancellationToken ct);

    // Sprint 48 (DEC-132): Cash Flow (Indirect) — Operating + Investing + Financing = Net Change
    Task<FinanceResult<CashFlowResponse>> GetCashFlowAsync(
        Guid companyId, DateTime from, DateTime to, CancellationToken ct);
}

/// <summary>
/// General Ledger Report (per-account).
///
/// يُرجع كل سطور القيد المحاسبي على حساب معيّن (حالة Posted) في فترة اختيارية،
/// مع رصيد جارٍ بحسب NormalBalance (Dr: +debit-credit، Cr: +credit-debit).
///
/// Opening balance = مجموع الحركات قبل `from` (لو from=null، الافتتاح = 0).
/// Closing = Opening + TotalDebit - TotalCredit (Debit-normal accounts)
/// أو Opening + TotalCredit - TotalDebit (Credit-normal).
/// </summary>
public sealed class GeneralLedgerReportService : IGeneralLedgerReportService
{
    private readonly IDbConnectionFactory _db;
    private readonly IAccountRepository _accounts;

    public GeneralLedgerReportService(IDbConnectionFactory db, IAccountRepository accounts)
    {
        _db = db; _accounts = accounts;
    }

    public async Task<FinanceResult<GeneralLedgerReportResponse>> GetAccountLedgerAsync(
        Guid companyId, Guid accountId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(accountId, ct);
        // Sprint 38 (DEC-124): L19 check on account lookup
        if (account == null || account.CompanyId != companyId)
            return FinanceResult<GeneralLedgerReportResponse>.Fail("الحساب غير موجود.", FinanceErrorCode.NotFound);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 1) Opening Balance — مجموع الحركات على الحساب قبل `from` (Posted only)
        // Sprint 38: added L19 filter on journal_lines + journal_entries
        decimal opening = 0m;
        if (from.HasValue)
        {
            var openingSql = @"
                SELECT COALESCE(SUM(jl.debit), 0) AS Dr, COALESCE(SUM(jl.credit), 0) AS Cr
                FROM journal_lines jl
                INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                    AND je.company_id = @CompanyId
                WHERE jl.account_id = @AccountId
                  AND jl.company_id = @CompanyId
                  AND je.status = 2 AND je.entry_date < @From";
            var opRow = await conn.QueryFirstOrDefaultAsync<(decimal Dr, decimal Cr)>(
                new CommandDefinition(openingSql,
                    new { CompanyId = companyId, AccountId = accountId, From = from.Value },
                    cancellationToken: ct));
            opening = account.NormalBalance == NormalBalance.Debit ? (opRow.Dr - opRow.Cr) : (opRow.Cr - opRow.Dr);
        }

        // 2) Period lines — Sprint 38: L19 filter on journal_lines + journal_entries
        var p = new DynamicParameters();
        p.Add("CompanyId", companyId);
        p.Add("AccountId", accountId);
        var sql = @"
            SELECT je.entry_date AS EntryDate, je.entry_number AS EntryNumber, je.id AS JournalEntryId,
                   je.reference, je.description AS EntryDescription,
                   jl.debit AS Debit, jl.credit AS Credit
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = @CompanyId
            WHERE jl.account_id = @AccountId
              AND jl.company_id = @CompanyId
              AND je.status = 2";
        if (from.HasValue) { sql += " AND je.entry_date >= @From"; p.Add("From", from.Value); }
        if (to.HasValue) { sql += " AND je.entry_date <= @To"; p.Add("To", to.Value); }
        sql += " ORDER BY je.entry_date, je.entry_number, jl.line_number";

        var rows = (await conn.QueryAsync<LedgerRow>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();

        decimal running = opening;
        decimal totalDr = 0m, totalCr = 0m;
        var lines = new List<GeneralLedgerLineResponse>();
        foreach (var r in rows)
        {
            totalDr += r.Debit; totalCr += r.Credit;
            var delta = account.NormalBalance == NormalBalance.Debit
                ? r.Debit - r.Credit
                : r.Credit - r.Debit;
            running += delta;
            lines.Add(new GeneralLedgerLineResponse
            {
                EntryDate = r.EntryDate,
                EntryNumber = r.EntryNumber,
                JournalEntryId = r.JournalEntryId,
                Reference = r.Reference,
                EntryDescription = r.EntryDescription,
                AccountCode = account.Code,
                AccountName = account.Name,
                Debit = r.Debit,
                Credit = r.Credit,
                RunningBalance = running
            });
        }

        return FinanceResult<GeneralLedgerReportResponse>.Ok(new GeneralLedgerReportResponse
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            AccountTypeName = account.Type.ToString(),
            From = from,
            To = to,
            OpeningBalance = opening,
            TotalDebit = totalDr,
            TotalCredit = totalCr,
            ClosingBalance = running,
            Lines = lines
        });
    }

    private sealed class LedgerRow
    {
        public DateTime EntryDate { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public Guid JournalEntryId { get; set; }
        public string? Reference { get; set; }
        public string EntryDescription { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    // ============== Sprint 48 (DEC-130) Balance Sheet ==============
    /// <summary>
    /// الميزانية العمومية — لقطة في تاريخ.
    /// Σ الأصول = Σ الالتزامات + Σ حقوق الملكية (المعادلة المحاسبية الأساسية).
    /// AccountType: 1=Asset, 2=Liability, 3=Equity (من Account.cs enum).
    /// للأصول: رصيد = debit − credit (Dr normal).
    /// للالتزامات وحقوق الملكية: رصيد = credit − debit (Cr normal).
    /// L19: company_id filter على كل الـ JOINs.
    /// </summary>
    public async Task<FinanceResult<BalanceSheetResponse>> GetBalanceSheetAsync(
        Guid companyId, DateTime asOfDate, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // نجلب كل الحسابات (postable = ParentId != null في النموذج، أو Leaves فقط) مع رصيدها
        // نشترط: الحساب مُرحَّل إليه (balance != 0) في تاريخ asOfDate
        // نعتمد على journal_lines.debit/credit للشركات
        const string sql = @"
            SELECT a.id AS AccountId, a.code AS AccountCode, a.name AS AccountName,
                   a.type AS AccountType, a.normal_balance AS NormalBalance,
                   COALESCE(SUM(jl.debit), 0) AS TotalDebit,
                   COALESCE(SUM(jl.credit), 0) AS TotalCredit
            FROM accounts a
            LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
            LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = a.company_id
                AND je.status = 2 AND je.entry_date <= @AsOfDate
            WHERE a.company_id = @CompanyId
              AND a.is_postable = true
              AND a.is_active = true
              AND a.type IN (1, 2, 3)
            GROUP BY a.id, a.code, a.name, a.type, a.normal_balance
            ORDER BY a.code";

        var rows = (await conn.QueryAsync<BSRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, AsOfDate = asOfDate.Date },
            cancellationToken: ct))).ToList();

        var resp = new BalanceSheetResponse { AsOfDate = asOfDate.Date };

        foreach (var r in rows)
        {
            // الرصيد بحسب NormalBalance
            var balance = r.NormalBalance == 1 ? (r.TotalDebit - r.TotalCredit) : (r.TotalCredit - r.TotalDebit);
            if (Math.Abs(balance) < 0.005m) continue; // skip صفر

            var row = new BalanceSheetRow
            {
                AccountId = r.AccountId,
                AccountCode = r.AccountCode,
                AccountName = r.AccountName,
                Balance = balance
            };

            switch (r.AccountType)
            {
                case 1: resp.Assets.Rows.Add(row); break;
                case 2: resp.Liabilities.Rows.Add(row); break;
                case 3: resp.Equity.Rows.Add(row); break;
            }
        }

        // Sprint 52a: نضيف NetIncome (من أول السنة لتاريخ asOfDate) كصف افتراضي في قسم Equity.
        // السبب: المعادلة المحاسبية Σ Assets = Σ Liab + Σ Equity + NetIncome.
        // بدون إضافة NetIncome للقسم، الـ BS دايماً "غير متوازن" طالما ما تم إغلاق السنة.
        // الحساب: NetIncome = Σ Revenue (Cr) − Σ Expenses (Dr) لنفس الفترة.
        var yearStart = new DateTime(asOfDate.Year, 1, 1);
        var plResult = await GetIncomeStatementAsync(companyId, yearStart, asOfDate, ct);
        if (plResult.Succeeded)
        {
            var netIncome = plResult.Value!.NetIncome;
            if (Math.Abs(netIncome) >= 0.005m)
            {
                resp.Equity.Rows.Add(new BalanceSheetRow
                {
                    AccountId = Guid.Empty, // synthetic row, not a real account
                    AccountCode = "NET",
                    AccountName = $"صافي دخل السنة ({asOfDate.Year}) — لم يُرحَّل بعد",
                    Balance = netIncome,
                });
            }
        }

        resp.TotalAssets = resp.Assets.Subtotal;
        resp.TotalLiabilities = resp.Liabilities.Subtotal;
        resp.TotalEquity = resp.Equity.Subtotal;

        return FinanceResult<BalanceSheetResponse>.Ok(resp);
    }

    // ============== Sprint 48 (DEC-131) Income Statement (P&L) ==============
    /// <summary>
    /// قائمة الدخل لفترة: Revenue − Expenses = Net Income.
    /// AccountType: 4=Revenue, 5=Expense.
    /// Revenue normal: Credit (credit − debit) → موجب.
    /// Expense normal: Debit (debit − credit) → موجب.
    /// </summary>
    public async Task<FinanceResult<IncomeStatementResponse>> GetIncomeStatementAsync(
        Guid companyId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        const string sql = @"
            SELECT a.id AS AccountId, a.code AS AccountCode, a.name AS AccountName,
                   a.type AS AccountType, a.normal_balance AS NormalBalance,
                   COALESCE(SUM(jl.debit), 0) AS TotalDebit,
                   COALESCE(SUM(jl.credit), 0) AS TotalCredit
            FROM accounts a
            LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
            LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = a.company_id
                AND je.status = 2
                AND je.entry_date >= @From AND je.entry_date <= @To
            WHERE a.company_id = @CompanyId
              AND a.is_postable = true
              AND a.is_active = true
              AND a.type IN (4, 5)
            GROUP BY a.id, a.code, a.name, a.type, a.normal_balance
            ORDER BY a.code";

        var rows = (await conn.QueryAsync<BSRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from.Date, To = to.Date },
            cancellationToken: ct))).ToList();

        var resp = new IncomeStatementResponse { From = from.Date, To = to.Date };

        foreach (var r in rows)
        {
            var amount = r.NormalBalance == 1 ? (r.TotalDebit - r.TotalCredit) : (r.TotalCredit - r.TotalDebit);
            if (Math.Abs(amount) < 0.005m) continue;

            var row = new IncomeStatementRow
            {
                AccountId = r.AccountId,
                AccountCode = r.AccountCode,
                AccountName = r.AccountName,
                Amount = amount
            };

            if (r.AccountType == 4) resp.Revenue.Rows.Add(row);
            else resp.Expenses.Rows.Add(row);
        }

        return FinanceResult<IncomeStatementResponse>.Ok(resp);
    }

    // ============== Sprint 48 (DEC-132) Cash Flow (Indirect) ==============
    /// <summary>
    /// التدفقات النقدية (الطريقة غير المباشرة):
    ///   Operating = NetIncome + Depreciation - ΔAR + ΔInventory + ΔAP
    ///   Investing = -ΔFixedAssets (Δ = closing − opening)
    ///   Financing = +ΔLoans + ΔCapital
    ///   NetChange = Operating + Investing + Financing
    ///
    /// نقارن في النهاية NetChange مع رصيد النقد (cash accounts) في أول وآخر الفترة.
    /// </summary>
    public async Task<FinanceResult<CashFlowResponse>> GetCashFlowAsync(
        Guid companyId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 1) Net income من قائمة الدخل
        var pl = await GetIncomeStatementAsync(companyId, from, to, ct);
        if (!pl.Succeeded) return FinanceResult<CashFlowResponse>.Fail(pl.Error ?? "خطأ غير معروف", FinanceErrorCode.ValidationError);
        var netIncome = pl.Value!.NetIncome;

        // 2) Δ الأرصدة لفئات حسابات معينة (من رصيد الافتتاح إلى رصيد الإقفال)
        // 1100-1199: Cash & Bank (نستبعد من الحسابات — نقيس الفرق الفعلي)
        // 1200-1299: AR (Δ = closing − opening) — زيادة AR = cash outflow
        // 1300-1399: Inventory — زيادة = outflow
        // 1400-1499: Prepaid + Other CA — outflow
        // 1500-1599: Fixed Assets — outflow
        // 2100-2199: AP (Δ closing − opening) — زيادة AP = cash inflow
        // 2200-2299: Accrued + Other CL — inflow
        // 3100-3199: Loans — inflow
        // 3200-3299: Capital + Drawings — inflow

        // حساب الأرصدة الافتتاحية (قبل from) والإقفال (≤ to)
        async Task<Dictionary<int, decimal>> GetBalancesByCodePrefixAsync(string prefix, DateTime asOf)
        {
            var p = new DynamicParameters();
            p.Add("CompanyId", companyId);
            p.Add("AsOf", asOf.Date);
            var q = @"
                SELECT a.id AS AccountId, a.code AS AccountCode, a.type AS AccountType,
                       a.normal_balance AS NormalBalance,
                       COALESCE(SUM(jl.debit), 0) AS TotalDebit,
                       COALESCE(SUM(jl.credit), 0) AS TotalCredit
                FROM accounts a
                LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
                LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                    AND je.company_id = a.company_id AND je.status = 2
                    AND je.entry_date <= @AsOf
                WHERE a.company_id = @CompanyId AND a.is_postable = true AND a.is_active = true
                  AND a.code LIKE @Prefix
                GROUP BY a.id, a.code, a.type, a.normal_balance";
            p.Add("Prefix", prefix + "%");
            var list = (await conn.QueryAsync<BSRow>(new CommandDefinition(q, p, cancellationToken: ct))).ToList();
            var map = new Dictionary<int, decimal>();
            foreach (var r in list)
            {
                var bal = r.NormalBalance == 1 ? (r.TotalDebit - r.TotalCredit) : (r.TotalCredit - r.TotalDebit);
                map[(int)r.AccountType] = map.GetValueOrDefault((int)r.AccountType, 0m) + bal;
            }
            return map;
        }

        // للأكواد المختلطة: نُبسّط — نستخدم prefix بحسب نوع الحساب في CoA
        // افتراض CoA:
        //   11xx: Cash & Bank (Type=1 Asset)
        //   12xx: AR (Type=1 Asset)
        //   13xx: Inventory (Type=1 Asset)
        //   15xx: Fixed Assets (Type=1 Asset)
        //   21xx: AP (Type=2 Liability)
        //   22xx: Accrued (Type=2 Liability)
        //   23xx: Short-term loans (Type=2 Liability)
        //   31xx: Capital (Type=3 Equity)
        //   32xx: Retained earnings + Drawings (Type=3 Equity)

        // حساب Δ لرصيد النقد (Cash & Bank فقط — الكود 11xx)
        async Task<decimal> GetCashBalanceAsync(DateTime asOf)
        {
            var p = new DynamicParameters();
            p.Add("CompanyId", companyId);
            p.Add("AsOf", asOf.Date);
            const string q = @"
                SELECT a.id AS AccountId, a.normal_balance AS NormalBalance,
                       COALESCE(SUM(jl.debit), 0) AS TotalDebit,
                       COALESCE(SUM(jl.credit), 0) AS TotalCredit
                FROM accounts a
                LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
                LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                    AND je.company_id = a.company_id AND je.status = 2
                    AND je.entry_date <= @AsOf
                WHERE a.company_id = @CompanyId AND a.is_postable = true AND a.is_active = true
                  AND a.code LIKE '11%'
                GROUP BY a.id, a.normal_balance";
            var list = (await conn.QueryAsync<BSRow>(new CommandDefinition(q, p, cancellationToken: ct))).ToList();
            decimal sum = 0m;
            foreach (var r in list)
            {
                var bal = r.NormalBalance == 1 ? (r.TotalDebit - r.TotalCredit) : (r.TotalCredit - r.TotalDebit);
                sum += bal;
            }
            return sum;
        }

        async Task<decimal> GetPrefixBalanceAsync(string prefix, DateTime asOf)
        {
            var p = new DynamicParameters();
            p.Add("CompanyId", companyId);
            p.Add("AsOf", asOf.Date);
            p.Add("Prefix", prefix + "%");
            const string q = @"
                SELECT a.id AS AccountId, a.normal_balance AS NormalBalance,
                       COALESCE(SUM(jl.debit), 0) AS TotalDebit,
                       COALESCE(SUM(jl.credit), 0) AS TotalCredit
                FROM accounts a
                LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
                LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                    AND je.company_id = a.company_id AND je.status = 2
                    AND je.entry_date <= @AsOf
                WHERE a.company_id = @CompanyId AND a.is_postable = true AND a.is_active = true
                  AND a.code LIKE @Prefix
                GROUP BY a.id, a.normal_balance";
            var list = (await conn.QueryAsync<BSRow>(new CommandDefinition(q, p, cancellationToken: ct))).ToList();
            decimal sum = 0m;
            foreach (var r in list)
            {
                var bal = r.NormalBalance == 1 ? (r.TotalDebit - r.TotalCredit) : (r.TotalCredit - r.TotalDebit);
                sum += bal;
            }
            return sum;
        }

        // الافتتاح = يوم قبل from
        var openDate = from.Date.AddDays(-1);

        // Depreciation expense (5xxx) in period — مصروف إهلاك (افتراض كود يبدأ 53)
        var depreciationExpense = await GetPrefixBalanceAsync("53", to.Date);
        var depreciationOpen = await GetPrefixBalanceAsync("53", openDate);
        var depreciationPeriod = depreciationExpense - depreciationOpen;

        // ΔAR (12xx): زيادة AR = cash outflow (negative operating)
        var arOpen = await GetPrefixBalanceAsync("12", openDate);
        var arClose = await GetPrefixBalanceAsync("12", to.Date);
        var deltaAR = arClose - arOpen;

        // ΔInventory (13xx)
        var invOpen = await GetPrefixBalanceAsync("13", openDate);
        var invClose = await GetPrefixBalanceAsync("13", to.Date);
        var deltaInv = invClose - invOpen;

        // ΔAP (21xx): زيادة AP = cash inflow
        var apOpen = await GetPrefixBalanceAsync("21", openDate);
        var apClose = await GetPrefixBalanceAsync("21", to.Date);
        var deltaAP = apClose - apOpen;

        // ΔFixed Assets (15xx) — زيادة = cash outflow (investing)
        var faOpen = await GetPrefixBalanceAsync("15", openDate);
        var faClose = await GetPrefixBalanceAsync("15", to.Date);
        var deltaFA = faClose - faOpen;

        // ΔLoans (23xx) — زيادة = cash inflow (financing)
        var loanOpen = await GetPrefixBalanceAsync("23", openDate);
        var loanClose = await GetPrefixBalanceAsync("23", to.Date);
        var deltaLoans = loanClose - loanOpen;

        // ΔCapital (31xx) — Drawings تُخصم (افتراض كود 33xx Drawings يُخصم)
        var capOpen = await GetPrefixBalanceAsync("31", openDate);
        var capClose = await GetPrefixBalanceAsync("31", to.Date);
        var deltaCapital = capClose - capOpen;

        var drwOpen = await GetPrefixBalanceAsync("33", openDate);
        var drwClose = await GetPrefixBalanceAsync("33", to.Date);
        var deltaDrawings = drwClose - drwOpen; // Debit-normal — زيادة = مالك سحب

        // Cash open/close
        var cashOpen = await GetCashBalanceAsync(openDate);
        var cashClose = await GetCashBalanceAsync(to.Date);
        var netChangeInCashActual = cashClose - cashOpen;

        // حساب التدفقات
        var operating = new CashFlowSection { Title = "الأنشطة التشغيلية" };
        operating.Lines.Add(new CashFlowLine { Description = "صافي الدخل", Amount = netIncome });
        if (Math.Abs(depreciationPeriod) >= 0.005m)
            operating.Lines.Add(new CashFlowLine { Description = "إهلاك الفترة", Amount = depreciationPeriod });
        if (Math.Abs(deltaAR) >= 0.005m)
            operating.Lines.Add(new CashFlowLine { Description = "التغير في المدينين", Amount = -deltaAR });
        if (Math.Abs(deltaInv) >= 0.005m)
            operating.Lines.Add(new CashFlowLine { Description = "التغير في المخزون", Amount = -deltaInv });
        if (Math.Abs(deltaAP) >= 0.005m)
            operating.Lines.Add(new CashFlowLine { Description = "التغير في الدائنين", Amount = deltaAP });

        var investing = new CashFlowSection { Title = "الأنشطة الاستثمارية" };
        if (Math.Abs(deltaFA) >= 0.005m)
            investing.Lines.Add(new CashFlowLine { Description = "شراء/بيع أصول ثابتة", Amount = -deltaFA });

        var financing = new CashFlowSection { Title = "أنشطة التمويل" };
        if (Math.Abs(deltaLoans) >= 0.005m)
            financing.Lines.Add(new CashFlowLine { Description = "صافي القروض", Amount = deltaLoans });
        if (Math.Abs(deltaCapital) >= 0.005m)
            financing.Lines.Add(new CashFlowLine { Description = "صافي رأس المال", Amount = deltaCapital });
        if (Math.Abs(deltaDrawings) >= 0.005m)
            financing.Lines.Add(new CashFlowLine { Description = "مسحوبات المالك", Amount = -deltaDrawings });

        var resp = new CashFlowResponse
        {
            From = from.Date,
            To = to.Date,
            Operating = operating,
            Investing = investing,
            Financing = financing
        };

        return FinanceResult<CashFlowResponse>.Ok(resp);
    }

    /// <summary>صف مساعد لـ BS / IS / CashFlow — يحتوي على الحقول المشتركة</summary>
    private sealed class BSRow
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int AccountType { get; set; }
        public int NormalBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }
}
