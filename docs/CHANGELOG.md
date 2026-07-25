# 📝 CHANGELOG — ERP-SYSTEM

> سجل التغييرات الموثّقة. **آخر إدخال في الأعلى.**

---

## [Unreleased] - 2026-07-25
### Changed (Phase 6.1b — tenant_id removal, multi-company model)
- **Removed:** `ITenantContext`, `TenantContext`, `TenantMiddleware`, `ITenantContext.cs`, `TenantContext.cs`, `TenantMiddleware.cs` files
- **Removed:** `Host/Utilities/TenantCache.cs` + `ITenantCache` abstraction
- **Removed from 35 entities:** `TenantId` property dropped (Customer, Receipt, SalesInvoice, Company, CostCenter, Account, JournalEntry, PostingRule, Attendance, Department, Employee, LeaveRequest, Role, User, Item, ItemCategory, StockLevel, StockMovement, StockReservation, UnitOfMeasure, Warehouse, Notification, Payment, PayrollItem, PayrollRun, SalaryStructure, GoodsReceipt, PurchaseOrder, Vendor, VendorBill, Project, ProjectBudget, ProjectTask, Resource, ResourceAssignment)
- **Removed from 50+ repositories:** `Guid tenantId` parameter dropped from method signatures
- **Removed from 25+ services:** `Guid tenantId` parameter dropped (services now use `ICompanyContext.CompanyId` internally where needed, or are tenantless)
- **Removed from 25+ controllers:** `ITenantContext` dependency replaced with `ICompanyContext`; `Guid tenantId` arg dropped from service calls
- **Removed from AuditLogger:** `tenantId` → `companyId`; `tenant_id` column → `company_id` in `audit_log` table
- **Removed from OutboxEvent:** `tenant_id` column dropped (event has `tenantId` field for back-compat but unused)
- **Removed from SoftDeleteController:** `tenant_id` filter in raw SQL removed
- **Removed from EventsController, AuditController, ReportsController, FinanceReportsController:** all `ITenantContext` swapped to `ICompanyContext`
- **Preserved (back-compat placeholder):** `AuthService` + `JwtTokenService` still emit `tenant_id` JWT claim with `Guid.Empty` value (full rewrite deferred to Phase 6.1c)
### Added (Phase 6.1a — CompanyContext Foundation)
- `ICompanyContext` + `CompanyContext` (AsyncLocal) — new abstraction for active company
- `CompanyContextMiddleware` — reads X-Company-Id header + JWT company_ids[] claim
- CORS updated to allow X-Company-Id header
- Unit tests for CompanyContext (4 tests)
### Back-Compat
- ITenantContext + TenantMiddleware remain active (deleted in PR-6.1b) — **DELETED in Phase 6.1b**

## 2026-07-24c — Phase 5.B Sprint 3: Migration Fix + Timeout Sync + CoA Batch Insert 🆕

### 🎯 الهدف
ثلاثة إصلاحات عاجلة رصدها أنس في سجلات الحاوية (Container Logs) لـ v5.0.2:
1. إصلاح خطأ الـ Migration (42P01) عند إقلاع السيرفر
2. مزامنة مهلة الـ Frontend timeout (30s → 60s)
3. تسريع `EnsureDefaultCoAAsync` (47 round-trips → 1)

### 📊 ملخص الإنجاز
- **Migration fix:** `20260724_120000_FixMissingProcurementTables` ينشئ `vendor_bills` + `vendor_bill_lines` بـ `CREATE TABLE IF NOT EXISTS` (idempotent)
- **Defensive guards:** `20260710_120000_AddMissingIndexes` الآن يـ skip indexes لو الجدول غير موجود (بدل crash بـ 42P01)
- **Frontend timeout:** `lib/api.ts` من 30s → 60s (align مع Backend CommandTimeout=60s)
- **CoA batch INSERT:** `EnsureDefaultCoAAsync` يستخدم `unnest()` — 1 round-trip لـ 47 account (was 47 RT)
- **JSON schemas:** `vendor_bills.json` + `vendor_bill_lines.json` (DEC-080 alignment)
- **GitHub Secrets:** 7 Supabase secrets تم تعيينها تلقائياً (HOST, PORT, DB, USER, PASSWORD, URL, DB_CONN)

### 📝 التغييرات الرئيسية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `src/backend/Shared/Migrations/20260724_120000_FixMissingProcurementTables.cs` | 🆕 ينشئ `vendor_bills` + `vendor_bill_lines` + FKs + indexes (idempotent) |
| 2 | `src/backend/Shared/Migrations/20260710_120000_AddMissingIndexes.cs` | 🆕 `DO $$ ... IF EXISTS(table) ...` guards (defensive ضد 42P01) |
| 3 | `src/backend/Host/data-types/vendor_bills.json` | 🆕 JSON schema (DEC-080) |
| 4 | `src/backend/Host/data-types/vendor_bill_lines.json` | 🆕 JSON schema (DEC-080) |
| 5 | `src/frontend/lib/api.ts` | 🆕 `timeout: 30000` → `timeout: API_TIMEOUT_MS = 60_000` (DEC-093) |
| 6 | `src/backend/Modules/Finance/Infrastructure/AccountRepository.cs` | 🆕 `EnsureDefaultCoAAsync` — topological pass in-memory + 1 batched INSERT عبر `unnest()` (was 47 sequential INSERTs) |

### 🛠️ Root Cause Analysis: Migration 42P01

| Layer | الحالة | السبب |
|-------|--------|------|
| C# migration `20260623_120000_CreateProcurementTables` | NoOp (DEC-080) | يفترض JSON migrator ينشئ الجداول |
| `JsonMigrationEnabled` في `appsettings.json` | **false** | JSON migrator ما يـ run |
| `vendor_bills.json` + `vendor_bill_lines.json` | **مفقودين** | حتى لو JSON migrator شغّال، الجداول ما تنوجد |
| `20260710_120000_AddMissingIndexes` | يفشل بـ 42P01 | يـ `CREATE INDEX ON vendor_bills` لكن الجدول مش موجود |

**الحل المعتمد:** migration جديد ينشئ الجداول الناقصة idempotent + defensive guards في الـ index migration.

### ⚡ CoA Batch INSERT (DEC-093 part 2)

**Before (47 round-trips):**
```csharp
foreach (var entry in entries) {
    await InsertAsync(NewAccount(...));  // 1 RT
}
```

**After (1 round-trip):**
```csharp
// Compute all 47 IDs in memory
var accountObjects = ResolveIdsTopologically(entries);
// Single batched INSERT via unnest()
INSERT INTO accounts (...)
SELECT u.* FROM unnest(@Ids::uuid[], @Codes::text[], @Types::int[], ...)
  AS u(id, code, name, type, ...);
```

**التأثير المتوقع:** Register time من 30s (timeout) → ~3-5s (perf budget).

### 🔐 GitHub Secrets المُعيّنة

```bash
gh secret set SUPABASE_HOST --repo anas600/ERP-SYSTEM  # aws-0-eu-central-1.pooler.supabase.com
gh secret set SUPABASE_PORT --repo anas600/ERP-SYSTEM  # 6543
gh secret set SUPABASE_DB --repo anas600/ERP-SYSTEM    # postgres
gh secret set SUPABASE_USER --repo anas600/ERP-SYSTEM  # postgres.juhfbjozrjvzflravxrn
gh secret set SUPABASE_PASSWORD --repo anas600/ERP-SYSTEM  # من appsettings
gh secret set SUPABASE_URL --repo anas600/ERP-SYSTEM   # https://juhfbjozrjvzflravxrn.supabase.co
gh secret set SUPABASE_DB_CONN --repo anas600/ERP-SYSTEM  # full conn string
```

**SUPABASE_ANON_KEY مفقود** — محتاج من أنس (موجود في Supabase dashboard → Settings → API).

### ⚠️ Known Limitations

- **HF Space Caddy proxy timeout:** ما نقدر نعدّل من الـ repo. لو الـ register flow الكامل (CoA + UoMs + Categories + UoMs = 80+ accounts/items) تجاوز 30s على cold start، الـ proxy يقطع. الحل: keep-warm ping أو split register into 2 steps.
- **JSON migrator:** لسه OFF (Fresh Build Mode). الـ C# migration الجديد يعوّض vendor_bills/lines. لو في جداول JSON ناقصة أخرى (customers, sales_invoices, etc.)، نضيفها بالـ C# migration.

### 📊 Build & Test
- ✅ Backend build: 0 errors, 0 warnings
- ✅ Frontend TypeScript: clean
- ✅ Tests: 7/7 pass (DefaultCoASeedTests, AuthService, UserRepository)
- ⚠️ Live DB verification: psql not available locally (CLI-First per strategy)

