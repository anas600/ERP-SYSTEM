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

_Last updated: 2026-07-29 by Mavis (Muhammad mode), approved by Anas_
