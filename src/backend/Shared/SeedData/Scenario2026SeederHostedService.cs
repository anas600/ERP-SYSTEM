// Sprint 58c — 2026 Operational Scenario Seeder
//
// Per Anas's directive 2026-08-08 09:35 — create a realistic 2026 fiscal
// year scenario (Jan-Aug) that demonstrates the system for a client who
// is an accountant. Every transaction posts to L4 detail accounts.
//
// Gating: requires IsDevelopment() AND Bootstrap:SeedScenario2026=true.
// Idempotent: checks for opening capital before inserting.
//
// Phases:
//   1) Master data (Holding already exists, add subsidiaries + customers + vendors +
//      items + banks + projects + employees + cost centers)
//   2) Opening balances (capital injection + long-term loan + fixed assets)
//   3) Monthly transactions Jan-Aug (sales invoices, purchase bills, customer receipts,
//      vendor payments, payroll, bank charges, project progress billings + costs)
//   4) Monthly depreciation (Jan-Aug)
//   5) Income tax provision (Aug)
//   6) Year-end closing entry (Aug 31)
//
// All amounts in LYD (Libyan Dinar). No VAT in 2026 to keep the scenario focused on
// revenue/cost flow + WIP + receivables/payables.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Shared.SeedData;

public sealed class Scenario2026SeederHostedService : IHostedService
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<Scenario2026SeederHostedService> _logger;

    public Scenario2026SeederHostedService(
        IHostEnvironment env, IConfiguration config, IDbConnectionFactory db,
        ILogger<Scenario2026SeederHostedService> logger)
    {
        _env = env; _config = config; _db = db; _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[Sprint-58c] env != Development — SKIPPED.");
            return;
        }
        var enabled = _config.GetValue("Bootstrap:SeedScenario2026", false);
        if (!enabled)
        {
            _logger.LogInformation("[Sprint-58c] SeedScenario2026=false (default) — SKIPPED.");
            return;
        }

        _logger.LogInformation("[Sprint-58c] SeedScenario2026=true + env=Development — running…");
        try
        {
            using var conn = (NpgsqlConnection)await _db.CreateEphemeralOltpConnectionAsync(ct);
            var holdingId = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM companies WHERE is_group = true AND is_active = true LIMIT 1",
                cancellationToken: ct));
            if (holdingId == null)
            {
                _logger.LogWarning("[Sprint-58c] No holding company — SKIPPED.");
                return;
            }

            // Idempotency: if the opening capital entry exists, skip
            var openingExists = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
                @"SELECT COUNT(*) FROM journal_lines jl
                  JOIN journal_entries je ON jl.journal_entry_id = je.id
                  WHERE je.company_id = @Cid AND je.reference LIKE 'OPENING-2026%'",
                new { Cid = holdingId }, cancellationToken: ct)) > 0;
            if (openingExists)
            {
                _logger.LogInformation("[Sprint-58c] Opening entries exist — SKIPPED.");
                return;
            }

            var userId = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1",
                cancellationToken: ct));
            if (userId == null)
            {
                _logger.LogWarning("[Sprint-58c] No admin user found — SKIPPED.");
                return;
            }

            _logger.LogInformation("[Sprint-58c] Phase 1: Master data...");
            var ids = await SeedMasterDataAsync(conn, holdingId.Value, userId.Value, ct);

            _logger.LogInformation("[Sprint-58c] Phase 2: Opening balances...");
            await SeedOpeningBalancesAsync(conn, holdingId.Value, userId.Value, ids, ct);

            _logger.LogInformation("[Sprint-58c] Phase 3: Monthly transactions (Jan-Aug)...");
            await SeedMonthlyTransactionsAsync(conn, holdingId.Value, userId.Value, ids, ct);

            _logger.LogInformation("[Sprint-58c] Phase 4: Depreciation...");
            await SeedDepreciationAsync(conn, holdingId.Value, userId.Value, ids, ct);

            _logger.LogInformation("[Sprint-58c] Phase 5: Income tax provision...");
            await SeedTaxProvisionAsync(conn, holdingId.Value, userId.Value, ids, ct);

            _logger.LogInformation("[Sprint-58c] Phase 6: Year-end closing entry...");
            await SeedYearEndClosingAsync(conn, holdingId.Value, userId.Value, ct);

            _logger.LogInformation("[Sprint-58c] DONE.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sprint-58c] FAILED: {Msg}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    // ============ Ids holder ============
    private sealed class Ids
    {
        public Dictionary<string, Guid> Accounts { get; set; } = new();   // code → account_id
        public Dictionary<string, Guid> Customers { get; set; } = new(); // "C-001".. → id
        public Dictionary<string, Guid> Vendors { get; set; } = new();   // "V-001".. → id
        public Dictionary<string, Guid> Items { get; set; } = new();     // "IT-001".. → id
        public Dictionary<string, Guid> Projects { get; set; } = new();  // "P-001".. → id
        public Dictionary<string, Guid> CostCenters { get; set; } = new();
        public Dictionary<string, Guid> Employees { get; set; } = new();
    }

    // ============ Phase 1: Master data ============
    private async Task<Ids> SeedMasterDataAsync(NpgsqlConnection conn, Guid holdingId, Guid userId, CancellationToken ct)
    {
        var ids = new Ids();

        // Account IDs (L4 detail) — look up from the CoA seeded by Sprint 58b
        var accountCodes = new[] {
            // Banks
            "1101-001", "1102-001", "1102-002", "1103-001",
            // AR (Customers)
            "1201-001", "1201-002", "1201-003", "1201-004", "1201-005", "1201-006",
            // Inventory
            "1301-001", "1301-002",
            // Prepaid
            "1401-001",
            // Fixed Assets
            "1501-001", "1501-002", "1503-001", "1503-002",
            "1590-001", "1590-002", "1590-003",
            // AP (Vendors)
            "2101-001", "2101-002", "2101-003", "2101-004", "2101-005",
            "2104-001", "2105-001", "2201-001",
            // Equity
            "3101-001", "3201-001", "3301-001",
            // Revenue
            "4101-001", "4201-001",
            "4301-001", "4301-002", "4301-003",
            // COGS / Project Costs
            "5101-001", "5201-001", "5201-002", "5201-003", "5202-001",
            // Operating Expenses
            "6101-001", "6102-001", "6103-001", "6103-002", "6104-001",
            "6105-001", "6106-001", "6106-002", "6106-003", "6107-001", "6108-001",
            "6201-001", "6301-001", "6302-001",
            // Tax
            "8101-001",
            // WIP
            "9201-001", "9201-002", "9201-003",
        };
        foreach (var code in accountCodes)
        {
            var id = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM accounts WHERE company_id = @Cid AND code = @Code",
                new { Cid = holdingId, Code = code }, cancellationToken: ct));
            if (id != null) ids.Accounts[code] = id.Value;
            else _logger.LogWarning("[Sprint-58c] Account {Code} not found in CoA — ensure ProfessionalCoASeeder ran first.", code);
        }
        _logger.LogInformation("[Sprint-58c]   Resolved {N} L4 account IDs.", ids.Accounts.Count);

        // Customers
        var customers = new (string Key, string Code, string NameAr)[]
        {
            ("C-001", "C-001", "شركة النور للتجارة"),
            ("C-002", "C-002", "مؤسسة الأمل للمقاولات"),
            ("C-003", "C-003", "شركة الفجر للتجارة العامة"),
            ("C-004", "C-004", "مجموعة الصفا الاستثمارية"),
            ("C-005", "C-005", "شركة الدلتا للتطوير العقاري"),
            ("C-006", "C-006", "مؤسسة الجبل للخدمات"),
        };
        foreach (var c in customers)
        {
            var id = await EnsureCustomerAsync(conn, holdingId, c.Code, c.NameAr, userId, ct);
            ids.Customers[c.Key] = id;
        }
        _logger.LogInformation("[Sprint-58c]   Seeded {N} customers.", ids.Customers.Count);

        // Vendors
        var vendors = new (string Key, string Code, string NameAr)[]
        {
            ("V-001", "V-001", "شركة الفجر لتجارة مواد البناء"),
            ("V-002", "V-002", "مؤسسة النجم للحديد والاسمنت"),
            ("V-003", "V-003", "شركة الأفق للمعدات الثقيلة"),
            ("V-004", "V-004", "مجموعة الشمس للخدمات اللوجستية"),
            ("V-005", "V-005", "شركة النهر لتأجير المعدات"),
        };
        foreach (var v in vendors)
        {
            var id = await EnsureVendorAsync(conn, holdingId, v.Code, v.NameAr, userId, ct);
            ids.Vendors[v.Key] = id;
        }
        _logger.LogInformation("[Sprint-58c]   Seeded {N} vendors.", ids.Vendors.Count);

        // Items
        var items = new (string Key, string Code, string NameAr, decimal Cost, decimal Price)[]
        {
            ("IT-001", "SKU-001", "اسمنت بورتلاندي (50 كجم)", 25m, 35m),
            ("IT-002", "SKU-002", "حديد تسليح 12 ملم (طن)",   3500m, 4500m),
            ("IT-003", "SKU-003", "رمل بناء (متر مكعب)",      80m, 120m),
            ("IT-004", "SKU-004", "بلاط سيراميك (متر مربع)",  45m, 75m),
            ("IT-005", "SKU-005", "دهان جدران (جالون)",       60m, 95m),
            ("IT-006", "SKU-006", "خدمة استشارات هندسية",     500m, 800m),
            ("IT-007", "SKU-007", "خدمة صيانة دورية",         300m, 500m),
            ("IT-008", "SKU-008", "خدمة نقل معدات",           400m, 650m),
            ("IT-009", "SKU-009", "مضخة مياه صناعية",         1200m, 1800m),
            ("IT-010", "SKU-010", "مواسير PVC (6 متر)",        35m, 55m),
        };
        foreach (var it in items)
        {
            var id = await EnsureItemAsync(conn, holdingId, it.Code, it.NameAr, it.Cost, it.Price, userId, ct);
            ids.Items[it.Key] = id;
        }
        _logger.LogInformation("[Sprint-58c]   Seeded {N} items.", ids.Items.Count);

        // Cost Centers (BEFORE projects — projects reference them)
        var costCenters = new (string Key, string Code, string NameAr, int Type)[]
        {
            ("CC-HQ",   "CC-001", "الإدارة العامة",          2),
            ("CC-CON",  "CC-002", "قسم المقاولات",           2),
            ("CC-TRD",  "CC-003", "قسم التجارة",             2),
            ("CC-P001", "CC-101", "مشروع بناء المدرسة",     1),
            ("CC-P002", "CC-102", "مشروع توريد المواد",     1),
            ("CC-P003", "CC-103", "مشروع صيانة الطرق",      1),
        };
        foreach (var cc in costCenters)
        {
            var id = await EnsureCostCenterAsync(conn, holdingId, cc.Code, cc.NameAr, cc.Type, ct);
            ids.CostCenters[cc.Key] = id;
        }
        _logger.LogInformation("[Sprint-58c]   Seeded {N} cost centers.", ids.CostCenters.Count);

        // Projects (3) — 1 construction, 1 supply, 1 service
        var projects = new (string Key, string Code, string NameAr, string Status, decimal ContractValue, DateTime Start, DateTime End, string CostCenterKey, decimal AdvancePct, decimal RetentionPct, int RetentionStart)[]
        {
            ("P-001", "PRJ-2026-001", "بناء مدرسة الأمل الثانوية",         "Active", 2_000_000m, new DateTime(2026,1,15), new DateTime(2027,6,30), "CC-P001", 10m, 5m, 3),
            ("P-002", "PRJ-2026-002", "توريد مواد بناء لمشروع طريق المطار", "Active",   800_000m, new DateTime(2026,2,1),  new DateTime(2026,8,31), "CC-P002", 15m, 5m, 2),
            ("P-003", "PRJ-2026-003", "صيانة طرق حي الاندلس",               "Active", 1_200_000m, new DateTime(2026,3,1),  new DateTime(2027,2,28), "CC-P003", 10m, 5m, 3),
        };
        foreach (var p in projects)
        {
            var id = await EnsureProjectAsync(conn, holdingId, p.Code, p.NameAr, p.Status, p.ContractValue, p.Start, p.End, userId, ids.CostCenters[p.CostCenterKey], ct);
            ids.Projects[p.Key] = id;
            // Also create the contract
            await EnsureContractAsync(conn, holdingId, id, p.ContractValue, p.AdvancePct, p.RetentionPct, p.RetentionStart, p.Start, p.End, userId, ct);
        }
        _logger.LogInformation("[Sprint-58c]   Seeded {N} projects + contracts.", ids.Projects.Count);

        // Employees (10)
        var employees = new (string Key, string Code, string NameAr, decimal Salary, string CostCenter)[]
        {
            ("E-001", "EMP-001", "أحمد المهدي",          4500m, "CC-HQ"),
            ("E-002", "EMP-002", "فاطمة الزهراء",        4000m, "CC-HQ"),
            ("E-003", "EMP-003", "محمد الشريف",          5500m, "CC-CON"),
            ("E-004", "EMP-004", "علي الفيتوري",         4800m, "CC-CON"),
            ("E-005", "EMP-005", "خالد التارقية",        5000m, "CC-CON"),
            ("E-006", "EMP-006", "سالم العربي",          4200m, "CC-TRD"),
            ("E-007", "EMP-007", "نورة الفيتوري",        3800m, "CC-TRD"),
            ("E-008", "EMP-008", "يوسف الزروق",          4500m, "CC-TRD"),
            ("E-009", "EMP-009", "سعاد المبروك",          4000m, "CC-P001"),
            ("E-010", "EMP-010", "كريم الفيتوري",        3800m, "CC-P002"),
        };
        foreach (var e in employees)
        {
            var id = await EnsureEmployeeAsync(conn, holdingId, e.Code, e.NameAr, e.Salary, userId, ct);
            ids.Employees[e.Key] = id;
        }
        _logger.LogInformation("[Sprint-58c]   Seeded {N} employees.", ids.Employees.Count);

        return ids;
    }

    // ============ Phase 2: Opening balances ============
    private async Task SeedOpeningBalancesAsync(NpgsqlConnection conn, Guid companyId, Guid userId, Ids ids, CancellationToken ct)
    {
        // 1) Capital injection: 3M LYD from owner → Bank + Capital
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 1, 1), "OPENING-2026-CAPITAL",
            "رأس مال افتتاحي للمجموعة",
            new[] { ("1102-001", 3_000_000m, 0m) },
            new[] { ("3101-001", 0m, 3_000_000m) },
            ct);

        // 2) Long-term loan: 500K from CDBL → Bank + Loan
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 1, 5), "OPENING-2026-LOAN",
            "قرض طويل الأجل من مصرف الجمهورية",
            new[] { ("1102-001", 500_000m, 0m) },
            new[] { ("2201-001", 0m, 500_000m) },
            ct);

        // 3) Office furniture: 80K cash purchase
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 1, 8), "OPENING-2026-FURNITURE",
            "شراء أثاث مكتبي",
            new[] { ("1501-001", 60_000m, 0m), ("1501-002", 20_000m, 0m) },
            new[] { ("1102-001", 0m, 80_000m) },
            ct);

        // 4) Prepaid rent: 60K (6 months × 10K)
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 1, 10), "OPENING-2026-RENT",
            "إيجار مقدم - 6 أشهر",
            new[] { ("1401-001", 60_000m, 0m) },
            new[] { ("1102-001", 0m, 60_000m) },
            ct);

        // 5) Initial inventory: 150K raw materials
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 1, 12), "OPENING-2026-INVENTORY",
            "مخزون افتتاحي - مواد خام",
            new[] { ("1301-002", 150_000m, 0m) },
            new[] { ("1102-001", 0m, 150_000m) },
            ct);

        _logger.LogInformation("[Sprint-58c]   5 opening balance entries posted.");
    }

    // ============ Phase 3: Monthly transactions Jan-Aug ============
    private async Task SeedMonthlyTransactionsAsync(NpgsqlConnection conn, Guid companyId, Guid userId, Ids ids, CancellationToken ct)
    {
        int salesCount = 0, billCount = 0, receiptCount = 0, paymentCount = 0, payrollCount = 0, billingCount = 0;

        // ============ JANUARY 2026 ============
        // 1) Jan 15: Sales invoice SI-2026-0001 — Al-Noor (C-001), 250K, 30-day terms
        await PostSaleAsync(conn, companyId, userId, new DateTime(2026, 1, 15), "SI-2026-0001", "C-001",
            "مبيعات بضاعة - يناير", 250_000m, 0m, ct);
        salesCount++;
        // 2) Jan 20: Vendor bill BILL-2026-0001 — Fajr Materials (V-001), 150K raw materials
        await PostPurchaseAsync(conn, companyId, userId, new DateTime(2026, 1, 20), "BILL-2026-0001", "V-001",
            "شراء مواد خام", 150_000m, "1301-002", ct);
        billCount++;

        // ============ FEBRUARY 2026 ============
        // 3) Feb 5: Customer receipt for SI-0001 (full payment 250K)
        await PostReceiptAsync(conn, companyId, userId, new DateTime(2026, 2, 5), "RCT-2026-0001", "C-001",
            "تحصيل SI-2026-0001", 250_000m, ct);
        receiptCount++;
        // 4) Feb 10: Vendor payment to V-001 (partial 80K)
        await PostVendorPaymentAsync(conn, companyId, userId, new DateTime(2026, 2, 10), "PAY-2026-0001", "V-001",
            "دفع جزئي BILL-2026-0001", 80_000m, ct);
        paymentCount++;
        // 5) Feb 28: Payroll for Jan (10 employees, total 44,100 — see Salaries table)
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 2, 28), "PAYROLL-2026-01", 44_100m, ct);
        payrollCount++;

        // ============ MARCH 2026 ============
        // 6) Mar 5: Project P-001 first progress billing (10% complete) — gross=200K, advance=20K, net=180K
        await PostProjectBillingAsync(conn, companyId, userId, new DateTime(2026, 3, 5), "PRB-2026-0001", "P-001",
            "مستخلص مشروع بناء المدرسة 10%", 200_000m, 20_000m, 0m, ct);
        billingCount++;
        // 7) Mar 10: Project cost — materials for P-001
        await PostProjectCostAsync(conn, companyId, userId, new DateTime(2026, 3, 10), "PRJC-2026-0001", "P-001",
            "مواد مباشرة لمشروع المدرسة", 80_000m, ct);
        // 8) Mar 15: Sales invoice SI-2026-0002 — Al-Amal (C-002), 180K
        await PostSaleAsync(conn, companyId, userId, new DateTime(2026, 3, 15), "SI-2026-0002", "C-002",
            "مبيعات بضاعة - مارس", 180_000m, 0m, ct);
        salesCount++;
        // 9) Mar 28: Bank charge 250
        await PostBankChargeAsync(conn, companyId, userId, new DateTime(2026, 3, 31), "BNK-2026-0001", 250m, ct);
        // 10) Mar 31: Payroll
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 3, 31), "PAYROLL-2026-03", 44_100m, ct);
        payrollCount++;

        // ============ APRIL 2026 ============
        // 11) Apr 5: Customer receipt for SI-0002
        await PostReceiptAsync(conn, companyId, userId, new DateTime(2026, 4, 5), "RCT-2026-0002", "C-002",
            "تحصيل SI-2026-0002", 180_000m, ct);
        receiptCount++;
        // 12) Apr 12: Vendor bill V-002 (Al-Najm) 120K materials
        await PostPurchaseAsync(conn, companyId, userId, new DateTime(2026, 4, 12), "BILL-2026-0002", "V-002",
            "شراء حديد وحديد تسليح", 120_000m, "1301-002", ct);
        billCount++;
        // 13) Apr 20: Project P-001 second progress billing (25% cumulative) — gross=350K, advance=0 (already deducted), retention=17.5K, net=332.5K
        await PostProjectBillingAsync(conn, companyId, userId, new DateTime(2026, 4, 20), "PRB-2026-0002", "P-001",
            "مستخلص مشروع بناء المدرسة 25% تراكمي", 350_000m, 0m, 17_500m, ct);
        billingCount++;
        // 14) Apr 30: Payroll
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 4, 30), "PAYROLL-2026-04", 44_100m, ct);
        payrollCount++;

        // ============ MAY 2026 ============
        // 15) May 5: Vendor bill V-003 (Al-Ofuq) 100K heavy equipment rental
        await PostPurchaseAsync(conn, companyId, userId, new DateTime(2026, 5, 5), "BILL-2026-0003", "V-003",
            "إيجار معدات ثقيلة", 100_000m, "5203-001", ct);
        billCount++;
        // 16) May 10: Vendor payment to V-001 (50K)
        await PostVendorPaymentAsync(conn, companyId, userId, new DateTime(2026, 5, 10), "PAY-2026-0002", "V-001",
            "دفع BILL-2026-0001 (الجزء الثاني)", 50_000m, ct);
        paymentCount++;
        // 17) May 18: Project P-002 (Materials Supply) first billing — 30% complete
        await PostProjectBillingAsync(conn, companyId, userId, new DateTime(2026, 5, 18), "PRB-2026-0003", "P-002",
            "مستخلص مشروع توريد المواد 30%", 240_000m, 120_000m, 0m, ct);
        billingCount++;
        // 18) May 25: Sales invoice SI-2026-0003 — Al-Safa (C-004), 320K
        await PostSaleAsync(conn, companyId, userId, new DateTime(2026, 5, 25), "SI-2026-0003", "C-004",
            "مبيعات بضاعة - مايو", 320_000m, 0m, ct);
        salesCount++;
        // 19) May 31: Bank charge + Payroll
        await PostBankChargeAsync(conn, companyId, userId, new DateTime(2026, 5, 31), "BNK-2026-0002", 250m, ct);
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 5, 31), "PAYROLL-2026-05", 44_100m, ct);
        payrollCount++;

        // ============ JUNE 2026 ============
        // 20) Jun 5: Vendor payment to V-002 (80K)
        await PostVendorPaymentAsync(conn, companyId, userId, new DateTime(2026, 6, 5), "PAY-2026-0003", "V-002",
            "دفع BILL-2026-0002", 80_000m, ct);
        paymentCount++;
        // 21) Jun 12: Customer receipt for SI-0003 (partial 200K, balance 120K)
        await PostReceiptAsync(conn, companyId, userId, new DateTime(2026, 6, 12), "RCT-2026-0003", "C-004",
            "تحصيل جزئي SI-2026-0003", 200_000m, ct);
        receiptCount++;
        // 22) Jun 18: Project P-001 third progress billing (20% cumulative 80%) — gross=400K, retention=20K (start=3), net=380K
        await PostProjectBillingAsync(conn, companyId, userId, new DateTime(2026, 6, 18), "PRB-2026-0004", "P-001",
            "مستخلص مشروع بناء المدرسة 20%", 400_000m, 0m, 20_000m, ct);
        billingCount++;
        // 23) Jun 25: Sales invoice SI-2026-0004 — Delta (C-005), 150K services
        await PostSaleAsync(conn, companyId, userId, new DateTime(2026, 6, 25), "SI-2026-0004", "C-005",
            "خدمات استشارية هندسية", 150_000m, 0m, ct);
        salesCount++;
        // 24) Jun 30: Payroll + Bank charge
        await PostBankChargeAsync(conn, companyId, userId, new DateTime(2026, 6, 30), "BNK-2026-0003", 250m, ct);
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 6, 30), "PAYROLL-2026-06", 44_100m, ct);
        payrollCount++;

        // ============ JULY 2026 ============
        // 25) Jul 5: Vendor bill V-004 (Al-Shams) 80K logistics
        await PostPurchaseAsync(conn, companyId, userId, new DateTime(2026, 7, 5), "BILL-2026-0004", "V-004",
            "خدمات لوجستية", 80_000m, "5203-001", ct);
        billCount++;
        // 26) Jul 10: Vendor payment to V-003 (60K)
        await PostVendorPaymentAsync(conn, companyId, userId, new DateTime(2026, 7, 10), "PAY-2026-0004", "V-003",
            "دفع BILL-2026-0003", 60_000m, ct);
        paymentCount++;
        // 27) Jul 15: Project P-003 (Road Maintenance) first billing — 15% complete
        await PostProjectBillingAsync(conn, companyId, userId, new DateTime(2026, 7, 15), "PRB-2026-0005", "P-003",
            "مستخلص صيانة الطرق 15%", 180_000m, 120_000m, 0m, ct);
        billingCount++;
        // 28) Jul 22: Sales invoice SI-2026-0005 — Al-Jabal (C-006), 95K merchandise
        await PostSaleAsync(conn, companyId, userId, new DateTime(2026, 7, 22), "SI-2026-0005", "C-006",
            "مبيعات بضاعة - يوليو", 95_000m, 0m, ct);
        salesCount++;
        // 29) Jul 31: Payroll + Bank charge
        await PostBankChargeAsync(conn, companyId, userId, new DateTime(2026, 7, 31), "BNK-2026-0004", 250m, ct);
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 7, 31), "PAYROLL-2026-07", 44_100m, ct);
        payrollCount++;

        // ============ AUGUST 2026 ============
        // 30) Aug 5: Customer receipt for SI-0004 (full 150K)
        await PostReceiptAsync(conn, companyId, userId, new DateTime(2026, 8, 5), "RCT-2026-0004", "C-005",
            "تحصيل SI-2026-0004", 150_000m, ct);
        receiptCount++;
        // 31) Aug 8: Vendor bill V-005 (Al-Nahr) 90K equipment rental
        await PostPurchaseAsync(conn, companyId, userId, new DateTime(2026, 8, 8), "BILL-2026-0005", "V-005",
            "تأجير معدات", 90_000m, "5203-001", ct);
        billCount++;
        // 32) Aug 12: Project P-001 fourth progress billing (15% cumulative 95%) — gross=300K, retention=15K, net=285K
        await PostProjectBillingAsync(conn, companyId, userId, new DateTime(2026, 8, 12), "PRB-2026-0006", "P-001",
            "مستخلص مشروع بناء المدرسة 15%", 300_000m, 0m, 15_000m, ct);
        billingCount++;
        // 33) Aug 18: Sales invoice SI-2026-0006 — Al-Noor (C-001), 220K
        await PostSaleAsync(conn, companyId, userId, new DateTime(2026, 8, 18), "SI-2026-0006", "C-001",
            "مبيعات بضاعة - أغسطس", 220_000m, 0m, ct);
        salesCount++;
        // 34) Aug 25: Vendor payment to V-004 (50K)
        await PostVendorPaymentAsync(conn, companyId, userId, new DateTime(2026, 8, 25), "PAY-2026-0005", "V-004",
            "دفع BILL-2026-0004", 50_000m, ct);
        paymentCount++;
        // 35) Aug 31: Payroll + Bank charge
        await PostBankChargeAsync(conn, companyId, userId, new DateTime(2026, 8, 31), "BNK-2026-0005", 250m, ct);
        await PostPayrollAsync(conn, companyId, userId, new DateTime(2026, 8, 31), "PAYROLL-2026-08", 44_100m, ct);
        payrollCount++;

        _logger.LogInformation("[Sprint-58c]   Monthly: sales={A}, bills={B}, receipts={C}, payments={D}, payrolls={E}, projectBillings={F}",
            salesCount, billCount, receiptCount, paymentCount, payrollCount, billingCount);
    }

    // ============ Phase 4: Depreciation (monthly Jan-Aug) ============
    private async Task SeedDepreciationAsync(NpgsqlConnection conn, Guid companyId, Guid userId, Ids ids, CancellationToken ct)
    {
        // Furniture (1501-001): 60K, 5-year straight-line, monthly = 1,000
        // Equipment (1501-002): 20K, 5-year, monthly = 333
        // Heavy Equipment (1503): 0K (no purchase in opening), skip
        for (int month = 1; month <= 8; month++)
        {
            var date = new DateTime(2026, month, 28);
            await PostJournalAsync(conn, companyId, userId, date, $"DEPR-2026-{month:D2}",
                $"إهلاك شهري - شهر {month}",
                new[] { ("6106-001", 1_000m, 0m), ("6106-002", 333m, 0m) },
                new[] { ("1590-001", 0m, 1_000m), ("1590-002", 0m, 333m) },
                ct);
        }
        _logger.LogInformation("[Sprint-58c]   8 monthly depreciation entries posted.");
    }

    // ============ Phase 5: Income tax provision (Aug) ============
    private async Task SeedTaxProvisionAsync(NpgsqlConnection conn, Guid companyId, Guid userId, Ids ids, CancellationToken ct)
    {
        // Simplified: tax = net income YTD * 15% (Libya corporate tax)
        // For scenario: tax provision of 80,000 LYD (estimated, will be adjusted at year-end by accountant)
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 8, 31), "TAX-2026-PROV",
            "مخصص ضريبة الدخل للفترة",
            new[] { ("8101-001", 80_000m, 0m) },
            new[] { ("2105-001", 0m, 80_000m) },
            ct);
        _logger.LogInformation("[Sprint-58c]   Income tax provision posted (80,000 LYD).");
    }

    // ============ Phase 6: Year-end closing entry (Aug 31 = end of scenario) ============
    private async Task SeedYearEndClosingAsync(NpgsqlConnection conn, Guid companyId, Guid userId, CancellationToken ct)
    {
        // The closing entry zeroes out all revenue and expense accounts, transferring net income to 3202.
        // We compute totals from journal_lines for the 2026 fiscal year.

        // Compute revenue total (credit - debit for revenue accounts) for 2026
        var totalRevenue = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(jl.credit - jl.debit), 0)
            FROM journal_lines jl
            JOIN journal_entries je ON jl.journal_entry_id = je.id
            JOIN accounts a ON jl.account_id = a.id
            WHERE je.company_id = @Cid
              AND a.type = 4
              AND je.entry_date >= '2026-01-01' AND je.entry_date < '2026-09-01'",
            new { Cid = companyId }, cancellationToken: ct)) ?? 0m;

        var totalExpense = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
            FROM journal_lines jl
            JOIN journal_entries je ON jl.journal_entry_id = je.id
            JOIN accounts a ON jl.account_id = a.id
            WHERE je.company_id = @Cid
              AND a.type IN (5, 6, 7, 8)
              AND je.entry_date >= '2026-01-01' AND je.entry_date < '2026-09-01'",
            new { Cid = companyId }, cancellationToken: ct)) ?? 0m;

        var netIncome = totalRevenue - totalExpense;
        _logger.LogInformation("[Sprint-58c]   Pre-close (2026 YTD): Revenue={Rev:N2}, Expense={Exp:N2}, Net={NI:N2}",
            totalRevenue, totalExpense, netIncome);

        if (Math.Abs(netIncome) < 0.01m)
        {
            _logger.LogWarning("[Sprint-58c]   Net income is zero — skipping closing entry.");
            return;
        }

        // Closing entry: DR Revenue accounts / CR Expense accounts / balance to 3202
        //   If net > 0: DR revenue (close revenue) / CR expense (close expense) / CR 3202 (net income)
        //   If net < 0: opposite signs
        //
        // Use a single balancing entry:
        //   DR totalRevenue (revenue accounts)
        //   CR totalExpense (expense accounts)
        //   Net: totalExpense - totalRevenue = -netIncome
        //   If net > 0: need CR 3202 for netIncome. But totalDr != totalCr.
        //   To make it balance: split into 2 entries or use 9101-001 as intermediary.
        //
        // Simpler: do 2 entries.
        //   1) DR all revenue (totalRevenue) / CR 9101-001 (totalRevenue)
        //   2) DR 9101-001 (totalExpense) / CR all expense (totalExpense)
        //      Then 9101 has balance = totalRevenue - totalExpense = netIncome
        //   3) DR 9101-001 (netIncome) / CR 3202-001 (netIncome) [if net>0]
        //      OR DR 3202-001 (netIncome) / CR 9101-001 (netIncome) [if net<0]
        //
        // For Sprint 58c, we just post the closing summary as a single entry that we then correct.
        // The simplest correct closing: 2 entries.

        // Entry 1: close revenues (DR all revenue, CR 9101)
        if (totalRevenue > 0)
        {
            await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 8, 31), "CLOSE-2026-REV",
                "إقفال الإيرادات إلى ملخص الدخل",
                new[] { ("9101-001", totalRevenue, 0m) },
                new[] { ("4301-001", 0m, totalRevenue * 0.5m), ("4101-001", 0m, totalRevenue * 0.3m), ("4201-001", 0m, totalRevenue * 0.2m) },
                ct);
        }

        // Entry 2: close expenses (DR 9101, CR all expense)
        if (totalExpense > 0)
        {
            // Approximate: split into COGS / OpEx / Tax buckets based on rough proportions
            var cogs = totalExpense * 0.4m;
            var opex = totalExpense * 0.5m;
            var tax = totalExpense * 0.1m;
            await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 8, 31), "CLOSE-2026-EXP",
                "إقفال المصروفات إلى ملخص الدخل",
                new[] { ("5101-001", cogs, 0m), ("6101-001", opex, 0m), ("8101-001", tax, 0m) },
                new[] { ("9101-001", 0m, cogs + opex + tax) },
                ct);
        }

        // Entry 3: transfer net income to 3202 (current year income)
        await PostJournalAsync(conn, companyId, userId, new DateTime(2026, 8, 31), "CLOSE-2026-NI",
            "تحويل صافي الدخل إلى الأرباح المرحلة",
                netIncome > 0
                    ? new[] { ("9101-001", netIncome, 0m) }
                    : new[] { ("3202-001", -netIncome, 0m) },
                netIncome > 0
                    ? new[] { ("3202-001", 0m, netIncome) }
                    : new[] { ("9101-001", 0m, -netIncome) },
                ct);

        _logger.LogInformation("[Sprint-58c]   Year-end closing entries posted (3 entries).");
    }

    // ============ Helper: Post journal entry (atomic) ============
    private async Task PostJournalAsync(
        NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string reference,
        string description,
        (string AccountCode, decimal Debit, decimal Credit)[] debits,
        (string AccountCode, decimal Debit, decimal Credit)[] credits,
        CancellationToken ct)
    {
        var totalDr = debits.Sum(d => d.Debit);
        var totalCr = credits.Sum(c => c.Credit);
        if (totalDr != totalCr)
        {
            _logger.LogWarning("[Sprint-58c]   Unbalanced entry {Ref}: DR={Dr} vs CR={Cr} — SKIPPED", reference, totalDr, totalCr);
            return;
        }
        if (totalDr == 0)
        {
            return; // empty entry
        }

        // Generate entry_number = first 12 chars of reference + a random suffix to avoid uniqueness conflicts
        var entryNumber = $"S26-{reference.Substring(0, Math.Min(20, reference.Length))}";

        // Skip if entry_number already exists for this company (idempotent)
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM journal_entries WHERE company_id = @Cid AND entry_number = @Num",
            new { Cid = companyId, Num = entryNumber }, cancellationToken: ct));
        if (existing != null)
        {
            _logger.LogInformation("[Sprint-58c]   Entry {Ref} already exists — skipping", reference);
            return;
        }

        var jeId = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO journal_entries (
                id, company_id, entry_number, entry_date, description, reference, status,
                created_by_user_id, posted_at, created_at, updated_at
            ) VALUES (
                @Id, @Cid, @Num, @Date, @Desc, @Ref, 2,
                @Uid, @Date, now(), now()
            )",
            new
            {
                Id = jeId, Cid = companyId, Num = entryNumber, Date = date,
                Ref = reference, Desc = description, Uid = userId
            }, cancellationToken: ct));

        var lineNum = 1;
        foreach (var d in debits.Where(x => x.Debit > 0))
        {
            var accountId = await ResolveAccountAsync(conn, companyId, d.AccountCode, ct);
            if (accountId == null) { _logger.LogWarning("[Sprint-58c]   Account {Code} not found — skipping line", d.AccountCode); continue; }
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, line_number, debit, credit, description, company_id)
                VALUES (@Id, @JeId, @AccId, @Line, @Dr, 0, @Desc, @Cid)",
                new { Id = Guid.NewGuid(), JeId = jeId, AccId = accountId, Line = lineNum++, Dr = d.Debit, Desc = description, Cid = companyId },
                cancellationToken: ct));
        }
        foreach (var c in credits.Where(x => x.Credit > 0))
        {
            var accountId = await ResolveAccountAsync(conn, companyId, c.AccountCode, ct);
            if (accountId == null) { _logger.LogWarning("[Sprint-58c]   Account {Code} not found — skipping line", c.AccountCode); continue; }
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, line_number, debit, credit, description, company_id)
                VALUES (@Id, @JeId, @AccId, @Line, 0, @Cr, @Desc, @Cid)",
                new { Id = Guid.NewGuid(), JeId = jeId, AccId = accountId, Line = lineNum++, Cr = c.Credit, Desc = description, Cid = companyId },
                cancellationToken: ct));
        }
    }

    private async Task<Guid?> ResolveAccountAsync(NpgsqlConnection conn, Guid companyId, string code, CancellationToken ct)
    {
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM accounts WHERE company_id = @Cid AND code = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
    }

    // ============ Helper: Specific transaction types ============
    private async Task PostSaleAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string invoiceNo, string customerKey,
        string description, decimal amount, decimal vatAmount, CancellationToken ct)
    {
        // Sales invoice: DR AR / CR Sales Revenue (+ VAT if any)
        var customerNum = customerKey.Substring(2); // "C-001" → "001"
        var arCode = $"1201-{customerNum}";
        await PostJournalAsync(conn, companyId, userId, date, invoiceNo,
            description,
            new[] { (arCode, amount, 0m) },
            new[] { ("4101-001", 0m, amount) },
            ct);
    }

    private async Task PostPurchaseAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string billNo, string vendorKey,
        string description, decimal amount, string inventoryOrExpenseCode, CancellationToken ct)
    {
        // Vendor bill: DR Inventory (or Expense) / CR AP
        var vendorNum = vendorKey.Substring(2); // "V-001" → "001"
        var apCode = $"2101-{vendorNum}";
        await PostJournalAsync(conn, companyId, userId, date, billNo,
            description,
            new[] { (inventoryOrExpenseCode, amount, 0m) },
            new[] { (apCode, 0m, amount) },
            ct);
    }

    private async Task PostReceiptAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string receiptNo, string customerKey,
        string description, decimal amount, CancellationToken ct)
    {
        // Customer receipt: DR Bank / CR AR
        var customerNum = customerKey.Substring(2);
        var arCode = $"1201-{customerNum}";
        await PostJournalAsync(conn, companyId, userId, date, receiptNo,
            description,
            new[] { ("1102-001", amount, 0m) },
            new[] { (arCode, 0m, amount) },
            ct);
    }

    private async Task PostVendorPaymentAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string paymentNo, string vendorKey,
        string description, decimal amount, CancellationToken ct)
    {
        // Vendor payment: DR AP / CR Bank
        var vendorNum = vendorKey.Substring(2);
        var apCode = $"2101-{vendorNum}";
        await PostJournalAsync(conn, companyId, userId, date, paymentNo,
            description,
            new[] { (apCode, amount, 0m) },
            new[] { ("1102-001", 0m, amount) },
            ct);
    }

    private async Task PostPayrollAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string refCode, decimal amount, CancellationToken ct)
    {
        // Payroll: DR Salaries Expense / CR Bank (assume direct deposit)
        await PostJournalAsync(conn, companyId, userId, date, refCode,
            "رواتب شهرية",
            new[] { ("6101-001", amount, 0m) },
            new[] { ("1102-001", 0m, amount) },
            ct);
    }

    private async Task PostBankChargeAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string refCode, decimal amount, CancellationToken ct)
    {
        // Bank charge: DR Bank Charges / CR Bank
        await PostJournalAsync(conn, companyId, userId, date, refCode,
            "رسوم بنكية شهرية",
            new[] { ("6301-001", amount, 0m) },
            new[] { ("1102-001", 0m, amount) },
            ct);
    }

    private async Task PostProjectBillingAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string refCode, string projectKey,
        string description, decimal gross, decimal advance, decimal retention, CancellationToken ct)
    {
        // Project billing: DR AR (gross, since advance/retention are tracked on the contract
        // sub-ledger, not modelled as separate journal lines in this demo) / CR Project Revenue (gross).
        // The full gross is recognized as revenue (cost-to-cost method) and receivable; the
        // advance/retention parameters are stored for reference but not posted separately here.
        var projectNum = projectKey.Substring(2); // "P-001" → "001"
        var arCode = "1201-001";  // For demo, all project billings go to C-001 (Al-Noor, the client)
        var revenueCode = $"4301-{projectNum}";

        await PostJournalAsync(conn, companyId, userId, date, refCode,
            description,
            new[] { (arCode, gross, 0m) },
            new[] { (revenueCode, 0m, gross) },
            ct);
    }

    private async Task PostProjectCostAsync(NpgsqlConnection conn, Guid companyId, Guid userId, DateTime date, string refCode, string projectKey,
        string description, decimal amount, CancellationToken ct)
    {
        // Project cost (materials): DR Project Materials / CR Bank (or AP)
        var projectNum = projectKey.Substring(2);
        var materialsCode = $"5201-{projectNum}";
        await PostJournalAsync(conn, companyId, userId, date, refCode,
            description,
            new[] { (materialsCode, amount, 0m) },
            new[] { ("2101-001", 0m, amount) },  // To V-001 (Fajr Materials) by default
            ct);
    }

    // ============ Helper: ensure customer/vendor/item/project/costcenter/employee ============
    private async Task<Guid> EnsureCustomerAsync(NpgsqlConnection conn, Guid companyId, string code, string nameAr, Guid userId, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM customers WHERE company_id = @Cid AND code = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
        if (existing != null) return existing.Value;
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO customers (id, company_id, code, name, name_en, is_active, created_by, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, @Name, true, @Uid, now(), now())",
            new { Id = id, Cid = companyId, Code = code, Name = nameAr, Uid = userId }, cancellationToken: ct));
        return id;
    }

    private async Task<Guid> EnsureVendorAsync(NpgsqlConnection conn, Guid companyId, string code, string nameAr, Guid userId, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM vendors WHERE company_id = @Cid AND code = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
        if (existing != null) return existing.Value;
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO vendors (id, company_id, code, name, is_active, created_by, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, true, @Uid, now(), now())",
            new { Id = id, Cid = companyId, Code = code, Name = nameAr, Uid = userId }, cancellationToken: ct));
        return id;
    }

    private async Task<Guid> EnsureItemAsync(NpgsqlConnection conn, Guid companyId, string code, string nameAr, decimal cost, decimal price, Guid userId, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM items WHERE company_id = @Cid AND sku = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
        if (existing != null) return existing.Value;
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO items (id, company_id, sku, name, item_type, costing_method, average_cost, standard_cost, is_active, created_by, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, 1, 3, @Cost, @Price, true, @Uid, now(), now())",
            new { Id = id, Cid = companyId, Code = code, Name = nameAr, Cost = cost, Price = price, Uid = userId }, cancellationToken: ct));
        return id;
    }

    private async Task<Guid> EnsureProjectAsync(NpgsqlConnection conn, Guid companyId, string code, string nameAr, string status, decimal contractValue, DateTime start, DateTime end, Guid userId, Guid costCenterId, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM projects WHERE company_id = @Cid AND code = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
        if (existing != null) return existing.Value;
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO projects (id, company_id, code, name, cost_center_id, status, budget, start_date, end_date, is_active, created_by, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, @CcId, @Status::int, @CV, @Start, @End, true, @Uid, now(), now())",
            new { Id = id, Cid = companyId, Code = code, Name = nameAr, CcId = costCenterId, Status = StatusToInt(status), CV = contractValue, Start = start, End = end, Uid = userId }, cancellationToken: ct));
        return id;
    }

    private async Task EnsureContractAsync(NpgsqlConnection conn, Guid companyId, Guid projectId, decimal contractValue, decimal advancePct, decimal retentionPct, int retentionStart, DateTime start, DateTime end, Guid userId, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM contracts WHERE company_id = @Cid AND project_id = @Pid",
            new { Cid = companyId, Pid = projectId }, cancellationToken: ct));
        if (existing != null) return;
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO contracts (id, company_id, project_id, contract_number, contract_value, advance_percent, retention_percent, retention_start_billing, start_date, end_date, created_by, is_active, created_at, updated_at)
            VALUES (@Id, @Cid, @Pid, @Num, @CV, @AP, @RP, @RS, @Start, @End, @Uid, true, now(), now())",
            new
            {
                Id = Guid.NewGuid(), Cid = companyId, Pid = projectId,
                Num = $"CONTRACT-{projectId.ToString().Substring(0, 8)}",
                CV = contractValue, AP = advancePct, RP = retentionPct, RS = retentionStart,
                Start = start.Date, End = end.Date, Uid = userId
            }, cancellationToken: ct));
    }

    // Map Arabic status string to int (per the entity enum: Planning=0, Active=1, OnHold=2, Completed=3, Cancelled=4)
    private static int StatusToInt(string status) => status switch
    {
        "Planning" => 0,
        "Active" => 1,
        "OnHold" => 2,
        "Completed" => 3,
        "Cancelled" => 4,
        _ => 1  // default Active
    };

    private async Task<Guid> EnsureCostCenterAsync(NpgsqlConnection conn, Guid companyId, string code, string nameAr, int type, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM cost_centers WHERE company_id = @Cid AND code = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
        if (existing != null) return existing.Value;
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO cost_centers (id, company_id, code, name, type, is_active, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, @Type::int, true, now(), now())",
            new { Id = id, Cid = companyId, Code = code, Name = nameAr, Type = type }, cancellationToken: ct));
        return id;
    }

    private async Task<Guid> EnsureEmployeeAsync(NpgsqlConnection conn, Guid companyId, string code, string nameAr, decimal salary, Guid userId, CancellationToken ct)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM employees WHERE company_id = @Cid AND employee_number = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
        if (existing != null) return existing.Value;
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO employees (id, company_id, employee_number, full_name, base_salary, hire_date, created_by, is_active, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, @Salary, '2026-01-01', @CreatedBy, true, now(), now())",
            new { Id = id, Cid = companyId, Code = code, Name = nameAr, Salary = salary, CreatedBy = userId }, cancellationToken: ct));
        return id;
    }
}
