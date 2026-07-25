// Phase 6.0b (P6-0b) — Default Holding bootstrap.
//
// Why this exists: PR #139 (Phase 6.0) dropped every row from every business table
// and rebuilt the schema without `tenant_id` (per CONSTITUTION.md §3: Multi-Company,
// not Multi-Tenancy). The system is now stateless w.r.t. tenants — a user logs in,
// the app resolves a list of `companies` they belong to via `user_companies`, and
// picks a current `company_id` from an X-Company-Id header. But the very first
// `companies` row — the default Holding — has to come from somewhere, or the whole
// auth + register flow has no place to hang.
//
// This hosted service runs once at app startup, AFTER DataTypeHostedService (which
// creates the `companies`, `accounts`, `units_of_measure`, `item_categories` tables
// from the JSON data-types) and BEFORE MigrationRunnerHostedService + the HTTP
// pipeline. It is fully idempotent: the very first step is a SELECT for the seed
// Holding; if it already exists, the whole bootstrap is a no-op.
//
// All SQL is hand-written via Dapper on a fresh OLTP connection. The C# entities /
// repos were refactored in Phase 6.1c to drop their legacy multi-tenant fields.
// Raw SQL keeps this file aligned with the Phase 6 multi-company spirit.

using Dapper;
using ERPSystem.Modules.Companies.Infrastructure;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.SeedData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Host.Bootstrap;

/// <summary>
/// خدمة تعمل مرة واحدة مع بدء التطبيق — تتأكّد من وجود شركة "Holding" افتراضية
/// في جدول <c>companies</c>، وتزرع دليل الحسابات (CoA) ووحدات القياس والتصنيفات
/// الخاصة بها عند الحاجة.
/// <para>
/// <b>Phase 6.0b (P6-0b)</b>: بعد الـ schema reset في PR #139، لا يوجد أي صف في
/// جدول <c>companies</c>. بدون Holding، لا يمكن تسجيل أول مستخدم ولا تسجيل
/// الدخول. هذه الخدمة تحلّ المشكلة بدون أن تترك أي مرجع لـ <c>tenant_id</c>
/// في الكود الجديد.
/// </para>
/// <para>
/// <b>Idempotency</b>: أول خطوة هي التحقّق من وجود الـ Holding عبر
/// <see cref="ICompanyRepository.GetHoldingCompanyIdAsync(CancellationToken)"/>.
/// إذا وُجد، تنتهي الخدمة فوراً. وإلا تُنشئه، ثم تبذر الـ CoA والـ UoMs
/// والتصنيفات. كل الـ INSERTs تستخدم <c>ON CONFLICT DO NOTHING</c> (أو
/// pre-check) لتكون آمنة عند التشغيل المتزامن لـ replicas.
/// </para>
/// <para>
/// <b>Configuration</b>:
/// <list type="bullet">
///   <item><c>Deployment:DefaultHoldingName</c> — اسم الـ Holding (الافتراضي: "Holding Enterprise").</item>
///   <item><c>Deployment:DefaultCurrency</c> — العملة الأساسية (الافتراضي: "LYD").</item>
/// </list>
/// </para>
/// </summary>
public sealed class DefaultHoldingBootstrapHostedService : IHostedService
{
    /// <summary>
    /// الـ UUID الثابت للـ Holding الافتراضي. نفسه موجود في
    /// src/backend/Host/data-types/seeds/seed_meta.json (holding_company_id)
    /// لتستخدمه الـ seed scripts وأي قراءة لاحقة.
    /// </summary>
    public static readonly Guid DefaultHoldingId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>رمز الـ Holding في عمود companies.code — ثابت ومعرّف دستورياً.</summary>
    private const string HoldingCode = "000";

    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DefaultHoldingBootstrapHostedService> _logger;

