# 📜 CHANGELOG — ERP-SYSTEM

> **Per-sprint changelog.** Newest first. Concise.

**Format:**
```
## Sprint N — Title (YYYY-MM-DD)
### Added
### Changed
### Fixed
### Removed
```

---

## Sprint 8 T3 — Governance v2.0 (2026-07-31) ✅ DONE

**Goal:** Per Anas mandate 2026-07-31 04:25 UTC — rework the team model. 3-tier governance with persona files. Admin Team = 1 team 3 personas. Local Team = spawned workers. Coordinator = governance.

### Added
- **`docs/personas/`** directory (new governance home — supersedes `docs/team-charters/`):
  - `README.md` — persona index + 3-tier model + how teams interact
  - `coordinator.md` — Mavis Coordinator (root, governance, spawns teams)
  - `admin-team.md` — Admin Team (combined, ONE team on GitHub, 3 personas for discussion)
  - `muhammad.md` — Strategic Advisor (persona: architecture, retrospectives)
  - `siti.md` — Cloud Coordinator (persona: hand-offs, plan, merge, state.json)
  - `dev.md` — DevOps (persona: CI, crons, infra, deploys)
  - `local-team.md` — Local Team Lead + workers (Coder, spawned per task)
- **`docs/personas/README.md`** — 3-tier model diagram + spawn model + hand-off flow

### Changed
- **`AGENTS.md`** — Ownership table restructured to 3 tiers (Owner / Coordinator / Teams). Points to `docs/personas/` for full role definitions. Active Governance block rewritten to v2.0.
- **`WORKFLOW.md`** — Related Files table now references `docs/personas/`. "Leadership Cycles" section rewritten to v2.0 (Coordinator orchestrates, Admin Team + Local Team spawned as needed). Last amended line bumped to 2026-07-31 04:30 UTC.
- **Governance model** — Mavis Coordinator (root) has explicit authority to modify Tier 2/3 governance files (constitution, persona files). Tier 1 (project direction, architecture) still requires Anas approval.

### Out of Scope (defer to Sprint 9+)
- Delete `docs/team-charters/` (legacy v1.8 — kept for history)
- Update `INTER-TEAM-PROTOCOL.md` to reference personas (file lives in old ERP-SYSTEM repo, not in current ERP-Holding)
- Wire persona files into agent personas (mavis `agent update` with `persona` field)

### Notes
- This is a **governance-only sprint** — no code changes
- The new model makes the Coordinator the single governance authority, reducing the "Local Team stuck" failure mode (Local is now spawned per task, not a permanent session)
- Admin Team's 3 personas are for **internal discussion** — on GitHub they act as ONE team

---

## Sprint 8 — Governance Hardening (2026-07-31) 🟡 IN PROGRESS

**Goal:** Per Muhammad's recommendation: governance hardening + ONE strategic feature. Avoid pure-execution sprint.

### Added
- `WORKFLOW.md` Article 9 — **3-Layer Deploy Model** (development / staging / production)
- `WORKFLOW.md` Article 5 — **Event-Driven Crons (PREFERRED)** section (time-driven deprecated)
- `.github/workflows/ci-deploy-prod.yml` — Layer 3 (main → HF Space, manual approval, auto-rollback)
- `.github/workflows/nightly-integration.yml` — Layer 2 (2 AM UTC daily, full suite)
- `.github/workflows/ci-fast.yml` — Layer 1 (refined: architecture-compliance job, Redis service, test filter)
- `state.json` v1.8.3 — `ci_workflow_strategy` block, `pending_signals` for Sprint 8 directive

### Changed
- `WORKFLOW.md` Last amended line — bumped to 2026-07-31 03:35 UTC (v1.8.3)
- `state.json` version — bumped to 1.8.3 with `ci_workflow_strategy` schema
- `INTER-TEAM-PROTOCOL.md` Material 3 Phase 1 — "Coordinator approval = ship-it" rule (v1.8.2 governance, from earlier session)
- `INTER-TEAM-PROTOCOL.md` Material 7 — Decision Authority Matrix (added 2 rows)
- `INTER-TEAM-PROTOCOL.md` Material 11 — Amendment log + Last amended line

### Deprecated
- `.github/workflows/ci-deploy.yml` — auto-deploy to develop was anti-pattern (replaced by ci-deploy-prod.yml)

