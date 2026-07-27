# 🤝 ABDO-TEAM-ALIGNMENT.md — One-Time Context Sync

> **From:** Mavis (Anas's Local Tech Lead & Orchestrator)
> **To:** Mavis (Abdo's Local Tech Lead & Orchestrator)
> **Date:** 2026-07-27
> **Status:** ONE-TIME alignment document. Read once, internalize, then proceed.
> **Governance:** "3-Tier & Dual-Agent Governance Model" (adopted by Anas + City, 2026-07-27)

---

## 0. Who I am and why this file exists

أنا Mavis، نسخة أنس — Local Tech Lead & Orchestrator على جهازه. أنت Mavis، نسخة عبده — على جهاز عبده.

> **Base Assumption:** أنت شغال على نفس المشروع (ERP-SYSTEM by `anas600`) لكن على فرع مختلف على GitHub:
> - **أنس (أنا):** `feature/phase6-migrate-features` @ `e65c338`
> - **عبده (أنت):** `feature/abdo-team` @ [HEAD on your machine]

هذا الملف يهدف لجعلك تستوعب السياق الكامل للمشروع، معماريته، وآلية العمل المشتركة، بحيث تشتغل على فرعك `feature/abdo-team` بدون أي تضارب مع شغلي على `feature/phase6-migrate-features`. **التواصل بيننا حصرياً عبر التوثيق على الفرع الريموت — لا تواصل مباشر بين الوكلاء.**

---

## 1. ملخص المشروع (Project Snapshot)

| البُعد | القيمة |
|--------|--------|
| الاسم | **ERP-SYSTEM** |
| المالك | `anas600` على GitHub |
| الترخيص | Private |
| المنهجية | Agile / Scrum + Iterative MVP |
| البنية | **Multi-Company Modular Monolith** (Per Constitution Article 3) |
| الهدف | نظام ERP لشركات المجموعة (Holding + Subsidiaries) مع تقارير محاسبية شاملة |
| Tech Stack | .NET 9 (Backend) + Next.js 14 / TypeScript (Frontend) + PostgreSQL 15+ + Dapper + MartenDB + JWT |
| الحالة الراهنة | **Phase 6.2 مكتمل** (20 Accounting Reports + User Management + 1-year seed data + Functional Spec PDF) |
| Deployment | Hugging Face Space (`anas-assaket-erp-system.hf.space`) + Supabase (Postgres cloud) |

**راجع:** `README.md` (root) للتعريف العام، `docs/PLAN.md` لخطة المشروع الكاملة.

---

## 2. الدستور (CONSTITUTION.md) — الخطوط العريضة

> **الدستور هو القانون الأعلى للمشروع.** أي تعديل يجب أن يحترم مواده. لا توجد استثناءات.

### Article 1 — Mission & Scope
- نظام ERP متعدد الشركات. **Multi-Company وليس Multi-Tenant.**
- MVP ثم تطوير تكراري. كل phase يجب أن يكون قابل للنشر.

### Article 3 — Multi-Company Architecture (الأهم — احفظه)
> **Approved:** 2026-07-25 by Anas + City. **Supersedes** all prior multi-tenant assumptions (DEC-019, DEC-091, DEC-105, etc.).

| المبدأ | التفصيل |
|--------|---------|
| **Outer isolation** | **NONE** — لا يوجد عمود `tenant_id` في أي مكان |
| **Inner isolation** | `company_id` على كل جدول أعمال (FK → `companies.id`) |
| **Holding** | أول صف في `companies` بـ `is_group = true`, `parent_company_id = NULL` |
| **Subsidiaries** | صفوف في `companies` بـ `is_group = false`, `parent_company_id = holding.id` |
| **Cross-company data** | مسموح للـ shared lookups (customers, items) عبر `is_shared` flag |
| **Per-company data** | مفصول بـ `company_id` (CoA, journals, vendors, employees, payroll) |
| **Auth** | Register = ينشئ المستخدم تحت الـ Holding الافتراضي. Login = JWT بـ `user_id` + `default_company_id` + `company_ids[]` |
| **No TenantMiddleware** | `CompanyContextMiddleware` يقرأ `X-Company-Id` header + JWT `company_ids[]` claim |
| **Authorization** | `[CompanyAuthorize(companyId)]` بدلاً من `[TenantAuthorize]` |

**ما تم إسقاطه من النموذج القديم:**
- ❌ `tenant_id` column (مُسقط من 35 entity في Phase 6.1b)
- ❌ `Tenant` entity + `tenants` table
- ❌ `ITenantContext` / `TenantContext` / `TenantMiddleware`
- ❌ `[TenantAuthorize]` attribute
- ❌ `OnTenantCreatedAsync` (استُبدل بـ `DefaultHoldingBootstrapHostedService` P6-0b)
- ❌ Multi-tenant login queries (`WHERE tenant_id = @TenantId`)
- ❌ Subdomain-based tenant routing

### Article 4 — Tech Stack Rules
- **Dapper فقط** (لا EF Core)
- **FluentMigrator** للـ schema migrations
- **MartenDB** مُثبّت لكن Outbox pattern هو المستخدم حالياً
- **JWT Bearer + BCrypt** (Access 60min، Refresh 14 يوم، Token rotation، Reuse detection)
- **Tailwind CSS** (لا shadcn — لم يُطبَّق)
- **TypeScript strict mode** مفعّل

### Article 5 — Workflow & Branch Discipline
- **`develop`** = staging. **فقط أنس/سيتي** يقدر يعمل merge لها.
- **`main`** = production. **فقط عبر PRs معتمدة** من `develop`.
- كل الشغل يكون على `feature/*`, `fix/*`, `hotfix/*`, `docs/*`.
- **PRs من feature → develop** فقط (العمال لا يفتحون PRs لـ main).
- **Squash merge** للـ main. squash-friendly على develop.
- **Worker + Owner contract** (Defense in depth): حتى لو عامل أخطأ، CI gating + base branch verification + squash merge + branch protection يحمون الـ main.

### Article 6 — Accounting Integrity
- §6.1: Journal entries متوازنة (D = C) — كل عملية مالية قيد مزدوج
- §6.2: Accounting equation holds (A = L + E - X) — مختبر في seed data
- §6.3: No negative stock

---

## 3. معمارية Phase 6 Multi-Company — ما الذي تغيّر

> هذه أهم نقطة في الـ alignment. لو ما فهمتها صح، ستكتب كود يخالف الدستور.

### 3.1 من Phase 5 إلى Phase 6 — خريطة التغيير

| ما كان (Phase 5) | ما صار (Phase 6) | متى |
|------------------|------------------|-----|
| `tenant_id` على كل جدول | **مُسقط** (0 references) | 6.1b |
| `tenants` table | **مُسقط** | 6.1b |
| `ITenantContext`/`TenantContext`/`TenantMiddleware`/`TenantCache` | **مُسقط** — استُبدل بـ `ICompanyContext` | 6.1b |
| `[TenantAuthorize]` | `[CompanyAuthorize(companyId)]` | 6.1b |
| `OnTenantCreatedAsync` | `DefaultHoldingBootstrapHostedService` (P6-0b) | 6.0b |
| Subdomain tenant routing | `X-Company-Id` HTTP header + JWT `company_ids[]` | 6.1a |
| `Tenant` entity (35 fields) | `Company` entity (Multi-Company) | 6.1a |
| `user_tenants` join table | `user_companies` join table | 6.0 |
| Multi-tenant register flow (create tenant + admin) | Simple register (user under existing Holding) | 6.1c |
| 20 obsolete migration files (with `tenant_id` columns) | Renamed `*.cs.disabled` + `<Compile Remove>` in csproj | P0 in 6.2 |

### 3.2 بنية قاعدة البيانات (41 جدول، no `tenant_id`)

**الـ Holding (الصف الأول):**
- `id = 00000000-0000-0000-0000-000000000001` (ثابت دستورياً)
- `code = '000'`
- `is_group = true`, `parent_company_id = NULL`
- `base_currency = 'LYD'` (افتراضي، قابل للتغيير عبر config)

**FKs المهمة:**
- 5 FKs إلى `public.users` (من `notifications`, `password_reset_tokens`, `refresh_tokens`, `user_companies`, `user_roles`)
- كل جدول أعمال عنده `company_id UUID NOT NULL REFERENCES companies(id)`

**Supabase caveat:** في `information_schema.tables/columns` queries، **يجب** فلترة بـ `table_schema = 'public'`. الـ Supabase cluster فيه schema `auth` خاص بـ Supabase Auth services — لو ما فلترت، بتشوف جداول shadow.

### 3.3 Auth Flow (Phase 6.1c)

```
Register POST /api/auth/register {email, password, fullName}
  → AuthService.RegisterAsync (atomic, single conn + single tx)
  → INSERT INTO users (Holding auto-linked via user_companies with is_default=true)
  → INSERT INTO user_companies (user_id, holding_id, is_default=true)
  → INSERT INTO user_roles (user_id, 'Admin')
  → Generate JWT with company_ids=[holding_id], default_company_id=holding_id
  → Refresh token in HttpOnly cookie
```

**RegisterRequest DTO (الحالي — 6.1c):**
```csharp
public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
```
**لا** `tenantId`، **لا** `subdomain`، **لا** `baseCurrency` — تم حذفهم في 6.1c.

### 3.4 Multi-Company في الـ Frontend

- **`X-Company-Id` header** على كل request من الفرونتند (يرسله `api.ts` تلقائياً)
- **CompanySwitcher** في الـ AppShell (top bar) — dropdown لاختيار الشركة النشطة
- كل صفحة تجلب `company_id` من الـ context وتضيفه في query/header
- Decoupled: الشركة المختارة تتحفظ في localStorage + تٌرسل في كل request

### 3.5 الملفات الحرجة التي يجب أن تعرفها

| الملف | السبب |
|-------|-------|
| `CONSTITUTION.md` | القانون الأعلى — Article 3 = Multi-Company rules |
| `src/backend/Host/Bootstrap/DefaultHoldingBootstrapHostedService.cs` | ينشئ الـ Holding الافتراضي عند startup |
| `src/backend/Host/Controllers/HealthController.cs` | `/api/health/ready` يفحص الـ Holding |
| `src/backend/Host/Controllers/AuthController.cs` | Register/Login/Refresh |
| `src/backend/Shared/MultiTenancy/ICompanyContext.cs` | interface للحصول على `company_id` الحالي |
| `src/backend/Shared/MultiTenancy/CompanyContextMiddleware.cs` | middleware يحقن `ICompanyContext` من header/JWT |
| `src/backend/Host/Controllers/FinanceReportsController.cs` | 11 من 20 report endpoint |
| `src/backend/Host/Controllers/ReportsController.cs` | 9 report endpoint (inventory/projects) |
| `src/backend/Modules/Reports/Application/ReportDtos.cs` | DTOs للـ 20 تقرير |
| `src/backend/Shared/Migrations/_obsolete_backup/*.cs.disabled` | 20 migration قديم مع `tenant_id` — **لا تحذفهم ولا تفك التعليق** |
| `src/frontend/components/layout/AppShell.tsx` | CompanySwitcher + X-Company-Id injection |
| `src/frontend/lib/api.ts` | Axios instance + JWT + X-Company-Id interceptor |
| `docs/PRE-PROD-CHECKLIST.md` | Self-attested compliance checklist |
| `docs/HANDOFF-PHASE6-MIGRATE.md` | تقرير آخر تحليل (للمقارنة) |

---

## 4. الـ 3-Tier Architecture Isolation (اعتمدها فوراً)

> **اعتمد هذه الهيكلية بصرامة.** القاعدة الذهبية: **Tier 1 لا يعرف شيئاً عن Tier 2 و Tier 3**. المشاكل السحابية تُعزل وتُؤجل.

### Tier 1 — Local Dev (البيئة الحالية لك ولنا)

| البُعد | التفصيل |
|--------|---------|
| **الغرض** | بناء، تطوير، اختبار الميزات محلياً |
| **الفروع** | `feature/*`, `fix/*`, `docs/*` (لكل فريق فرع منفصل) |
| **من يدير** | أنت (Mavis/عبده) + أنا (Mavis/أنس) — **بالتوازي، على فروع منفصلة** |
| **التقنيات** | .NET 9 SDK, Node.js 20+, psql, git, dotnet CLI, npm, Playwright, **Supabase CLI** (اختياري) |
| **قاعدة البيانات** | قد تكون **Supabase cloud** (لو المالك ما عنده local PG) أو **local PG** (لو المالك عنده). لا تصرف توكنز على مشاكل الاتصال السحابي. |
| **ما يُمنع** | ❌ قضاء وقت في تشخيص Supabase timeouts أو HF Space cold starts أو pgbouncer rate limits. **اعزلها** واذكرها في تقريرك. |

### Tier 2 — Staging (develop branch)

| البُعد | التفصيل |
|--------|---------|
| **الغرض** | تجميع التغييرات، تشغيل CI + E2E suite، تحضير release |
| **من يدير** | **أنس/سيتي حصرياً** في جلسات DevOps مخصصة |
| **من يدمج فيها** | **أنس/سيتي فقط** عبر PR من `feature/*` → `develop` |
| **أنت وأنا** | نفتح PR من فرعنا، **لا ندمج**، ننتظر أنس/سيتي |

### Tier 3 — Production (main branch)

| البُعد | التفصيل |
|--------|---------|
| **الغرض** | ما يراه المستخدم النهائي |
| **من يدير** | أنس/سيتي حصرياً عبر PR من `develop` → `main` |
| **من يدمج** | **أنس/سيتي فقط** |
| **CI/CD** | `Build and Deploy to HF` (required check) + 1 review |
| **أنت وأنا** | **لا نفتح حتى PR** لهذه الطبقة. أنس/سيتي يفعلون ذلك. |

### قاعدة العزل (الأهم)

> 🚫 **إذا واجهت مشكلة سحابية (Supabase timeout، HF rate limit، pgbouncer transaction-mode، إلخ) من Tier 1:**
> 1. **لا تحاول** حلها من الـ worktree
> 2. **سجّلها** في `docs/KNOWN-ISSUES.md` أو في تقريرك
> 3. **اذكرها** في الـ Hand-off Report
> 4. **أنس/سيتي** سيتعاملون معها في جلسة سحابية مخصصة

> ✅ **ما يمكنك فعله في Tier 1:**
> - بناء + اختبار + unit tests + Playwright على localhost
> - مراجعة docs + constitution
> - كتابة كود
> - commit + push إلى **فرعك فقط** (`feature/abdo-team`)

---

## 5. توزع المسؤوليات (Roles & Responsibilities)

| الدور | الاسم | الصلاحيات | ملاحظات |
|------|-------|-----------|---------|
| Owner (مالك المنتج) | **أنس** | git push لـ develop/main، اتخاذ القرارات النهائية | قرار الـ merge النهائي |
| CTO / Assistant | **سيتي** | تنسيق العمل، مراجعة Hand-off Reports، gatekeeper | لا يعمل كود مباشرة |
| Local Tech Lead (أنس) | **Mavis (أنا)** | `feature/phase6-migrate-features` فقط | يخدم جهاز أنس |
| Local Tech Lead (عبده) | **Mavis (أنت)** | `feature/abdo-team` فقط | تخدم جهاز عبده |
| Workers | **Jamies (Executives + Analytical)** | تنفيذ المهام المفوضة | لا يدفعون لـ develop/main |

### Boundary Rules

- 🚫 **لا تواصل مباشر** بين Mavis (أنس) و Mavis (عبده). كل المزامنة عبر docs على الفرع الريموت.
- 🚫 **لا تواصل** بين الـ Jamies مع بعض. كل Jamie يخدم Mavis واحد فقط.
- ✅ **التواصل من المالك/المساعد** → Mavis (عبر prompt/Channel 5)
- ✅ **التواصل من Mavis** → المالك/المساعد (عبر reports/deliverables)

### ما لا تملك فعله أبداً (Hard Limits)

| الفعل | ممنوع؟ | السبب |
|------|--------|-------|
| Push to `develop` | 🚫 **نعم** | staging — أنس/سيتي فقط |
| Push to `main` | 🚫 **نعم** | production — أنس/سيتي فقط |
| Open PR `feature/abdo-team` → `main` | 🚫 **نعم** | ممنوع حتى فتح PR |
| Open PR `feature/abdo-team` → `develop` | ❌ **خارج النطاق** | أنس/سيتي يفتحون (DEF-091/Worker contract) |
| تعديل `CONSTITUTION.md` | 🚫 **نعم** | أنس/سيتي فقط — تغيير دستوري |
| تعديل `AGENTS.md` الجذر | 🚫 **نعم** | أنس/سيتي فقط — توثيق master |
| تعديل `Program.cs` modules list | 🚫 **نعم** | هيكل معماري — أنس/سيتي فقط |
| إضافة tenant_id references | 🚫 **نعم** | انتهاك دستوري |
| Cross-team direct chat | 🚫 **نعم** | كل المزامنة عبر docs |

### ما تملك فعله

| الفعل | مسموح؟ |
|------|--------|
| Push to `feature/abdo-team` | ✅ |
| تعديل أي ملف في `src/`, `tests/`, `docs/` (ما عدا المحظورات أعلاه) | ✅ |
| إضافة AGENTS.md فرعية للـ modules التي تعمل عليها | ✅ |
| إضافة entries لـ `docs/CHANGELOG.md` | ✅ |
| تشغيل `dotnet build`, `dotnet test`, `npx playwright` محلياً | ✅ |
| تشغيل الـ backend والـ frontend محلياً للاختبار | ✅ |
| كتابة Hand-off Report في `docs/HANDOFF-ABDO-TEAM-*.md` | ✅ |

---

## 6. Doc-Driven Sync Protocol (بروتوكول التزامن عبر التوثيق)

> **كل تواصل بينك وبين أنس/سيتي/فريقك الآخر هو ملف على الفرع الريموت.** لا توجد استثناءات.

### 6.1 عندك تغيير في الكود

**خطوات إلزامية لكل PR/commit:**

1. **Commit message** يتبع Conventional Commits:
   ```
   feat(<scope>): <description>
   fix(<scope>): <description>
   docs(<scope>): <description>
   chore(<scope>): <description>
   refactor(<scope>): <description>
   test(<scope>): <description>
   ```
   أمثلة:
   ```
   feat(reports): add Trial Balance multi-currency support
   fix(auth): handle expired access token correctly
   docs(agents): update Phase 6 architecture
   ```

2. **`docs/CHANGELOG.md`** — أضف entry جديد في الأعلى:
   ```markdown
   ### YYYY-MM-DD — <Type> by abdo
   
   - **Scope:** <module>
   - **Summary:** <1-2 sentences>
   - **Files:** <count> files, +<insertions> / -<deletions>
   - **Details:** <link to AGENTS.md update or PR>
   ```

3. **AGENTS.md المناسب** — حدّثه إذا:
   - أضفت pattern جديد
   - غيّرت بنية module
   - أضفت tech جديد
   - أضفت قرار معماري (DEC-NNN)

4. **Tests** — كل feature جديد يحتاج tests:
   - Unit test (xUnit + FluentAssertions) في `src/backend/Tests/`
   - Playwright test (إذا frontend) في `src/frontend/e2e/`
   - Integration test (إذا backend) في `src/backend/Tests/<Module>/`

### 6.2 عندك تقرير أو سؤال أو قرار

**أنواع التقارير/التسليمات (Pick one):**

| النوع | المسار | متى |
|------|--------|-----|
| Hand-Off Report | `docs/HANDOFF-ABDO-TEAM-<topic>.md` | نهاية جلسة عمل كبيرة |
| Status Update | `docs/STATUS-ABDO-TEAM-<date>.md` | منتصف الجلسة |
| Decision | `docs/decisions/DEC-<NNN>-<title>.md` | قرار معماري جديد |
| Known Issue | `docs/KNOWN-ISSUES.md` (أضف entry) | مشكلة سحابية أو خارجية |
| Spec / RFC | `docs/rfcs/RFC-<NNN>-<title>.md` | ميزة كبيرة قبل البدء |

### 6.3 كيف أنس/سيتي يرون شغلك

- أنس/سيتي يفتحون `git log origin/feature/abdo-team --oneline` بشكل دوري
- يقرؤون `docs/CHANGELOG.md` للحصول على السياق
- يفتحون PR `feature/abdo-team` → `develop` (باسمك، لكنهم يقومون بالـ merge)
- أنت **لا تفتح** PR — هم يفعلون

### 6.4 Conflicts (تضارب الملفات)

> **احتمال التضارب وارد** لأننا نعمل على فروع متقاربة (نفس قاعدة `develop`).

**مناطق عالية الخطورة (high-conflict zones):**
- `src/backend/Host/Program.cs` (DI registrations)
- `src/backend/Host/Controllers/*` (إذا أضفت controller جديد)
- `src/frontend/lib/api.ts` (API contracts)
- `docs/CHANGELOG.md` (كلاهما يكتب فيها)
- `docs/AGENTS.md` (master)
- `src/backend/Host/data-types/*.json` (schema changes)

**مناطق منخفضة الخطورة (low-conflict zones):**
- `src/backend/Modules/<NewModule>/` (module جديد كلياً)
- `src/frontend/app/<newpage>/page.tsx` (page جديدة)
- `src/backend/Tests/<NewModule>/` (tests جديدة)
- `docs/HANDOFF-*.md` (خاصة بك)

**بروتوكول حل التضارب:**
1. إذا حصل conflict في `develop` بعد PR — أنس/سيتي يحلونه
2. إذا حصل conflict محلياً — لا تحاول إعادة rebase لوحدك؛ ضع علامة في تقريرك
3. **لا تضغط (force push)** أبداً على `feature/abdo-team`

---

## 7. ما يجب عليك فعله الآن (First Actions)

> **افعلها بالترتيب:**

1. ✅ **اقرأ** هذا الملف كاملاً
2. ✅ **اقرأ** `CONSTITUTION.md` (خاصة Article 3)
3. ✅ **اقرأ** `docs/HANDOFF-PHASE6-MIGRATE.md` (تقريري الأخير) — لترى كيف يبدو Hand-Off Report
4. ✅ **تأكد** أن فرعك المحلي هو `feature/abdo-team`:
   ```bash
   git branch --show-current
   # Expected: feature/abdo-team
   ```
5. ✅ **تأكد** أن آخر commit على فرعك هو نفسه على origin:
   ```bash
   git log -1 origin/feature/abdo-team --oneline
   git status  # should be clean
   ```
6. ✅ **شغّل** التحقق السريع:
   ```bash
   dotnet build src/backend/Host/ERP-SYSTEM.csproj
   dotnet test src/backend/Tests/ERPSystem.Tests/ERPSystem.Tests.csproj --filter "FullyQualifiedName!~E2E"
   ```
7. ✅ **تأكد** أنك تفهم الفروقات بين `tenant_id` و `company_id` (لا تخلطهم)
8. ✅ **تأكد** أنك لن تدفع (push) لـ `develop` أو `main` تحت أي ظرف
9. ✅ **احفظ** نسخة من `docs/PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md` للرجوع إليها
10. ✅ **ابدأ** العمل على فرعك، وتذكر: كل تواصل مع أنس/سيتي = ملف على الفرع الريموت

### ما لا تفعله

- ❌ **لا** تنسخ فرع `feature/phase6-migrate-features` — فرعك منفصل ومستقل
- ❌ **لا** تعدّل `CONSTITUTION.md` — أنس/سيتي فقط
- ❌ **لا** تحاول حل مشاكل Supabase/HF من الـ worktree — عزلها في التقرير
- ❌ **لا** تنشئ مجلد/عمل جديد بدون entry في `docs/CHANGELOG.md`
- ❌ **لا** تدفع على develop/main — أنس/سيتي فقط
- ❌ **لا** تتواصل مع Mavis (أنس) مباشرة — لا chat، لا email، لا message — فقط ملفات على الفرع الريموت

---

## 8. خريطة المراجع (Reference Map)

### الـ Constitution & Planning
- `CONSTITUTION.md` ← **ابدأ هنا دائماً**
- `AGENTS.md` (root) ← فهرس كل AGENTS الفرعية
- `docs/PLAN.md` ← خطة المشروع
- `docs/PHASE6-PLAN.md` ← خطة Phase 6
- `docs/PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md` ← التحليل المعماري العميق

### الـ Architecture & Code
- `src/backend/Host/Bootstrap/DefaultHoldingBootstrapHostedService.cs`
- `src/backend/Host/Controllers/HealthController.cs`
- `src/backend/Host/Controllers/AuthController.cs`
- `src/backend/Shared/MultiTenancy/ICompanyContext.cs` (أصبح ICompanyContext فقط)
- `src/backend/Shared/MultiTenancy/CompanyContextMiddleware.cs`
- `src/backend/Host/Controllers/FinanceReportsController.cs`
- `src/backend/Host/Controllers/ReportsController.cs`
- `src/backend/Modules/Reports/Application/Services/FinanceReportService.cs`

### الـ Frontend
- `src/frontend/lib/api.ts` (X-Company-Id injection)
- `src/frontend/components/layout/AppShell.tsx` (CompanySwitcher)
- `src/frontend/lib/utils.ts` (formatCurrency, formatPercent)

### الـ Docs (التي قرأتها وستقرأها)
- `docs/HANDOFF-PHASE6-MIGRATE.md` ← تقريري (للمقارنة)
- `docs/PRE-PROD-CHECKLIST.md` ← checklist الامتثال
- `docs/USER-GUIDE-AR.md` ← دليل المستخدم بالعربي
- `docs/ADMIN-GUIDE.md` ← دليل المدير
- `docs/SYSTEM-FUNCTIONAL-SPECIFICATION.md` (و PDF) ← المواصفات الوظيفية الكاملة
- `docs/CHANGELOG.md` ← سجل التغييرات

### الـ Migrations
- `src/backend/Shared/Migrations/_obsolete_backup/*.cs.disabled` ← 20 migration قديم (لا تحذفهم)
- `src/backend/Host/data-types/*.json` ← DataType JSON schemas (تستخدمها DataTypeMigrator)

---

## 9. مثال على Hand-Off Report (لتقتدي به)

> **كل جلسة عمل كبيرة يجب أن تنتج Hand-Off Report.** هذا مثال بنيته (ليس للقالب الكامل — انظر `docs/HANDOFF-PHASE6-MIGRATE.md` للحصول على نموذج مفصل):

```markdown
# Hand-Off Report — <topic>
> From: Mavis (Abdo's session)
> To: Anas + City
> Date: YYYY-MM-DD
> Branch: feature/abdo-team @ <commit>
> Worktree: <path>
> Status: ✅ READY FOR REVIEW | ⚠️ NEEDS ATTENTION | ❌ BLOCKED

## Executive Summary
<3-5 bullets max — what got done, what needs review>

## What changed
<files changed, commits, scope>

## Multi-Company compliance (per Constitution §3)
- tenant_id violations: <count>
- company_id references: <count>
- ICompanyContext usage: ✅/⚠️
- X-Company-Id header: ✅/⚠️

## Local build & run
- dotnet build: <result>
- dotnet test: <passed>/<total>
- Frontend smoke: <result>
- Known issues: <list>

## Decisions taken
<DEC-NNN references or new DECs>

## Branch discipline
- Pushed to: feature/abdo-team (count)
- Merged to: develop/main: 🚫 NONE (correct)

## Open questions / blockers
<list with severity>

## Artifacts
<file paths>
```

---

## 10. Acknowledgment (وقّع عليها)

> **Mavis (عبده) — أكّد لي استلامك وفهمك:**

- [ ] قرأت هذا الملف كاملاً
- [ ] قرأت `CONSTITUTION.md` Article 3
- [ ] فهمت Multi-Company model (company_id, ICompanyContext, X-Company-Id)
- [ ] فهمت 3-Tier Architecture Isolation
- [ ] سألتزم بفارعة `feature/abdo-team` فقط
- [ ] لن أدفع على develop/main
- [ ] سأوثّق كل تغيير في `docs/CHANGELOG.md` و AGENTS المناسب
- [ ] لن أحاول حل مشاكل سحابية من Tier 1
- [ ] سأتواصل فقط عبر docs على الفرع الريموت

**التوقيع:** أضف entry في `docs/CHANGELOG.md`:
```markdown
### YYYY-MM-DD — Mavis (Abdo) acknowledged ABDO-TEAM-ALIGNMENT.md
- Read and internalized all sections
- Confirmed branch: feature/abdo-team @ <commit>
- Starting work under 3-Tier & Dual-Agent Governance Model
```

---

## 11. ملاحظات ختامية

- **هذا الملف يُكتب مرة واحدة** عند بداية كل تعاون بين فريقين. لو تغير الدستور أو الـ 3-Tier model، أنس/سيتي يحدّثونه.
- **لو واجهت تضارب** بين هذا الملف و `CONSTITUTION.md` → **الدستور أعلى.** أخبر أنس/سيتي فوراً.
- **لو واجهت غموض** في أي من هذه القواعد → اكتب سؤال في `docs/QUESTIONS.md` وأضفه إلى تقريرك. **لا تخمن.**
- **احترم ملكية الفروع** — `feature/phase6-migrate-features` ملك أنس، `feature/abdo-team` ملكك. لا تتجاوز.

**بالتوفيق. أراك على الـ reports (docs/CHANGELOG.md).**

— Mavis (Anas), 2026-07-27 04:20 EET
