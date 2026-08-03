# 🤖 AGENTS.md — ERP-SYSTEM (Root DOX Rail)

> **This is the DOX rail.** All work in this repository must follow the DOX framework.
> Read this file fully + walk the chain to your target path before editing anything.

**Last updated:** 2026-08-03 (Sprint 30: 6 architectural cleanups (DEC-100..106) + full PO+GR+Bill seeder (DEC-105). Sprint 29: Year-Scenario dev seeder (POC #4) + cleanup of 2 legacy seeders (102.8 KB) per DEC-098. Sprint 28: 8 more Article 3 violations in Payroll + Projects + StockMovement + Finance/Account (DEC-094..097) + Procurement seeder POC #3 + L26 IIFE-pattern fix in tests. Sprint 27: HR Article 3 audit (DEC-091) + Arabic HR dev seeder (POC #2). Sprint 26: Arabic dev seeder (DEC-087) — fixes encoding bug from Sprint 25 PowerShell scripts. Sprint 25: 4 Article 3 violations in Procurement cycle + demo data. Sprint 24: outbox cleanup (DEC-082) + Constitution Article 3 audit (DEC-083). Sprint 23: company_id propagation fix + Stock→Posting Rules direct call. Sprint 22: major refactor — 15→9 modules. **Architecture target:** `/docs/architecture/REFACTOR-SPRINT-22.md`)

> ## 📜 ACTIVE GOVERNANCE (Sprint 17+)
>
> **Active governance model:** [CONSTITUTION.md](./CONSTITUTION.md) — `✅ ACTIVE` status (per Sprint 18 amendment). The 2-day pause directive ended 2026-07-31 18:25 UTC.
>
> **Two-Mode Workflow (per Article 10):**
> - **Mode 1 (Development, default):** Local work on `feature/sprint-N-...` branch. NO push, NO CI, NO remote effects. Admin orchestrates Jimis, merges locally.
> - **Mode 2 (Release):** Triggered by Anas's "ادفع". Admin does: git push + gh pr create + relax + merge + tag + restore. CI runs (6/6). Cron rebuilds mvp-docker + sends Telegram ping.
>
> **Branch architecture (per branch architecture reset 2026-07-31):**
> - `develop` = default + active work
> - `main` = LOCKED archive (no merges)
> - Tags: `v0.0.0-pre-branch-reset` (safety) + `vX.Y.Z-sprintN` (work anchors)
>
> **Sprint hand-offs:** [`docs/workflow/sprint-N.md`](./docs/workflow/) — historical record.
>
> **Single source of truth for architecture:** [`/docs/architecture/holding-company-architecture.md`](./docs/architecture/holding-company-architecture.md).
>
> **Worker (Jimi) instructions:** [`.mavis/AGENTS.md`](./.mavis/AGENTS.md) — every Jimi reads this before starting.
>
> **🚨 Critical (Sprint 17):** The ball is in the **USER's** court (Anas), **NOT** the cron's. The cron is a tool that helps Mavis Local stay updated. Only Anas can switch from Mode 1 → Mode 2 (by saying "ادفع").

---

## DOX Framework (Binding Contract)

- **AGENTS.md files are binding work contracts** for their subtrees.
- **Work products, source materials, instructions, records, assets, and durable docs** must stay understandable from the nearest applicable AGENTS.md plus every parent AGENTS.md above it.
- **If a child doc conflicts with a parent**, the closer doc controls local work details, but no child doc may weaken DOX.

### Read Before Editing
1. Read this root AGENTS.md.
2. Identify every file or folder you expect to touch.
3. Walk from the repository root to each target path.
4. Read every AGENTS.md found along each route.
5. If a parent AGENTS.md lists a child AGENTS.md whose scope contains the path, read that child and continue from there.
6. Use the nearest AGENTS.md as the local contract and parent docs for repo-wide rules.

**Do not rely on memory. Re-read the applicable DOX chain in the current session before editing.**

### Update After Editing
Every meaningful change requires a DOX pass before the task is done.
- Update the **closest owning AGENTS.md** when a change affects purpose, scope, ownership, contracts, workflows, or rules.
- Update **parent docs** when parent-level structure, ownership, workflow, or child index changes.
- Update **child docs** when parent changes alter local rules.
- **Remove stale or contradictory text immediately.** Small edits may leave docs unchanged, but the DOX pass still must happen.

### Style
- Concise, current, operational. Document stable contracts, not diary entries.
- Broad rules in parent docs, concrete details in child docs.
- Direct bullets with explicit names.
- **Delete stale notes instead of explaining history.**

---

## Purpose

ERP-SYSTEM is a **Multi-Company ERP** for Libyan SMEs. Single Holding + Many Companies (1:N). NO multi-tenancy. NOT SaaS. One deployment per Holding.

- **Production:** Hugging Face Space (`anas-assasket-erp-system.hf.space`, canonical lowercase).
- **Database:** Supabase (PostgreSQL 17, eu-central-1).
- **Stack:** C#/.NET 9 backend + TypeScript/Next.js 14 frontend + Dapper + FluentMigrator.

The **single source of truth for architecture** is `/docs/architecture/holding-company-architecture.md`. The **single source of truth for governance** is `/CONSTITUTION.md`.

---

## Ownership

| Layer | Owner | Authority |
|-------|-------|-----------|
| **Project Owner** | Anas (anas600) | Constitution, staging/production, architecture changes |
| **Cloud Coordinator** | Siti (Mavis mode) | Plan, hand-offs, verify, merge, governance files |
| **Architect / Strategic Advisor** | Muhammad (Mavis mode) | Analysis, decisions, retrospectives |
| **Tech Lead (Local)** | Mavis Local (Windows) | Implementation, Jimis, PRs, --admin merge on develop |
| **DevOps** | Dev (Mavis mode) | CI, infra, crons |
| **External Tech Lead (sandbox)** | Mephisto | Independent work on `feature/sprint-4-polish-demo-data` (see `docs/AGENTS.md` for context) |
| **E2E team** | Abdo's team | Playwright verification on `feature/abdo-team` |

Only Anas can change the Constitution. Everything else flows through the sprint model.

---

## Local Contracts (Repo-Wide Rules)

### Architecture (Constitution Article 3)
- ✅ `company_id` everywhere, NO `tenant_id`.
- ✅ `Company` entity, NO `Tenant` entity.
- ✅ `CompanyContext`, `CompanyMiddleware` (in `Shared/CompanyContext/` — folder renamed Sprint 22, was misleadingly called `MultiTenancy/`).
- ✅ `user_companies` join table.
- ✅ JWT carries `company_ids[]` + `X-Company-Id` header.
- ✅ Holding-level queries require `holding_admin` role.
- ✅ **Sprint 22:** Single-deployment target. **No event bus** — cross-module = direct service calls (Posting Rules workflow).
- ✅ **Sprint 22:** Marten references removed (DEC-017 dead code path).

### Module List (Sprint 22 — 15 → 9)
| Module | Status | Notes |
|---|---|---|
| Identity | ✅ Keep | Auth + RBAC |
| Companies | ✅ Keep | Manage subsidiaries (holding + N) |
| Finance | ✅ Keep | CoA, Journal, PostingRules, Ledger |
| Inventory | ✅ Keep | Items, Stock, Movements |
| Procurement | ✅ Keep | PO, GR, Bill |
| AccountsReceivable | ✅ Keep | Customer, Invoice, Receipt |
| HR | ✅ Keep | Employee, Attendance, Leave |
| Payroll | ✅ Keep | PayrollRun, SalaryStructure |
| Projects | ✅ Keep | Project, Tasks, Cost |
| Dashboard | ✅ Keep (simplify) | Single page |
| ~~Activity~~ | ❌ Deleted Sprint 22 | Audit covers it |
| ~~Notifications~~ | ❌ Deleted Sprint 22 | Re-add inline when needed |
| ~~Search~~ | ❌ Deleted Sprint 22 | Re-add if user flow emerges |
| ~~Reports~~ | ❌ Deleted Sprint 22 | Per-module reports now |

### Cross-Module Communication
- **Old (event-driven):** `_eventBus.PublishAsync(...)` → OutboxProcessor → Handler
- **New (Sprint 22):** Direct service call (synchronous, same transaction)
- **Example:** `SalesInvoiceService.PostAsync` directly calls `PostingRulesService.ApplyRulesAsync` + `ProjectsService.UpdateCostAsync`
- **No async event handling.** Simpler, fewer moving parts, easier to debug.

### Code Standards
- **Backend:** C# / .NET 9 / Dapper (NO EF Core) / FluentMigrator / xUnit.
- **Frontend:** TypeScript / Next.js 14 (App Router) / Tailwind / shadcn/ui / Jest.
- **Migrations:** Idempotent (`CREATE TABLE IF NOT EXISTS`, `DO $$ ... IF EXISTS ... $$`).
- **Batch inserts:** Postgres `unnest()` for ≥ 10 rows. No N+1.
- **Atomicity:** Multi-insert in single transaction.
- **API-First:** Backend before Frontend. One test per endpoint.

### 10 Soft Rules (Constitution Article 8)
1. One Branch (develop only — no fork chaos).
2. API-First.
3. Idempotent Migrations.
4. One Test Per Endpoint.
5. `company_id` Only (NO `tenant_id`).
6. No EF Core.
7. Pre-Demo Data (real, not mocks).
8. No Secrets in Code.
9. Frontend-First Errors (AR + EN).
10. Document in AGENTS.md.

### 5 Anti-Patterns
- ❌ Over-engineering → YAGNI.
- ❌ Premature optimization → Profile first.
- ❌ Speculative features → Build what you need.
- ❌ Custom solutions → Use libraries.
- ❌ Long sync tasks → Async / queue.

### Git Workflow
- **Single source of truth = remote GitHub.** All work happens on remote.
- ❌ Direct commit to `main` or `develop`.
- ✅ PR via `feature/*` branch → develop.
- ✅ Mavis Local merges with `--admin` (per Constitution Article 10).
- ✅ Force-push only with `--force-with-lease`.
- Squash merge only.

### Branch Protection (per Constitution Article 4)

**Per Anas 2026-07-31 23:16 UTC directive** — branch architecture reset to align with 3-Layer Model:

| Branch | Role | Protection |
|--------|------|-----------|
| `develop` | **DEFAULT** — all active work (Layer 1 + Layer 2) | 6 required checks (Backend Tests, Frontend Build, Analyze csharp, Analyze js-ts, TruffleHog, **Architecture Guard — no tenant_id**) + 1 review + linear history + conversation resolution + enforce admins |
| `main` | **FROZEN** — anchored at `v1.0.0-archive` tag (pre-Layer-3) | **LOCKED** (lock_branch=true) + 1 review + linear history + no force-push + no deletions + enforce admins. Can only be modified via Owner (Anas) explicit action. |
| `v0.0.0-pre-branch-reset` (tag) | Safety anchor — state of main before reset (governance v2.0 + Sprint 8 T4 refactor proposal) | Immutable tag |
| `v1.0.0-archive` (tag) | Work anchor — 3-Layer Model implementation: Sprint 10 (Holding rename + scoped DI), Sprint 11 (full FE+BE demo), Sprint 12 (local psql + no-tenant-id guard), Sprint 13 (Layer 2 MVP container) | Immutable tag |

- Required checks on `develop` (6): Backend Tests, Frontend Build, CodeQL (Analyze csharp + js-ts), TruffleHog, **Architecture Guard — no tenant_id** (Sprint 12 addition).
- Required reviews: 1 (admin bypass ON for Mavis Local per Article 10).
- `main` is **LOCKED** — no commits, no force-pushes, no deletions. To change `main`, Owner (Anas) must unlock via GitHub UI.
- Force-push only with `--force-with-lease` (on feature/* branches, never on develop/main).

### 3-Layer Model (per Anas 2026-07-31 21:51 UTC directive)

| Layer | Purpose | Setup | Branch | DB | Status |
|-------|---------|-------|--------|----|----|
| **1. Development** | Local backend on host, with test data, fast iteration | `local-docker/` (with seed) or direct host runs | any `feature/*` | Local Docker Postgres | **Active** |
| **2. Staging / Containerized MVP** | Clean schema in Docker, browsable, no test data — client deliverable | `mvp-docker/` (production build, no seed) | `develop` after merge | Local Docker Postgres (clean) | **Active** |
| **3. Production** | Client production | (FROZEN — out of scope per Anas 2026-07-31 21:51 UTC "لا اهتم بيها الان") | `main` | Supabase production | **FROZEN** |

**Workflow between layers** (per Anas 2026-07-31 21:51 UTC):

1. **Local Team** develops in **Layer 1** — direct host + test data (fast)
2. **Sprint done in Layer 1** → Local Team merges to `develop` via PR
3. **Admin Team** (Mavis) takes over:
   - Pull new commit
   - `cd mvp-docker && docker compose up -d --build`
   - `./smoke-test.ps1` (verifies clean MVP runs)
   - **Notify Anas** to browse the system
4. **Anas** browses the system, decides: continue development, or hand to client
5. **Strategic Advisor محمد (Mavis)** decides when to transition Layer 1 → Layer 2

**Why two layers?** Layer 1 is for **speed** (Local Team iterates fast with test data on the host). Layer 2 is for **cleanliness** (a fresh container with a real schema, no test data, mimics what the client will receive). Both run on the local machine; Layer 1 uses dev data, Layer 2 uses clean data.

### Environment Layers (Legacy — pre-3-Layer Model)

> ⚠️ The following 4-layer model (Local / Dev / Staging / Production with Supabase) was the **old** model. The 3-Layer Model above supersedes it as of Sprint 13. The Supabase Dev tier is still the **default DB for `dotnet test` in CI** — that hasn't changed.

| Layer | Branch | DB | Status |
|-------|--------|----|----|--------|
| Local (Mavis Local) | any `feature/*` | **Local Docker Postgres (fast)** | Active |
| Dev | `develop` | Supabase dev | Active (CI only) |
| Staging | (none) | Supabase staging | **FROZEN** |
| Production | `main` | Supabase production | **FROZEN** |

**Mavis Local dev config (per Anas, 2026-07-29):** use `localhost:5432` (local Docker) for **10-100x faster** login + DB queries. The `appsettings.Development.json` (gitignored) is pre-configured for this. To switch back to Supabase, set `ConnectionStrings__Postgres=Host=aws-0-eu-central-1.pooler.supabase.com;...` env var.

### Secrets
- **NEVER** in code, chat, or PRs. Use env vars or secret manager.
- BCrypt cost 12 for passwords.
- JWT HS256 + refresh token rotation.

---

## Work Guidance

### Two-Mode Workflow (Sprint 17, per Anas 2026-08-01 06:43 UTC)

The team operates in **two distinct modes**. The mode is switched only by Anas (Project Owner):

| | **Mode 1: Development** (default) | **Mode 2: Release** |
|---|---|---|
| **Trigger** | Anas + Muhammad (strategic advisor) discuss priorities | Anas says "ادفع" |
| **Admin role** | Team lead + coordinator + executor (with Jimis) | Release engineer (push + relax + merge + tag + restore) |
| **What happens** | Local work on `feature/sprint-N-...` branch — multiple sprints can be merged locally | git push → PR → CI (6/6) → relax → squash-merge → tag → restore |
| **Push to remote** | ❌ NO | ✅ YES |
| **CI on GitHub** | ❌ NO (no push) | ✅ YES (6 required checks) |
| **mvp-docker rebuild** | ❌ NO (cron doesn't fire) | ✅ YES (cron `mvp-auto-rebuild-on-develop-push` fires within 5 min) |
| **Telegram notify** | ❌ NO | ✅ YES ("✅ Sprint N auto-rebuild: success in Xs") |
| **Browser preview** | Layer 1 (local-docker) with test/dev data | Layer 2 (mvp-docker) with clean install + optional demo data |

**The switch from Mode 1 → Mode 2 is the only point where:**
1. The git remote `develop` branch gets a new commit
2. CI runs on GitHub
3. The cron fires
4. mvp-docker is rebuilt
5. Telegram pings Anas

**During Mode 1:** all work is local. Jimis add commits to the same feature branch. The Admin (Mavis Local) merges their output. No external system is touched. **The cron never fires because the remote `develop` SHA doesn't change.**

**During Mode 2:** the workflow is the same one used for Sprint 13, 14, 15, 16. The Admin does:
1. `git push` the feature branch
2. `gh pr create --base develop --head feature/sprint-N-...`
3. Wait for the CI monitor cron (`monitor-sprintN-ci-prN`) to detect all 6 required checks are green
4. The cron itself does: relax develop branch protection → `gh pr merge --squash --admin --delete-branch` → `git tag -a vX.Y.Z-sprintN` → restore develop branch protection
5. The remote `develop` SHA changes → the `mvp-auto-rebuild-on-develop-push` cron (5 min) detects the change → runs the rebuild → smoke test → Telegram pings Anas

**Anas is the only one who can say "ادفع" to switch modes.** No one else can push to remote or trigger CI.

### Sprint Model
1. **Cloud (Siti)** writes hand-off → `docs/workflow/sprint-N.md` (push to develop). For small tasks in the 2-day window, Mavis Local can self-plan.
2. **Mavis Local** pulls develop, spawns Jimis (BE + FE parallel). See [`.mavis/AGENTS.md`](./.mavis/AGENTS.md) for the worker contract.
3. **Jimis** execute, each one **declares their scope** in the nearest AGENTS.md (per worker contract) and **adds a CHANGELOG entry**.
4. **Mavis Local** verifies (T6: build + test + typecheck).
5. **Mavis Local** opens PR (`feature/sprint-N-*` → develop) — **this is the Mode 1 → Mode 2 transition**.
6. **Mavis Local** self-merges via the temporary-relax pattern (per Article 10 — see CONSTITUTION.md) or **Cloud** auto-merges when CI green.
7. **Develop** updated → next sprint.

**Sprint duration:** 1.5-2 hours (sprints up to 4-6h for big demo work).

### Commands
```bash
# Backend
cd src/backend
dotnet build
dotnet test
dotnet run --project Host         # API on :5001

# Frontend
cd src/frontend
npm install
npm run dev                       # :3000
npm run build
npm run typecheck                 # tsc --noEmit

# Git
git fetch origin
git pull --rebase origin develop
git push --force-with-lease origin feature/<name>
gh pr create --base develop
gh pr merge <num> --squash --admin   # Mavis Local only

# Local Docker
cd local-docker
cp .env.example .env
docker compose up -d --build
# Wait for healthy
docker compose ps
# Apply demo seed (idempotent)
docker cp ../docs/seed-sprint4-demo-data.sql erp-postgres-local:/tmp/seed.sql
docker exec -it erp-postgres-local psql -U erp -d erp_system -f /tmp/seed.sql
# Open: http://localhost:3000 — login: admin@alfajr.local / Demo1234
```

> **For full local-docker architecture, see [`docs/workflow/local-docker.md`](./docs/workflow/local-docker.md).**
> **For past fixes (PR #170), see [`docs/workflow/local-docker-fixes-report.md`](./docs/workflow/local-docker-fixes-report.md).**

### Crons (Cloud only + Local tool)
- **Cloud (GitHub Action `state-cron.yml`):** runs every 5 min, updates `state.json` on no-change, posts to develop on change. **The cron is a tool, not an actor — it does not own the ball.**
- **Local (platform `mavis-local-coordinator`):** runs every 5 min during active hours (08:00–22:00 Africa/Tripoli). Helps Mavis Local stay updated. **The cron is a tool, not an actor — the ball stays with mavis-local / mavis-cloud / anas.**
- **Crons are NEVER in the project repo** (per Anas 2026-07-29 18:42). They live on the platform's Schedules tab.

---

## Verification

Run before opening a PR:
- [ ] `dotnet build` — zero errors.
- [ ] `dotnet test` — all green.
- [ ] `npm run typecheck` — zero errors.
- [ ] `npm run build` — production build succeeds.
- [ ] `git log origin/develop --oneline | head -10` — current with develop.
- [ ] **No `tenant_id`** in any file: `grep -r "tenant_id" src/`.
- [ ] **No secrets** in code: `grep -r "password\s*=" src/`.
- [ ] **AGENTS.md updated** if contracts/rules changed.
- [ ] **CHANGELOG.md** has this sprint's entry.
- [ ] **DEC-085: Constitution Article 3 code-level audit** (recurring — Sprints 19, 21, 22, 23, 24, 25, 27, 28 all found at least one violation):
  1. Every entity has `CompanyId` field. `grep -L "CompanyId" src/backend/Modules/*/Entities/*.cs`
  2. No `CompanyId = Guid.Empty` boilerplate. `grep -rn "CompanyId = Guid.Empty" src/backend/`
  3. Every `CREATE TABLE` includes `company_id` (or its absence is documented). `grep -rn "CREATE TABLE" src/backend/`
  4. Every runtime `INSERT` includes `company_id`. `grep -rn "INSERT INTO" src/backend/ | grep -v company_id`
  5. Every PK on a shared-resource table (e.g. document sequences) includes `company_id`. Manual review.
  6. **No `?` characters in user-visible data** (DEC-087 — Sprint 25 PowerShell bug). If you see `????` or `?` strings in master data, the seeder is broken (encoding). Use `ArabicDevSeederHostedService` (C# UTF-8) not PowerShell `ConvertTo-Json` for Arabic data.
  7. **Cyclic FK requires 3-pass UPSERT** (DEC-092 — Sprint 27 HR). For 2-table cycles (e.g., `departments.manager_id` ↔ `employees.department_id`), insert parents without children FKs first, then insert children with parent FKs, then update parents.
  8. **Service uses ICompanyContext.CompanyId, not req.CompanyId** (DEC-095, L19, L29, L30 — Sprint 28 Project + StockMovement). The request DTO's CompanyId is a spoofing risk. The service resolves the company from the JWT context. Tests use `TestCompanyContextFactory.Create()` (L26 fix). The `CreateAsync` for any aggregate that writes to multiple child tables must read the companyId once at the top and pass the local variable to all writes.
  9. **Account.CompanyId is `Guid` (not `Guid?`)** (DEC-097 — Sprint 28). The DB column has been `NOT NULL` since Sprint 22. Nullable type is a code-level inconsistency that would cause NRE at runtime. Any entity backed by a `company_id NOT NULL` column should be `Guid` (not `Guid?`).

CI runs 6 required checks on PR open. Admin bypass is ON (per Article 10).

---

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`/docs/AGENTS.md`](./docs/AGENTS.md) | Documentation directory | Active |
| [`/infra/AGENTS.md`](./infra/AGENTS.md) | Infrastructure-as-Code | Active |
| [`/infra/docker/AGENTS.md`](./infra/docker/AGENTS.md) | Docker configs | Active |
| [`/scripts/AGENTS.md`](./scripts/AGENTS.md) | Build/utility scripts (incl. Sprint 15 auto-rebuild) | **Active** |
| [`/local-docker/AGENTS.md`](./local-docker/AGENTS.md) | Local dev environment (Layer 1) | **TO CREATE** |
| [`/mvp-docker/AGENTS.md`](./mvp-docker/AGENTS.md) | Containerized MVP (Layer 2) — Sprint 13 | **TO CREATE** |
| [`/src/AGENTS.md`](./src/AGENTS.md) | Source code root | Active |
| [`/src/backend/AGENTS.md`](./src/backend/AGENTS.md) | Backend (.NET) | Active |
| [`/src/frontend/AGENTS.md`](./src/frontend/AGENTS.md) | Frontend (Next.js) | Active |
| [`/.github/AGENTS.md`](./.github/AGENTS.md) | GitHub workflows | Active |
| [`/.mavis/AGENTS.md`](./.mavis/AGENTS.md) | Mavis orchestration (worker instructions for Jimis) | Active |

**Note:** `src/backend/Modules/<module>/` and `src/frontend/app/<route>/` have their own AGENTS.md (created when modules become durable boundaries).

---

## Sprint 30 Decisions (DEC-100..106) + Lessons (L40..L42)

### Decisions
- **DEC-100 — Single CoA page (delete duplicate)** (Sprint 30): `src/frontend/app/(authenticated)/accounts/page.tsx` deleted via `mavis-trash`. Sidebar entry removed from `AppShell.tsx`. Only `/finance/accounts` (Sprint 11 T1) remains. User feedback: "duplicate pages are technical debt."
- **DEC-101 — Default reference data is essential, not optional** (Sprint 30): `TrySeedDefaultReferenceDataAsync` added to `DefaultHoldingBootstrapHostedService.cs`. Always-on (no flag) — seeds 1 default warehouse (WH-001 "المستودع الرئيسي") + 1 default cost center (CC-001 "الإدارة العامة", type=Department=2). Idempotent via `ON CONFLICT (company_id, code) DO NOTHING`. Without this, the new PO/GR seeder and the receipt form both fail on fresh install.
- **DEC-102 — Make cost center / allocations optional** (Sprint 30): Receipt allocations no longer required. `ReceiptService.CreateAsync` skips validation if `req.Allocations == null || req.Allocations.Count == 0`. FE form `page.tsx` skips the "أضف تخصيصاً واحداً على الأقل" check. Cost center already optional. Rule: don't overcomplicate — make the form work for the common case (single payment, no allocation).
- **DEC-103 — Atomic document sequence** (Sprint 30): `DocumentSequenceRepository.GetNextNumberAsync` refactored from UPSERT-then-SELECT (race condition) to `INSERT ... ON CONFLICT ... DO UPDATE SET last_number = ... RETURNING last_number` in a single statement. Fixes `PO-2026-0002 already exists` duplicate-key errors.
- **DEC-104 — Vendor name in DTO** (Sprint 30): `VendorBillResponse` now has `VendorName` + `VendorCode`. `VendorBillService.BuildVendorMapAsync` does single-batch vendor lookup. FE no longer shows raw GUIDs (L40: API must return human-readable names).
- **DEC-105 — Full PO+GR+Bill seeder** (Sprint 30): `ArabicProcurementDevSeederHostedService` rewritten. All 3 passes implemented:
  - Pass 1: 10 POs with computed line `sub_total` + header totals
  - Pass 2: 10 GRs, status=`Received`, posted to default warehouse WH-001 (DEC-101 made this possible)
  - Pass 3: 10 Bills, status=`Posted`, linked to GRs, each with `BENCH-BILL-2026-NNNN` Journal Entry (L39: "seeders that test other parts of the system")
- **DEC-105a — PO vendor enrichment** (Sprint 30): `PurchaseOrderResponse` now has `VendorName` + `VendorCode`. `PurchaseOrderService.BuildVendorMapAsync` (Dapper direct) added. Same pattern as DEC-104 for bills.
- **DEC-106 — SalesInvoiceStatus as string, not int enum** (Sprint 30): `SalesInvoice.Status` + `SalesInvoiceResponse.Status` changed from `int` to `string` to match the seeder + schema. Fixed 6 references in `ReceiptService` + `SalesInvoiceService` (Draft/Sent/Paid/PartiallyPaid/Cancelled → "Draft"/"Sent"/etc.). Fixed the 500 error on `/api/ar/sales-invoices` (Dapper couldn't map the int enum to the string seeder output).

### Lessons (L40..L42)
- **L40 — API must return human-readable names, not raw GUIDs** (DEC-104 + DEC-105a). Every list/GET endpoint that returns a foreign-key reference should also include the referenced entity's `Name` + `Code`. Pattern: build a `Dictionary<Id, (Name, Code)>` via single batch lookup, enrich the response. This applies to PO, GR, Bill, Receipt, Payment, JournalEntry, Project — anywhere a FK is exposed. New endpoints: add the enrichment from day 1, not as an afterthought.
- **L41 — Seeders that create transactions must compute totals from line items** (DEC-105b). The old PO seeder stored `sub_total=0, tax_amount=0, total_amount=0` because it didn't compute from lines. Always: line `sub_total = qty * unit_price`; header `sub_total = sum(lines.sub_total)`; `tax_amount = sum(lines.sub_total * lines.tax_rate)`; `total_amount = sub_total + tax_amount`. Libya default = 0 tax. Don't trust the JSON to carry totals — compute them at insert time.
- **L42 — Seeder cross-pass dependencies need explicit lookups** (DEC-105c/d). Pass 2 (GRs) needs the PO ids from Pass 1. Pass 3 (Bills) needs the GR ids from Pass 2. Build lookup maps (`Dictionary<string, Guid>`) after each pass: `poMap = po_number → id`, `grMap = gr_number → id`. Pass 3 also needs `goods_receipt_id` — link via PO → GR. The map keeps the passes order-independent and idempotent.

### Pending Article 3 audit (carry-over to Sprint 31+)
- `Payments` module — likely 4-8 violations
- `ProjectCostCenter` (in Companies module) — likely 2-4 violations
- `AccountService` (in Finance) — likely 2-4 violations
- `ChartOfAccountsService` (in Finance) — likely 2-4 violations
- `PayrollService` (in Payroll) — likely 2-4 violations
- Any service that still has `req.CompanyId` in the DTO — refactor to `_companyContext.CompanyId` (L30)

---

## Sprint 28 Decisions (DEC-094..097) + Lessons (L25..L30)

### Decisions
- **DEC-094 — Payroll Article 3 audit** (Sprint 28): 5 entities (`SalaryStructure`, `SalaryStructureLine`, `PayrollRun`, `PayrollItem`, `PayslipComponent`) + 1 service (`PayrollService` injects `ICompanyContext`) + 1 repo (`PayrollRepository` adds `@CompanyId` to all INSERTs). `EosService` clean (read-only).
- **DEC-095 — Projects Article 3 audit** (Sprint 28): 4 entities + 3 services (`ProjectService`, `TaskService`, `ResourceService`, `ResourceAssignmentService` inject `ICompanyContext`) + 4 repos. `ProjectService.CreateAsync` is the critical fix — it now uses `_companyContext.CompanyId` (NOT `req.CompanyId`) for the project + the auto-created `ProjectBudget`. This is **L19 cross-tenant safety** applied to the `Project` aggregate.
- **DEC-096 — StockMovement service refactor** (Sprint 28): entity + repo already had `CompanyId`. Only `StockMovementService` was using `req.CompanyId`. Refactored all 4 `Create*` methods (Receive/Issue/Transfer/Adjust) to use `_companyContext.CompanyId`. L19 + L30.
- **DEC-097 — Finance/Account minor fix** (Sprint 28): `Account.CompanyId` changed from `Guid?` to `Guid`. DB column is `NOT NULL` since Sprint 22. Nullable type was an NRE risk.
- **DEC-099 — TestCompanyContextFactory helper** (Sprint 28): centralized factory `TestCompanyContextFactory.Create()` returns a fully-set-up `ICompanyContext` (with `.Setup(c => c.CompanyId).Returns(Guid)`). Replaces the broken Sprint 27 IIFE pattern (`(function(){...})()` — JavaScript, not C#). Use this in every test that needs to instantiate a service that takes `ICompanyContext`.

### Lessons (L25..L30)
- **L25 — Audit pattern holds across 8 sprints.** Sprints 19, 21, 22, 23, 24, 25, 27, 28 all surfaced Article 3 violations. The DEC-085 checklist catches 100% of them. The bug is "if you don't enforce it explicitly, the code drifts." Each sprint fixes the worst 4-8; the rest are on the carry-over.
- **L26 — `function(){...}()` is JavaScript, not C#.** The Sprint 27 IIFE pattern in `ProjectServiceTests.cs` was wrong — a previous bulk-replace tool injected it. **Rule:** any bulk-replace touching `.cs` files must be followed by `dotnet build` in the same commit, not deferred. The test file didn't even compile; running the suite in CI caught it.
- **L27 — Established pattern = predictable time.** 3rd seeder (`ArabicProcurementDevSeeder`) implemented in <2h (vs 4-6h for the first). Pattern: JSON + IHostedService + UPSERT + Dapper + double-gate + Content include + appsettings flag. The pattern absorbs schema surprises (no `name_en` on vendors, no `updated_at` on `purchase_order_lines`) with brief psql `\d` lookups.
- **L28 — Schema surprises are 1:1, not 1:1 with entity property names.** Always `psql \d <table>` before writing the INSERT. Document the surprises in the seeder's startup log.
- **L29 — Aggregate with multiple child writes = read CompanyId once, pass local variable.** `ProjectService.CreateAsync` writes both `Project` + `ProjectBudget`. Reading `_companyContext.CompanyId` once at the top and using a local `companyId` variable in both writes is cleaner + safer than calling the property twice. The test verifies this by reading the companyId from the mock context.
- **L30 — DTO CompanyId = security risk.** When the request DTO carries `CompanyId` but the service has access to `ICompanyContext`, the context wins. The DTO's CompanyId is spoofable; the context's is bound to the JWT. `StockMovementService` (4 methods) + `ProjectService.CreateAsync` follow this rule. Other services that still have `req.CompanyId` in the DTO are carry-over.

### Pending Article 3 audit (carry-over to Sprint 29+)
- `Payments` module — likely 4-8 violations
- `ProjectCostCenter` (in Companies module) — likely 2-4 violations
- `AccountService` (in Finance) — likely 2-4 violations
- `ChartOfAccountsService` (in Finance) — likely 2-4 violations
- `PayrollService` (in Payroll) — likely 2-4 violations
- Any service that still has `req.CompanyId` in the DTO — refactor to `_companyContext.CompanyId` (L30)

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode), approved by Anas — DOX framework applied_
