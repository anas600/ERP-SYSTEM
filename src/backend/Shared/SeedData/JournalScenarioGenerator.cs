using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Sprint 50 — مولِّد سيناريو القيود اليومي.
/// يولّد ~500 قيد للـ Holding و ~200 لكل شركة فرعية على فترة 18 شهر.
/// كل قيد متوازن (مدين = دائن).
/// </summary>
public sealed class JournalScenarioGenerator
{
    private readonly IDbConnection _conn;
    private readonly Guid _companyId;
    private readonly Dictionary<string, Guid> _accounts;
    private readonly Dictionary<string, Guid> _customers;
    private readonly Dictionary<string, Guid> _vendors;
    private readonly Dictionary<string, Guid> _items;
    private readonly bool _isHolding;
    private readonly int _targetEntries;
    private readonly Guid _systemUserId;
    private readonly ILogger _logger;
    private readonly CancellationToken _ct;

    private int _entryCounter = 1;
    private string _currentYear = "2025";
    private readonly Random _rand = new(42); // Seed ثابت للـ reproducibility

    // Sprint 51: تتبع الفواتير اللي ما اندفعت (للـ AP aging)
    private int _billCounter = 0;
    private readonly HashSet<int> _unpaidBills = new();

    public JournalScenarioGenerator(
        IDbConnection conn, Guid companyId,
        Dictionary<string, Guid> accounts,
        Dictionary<string, Guid> customers,
        Dictionary<string, Guid> vendors,
        Dictionary<string, Guid> items,
        bool isHolding, int targetEntries,
        Guid systemUserId,
        ILogger logger, CancellationToken ct)
    {
        _conn = conn; _companyId = companyId;
        _accounts = accounts; _customers = customers; _vendors = vendors; _items = items;
        _isHolding = isHolding; _targetEntries = targetEntries; _systemUserId = systemUserId;
        _logger = logger; _ct = ct;
    }

    public async Task<int> RunAsync()
    {
        // عدد القيود الشهرية المطلوبة
        var monthsSpan = 18; // 2025-01 → 2026-06
        var perMonth = _targetEntries / monthsSpan;
        if (perMonth < 1) perMonth = 1;

        // 1) Opening Balance (في 2025-01-01)
        await SeedOpeningBalanceAsync();
        var totalEntries = 1;

        // 2) 18 شهر من المعاملات
        for (int year = 2025; year <= 2026; year++)
        {
            int monthStart = (year == 2025) ? 1 : 1;
            int monthEnd = (year == 2026) ? 6 : 12;
            for (int month = monthStart; month <= monthEnd; month++)
            {
                _currentYear = year.ToString();
                for (int i = 0; i < perMonth - 1; i++) // -1 لأن opening balance في أول شهر
                {
                    await SeedMonthlyTransactionAsync(year, month, i);
                    totalEntries++;
                    if (totalEntries >= _targetEntries) break;
                }
                if (totalEntries >= _targetEntries) break;
            }
            if (totalEntries >= _targetEntries) break;
        }

        // 3) Sprint 51: إقفال نهاية السنة 2025 — ينقل صافي الدخل إلى 3210 (Current Year P&L)
        await SeedYearEndClosingAsync(2025, totalEntries);
        return totalEntries;
    }

