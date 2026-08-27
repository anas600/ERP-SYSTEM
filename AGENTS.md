# 🤖 AGENTS.md — ERP-SYSTEM (Root DOX Rail)

> **This is the DOX rail.** All work in this repository must follow the DOX framework.
> Read this file fully + walk the chain to your target path before editing anything.

<<<<<<< HEAD
**Last updated:** 2026-08-27 (Sprint 62: Progress Billing Refinement FULLY DONE in 2 waves — DEC-197 Regional Premium (NDB+CIT+SS) + DEC-198 PDF Export via QuestPDF, 24 tests pass, 0 regressions, branch `feature/sprint-62-progress-billing` @ `3ba6e47`, awaiting Anas "ادفع" for Mode 2. Sprint 61: Engineer's Report + 5 Permanent Fixes fully DONE in 3 waves — 7 DECs (DEC-192..198), 56 tests pass, 0 regressions. Sprint 60: CoA Cleanup fully DONE. Sprint 59: BOQ + Variation Orders + 6 CoA accounts. Sprint 22: 15→9 modules major refactor. **Architecture target:** `/docs/architecture/REFACTOR-SPRINT-22.md`)
=======
**Last updated:** 2026-08-27 (Sprint 63: RBAC + Module Visibility FULLY DONE in 3 waves — DEC-211..218 (8 DECs), 9 new lessons L189-L197, BE 48/48 + FE 9/9 tests pass, 0 regressions, branch `feature/sprint-63-rbac` @ `5a34326`, awaiting Anas "ادفع" for Mode 2. Sprint 62: Progress Billing Refinement FULLY DONE in 2 waves — DEC-197 Regional Premium + DEC-198 PDF Export, 24 tests, 0 regressions. Sprint 61: Engineer's Report + 5 Permanent Fixes fully DONE. Sprint 60: CoA Cleanup fully DONE. Sprint 22: 15→9 modules major refactor. **Architecture target:** `/docs/architecture/REFACTOR-SPRINT-22.md`)
>>>>>>> 8e15454 (docs(governance): Sprint 63 - Last updated header refreshed)

> ## 🗂️ NOTION-FIRST INTEGRATION (Sprint 60+)
>
> **Notion Hub = Source of Truth للقرارات والمهام والدروس والتقارير.** [🏢 Hub Page](https://app.notion.com/p/3c6c003bf39681dcb1cbc904cbaf82da)
>
> ### 📊 6 قواعد بيانات + 1 صفحة ربط
> - **🚀 Sprints DB** (`collection://05812e3c-...`): كل سبرنت بحالته (Status, Tag, Commit, PR, Lessons)
> - **📋 DEC Log DB** (`collection://d6790d05-...`): كل قرار (DEC-XXX) بحالته
> - **✅ Tasks DB** (`collection://d7372a8a-...`): كل مهمة (Open/In Progress/Done)
> - **💭 Discussions DB** (`collection://3a5d445a-...`): كل نقاش (Active/Resolved)
> - **📊 Reports DB** (`collection://2439683d-...`): كل تقرير (مع File Path + Notion Page)
> - **📚 Lessons DB** (`collection://6209269c-...`) 🆕: كل درس (L001..L175+ قابل للبحث)
> - **📂 Local Files Map** 🆕: ربط المسارات المحلية (`C:\Users\Anas\Documents\`) ↔ Notion Pages
>
> ### 🔄 Auto-Update Rule (مُلزِم)
> **كل إغلاق Sprint = Admin يحدّث Notion Hub تلقائياً** (قبل ما يقول "ادفع"):
> 1. ✅ إضافة/تحديث row في `Sprints DB` (Status: ✅ Done + Tag/Version + Commit + PR + Lessons)
> 2. ✅ إضافة/تحديث rows في `DEC Log DB` (Status: ✅ Approved + Date)
> 3. ✅ إضافة/تحديث rows في `Tasks DB` (Status: ✅ Done + Owner)
> 4. ✅ إضافة rows في `Lessons DB` (L### جديد مع Date Learned + Sprint + Related DEC + Body)
> 5. ✅ تحديث Action Card في Hub (ما يحتاج قرار Anas للـ Sprint القادم)
> 6. ✅ (اختياري) إضافة row في `Reports DB` (إن وُجد تقرير جديد مع File Path + Notion Page)
> 7. ✅ (اختياري) تحديث `Local Files Map` (إن وُجد ملف جديد محلياً)
> 8. ✅ إضافة entry جديد في `CHANGELOG.md` (Source of Truth للتغييرات الكود)
> 9. ✅ تحديث `docs/workflow/sprint-N.md` (Sprint hand-off doc)
>
> **كل ملف يُحفظ محلياً → row في Reports DB + entry في Local Files Map.**
> **كل درس يُكتشف → row في Lessons DB.**
> **كل مهمة → row في Tasks DB (تتحدّث دورياً).**
>
> **ممنوع:** إغلاق Sprint بدون تحديث Notion Hub.

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

## Internal Personas (Mavis Roles — effective 2026-08-25)

> **Source of truth:** This section clarifies the **internal Mavis roles** used during sprint execution. They are **personas within Mavis**, not external people, to make the workflow clear and conversational for Anas.

### 🎭 The Three Internal Personas

| Persona | Role | Modes | Lives In |
|---------|------|-------|----------|
| **محمد (Muhammad)** | Personal consultant + orchestrator + verifier | **M1-Exec** (with Admin), **M2-Discussion** (with Anas), **M3-Trust** (as human client) | Root session — the only "human-facing" voice |
| **Admin (القائد التقني)** | Tech Lead — owns code, tests, and quality | **M1-Local** (oversees Workers on local), **M2-Release** (git push + PR + merge + tag + Docker rebuild) | Same root session as Muhammad, internal hand-off via context |
| **Workers (Jimis)** | Bounded producers — write code, run tests | Single mode (execute assigned contract) | Sub-sessions spawned via `task` tool with `run_in_background: true` |

### 🧢 Muhammad's Expert Hats (worn as needed)

| Hat | Trigger | Example |
|-----|---------|---------|
| 💼 **محاسب خبير (IFRS/IAS)** | CoA, Journal, P&L, BS, Tax | Reviewing DEC-NEW-5 (NDB + Stamps + CIT) |
| 🏗️ **مهندس مشاريع** | BOQ, Progress Billing, Project P&L | Sprint 62 (net_amount + regional premium) |
| 🎯 **مستشار استراتيجي** | Roadmap, sequencing, decisions | Sprint 60 ↔ 63 ↔ 64 ↔ 65 orchestration |
| 🏛️ **معماري أنظمة** | Clean Architecture, Clean CoA, Layering | DEC-189 balance migration design |
| 🎨 **خبير تصميم (UX)** | Module visibility, role templates | Sprint 63 Smart Sidebar design |
| 📘 **معلم** | Teaching Anas the system | Educational Report (4 expert reports) |

### 🔁 How the Three Personas Interact

```
┌──────────────────────────────────────────────────────────────┐
│  M1-Local Loop (every sprint, default)                       │
│  ─────────────────────────────────────────────────────────── │
│  Anas ─→ Muhammad(M1-Exec) ─ plan + DECs + contract          │
│         ↓                                                    │
│         Admin(M1-Local) ── spawn Worker via `task` tool      │
│         ↓                                                    │
│         Worker(Jimi) ── write code + tests on feature/*      │
│         ↓                                                    │
│         Admin(M1-Local) ── verify success criteria            │
│         ↓                                                    │
│         Muhammad(M1-Exec) ── report to Anas                  │
│         ↓                                                    │
│         [if fixes needed] → loop back to spawn Worker        │
│         [if OK] → ready for Mode 2 or Trust Mode             │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  M2-Release Mode (only when Anas says "ادفع")                │
│  ─────────────────────────────────────────────────────────── │
│  Admin(M2-Release) ── git push + gh pr create + relax        │
│         ↓                                                    │
│         CI runs (6/6 must pass)                               │
│         ↓                                                    │
│         Admin(M2-Release) ── gh pr merge --squash --admin    │
│         ↓                                                    │
│         Admin(M2-Release) ── git tag -a vX.Y.Z-sprintN       │
│         ↓                                                    │
│         Admin(M2-Release) ── restore branch protection       │
│         ↓                                                    │
│         mvp-auto-rebuild cron fires → Telegram ping          │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  M3-Trust Mode (Anas requests manual verification)           │
│  ─────────────────────────────────────────────────────────── │
│  Muhammad(M3-Trust) ── wear "client" hat                     │
│         ↓                                                    │
│         Open FE on localhost:3000 + BE on :5000              │
│         ↓                                                    │
│         Walk through the sprint scenario as a real user     │
│         ↓                                                    │
│         Capture findings (pass/fail/blocked)                │
│         ↓                                                    │
│         Report back to Anas (with screenshots if needed)    │
└──────────────────────────────────────────────────────────────┘
```

### 🛡️ Persona Discipline Rules

1. **Single conversation voice = Muhammad.** Even when discussing Admin or Worker work, the response comes from Muhammad. Internal hand-offs are noted but not acted out in dialogue.
2. **Admin is internal — no separate session.** As of 2026-08-25, the Admin role lives in the **same root session** as Muhammad. The previous "Local Team session" pattern (separate session) is **deprecated** for the sprint 60+ roadmap. See `docs/workflow/sprint-60.md` for migration notes.
3. **Workers are sub-sessions only.** They never become "Muhammad" or "Admin" — they are bounded producers with a single deliverable and a "definition of done".
4. **Anas never talks to Workers directly.** All communication goes through Muhammad (M1-Exec) → Admin (M1-Local) → Worker. This keeps contracts clean and verification centralized.
5. **Workspace discipline:** Every Worker prompt must include the absolute workspace path. The correct workspace is `C:\Users\Anas\.minimax-agent\projects\ERP-Holding`. Never use `sprint-N` subfolders (those are historical worktrees, not active projects).

### 🔗 Related Documentation

- **Notion Hub Workflow page:** [`📋 Workflow — تنفيذ خارطة الطريق`](https://app.notion.com/p/3c6c003bf39681cfb868f1b34d37fd70) — mirrors this section + includes current pipeline status
- **`docs/workflow/sprint-60.md`** — Sprint 60 contract (15 DECs, 3 waves, success criteria)
- **`CONSTITUTION.md`** — governance v2.0 (Anas-only edits)
- **`.mavis/AGENTS.md`** — worker/jimi instructions (next to be updated to reference these personas)

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

## Sprint 32 Decisions (DEC-112) + Lessons (L47..L49)

### Decisions
- **DEC-112 — Projects module tables + `quoted` flag** (Sprint 32): 4 missing `data-types/*.json` (resources, project_tasks, resource_assignments, project_budgets) added. `FieldDefinition` got a `quoted: true` flag so DataTypeMigrator can force double-quoted SQL identifiers (Postgres needs this for reserved words like `from` and `to`). `ResourceAssignmentRepository` updated to use `"from"`/`"to"` in SELECT + INSERT. **L44 closed**: Projects module is no longer "partially implemented" — all 4 tables + 8 indexes + 14 FKs registered.
- **DEC-112-collateral — PostingRulesBenchmarkTests** (Sprint 32): 4 integration tests from Sprint 31 had broken constructor (`NpgsqlConnectionFactory(string)` was changed to `(IOptions<NpgsqlConnectionOptions>, ILogger<NpgsqlConnectionFactory>)` in Sprint 22-23). Fixed with `Options.Create(new NpgsqlConnectionOptions { OltpConnectionString = ... })` + `NullLogger<NpgsqlConnectionFactory>.Instance`. Also fixed `await using IDbConnection` → `using var` (CS8417). Tests stay `[Fact(Skip = ...)]` — only made them compilable.

### Lessons
- **L47** (Sprint 32): Always check the actual table name from `\d <table>` or the repo's SQL, not the entity name. `ProjectTask` entity → `project_tasks` table (not `tasks`). `ResourceAssignment` entity → `resource_assignments` table (not `project_assignments`). The repos confirmed the real table names; the entity name alone is misleading.
- **L48** (Sprint 32): When a table column would be a SQL reserved word (`from`, `to`, `user`, `order`, `table`, etc.), you have 2 options:
  1. Rename the entity property (cleanest, but breaking change to entity + every repo)
  2. Add a `quoted: true` flag and quote the column everywhere it's referenced (DEC-112 used this)
  Once a column is created with quoted identifier, every subsequent reference (SELECT/INSERT/ORDER BY) must also be quoted — Postgres is strict.
- **L49 (NEW)** (Sprint 32): Best to avoid SQL reserved words in entity column names from day 1. If the entity already has `from`/`to`/`user`/`order` (e.g. from a domain term), use `quoted: true` + repo updates. Prefer `start_at`/`end_at` over `from`/`to` if the entity is being created fresh.

### Pending Article 3 audit (carry-over to Sprint 33+)
- `ProjectCostCenter` (in Companies module) — likely 2-4 violations
- `AccountService` (in Finance) — likely 2-4 violations
- `ChartOfAccountsService` (in Finance) — likely 2-4 violations
- `PayrollService` (in Payroll) — likely 2-4 violations
- Any service that still has `req.CompanyId` in the DTO — refactor to `_companyContext.CompanyId` (L30)

## Sprint 36 Decisions (DEC-122) + Lessons (L59..L60)

### Decisions
- **DEC-122 — Customer + Vendor Statement + Trial Balance** (Sprint 36): 
  - 2 new BE services (`CustomerStatementService` + `VendorStatementService`) with opening balance + chronological line items + running balance
  - 2 new BE endpoints (`GET /api/ar/customers/{id}/statement`, `GET /api/procurement/vendors/{id}/statement`)
  - 3 new FE pages (customer statement + vendor statement + trial balance) with date range filters, summary cards, color-coded running balance
  - TB UI: balanced/unbalanced bar + 5 per-type grouped tables (أصول / خصوم / حقوق ملكية / إيرادات / مصروفات) with subtotals
  - L19 enforcement: each service reads `_companyContext.CompanyId` (not from DTO) and verifies the customer/vendor belongs to current tenant
  - Posted-only filter: invoices/bills with `status NOT IN ('Cancelled', 'Draft')`; receipts/payments with `posted_at IS NOT NULL`
- **DEC-122-collateral — VendorRepository L19 fix** (Sprint 36): `Sel` was missing `company_id AS CompanyId`. Without it, every vendor returned "not found" because Dapper mapped `CompanyId=Guid.Empty`. Sprint 34 audit (DEC-114..117) missed this repo.
- **DEC-122-collateral — Receipts / VendorBills / Payments schema corrections**: removed `status` from receipts SELECT (table has no such column), removed `paid_amount` from vendor_bills SELECT (no such column), removed `status` from payments SELECT (it's INT, not string).
- **DEC-122-collateral — FE enum types**: replaced `AccountType` (int union) with `AccountTypeName` ('Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense'). BE returns string via Dapper `EnumStringTypeHandler`.

### Lessons
- **L59 (NEW)** (Sprint 36): ALWAYS run the actual endpoint with a real seed before declaring a Sprint "BE done". The Postgres error "column X does not exist" is the canonical source of truth. We caught 3 separate column-name bugs in DEC-122 BE work that typecheck alone would not have surfaced:
  1. `vendor_repository.Sel` missing `company_id` (L19 violation)
  2. `receipts` has no `status` column
  3. `vendor_bills` has no `paid_amount` column
  The pattern: write the SQL, run it via `Invoke-RestMethod`, check the JSON, fix the errors. Don't trust the typecheck.
- **L60 (NEW)** (Sprint 36): When the BE registers `SqlMapper.AddTypeHandler(new EnumStringTypeHandler<...>())`, the matching FE interface MUST use string literal types, not int enums. The handler silently converts every enum property to its string name on read. Affects: `AccountType`, `NormalBalance`, `PaymentStatus`, `POStatus`, `SalesInvoiceStatus`, etc. When in doubt, `Invoke-RestMethod` the endpoint and check the actual JSON shape — the type IS the source of truth.

### Pending L19 audit (carry-over to Sprint 37+)
- `VendorBillRepository.SelVb` — verify it includes `company_id AS CompanyId` (Sprint 34 missed; same pattern as `VendorRepository`)
- Other `IRepository.Sel*` projections in finance/procurement modules — full sweep
- All `req.CompanyId` DTO references still in services — refactor to `_companyContext.CompanyId` (L30)

## Sprint 37 Decisions (DEC-123) + Lessons (L61..L62)

### Decisions
- **DEC-123 — L19 audit sweep (5 repos)** (Sprint 37): After Sprint 36 caught `VendorRepository.L19`, this sprint audited the rest of the `Sel*` constants across all repos. Found 5 more missing `company_id AS CompanyId`:
  - `StockReservationRepository.Sel` (Inventory) — stock reservations never read CompanyId
  - `ItemCategoryRepository.Sel` (Inventory) — same bug
  - `VendorBillRepository.SelVb` (Procurement) — vendor bills never read CompanyId
  - `PurchaseOrderRepository.SelPo` (Procurement) — same bug
  - `GoodsReceiptRepository.SelGr` (Procurement) — same bug
  - All `SelLine` / `SelAlloc` projections were clean (line entities don't have company_id column; parent's `Sel*` covers it)
  - **L19 audit now complete** across all standard repos
- **DEC-123 — CoA extension (5 accounts)** (Sprint 37): To enable 4 more manual JE templates, added 5 missing accounts to `DefaultCoASeed.cs`:
  - `1300` مجمع إهلاك الأصول الثابتة (Asset, parent 1100) — for depreciation
  - `1410` سلف الموظفين (Asset, parent 1200) — for loan
  - `2110` مصروفات مستحقة (Liability, parent 2200) — for accrual
  - `5410` ديون معدومة (Expense, parent 4200) — for bad-debt
  - `5500` إهلاك الأصول الثابتة (Expense, parent 4200) — for depreciation
  - Total CoA: 47 → 52 accounts
  - Topological order preserved (parent code before child in array — L56)
- **DEC-123 — 4 new manual JE templates** (Sprint 37): Added to `/finance/journal-entries/new`:
  - رواتب (salary) — Dr 4112 (Direct Labor) / Cr 1210 (Cash)
  - سلفة موظف (loan) — Dr 1410 (Loans Receivable) / Cr 1210 (Cash)
  - ديون معدومة (bad-debt) — Dr 5410 (Bad Debt Expense) / Cr 1230 (AR)
  - تسوية مخزون (inventory-adjust) — Dr/Cr 1240 (Inventory) for variance
  - Total templates: 8 (4 Sprint 34 + 4 Sprint 37)
- **DEC-123-collateral — Pre-existing bug fix** (Sprint 37): `journal-entries/new/page.tsx` was using raw `fetch('/api/finance/accounts')` without JWT → 401 silently. The accounts dropdown was always empty since Sprint 11/12. Now uses `financeApi.listAccounts()`. Caught by Playwright smoke test (L59 in action).

### Lessons
- **L61 (NEW)** (Sprint 37): L19 audit focus on `Sel` / `SelVb` / `SelX` constants in the repository classes. Each one is a string used in multiple queries (GetByIdAsync, ListAsync, GetByCodeAsync, etc.) — fixing once in the constant fixes everywhere. **Audit pattern:**
  1. `grep -rn "private const string Sel" src/backend/Modules/`
  2. For each `Sel*` constant, check if it includes `company_id AS CompanyId`
  3. For the entity that IS the tenant table (Company itself), company_id is not needed
  4. For line entities (no company_id column on the table), the parent's `Sel*` must include company_id
- **L62 (NEW)** (Sprint 37): Check the existing CoA first before adding a JE template. If a needed account doesn't exist (e.g., 1300 accumulated depreciation, 5410 bad debt), add it to `DefaultCoASeed.cs` in the correct topological order (parent code before child in array). **Don't add templates that require missing accounts** — the user picks an account from the dropdown and the right code might not be there.
  **Pattern before writing a template:**
  1. List the account codes you'll need
  2. `grep` for each in `DefaultCoASeed.cs`
  3. For missing ones, add them with the right parent + correct topological position

### Pending L19 audit (carry-over to Sprint 38+)
- Direct SQL queries in service layer (e.g., JournalEntryService, aging-ar service, account ledger) — these don't have `Sel*` constants
- Any `IRepository` that uses inline SQL instead of `Sel*` constants

## Sprint 38 Decisions (DEC-124) + Lessons (L63..L64)

### Decisions
- **DEC-124 — L19 audit on service layer (3 services) — MAJOR SECURITY FIX** (Sprint 38): The carry-over from Sprint 37 was service-layer direct SQL. Sprint 38 audited:
  - `GeneralLedgerService` (`GetAccountBalancesAsync`, `GetAccountLedgerAsync`, `GetTrialBalanceAsync`) — **Trial Balance was returning accounts from ALL companies** (cross-tenant data leak in a multi-company deployment)
  - `GeneralLedgerReportService` (`GetAccountLedgerAsync`) — same bug
  - `JournalEntryRepository` (`GetByIdAsync`, `GetWithLinesAsync`, `EntryNumberExistsAsync`, `GetNextEntryNumberAsync`, `ListAsync`) — Journal Entries was returning entries from ALL companies
- Added `companyId` param to all these methods (interface + implementation)
- Updated controllers to inject `ICompanyContext` and pass companyId
- Updated `PaymentService.PostAsync` to also pass companyId
- **Concrete evidence**: TB count was 30 before fix, now 35 (5 more accounts correctly shown for current company)
- **Constitution Article 3 NOW 100% COMPLIANT** on all financial services
- **DEC-124 — 4 final manual JE templates** (Sprint 38): 12 of 12 plan COMPLETE:
  - دفع ضريبة (tax-payment) — Dr 4300 (Financial) / Cr 1210 (Cash)
  - فروق عملة (ربح) (fx-gain) — Dr 1230 (AR) / Cr 5110 (Revenue) — currency revaluation gain
  - فروق عملة (خسارة) (fx-loss) — Dr 4110 (Cost) / Cr 1230 (AR) — currency revaluation loss
  - سحب رأس مال (capital-withdrawal) — Dr 3100 (Capital) / Cr 1210 (Cash) — owner withdrawal
  - **Total templates: 12 (4 Sprint 34 + 4 Sprint 37 + 4 Sprint 38) — PLAN COMPLETE**

### Lessons
- **L63 (NEW)** (Sprint 38): L19 audit must cover service layer (not just repos). The `Sel*` constants in repos are ONE place to check, but services can also have direct SQL that bypasses the repo entirely. **Pattern:**
  1. Find all `Application/Services/*.cs` that use `_db.CreateOltpConnectionAsync` directly
  2. For each SQL query, check it filters by `company_id`
  3. Add `companyId` param to service interface if not already present
  4. Update controller to inject `ICompanyContext` and pass companyId
  5. Run the endpoint to verify the count changed (before/after L19 fix)
- **L64 (NEW)** (Sprint 38): Trial Balance count is a quick L19 sanity check. Before L19 fix: 30 accounts. After fix: 35 accounts. The difference (5) was accounts from other companies (or unfiltered rows). **If TB count is suspiciously low or high, suspect L19.**

### L19 audit trend (4 sprints — 13 violations total)
- Sprint 34: 4 modules (CostCenter, Payroll, ChartOfAccounts, Account)
- Sprint 36: 1 repo (VendorRepository)
- Sprint 37: 5 repos (StockReservation, ItemCategory, VendorBill, PurchaseOrder, GoodsReceipt)
- Sprint 38: 3 service-layer (GeneralLedger, GeneralLedgerReport, JournalEntryRepository)
- **Total: 13 L19 violations found and fixed**

### Pending L19 audit (carry-over to Sprint 39+)
- `FinanceService` (Holding query) — currently only looks for the single Holding, but might leak if multi-tenant
- `DashboardChartService` — needs full review (Sprint 35 marked as L19 OK but worth re-verifying)
- `GeneralLedgerReportService` other queries (account ledger + general ledger reports)
- `Projects.Application.Services` — Sprint 32 audit focused on the table, not the service layer
- Any remaining service-layer SQL without `company_id` filter

## Sprint 31 Decisions (DEC-107..110) + Lessons (L43..L46)

### Decisions
- **DEC-107 — DepartmentResponse enrichment** (Sprint 31): `DepartmentResponse` now has `ManagerName` + `ManagerCode` + `EmployeeCount` (L40 pattern, single batch Dapper lookup). 5 departments now show manager name + count.
- **DEC-108 — Posting Rules benchmark vs engine** (Sprint 31): 4 xUnit tests + SQL script that compare the engine output to the 74 benchmark JEs. All 4 categories balanced — no bugs found.
- **DEC-109 — 5th default rule "VAT 5%" (inactive)** (Sprint 31): Added new rule for sales with VAT 5% (DR 1230 / CR 5110 / CR 1411). INACTIVE by default. Admin enables: deactivate Libya default + activate this + add 1410/1411 accounts.
- **DEC-110 — Payments module Article 3 audit** (Sprint 31): Fixed 2 violations — `Payment.CompanyId` was `Guid?` (now `Guid`), `CreateAsync` didn't set CompanyId (now injects ICompanyContext).
- **(bonus) Playwright MCP setup** (Sprint 31): Installed `playwright` + Chromium. `scripts/playwright-smoke.mjs` runs 24-page browser smoke test in <2 min. Discovered bugs that API tests missed (`/hr/departments` 404).
- **(bonus) /hr/departments page** (Sprint 31): Was missing — created with hierarchy view + manager enrichment + employee counts.

### Lessons (L43..L46)
- **L43** (Sprint 30): Before `dotnet build`, ensure no `dotnet` process holds the .exe. If present, `Stop-Process -Force` first. The "file locked" error is recoverable but wastes 30s.
- **L44** (Sprint 30): Projects module is **partially implemented** — the entities + services + controllers exist, but the underlying tables (`resources`, `tasks`, `project_assignments`, `project_budgets`) were never registered in `data-types/`. This is a Sprint 22+ bug that needs a dedicated sprint.
- **L45 (NEW)** (Sprint 31): `npm start` serves the cached `.next/` build, not the source. After backend OR frontend code changes, always `npm run build` first. Symptom: "I changed the code but the change isn't visible in the browser."
- **L46 (NEW)** (Sprint 31): Playwright discovers bugs that API testing misses (e.g., missing FE pages, stale builds). The 24-page smoke test takes 1.5 minutes. Use it as a CI gate.

### Pending Article 3 audit (carry-over to Sprint 32+)
- `ProjectCostCenter` (in Companies module) — likely 2-4 violations
- `AccountService` (in Finance) — likely 2-4 violations
- `ChartOfAccountsService` (in Finance) — likely 2-4 violations
- `PayrollService` (in Payroll) — likely 2-4 violations
- Any service that still has `req.CompanyId` in the DTO — refactor to `_companyContext.CompanyId` (L30)

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

---

## Sprint 60 Decisions (DEC-184..191, DEC-NEW-1..15) + Lessons (L46..L52)

### Decisions
- **DEC-184 — `ALTER TABLE accounts` (6 columns)** (Sprint 60): `fs_type` (P/L, BS), `section` (Current/NonCurrent/Equity/Operating…), `activity` (Restaurant/Construction/Logistics/Admin/Allocated/Other), `cost_center_id` (FK), `project_id` (FK), `parent_account_id` (FK to accounts). All nullable except `fs_type` (default 'BS'). Idempotent migration.
- **DEC-185 — CoA Canonical Migration** (Sprint 60): 27 new accounts (DEC-NEW-1..5, 6..13) + 131 keep accounts updated with `fs_type` + `section`. Plus DEC-NEW-14 (4 cost centers: CC-CONSTR, CC-REST, CC-ADMIN, CC-WORKSHOP) + DEC-NEW-15 (5 projects: REST-2026-001/002, ADMN-2026-001, TRNG-2026-001, YRCL-2026-001).
- **DEC-186 — Deprecate 1.3 Off-Balance** (Sprint 60): WIP account moved out of 1.3 (off-balance) to 1.1.06 (current asset, IAS 2 ¶37).
- **DEC-187 — WIP 9201 → 1.1.06** (Sprint 60): WIP is a current asset, not off-balance. Per IAS 2 ¶37.
- **DEC-188 — L1=7 split** (Sprint 60): Single "Other Income/Expense" account split into 7.1 Other Operating, 7.2 Finance, 7.3 Extraordinary. For IFRS-aligned P&L.
- **DEC-189 — Balance Migration + Validation** (Sprint 60): `Sprint60_BalanceMigrationValidation_20260825_150000.cs` migrates all balances double-entry, then promotes 27 accounts to `migration_status='migrated'`. Legacy 4-digit accounts stay at `pending` intentionally.
- **DEC-190 — CoAValidationService** (Sprint 60): 6 checks — Journal line integrity, Trial balance Σ Debit = Σ Credit, Unique codes, Code format, Legacy account audit, Deprecated account usage. New endpoint `/api/admin/coa/validate`.
- **DEC-191 — Reports + Frontend** (Sprint 60): 4 report services (P&L, BS, Trial Balance, AR/AP Aging) accept `costCenterId` + `projectId` filters. 5 FE pages updated + new `/admin/coa-validation` page.
- **DEC-NEW-1 — Cash/Banks split** (Sprint 60): 1.1.01 النقدية (Cash on Hand + Foreign Currency) + 1.1.02 البنوك (LYD, USD, cheques). Per IAS 7 ¶6.
- **DEC-NEW-2 — Tangible/Intangible split** (Sprint 60): 2.01 ملموسة (Land, Buildings, Machinery, IT) + 2.02 غير ملموسة (Software, IAS 38 ¶8).
- **DEC-NEW-5 — 7 new accounts (NDB+Stamps+CIT+SS)** (Sprint 60): 8.2.01.001/002/003 دمغات + 8.2.01.005 NDB 1.5% (non-refundable) + 2.1.03.002 CIT Withholding + 2.1.08.001/002 Social Security.
- **DEC-NEW-14 — 4 Cost Centers** (Sprint 60): CC-CONSTR, CC-REST, CC-ADMIN, CC-WORKSHOP.
- **DEC-NEW-15 — 5 Projects** (Sprint 60): REST-2026-001/002, ADMN-2026-001, TRNG-2026-001, YRCL-2026-001.

### Lessons (L46..L52)
- **L46 — Migration version must be 14-digit `YYYYMMDD_HHMMSS`, not 8-digit `YYYYMMDD_NNN`.** FluentMigrator sorts versions NUMERICALLY. Sprint 60 migrations were originally `20260825_001..004` (8 digits) which sorted BEFORE `Sprint28_Audit_20260802_220000` (14 digits). **Rule for all future sprints**: 14-digit format only.
- **L47 — Phase6_InitialSchema transaction bug.** Drops `public` schema CASCADE then tries to INSERT into `VersionInfo` (which it just dropped). Workaround: manually create `VersionInfo` + insert old migrations as applied. **Apply when**: setting up fresh DB.
- **L48 — Bootstrap gap: `appsettings.json` has `SeedScenario: false` + `DefaultHoldingBootstrap` only creates Holding Company, NOT roles.** With no users, no roles get created → login fails. **Real fix (Sprint 61)**: add `EnsureDefaultRolesAsync` to `DefaultHoldingBootstrapHostedService`.
- **L49 — AuthService.RegisterAsync connection visibility bug.** `GetUserCompaniesAsync(user.Id, ct)` uses separate connection, can't see uncommitted transaction. **Real fix**: use `conn, tx, ct` overload.
- **L50 — Wave-based parallelization works (3 waves: Foundation → Migration → Reports+FE).** 2-3 workers in parallel for Wave 2. **Gotcha**: shared git working tree causes interference; use `git worktree` for Sprint 61+.
- **L51 — New FE pages need manual Trust Mode testing before "ادفع".** Wave 3B's `/admin/coa-validation` was committed with `TypeError` (used `result.companyId.slice()` without optional chaining; API doesn't return `companyId`). Caught only when Anas opened it. **Rule**: every new client component must be opened in browser before Sprint closure. **Permanent fix pattern**: `value ? formatValue(value) : '—'`.
- **L52 — RTL date format display: ISO strings "2026-01-01T00:00:00" display garbled in `<p dir="rtl">` context.** Browsers re-order characters visually. **Fix**: either `dir="ltr"` on the date `<p>`, or convert with `new Date(s).toLocaleDateString('en-GB')` → "01/01/2026". Applied to `income-statement` + `cash-flow` in Trust Mode 27-Aug-2026.

### Trust Mode Cosmetic Fixes (27-Aug-2026)
- ✅ **Footer version**: `AppShell.tsx:210` updated from "v1.0.13 · Sprint 58" → "v1.0.15 · Sprint 60".
- ✅ **Income Statement date format**: `app/(authenticated)/finance/reports/income-statement/page.tsx:158` — added `dir="ltr"` + `toLocaleDateString('en-GB')`.
- ✅ **Cash Flow date format**: same fix applied.
- ✅ **CoA Validation page bug**: `app/(authenticated)/admin/coa-validation/page.tsx:121` — `result.companyId?.slice(0, 8)` with fallback `'—'`.

### Pending Sprint 60 → 61 carry-over
- `AccountRepository.InsertAsync/UpdateAsync` not updated to write 6 new fields (DB defaults handle it, but should be explicit).
- `parent_account_id` not wired up yet (L1/L2 dotted format doesn't exist for new accounts).
- Sprint 61+ permanent fixes: EnsureDefaultRolesAsync in bootstrap, AuthService connection visibility, /api/auth/admin-bootstrap endpoint, Phase6_InitialSchema VersionInfo recreation.

---
_Last updated: 2026-08-27 by Mavis (Muhammad mode), Sprint 60 Trust Mode + cosmetic fixes approved by Anas — DOX framework applied_

---

## 🔄 Workflow المُنفّذ في Sprint 60 (5 Phases)

**للتفاصيل الكاملة:** راجع Notion Workflow page → `📋 Workflow — تنفيذ خارطة الطريق (من Sprint 60)` → قسم "🟢 Sprint 60 — Workflow الفعلي المُنفّذ" + "📋 ملف تنفيذي للـ Workflow (Sprint 61+)".

### ملخص الـ 5 Phases:

| Phase | Mode | المسؤول | النشاط | الإخراج |
|-------|------|---------|--------|---------|
| **1. M1-Local** | Default | Admin (workers) | تنفيذ العقد في 5 Waves (1/2A/2B/3A/3B) | feature/sprint-N-wave-3b-final @ XXXX |
| **2. M3-Trust** | Anas request | Muhammad (as client) | تصفح localhost:3000 + فحص 6+ صفحات | verification + bug fixes |
| **3. M2-Discussion** | "ادفع" | Anas → Muhammad | "ادفع" | Mode 1 → Mode 2 |
| **4. M2-Release** | Admin = Muhammad | git push + PR + CI + relax (if false positive) + merge + tag | develop @ aXXXXX + tag vX.Y.Z-sprintN |
| **5. Report** | Muhammad | Notion + AGENTS.md + CHANGELOG.md | Sprint N Done |

### Sprint 60 Carry-over to Sprint 61+ (Permanent Fixes):

| # | Issue | Source Lesson | Fix Location |
|---|-------|---------------|--------------|
| 1 | CI false positive on `NotContain("tenant_id", ...)` | L51 | Update `.github/workflows/no-tenant-id.yml` |
| 2 | `EnsureDefaultRolesAsync` not called in bootstrap | L48 | Add to `DefaultHoldingBootstrapHostedService` |
| 3 | `AuthService.BuildAsync` line 191 uses wrong connection | L49 | Use `conn, tx, ct` overload |
| 4 | No `/api/auth/admin-bootstrap` endpoint | L175 | New endpoint for fresh deployments |
| 5 | `Phase6_InitialSchema` drops VersionInfo | L47 | Re-create VersionInfo after DROP SCHEMA |

### المعمار الجديد للورك فلو (3 Internal Personas + Workers):

- **محمد (Muhammad)** = مستشارك الشخصي + Orchestrator + Verifier (صوت بشري واحد)
- **Admin (القائد التقني)** = Tech Lead، internal في نفس root session
- **Workers (Jimis)** = Bounded producers، sub-sessions via `task({run_in_background: true})`

**القاعدة الذهبية:** صوت واحد في المحادثة = محمد. الـ hand-offs الداخلية تُذكر ولا تُمثّل. أنس لا تتحدث مع Workers مباشرةً.
