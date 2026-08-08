// Sprint 58b (DEC-...) — Professional 4-Level CoA Seeder
//
// Replaces the legacy illogical seeder accounts with a professional
// 4-level chart of accounts that serves both general accounting and
// project/cost-center accounting.
//
// Levels (per Anas's spec 2026-08-08):
//   L1 (1 char)  — Account type:   0=Holding, 1=Assets, 2=Liab, 3=Equity,
//                                    4=Rev, 5=COGS, 6=OpEx, 7=Other, 8=Tax, 9=Closing
//   L2 (1 char)  — Sub-class:      11=Current Assets, 12=Receivables, 13=Inventory, ...
//   L3 (2 chars) — Control account: 1101=Cash, 1102=Bank, 1201=AR, 2101=AP, ...
//   L4 (suffix)  — Detail account: 1101-001=Office Main Cash, 1102-001=Bank ABC, ...
//
// CRITICAL accounting rule: L1, L2, L3 are NOT postable. Only L4 detail
// accounts accept journal entries. The level backfill hosted service
// (Sprint 52a) computes level from the parent chain, so we just need to
// create accounts in the right parent structure.
//
// Gating: requires IsDevelopment() AND Bootstrap:SeedProfessionalCoA=true.
// Idempotent: skips if any L1 root already exists for the company.

using Dapper;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Shared.SeedData;