    /// <summary>
    /// Sprint 51: إقفال نهاية السنة.
    /// ينقل رصيد كل حسابات الإيرادات والمصروفات إلى 3210 (Current Year P&L).
    /// بعد الإقفال: Σ حسابات الإيرادات = 0، Σ حسابات المصروفات = 0، 3210 = NetIncome.
    /// </summary>
    private async Task SeedYearEndClosingAsync(int year, int totalEntriesSoFar)
    {
        if (!_accounts.ContainsKey("3210")) return;

        // 1) استعلام: رصيد كل حساب إيراد ومصروف في هذه السنة
        var sql = @"
            SELECT a.id, a.code, a.name, a.normal_balance,
                   COALESCE(SUM(jl.debit), 0) AS Dr, COALESCE(SUM(jl.credit), 0) AS Cr
            FROM accounts a
            LEFT JOIN journal_lines jl ON jl.account_id = a.id AND jl.company_id = a.company_id
            LEFT JOIN journal_entries je ON je.id = jl.journal_entry_id
                AND je.company_id = a.company_id AND je.status = 2
                AND je.entry_date >= @YearStart AND je.entry_date < @YearEndExclusive
            WHERE a.company_id = @CompanyId AND a.is_postable = true AND a.is_active = true
              AND a.type IN (4, 5)
            GROUP BY a.id, a.code, a.name, a.normal_balance";

        var rows = (await _conn.QueryAsync<(Guid Id, string Code, string Name, int NormalBalance, decimal Dr, decimal Cr)>(
            new CommandDefinition(sql,
                new { CompanyId = _companyId, YearStart = new DateTime(year, 1, 1), YearEndExclusive = new DateTime(year + 1, 1, 1) },
                cancellationToken: _ct))).ToList();

        if (rows.Count == 0) return;

        var lines = new List<(string, decimal, decimal, string)>();
        decimal netIncome = 0m;

        foreach (var r in rows)
        {
            // رصيد الحساب في هذه السنة بحسب NormalBalance
            var balance = r.NormalBalance == 1 ? (r.Dr - r.Cr) : (r.Cr - r.Dr);
            if (Math.Abs(balance) < 0.01m) continue;

            if (r.Code == "3210") continue; // تخطي 3210 نفسه (يأخذ الفارق)

            // لإقفال الحساب: عكس الرصيد
            // Revenue (Cr normal, balance موجب): DR balance (يصفّر الرصيد)
            // Expense (Dr normal, balance موجب): CR balance (يصفّر الرصيد)
            if (r.NormalBalance == 2) // Revenue (Cr)
            {
                lines.Add((r.Code, balance, 0m, $"إقفال {r.Name} — نهاية {year}"));
                netIncome += balance; // صافي موجب
            }
            else // Expense (Dr)
            {
                lines.Add((r.Code, 0m, balance, $"إقفال {r.Name} — نهاية {year}"));
                netIncome -= balance; // يطرح من صافي
            }
        }

        if (lines.Count == 0) return;

        // 2) إضافة 3210 (صافي الدخل)
        if (netIncome > 0)
        {
            // ربح → CR 3210
            lines.Add(("3210", 0m, netIncome, $"صافي دخل {year}"));
        }
        else if (netIncome < 0)
        {
            // خسارة → DR 3210
            lines.Add(("3210", -netIncome, 0m, $"خسارة {year}"));
        }

        // 3) تحقق من التوازن
        decimal totalDr = lines.Sum(l => l.Item2);
        decimal totalCr = lines.Sum(l => l.Item3);
        if (Math.Abs(totalDr - totalCr) > 0.01m)
        {
            _logger.LogWarning("[SPRINT-51] Year-end closing unbalanced: Dr={Dr} Cr={Cr}", totalDr, totalCr);
            return;
        }

        var entryNo = $"CL-{year}-{_entryCounter++:D4}";
        var entryDate = new DateTime(year, 12, 31);
        await PostJournalEntryAsync(entryNo, entryDate, $"إقفال السنة المالية {year}", lines);
        _logger.LogInformation("[SPRINT-51] {Company} Year-end closing for {Year}: NetIncome={NI}, lines={Lines}",
            _companyId, year, netIncome, lines.Count);
    }

