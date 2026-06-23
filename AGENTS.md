# 🤖 AGENTS.md — ERP-SYSTEM (Root)

> **التوثيق الذاتي لـ AI Agents والـ humans معاً.** قبل أي تعديل، اقرأ من الجذر → للمجلد المطلوب.
> محدّث: Phase 2.5 — Reports + Frontend (يونيو 2026)

---

## 📌 نظرة عامة

نظام ERP متعدد المستأجرين (Multi-tenant Modular Monolith) للمرحلة الأولى (MVP). يتكون من **7 وحدات أعمال** (Identity + Companies + Finance + Projects + Inventory + Reports + Notifications) فوق أساس Multi-tenancy + Event Store + Outbox.

| الخاصية | القيمة |
|---------|--------|
| المنهجية | Agile / Scrum + Iterative MVP |
| المدة المتوقعة | 8-10 أسابيع |
| المالك | anas600 (https://github.com/anas600) |
| الترخيص | Private — جميع الحقوق محفوظة |
| الحالة | Phase 2.5+ مكتمل (PR #1 → #8)، Phase 3 قادم |

---

## 🛠️ Tech Stack المعتمد

| الطبقة | التقنية | الإصدار | ملاحظات |
|--------|---------|---------|---------|
| Runtime | .NET | 9.0 | `net9.0` target (يعمل على SDK 9.x و 10.x) |
| Language (Backend) | C# | 12+ | Nullable Reference Types مفعّلة |
| Database (OLTP) | **PostgreSQL** | **15** | ✅ مُختبَر محلياً (15.18). 16+ مقبول |
| Database (Events) | PostgreSQL | 15 | نفس الـ instance، schema منفصل `mt_events` |
| Migrations | FluentMigrator | 5.0 | 7 migrations: identity → finance → projects → inventory → outbox |
| ORM | Dapper | 2.1+ | لا EF Core (القرار في PLAN.md) |
| Event Store | MartenDB | 7.34+ | حزمة مُثبّتة (Phase 3+)؛ حالياً Outbox pattern في Postgres |
| Cache/Queue | Redis | 7 | **اختياري** في dev؛ الكود يتفحص `ConnectionStrings:Redis` |
| Auth | JWT Bearer + BCrypt | — | Access 60min، Refresh 14 يوم، Token rotation، Reuse detection |
| Frontend | Next.js | 14.2 | App Router، RTL |
| Frontend Language | TypeScript | 5.5+ | Strict mode |
| UI Components | **Tailwind CSS** | 3.4 | ⚠️ shadcn/ui مذكور تاريخياً لكن **غير مُطبَّق** (لا يوجد `components/ui/`) |
| API Docs | Swashbuckle | 6.6+ | Swagger UI على `/swagger` |
| Testing | xUnit + FluentAssertions | — | 15 اختبار في `src/backend/Tests/` |
| Container | Docker Compose | 3.9 | `infra/docker/docker-compose.dev.yml` |
| CI | GitHub Actions | — | `.github/workflows/ci.yml` |

---

## 📁 Index للـ AGENTS.md الفرعية

قبل ما تعدّل أي ملف، اقرأ AGENTS.md للمجلد اللي بيشمله تعديلك:

| المسار | الوصف |
|--------|-------|
| [`docs/AGENTS.md`](docs/AGENTS.md) | خطة المشروع (PLAN.md) والتوثيق |
| [`src/AGENTS.md`](src/AGENTS.md) | كل الـ source code (backend + frontend) |
| [`src/backend/AGENTS.md`](src/backend/AGENTS.md) | الـ Backend (ASP.NET Core) |
| [`src/backend/Host/AGENTS.md`](src/backend/Host/AGENTS.md) | نقطة الدخول + Controllers + Swagger |
| [`src/backend/Modules/Identity/AGENTS.md`](src/backend/Modules/Identity/AGENTS.md) | Identity Module (Users, Roles, Tenants) |
| [`src/backend/Modules/Finance/AGENTS.md`](src/backend/Modules/Finance/AGENTS.md) | Finance Module (Phase 1) |
| [`src/backend/Modules/Projects/AGENTS.md`](src/backend/Modules/Projects/AGENTS.md) | Projects Module (Phase 2.1) |
| [`src/backend/Modules/Inventory/AGENTS.md`](src/backend/Modules/Inventory/AGENTS.md) | Inventory Module (Phase 2.2-2.3) |
| [`src/backend/Modules/Reports/AGENTS.md`](src/backend/Modules/Reports/AGENTS.md) | Reports Module (Phase 2.5) |
| [`src/backend/Shared/AGENTS.md`](src/backend/Shared/AGENTS.md) | كود مشترك (Tenant, Migrations, Events) |
| [`src/backend/Tests/AGENTS.md`](src/backend/Tests/AGENTS.md) | xUnit test projects |
| [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md) | Next.js frontend |
| [`src/backend/Modules/Procurement/AGENTS.md`](src/backend/Modules/Procurement/AGENTS.md) | Procurement Module (Phase 3) |
| [`src/backend/Modules/HR/AGENTS.md`](src/backend/Modules/HR/AGENTS.md) | HR Core Module (Phase 3.5) |
| [`infra/AGENTS.md`](infra/AGENTS.md) | Docker + CI/CD |
| [`infra/docker/AGENTS.md`](infra/docker/AGENTS.md) | docker-compose + init-scripts |
| [`infra/.github/AGENTS.md`](infra/.github/AGENTS.md) | GitHub Actions workflows |

---

## 📐 معايير الكود (Code Standards)

### C# / Backend

- **Nullable Reference Types** مفعّلة (`<Nullable>enable</Nullable>`) — لا تترك null warnings
- **Async/Await**: كل IO-bound method يكون `async Task<T>`، لا تحجب الـ thread
- **Naming**: PascalCase للأسماء العامة، camelCase للمتغيرات المحلية والـ params
- **Comments**: **بالعربي** — المالك يفهمها أكثر. الـ code identifiers بالإنجليزي
- **DTOs**: في `Application/*/Dtos.cs` أو `*Dtos.cs` بجانب الـ handler
- **Entities**: في `Entities/` folder — كل entity في ملف منفصل
- **Validation**: FluentValidation، لا تتحقق داخل الـ service
- **Errors**: استخدم `Result<T>` patterns أو throw typed exceptions، لا تُرجع null بدون توثيق

### TypeScript / Frontend

- **Strict mode** مفعّل
- **Components**: Functional components فقط، مع hooks
- **Types**: TypeScript types، تجنب `any`
- **Comments**: بالعربي، الـ identifiers بالإنجليزي
- **Styling**: Tailwind CSS utility classes فقط (لا shadcn حتى الآن)
- **Auth client**: `lib/api.ts` يحوي Axios instance + JWT interceptors + localStorage

### SQL / Migrations

- **Migrations**: FluentMigrator، كل migration له version number فريد (timestamp)
- **Naming**: snake_case للجداول والأعمدة (Postgres convention)
- **Indexes**: أنشئ index على كل foreign key + أعمدة البحث الشائعة
- **Foreign Keys**: حدد `OnDelete` بشكل صريح (Cascade أو Restrict)

---

## 🌿 Git Workflow

### Branch Strategy

- `main` — فرع الإنتاج، كل push يخضع لـ PR + review
- `develop` — فرع التطوير النشط (Integration branch) — كل الـ features تندمج فيه أولاً، ثم PR من `develop` → `main`
- `feature/<phase>-<scope>` — لكل feature
- `fix/<issue>` — لإصلاحات بسيطة
- `chore/<task>` — للصيانة (تحديث deps، توثيق)

### Commit Convention

نستخدم Conventional Commits:

```
feat(identity): add refresh token rotation
fix(auth): handle expired access token correctly
docs(agents): implement DOX framework
chore(deps): bump Marten to 7.34
refactor(shared): extract TenantContext
test(auth): add JwtTokenService tests
```

### PR Rules

- عنوان PR واضح + وصف بـ "what" و"why"
- ربط بـ Issue أو Phase tag إن وُجد
- CI يمر قبل المراجعة
- Squash merge للـ `main`، يحتفظ بتاريخ الـ commits في `develop`

---

## 🌍 Multi-tenancy Convention

كل entity في أي module **يجب** أن يحتوي على `TenantId` (Guid). الـ `TenantContext` يُملأ من JWT claim `tenant_id` عبر `TenantMiddleware`. أي استعلام DB يجب أن يفلتر بـ `tenant_id` (القاعدة لاحقة — حالياً Auth module فقط يطبّقها، باقي الـ modules تبدأ مع Phase 1).

---

## 🔐 Secrets & Environment

- **لا تُحفظ** أي secrets في git (`appsettings.Production.json`, `.env`, tokens)
- `appsettings.json` يحوي placeholders فقط
- `appsettings.Development.json` موجود في repo (مع secrets dev فقط)
- الإنتاج: نستخدم environment variables أو Docker secrets

---

## 📅 Phase Status

| Phase | المحتوى | الحالة |
|-------|---------|--------|
| Phase 0 | Foundation + Identity | ✅ مكتمل (PR #1) |
| Phase 1 | Finance Core (CoA, Journal, GL, Rules Engine) | ✅ مكتمل (PR #2) |
| Phase 1.5 | Multi-Company Foundation (Companies, CostCenters) | ✅ مكتمل (PR #3) |
| Phase 2.1 | Projects Module (Project, Task, Resource, Budget) | ✅ مكتمل (PR #4) |
| Phase 2.2-2.3 | Inventory Core + Stock Movements | ✅ مكتمل (PR #5, #6) |
| Phase 2.4 | Event Bus + Integration (Outbox pattern) | ✅ مكتمل (PR #7) |
| Phase 2.5 | Reports + Polish (12 endpoints + 2 events) | ✅ مكتمل (PR #8) |
| **Phase 2.5+** | **Frontend integration (Next.js 8 pages) + Auth + Tailwind UI** | ✅ مكتمل |
| **Phase 3** | **Procurement Core (Vendor + PO + GR + Bill) + AppShell + 8 UI components** | ✅ مكتمل |
| **Phase 3.5** | **HR Core (Department + Employee + Attendance + Leave)** | ✅ مكتمل |
| Phase 4 | HR + Payroll (calculation engine, payslips, EOS) | 📋 قادم |

راجع [`docs/PLAN.md`](docs/PLAN.md) للتفاصيل الكاملة.

---

## 📝 Changelog (آخر التحديثات)

### 2026-06-24 — Phase 3: Procurement + HR + Frontend Foundation

**التغييرات المطبّقة:**

| المنطقة | التغيير |
|---------|---------|
| **Backend (جديد)** | Procurement Module (4 entities + 5 repos + 4 services + 11 endpoints + 7 جداول + 1 migration) + HR Core Module (4 entities + repos + services + controller + 4 جداول + 1 migration) |
| **Frontend (جديد)** | AppShell layout (sidebar + topbar + breadcrumb) + 8 UI components (Button, Input, Select, Table, Badge, Card, Modal, PageHeader) + 12 صفحة (Procurement: vendors/POs/GRs/Bills list+form، HR: employees/attendance/leaves list+form) |
| **API Contracts** | `procurementApi.*` و `hrApi.*` في `lib/api.ts` بنفس النمط (axios + JWT) |
| **Migrations** | `20260623_120000_CreateProcurementTables.cs` + `20260623_130000_CreateHRTables.cs` |
| **AGENTS.md جديدة** | `src/backend/Modules/HR/AGENTS.md` (Procurement كان موجود) — فهرسة كاملة في الـ root |
| **Phase Status** | Phase 3 + Phase 3.5 → ✅ مكتمل، Phase 4 → 📋 قادم |
| **توثيق** | `docs/research/` (Daftra, ERPNext, Odoo, gap-analysis) + `docs/RELEASE-REPORT-PHASE3.html` (23KB) |
| **E2E Test** | 12/12 PASS — 100% — مسجّل في `docs/E2E-TEST-RESULT.json` |

**قاعدة جديدة للـ workflow:** كل المهام الكبيرة (modules جديدة + frontend + research) لا بد من تحديث الـ AGENTS.md files المعنية + إضافة entry في `docs/CHANGELOG.md` + commit منفصل.

### 2026-06-17 — توثيق vs كود: تسوية الحقائق

**التغييرات المطبّقة في AGENTS.md files بناءً على الكود الفعلي:**

| الملف | التغيير |
|------|--------|
| `AGENTS.md` (root) | PostgreSQL 16 → **15**؛ shadcn/ui → Tailwind CSS (مع تنبيه)؛ إضافة Phase 2.5+ |
| `src/frontend/AGENTS.md` | إزالة shadcn من Tech Stack؛ تحديث Auth contracts (إزالة subdomain)؛ إضافة lib/api.ts في الهيكل |
| `src/backend/Modules/Identity/AGENTS.md` | إضافة `BaseCurrency` للـ Register؛ توثيق Slugify (subdomain يُحسب تلقائياً)؛ إضافة `HoldingCompanyId` |
| `infra/docker/AGENTS.md` | إضافة قسم init-scripts (ينشئ DBs من `POSTGRES_MULTIPLE_DATABASES`) |
| `infra/docker/docker-compose.dev.yml` | `postgres:16-alpine` → `postgres:15-alpine` (تطابق AGENTS) |
| `src/frontend/lib/api.ts` | ✅ **إصلاح bug:** إزالة `subdomain` من `RegisterRequest`؛ استبداله بـ `BaseCurrency` |
| `src/frontend/app/register/page.tsx` | ✅ **إصلاح bug:** إزالة حقل subdomain من الـ form (كان يتم تجاهله من قبل الـ backend) |
| `docs/CHANGELOG.md` | جديد — سجل التغييرات |

---

## 🤝 لما تنضم للـ repo (AI Agent جديد)

1. اقرأ هذا الملف (root AGENTS.md) كاملاً
2. ارجع للـ AGENTS.md الخاصة بالمجلد اللي بتشتغل فيه
3. افهم الـ patterns المستخدمة (Dapper + Marten + Multi-tenancy)
4. لا تخترع patterns جديدة — اتبع الموجود
5. اكتب tests لكل feature جديد
6. حدّث AGENTS.md المعني إذا أضفت pattern جديد أو غيّرت بنية

---

**حافظ على هذا الملف محدّثاً** عند إضافة AGENTS.md جديدة أو tech جديد.