    public DefaultHoldingBootstrapHostedService(
        IConfiguration config,
        IDbConnectionFactory db,
        IServiceScopeFactory scopeFactory,
        ILogger<DefaultHoldingBootstrapHostedService> logger)
    {
        _config = config;
        _db = db;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var holdingName = _config["Deployment:DefaultHoldingName"] ?? "Holding Enterprise";
        var currency = (_config["Deployment:DefaultCurrency"] ?? "LYD").ToUpperInvariant();
        var holdingId = DefaultHoldingId;

        _logger.LogInformation(
            "[P6-0b] DefaultHoldingBootstrap starting (id={HoldingId}, name='{Name}', currency={Currency})",
            holdingId, holdingName, currency);

        try
        {
            // 1) Idempotency check — هل الـ Holding موجود فعلاً؟
            //    الشركة القابضة = code='000' AND is_group=true AND parent_company_id IS NULL.
            //    ICompanyRepository is Scoped — resolve it via a scope (hosted service itself is Singleton).
            using var scope = _scopeFactory.CreateScope();
            var companies = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
            var existing = await companies.GetHoldingCompanyIdAsync(cancellationToken);
            if (existing.HasValue)
            {
                _logger.LogInformation(
                    "[P6-0b] Default Holding already exists (id={Id}) — bootstrap is a no-op",
                    existing.Value);
                return;
            }

            // 2) أنشئ صف الـ Holding في companies عبر raw SQL.
            //    السبب: في الـ multi-company model، لا يوجد عمود قديم للتأجير في الـ schema.
            //    الـ INSERT عبر الـ entity سيرمي SqlException: column قديم غير موجود.
            //    الـ raw SQL يلتزم بمتطلبات الـ schema الجديدة (CONSTITUTION.md §3).
            using (var conn = await _db.CreateOltpConnectionAsync(cancellationToken))
            {
                var now = DateTime.UtcNow;
                var rows = await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO companies
                        (id, code, name, legal_name, parent_company_id,
                         is_group, base_currency, is_active, created_at, updated_at)
                    VALUES
                        (@Id, @Code, @Name, @LegalName, NULL,
                         true, @Currency, true, @Now, @Now)
                    ON CONFLICT (id) DO NOTHING;",
                    new
                    {
                        Id = holdingId,
                        Code = HoldingCode,
                        Name = holdingName,
                        LegalName = holdingName,
                        Currency = currency,
                        Now = now,
                    },
                    cancellationToken: cancellationToken));

                _logger.LogInformation(
                    "[P6-0b] Default Holding inserted (id={Id}, rows={Rows}, code={Code}, currency={Currency})",
                    holdingId, rows, HoldingCode, currency);
            }

            // 3) ابذر دليل الحسابات (47 حساب) للـ Holding عبر raw SQL.
            //    لا نستدعي IAccountRepository.EnsureDefaultCoAAsync لأن الـ INSERT
            //    داخله يكتب إلى tenant_id العمود الذي لم يعد موجوداً في الـ schema
            //    الجديد. نكرّر نمط الـ batched unnest من AccountRepository (DEC-093)
            //    لتفادي 47 round-trip متتالية.
            var coaCount = await SeedDefaultCoAAsync(holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default CoA seeded (count={Count}, holdingId={HoldingId})",
                coaCount, holdingId);

            // 4) ابذر وحدات القياس والتصنيفات بنفس الأسلوب (raw SQL).
            var uomCount = await SeedDefaultUoMsAsync(holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default UoMs seeded (count={Count}, holdingId={HoldingId})",
                uomCount, holdingId);

            var catCount = await SeedDefaultCategoriesAsync(holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default Item Categories seeded (count={Count}, holdingId={HoldingId})",
                catCount, holdingId);