    // ============== Opening Balance (رأس المال + قرض + أصول) ==============
    private async Task SeedOpeningBalanceAsync()
    {
        var entryDate = new DateTime(2025, 1, 1);
        // رأس مال 200,000 + قرض 100,000 + أصول ثابتة 80,000 = 380,000
        // (Cash=200K, Fixed Assets=80K, Capital=200K, Loan=100K, AR=20K) → balanced

        var lines = new List<(string AccountCode, decimal Debit, decimal Credit, string Desc)>
        {
            ("1100", 200000m, 0m, "رأس مال نقدي في الصندوق"),
            ("1110", 50000m, 0m, "رصيد افتتاحي في البنك"),
            ("1200", 20000m, 0m, "ذمم مدينة قائمة"),
            ("1520", 80000m, 0m, "أجهزة ومعدات"),
            ("3100", 0m, 200000m, "رأس مال الشركة"),
            ("2300", 0m, 100000m, "قرض بنكي قصير الأجل"),
            ("2100", 0m, 50000m, "ذمم دائنة (موردين)"),
        };
        await PostJournalEntryAsync("JE-2025-0001", entryDate, "رصيد افتتاحي 2025-01-01 — رأس مال + قرض + أصول", lines);
    }

    // ============== Monthly Transactions ==============
    private async Task SeedMonthlyTransactionAsync(int year, int month, int idx)
    {
        // نمط دوري: 0=مبيعات, 1=تحصيل, 2=مشتريات, 3=مدفوعات, 4=رواتب, 5=إيجار, 6=كهرباء, 7=إهلاك, 8=فوائد, 9=VAT settlement
        var day = Math.Min(28, 5 + (idx * 3) % 25);
        var entryDate = new DateTime(year, month, day);

        switch (idx % 10)
        {
            case 0: await SeedSalesInvoiceAsync(entryDate, idx); break;
            case 1: await SeedCustomerReceiptAsync(entryDate); break;
            case 2: await SeedVendorBillAsync(entryDate, idx); break;
            case 3: await SeedVendorPaymentAsync(entryDate); break;
            case 4: await SeedSalariesAsync(entryDate); break;
            case 5: await SeedRentAsync(entryDate); break;
            case 6: await SeedUtilitiesAsync(entryDate); break;
            case 7: await SeedDepreciationAsync(entryDate); break;
            case 8: await SeedBankInterestAsync(entryDate); break;
            case 9: await SeedVATSettlementAsync(year, month); break;
        }
    }

