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

## Local Docker Demo — Setup (2026-07-29) ✅ MERGED

**Goal:** Self-contained local Docker stack for client demo on Anas's machine.

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
