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
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
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
///   <item><c>Bootstrap:CreateDefaultAdmin</c> — لو true، ينشئ admin user افتراضي (الافتراضي: false).</item>
///   <item><c>Bootstrap:DefaultAdminEmail</c> — ايميل الـ admin (الافتراضي: "admin@erp.local").</item>
///   <item><c>Bootstrap:DefaultAdminPassword</c> — كلمة مرور الـ admin (مطلوبة لو CreateDefaultAdmin=true).</item>
///   <item><c>Bootstrap:DefaultAdminFullName</c> — اسم الـ admin الكامل (الافتراضي: "Administrator").</item>
/// </list>
/// </para>
/// <para>
/// <b>Why the env-driven admin user (Sprint 14, per Anas 2026-07-31 directive)</b>: the 3-Layer Model
/// requires Layer 2 (Containerized MVP) to be a <i>clean</i> install — no manual seed data.
/// Without a default admin, the client cannot log in. The bootstrap now creates a default
/// admin user + user_companies entry on first run when the env flag is set. The same
/// pattern is used by ERPNext (Administrator) and Odoo (admin). Idempotent: skips if a
/// user with the configured email already exists.
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

        // Phase 6.3 hotfix (P6-3, the real fix): افتح connection واحد مباشر
        // (Pooling=false) واستخدمه لكل عمليات الـ bootstrap. السبب: Supabase
        // pgbouncer transaction-mode pool (port 6543) يعيد الـ backend connections
        // بعد كل transaction. لو فتحنا N connections متتالية من client pool،
        // الـ acquire الثاني قد ينتظر 5+ دقائق. اتصال واحد مباشر = acquire
        // واحد فقط، يلبّي كل العمليات على نفس الـ backend connection.
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken);

        try
        {
            // 1) Idempotency check — هل الـ Holding موجود فعلاً؟
            //    الشركة القابضة = code='000' AND is_group=true AND parent_company_id IS NULL.
            var existing = await GetHoldingIdOnConnAsync(conn, cancellationToken);
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

            // 2b) L48 (Sprint 61, DEC-197): Ensure default system roles exist on
            //     a fresh database BEFORE any user tries to register. With
            //     SeedScenario=false and CreateDefaultAdmin=false, no other code
            //     path created the Admin / Accountant / ProjectManager / Viewer
            //     roles, so the first user to register would fail with
            //     "could not find Admin role" inside the register flow. We call
            //     the connection-aware overload to stay in the same transaction
            //     as the Holding Company insert. Idempotent — re-runs are no-ops.
            try
            {
                using var roleScope = _scopeFactory.CreateScope();
                var roleRepo = roleScope.ServiceProvider.GetRequiredService<IRoleRepository>();
                await roleRepo.EnsureDefaultRolesAsync(conn, null, cancellationToken);
                _logger.LogInformation(
                    "[Sprint61-L48] Default roles ensured (Admin, Accountant, ProjectManager, Viewer)");
            }
            catch (Exception roleEx)
            {
                _logger.LogError(roleEx,
                    "[Sprint61-L48] Failed to seed default roles (non-fatal — register will retry)");
            }

            // 3) ابذر دليل الحسابات (47 حساب) للـ Holding عبر raw SQL.
            //    لا نستدعي IAccountRepository.EnsureDefaultCoAAsync لأن الـ INSERT
            //    داخله يكتب إلى tenant_id العمود الذي لم يعد موجوداً في الـ schema
            //    الجديد. نكرّر نمط الـ batched unnest من AccountRepository (DEC-093)
            //    لتفادي 47 round-trip متتالية.
            var coaCount = await SeedDefaultCoAAsync(conn, holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default CoA seeded (count={Count}, holdingId={HoldingId})",
                coaCount, holdingId);

            // 4) ابذر وحدات القياس والتصنيفات بنفس الأسلوب (raw SQL).
            var uomCount = await SeedDefaultUoMsAsync(conn, holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default UoMs seeded (count={Count}, holdingId={HoldingId})",
                uomCount, holdingId);

            var catCount = await SeedDefaultCategoriesAsync(conn, holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default Item Categories seeded (count={Count}, holdingId={HoldingId})",
                catCount, holdingId);

            // 4b) Sprint 30 (DEC-101): Default reference data — 1 warehouse + 1 cost center.
            //     Fixes the empty dropdowns in /procurement/goods-receipts/new and the
            //     "no cost center" errors in /finance/receipts/new. Idempotent: re-runs
            //     are no-ops (ON CONFLICT DO NOTHING).
            var refCount = await TrySeedDefaultReferenceDataAsync(conn, holdingId, cancellationToken);
            _logger.LogInformation(
                "[P6-0b] Default reference data seeded (warehouses={Wh}, cost_centers={Cc}, holdingId={HoldingId})",
                refCount.warehouses, refCount.costCenters, holdingId);

            // 5) Sprint 14: Optionally create a default admin user (env-driven).
            //    Layer 2 (Containerized MVP) needs a login-able user on first run.
            //    By default this is OFF (security: no default credentials in production).
            //    Set Bootstrap:CreateDefaultAdmin=true in the deployment config to enable.
            if (await TrySeedDefaultAdminAsync(conn, holdingId, cancellationToken))
            {
                _logger.LogInformation(
                    "[P6-0b] Default admin user seeded (see logs above for credentials)");
            }

            // 6) Sprint 17: Optionally seed demo data (3 customers, 3 vendors, 5 items).
            //    Makes Layer 2 "client demo ready" — the dashboard shows real data on first run.
            //    By default this is OFF (no demo data in production).
            //    Set Bootstrap:SeedDemoData=true in appsettings.Development.json or mvp-docker/.env
            //    to enable. The local-docker profile has it ON by default (developers want to see data).
            if (await TrySeedDemoDataAsync(conn, holdingId, cancellationToken))
            {
                _logger.LogInformation("[P6-0b] Demo data seeded (customers, vendors, items)");
            }

            // 7) Sprint 21: Seed default posting rules (5 rules: Stock + 4 Libya-default).
            //    الـ rules تحتاج حسابات في الـ CoA (مبذورة في الخطوة 4 أعلاه).
            //    idempotent: لو في rules موجودة، ما يضيف شي جديد.
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var postingRules = scope.ServiceProvider.GetRequiredService<IPostingRulesService>();
                await postingRules.EnsureDefaultRulesAsync(holdingId, cancellationToken);
                _logger.LogInformation("[Sprint21] Default posting rules seeded (Libya default — no tax)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sprint21] Failed to seed default posting rules (non-fatal)");
            }

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

    // ============ Transient Npgsql retry (stale pooled connection) ============

    /// <summary>
    /// يلتفّ حول استعلام الـ idempotency (SELECT أول DB call بعد DataTypeMigrator) مع
    /// إعادة محاولة عند transient Npgsql/timeout errors. السبب الجذري: Supabase
    /// pgbouncer قد يغلق connection كان idle في الـ pool بين الـ migrator والـ
    /// bootstrap (~60-120s)، فأول قراءة على connection قديم تعلّق 60s ثم ترمي
    /// <c>NpgsqlException: Exception while reading from stream</c>. عند إعادة
    /// المحاولة، الـ pool يعطي connection جديد من جديد.
    /// <para>
    /// 3 محاولات كافيتان: الـ pool <c>MaxPoolSize=20</c> فلو الـ attempt الأول أخذ
    /// connection سيئ، الـ attempt الثاني على الأرجح يأخذ غيره. backoff: 2s ثم 4s.
    /// لا نعيد المحاولة على SqlException غير الـ timeout (مثل syntax/permission) لأن
    /// إعادة المحاولة لن تغيّر النتيجة.
    /// </para>
    /// <summary>
    /// الـ idempotency check يعمل على الـ connection المُمرّر (ephemeral) مباشرة
    /// — لا retry، لا scope، لا factory. الـ connection الطازج يضمن إن pgbouncer
    /// لن يعلّق في acquire.
    /// </summary>
    private async Task<Guid?> GetHoldingIdOnConnAsync(System.Data.IDbConnection conn, CancellationToken ct)
    {
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            @"SELECT id FROM companies
              WHERE is_group = true
                AND parent_company_id IS NULL
                AND code = '000'
              LIMIT 1",
            cancellationToken: ct));
    }

    // ============ Internal seed helpers (raw SQL) — all use the single ephemeral conn ============

    /// <summary>
    /// يبذر الـ 47 حساباً من <see cref="DefaultCoASeed.HoldingAccounts"/> في جدول
    /// <c>accounts</c>، كلها مرتبطة بـ Holding عن طريق <c>company_id</c>.
    /// Idempotent: لو وُجد حساب بالـ code 0000 (جذر شجرة CoA) يُعتبر CoA موجوداً
    /// ويُعاد 0.
    /// </summary>
    private async Task<int> SeedDefaultCoAAsync(System.Data.IDbConnection conn, Guid holdingId, CancellationToken ct)
    {
        // Pre-check: لو الـ CoA موجود بالفعل (نتحقّق من الحساب الجذر 0000)، نتخطّى.
        var hasRoot = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM accounts WHERE company_id = @HoldingId AND code = '0000' LIMIT 1",
            new { HoldingId = holdingId }, cancellationToken: ct));
        if (hasRoot > 0) return 0;

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
    private async Task<int> SeedDefaultUoMsAsync(System.Data.IDbConnection conn, Guid holdingId, CancellationToken ct)
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
    private async Task<int> SeedDefaultCategoriesAsync(System.Data.IDbConnection conn, Guid holdingId, CancellationToken ct)
    {
        // Pre-check: لو التصنيف الجذر "RM" موجود، نتخطّى.
        var hasRoot = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM item_categories WHERE company_id = @HoldingId AND code = 'RM' LIMIT 1",
            new { HoldingId = holdingId }, cancellationToken: ct));
        if (hasRoot > 0) return 0;

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

    // ============ Sprint 14: Default admin user (env-driven) ============

    /// <summary>
    /// لو <c>Bootstrap:CreateDefaultAdmin=true</c>، ينشئ admin user (BCrypt-hashed password)
    /// ويربطه بالـ Holding عبر <c>user_companies</c>. Idempotent: لو الـ user موجود بالفعل
    /// (نفس الإيميل)، يتخطى. لو الـ flag=false أو الـ password غير معطى، يفعل nothing.
    /// <para>
    /// الـ workFactor = 12 (يطابق <see cref="AuthService"/>).
    /// </para>
    /// </summary>
    /// <returns>true إذا تم إنشاء admin جديد فعلاً، false إذا تخطّى.</returns>
    private async Task<bool> TrySeedDefaultAdminAsync(
        System.Data.IDbConnection conn, Guid holdingId, CancellationToken ct)
    {
        var create = _config.GetValue<bool>("Bootstrap:CreateDefaultAdmin", false);
        if (!create) return false;

        var email = (_config["Bootstrap:DefaultAdminEmail"] ?? "admin@erp.local").Trim().ToLowerInvariant();
        var password = _config["Bootstrap:DefaultAdminPassword"];
        var fullName = _config["Bootstrap:DefaultAdminFullName"] ?? "Administrator";

        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError(
                "[P6-0b] Bootstrap:CreateDefaultAdmin=true but Bootstrap:DefaultAdminPassword is empty — skipping admin user creation. " +
                "Set the password in your env vars or disable the flag.");
            return false;
        }

        // Ensure the "Admin" role exists — needed whether we create a new user or
        // backfill roles for an existing one.
        var adminRoleId = await EnsureRoleAsync(conn, ERPSystem.Host.Auth.Roles.Admin, ct);

        // Idempotency check: if the user already exists, backfill roles + user_companies
        // if needed. This handles the legacy case where a previous Sprint 14 run created
        // the user without the Admin role (Sprint 14 P0c — 403 on dashboard).
        var existingId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM users WHERE email = @Email LIMIT 1",
            new { Email = email }, cancellationToken: ct));
        if (existingId.HasValue)
        {
            _logger.LogInformation(
                "[P6-0b] Default admin user already exists (email={Email}, id={Id}) — backfilling roles + user_companies",
                email, existingId.Value);

            // Backfill: link to Holding if missing
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO user_companies
                    (user_id, company_id, is_default, assigned_at)
                VALUES
                    (@UserId, @CompanyId, true, now())
                ON CONFLICT (user_id, company_id) DO NOTHING;",
                new
                {
                    UserId = existingId.Value,
                    CompanyId = holdingId,
                }, cancellationToken: ct));

            // Backfill: assign Admin role if missing
            var roleAssigned = await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO user_roles
                    (user_id, role_id, assigned_at)
                VALUES
                    (@UserId, @RoleId, now())
                ON CONFLICT (user_id, role_id) DO NOTHING;",
                new
                {
                    UserId = existingId.Value,
                    RoleId = adminRoleId,
                }, cancellationToken: ct));

            if (roleAssigned > 0)
            {
                _logger.LogInformation(
                    "[P6-0b] Backfilled Admin role for existing user (userId={UserId}, roleId={RoleId})",
                    existingId.Value, adminRoleId);
            }
            return false;
        }

        var userId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var now = DateTime.UtcNow;

        // 1) Insert the user
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO users
                (id, email, password_hash, full_name, is_active,
                 two_factor_enabled, is_deleted, created_at, updated_at)
            VALUES
                (@Id, @Email, @PasswordHash, @FullName, true,
                 false, false, @Now, @Now)",
            new
            {
                Id = userId,
                Email = email,
                PasswordHash = passwordHash,
                FullName = fullName,
                Now = now,
            }, cancellationToken: ct));

        // 2) Link to the Holding (is_default = true so it appears in the user's company list)
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO user_companies
                (user_id, company_id, is_default, assigned_at)
            VALUES
                (@UserId, @CompanyId, true, @Now)
            ON CONFLICT (user_id, company_id) DO NOTHING;",
            new
            {
                UserId = userId,
                CompanyId = holdingId,
                Now = now,
            }, cancellationToken: ct));

        // 3) Assign the Admin role (role already ensured above).
        //    Idempotent: ON CONFLICT (user_id, role_id) DO NOTHING.
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO user_roles
                (user_id, role_id, assigned_at)
            VALUES
                (@UserId, @RoleId, @Now)
            ON CONFLICT (user_id, role_id) DO NOTHING;",
            new
            {
                UserId = userId,
                RoleId = adminRoleId,
                Now = now,
            }, cancellationToken: ct));

        _logger.LogInformation(
            "[P6-0b] Default admin user created (id={Id}, email={Email}, fullName={FullName}, holdingId={HoldingId}, roleId={RoleId}, workFactor=12)",
            userId, email, fullName, holdingId, adminRoleId);
        _logger.LogWarning(
            "[P6-0b] SECURITY: default admin enabled via env var. CHANGE THE PASSWORD after first login in any non-demo deployment.");

        return true;
    }

    // ============ Sprint 17: Demo data seeding (env-driven) ============

    /// <summary>
    /// لو <c>Bootstrap:SeedDemoData=true</c>، ينشئ demo rows (3 customers، 3 vendors، 5 items)
    /// مرتبطة بالـ Holding. الـ dashboard يعرض بيانات حقيقية بدل فاضي. **للتسليم/العرض فقط** — 
    /// default false في الـ production. Idempotent: يتخطّى لو customer واحد على الأقل موجود.
    /// <para>
    /// الترتيب مهم: customers → vendors → items (لأن items ممكن تشير لهم).
    /// </para>
    /// </summary>
    /// <returns>true إذا تم زرع demo data جديد فعلاً، false إذا تخطّى.</returns>
    private async Task<bool> TrySeedDemoDataAsync(
        System.Data.IDbConnection conn, Guid holdingId, CancellationToken ct)
    {
        var seed = _config.GetValue<bool>("Bootstrap:SeedDemoData", false);
        if (!seed) return false;

        // Idempotency check: لو في customer واحد على الأقل، نعتبر الـ seed تم.
        var existingCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT count(*) FROM customers WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: ct));
        if (existingCount > 0)
        {
            _logger.LogInformation(
                "[P6-0b] Demo data already exists (customers={Count}) — skipping seed", existingCount);
            return false;
        }

        var now = DateTime.UtcNow;
        var seedUserId = Guid.Empty; // Demo rows have no real created_by; use the empty UUID as a marker.
        // Actually we need a real user_id for created_by/updated_by (NOT NULL FK).
        // Use the admin user if available, else the empty GUID.
        var firstUser = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM users ORDER BY created_at LIMIT 1",
            cancellationToken: ct));
        var createdBy = firstUser ?? Guid.Empty;

        // 1) Customers (3 — local Libyan companies)
        var customers = new[]
        {
            new { Id = Guid.NewGuid(), Code = "CUST-001", Name = "شركة الفجر للتوزيع", NameEn = "Al-Fajr Distribution Co.", TaxId = "LTD-12345", Email = "sales@alfajr.ly", Phone = "+218 91 234 5678", CreditLimit = 100000m, Terms = 30 },
            new { Id = Guid.NewGuid(), Code = "CUST-002", Name = "مؤسسة النور التجارية", NameEn = "Al-Noor Trading Est.", TaxId = "LTD-67890", Email = "info@alnoor.ly", Phone = "+218 92 345 6789", CreditLimit = 50000m, Terms = 60 },
            new { Id = Guid.NewGuid(), Code = "CUST-003", Name = "مكتب البركة للخدمات", NameEn = "Al-Baraka Services Office", TaxId = "LTD-11111", Email = "contact@albaraka.ly", Phone = "+218 94 456 7890", CreditLimit = 25000m, Terms = 15 },
        };
        foreach (var c in customers)
        {
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO customers
                    (id, company_id, code, name, name_en, tax_id, email, phone,
                     credit_limit, payment_terms_days, is_active,
                     created_at, created_by, updated_at, updated_by)
                VALUES
                    (@Id, @CompanyId, @Code, @Name, @NameEn, @TaxId, @Email, @Phone,
                     @CreditLimit, @Terms, true,
                     @Now, @CreatedBy, @Now, @CreatedBy)
                ON CONFLICT (company_id, code) DO NOTHING;",
                new
                {
                    c.Id, CompanyId = holdingId, c.Code, c.Name, c.NameEn, c.TaxId, c.Email, c.Phone,
                    c.CreditLimit, c.Terms, Now = now, CreatedBy = createdBy
                }, cancellationToken: ct));
        }
        _logger.LogInformation("[P6-0b] Demo customers seeded: {Count}", customers.Length);

        // 2) Vendors (3 — local Libyan suppliers)
        var vendors = new[]
        {
            new { Id = Guid.NewGuid(), Code = "VEND-001", Name = "شركة المورد الذهبي", Email = "orders@golden.ly", Phone = "+218 91 111 2222", TaxNumber = "TAX-90001", Website = "https://golden.ly" },
            new { Id = Guid.NewGuid(), Code = "VEND-002", Name = "مكتب الاستيراد الموحد", Email = "imports@unified.ly", Phone = "+218 92 222 3333", TaxNumber = "TAX-90002", Website = "https://unified.ly" },
            new { Id = Guid.NewGuid(), Code = "VEND-003", Name = "الشركة الليبية للتوريدات", Email = "supply@libyansupply.ly", Phone = "+218 94 333 4444", TaxNumber = "TAX-90003", Website = "https://libyansupply.ly" },
        };
        foreach (var v in vendors)
        {
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO vendors
                    (id, company_id, code, name, email, phone, tax_number, website,
                     currency, payment_terms, is_active,
                     created_at, created_by, updated_at, updated_by)
                VALUES
                    (@Id, @CompanyId, @Code, @Name, @Email, @Phone, @TaxNumber, @Website,
                     'LYD', 'Net30', true,
                     @Now, @CreatedBy, @Now, @CreatedBy)
                ON CONFLICT (company_id, code) DO NOTHING;",
                new
                {
                    v.Id, CompanyId = holdingId, v.Code, v.Name, v.Email, v.Phone, v.TaxNumber, v.Website,
                    Now = now, CreatedBy = createdBy
                }, cancellationToken: ct));
        }
        _logger.LogInformation("[P6-0b] Demo vendors seeded: {Count}", vendors.Length);

        // 3) Items (5 — use existing item_categories + units_of_measure from Sprint 14 P0d seed)
        // We pick the first available category + UoM (they exist from the default seed).
        var firstCategory = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM item_categories WHERE company_id = @HoldingId ORDER BY code LIMIT 1",
            new { HoldingId = holdingId }, cancellationToken: ct));
        var firstUom = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM units_of_measure WHERE company_id = @HoldingId ORDER BY code LIMIT 1",
            new { HoldingId = holdingId }, cancellationToken: ct));

        if (firstCategory.HasValue && firstUom.HasValue)
        {
            var items = new[]
            {
                new { Id = Guid.NewGuid(), Sku = "ITEM-001", Name = "أرز بسمتي 5 كجم", Barcode = "6001234567890", Cost = 25.00m, Price = 35.00m },
                new { Id = Guid.NewGuid(), Sku = "ITEM-002", Name = "زيت زيتون 1 لتر", Barcode = "6001234567891", Cost = 18.50m, Price = 28.00m },
                new { Id = Guid.NewGuid(), Sku = "ITEM-003", Name = "سكر أبيض 2 كجم", Barcode = "6001234567892", Cost = 8.00m, Price = 12.00m },
                new { Id = Guid.NewGuid(), Sku = "ITEM-004", Name = "شاي أحمر 500 جم", Barcode = "6001234567893", Cost = 15.00m, Price = 22.00m },
                new { Id = Guid.NewGuid(), Sku = "ITEM-005", Name = "قهوة تركية 250 جم", Barcode = "6001234567894", Cost = 20.00m, Price = 32.00m },
            };
            foreach (var i in items)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO items
                        (id, company_id, sku, barcode, name, description,
                         category_id, unit_of_measure_id, item_type, costing_method,
                         average_cost, standard_cost,
                         reorder_level, reorder_quantity, is_active,
                         created_at, created_by, updated_at, updated_by)
                    VALUES
                        (@Id, @CompanyId, @Sku, @Barcode, @Name, @Name,
                         @CategoryId, @UomId, 1, 3,
                         @Cost, @Cost,
                         10, 50, true,
                         @Now, @CreatedBy, @Now, @CreatedBy)
                    ON CONFLICT (company_id, sku) DO NOTHING;",
                    new
                    {
                        i.Id, CompanyId = holdingId, i.Sku, i.Barcode, i.Name,
                        Description = i.Name, // description = name (kept simple for demo)
                        CategoryId = firstCategory.Value, UomId = firstUom.Value,
                        i.Cost,
                        Now = now, CreatedBy = createdBy
                    }, cancellationToken: ct));
            }
            _logger.LogInformation("[P6-0b] Demo items seeded: {Count}", items.Length);
        }
        else
        {
            _logger.LogWarning(
                "[P6-0b] Cannot seed demo items: no item_categories or units_of_measure found for holding {HoldingId}",
                holdingId);
        }

        _logger.LogInformation(
            "[P6-0b] Demo data seeded: {Customers} customers, {Vendors} vendors, 5 items (holdingId={HoldingId})",
            customers.Length, vendors.Length, holdingId);
        return true;
    }

    /// <summary>
    /// Idempotently insert a role row and return its id. ON CONFLICT (name) DO NOTHING + RETURNING id.
    /// If the role already exists, returns the existing id.
    /// </summary>
    private async Task<Guid> EnsureRoleAsync(
        System.Data.IDbConnection conn, string roleName, CancellationToken ct)
    {
        var roleId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM roles WHERE name = @Name LIMIT 1",
            new { Name = roleName }, cancellationToken: ct));

        if (roleId.HasValue) return roleId.Value;

        var newId = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO roles
                (id, name, description, created_at)
            VALUES
                (@Id, @Name, '', now())
            ON CONFLICT (name) DO NOTHING;",
            new
            {
                Id = newId,
                Name = roleName,
            }, cancellationToken: ct));

        // Re-fetch (in case another process inserted concurrently)
        roleId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM roles WHERE name = @Name LIMIT 1",
            new { Name = roleName }, cancellationToken: ct));
        return roleId ?? newId;
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

    /// <summary>
    /// Sprint 30 (DEC-101): يبذر الحد الأدنى من reference data المطلوب لتشغيل الـ flow:
    ///   - 1 default warehouse "المستودع الرئيسي" (WH-001) — لإصلاح dropdowns /procurement/goods-receipts/new
    ///   - 1 default cost center "الإدارة العامة" (CC-001, type=Department=2) — لإصلاح "no cost center" errors
    /// <para>
    /// Idempotent عبر ON CONFLICT DO NOTHING على (company_id, code). دائماً يُشغّل
    /// (لا flag) لأن هذه بيانات reference لازمة لكل install.
    /// </para>
    /// </summary>
    private async Task<(int warehouses, int costCenters)> TrySeedDefaultReferenceDataAsync(
        System.Data.IDbConnection conn, Guid holdingId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Default warehouse
        var whRows = await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO warehouses
                (id, company_id, code, name, location, is_active, created_at, created_by, updated_at, updated_by)
            VALUES
                (@Id, @HoldingId, 'WH-001', 'المستودع الرئيسي', 'المقر الرئيسي', true, @Now, @UserId, @Now, @UserId)
            ON CONFLICT (company_id, code) DO NOTHING
            RETURNING id;",
            new
            {
                Id = Guid.NewGuid(),
                HoldingId = holdingId,
                Now = now,
                UserId = Guid.Empty  // System-seeded, no specific user
            },
            cancellationToken: ct));

        // Default cost center (Department type = 2)
        var ccRows = await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO cost_centers
                (id, company_id, code, name, type, is_active, created_at, updated_at)
            VALUES
                (@Id, @HoldingId, 'CC-001', 'الإدارة العامة', 2, true, @Now, @Now)
            ON CONFLICT (company_id, code) DO NOTHING
            RETURNING id;",
            new
            {
                Id = Guid.NewGuid(),
                HoldingId = holdingId,
                Now = now
            },
            cancellationToken: ct));

        return (whRows, ccRows);
    }
}