            _logger.LogInformation(
                "[P6-0b] DefaultHoldingBootstrap completed successfully (holdingId={HoldingId})",
                holdingId);
        }
        catch (Exception ex)
        {
            // Phase 6.3 hotfix: لا نرمي exception — نسجل فقط ونكمل.
            // السبب: في الـ debug builds على CI، كان الـ app يبدأ حتى يعرض
            // /api/health الخطأ الحقيقي بدل 502. لكن في production هذا
            // يخفي مشاكل حقيقية. نضع flag للسماح بإرجاع exception.
            //
            // الافتراضي: throw (fail loud). للـ debug builds اضبط:
            //   "Bootstrap:AllowBootstrapFailure": "true"
            var allowFailure = _config.GetValue<bool>("Bootstrap:AllowBootstrapFailure", false);
            if (allowFailure)
            {
                _logger.LogError(ex,
                    "[P6-0b] DefaultHoldingBootstrap failed — Bootstrap:AllowBootstrapFailure=true, app will start in degraded mode (register/login will not work)");
                return;
            }
            _logger.LogCritical(ex,
                "[P6-0b] DefaultHoldingBootstrap failed — throwing so the app does NOT start with broken state (register/login would fail)");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ============ Internal seed helpers (raw SQL) ============

    /// <summary>
    /// يبذر الـ 47 حساباً من <see cref="DefaultCoASeed.HoldingAccounts"/> في جدول
    /// <c>accounts</c>، كلها مرتبطة بـ Holding عن طريق <c>company_id</c>.
    /// Idempotent: لو وُجد حساب بالـ code 0000 (جذر شجرة CoA) يُعتبر CoA موجوداً
    /// ويُعاد 0.
    /// </summary>
    private async Task<int> SeedDefaultCoAAsync(Guid holdingId, CancellationToken ct)
    {
        // Pre-check: لو الـ CoA موجود بالفعل (نتحقّق من الحساب الجذر 0000)، نتخطّى.
        using (var conn = await _db.CreateOltpConnectionAsync(ct))
        {
            var hasRoot = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(*) FROM accounts WHERE company_id = @HoldingId AND code = '0000' LIMIT 1",
                new { HoldingId = holdingId }, cancellationToken: ct));
            if (hasRoot > 0) return 0;
        }

        // نفس نمط الـ AccountRepository: pass 1 = roots (no parent)، pass 2 = children
        // (parent must be resolved أولاً). نولّد UUIDs لكل صف ونبني الـ hierarchy.
        var idByCode = new Dictionary<string, Guid>();
        var rows = new List<AccountRow>(DefaultCoASeed.HoldingAccounts.Length);

        // Pass 1: roots
        foreach (var (code, name, type, parentCode, postable, intercompany)
                 in DefaultCoASeed.HoldingAccounts.Where(e => e.ParentCode == null))
        {
            var id = Guid.NewGuid();
            idByCode[code] = id;
            rows.Add(new AccountRow(
                Id: id,
                CompanyId: holdingId,
                Code: code,
                Name: name,
                Type: (int)type,
                NormalBalance: ResolveNormalBalance(type),
                ParentId: null,
                IsPostable: postable,
                IsIntercompany: intercompany));
        }

        // Pass 2: children
        foreach (var (code, name, type, parentCode, postable, intercompany) in DefaultCoASeed.HoldingAccounts)
        {
            if (parentCode == null) continue;
            if (!idByCode.TryGetValue(parentCode, out var parentId))
            {
                _logger.LogError(
                    "[P6-0b] CoA seed bug: parent code {ParentCode} not resolved before child {Code}",
                    parentCode, code);
                throw new InvalidOperationException(
                    $"CoA seed bug: parent code {parentCode} not resolved before child {code}");
            }
            var id = Guid.NewGuid();
            idByCode[code] = id;
            rows.Add(new AccountRow(
                Id: id,
                CompanyId: holdingId,
                Code: code,
                Name: name,
                Type: (int)type,
                NormalBalance: ResolveNormalBalance(type),
                ParentId: parentId,
                IsPostable: postable,
                IsIntercompany: intercompany));
        }

        // Single batched INSERT using unnest() — 1 round-trip for all 47 rows
        // (mirrors the DEC-093 perf pattern from AccountRepository.EnsureDefaultCoAAsync).
        const string batchInsertSql = @"
            INSERT INTO accounts
                (id, company_id, code, name, type, normal_balance,
                 parent_account_id, is_postable, is_active, is_intercompany, created_at, updated_at)
            SELECT u.id, @CompanyId, u.code, u.name, u.type, u.balance,
                   u.parent_id, u.postable, true, u.intercompany, now(), now()
            FROM unnest(
                @Ids::uuid[], @Codes::text[], @Names::text[],
                @Types::int[], @Balances::int[],
                @ParentIds::uuid[], @Postables::bool[], @Inters::bool[]
            ) AS u(id, code, name, type, balance, parent_id, postable, intercompany);";

        using var conn2 = await _db.CreateOltpConnectionAsync(ct);
        var inserted = await conn2.ExecuteAsync(new CommandDefinition(batchInsertSql, new
        {
            CompanyId = holdingId,
            Ids = rows.Select(r => r.Id).ToArray(),
            Codes = rows.Select(r => r.Code).ToArray(),
            Names = rows.Select(r => r.Name).ToArray(),
            Types = rows.Select(r => r.Type).ToArray(),
            Balances = rows.Select(r => r.NormalBalance).ToArray(),
            ParentIds = rows.Select(r => r.ParentId).ToArray(),
            Postables = rows.Select(r => r.IsPostable).ToArray(),
            Inters = rows.Select(r => r.IsIntercompany).ToArray(),
        }, cancellationToken: ct));

        return inserted;
    }

    /// <summary>
    /// يبذر الـ 6 وحدات قياس من <see cref="DefaultInventorySeed.DefaultUoMs"/> في
    /// جدول <c>units_of_measure</c>، مرتبطة بـ Holding عن طريق <c>company_id</c>.
    /// Idempotent: ON CONFLICT (id) DO NOTHING (id فريد من نوعه لكل تشغيل).
    /// </summary>
    private async Task<int> SeedDefaultUoMsAsync(Guid holdingId, CancellationToken ct)
    {
        var rows = DefaultInventorySeed.DefaultUoMs
            .Select(uom => new UomRow(
                Id: Guid.NewGuid(),
                CompanyId: holdingId,
                Code: uom.Code,
                Name: uom.Name,
                Symbol: uom.Symbol,
                IsActive: true))
            .ToArray();

        const string sql = @"
            INSERT INTO units_of_measure
                (id, company_id, code, name, symbol, is_active, created_at)
            SELECT u.id, u.company_id, u.code, u.name, u.symbol, u.is_active, now()
            FROM unnest(
                @Ids::uuid[], @CompanyIds::uuid[], @Codes::text[],
                @Names::text[], @Symbols::text[], @Actives::bool[]
            ) AS u(id, company_id, code, name, symbol, is_active)
            ON CONFLICT (id) DO NOTHING;";

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var inserted = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Ids = rows.Select(r => r.Id).ToArray(),
            CompanyIds = rows.Select(r => r.CompanyId).ToArray(),
            Codes = rows.Select(r => r.Code).ToArray(),
            Names = rows.Select(r => r.Name).ToArray(),
            Symbols = rows.Select(r => r.Symbol).ToArray(),
            Actives = rows.Select(r => r.IsActive).ToArray(),
        }, cancellationToken: ct));

        return inserted;
    }

    /// <summary>
    /// يبذر الـ 5 تصنيفات أصناف من <see cref="DefaultInventorySeed.DefaultCategories"/>
    /// في جدول <c>item_categories</c>، مرتبطة بـ Holding عن طريق <c>company_id</c>.
    /// Idempotent: لو التصنيف الجذر "RM" موجود بالفعل (pre-check) يُعتبر الزرع
    /// تم ويُعاد 0.
    /// </summary>
    private async Task<int> SeedDefaultCategoriesAsync(Guid holdingId, CancellationToken ct)
    {
        // Pre-check: لو التصنيف الجذر "RM" موجود، نتخطّى.
        using (var conn0 = await _db.CreateOltpConnectionAsync(ct))
        {
            var hasRoot = await conn0.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(*) FROM item_categories WHERE company_id = @HoldingId AND code = 'RM' LIMIT 1",
                new { HoldingId = holdingId }, cancellationToken: ct));
            if (hasRoot > 0) return 0;
        }

        // كل التصنيفات في الـ seed الحالي roots (ParentCode == null)، فلا حاجة لـ
        // مرحلتين مثل الـ CoA. نبني UUID لكل صف ونبادر بـ batched INSERT.
        var rows = DefaultInventorySeed.DefaultCategories
            .Select(cat => new CategoryRow(
                Id: Guid.NewGuid(),
                CompanyId: holdingId,
                Code: cat.Code,
                Name: cat.Name,
                Description: cat.Description,
                ParentId: null,
                IsActive: true))
            .ToArray();

        const string sql = @"
            INSERT INTO item_categories
                (id, company_id, code, name, description, parent_id, is_active, created_at, updated_at)
            SELECT u.id, u.company_id, u.code, u.name, u.description, u.parent_id, u.is_active, now(), now()
            FROM unnest(
                @Ids::uuid[], @CompanyIds::uuid[], @Codes::text[], @Names::text[],
                @Descriptions::text[], @ParentIds::uuid[], @Actives::bool[]
            ) AS u(id, company_id, code, name, description, parent_id, is_active)
            ON CONFLICT (id) DO NOTHING;";

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var inserted = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Ids = rows.Select(r => r.Id).ToArray(),
            CompanyIds = rows.Select(r => r.CompanyId).ToArray(),
            Codes = rows.Select(r => r.Code).ToArray(),
            Names = rows.Select(r => r.Name).ToArray(),
            Descriptions = rows.Select(r => r.Description).ToArray(),
            ParentIds = rows.Select(r => r.ParentId).ToArray(),
            Actives = rows.Select(r => r.IsActive).ToArray(),
        }, cancellationToken: ct));

        return inserted;
    }

    // ============ Row records (avoid dynamic casting) ============

    private sealed record AccountRow(
        Guid Id,
        Guid CompanyId,
        string Code,
        string Name,
        int Type,
        int NormalBalance,
        Guid? ParentId,
        bool IsPostable,
        bool IsIntercompany);

    private sealed record UomRow(
        Guid Id,
        Guid CompanyId,
        string Code,
        string Name,
        string? Symbol,
        bool IsActive);

    private sealed record CategoryRow(
        Guid Id,
        Guid CompanyId,
        string Code,
        string Name,
        string? Description,
        Guid? ParentId,
        bool IsActive);

    /// <summary>
    /// يحوّل نوع الحساب إلى نوع الرصيد (مدين/دائن) وفق القاعدة المحاسبية القياسية:
    /// الأصول والمصروفات مدينة، الخصوم والإيرادات وحقوق الملكية دائنة.
    /// </summary>
    private static int ResolveNormalBalance(AccountType type) =>
        (type == AccountType.Asset || type == AccountType.Expense)
            ? (int)NormalBalance.Debit
            : (int)NormalBalance.Credit;
}
