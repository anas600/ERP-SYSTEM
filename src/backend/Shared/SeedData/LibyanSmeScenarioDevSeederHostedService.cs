using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Sprint 50 — Libyan SME Scenario Dev Seeder
///
/// يُنشئ بيانات تجريبية واقعية لشركة ليبية (Holding + N subsidiaries) للفترة 2025-01-01 إلى 2026-06-30:
///   1. شجرة حسابات موحدة (~70 حساب لكل شركة) — قابلة للتطبيق على كل الشركات
///   2. رصيد افتتاحي في 2025-01-01 (لكل شركة)
///   3. سيناريو دوري شهري: مبيعات، تحصيلات، مشتريات، مدفوعات، رواتب، إيجار، إهلاك
///   4. إقفال شهري للضرائب
///
/// الحجوم المستهدفة:
///   - Holding: ≤ 500 قيد يومية
///   - كل شركة فرعية: ≤ 200 قيد يومية
///   - الفترة: 18 شهر (2025-01-01 → 2026-06-30)
///
/// Gating: requires `IsDevelopment()` AND `Bootstrap:SeedLibyanSme=true`.
/// Idempotent: يفحص existence قبل الإدراج.
/// </summary>
public sealed class LibyanSmeScenarioDevSeederHostedService : IHostedService
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<LibyanSmeScenarioDevSeederHostedService> _logger;

    public LibyanSmeScenarioDevSeederHostedService(
        IHostEnvironment env,
        IConfiguration config,
        IDbConnectionFactory dbFactory,
        ILogger<LibyanSmeScenarioDevSeederHostedService> logger)
    {
        _env = env; _config = config; _dbFactory = dbFactory; _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[SPRINT-50] SeedLibyanSme=false (env=Production) — SKIPPED.");
            return;
        }

        var enabled = _config.GetValue("Bootstrap:SeedLibyanSme", false);
        if (!enabled)
        {
            _logger.LogInformation("[SPRINT-50] SeedLibyanSme=false (default) — SKIPPED.");
            return;
        }

        _logger.LogInformation("[SPRINT-50] SeedLibyanSme=true + env=Development — running scenario…");

        try
        {
            using var conn = await _dbFactory.CreateEphemeralOltpConnectionAsync(ct);

            // 0) تنظيف البيانات القديمة (Sprint 50 — per Anas: "لك الحريه ان تقوم بتنظيف البيانات")
            _logger.LogInformation("[SPRINT-50] Cleaning existing transactional data…");
            await CleanupTransactionalDataAsync(conn, ct);
            _logger.LogInformation("[SPRINT-50] Cleanup done.");

            // 1) أول مستخدم (يستخدم كـ created_by)
            var systemUserId = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1") ?? Guid.Empty;

            // 2) كل الشركات (Holding + subsidiaries)
            var companies = (await conn.QueryAsync<(Guid Id, string Code, string Name, bool IsHolding, int TargetEntries)>(
                @"SELECT id, code, name, is_group AS IsHolding,
                         CASE WHEN is_group THEN 500 ELSE 200 END AS TargetEntries
                  FROM companies
                  WHERE is_active = true
                  ORDER BY is_group DESC, code"))
                .ToList();

            if (companies.Count == 0)
            {
                _logger.LogWarning("[SPRINT-50] No companies found — SKIPPED.");
                return;
            }

            // 2) شجرة الحسابات الموحدة
            var coa = UnifiedCoA.GetAccounts();

            foreach (var (id, code, name, isHolding, target) in companies)
            {
                _logger.LogInformation("[SPRINT-50] Seeding {Code} {Name} (target ≤{Target} entries)", code, name, target);

                // شجرة الحسابات — Idempotent
                var accountMap = await SeedCoAAsync(conn, id, coa, ct);

                // البيانات المرجعية (customers, vendors, items) — بسيطة لكل شركة
                var customerMap = await SeedCustomersAsync(conn, id, systemUserId, ct);
                var vendorMap = await SeedVendorsAsync(conn, id, systemUserId, ct);
                var itemMap = await SeedItemsAsync(conn, id, systemUserId, ct);

                // سيناريو القيود
                var generator = new JournalScenarioGenerator(
                    conn, id, accountMap, customerMap, vendorMap, itemMap,
                    isHolding: isHolding, targetEntries: target,
                    systemUserId: systemUserId, _logger, ct);
                var totalEntries = await generator.RunAsync();

                _logger.LogInformation("[SPRINT-50] {Code}: {Entries} journal entries seeded", code, totalEntries);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SPRINT-50] Seeder FAILED");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    // ============== تنظيف البيانات القديمة ==============
    // TRUNCATE مع CASCADE يحذف كل المعاملات القديمة ويحتفظ بـ:
    //   - companies, accounts, customers, vendors, items, employees, projects, departments
    private static async Task CleanupTransactionalDataAsync(System.Data.IDbConnection conn, CancellationToken ct)
    {
        var tables = new[]
        {
            "journal_lines", "journal_entries",
            "payment_allocations", "payments",
            "receipts", "sales_invoice_lines", "sales_invoices",
            "vendor_bill_lines", "vendor_bills",
            "goods_receipt_lines", "goods_receipts",
            "purchase_order_lines", "purchase_orders",
            "stock_movements", "stock_levels", "stock_reservations",
            "purchase_requests",
        };
        foreach (var t in tables)
        {
            try
            {
                await conn.ExecuteAsync($"TRUNCATE TABLE {t} RESTART IDENTITY CASCADE");
            }
            catch
            {
                // الجدول قد لا يوجد — نتجاهل
            }
        }
    }

    // ============== شجرة الحسابات الموحدة ==============
    // Sprint 51: تنظيف الـ CoA القديم قبل الإدراج — كل حساب بكود غير موجود في الـ UnifiedCoA يُحذف
    private async Task<Dictionary<string, Guid>> SeedCoAAsync(System.Data.IDbConnection conn, Guid companyId, IReadOnlyList<UnifiedCoA.Account> coa, CancellationToken ct)
    {
        var map = new Dictionary<string, Guid>();
        var unifiedCodes = coa.Select(c => c.Code).ToHashSet();

        // 1) Sprint 51: حذف الحسابات القديمة اللي مش في الـ UnifiedCoA
        var unifiedCodesArray = unifiedCodes.ToArray();
        var oldCount = await conn.ExecuteAsync(
            "DELETE FROM accounts WHERE company_id = @CompanyId AND code != ALL(@Codes)",
            new { CompanyId = companyId, Codes = unifiedCodesArray });
        if (oldCount > 0)
        {
            _logger.LogInformation("[SPRINT-51] {Company} dropped {Count} old CoA accounts not in unified set", companyId, oldCount);
        }

        // 2) إدراج الحسابات الموحدة (idempotent)
        foreach (var a in coa)
        {
            // فحص وجود
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM accounts WHERE company_id = @CompanyId AND code = @Code",
                new { CompanyId = companyId, Code = a.Code });
            if (existing.HasValue)
            {
                map[a.Code] = existing.Value;
                continue;
            }
            var id = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO accounts (id, company_id, code, name, type, normal_balance,
                                      parent_account_id, is_postable, is_active, is_intercompany,
                                      created_at, updated_at)
                VALUES (@Id, @CompanyId, @Code, @Name, @AccountType, @NormalBalance,
                        @ParentAccountId, true, true, false, NOW(), NOW())",
                new
                {
                    Id = id, CompanyId = companyId, a.Code, a.Name, a.AccountType,
                    a.NormalBalance, ParentAccountId = (Guid?)null
                });
            map[a.Code] = id;
        }
        return map;
    }

    // ============== Customers (3-5 لكل شركة) ==============
    private async Task<Dictionary<string, Guid>> SeedCustomersAsync(System.Data.IDbConnection conn, Guid companyId, Guid systemUserId, CancellationToken ct)
    {
        var map = new Dictionary<string, Guid>();
        var customers = new[]
        {
            ("C001", "شركة الفجر للتجارة", "ALFjr Trading Co."),
            ("C002", "مؤسسة النور", "Al-Noor Est."),
            ("C003", "شركة الأمل للتوزيع", "Al-Amal Distribution"),
            ("C004", "مكتب الإخاء التجاري", "Al-Ikhaa Trading"),
            ("C005", "شركة النجاح للتوريدات", "Al-Najah Supplies"),
        };
        foreach (var (code, nameAr, nameEn) in customers)
        {
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM customers WHERE company_id = @CompanyId AND code = @Code",
                new { CompanyId = companyId, Code = code });
            if (existing.HasValue) { map[code] = existing.Value; continue; }
            var id = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO customers (id, company_id, code, name, name_en, is_active, created_at, updated_at, created_by)
                VALUES (@Id, @CompanyId, @Code, @Name, @NameEn, true, NOW(), NOW(), @SystemUserId)",
                new { Id = id, CompanyId = companyId, Code = code, Name = nameAr, NameEn = nameEn, SystemUserId = systemUserId });
            map[code] = id;
        }
        return map;
    }

    // ============== Vendors (3-5 لكل شركة) ==============
    private async Task<Dictionary<string, Guid>> SeedVendorsAsync(System.Data.IDbConnection conn, Guid companyId, Guid systemUserId, CancellationToken ct)
    {
        var map = new Dictionary<string, Guid>();
        var vendors = new[]
        {
            ("V001", "موردي الجملة الأولى"),
            ("V002", "شركة الإمداد الموحد"),
            ("V003", "مكتب التعاون التجاري"),
            ("V004", "موردي الجودة العالية"),
        };
        foreach (var (code, nameAr) in vendors)
        {
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM vendors WHERE company_id = @CompanyId AND code = @Code",
                new { CompanyId = companyId, Code = code });
            if (existing.HasValue) { map[code] = existing.Value; continue; }
            var id = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO vendors (id, company_id, code, name, is_active, created_at, updated_at, created_by)
                VALUES (@Id, @CompanyId, @Code, @Name, true, NOW(), NOW(), @SystemUserId)",
                new { Id = id, CompanyId = companyId, Code = code, Name = nameAr, SystemUserId = systemUserId });
            map[code] = id;
        }
        return map;
    }

    // ============== Items (5 منتجات) ==============
    private async Task<Dictionary<string, Guid>> SeedItemsAsync(System.Data.IDbConnection conn, Guid companyId, Guid systemUserId, CancellationToken ct)
    {
        var map = new Dictionary<string, Guid>();
        var items = new (string Code, string Name)[]
        {
            ("IT-001", "منتج A — سلعة استهلاكية"),
            ("IT-002", "منتج B — مواد تنظيف"),
            ("IT-003", "منتج C — قطع غيار"),
            ("IT-004", "منتج D — خدمات"),
            ("IT-005", "منتج E — بضاعة عامة"),
        };
        foreach (var it in items)
        {
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM items WHERE company_id = @CompanyId AND sku = @Code",
                new { CompanyId = companyId, Code = it.Code });
            if (existing.HasValue) { map[it.Code] = existing.Value; continue; }
            var id = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO items (id, company_id, sku, name, is_active, created_at, updated_at, created_by)
                VALUES (@Id, @CompanyId, @Sku, @Name, true, NOW(), NOW(), @SystemUserId)",
                new { Id = id, CompanyId = companyId, Sku = it.Code, Name = it.Name, SystemUserId = systemUserId });
            map[it.Code] = id;
        }
        return map;
    }
}