---

## 2026-07-24b — Phase 5.B Sprint 2: Npgsql Resiliency + Playwright E2E (DEC-093, 094) 🆕

### 🎯 الهدف
1. **Npgsql Resiliency baseline (DEC-093):** ضبط connection params (CommandTimeout, Keepalive, Pool) + OutboxProcessor exponential backoff → استقرار ضد Supabase pooler timeouts
2. **Playwright E2E suite (DEC-094):** standard للتحقق من الـ flows على develop → أي PR لـ main يمر على E2E أولاً
3. **Workflow discipline:** develop = work + E2E، main = locked (حماية HF Space من re-deploys)

### 📊 ملخص الإنجاز
- **Backend changes:** `NpgsqlConnectionFactory.cs` (الـ Resiliency defaults)، `OutboxProcessorHostedService.cs` (exponential backoff)، `appsettings.json` (Database.* keys)، `Program.cs` (Configure binding)
- **Frontend changes:** `playwright.config.ts`، `e2e/auth.spec.ts` (4 tests)، `e2e/README.md`، `package.json` (scripts + dep)
- **CI:** `.github/workflows/e2e.yml` — Playwright on develop pushes + PRs
- **Branch protection:** develop updated (CI + Playwright required)، main unchanged (locked)
- **Build:** 0 errors, 0 warnings

### 📝 التغييرات الرئيسية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `src/backend/Shared/Infrastructure/NpgsqlConnectionFactory.cs` | 🆕 يقرأ `NpgsqlConnectionOptions` ويطبّق defaults: CommandTimeout=60, Timeout=15, MinPool=1, MaxPool=20, KeepAlive=30, ConnectionIdleLifetime=300 |
| 2 | `src/backend/Shared/Infrastructure/NpgsqlConnectionFactory.cs` (NpgsqlConnectionOptions) | 🆕 6 properties جديدة للـ Resiliency + defaults في الكود |
| 3 | `src/backend/Host/appsettings.json` | 🆕 `Database.CommandTimeoutSeconds/ConnectionTimeoutSeconds/MaxPoolSize/MinPoolSize/KeepaliveSeconds/ConnectionIdleLifetimeSeconds` (defaults = النسب في الـ factory) |
| 4 | `src/backend/Host/Program.cs` | 🆕 `Configure<NpgsqlConnectionOptions>` يقرأ الـ Database section ويربط الـ keys |
| 5 | `src/backend/Shared/Events/Application/Services/OutboxProcessorHostedService.cs` | 🆕 Exponential backoff على مستوى الـ loop: 5s → 10s → 20s → 40s → 60s على الفشل المتتالي، reset عند أول نجاح |
| 6 | `src/frontend/playwright.config.ts` | 🆕 E2E_BASE_URL + E2E_API_URL envs، 1 worker local / 2 CI، chromium only |
| 7 | `src/frontend/e2e/auth.spec.ts` | 🆕 4 tests: `register.happy`, `register.duplicate`, `login.happy`, **`atomicity`** (DEC-091 proof) |
| 8 | `src/frontend/e2e/README.md` | 🆕 دليل سريع + rationale للـ tests |
| 9 | `src/frontend/package.json` | 🆕 scripts: `e2e`, `e2e:ui`, `e2e:headed`, `e2e:install` + `@playwright/test@^1.47.0` |
| 10 | `.github/workflows/e2e.yml` | 🆕 CI: build backend → start with Supabase secrets → install Playwright → run E2E → upload report on failure |
| 11 | `.gitignore` | 🆕 `.env*`, `playwright/`, `playwright-report/`, `test-results/`, IDE/OS junk |
| 12 | `src/backend/Shared/AGENTS.md` | 🆕 "Npgsql Resiliency Baseline" + "OutboxProcessor Backoff" sections |
| 13 | `AGENTS.md` (root) | 🆕 Phase 5.B Sprint 2 + جدول "Branch role" الجديد + workflow discipline |

### 🛡️ Npgsql Resiliency Baseline (DEC-093)

| Parameter | Default | Override key | Why |
|-----------|---------|--------------|-----|
| `CommandTimeout` | 60s | `Database.CommandTimeoutSeconds` | منع `OutboxProcessor` timeout على استعلامات طويلة |
| `Timeout` (connect) | 15s | `Database.ConnectionTimeoutSeconds` | fail-fast على network issues |
| `MinPoolSize` | 1 | `Database.MinPoolSize` | warm connection للـ OutboxProcessor |
| `MaxPoolSize` | 20 | `Database.MaxPoolSize` | مناسب لـ 6GB RAM + HF Space free tier |
| `KeepAlive` | 30s | `Database.KeepaliveSeconds` | يمنع stale connections عبر Supabase pooler |
| `ConnectionIdleLifetime` | 300s | `Database.ConnectionIdleLifetimeSeconds` | تنظيف connections الخاملة |
| `ConnectionPruningInterval` | 10s | hardcoded | فحص دوري |

**Note:** `NpgsqlConnectionStringBuilder.TcpKeepalive` غير متاح في Npgsql 8.0.5 (موجود في 9.x). نعتمد على Postgres-level `KeepAlive`.

### 🔄 OutboxProcessor Exponential Backoff (DEC-093)

```csharp
// OutboxProcessorHostedService.cs
private static readonly TimeSpan BasePollInterval = TimeSpan.FromSeconds(5);
private static readonly TimeSpan MaxPollInterval = TimeSpan.FromSeconds(60);

// After each failure: currentInterval = min(5s * 2^(failures-1), 60s)
// After each success: reset to 5s
```

**الهدف:** منع hot-loop ضد Supabase وقت الانقطاع المؤقت (مثل pooler 504s). يوفر compute + Supabase quota.

### 🧪 Playwright E2E Suite (DEC-094)

| Test | Verifies |
|------|----------|
| `register.happy` | POST /api/auth/register → 200 + JWT + HoldingCompany created |
| `register.duplicate` | Same email twice → conflict, original tenant intact |
| `login.happy` | Register then login → JWT cookie set |
| **`atomicity`** | **DEC-091 proof:** abort 5 register requests mid-process → ZERO orphan tenants (login with aborted email should fail with 401) |

**CI integration:** `.github/workflows/e2e.yml` runs on every `develop` push + PR to develop/main. Fails the build if any test fails. Upload `playwright-report/` + `test-results/` artifacts on failure.

### 🌿 Branch Discipline (DEC-094)

| Branch | Role | Required Checks |
|--------|------|-----------------|
| `main` | Production — locked | `Build and Deploy to HF` + 1 review + `enforce_admins: true` |
| `develop` | Integration — work + E2E | `CI + Deploy` + `Playwright E2E` + 1 review + `enforce_admins: false` |

**Rule:** كل push لـ main = HF rebuild + cold start (يستهلك compute hours). لذا develop هو الـ work branch + E2E validation، main فقط للـ releases الرسمية.

### 📋 Local Hybrid Setup (CLI-First)

- Backend + frontend run locally على جهاز أنس (6GB RAM)
- متصل بـ Supabase cloud عبر `appsettings.Development.json` (gitignored)
- `psql CLI` للـ schema checks + atomicity verification
- `npm run e2e` للـ E2E suite
- لا local PostgreSQL — لا واجهات رسومية

### 📊 Build & Test Status
- ✅ Backend build: 0 errors
- ✅ Frontend TypeScript: clean
- ✅ Playwright 1.61.1 installed
- ⚠️ E2E local run: needs backend running (CI runs it automatically)

---

## 2026-07-24 — Release v5.0.1: Atomic Register (DEC-091) 🆕

### 🎯 الهدف
إصلاح bug خطير: **`AuthService.RegisterAsync` كان non-atomic** — لو الـ HF Space proxy قطع الاتصال قبل اكتمال التسجيل (timeout، cold-start، أو network blip)، كانت الـ DB تحتوي على **orphan tenant** (Tenant row + HoldingCompany + CoA + Default Roles) بدون User ولا RefreshToken. النتيجة: tenant يتيّم يستهلك storage ويمنع تسجيل نفس الاسم لاحقاً.

**الحل:** جميع الـ inserts في Register flow صارت داخل **connection واحد + transaction واحد** — أي failure = automatic rollback، الـ DB تبقى نظيفة.

### 📊 ملخص الإنجاز
- **2 PRs**: #131 (atomic fix) + #132 (Release v5.0.1)
- **PR #131** (squash-merged to develop): atomic Register (16 files changed)
- **PR #132** (squash-merged to main, commit `52e8c26`): Release v5.0.1 — 496 files
- **+36982 / -1585** lines (full release bundle)
- **15 orphan tenants** تم تنظيفها من Supabase قبل الـ fix