public sealed class ProfessionalCoASeederHostedService : IHostedService
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<ProfessionalCoASeederHostedService> _logger;

    public ProfessionalCoASeederHostedService(
        IHostEnvironment env, IConfiguration config, IDbConnectionFactory db,
        ILogger<ProfessionalCoASeederHostedService> logger)
    {
        _env = env; _config = config; _db = db; _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[Sprint-58b] env != Development — SKIPPED.");
            return;
        }
        var enabled = _config.GetValue("Bootstrap:SeedProfessionalCoA", false);
        if (!enabled)
        {
            _logger.LogInformation("[Sprint-58b] SeedProfessionalCoA=false (default) — SKIPPED.");
            return;
        }

        _logger.LogInformation("[Sprint-58b] SeedProfessionalCoA=true + env=Development — running…");
        try
        {
            using var conn = (NpgsqlConnection)await _db.CreateEphemeralOltpConnectionAsync(ct);
            // Apply to all active companies (Holding + subsidiaries)
            var companies = (await conn.QueryAsync<Guid>(new CommandDefinition(
                "SELECT id FROM companies WHERE is_active = true ORDER BY is_group DESC, created_at",
                cancellationToken: ct))).ToList();
            if (companies.Count == 0)
            {
                _logger.LogWarning("[Sprint-58b] No companies found — SKIPPED.");
                return;
            }

            foreach (var companyId in companies)
            {
                await SeedForCompanyAsync(conn, companyId, ct);
            }
            _logger.LogInformation("[Sprint-58b] DONE.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sprint-58b] FAILED: {Msg}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedForCompanyAsync(NpgsqlConnection conn, Guid companyId, CancellationToken ct)
    {
        // Idempotency: check for our specific L1 root (e.g., code = "0" for Holding).
        // The unified CoA may have other L1 roots — we don't conflict with them; we just
        // add our professional chart alongside.
        var existingProfessionalL1 = (int)await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM accounts WHERE company_id = @Cid AND code IN ('0','1','2','3','4','5','6','7','8','9')",
            new { Cid = companyId }, cancellationToken: ct));
        if (existingProfessionalL1 >= 10)
        {
            _logger.LogInformation("[Sprint-58b] Company {Cid} already has all 10 professional L1 roots — SKIPPED.", companyId);
            return;
        }

        _logger.LogInformation("[Sprint-58b] Seeding professional CoA for company {Cid}...", companyId);

        // 1) Seed L1 (Class) — roots, is_postable=false
        var l1 = L1Accounts();
        var l1Ids = await SeedLevelAsync(conn, companyId, l1, parentId: null, level: 1, ct);

        // 2) Seed L2 (Sub-class) — parent is L1
        var l2 = L2Accounts();
        var l2Ids = await SeedLevelAsync(conn, companyId, l2, parentResolver: c => l1Ids[c], level: 2, ct);

        // 3) Seed L3 (Control) — parent is L2
        var l3 = L3Accounts();
        var l3Ids = await SeedLevelAsync(conn, companyId, l3, parentResolver: c => l2Ids[c], level: 3, ct);

        // 4) Seed L4 (Detail) — parent is L3
        var l4 = L4Accounts();
        var l4Ids = await SeedLevelAsync(conn, companyId, l4, parentResolver: c => l3Ids[c], level: 4, ct);

        _logger.LogInformation("[Sprint-58b]   Company {Cid}: L1={A}, L2={B}, L3={C}, L4={D}, Total={T}",
            companyId, l1Ids.Count, l2Ids.Count, l3Ids.Count, l4Ids.Count,
            l1Ids.Count + l2Ids.Count + l3Ids.Count + l4Ids.Count);
    }

    /// <summary>
    /// Inserts a batch of accounts. Returns a map of code → id.
    /// For L1 (parentId != null parameter), all share the same parent.
    /// For L2/L3/L4 (parentResolver != null), parent is looked up by code.
    /// </summary>
    private async Task<Dictionary<string, Guid>> SeedLevelAsync(
        NpgsqlConnection conn, Guid companyId, List<AccountDef> accounts,
        Guid? parentId, short level, CancellationToken ct)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var a in accounts)
        {
            Guid? actualParent = parentId ?? (a.ParentCode != null && a.ParentCode != ""
                ? await GetIdByCodeAsync(conn, companyId, a.ParentCode, ct)
                : null);
            // Skip if account already exists (idempotent within level)
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM accounts WHERE company_id = @Cid AND code = @Code",
                new { Cid = companyId, Code = a.Code }, cancellationToken: ct));
            if (existing != null)
            {
                ids[a.Code] = existing.Value;
                continue;
            }
            var id = Guid.NewGuid();
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO accounts (
                    id, company_id, code, name, description, type, normal_balance,
                    parent_account_id, is_intercompany, is_postable, level, is_active,
                    created_at, updated_at
                ) VALUES (
                    @Id, @Cid, @Code, @Name, @Desc, @Type::int, @NB::int,
                    @Parent, false, @Postable, @Level, true,
                    now(), now()
                )",
                new
                {
                    Id = id,
                    Cid = companyId,
                    Code = a.Code,
                    Name = a.NameAr,
                    Desc = a.DescEn,
                    Type = (int)a.Type,
                    NB = (int)a.NormalBalance,
                    Parent = actualParent,
                    Postable = a.IsPostable,
                    Level = level
                }, cancellationToken: ct));
            ids[a.Code] = id;
        }
        return ids;
    }

    // Same as above but uses parentResolver for variable parents
    private async Task<Dictionary<string, Guid>> SeedLevelAsync(
        NpgsqlConnection conn, Guid companyId, List<AccountDef> accounts,
        Func<string, Guid?> parentResolver, short level, CancellationToken ct)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var a in accounts)
        {
            Guid? actualParent = a.ParentCode != null ? parentResolver(a.ParentCode) : null;
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM accounts WHERE company_id = @Cid AND code = @Code",
                new { Cid = companyId, Code = a.Code }, cancellationToken: ct));
            if (existing != null)
            {
                ids[a.Code] = existing.Value;
                continue;
            }
            var id = Guid.NewGuid();
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO accounts (
                    id, company_id, code, name, description, type, normal_balance,
                    parent_account_id, is_intercompany, is_postable, level, is_active,
                    created_at, updated_at
                ) VALUES (
                    @Id, @Cid, @Code, @Name, @Desc, @Type::int, @NB::int,
                    @Parent, false, @Postable, @Level, true,
                    now(), now()
                )",
                new
                {
                    Id = id,
                    Cid = companyId,
                    Code = a.Code,
                    Name = a.NameAr,
                    Desc = a.DescEn,
                    Type = (int)a.Type,
                    NB = (int)a.NormalBalance,
                    Parent = actualParent,
                    Postable = a.IsPostable,
                    Level = level
                }, cancellationToken: ct));
            ids[a.Code] = id;
        }
        return ids;
    }

    private async Task<Guid?> GetIdByCodeAsync(NpgsqlConnection conn, Guid companyId, string code, CancellationToken ct)
    {
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM accounts WHERE company_id = @Cid AND code = @Code",
            new { Cid = companyId, Code = code }, cancellationToken: ct));
    }

    private sealed class AccountDef
    {
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? DescEn { get; set; }
        public AccountType Type { get; set; }
        public NormalBalance NormalBalance { get; set; }
        public string? ParentCode { get; set; }
        public bool IsPostable { get; set; }
    }

    // ============ L1: Account Types (10 roots) ============
    private static List<AccountDef> L1Accounts() => new()
    {
        new() { Code = "0",   NameAr = "حسابات الشركة القابضة", DescEn = "Holding Company Accounts", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, IsPostable = false },
        new() { Code = "1",   NameAr = "الأصول",                  DescEn = "Assets",                  Type = AccountType.Asset,   NormalBalance = NormalBalance.Debit,  IsPostable = false },
        new() { Code = "2",   NameAr = "الخصوم",                  DescEn = "Liabilities",             Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, IsPostable = false },
        new() { Code = "3",   NameAr = "حقوق الملكية",            DescEn = "Equity",                 Type = AccountType.Equity,  NormalBalance = NormalBalance.Credit, IsPostable = false },
        new() { Code = "4",   NameAr = "الإيرادات",               DescEn = "Revenue",                Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, IsPostable = false },
        new() { Code = "5",   NameAr = "تكلفة المبيعات",          DescEn = "Cost of Goods Sold",     Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  IsPostable = false },
        new() { Code = "6",   NameAr = "المصروفات التشغيلية",     DescEn = "Operating Expenses",     Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  IsPostable = false },
        new() { Code = "7",   NameAr = "إيرادات ومصروفات أخرى",  DescEn = "Other Income/Expense",   Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  IsPostable = false },
        new() { Code = "8",   NameAr = "الضرائب",                 DescEn = "Tax",                    Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  IsPostable = false },
        new() { Code = "9",   NameAr = "حسابات الإقفال",          DescEn = "Closing Accounts",       Type = AccountType.Equity,  NormalBalance = NormalBalance.Credit, IsPostable = false },
    };

    // ============ L2: Sub-classification (parent = L1) ============
    private static List<AccountDef> L2Accounts() => new()
    {
        // 1 (Assets)
        new() { Code = "11", NameAr = "الأصول المتداولة",       DescEn = "Current Assets",     Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1", IsPostable = false },
        new() { Code = "12", NameAr = "المدينون",                DescEn = "Receivables",        Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1", IsPostable = false },
        new() { Code = "13", NameAr = "المخزون",                 DescEn = "Inventory",          Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1", IsPostable = false },
        new() { Code = "14", NameAr = "مصروفات مقدمة",           DescEn = "Prepaid Expenses",   Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1", IsPostable = false },
        new() { Code = "15", NameAr = "الأصول الثابتة",          DescEn = "Fixed Assets",       Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1", IsPostable = false },
        new() { Code = "16", NameAr = "أصول غير ملموسة",         DescEn = "Intangible Assets",  Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1", IsPostable = false },
        // 2 (Liabilities)
        new() { Code = "21", NameAr = "الخصوم المتداولة",        DescEn = "Current Liabilities", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2", IsPostable = false },
        new() { Code = "22", NameAr = "الخصوم طويلة الأجل",      DescEn = "Long-term Liabilities", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2", IsPostable = false },
        // 3 (Equity)
        new() { Code = "31", NameAr = "رأس المال",               DescEn = "Capital",            Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3", IsPostable = false },
        new() { Code = "32", NameAr = "أرباح مرحلة",             DescEn = "Retained Earnings",  Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3", IsPostable = false },
        new() { Code = "33", NameAr = "الاحتياطيات",             DescEn = "Reserves",           Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3", IsPostable = false },
        // 4 (Revenue)
        new() { Code = "41", NameAr = "إيرادات المبيعات",        DescEn = "Sales Revenue",      Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4", IsPostable = false },
        new() { Code = "42", NameAr = "إيرادات الخدمات",         DescEn = "Service Revenue",    Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4", IsPostable = false },
        new() { Code = "43", NameAr = "إيرادات المشاريع",        DescEn = "Project Revenue",    Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4", IsPostable = false },
        new() { Code = "49", NameAr = "إيرادات أخرى",            DescEn = "Other Revenue",      Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4", IsPostable = false },
        // 5 (COGS)
        new() { Code = "51", NameAr = "تكلفة البضاعة المباعة",   DescEn = "Cost of Goods Sold", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "5", IsPostable = false },
        new() { Code = "52", NameAr = "تكاليف المشاريع",         DescEn = "Project Costs",      Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "5", IsPostable = false },
        // 6 (OpEx)
        new() { Code = "61", NameAr = "مصاريف إدارية وعمومية",  DescEn = "Administrative Expenses", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "6", IsPostable = false },
        new() { Code = "62", NameAr = "مصاريف بيعية وتسويقية",  DescEn = "Selling Expenses",   Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6", IsPostable = false },
        new() { Code = "63", NameAr = "مصاريف مالية",            DescEn = "Financial Expenses", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6", IsPostable = false },
        // 7 (Other)
        new() { Code = "71", NameAr = "إيرادات أخرى متنوعة",     DescEn = "Miscellaneous Income", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "7", IsPostable = false },
        new() { Code = "72", NameAr = "مصروفات أخرى متنوعة",    DescEn = "Miscellaneous Expenses", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "7", IsPostable = false },
        // 8 (Tax)
        new() { Code = "81", NameAr = "ضريبة الدخل",             DescEn = "Income Tax",         Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "8", IsPostable = false },
        // 9 (Closing)
        new() { Code = "91", NameAr = "ملخص الدخل",              DescEn = "Income Summary",     Type = AccountType.Equity,  NormalBalance = NormalBalance.Credit, ParentCode = "9", IsPostable = false },
        new() { Code = "92", NameAr = "أعمال تحت التنفيذ",       DescEn = "Work In Progress",   Type = AccountType.Asset,   NormalBalance = NormalBalance.Debit,  ParentCode = "9", IsPostable = false },
    };

    // ============ L3: Control Accounts (parent = L2) ============
    private static List<AccountDef> L3Accounts() => new()
    {
        // 11 (Current Assets)
        new() { Code = "1101", NameAr = "النقدية في الصندوق",        DescEn = "Cash on Hand",           Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "11", IsPostable = false },
        new() { Code = "1102", NameAr = "البنوك",                     DescEn = "Bank Accounts",          Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "11", IsPostable = false },
        new() { Code = "1103", NameAr = "عهدة نقدية",                DescEn = "Petty Cash",             Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "11", IsPostable = false },
        // 12 (Receivables)
        new() { Code = "1201", NameAr = "المدينون (ذمم مدينة)",      DescEn = "Accounts Receivable",    Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "12", IsPostable = false },
        new() { Code = "1202", NameAr = "أوراق القبض",                DescEn = "Notes Receivable",       Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "12", IsPostable = false },
        new() { Code = "1203", NameAr = "سلف الموظفين",               DescEn = "Employee Advances",      Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "12", IsPostable = false },
        // 13 (Inventory)
        new() { Code = "1301", NameAr = "المخزون",                    DescEn = "Inventory",              Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "13", IsPostable = false },
        // 14 (Prepaid)
        new() { Code = "1401", NameAr = "مصروفات مقدمة",              DescEn = "Prepaid Expenses",       Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "14", IsPostable = false },
        new() { Code = "1402", NameAr = "ضريبة مدخلات (VAT Input)",   DescEn = "VAT Input (Recoverable)", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, ParentCode = "14", IsPostable = false },
        // 15 (Fixed Assets)
        new() { Code = "1501", NameAr = "أثاث ومعدات مكتبية",        DescEn = "Office Furniture & Equipment", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, ParentCode = "15", IsPostable = false },
        new() { Code = "1502", NameAr = "سيارات",                     DescEn = "Vehicles",               Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "15", IsPostable = false },
        new() { Code = "1503", NameAr = "معدات ثقيلة",                DescEn = "Heavy Equipment",        Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "15", IsPostable = false },
        new() { Code = "1504", NameAr = "مباني",                      DescEn = "Buildings",              Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "15", IsPostable = false },
        new() { Code = "1505", NameAr = "أراضي",                      DescEn = "Land",                   Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "15", IsPostable = false },
        new() { Code = "1590", NameAr = "مجمع الإهلاك",               DescEn = "Accumulated Depreciation (contra-asset)", Type = AccountType.Asset, NormalBalance = NormalBalance.Credit, ParentCode = "15", IsPostable = false },
        // 16 (Intangible)
        new() { Code = "1601", NameAr = "برامج وأصول غير ملموسة",   DescEn = "Software & Intangibles", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "16", IsPostable = false },
        // 21 (Current Liabilities)
        new() { Code = "2101", NameAr = "الدائنون (ذمم دائنة)",     DescEn = "Accounts Payable",       Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "21", IsPostable = false },
        new() { Code = "2102", NameAr = "قروض قصيرة الأجل",          DescEn = "Short-term Loans",       Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "21", IsPostable = false },
        new() { Code = "2103", NameAr = "مصروفات مستحقة",            DescEn = "Accrued Expenses",       Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "21", IsPostable = false },
        new() { Code = "2104", NameAr = "ضريبة مخرجات (VAT Output)", DescEn = "VAT Output (Payable)",   Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "21", IsPostable = false },
        new() { Code = "2105", NameAr = "رواتب مستحقة",              DescEn = "Accrued Salaries",       Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "21", IsPostable = false },
        // 22 (Long-term Liabilities)
        new() { Code = "2201", NameAr = "قروض طويلة الأجل",          DescEn = "Long-term Loans",        Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "22", IsPostable = false },
        // 31 (Capital)
        new() { Code = "3101", NameAr = "رأس المال",                  DescEn = "Share Capital",          Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "31", IsPostable = false },
        new() { Code = "3102", NameAr = "المساهمون / الشركاء",       DescEn = "Shareholders / Partners", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "31", IsPostable = false },
        // 32 (Retained Earnings)
        new() { Code = "3201", NameAr = "أرباح مرحلة",                DescEn = "Retained Earnings",      Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "32", IsPostable = false },
        new() { Code = "3202", NameAr = "صافي دخل السنة",            DescEn = "Current Year Net Income", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "32", IsPostable = false },
        // 33 (Reserves)
        new() { Code = "3301", NameAr = "احتياطي قانوني",             DescEn = "Statutory Reserve",      Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "33", IsPostable = false },
        new() { Code = "3302", NameAr = "احتياطي اختياري",            DescEn = "Optional Reserve",       Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "33", IsPostable = false },
        // 41 (Sales Revenue)
        new() { Code = "4101", NameAr = "إيرادات المبيعات",           DescEn = "Sales Revenue",          Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "41", IsPostable = false },
        // 42 (Service Revenue)
        new() { Code = "4201", NameAr = "إيرادات الخدمات",            DescEn = "Service Revenue",        Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "42", IsPostable = false },
        // 43 (Project Revenue)
        new() { Code = "4301", NameAr = "إيرادات المستخلصات",         DescEn = "Progress Billing Revenue", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "43", IsPostable = false },
        new() { Code = "4302", NameAr = "إيرادات أعمال تحت التنفيذ", DescEn = "WIP Revenue (Cost-Plus)", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "43", IsPostable = false },
        // 49 (Other Revenue)
        new() { Code = "4901", NameAr = "إيرادات أخرى متنوعة",       DescEn = "Miscellaneous Revenue",  Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "49", IsPostable = false },
        // 51 (COGS)
        new() { Code = "5101", NameAr = "تكلفة البضاعة المباعة",     DescEn = "COGS",                   Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "51", IsPostable = false },
        // 52 (Project Costs)
        new() { Code = "5201", NameAr = "مواد مباشرة (مشاريع)",      DescEn = "Direct Materials (Projects)", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "52", IsPostable = false },
        new() { Code = "5202", NameAr = "عمالة مباشرة (مشاريع)",     DescEn = "Direct Labor (Projects)", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "52", IsPostable = false },
        new() { Code = "5203", NameAr = "مصروفات مشاريع غير مباشرة", DescEn = "Indirect Project Costs", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "52", IsPostable = false },
        // 61 (Admin)
        new() { Code = "6101", NameAr = "رواتب وأجور",               DescEn = "Salaries & Wages",       Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6102", NameAr = "إيجار",                      DescEn = "Rent",                   Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6103", NameAr = "كهرباء ومياه",               DescEn = "Utilities",              Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6104", NameAr = "اتصالات وإنترنت",            DescEn = "Telecom & Internet",     Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6105", NameAr = "مستلزمات مكتبية",            DescEn = "Office Supplies",        Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6106", NameAr = "مصروف إهلاك",                DescEn = "Depreciation Expense",   Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6107", NameAr = "صيانة",                      DescEn = "Maintenance",            Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        new() { Code = "6108", NameAr = "تأمين",                      DescEn = "Insurance",              Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "61", IsPostable = false },
        // 62 (Selling)
        new() { Code = "6201", NameAr = "تسويق وإعلان",               DescEn = "Marketing & Advertising", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "62", IsPostable = false },
        new() { Code = "6202", NameAr = "عمولات مبيعات",              DescEn = "Sales Commissions",      Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "62", IsPostable = false },
        // 63 (Financial)
        new() { Code = "6301", NameAr = "رسوم بنكية",                 DescEn = "Bank Charges",           Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "63", IsPostable = false },
        new() { Code = "6302", NameAr = "مصروف فائدة",                DescEn = "Interest Expense",       Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "63", IsPostable = false },
        new() { Code = "6303", NameAr = "فروقات عملة",                DescEn = "FX Differences",         Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "63", IsPostable = false },
        // 71 (Other Income)
        new() { Code = "7101", NameAr = "إيرادات استثمارات",          DescEn = "Investment Income",      Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "71", IsPostable = false },
        new() { Code = "7102", NameAr = "إيرادات متنوعة",             DescEn = "Miscellaneous Income",   Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "71", IsPostable = false },
        // 72 (Other Expenses)
        new() { Code = "7201", NameAr = "خسائر متنوعة",               DescEn = "Miscellaneous Losses",   Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "72", IsPostable = false },
        // 81 (Tax)
        new() { Code = "8101", NameAr = "ضريبة الدخل",                DescEn = "Income Tax",             Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "81", IsPostable = false },
        // 91 (Closing)
        new() { Code = "9101", NameAr = "ملخص الدخل (إقفال)",        DescEn = "Income Summary (Closing)", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "91", IsPostable = false },
        // 92 (WIP)
        new() { Code = "9201", NameAr = "أعمال تحت التنفيذ (WIP)",   DescEn = "Work In Progress (WIP)", Type = AccountType.Asset,   NormalBalance = NormalBalance.Debit,  ParentCode = "92", IsPostable = false },
    };

    // ============ L4: Detail Accounts (parent = L3) — only postable ============
    private static List<AccountDef> L4Accounts() => new()
    {
        // 1101 Cash
        new() { Code = "1101-001", NameAr = "النقدية في الصندوق الرئيسي", DescEn = "Office Main Cash",       Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1101", IsPostable = true },
        // 1102 Banks
        new() { Code = "1102-001", NameAr = "مصرف الجمهورية - حساب جاري", DescEn = "CDBL - Checking",          Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1102", IsPostable = true },
        new() { Code = "1102-002", NameAr = "مصرف الوحدة - حساب توفير", DescEn = "Wahda Bank - Savings",      Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1102", IsPostable = true },
        // 1103 Petty Cash
        new() { Code = "1103-001", NameAr = "عهدة السكرتارية",              DescEn = "Secretarial Petty Cash",  Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1103", IsPostable = true },
        // 1201 AR (Customers)
        new() { Code = "1201-001", NameAr = "عميل - شركة النور",            DescEn = "Customer - Al-Noor Co",   Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1201", IsPostable = true },
        new() { Code = "1201-002", NameAr = "عميل - مؤسسة الأمل",           DescEn = "Customer - Al-Amal Est.",  Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1201", IsPostable = true },
        new() { Code = "1201-003", NameAr = "عميل - شركة الفجر للتجارة",   DescEn = "Customer - Fajr Trading", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1201", IsPostable = true },
        new() { Code = "1201-004", NameAr = "عميل - مجموعة الصفا",          DescEn = "Customer - Al-Safa Group", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, ParentCode = "1201", IsPostable = true },
        new() { Code = "1201-005", NameAr = "عميل - شركة الدلتا",           DescEn = "Customer - Delta Co",     Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1201", IsPostable = true },
        new() { Code = "1201-006", NameAr = "عميل - مؤسسة الجبل",           DescEn = "Customer - Al-Jabal Est.", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, ParentCode = "1201", IsPostable = true },
        // 1301 Inventory
        new() { Code = "1301-001", NameAr = "مخزون البضاعة",                DescEn = "Merchandise Inventory",   Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1301", IsPostable = true },
        new() { Code = "1301-002", NameAr = "مخزون المواد الخام",            DescEn = "Raw Materials Inventory", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1301", IsPostable = true },
        // 1401 Prepaid
        new() { Code = "1401-001", NameAr = "إيجار مقدم",                    DescEn = "Prepaid Rent",            Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1401", IsPostable = true },
        // 1501 Office Furniture
        new() { Code = "1501-001", NameAr = "أثاث مكتبي",                    DescEn = "Office Furniture",        Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1501", IsPostable = true },
        new() { Code = "1501-002", NameAr = "معدات مكتبية",                  DescEn = "Office Equipment",        Type = AccountType.Asset, NormalBalance = NormalBalance.Debit,  ParentCode = "1501", IsPostable = true },
        // 1503 Heavy Equipment
        new() { Code = "1503-001", NameAr = "معدات ثقيلة - خلاطات",         DescEn = "Heavy Equipment - Mixers", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, ParentCode = "1503", IsPostable = true },
        new() { Code = "1503-002", NameAr = "معدات ثقيلة - رافعات",         DescEn = "Heavy Equipment - Cranes", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, ParentCode = "1503", IsPostable = true },
        // 1590 Accumulated Depreciation
        new() { Code = "1590-001", NameAr = "مجمع إهلاك الأثاث المكتبي",   DescEn = "Accum. Depreciation - Furniture", Type = AccountType.Asset, NormalBalance = NormalBalance.Credit, ParentCode = "1590", IsPostable = true },
        new() { Code = "1590-002", NameAr = "مجمع إهلاك المعدات المكتبية", DescEn = "Accum. Depreciation - Equipment", Type = AccountType.Asset, NormalBalance = NormalBalance.Credit, ParentCode = "1590", IsPostable = true },
        new() { Code = "1590-003", NameAr = "مجمع إهلاك المعدات الثقيلة", DescEn = "Accum. Depreciation - Heavy Equipment", Type = AccountType.Asset, NormalBalance = NormalBalance.Credit, ParentCode = "1590", IsPostable = true },
        // 2101 AP (Vendors)
        new() { Code = "2101-001", NameAr = "مورد - شركة الفجر للمواد",     DescEn = "Vendor - Fajr Materials", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2101", IsPostable = true },
        new() { Code = "2101-002", NameAr = "مورد - مؤسسة النجم",            DescEn = "Vendor - Al-Najm Est.",   Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2101", IsPostable = true },
        new() { Code = "2101-003", NameAr = "مورد - شركة الأفق",            DescEn = "Vendor - Al-Ofuq Co",     Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2101", IsPostable = true },
        new() { Code = "2101-004", NameAr = "مورد - مجموعة الشمس",           DescEn = "Vendor - Al-Shams Group", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2101", IsPostable = true },
        new() { Code = "2101-005", NameAr = "مورد - شركة النهر",            DescEn = "Vendor - Al-Nahr Co",     Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2101", IsPostable = true },
        // 2104 VAT Output
        new() { Code = "2104-001", NameAr = "ضريبة القيمة المضافة على المبيعات", DescEn = "VAT on Sales (5%)", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2104", IsPostable = true },
        // 2105 Accrued Salaries
        new() { Code = "2105-001", NameAr = "رواتب مستحقة الدفع",            DescEn = "Accrued Salaries Payable", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2105", IsPostable = true },
        // 2201 Long-term Loans
        new() { Code = "2201-001", NameAr = "قرض مصرف الجمهورية - طويل الأجل", DescEn = "CDBL Long-term Loan", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, ParentCode = "2201", IsPostable = true },
        // 3101 Capital
        new() { Code = "3101-001", NameAr = "رأس مال المجموعة",              DescEn = "Group Share Capital",     Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3101", IsPostable = true },
        // 3201 Retained Earnings
        new() { Code = "3201-001", NameAr = "أرباح مرحلة - سنوات سابقة",     DescEn = "Retained Earnings - Prior Years", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3201", IsPostable = true },
        // 3202 Current Year Net Income
        new() { Code = "3202-001", NameAr = "صافي دخل السنة الجارية",        DescEn = "Current Year Net Income",   Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3202", IsPostable = true },
        // 3301 Statutory Reserve
        new() { Code = "3301-001", NameAr = "احتياطي قانوني",                DescEn = "Statutory Reserve",       Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "3301", IsPostable = true },
        // 4101 Sales Revenue
        new() { Code = "4101-001", NameAr = "مبيعات بضاعة",                  DescEn = "Merchandise Sales",       Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4101", IsPostable = true },
        // 4201 Service Revenue
        new() { Code = "4201-001", NameAr = "خدمات استشارية",                DescEn = "Consulting Services",     Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4201", IsPostable = true },
        // 4301 Project Revenue
        new() { Code = "4301-001", NameAr = "مستخلصات - مشروع بناء المدرسة", DescEn = "Progress Billings - School Project", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4301", IsPostable = true },
        new() { Code = "4301-002", NameAr = "مستخلصات - مشروع توريد المواد", DescEn = "Progress Billings - Materials Supply", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4301", IsPostable = true },
        new() { Code = "4301-003", NameAr = "مستخلصات - مشروع صيانة الطرق", DescEn = "Progress Billings - Road Maintenance", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, ParentCode = "4301", IsPostable = true },
        // 5101 COGS
        new() { Code = "5101-001", NameAr = "تكلفة البضاعة المباعة",        DescEn = "Merchandise COGS",        Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "5101", IsPostable = true },
        // 5201 Direct Materials
        new() { Code = "5201-001", NameAr = "مواد - مشروع بناء المدرسة",     DescEn = "Materials - School Project", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "5201", IsPostable = true },
        new() { Code = "5201-002", NameAr = "مواد - مشروع توريد المواد",     DescEn = "Materials - Materials Supply", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "5201", IsPostable = true },
        new() { Code = "5201-003", NameAr = "مواد - مشروع صيانة الطرق",     DescEn = "Materials - Road Maintenance", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "5201", IsPostable = true },
        // 5203 Indirect Project Costs
        new() { Code = "5203-001", NameAr = "إيجار معدات ثقيلة",              DescEn = "Heavy Equipment Rental",    Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "5203", IsPostable = true },
        // 5202 Direct Labor
        new() { Code = "5202-001", NameAr = "عمالة مباشرة - مقاولات",       DescEn = "Direct Labor - Construction", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "5202", IsPostable = true },
        // 6101 Salaries
        new() { Code = "6101-001", NameAr = "رواتب الموظفين",                DescEn = "Staff Salaries",          Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6101", IsPostable = true },
        // 6102 Rent
        new() { Code = "6102-001", NameAr = "إيجار المكاتب",                 DescEn = "Office Rent",            Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6102", IsPostable = true },
        // 6103 Utilities
        new() { Code = "6103-001", NameAr = "كهرباء",                        DescEn = "Electricity",            Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6103", IsPostable = true },
        new() { Code = "6103-002", NameAr = "مياه",                          DescEn = "Water",                  Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6103", IsPostable = true },
        // 6104 Telecom
        new() { Code = "6104-001", NameAr = "هاتف وإنترنت",                 DescEn = "Phone & Internet",       Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6104", IsPostable = true },
        // 6105 Office Supplies
        new() { Code = "6105-001", NameAr = "مستلزمات مكتبية",               DescEn = "Office Supplies",        Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6105", IsPostable = true },
        // 6106 Depreciation
        new() { Code = "6106-001", NameAr = "مصروف إهلاك الأثاث المكتبي",  DescEn = "Depreciation - Furniture", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "6106", IsPostable = true },
        new() { Code = "6106-002", NameAr = "مصروف إهلاك المعدات المكتبية", DescEn = "Depreciation - Equipment", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "6106", IsPostable = true },
        new() { Code = "6106-003", NameAr = "مصروف إهلاك المعدات الثقيلة", DescEn = "Depreciation - Heavy Equipment", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "6106", IsPostable = true },
        // 6107 Maintenance
        new() { Code = "6107-001", NameAr = "صيانة المعدات",                 DescEn = "Equipment Maintenance",  Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6107", IsPostable = true },
        // 6108 Insurance
        new() { Code = "6108-001", NameAr = "تأمين على المعدات",             DescEn = "Equipment Insurance",    Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6108", IsPostable = true },
        // 6201 Marketing
        new() { Code = "6201-001", NameAr = "تسويق وإعلان",                  DescEn = "Marketing & Advertising", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, ParentCode = "6201", IsPostable = true },
        // 6301 Bank Charges
        new() { Code = "6301-001", NameAr = "رسوم بنكية",                    DescEn = "Bank Charges",           Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6301", IsPostable = true },
        // 6302 Interest
        new() { Code = "6302-001", NameAr = "فائدة على القروض",              DescEn = "Loan Interest",          Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "6302", IsPostable = true },
        // 8101 Income Tax
        new() { Code = "8101-001", NameAr = "ضريبة الدخل المستحقة",          DescEn = "Income Tax Payable",     Type = AccountType.Expense, NormalBalance = NormalBalance.Debit,  ParentCode = "8101", IsPostable = true },
        // 9101 Income Summary
        new() { Code = "9101-001", NameAr = "ملخص الدخل - إقفال",            DescEn = "Income Summary - Closing", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, ParentCode = "9101", IsPostable = true },
        // 9201 WIP
        new() { Code = "9201-001", NameAr = "WIP - مشروع بناء المدرسة",      DescEn = "WIP - School Project",   Type = AccountType.Asset,   NormalBalance = NormalBalance.Debit,  ParentCode = "9201", IsPostable = true },
        new() { Code = "9201-002", NameAr = "WIP - مشروع توريد المواد",      DescEn = "WIP - Materials Supply", Type = AccountType.Asset,   NormalBalance = NormalBalance.Debit,  ParentCode = "9201", IsPostable = true },
        new() { Code = "9201-003", NameAr = "WIP - مشروع صيانة الطرق",      DescEn = "WIP - Road Maintenance", Type = AccountType.Asset,   NormalBalance = NormalBalance.Debit,  ParentCode = "9201", IsPostable = true },
    };
}