    // 0) فاتورة مبيعات
    private async Task SeedSalesInvoiceAsync(DateTime date, int idx)
    {
        var amount = 8000m + _rand.Next(0, 12000); // 8K-20K
        var vat = Math.Round(amount * 0.05m, 3);
        var customerKey = "C00" + (1 + (idx % 5));
        var itemKey = "IT-00" + (1 + (idx % 5));

        if (!_customers.ContainsKey(customerKey) || !_accounts.ContainsKey("1200") || !_accounts.ContainsKey("4110") || !_accounts.ContainsKey("2220"))
            return;

        var entryNo = $"SI-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("1200", amount + vat, 0m, $"مبيعات للعميل {customerKey} — {entryNo}"),
            ("4110", 0m, amount, "إيراد المبيعات"),
            ("2220", 0m, vat, "ضريبة القيمة المضافة 5%"),
        };
        await PostJournalEntryAsync(entryNo, date, $"فاتورة مبيعات {entryNo}", lines);
    }

    // 1) تحصيل من عميل
    private async Task SeedCustomerReceiptAsync(DateTime date)
    {
        var amount = 5000m + _rand.Next(0, 15000);
        if (!_accounts.ContainsKey("1100") || !_accounts.ContainsKey("1200")) return;
        var entryNo = $"RV-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("1100", amount, 0m, "تحصيل نقدي"),
            ("1200", 0m, amount, "تخفيض الذمم المدينة"),
        };
        await PostJournalEntryAsync(entryNo, date, $"سند قبض {entryNo}", lines);
    }

    // 2) فاتورة مورد
    private async Task SeedVendorBillAsync(DateTime date, int idx)
    {
        var amount = 4000m + _rand.Next(0, 10000);
        var vat = Math.Round(amount * 0.05m, 3);
        var vendorKey = "V00" + (1 + (idx % 4));
        if (!_accounts.ContainsKey("2100") || !_accounts.ContainsKey("1300") || !_accounts.ContainsKey("5100") || !_accounts.ContainsKey("1420"))
            return;
        var entryNo = $"VB-{_currentYear}-{_entryCounter++:D4}";
        // 50% بضاعة + 50% مصروف (للتبسيط)
        var inventory = Math.Round(amount * 0.6m, 3);
        var cogs = amount - inventory;
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("1300", inventory, 0m, "مشتريات بضاعة"),
            ("5100", cogs, 0m, "تكلفة خدمات"),
            ("1420", vat, 0m, "ضريبة مدخلة"),
            ("2100", 0m, amount + vat, $"فاتورة مورد {vendorKey}"),
        };
        await PostJournalEntryAsync(entryNo, date, $"فاتورة مشتريات {entryNo}", lines);
        // Sprint 51: ~30% من الفواتير تبقى unpaid (للـ AP aging)
        _billCounter++;
        if (_rand.Next(100) < 30)
        {
            _unpaidBills.Add(_billCounter);
        }
    }

    // 3) مدفوعات لمورد
    private async Task SeedVendorPaymentAsync(DateTime date)
    {
        // Sprint 51: ~30% من المدفوعات تُؤجل (لا تُنفذ) — ينتج AP aging حقيقي
        if (_rand.Next(100) < 30) return;

        var amount = 3000m + _rand.Next(0, 10000);
        if (!_accounts.ContainsKey("1100") || !_accounts.ContainsKey("2100")) return;
        var entryNo = $"PV-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("2100", amount, 0m, "سداد ذمم دائنة"),
            ("1100", 0m, amount, "صرف نقدي"),
        };
        await PostJournalEntryAsync(entryNo, date, $"سند دفع {entryNo}", lines);
    }

    // 4) رواتب
    private async Task SeedSalariesAsync(DateTime date)
    {
        var amount = _isHolding ? 35000m : 18000m;
        if (!_accounts.ContainsKey("1100") || !_accounts.ContainsKey("5200") || !_accounts.ContainsKey("5210")) return;
        var entryNo = $"PAY-{_currentYear}-{_entryCounter++:D4}";
        var salaries = Math.Round(amount * 0.85m, 3);
        var insurance = amount - salaries;
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("5200", salaries, 0m, "رواتب صافية"),
            ("5210", insurance, 0m, "تأمينات اجتماعية"),
            ("1100", 0m, amount, "صرف رواتب"),
        };
        await PostJournalEntryAsync(entryNo, date, $"رواتب شهر — {entryNo}", lines);
    }

    // 5) إيجار
    private async Task SeedRentAsync(DateTime date)
    {
        var amount = _isHolding ? 5000m : 2500m;
        if (!_accounts.ContainsKey("1100") || !_accounts.ContainsKey("5400")) return;
        var entryNo = $"RNT-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("5400", amount, 0m, "إيجار شهري"),
            ("1100", 0m, amount, "صرف إيجار"),
        };
        await PostJournalEntryAsync(entryNo, date, $"إيجار — {entryNo}", lines);
    }

    // 6) كهرباء + اتصالات
    private async Task SeedUtilitiesAsync(DateTime date)
    {
        var electricity = _isHolding ? 800m : 400m;
        var telecom = _isHolding ? 300m : 200m;
        if (!_accounts.ContainsKey("1100") || !_accounts.ContainsKey("5410") || !_accounts.ContainsKey("5420")) return;
        var entryNo = $"UTL-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("5410", electricity, 0m, "كهرباء وماء"),
            ("5420", telecom, 0m, "اتصالات"),
            ("1100", 0m, electricity + telecom, "صرف فواتير خدمات"),
        };
        await PostJournalEntryAsync(entryNo, date, $"خدمات — {entryNo}", lines);
    }

    // 7) إهلاك شهري
    private async Task SeedDepreciationAsync(DateTime date)
    {
        if (!_accounts.ContainsKey("5310") || !_accounts.ContainsKey("1610")) return;
        var amount = _isHolding ? 1500m : 700m;
        var entryNo = $"DEP-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("5310", amount, 0m, "مصروف إهلاك شهري"),
            ("1610", 0m, amount, "مجمع إهلاك الأجهزة"),
        };
        await PostJournalEntryAsync(entryNo, date, $"إهلاك شهري — {entryNo}", lines);
    }

    // 8) فوائد بنكية
    private async Task SeedBankInterestAsync(DateTime date)
    {
        var amount = 100m + _rand.Next(0, 200);
        if (!_accounts.ContainsKey("1110") || !_accounts.ContainsKey("4910")) return;
        var entryNo = $"INT-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("1110", amount, 0m, "فوائد مدينة"),
            ("4910", 0m, amount, "إيراد فوائد"),
        };
        await PostJournalEntryAsync(entryNo, date, $"فوائد بنكية — {entryNo}", lines);
    }

    // 9) تسوية ضريبة القيمة المضافة (شهرياً)
    private async Task SeedVATSettlementAsync(int year, int month)
    {
        // VAT settlement: صافي الضريبة (مخرجات - مدخلات) ← دائن
        var vatPayable = 200m + _rand.Next(0, 800);
        if (!_accounts.ContainsKey("1420") || !_accounts.ContainsKey("2220") || !_accounts.ContainsKey("1110")) return;
        var entryNo = $"VAT-{_currentYear}-{_entryCounter++:D4}";
        var lines = new List<(string, decimal, decimal, string)>
        {
            ("2220", vatPayable, 0m, "تسوية ضريبة مخرجات"),
            ("1110", 0m, vatPayable, "تحويل ضريبة لمصلحة الضرائب"),
        };
        await PostJournalEntryAsync(entryNo, new DateTime(year, month, 28), $"تسوية VAT {year}-{month:D2}", lines);
    }

    // ============== Insert helper ==============
    private async Task PostJournalEntryAsync(string entryNo, DateTime date, string desc, List<(string AccountCode, decimal Debit, decimal Credit, string Desc)> lines)
    {
        // فحص وجود
        var existing = await _conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM journal_entries WHERE company_id = @CompanyId AND entry_number = @EntryNo",
            new { CompanyId = _companyId, EntryNo = entryNo });
        if (existing > 0) return;

        // حساب المجاميع
        decimal totalDr = 0, totalCr = 0;
        foreach (var l in lines) { totalDr += l.Debit; totalCr += l.Credit; }
        if (Math.Abs(totalDr - totalCr) > 0.01m)
        {
            _logger.LogWarning("[SPRINT-50] Skipping unbalanced entry {EntryNo}: Dr={Dr} Cr={Cr}", entryNo, totalDr, totalCr);
            return;
        }

        var entryId = Guid.NewGuid();

        await _conn.ExecuteAsync(@"
            INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, status, created_by_user_id, created_at, updated_at, posted_at)
            VALUES (@Id, @CompanyId, @EntryNo, @Date, @Desc, 2, @UserId, NOW(), NOW(), @PostedAt)",
            new { Id = entryId, CompanyId = _companyId, EntryNo = entryNo, Date = date, Desc = desc, UserId = _systemUserId, PostedAt = date });

        for (int i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            if (!_accounts.TryGetValue(l.AccountCode, out var accountId))
            {
                _logger.LogWarning("[SPRINT-50] Account {Code} not found, skipping line", l.AccountCode);
                continue;
            }
            await _conn.ExecuteAsync(@"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, company_id, line_number, debit, credit, description)
                VALUES (@Id, @EntryId, @AccountId, @CompanyId, @LineNo, @Debit, @Credit, @Desc)",
                new
                {
                    Id = Guid.NewGuid(),
                    EntryId = entryId,
                    AccountId = accountId,
                    CompanyId = _companyId,
                    LineNo = i + 1,
                    Debit = l.Debit,
                    Credit = l.Credit,
                    Desc = l.Desc
                });
        }
    }
}
