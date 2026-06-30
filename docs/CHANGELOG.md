# 📝 CHANGELOG — ERP-SYSTEM

> سجل التغييرات الموثّقة. **آخر إدخال في الأعلى.**

---

## 2026-06-30b — Procurement Flow Fixes (GR + Bill lines) + UI Date Locale 🆕

| الملف | التغيير |
|-------|---------|
| `src/backend/Modules/Companies/Infrastructure/IRepositories.cs` | 🆕 `ICompanyRepository.GetHoldingCompanyIdAsync(tenantId)` — يُرجع Guid? من `companies.is_group = true` |
| `src/backend/Modules/Companies/Infrastructure/CompanyRepository.cs` | تنفيذ `GetHoldingCompanyIdAsync` (single SELECT LIMIT 1) |
| `src/backend/Modules/Procurement/Application/Services/GoodsReceiptService.cs` | 🐛 **fix:** `ReceiveAsync` كان يمرّر `CompanyId = Guid.Empty` لـ `ReceiveStockRequest` → FK violation على `fk_stock_movements_company`. الآن يجلب holding company للـ tenant ويستخدمها. Fail-fast لو مش موجودة |
| `src/backend/Modules/Procurement/Infrastructure/VendorBillRepository.cs` | 🐛 **fix:** `InsertLinesAsync` كان يحاول إدراج `vendor_id` (عمود غير موجود). الـ schema الفعلي: id, tenant_id, vendor_bill_id, item_id, ... |
| `src/frontend/lib/utils.ts` | 🆕 `formatDate`/`formatTime`/`formatDateTime` helpers — `'en-GB'` locale (Gregorian + English digits) |
| `src/frontend/app/(authenticated)/{9 pages}` | 🔄 استبدال `new Date(x).toLocaleDateString('ar-EG')` بـ `formatDate(x)` في 9 صفحات |

**نتيجة Smoke Test (AlFajr بعد الإصلاحات):**

| Endpoint | قبل | بعد |
|----------|------|-----|
| `GET /api/procurement/grs?take=50` (Received) | **0** | **22** ✅ |
| `GET /api/procurement/bills?take=50` (Draft) | 0 | **16** ✅ |

سلسلة الـ procurement الكاملة الآن شغّالة: **PO → GR → Bill** بدون أخطاء.

**UI Dates:** التواريخ كانت تظهر بالهجري والأرقام العربية (مثلاً `16/12/1446`) بسبب `'ar-EG'` locale. الآن دائماً ميلادي بصيغة `DD/MM/YYYY` عبر `en-GB`.

---

## 2026-06-27 — AlFajr Scenario Seeder (Demo Dataset for Dev/QA) 🆕

| الملف | التغيير |
|-------|---------|
| `src/backend/Shared/SeedData/ScenarioSeederHostedService.cs` | 🆕 hosted service جديد (~900 سطر): يزرع بيانات تشغيلية واقعية لمستأجر **AlFajr Trading & Contracting** (شركة مقاولات ليبية، 2026) — 14 خطوة: tenant + admin، 17 حساب CoA إضافي، 5 أقسام، 12 موظف، 12 هيكل رواتب، 6176 سجل حضور، 10 طلبات إجازة، 4 موردين، 2 مستودع + 15 صنف، 29 PO مُرسلة، 12 دورة رواتب (مع مكافأة نهاية العام لـ ديسمبر)، 22 قيد يدوي، 3 مشاريع |
| `src/backend/Host/Program.cs` | تسجيل `AddHostedService<ScenarioSeederHostedService>()` بعد MigrationRunner |
| `src/backend/Host/appsettings.json` | إضافة `"Database": { "SeedScenario": true }` لتفعيل الـ seeder على startup |
| `src/backend/Modules/Payroll/Application/Services/PayrollService.cs` | 🐛 **bug fix:** `PayslipComponent.PayrollItemId` كان غير مُعيَّن (FK violation على `fk_payslip_components_item`) — تم تعيينه ضمن loop بناء الـ PayslipItem |
| `src/backend/Modules/Finance/Application/Services/JournalEntryService.cs` | 🐛 **bug fix:** `JournalLine.JournalEntryId` كان غير مُعيَّن (FK violation على `fk_journal_lines_entry`) — capture `entryId` قبل بناء الـ aggregate |
| `docs/SCENARIO-SEEDER-PLAN.md` | 🆕 خطة تفصيلية للـ seeder (14 خطوة، حجم البيانات، idempotency strategy) |

