# 🤖 AGENTS.md — ERP-SYSTEM (Root)

> **التوثيق الذاتي لـ AI Agents والـ humans معاً.** قبل أي تعديل، اقرأ من الجذر → للمجلد المطلوب.
> محدّث: Phase 2.5 — Reports (يونيو 2026)

---

## 📌 نظرة عامة

نظام ERP متعدد المستأجرين (Multi-tenant Modular Monolith) للمرحلة الأولى (MVP). يتكون من 3 وحدات أعمال (Finance + Projects + Inventory) فوق أساس Identity + Multi-tenancy + Event Store.

| الخاصية | القيمة |
|---------|--------|
| المنهجية | Agile / Scrum + Iterative MVP |
| المدة المتوقعة | 8-10 أسابيع |
| المالك | anas600 (https://github.com/anas600) |
| الترخيص | Private — جميع الحقوق محفوظة |
| الحالة | Phase 0 (Foundation + Identity) |

---

## 🛠️ Tech Stack المعتمد

| الطبقة | التقنية | الإصدار |
|--------|---------|---------|
| Runtime | .NET | 9.0 |
| Language (Backend) | C# | 12+ |
| Database (OLTP) | PostgreSQL | 16 |
| Migrations | FluentMigrator | 5.0 |
| ORM | Dapper | 2.1+ |
| Event Store | MartenDB | 7.34+ |
| Cache/Queue | Redis | 7 |
| Auth | JWT Bearer + BCrypt | — |
| Frontend | Next.js | 14.2 |
| Frontend Language | TypeScript | 5.5+ |
| UI Components | shadcn/ui + Tailwind | — |
| API Docs | Swashbuckle | 6.6+ |
| Testing | xUnit + FluentAssertions | — |
| Container | Docker Compose | 3.9 |
| CI | GitHub Actions | — |

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
- **Styling**: Tailwind utility classes + shadcn/ui components

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
| Phase 2.5+ | Reports tests + AGENTS.md | 📋 قادم (PR #9) |
| Phase 3 | Polish + VPS Deploy | 📋 |

راجع [`docs/PLAN.md`](docs/PLAN.md) للتفاصيل الكاملة.

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