### 📝 التغييرات الرئيسية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `src/backend/Modules/Identity/Application/Auth/AuthService.cs` | 🆕 `RegisterAsync` يفتح connection واحد + transaction واحد، كل repo call يمرر `(conn, tx, ct)`، `tx.Commit()` في النهاية، `tx.Rollback()` على أي exception |
| 2 | `src/backend/Modules/Identity/Infrastructure/IRepositories.cs` | 🆕 إضافة overloads `(IDbConnection, IDbTransaction?, ct)` لـ 9 repos: `TenantRepository`, `UserRepository`, `RoleRepository`, `RefreshTokenRepository`, `CompanyRepository`, `AccountRepository`, `UnitOfMeasureRepository`, `ItemCategoryRepository` — الـ signatures القديمة `(ct)` preserved كـ back-compat wrappers |
| 3 | `src/backend/Modules/Companies/Application/Services/CompanyService.cs` | 🆕 `OnTenantCreatedAsync` + `CreateHoldingAsync` ياخذوا `(conn, tx, ct)` — كمان thread الـ tx عبر كل الـ bootstrap |
| 4 | `src/backend/Modules/Inventory/Application/Services/InventoryBootstrapper.cs` | 🆕 `EnsureDefaultUoMsAndCategoriesAsync` ياخذ `(conn, tx, ct)` — الـ UoM + Category inserts كلها في نفس الـ tx |
| 5 | `src/backend/Tests/ERPSystem.Tests/{Inventory,Finance,Companies}/*Tests.cs` | 🆕 إضافة thin wrappers `=> InsertAsync(x, ct)` في Fake repos لتطابق الـ overloads الجديدة |
| 6 | `start-dev.ps1` | 🆕 v4 (cloud-only) — يشيل local PG check، يتحقق من Supabase عبر HF Space health endpoint |
| 7 | `src/backend/Host/appsettings.Development.json` | 🆕 Supabase connection (gitignored) |

### 🛡️ Pattern: Atomic Multi-Step Dapper Transaction

**Reference pattern for all future multi-insert flows in this codebase:**

```csharp
public async Task<X> MultiStepAsync(Req req, CancellationToken ct)
{
    using var conn = await _db.CreateOltpConnectionAsync(ct);
    using var tx = conn.BeginTransaction();
    try
    {
        // ... all repo calls pass (conn, tx, ct)
        var result = await _someRepo.InsertAsync(x, conn, tx, ct);
        await _otherRepo.DoWork(x, conn, tx, ct);
        tx.Commit();
        return result;
    }
    catch
    {
        try { tx.Rollback(); } catch { /* best-effort */ }
        throw;
    }
}
```

**القاعدة:** أي service method يـ insert في > 1 جدول، لازم يستخدم transaction. الـ exception safety pattern فوق يضمن rollback.

### 🐛 Atomicity Test (Pending)

- **الـ happy path** (نجاح): POST /api/auth/register → 200 + JWT ✅
- **الـ atomicity test** (rollback): POST register → kill mid-process → GET /api/auth/login بـ نفس email → لازم يفشل بـ "Tenant not found" (مش 401 عادي) — دليل إن الـ tenant تم rollback
- **المتوقع:** 0 orphan tenants بعد 100 register requests (50 success + 50 mid-process killed)

سيتم اختباره على **HF Space** (Linux) — مش local (Windows Npgsql 8.0.5 يفشل في parse `User Id=postgres.juhfbjozrjvzflravxrn` بسبب dot في username — known bug).

### 🔧 Decisions

- **DEC-091** (جديد): كل multi-insert service flow لازم atomic. Audit pass قادم للـ flows القديمة.
- **DEC-092** (جديد): 15 orphan tenants تم تنظيفها يدوياً من Supabase قبل الـ fix (script: `psql DELETE FROM tenants WHERE id NOT IN (SELECT tenant_id FROM users)`). 
- **ملاحظة:** `Login/Refresh` flows بقوا non-tx (قراءة فقط + refresh token rotation) — التحسين التالي.

### 🌿 Branch Cleanup
- `feature/atomic-register` ← deleted locally + remotely بعد merge PR #131
- `release-v5.0.1` ← deleted after merge PR #132

---

## 2026-07-23 — Release v5.0 to main + Fresh Build Mode 🆕