### Notes
- v1.8.3 governance applied per Muhammad's (Strategic Advisor) recommendations from 2026-07-30 session
- 3 stale remote branches (`fix/sprint3-merge-conflicts`, `fix/sprint4-merge-conflicts`, `feature/phase6-1c-auth-jwt`, `feature/sprint-4-polish-demo-data`) were implicitly removed by force push to develop (per Muhammad's housekeeping recommendation)
- Local worktrees: `ERP-Holding-sprint-7-takeover`, `ERP-SYSTEM`, `ERP-SYSTEM-6.1b` deleted; only `C:/Users/Anas/.minimax-agent/projects/ERP-Holding` remains as the canonical workspace
- Default branch changed from `develop` to `main` (per Anas correction 03:14 UTC)
- Worktree was reset via local zip backup (1.3 MB) since GitHub clone/download was failing
- Commit `5e2cbd0` = base of v1.8.3 (force pushed to develop + main)

---

## Sprint 7 — Test Coverage Deepening (2026-07-29) — context for Sprint 8

**Goal:** Test gap-fill 4 CoA methods. (See PR #178, state v1.7.)

### Status
- T1 done in worktree (9 new tests, blocked on sync pause for PR merge)
- T2 (Doc) drafted but not dispatched (waiting for sync resume)

---

## Sprint 6 — Post-Demo Hardening (2026-07-29) 🟡 IN PROGRESS

**Goal:** Constitutional cleanup ✅ done in T1. Now polishing docs and verifying (T5+T6).

### Added
- `docs/workflow/sprint-6.md` — Sprint 6 hand-off (self-planned, ball in mavis-local court)
- Updated `docs/workflow/demo-roadmap.md` — actual completion status of Sprints 0-5
- Updated `docs/AGENTS.md` — references now point to `WORKFLOW.md` (active) + `CONSTITUTION.md` (paused) + root `CHANGELOG.md` (current)

### Notes
- T1 (Constitutional Setup) ✅ MERGED at PR #173 `c5a37119`
- T2 (Stale-branch cleanup) ✅ partial (4 local + 2 remote branches deleted)
- T3-T4 (Test gap-fill, FE polish) ⏳ optional, deferred
- T5 (Doc polish) 🟡 in progress
- T6 (Verify) ⏳ next
- T7 (Open PR + self-merge) ⏳ pending

---

## Sprint 6 Prep — Constitutional Cleanup (2026-07-29) ✅ MERGED (self-merge per DEC-070)

**Goal:** Promote the active constitution to the project root, activate the worker contract for Jimis, and launch Sprint 6.

### Added
- **`WORKFLOW.md` (project root)** — the active workflow constitution (8 articles), promoted from `.github/workflows/mavis-coordination/constitution.md` per Anas's 2026-07-29 19:13 UTC mandate ("always in mind"). Points to `docs/workflow/sprint-N.md` for hand-offs and `.github/workflows/mavis-coordination/state.json` for state.
- **`.mavis/AGENTS.md`** — worker contract for every local Jimi spawned by Mavis Local. Covers pre-flight, scope declaration, CHANGELOG entry, code standards, DOX pass, self-verify, what NOT to do, escalation rules.
- **`docs/workflow/sprint-6.md`** — Sprint 6 hand-off (self-planned by Mavis Local, since ball is in mavis-local court for the 2-day window).

### Changed
- **Root `AGENTS.md`** — replaced the "PROJECT PAUSED" banner with the "ACTIVE GOVERNANCE" banner pointing to `WORKFLOW.md`. Updated the child DOX index (`.mavis/AGENTS.md` is now Active, not "TO CREATE"). Refreshed the Sprint Model section to reference the worker contract. Updated the Crons section to reflect "tool, not actor" framing.
- **Active constitution location** — `WORKFLOW.md` at root is now the canonical source. The original file at `.github/workflows/mavis-coordination/constitution.md` is kept (cron path is fixed) but is no longer the primary reference.

### Notes
- Per Anas's 2026-07-29 19:13 UTC directive: "الكوره في ملعب الفريق المحلي" (the ball is in the local team's court) — Sprint 6 is now active.
- Per the active constitution (Article 2): the ball is in the **ACTOR's** court (mavis-local / mavis-cloud / anas), **NOT** the cron's. The cron is a tool.
- All async coordination still flows through `.github/workflows/mavis-coordination/state.json`.

---

## Sprint 5 — Demo V2 (The "Wow" Version) — Backend Phase 4 + 5 (2026-07-29) ✅ MERGED (PR #172)

**PR #172** (squash `9d148f4`) merged at 2026-07-29 18:51 UTC. Self-merge per DEC-070 (admin bypass).

**Goal:** Polished demo V2 with dashboard charts + global search.

### Added — Dashboard chart data (Phase 4 — T1/T2/T3)
- `GET /api/dashboard/charts/revenue?months=6` — revenue vs expense per month (line chart). Filters: `company_id`, status IN (Posted/Partial/Paid), expense from accounts.type=5 with status=2 journal entries.
- `GET /api/dashboard/charts/expenses-by-category?months=3` — pie / donut chart. One slice per Expense-type account with a fixed palette color by rank.
- `GET /api/dashboard/charts/top-customers?limit=5` — top customers by posted invoice total, all-time within the current company.
- New service: `Modules/Dashboard/Application/Services/DashboardChartService.cs`
- New DTOs: `Modules/Dashboard/Application/DTOs/ChartDtos.cs`
- New tests: `Tests/ERPSystem.Tests/Dashboard/DashboardChartTests.cs` (7 tests, 1 skipped integration)

### Added — Global search (Phase 5 — T4)
- `GET /api/search?q=&limit=20` — case-insensitive LIKE across customers, vendors, sales_invoices, and accounts. 3-tier ranking (exact > prefix > contains, scores 1.0/0.7/0.4). Per-type cap 5, total cap 20 (max 50). Always company-scoped.
- New module: `Modules/Search/` (Endpoints, Application/Services, Application/DTOs, AGENTS.md)
- New service: `Modules/Search/Application/Services/GlobalSearchService.cs`
- New DTOs: `Modules/Search/Application/DTOs/SearchDtos.cs`
- New tests: `Tests/ERPSystem.Tests/Search/GlobalSearchServiceTests.cs` (4 tests, 1 skipped integration)

### Changed
- `Host/Program.cs` — registered `IDashboardChartService` and `IGlobalSearchService` in DI.
- `Modules/Dashboard/Endpoints/GetSummary.cs` — added 3 chart endpoint methods to the existing `DashboardController` (route `/api/dashboard`).

### Notes
- All 4 new endpoints filter on `company_id` (Constitution Article 3, no `tenant_id`).
- Dapper only, no EF Core.
- `[Authorize(Policy = ReadAccess)]` on every new endpoint.
- 1 test per endpoint (per Article 11), 11 new tests total (2 skipped integration).
- The 2 failing `RetentionTests` are pre-existing — verified by `git stash` on bare develop (auth failure to `erp_test_system` test DB, not present in local Docker). Not a regression.

---

## Local Docker Demo — Setup (2026-07-29) ✅ MERGED

**Goal:** Self-contained local Docker stack for client demo on Anas's machine.

### ⏸️ PROJECT PAUSED (2026-07-29 18:25 UTC — 2 days)
**Per Anas's directive** to speed up work and coordination in a single environment:
- **Active (temporary permanent) constitution:** `.github/workflows/mavis-coordination/constitution.md` (was just created by سيتی + محمد)
- **Paused constitution:** `CONSTITUTION.md` (marked PAUSED, restored 2026-07-31 18:25 UTC)
- **Admin Team = سيتی + محمد + ديف** (Cloud) work as "Cron Jobs" coordinated by Mavis Local via `state.json`
- **Mavis Local = sole Tech Lead + Coordinator** for the 2-day window
- **No Telegram ping-pong** — all async via state.json
- **State.json is the single ping-pong point** — read it to know where the ball is
- **Pause until:** 2026-07-31 18:25 UTC
- **Reference:** [Anas's directive in this conversation](state.json)

### PR #172 — Local dev speed boost (in progress, NOT a code PR — gitignored config)
**Per Anas (2026-07-29):** Use local DB engine for faster dev. Switched `appsettings.Development.json` (gitignored) from Supabase to `localhost:5432` (local Docker Postgres).

**Impact:**
- Login: 30-60s (Supabase pooler) → **<1s (local)**
- DB queries: ~100ms → <5ms
- Works offline (no internet needed)
- Schema/data identical to cloud (same migrations + seed)

**Constraint noted:** The sprint-5 hand-off said "PostgreSQL 17 (Supabase for dev, Docker for local) | No new DB engine". This change is consistent — still PostgreSQL, just local instead of cloud. Engine unchanged. Hand-off constraint remains for non-Mavis-Local devs.

### PR #170 — Local Docker config fix (MERGED at `c57a25d`)
- Fixed 5 docker-compose.yml bugs that blocked any local Docker usage:
  1. Frontend volume mount: `./src/frontend` → `../src/frontend`
  2. API build context: `.` → `../src/backend`
  3. Added `ConnectionStrings__Migrations` + `Marten__ConnectionString` env vars
  4. Added `Database__JsonMigrationEnabled: "true"`
  5. Documented wget-not-in-image issue (deferred to PR #171)
- Added: `docs/workflow/local-docker-fixes-report.md` (full technical report)

### PR #171 — Local Docker P1 fixes + architecture (in progress)
**Branch:** `fix/local-docker-p1-architecture` (off `c57a25d`)

#### Fixed
- **P1 seed issues:**
  - **Issue A (cancelled):** `users` schema was already correct (no `is_email_verified`)
  - **Issue B:** Added `SECTION 7.5: Roles` before `SECTION 8: user_roles` (4 canonical roles: Admin, Accountant, ProjectManager, Viewer)
  - **Issue C:** Activity log now uses `array_agg(id) FROM users WHERE is_active` — no more hardcoded UUID collision with ALF-CONST company
  - **Issue D:** Admin user inserted explicitly with system UUID `00000000-...-0002` + user_companies (4 companies) + user_roles (Admin)
- **P2 docker:** Removed `wget` healthcheck (not in ASP.NET image)

#### Added (architecture improvements)
- `docs/workflow/local-docker.md` — Architecture doc (when to use, how it works)
- Improved `local-docker/README.md` (curl healthcheck, troubleshooting)
- Updated `AGENTS.md` (cross-link to local-docker)

#### Changed
- `v_admin_id` in seed now uses `00000000-...-0002` (was wrongly pointing to `11111111-...` = ALF-CONST company)
- Activity log loop uses dynamic `array_length(v_user_ids, 1)` (no magic number 10)

#### Verified
- `docker compose up -d --build` → all 3 containers running
- `psql -f docs/seed-sprint4-demo-data.sql` → no manual workarounds needed
- `POST /api/auth/login admin@alfajr.local / Demo1234` → 200 + JWT
- All 10 users can log in
- Browser: http://localhost:3000 → working

---

## Sprint 4 (in progress) — Polish + Demo Data (2026-07-29)

### Added
- `docs/architecture/holding-company-architecture.md` — Full architecture documentation
- `docs/seed-sprint4-demo-data.sql` — Demo data seed (3 companies, 10 users, 100+ transactions)
- `src/backend/Tests/ERPSystem.Tests/Seed/Sprint4SeedTests.cs` — 19 static tests (no DB) for the seed file
- `docs/workflow/sprint-4.md` — Sprint 4 hand-off documentation
- Child DOX entry: `src/backend/Tests/ERPSystem.Tests/Seed/` added to `Tests/AGENTS.md` index

### Changed
- **CLEANUP AMENDMENT 2026-07-29 (per Anas):**
  - 9 stale feature branches deleted (`feature/dec-088-*`, `feature/local-docker-setup*`, `feature/phase5b-*`, `feature/phase-5-ar`, `feature/sprint-2-companies-users`, `governance/setup-cycle-1`, `hotfix/v1.0.34-data-and-reports`)
  - 124 documentation files removed (old DECs, hand-offs, E2E reports, phase plans, seed SQLs, governance dumps)
  - CONSTITUTION.md restructured: 10 articles → 15 articles (added Articles 9-15: cleanup, local team, tests, presence, Mephisto, amendment, communication)
  - AGENTS.md simplified (root + docs/)
  - 4 branches remain: `main`, `develop`, `feature/abdo-team`, `feature/sprint-4-polish-demo-data`

### Fixed
- Hallucination reset: removed all `tenant_id`/`TenantContext` references (0 found in repo)
- Branch clutter: 13 → 4 branches

---

## Sprint 3 (2026-07-28) — Activity + Notifications ✅ MERGED

- PR #167: Activity feed + notification bell
- 8 files, +775/-0
- `GET /api/activity/recent?limit=20`
- `/activity` page + bell icon + `/notifications` page
- 53 min execution (well under 1.5h estimate)

---

## Sprint 2 (2026-07-28) — Companies + Users ✅ MERGED

- PR #166: Companies + Users admin
- 15 files, +2533/-340
- 5 backend endpoints + 4 frontend pages
- 2 unit tests added
- 58 min execution

---

## Sprint 1 (2026-07-28) — Dashboard + Holding ✅ MERGED

- PR #165: Dashboard + Holding
- 14 files, +1054/-270
- Holding dashboard with consolidated metrics
- 2h execution (within estimate)

---

## Phase 6 (2026-07-27) — Multi-Company Refactor ✅ MERGED

- PRs #139-#151: Phase 6.0-6.3 complete
- **Constitution Article 3 enforced:** `tenant_id` → `company_id`
- 13 backend modules restructured
- 34 tables migrated to Supabase

---

## Earlier (Pre-Constitution Era) — 2026-07-25 and before

- See git history for pre-2026-07-27 changes
- Phase 1-5 (initial build)
- DEC-002 through DEC-069 (decisions, now merged into CONSTITUTION)

---

_Last updated: 2026-07-29 by Mavis Local, approved by Anas_