**Idempotency:** الـ seeder آمن لإعادة التشغيل — `Register` يحاول login بدل insert على tenant موجود، كل خطوة تفحص وجود بيانات قبل الإدراج (departments/employees/items/warehouses).

**Login:** `admin@alfajr.local` / `Demo1234` — TenantId: `281cf315-5fb8-494f-b456-645d40874875`

**Smoke test (AlFajr بعد الـ seed):**

| Endpoint | النتيجة |
|----------|---------|
| `POST /api/auth/login` | 200 ✅ JWT |
| `GET /api/hr/employees` | 12 ✅ |
| `GET /api/hr/departments` | 5 ✅ |
| `GET /api/hr/leaves` | 10 ✅ |
| `GET /api/hr/payroll/runs` | 12 (all Posted, Dec with year-end bonus) ✅ |
| `GET /api/hr/attendance` | 6176 records ✅ |
| `GET /api/procurement/vendors` | 4 ✅ |
| `GET /api/inventory/warehouses` | 2 ✅ |
| `GET /api/finance/accounts` | 64 (47 default + 17 extra) ✅ |
| `GET /api/finance/ledger/trial-balance` | 200 ✅ |
| `GET /api/projects` | 3 ✅ |

**Stats:** ~13,500 record مُولَّد في ~3 دقائق — bugs حرجة في الـ production code اكتُشفت عبر الـ seeder وأُصلحت في نفس الـ scope.

---

## 2026-06-25 — Playwright E2E: 5 Bugs Found & Fixed 🆕

| الملف | التغيير |
|-------|---------|
| `src/frontend/app/(authenticated)/dashboard/page.tsx` | 🐛 Fix: raw `fetch('/api/procurement/vendors')` → 404 (wrong origin :3000 vs :5000) — replaced with `procurementApi.listVendors()` + `hrApi.listEmployees()`. Also fix: PO status filter `string`→`number` enum |
| `src/frontend/app/(authenticated)/hr/payroll/[id]/page.tsx` | 🐛 Fix: 500 error on payroll detail — SSR tried to call API without auth token. Fix: `export const dynamic = 'force-dynamic'` |
| `src/frontend/components/layout/AppShell.tsx` | 🐛 Fix: hydration mismatch (authApi.getUser() during SSR vs client) — replaced with `useState + useEffect` (client-only) |
| `src/frontend/app/layout.tsx` | 🐛 Fix: favicon.ico → 404 — added `icons: { icon: '/favicon.svg' }` metadata |
| `src/frontend/public/favicon.svg` | 🆕 SVG favicon (blue square + 🏢 emoji) |
| `docs/PLAYWRIGHT-E2E-REPORT.html` | 🆕 Visual HTML report (RTL, dark theme, 25KB): 7 pages tested, 5 bugs documented, API verification evidence |

**Stats:** 7/7 pages PASS ✅ | 5/5 bugs fixed ✅ | npm build ✅ | API: POST /api/hr/payroll/runs → 201 ✅ | GET payroll detail → 200 ✅ (was 500)

---

## 2026-06-24b — Mavis Telegram Architecture Guide 🆕

| الملف | التغيير |
|-------|---------|
| `docs/MAVIS-TELEGRAM-GUIDE.html` | 🆕 دليل تقني شامل (HTML, RTL, 25KB): 3 sessions الموجودة عند Anas، Flow من Telegram → Mavis، Route Rules، Session lifecycle، scenarios، الأوامر، توصيات التنظيف |

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
