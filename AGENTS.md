# 🤖 AGENTS.md — ERP-SYSTEM (Root DOX Rail)

> **This is the DOX rail.** All work in this repository must follow the DOX framework.
> Read this file fully + walk the chain to your target path before editing anything.

**Last updated:** 2026-07-29 19:15 UTC (Constitutional update per Anas mandate: WORKFLOW.md promoted to project root, .mavis/AGENTS.md activated, Sprint 6 launched)

> ## 📜 ACTIVE GOVERNANCE (per Anas, 2026-07-29 19:13 UTC)
>
> **Active workflow constitution (temporary permanent, 2 days):**
> **[`WORKFLOW.md`](./WORKFLOW.md)** — at the project root, always in mind.
>
> **Paused legacy constitution:** [CONSTITUTION.md](./CONSTITUTION.md) (marked PAUSED — restored 2026-07-31 18:25 UTC).
>
> **Sprint hand-offs:** [`docs/workflow/sprint-N.md`](./docs/workflow/) — سيتی writes them, Mavis Local executes.
>
> **State machine (the ping-pong point):** [`.github/workflows/mavis-coordination/state.json`](./.github/workflows/mavis-coordination/state.json) — the single source of truth for "where the ball is."
>
> **Worker (Jimi) instructions:** [`.mavis/AGENTS.md`](./.mavis/AGENTS.md) — every Jimi reads this before starting.
>
> **What's the same:** architectural constraints (company_id, Dapper, no EF Core, etc.), DOX framework, CHANGELOG discipline, branch protection.
>
> **What's different in the 2-day window:** Smart cron + state.json is the primary async signal. Admin Team (سيتی + محمد + ديف) work as "Cron Jobs" coordinated by Mavis Local. No Telegram ping-pong. Mavis Local is sole Tech Lead + Coordinator.
>
> **🚨 Critical (per Anas, 2026-07-29 18:50 UTC):** The ball is in the **ACTOR's** court (mavis-local / mavis-cloud / anas), **NOT** the cron's. The cron is a tool that helps Mavis Local stay updated.

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
- ✅ `CompanyContext`, `CompanyMiddleware`, `[CompanyAuthorize]`.
- ✅ `user_companies` join table.
- ✅ JWT carries `company_ids[]` + `X-Company-Id` header.
- ✅ Holding-level queries require `holding_admin` role.
- ⚠️ **MISLEADING FOLDER:** `src/backend/Shared/MultiTenancy/` contains `CompanyContext.cs` files. **Folder name should be renamed to `CompanyContext/`** in a future refactor (out of scope for current sprints).

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

### Sprint Model
1. **Cloud (Siti)** writes hand-off → `docs/workflow/sprint-N.md` (push to develop). For small tasks in the 2-day window, Mavis Local can self-plan.
2. **Mavis Local** pulls develop, spawns Jimis (BE + FE parallel). See [`.mavis/AGENTS.md`](./.mavis/AGENTS.md) for the worker contract.
3. **Jimis** execute, each one **declares their scope** in the nearest AGENTS.md (per worker contract) and **adds a CHANGELOG entry**.
4. **Mavis Local** verifies (T6: build + test + typecheck).
5. **Mavis Local** opens PR (`feature/sprint-N-*` → develop).
6. **Mavis Local** self-merges (per DEC-070 admin) or **Cloud** auto-merges when CI green.
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

CI runs 6 required checks on PR open. Admin bypass is ON (per Article 10).

---

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`/docs/AGENTS.md`](./docs/AGENTS.md) | Documentation directory | Active |
| [`/infra/AGENTS.md`](./infra/AGENTS.md) | Infrastructure-as-Code | Active |
| [`/infra/docker/AGENTS.md`](./infra/docker/AGENTS.md) | Docker configs | Active |
| [`/scripts/AGENTS.md`](./scripts/AGENTS.md) | Build/utility scripts | **TO CREATE** |
| [`/local-docker/AGENTS.md`](./local-docker/AGENTS.md) | Local dev environment (Layer 1) | **TO CREATE** |
| [`/mvp-docker/AGENTS.md`](./mvp-docker/AGENTS.md) | Containerized MVP (Layer 2) — Sprint 13 | **TO CREATE** |
| [`/src/AGENTS.md`](./src/AGENTS.md) | Source code root | Active |
| [`/src/backend/AGENTS.md`](./src/backend/AGENTS.md) | Backend (.NET) | Active |
| [`/src/frontend/AGENTS.md`](./src/frontend/AGENTS.md) | Frontend (Next.js) | Active |
| [`/.github/AGENTS.md`](./.github/AGENTS.md) | GitHub workflows | Active |
| [`/.mavis/AGENTS.md`](./.mavis/AGENTS.md) | Mavis orchestration (worker instructions for Jimis) | Active |

**Note:** `src/backend/Modules/<module>/` and `src/frontend/app/<route>/` have their own AGENTS.md (created when modules become durable boundaries).

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode), approved by Anas — DOX framework applied_