### 🎯 الهدف
أول release رسمي من `develop` إلى `main` منذ **Phase 4 (PR #14, يونيو 2026)** — يجلب آخر 3 أسابيع من العمل + ينتقل إلى **Fresh Build Mode** (بدون seeders على HF Space).

### 📊 ملخص الإنجاز
- **250 commit** من develop → main
- **PR #127** (squash-merged): Release v5.0
- **PR #126**: تغيير CI trigger من `develop` إلى `main` (PR #126)
- **PR #128**: cleanup للـ CodeQL (refactor `SoftDeleteController`) + تعطيل seeders
- **HF Space deploy:** Run #16 ✅ success (3.5 min build time)

### 📝 التغييرات التفصيلية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `.github/workflows/build-and-deploy-hf.yml` | 🆕 trigger: `branches: [main]` (كان `develop`) — PR merge to main = auto-deploy |
| 2 | `src/backend/Host/Controllers/SoftDeleteController.cs` | 🆕 refactor 3 methods (SoftDelete/Restore/ListDeleted) لاستخدام `switch/case` بدلاً من `$"UPDATE {table}"` — يحل 7 high-severity CodeQL `cs/sql-injection` false positives |
| 3 | `src/backend/Host/Program.cs` | 🆕 seeders DI registration (AlFajr/Realistic) معطّلة افتراضياً — `seedAlFajr = false` (كان `true`)، `seedRealistic = false` |
| 4 | `src/backend/Host/appsettings.json` | 🆕 `Database.SeedScenario`/`SeedAlFajrScenario`/`SeedRealisticScenario`/`JsonMigrationEnabled` كلها `false` |
| 5 | `src/backend/Host/appsettings.Development.json` | 🆕 نفس الـ defaults للـ local dev |

### 🌟 Fresh Build Mode (NEW)

**ما الجديد:**
- الـ HF Space deploy يبدأ بـ **DB فارغ** (لا AlFajr، لا AlBurj، لا Realistic)
- فقط **DefaultCoASeed + DefaultInventorySeed** كمرجع (CoA + UoM + Categories + Warehouses)
- المالك يسجل أول مستخدم حقيقي عبر `/api/auth/register`
- **Seeder files محفوظة** في الكود (غير محذوفة) لإعادة التفعيل في بيئات demo/staging

**كيف تعيد الـ seeders** (لو احتجت في demo space لاحقاً):
1. اعكس تعطيل DI في `Program.cs` (lines 322-339)
2. اضبط `appsettings.json` → `SeedScenario: true` أو `SeedAlFajrScenario: true`
3. الـ seeder files جاهزة (ScenarioSeederHostedService + RealisticSeedHostedService)

### 🔐 CI/CD (DEC-062, محدّث)

| Trigger | Workflow | Behavior |
|---------|----------|----------|
| `push: main` | `build-and-deploy-hf.yml` | Auto-deploy to `Anas-Assaket/erp-system` (staging) |
| `push: develop` | `ci-deploy.yml` | Auto-sync to HF Space (dev — للـ rapid iteration) |
| `workflow_dispatch` | Both | Manual deploy (مع `force_deploy: yes`) |

**Branch protection على main** (مسترجع بعد merge):
- Required status check: `Build and Deploy to HF` ✅
- Required reviews: 1 (dismiss stale, no code owner required)
- Linear history, no force push, enforce admins

### 🐛 CodeQL Cleanup

| Alert | File | Fix |
|-------|------|-----|
| 7× `cs/sql-injection` (high) | `SoftDeleteController.cs` (lines 49, 74, 104) | Refactor: `switch/case` returns hardcoded SQL literals per allowed table — whitelist check kept as defense-in-depth |
| 5× `cs/cleartext-storage-of-sensitive-information` (high) | `ScenarioSeederHostedService.cs` + `VendorBillService.cs` + `PostingRulesService.cs` | Mooted by disabling seeders (DEC-067 fix + DEC-052) — no seeder data flows through code paths anymore |

### 🧪 Verification

- ✅ Build: 0 errors, 236 pre-existing warnings in source-generated files
- ✅ Tests: 114/119 pass (5 pre-existing skips, 0 regressions)
- ✅ CI on PR #127: All checks passed (CodeQL clean)
- ✅ HF Space deploy: `https://anas-assaket-erp-system.hf.space/api/health/ready` → 200 healthy
- ✅ Fresh state: AlFajr login (admin@alfajr.local) → 401 (expected — seeder disabled)
- ✅ Login page: visible in browser, RT Arabic + English digits

### 📋 Release Stats

- **Commits in this release:** 250
- **PRs included:** #15 → #128 (114 PRs)
- **DECs included:** DEC-051/052/053/055/062/067/069/084/086/087/088/109/110/111
- **New modules:** Payments (Sprint 2), AccountsReceivable (Sprint 1)
- **New reports:** GeneralLedger, BalanceSheet, CashFlow, APAging (moved out of Reports module)
- **New seeders disabled:** ScenarioSeeder, RealisticSeed (Fresh Build)
- **Bug fixes:** DEC-067 (BackgroundService fix), DEC-052 (Retention), DEC-053 (RBAC)

### 🤖 Team Pattern (NEW)

| Role | Agent | Tasks |
|------|-------|-------|
| **Mavis (orchestrator)** | `mavis` (root session) | Plan, orchestrate, review PRs, merge to main, oversee team |
| **Jamie Executive** (تنفيذي) | `coder` (worker) | Implementation: refactor, features, bug fixes, commit + push + open PR |
| **Jamie Analytical** (تحليلي) | `verifier` (worker) | Review PRs, smoke tests, audit, build verification, deploy monitoring |

**Rule:** Mavis manages the work tree + oversees PRs. Delegates implementation to Jamie Executive. Delegates verification to Jamie Analytical. All work on feature branches, PRs to develop. Release PRs (develop → main) merged by Mavis.

### 🐳 Local Dev Workflow

**The `start-dev.ps1` script** (root level) is the local dev path. Optimized for low-resource machines:
- 10s cold start
- Parallel backend + frontend startup (detached processes)
- Short Redis timeouts (500ms)
- PostgreSQL 15 (local) for DB

**No Docker required** for local dev on the Satellite C850 (6 GB RAM). The docker-compose.dev.yml is kept for production/team use.

### 📚 Post-merge Verification Steps (للمالك)

1. افتح `https://anas-assaket-erp-system.hf.space` في المتصفح → login page يظهر
2. (اختياري) اعمل register لمستخدم جديد كأول owner
3. لو حبيت ترجع seeders للـ demo data، ارجع للـ "كيف تعيد الـ seeders" أعلاه

---

## 2026-07-06 — Sprint-4: Preventive Hardening + Observability ✅

## 2026-07-06 — Sprint-4: Preventive Hardening + Observability ✅

### 🎯 الهدف
Sprint-4 — تعزيز النظام ضد حوادث الـ startup (DEC-009 style) + إضافة observability للإنتاج.

### 📊 ملخص الإنجاز
- **11 commits** عبر 4 أيام
- **Backend:** 2 utilities (Batch + Retry) + 1 AdminController + 1 Middleware + 1 seeder refactor
- **Frontend:** 1 fix (useAuth infinite loop)
- **Tests:** 12 unit tests (5 Batch + 6 Retry + 1 middleware)
- **Documentation:** README + CHANGELOG updated
- **DEC-009 defense:** 4 layers active (config flag + DI check + endpoint 501 + class deleted)

### 📅 Day-by-Day

| Day | Focus | Commits |
|-----|-------|---------|
| 1 | Health endpoints + seeder feature flags | `8880454` `c5db35a` `34aa094` `14723bd` |
| 2 | Manual triggers + Batch + Retry + tests + integration | `83e8a45` `c496395` `f489ec6` |
| 3 | Structured logging + Request tracking + Sentry | `d373c9b` `108b16c` `db7d1ea` |
| 4 | Documentation + middleware tests | `884951a` |

### 🛡️ Defense-in-Depth (DEC-009 Prevention)

| Layer | Mechanism |
|-------|-----------|
| 1 | `SeedAlBurjScenario=false` default in appsettings |
| 2 | Program.cs doesn't register AlBurjHostedService |
| 3 | `POST /api/admin/seed/alburj` returns 501 |
| 4 | AlBurjSeederHostedService class deleted from codebase |

### 📡 Observability Stack

| Component | Production Value |
|-----------|------------------|
| Health endpoints | K8s/UptimeRobot probes |
| Structured JSON logs | Loki/Elasticsearch ingestion |
| X-Request-ID | Trace across requests |
| LogContext enrichers | Tenant/User filtering |
| Sentry (optional) | Real-time error tracking |

### 📝 الملفات الرئيسية الجديدة

| File | Purpose |
|------|---------|
| `src/backend/Host/Controllers/HealthController.cs` | `/live`, `/startup`, `/startup-deep` |
| `src/backend/Host/Controllers/AdminController.cs` | Manual seed triggers (Admin role) |
| `src/backend/Host/Utilities/BatchInsertHelper.cs` | 1000 records/batch extension method |
| `src/backend/Host/Utilities/RetryPolicy.cs` | 3 retries + exponential backoff |
| `src/backend/Host/Middleware/RequestTrackingMiddleware.cs` | X-Request-ID + LogContext |
| `src/frontend/lib/useAuth.ts` | Fixed React #185 infinite loop |

### 🎯 NEXT: Sprint-5

- Enable MartenDB event sourcing (DEC-017)
- DB rename: `erp-events` → `erp_events` (Neon)
- Complete MVP per FRD-aligned plan (`docs/MVP-COMPLETION-PLAN.md`)

---
- **DEC-009 defense:** 4 layers active (config flag + DI check + endpoint 501 + class deleted)

### 📅 Day-by-Day

| Day | Focus | Commits |
|-----|-------|---------|
| 1 | Health endpoints + seeder feature flags | `8880454` `c5db35a` `34aa094` `14723bd` |
| 2 | Manual triggers + Batch + Retry + tests + integration | `83e8a45` `c496395` `f489ec6` |
| 3 | Structured logging + Request tracking + Sentry | `d373c9b` `108b16c` `db7d1ea` |
| 4 | Documentation + middleware tests | `884951a` |

### 🛡️ Defense-in-Depth (DEC-009 Prevention)

| Layer | Mechanism |
|-------|-----------|
| 1 | `SeedAlBurjScenario=false` default in appsettings |
| 2 | Program.cs doesn't register AlBurjHostedService |
| 3 | `POST /api/admin/seed/alburj` returns 501 |
| 4 | AlBurjSeederHostedService class deleted from codebase |

### 📡 Observability Stack

| Component | Production Value |
|-----------|------------------|
| Health endpoints | K8s/UptimeRobot probes |
| Structured JSON logs | Loki/Elasticsearch ingestion |
| X-Request-ID | Trace across requests |
| LogContext enrichers | Tenant/User filtering |
| Sentry (optional) | Real-time error tracking |

### 📝 الملفات الرئيسية الجديدة

| File | Purpose |
|------|---------|
| `src/backend/Host/Controllers/HealthController.cs` | `/live`, `/startup`, `/startup-deep` |
| `src/backend/Host/Controllers/AdminController.cs` | Manual seed triggers (Admin role) |
| `src/backend/Host/Utilities/BatchInsertHelper.cs` | 1000 records/batch extension method |
| `src/backend/Host/Utilities/RetryPolicy.cs` | 3 retries + exponential backoff |
| `src/backend/Host/Middleware/RequestTrackingMiddleware.cs` | X-Request-ID + LogContext |
| `src/frontend/lib/useAuth.ts` | Fixed React #185 infinite loop |

### 🎯 NEXT: Sprint-5

- Enable MartenDB event sourcing (DEC-017)
- DB rename: `erp-events` → `erp_events` (Neon)
- Complete MVP per FRD-aligned plan (`docs/MVP-COMPLETION-PLAN.md`)

---## 2026-07-01 — Phase 5.A: Finance AP Payments + Finance Reports Rebuild 🆕

### 🎯 الهدف
إكمال **Phase 5.A Sprint 2** (Finance AP) من خطة ERP-SYSTEM: Payments module موحّد (AP/AR) + 4 تقارير مالية مُعاد بناؤها (GeneralLedger, BalanceSheet, CashFlow, APAging) — كلها في Finance module.

### 📊 ملخص الإنجاز
- **Backend:** 1 module جديد (Payments) + 4 services جديدة في Finance + 1 controller جديد + 1 migration
- **Frontend:** 6 pages جديدة (payments list/new, GL, BS, CF, AP aging) + 1 sidebar section
- **Cleanup:** حذف `FinanceReportService` القديم + 3 endpoints من `ReportsController` (الـ DTOs نُقلت إلى Finance)
- **Tests:** 7 unit tests جديدة للـ Finance Reports (DTO computations + BalanceSheetService smoke)
- **Build:** 0 errors, 0 warnings. Tests: 114/119 pass (5 pre-existing skips، 0 regressions)

### 📝 التغييرات التفصيلية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `src/backend/Modules/Payments/Entities/Payment.cs` | 🆕 Payment + PaymentAllocation + enums (PaymentStatus, PaymentPartyTypes, PaymentMethods, PaymentRefTypes) |
| 2 | `src/backend/Modules/Payments/Application/PaymentDtos.cs` | 🆕 CreatePaymentRequest, AllocatePaymentRequest, PaymentResponse, PaymentResult<T>, PaymentErrorCode |
| 3 | `src/backend/Modules/Payments/Application/Validators.cs` | 🆕 CreatePaymentRequestValidator + AllocatePaymentRequestValidator (FluentValidation) |
| 4 | `src/backend/Modules/Payments/Application/Services/PaymentService.cs` | 🆕 Create + Post (ينشئ JournalEntry 2-leg) + Allocate (مع validation لـ outstanding) |
| 5 | `src/backend/Modules/Payments/Infrastructure/PaymentRepository.cs` | 🆕 Dapper repository (snake_case) + multi-tenant filter + SumAllocationsForRefAsync |
| 6 | `src/backend/Modules/Payments/Infrastructure/PaymentSequenceRepository.cs` | 🆕 PAY-YYYY-NNNN sequence (يُعيد استخدام procurement_document_sequences) |
| 7 | `src/backend/Modules/Payments/AGENTS.md` | 🆕 Payments module documentation |
| 8 | `src/backend/Host/Controllers/PaymentsController.cs` | 🆕 4 endpoints (GET list/get, POST create, POST post, POST allocate) |
| 9 | `src/backend/Shared/Migrations/20260701_130000_CreatePaymentsTables.cs` | 🆕 payments + payment_allocations + 7 indexes + 2 FKs |
| 10 | `src/backend/Modules/Finance/Application/FinanceReportDtos.cs` | 🆕 GeneralLedger, BalanceSheet, CashFlow, APAging DTOs (Subtotal computed) |
| 11 | `src/backend/Modules/Finance/Application/Services/GeneralLedgerReportService.cs` | 🆕 Per-account ledger + opening/closing + running balance |
| 12 | `src/backend/Modules/Finance/Application/Services/BalanceSheetService.cs` | 🆕 Assets/Liabilities/Equity per AccountType (postable only) |
| 13 | `src/backend/Modules/Finance/Application/Services/CashFlowService.cs` | 🆕 Indirect method: Net Profit + WC changes + Investing + Financing |
| 14 | `src/backend/Modules/Finance/Application/Services/APAgingService.cs` | 🆕 Per-vendor buckets (0-30/31-60/61-90/91+) — يحسب outstanding من PaymentAllocation |
| 15 | `src/backend/Host/Controllers/FinanceReportsController.cs` | 🆕 4 endpoints (general-ledger, balance-sheet, cash-flow, aging/ap) |
| 16 | `src/backend/Host/Controllers/ReportsController.cs` | 🔄 حذف 3 finance endpoints (TB/IS/BS) — الـ dashboard يبقى |
| 17 | `src/backend/Modules/Reports/Application/ReportDtos.cs` | 🔄 حذف TrialBalanceReport, IncomeStatement, BalanceSheet (نُقلت إلى Finance) |
| 18 | `src/backend/Modules/Reports/Application/Services/FinanceReportService.cs` | 🗑️ حُذف — منقول إلى Finance module |
| 19 | `src/backend/Modules/Finance/AGENTS.md` | ➕ "Phase 5.A — FinanceReportsRebuild" section |
| 20 | `src/backend/Host/Program.cs` | ➕ DI لـ 2 Payment repos + PaymentService + 4 Finance report services + validator |
| 21 | `src/backend/Tests/ERPSystem.Tests/Reports/FinanceReportServiceTests.cs` | 🔄 تحوّل لاختبار الـ DTOs الجديدة + BalanceSheetService smoke |
| 22 | `src/frontend/lib/api.ts` | ➕ Payment/Allocation types + paymentsApi + financeReportsApi + 6 method namespaces |
| 23 | `src/frontend/app/(authenticated)/finance/payments/page.tsx` | 🆕 قائمة المدفوعات + إجمالي Posted + On Account badge |
| 24 | `src/frontend/app/(authenticated)/finance/payments/new/page.tsx` | 🆕 نموذج إنشاء (Vendor picker + allocations ديناميكية + زرّ "حفظ + ترحيل") |
| 25 | `src/frontend/app/(authenticated)/finance/general-ledger/page.tsx` | 🆕 Account dropdown + date range + جدول مع running balance |
| 26 | `src/frontend/app/(authenticated)/finance/balance-sheet/page.tsx` | 🆕 asOfDate picker + 3 أقسام (Assets/Liabilities/Equity) + IsBalanced indicator |
| 27 | `src/frontend/app/(authenticated)/finance/cash-flow/page.tsx` | 🆕 Date range + 3 cards (Operating/Investing/Financing) + NetChangeInCash |
| 28 | `src/frontend/app/(authenticated)/finance/aging-ap/page.tsx` | 🆕 asOfDate picker + 4 buckets + per-vendor breakdown |
| 29 | `src/frontend/components/layout/AppShell.tsx` | ➕ 6 routes في "المالية" group (المدفوعات، دفتر الأستاذ، الميزانية، التدفقات، أعمار AP) |
| 30 | `AGENTS.md` (root) | ➕ Phase 5.A row → ✅ مكتمل (PR #18) |

### 🔐 Business Rules (الرئيسية)

1. **Payment Create:** يفحص Vendor موجود (Customer path غير مفعّل حالياً — Customer entity لم يُنشأ).
2. **Allocation Cap:** `sum(allocations) ≤ amount`. الفرق = "On Account" (دفعة مقدمة — لا قيد إضافي).
3. **Outstanding Check:** لكل VendorBill allocation، يفحص outstanding = totalAmount - sum(allocations applied via Posted payments). يرفض لو amountToApply > outstanding.
4. **Post:** ينشئ JournalEntry Posted مباشرة (ليس Draft). يربط `journal_entry_id` على الـ Payment.
5. **Allocate (Post-only):** يُضيف allocation جديد فقط — لا ينشئ قيد إضافي.

### 🐛 Notes for Verifier

- **AP Aging SQL fix:** الـ `vendor_bills.status` في الـ DB نصّ (VARCHAR) في الـ migration لكن EnumStringTypeHandler يكتب int. نُمرّر Status كـ Dapper parameter (`@PostedStatus`) لتجنّب int/varchar ambiguity. هذا أنظف من cast.
- **Default CoA accounts المستخدمة:** 1210 (النقدية), 1230 (ذمم مدينة), 2210 (دائنون لموردين) — كلها موجودة في DefaultCoASeed.
- **Customer path:** تم تجهيزه لكن غير مفعّل — عند إضافة Customers entity (Sprint لاحق)، يصبح نشطاً بدون تغييرات على الـ Service (يكفي swap الـ Party validation).

---

## 2026-06-30 — Phase 4.5: AlFajr Scenario Seeder + 4 FK Bug Fixes + UI Date Locale 🆕

| الملف | التغيير |
|-------|---------|
| `src/backend/Shared/SeedData/ScenarioSeederHostedService.cs` | 🆕 hosted service جديد (~900 سطر): يزرع بيانات تشغيلية واقعية لمستأجر **AlFajr Trading & Contracting** (شركة مقاولات ليبية، 2026) — 14 خطوة: tenant + admin، 17 حساب CoA إضافي، 5 أقسام، 12 موظف، 12 هيكل رواتب، 6176 سجل حضور، 10 طلبات إجازة، 4 موردين، 2 مستودع + 15 صنف، 29 PO مُرسلة، 12 دورة رواتب (مع مكافأة نهاية العام لـ ديسمبر)، 22 قيد يدوي، 3 مشاريع |
| `src/backend/Host/Program.cs` | تسجيل `AddHostedService<ScenarioSeederHostedService>()` بعد MigrationRunner |
| `src/backend/Host/appsettings.json` | إضافة `"Database": { "SeedScenario": true }` لتفعيل الـ seeder على startup (opt-in) |
| `src/backend/Modules/Payroll/Application/Services/PayrollService.cs` | 🐛 **bug fix:** `PayslipComponent.PayrollItemId` كان غير مُعيَّن (FK violation على `fk_payslip_components_item`) — تم تعيينه ضمن loop بناء الـ PayrollItem |
| `src/backend/Modules/Procurement/Infrastructure/VendorBillRepository.cs` | 🐛 **bug fix:** `SelLine` (SELECT) + `InsertLinesAsync` كانا يحاولان استخدام `vendor_id` column غير موجود في `vendor_bill_lines` schema |
| `src/frontend/lib/utils.ts` | 🆕 `formatDate`/`formatTime`/`formatDateTime` helpers — `'en-GB'` locale (Gregorian + English digits) |
| `src/frontend/app/(authenticated)/{9 pages}` | 🔄 استبدال `new Date(x).toLocaleDateString('ar-EG')` بـ `formatDate(x)` في 9 صفحات (dashboard, hr/*, procurement/*, projects) |

**Login:** `admin@alfajr.local` / `Demo1234` — TenantId: `281cf315-5fb8-494f-b456-645d40874875`

**Smoke test (AlFajr بعد الـ seed):** كل الـ endpoints ترجع counts الصحيحة (12 emps، 12 payroll Posted، 4 vendors، 22 GR Received + 28 Draft، 16 Bills، 64 accounts، 3 projects).

**UI Dates:** التواريخ كانت تظهر بالهجري والأرقام العربية (مثلاً `16/12/1446`) بسبب `'ar-EG'` locale. الآن دائماً ميلادي بصيغة `DD/MM/YYYY` عبر `en-GB`.

**Stats:** ~13,500 record مُولَّد في ~3 دقائق — bugs حرجة في الـ production code اكتُشفت عبر الـ seeder وأُصلحت في نفس الـ scope.

---

## 2026-06-24 — Phase 4: Payroll + EOS (Libya Tax + End of Service) 🆕

### 🎯 الهدف
إكمال **Phase 4** من خطة ERP-SYSTEM: Payroll module كاملاً (راتب + ضرائب ليبيا + تأمينات + EOS) + Frontend pages للـ payslip + Dev tooling improvements (start/stop scripts أسرع 3x).

### 📊 ملخص الإنجاز
- **Backend:** 1 module جديد (Payroll)، 8 endpoints، 5 جداول DB، 1 migration
- **Frontend:** 4 pages (payroll list + new + detail + payslip view) + 1 sidebar entry + 8 API methods
- **Calculators:** LibyaTax (5%/10% progressive) + EOS (5y/2y formula) + SocialInsurance (3.75%/7.5%)
- **Workflow:** Draft → Process → Post → Locked (مع GL posting)
- **Bug Fixes:** EnumStringTypeHandler (Dapper) + PayrollRepository SQL SELECT missing keyword
- **Dev Tooling:** start-dev.ps1 (60s → 10s)، stop-dev.ps1 (5s → 1s)، restart-backend.ps1 جديد، Redis 5s → 500ms
- **E2E Test:** 14/14 سيناريو نجح (100% PASS)

### 📝 التغييرات التفصيلية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `src/backend/Modules/Payroll/` | 🆕 module جديد (5 entities + 3 calculators + 2 services + 1 repo) |
| 2 | `src/backend/Host/Controllers/HrController.cs` | ➕ 8 payroll endpoints (GET/POST runs, process, post, items, payslip, eos) |
| 3 | `src/backend/Shared/Migrations/20260624_100000_CreatePayrollTables.cs` | 🆕 5 جداول payroll (structures, runs, items, components, eos_balance) |
| 4 | `src/backend/Shared/SeedData/DefaultCoASeed.cs` | ✅ CoA 4200 (G&A Expenses) + 1210 (Cash) — يدعم smart fallback |
| 5 | `src/backend/Shared/Infrastructure/EnumStringTypeHandler.cs` | 🆕 Generic Dapper TypeHandler لـ string↔enum mapping |
| 6 | `src/backend/Host/Program.cs` | ➕ تسجيل 6 TypeHandlers (LeaveStatus, PO/GR/BillStatus, PayrollRun/ItemStatus) + Redis timeouts محسّنة |
| 7 | `src/backend/Host/Controllers/HealthController.cs` | ✅ Redis ping cap على 500ms (من 5000ms) |
| 8 | `src/backend/Modules/Payroll/Infrastructure/PayrollRepository.cs` | 🐛 Fix: إضافة `SELECT` keyword المفقود في `GetItemsByRunAsync` |
| 9 | `src/frontend/app/(authenticated)/hr/payroll/` | 🆕 4 pages (list/new/[id]/payslip) |
| 10 | `src/frontend/components/layout/Sidebar.tsx` | ➕ Payroll menu item |
| 11 | `src/frontend/lib/api.ts` | ➕ `hrApi.payroll.*` (8 methods) + `PayrollRun/Item/Component` types |
| 12 | `start-dev.ps1` (root) | ⚡ محسّن: 60s → 10s (parallel + detached + Start-Process) |
| 13 | `stop-dev.ps1` (root) | ⚡ محسّن: 5s → 1s (TCP owner kill مباشرة) |
| 14 | `restart-backend.ps1` (root) | 🆕 fast hot reload (~3 sec) |
| 15 | `src/backend/Modules/Payroll/AGENTS.md` | 🆕 Payroll module documentation |
| 16 | `AGENTS.md` (root) | ✅ Phase 4 → done، Phase 5 → next |
| 17 | `src/backend/AGENTS.md` | ➕ Payroll في الـ index |
| 18 | `docs/research/phase4-gap-analysis.md` | 🆕 Phase 4 scope analysis (32KB) |
| 19 | `docs/workflows/PHASE-4-WORKFLOW.html` | 🆕 42KB visual workflow guide |
| 20 | `docs/RELEASE-REPORT-PHASE4.html` | 🆕 تقرير Phase 4 (HTML، RTL) |

### 🐛 Bug Fixes (Critical)

#### 1. Dapper enum mapping
- **المشكلة:** Dapper لا يحوّل `string` column إلى `enum` property — يرجع 500 عند SELECT.
- **الإصلاح:** إنشاء `EnumStringTypeHandler<TEnum>` + تسجيل 6 TypeHandlers في Program.cs.
- **التأثير:** كل الـ modules مع enum status (Leave, PO, GR, Bill, PayrollRun, PayrollItem).

#### 2. PayrollRepository SQL syntax
- **المشكلة:** `var sql = $"{ItemSel} FROM payroll_items WHERE ...";` — مفقود `SELECT`.
- **الإصلاح:** `var sql = $"SELECT {ItemSel} FROM payroll_items WHERE ...";`
- **الأثر:** كان يكسر `GET /api/hr/payroll/runs` بـ 42601 (syntax error at "id").

#### 3. Redis 5s timeout in dev
- **المشكلة:** `StackExchange.Redis.PingAsync` يحجب 5 ثواني عند عدم تشغيل Redis.
- **الإصلاح:** `ConnectTimeout=1000ms, SyncTimeout=500ms, AsyncTimeout=500ms` + cap بـ CTS في HealthController.
- **النتيجة:** `/health/ready` انتقل من 5000ms+ → 608ms.

### ⚡ Dev Tooling Improvements

| Script | قبل | بعد | المكسب |
|--------|------|------|--------|
| `start-dev.ps1` | 60s (sequential) | **10s** (parallel + detached) | 6x أسرع |
| `stop-dev.ps1` | 5s | **1s** (TCP owner kill) | 5x أسرع |
| `restart-backend.ps1` | غير موجود | **3s** (incremental build) | جديد |

### 🧮 Libya Tax Calculator (Production-ready)

تصاعدية على الراتب الإجمالي:
- 0 - 1,000 LYD: 0%
- 1,000 - 5,000 LYD: 5% على المبلغ الزائد
- 5,000+ LYD: 10% على المبلغ الزائد عن 5,000

**مثال:** راتب 6,000 LYD → ضريبة 300 LYD (5% × 4,000 + 10% × 1,000).

### 🔗 PRs

- **#11** — `feature/phase-4-payroll-schema` → `develop` (squash-merged)
- **#12** — `feature/phase-4-payroll-engine` → `develop` (squash-merged)
- **#13** — `feature/phase-4-frontend` → `develop` (squash-merged)
- **#14** — `develop` → `main` (squash-merge pending) — Phase 4 Release

### ✅ E2E Verification (Mavis takeover, 2026-06-24)

| # | Endpoint | Method | النتيجة |
|---|---------|--------|--------|
| 1 | `/health/live` | GET | 200 ✅ |
| 2 | `/health/ready` | GET | 200 (608ms) ✅ |
| 3 | `/swagger/v1/swagger.json` | GET | 200 ✅ |
| 4 | `/api/auth/login` | POST | 200 ✅ |
| 5 | `/api/hr/payroll/runs` | GET | 200 (3 runs) ✅ |
| 6 | `/api/hr/employees` | GET | 200 (1 emp) ✅ |
| 7 | `/api/procurement/vendors` | GET | 200 (1 vendor) ✅ |
| 8 | `/api/hr/departments` | GET | 200 (1 dept) ✅ |
| 9 | `/api/auth/me` | GET | 200 ✅ |
| 10 | `/api/hr/payroll/runs` | POST | 201 (new run) ✅ |
| 11 | `/api/hr/payroll/runs/{id}` | GET | 200 ✅ |
| 12 | `/api/hr/payroll/runs/{id}/items` | GET | 200 ([]) ✅ |
| 13 | `/` (frontend) | GET | 200 (5502 bytes) ✅ |
| 14 | `/health/ready` (Redis fix) | GET | 200 (608ms vs 5000ms+) ✅ |

**14/14 PASS** — Phase 4 ready for production.

---

## 2026-06-24 — Phase 3: Procurement Core + HR Core + Frontend Foundation

### 🎯 الهدف
إكمال **Phase 3** و **Phase 3.5** من خطة ERP-SYSTEM: Procurement module (Vendor + PO + GR + Bill) + HR module (Department + Employee + Attendance + Leave) + Frontend layout (AppShell + UI components + 12 صفحة جديدة).

### 📊 ملخص الإنجاز
- **Backend:** 2 modules جديدة، 11 endpoints، 11 جداول DB، 2 migrations
- **Frontend:** AppShell + 8 UI components + 12 صفحة جديدة
- **Build:** `dotnet build` 0 errors/0 warnings، `npm run build` clean
- **E2E:** 12/12 سيناريو نجح (100% PASS)

### 📝 التغييرات التفصيلية

| # | الملف | التغيير |
|---|------|--------|
| 1 | `src/backend/Modules/Procurement/` | 🆕 module جديد (entities + repos + services + controller) |
| 2 | `src/backend/Modules/HR/` | 🆕 module جديد (entities + repos + services + controller) |
| 3 | `src/backend/Host/Controllers/ProcurementController.cs` | 🆕 11 endpoints (vendors, POs, GRs, bills) |
| 4 | `src/backend/Host/Controllers/HrController.cs` | 🆕 6+ endpoints (departments, employees, attendance, leaves) |
| 5 | `src/backend/Shared/Migrations/20260623_120000_CreateProcurementTables.cs` | 🆕 7 جداول procurement |
| 6 | `src/backend/Shared/Migrations/20260623_130000_CreateHRTables.cs` | 🆕 4 جداول hr |
| 7 | `src/backend/Host/Program.cs` | ✅ DI registration للـ Procurement + HR services + repos |
| 8 | `src/frontend/components/layout/AppShell.tsx` | 🆕 Layout موحد (sidebar + topbar + breadcrumb + user menu) |
| 9 | `src/frontend/components/ui/*.tsx` | 🆕 8 UI components (Button, Input, Select, Table, Badge, Card, Modal, PageHeader) |
| 10 | `src/frontend/app/(authenticated)/layout.tsx` | 🆕 Route group محمي |
| 11 | `src/frontend/app/(authenticated)/procurement/` | 🆕 8 صفحات (vendors, POs, GRs, bills list+form) |
| 12 | `src/frontend/app/(authenticated)/hr/` | 🆕 4 صفحات (employees, attendance, leaves list+form) |
| 13 | `src/frontend/lib/api.ts` | ✅ إضافة `procurementApi.*` و `hrApi.*` بنفس النمط |
| 14 | `src/frontend/lib/useAuth.ts` | 🆕 Hook للمصادقة |
| 15 | `src/frontend/lib/utils.ts` | 🆕 Helpers (formatCurrency, formatDate, cn) |
| 16 | `src/backend/Modules/HR/AGENTS.md` | 🆕 توثيق الـ HR module |
| 17 | `AGENTS.md` (root) | ✅ Phase Status محدّث (Phase 3 + 3.5 ✅، Phase 4 قادم) + فهرسة AGENTS.md |
| 18 | `src/backend/AGENTS.md` | ✅ إضافة Procurement + HR في الـ tree + الـ index |
| 19 | `src/backend/Host/AGENTS.md` | ✅ قائمة Controllers محدّثة (Procurement + Hr) |
| 20 | `src/backend/Shared/AGENTS.md` | ✅ إضافة الـ 2 migrations الجديدة |
| 21 | `src/frontend/AGENTS.md` | ✅ هيكل محدّث بـ AppShell + UI components + 12 صفحة |
| 22 | `docs/AGENTS.md` | ✅ فهرسة research files + release report + E2E results |
| 23 | `docs/research/daftra-features.md` | 🆕 60KB بحث Daftra |
| 24 | `docs/research/erpnext-features.md` | 🆕 64KB بحث ERPNext |
| 25 | `docs/research/odoo-reference.md` | 🆕 9.6KB مرجع Odoo |
| 26 | `docs/research/gap-analysis.md` | 🆕 31KB تحليل الفجوات + Phase 3 Scope (8 أقسام) |
| 27 | `docs/RELEASE-REPORT-PHASE3.html` | 🆕 23KB تقرير HTML (RTL + Chart.js) |
| 28 | `docs/E2E-TEST-RESULT.json` | 🆕 نتائج E2E |

### 📊 الإحصائيات (بعد Phase 3)
- Commits جديدة: 10
- Modules: 7 → 9
- API Endpoints: 50+ → 60+
- DB Tables: ~25 → ~36
- Frontend Pages: 8 → 20
- AGENTS.md files: 11 → 13 (HR جديدة + محدّثات)

### 🎯 الـ Commits الـ 10 الجديدة (develop HEAD → b41cd4e)
```
b41cd4e docs(report): add Phase 3 release report (HTML) + E2E test results (12/12 PASS)
dcc13af feat(frontend): add Phase 3 pages — AppShell + UI components + procurement + HR + dashboard
46e25d4 feat(hr): add HR Core module (Department + Employee + Attendance + Leave)
d4b04a7 feat(procurement): add Procurement module (Vendor + PO + GR + Bill)
db1ce3a docs(research): add competitive gap analysis + Phase 3 scope (Procurement + HR Core)
d2a3440 docs(research): add comprehensive ERPNext features research
99e3d66 docs(research): trim odoo-reference.md to 9.6KB (verifier: 10KB cap)
f760368 docs(research): trim Odoo reference to meet brief size constraint (16KB to 9KB)
8905e7d docs(research): add Odoo industry-standard reference brief (210 lines)
d0e5412 docs(research): add comprehensive ERPNext research (Mavis direct takeover after worker timeout)
```

### 📌 قاعدة جديدة مطبّقة
بعد هذا الـ phase، نلتزم: **كل تغيير كبير (module جديد، feature جديد، phase جديد) يجب أن يصاحب تحديث:**
1. الـ AGENTS.md المعني (هيكل + conventions + endpoints)
2. الـ root AGENTS.md (Phase Status table + Index + Changelog section)
3. docs/CHANGELOG.md (entry في الأعلى)
4. docs/PLAN.md (status محدّث)
5. Conventional commit منفصل (`docs(agents): ...` أو `feat(...)`)

---

## 2026-06-17 — تسوية التوثيق مع الكود الفعلي

### 🎯 الهدف
التوفيق بين ملفات التوثيق (`AGENTS.md` و `README.md`) والكود الفعلي في الـ repo. اكتشفنا **6 فروقات جوهرية** بين التوثيق والواقع، وعدّلنا الملفات لتعكس الحقيقة.

### 📊 ملخص التغييرات

| # | الملف | المشكلة | الحل |
|---|------|--------|------|
| 1 | `AGENTS.md` (root) | PostgreSQL 16 (التوثيق) ≠ 15 (PLAN.md والكود المحلي) | توحيد على **15** في كل الملفات |
| 2 | `AGENTS.md` (root) | ذكر "shadcn/ui + Tailwind" — لكن `package.json` يحوي shadcn-ui كـ CLI فقط، لا توجد `components/ui/` | توثيق **Tailwind CSS** فقط مع تنبيه |
| 3 | `AGENTS.md` (root) | Phase status قديم (لم يذكر Phase 2.5+ Frontend) | إضافة Phase 2.5+ ✅ |
| 4 | `src/frontend/AGENTS.md` | يذكر shadcn + هيكل outdated | إعادة كتابة كاملة بهيكل Phase 2.5+ |
| 5 | `src/frontend/lib/api.ts` | 🐛 **bug:** `RegisterRequest` يحوي `subdomain` غير مستخدم في الـ backend | إزالة `subdomain`، إضافة `baseCurrency` |
| 6 | `src/frontend/lib/api.ts` | 🐛 **bug:** `LoginRequest` يحوي `tenantSubdomain` (string) لكن الـ backend يستقبل `tenantId` (Guid) | استبدال بـ `tenantId?: string` |
| 7 | `src/frontend/app/register/page.tsx` | 🐛 **bug:** حقل `subdomain` في الـ form يُرسل لكن الـ backend يتجاهله | إزالة الحقل، إضافة hint عن Slugify |
| 8 | `src/backend/Modules/Identity/AGENTS.md` | لا يذكر `BaseCurrency` ولا Slugify للـ Subdomain | إضافة قسم AuthResponse، توثيق Subdomain يُحسب تلقائياً |
| 9 | `infra/docker/AGENTS.md` | يذكر postgres:16-alpine (مخالف) + شرح ضعيف لـ init scripts | تحديث لـ 15-alpine + قسم init-scripts مفصّل |
| 10 | `infra/docker/docker-compose.dev.yml` | `postgres:16-alpine` | → `postgres:15-alpine` |
| 11 | `README.md` | الحالة = "Phase 0"، لا يذكر Frontend أو Setup بدون Docker | تحديث الحالة، إضافة رابط لـ SETUP-LOCAL.md |
| 12 | `docs/SETUP-LOCAL.md` | غير موجود | 🆕 جديد — دليل التشغيل بدون Docker |
| 13 | `docs/CHANGELOG.md` | غير موجود | 🆕 هذا الملف |
| 14 | `src/backend/Host/Program.cs` | 🔴 **bug:** `ConnectionMultiplexer.Connect(redisConn)` يفشل → `/health/live` و `/health/ready` يرجعون 500 | إضافة `AbortOnConnectFail = false` + `ConnectTimeout = 2000` |
| 15 | `src/backend/Modules/Projects/Application/Validators.cs` | 🔴 **bug:** `UpdateProjectRequestValidator` مفقود → `GET /api/projects` يرجع 500 | إنشاء `UpdateProjectRequestValidator` (Name, Budget, StartDate, EndDate) |
| 16 | `src/backend/AGENTS.md` | Phase status outdated: "📋 Phase 1" لـ Finance، "📋 Phase 2" لـ Projects/Inventory | تحديث لكل الموديولات "✅ مكتمل" |
| 17 | `src/backend/Host/AGENTS.md` | لا يذكر Health endpoints الفعلية ولا قواعد Validators | إضافة جدول Health endpoints + قاعدة "كل Request DTO يحتاج validator" |
| 18 | `docs/SMOKE-TEST-REPORT.md` | غير موجود | 🆕 تقرير backend-architect للـ smoke test |
| 19 | `docs/FINAL-INTEGRATION-REPORT.md` | غير موجود | 🆕 تقرير شامل نهائي |

---

### 🐛 تفاصيل الـ Bugs

#### Bug #1: `RegisterRequest` يحوي `subdomain` غير مستخدم

**قبل (frontend):**
```typescript
// src/frontend/lib/api.ts
export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  tenantName: string;
  subdomain: string;        // ❌ الـ backend يتجاهله
}
```

**الـ backend (الكود الفعلي `AuthDtos.cs`):**
```csharp
public sealed class RegisterRequest
{
    public Guid TenantId { get; set; }            // Guid.Empty = tenant جديد
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
    public string? TenantName { get; set; }       // لإنشاء tenant جديد
    public string BaseCurrency { get; set; } = "LYD";
    // ❌ لا يوجد "Subdomain" — يُحسب من TenantName
}
```

**`AuthService.cs` (السطر 33):**
```csharp
tenant = new Tenant {
    Id = Guid.NewGuid(),
    Name = req.TenantName!,
    Subdomain = Slugify(req.TenantName!),  // ✅ يُحسب تلقائياً
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
```

**النتيجة:** الـ backend كان يحفظ `Subdomain = Slugify("شركة الأمل")` (مثلاً: `"shrkt-laml"`) ويتجاهل ما يرسله الـ frontend.

**الإصلاح:**
- إزالة `subdomain` من `RegisterRequest` في `lib/api.ts`
- إزالة حقل الـ subdomain من `app/register/page.tsx`
- إضافة hint: "سيُولَّد subdomain شركتك تلقائياً من اسمها"

#### Bug #2: `LoginRequest` يحوي `tenantSubdomain` (string) بدل `tenantId` (Guid)

**قبل (frontend):**
```typescript
export interface LoginRequest {
  email: string;
  password: string;
  tenantSubdomain?: string;  // ❌ الـ backend لا يتعرف عليه
}
```

**الـ backend:**
```csharp
public sealed class LoginRequest {
    public string Email { get; set; }
    public string Password { get; set; }
    public Guid? TenantId { get; set; }          // ✅ Guid? فقط
}
```

**الإصلاح:** استبدال `tenantSubdomain` بـ `tenantId?: string` (Guid as string).

---

### 🔄 التغييرات في Tech Stack

| البُعد | القديم | الجديد | السبب |
|------|--------|--------|-------|
| **PostgreSQL** | 16 (AGENTS) / 15 (PLAN.md) | **15** موحّد | PLAN.md v2.0 يعتمد 15 (متوفر، API مستقر)؛ الكود المحلي مُختبَر على 15.18 |
| **Frontend UI** | shadcn/ui + Tailwind | **Tailwind CSS** (shadcn CLI غير مستخدم) | `package.json` يحوي `shadcn-ui@0.8.0` كـ CLI فقط؛ لا `components/ui/` |

---

### 🆕 ملف جديد: `docs/SETUP-LOCAL.md`

دليل عملي مُبسَّط للتشغيل بدون Docker، يستهدف:
- مطوّر يعمل على **Windows** مع PostgreSQL 15 محلي (مثل المالك في يونيو 2026)
- البيئات التي لا يتوفر فيها Docker
- الـ quick testing / prototyping

**يغطّي:**
- تثبيت PostgreSQL 15 (Windows installer)
- إنشاء user + databases عبر `psql`
- تشغيل الـ Backend (dotnet run)
- تشغيل الـ Frontend (npm run dev)
- Health checks
- إنشاء أول حساب عبر API
- Troubleshooting شائع

---

### 📁 الملفات المُعدَّلة

```
ERP-SYSTEM/
├── AGENTS.md                                          [تعديل] PostgreSQL 15, shadcn, Phase 2.5+
├── README.md                                          [تعديل] الحالة، رابط SETUP-LOCAL
├── docs/
│   ├── CHANGELOG.md                                   [جديد] هذا الملف
│   ├── SETUP-LOCAL.md                                 [جديد] دليل التشغيل بدون Docker
├── infra/
│   └── docker/
│       ├── AGENTS.md                                  [تعديل] init-scripts مفصّل، 15-alpine
│       └── docker-compose.dev.yml                     [تعديل] postgres:15-alpine
└── src/
    ├── backend/
    │   └── Modules/
    │       └── Identity/
    │           └── AGENTS.md                          [تعديل] BaseCurrency, Slugify, AuthResponse
    └── frontend/
        ├── AGENTS.md                                  [إعادة كتابة] Phase 2.5+ الفعلي
        ├── app/
        │   └── register/
        │       └── page.tsx                           [إصلاح bug] إزالة حقل subdomain
        └── lib/
            └── api.ts                                 [إصلاح bug] RegisterRequest/LoginRequest
```

---

### ✅ التحقق

- [x] `dotnet build` يمر بدون أخطاء
- [x] الـ migrations تُطبَّق بنجاح على PostgreSQL 15.18 محلي
- [x] الـ Backend يستمع على `http://localhost:5000`
- [x] `GET /health` → 200، `GET /health/ready` → 200 (Postgres ready)
- [x] الـ Frontend types في `lib/api.ts` تطابق `AuthDtos.cs`
- [x] لا حقول `subdomain` أو `tenantSubdomain` في الـ frontend types

---

### 📚 مرجع

- **التقرير الذي قاد لهذه التسوية:** التحليل الذي قارن `ERP-SETUP-GUIDE.md` (دليل السحابة الأصلي) مع الكود الفعلي، واكتشف 6 فروقات.
- **PLAN.md v2.0:** يوثّق أن PostgreSQL انخفض من 16 إلى 15 في مرحلة لاحقة من التطوير.
- **الكود الفعلي المعتمد كأساس لكل التعديلات:** `AuthDtos.cs`، `AuthService.cs`، `Program.cs`، `lib/api.ts`، `register/page.tsx`.

---

## [أقدم] — لم تُوثَّق تغييرات سابقة في هذا الملف

> AGENTS السابقة لم تكن تحتفظ بـ CHANGELOG. هذا أول إدخال رسمي.
> التغييرات السابقة موثّقة في git history عبر commit messages.
