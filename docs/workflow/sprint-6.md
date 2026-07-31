# 🚀 Sprint 6: Post-Demo Hardening & Polish

**Date:** 2026-07-29
**Architect:** Mavis Local (self-planned, per the 2-day window rule — ball is in mavis-local court)
**Implementer:** Mavis Local (Tech Lead + Coordinator) + Jimis (BE + FE parallel, if needed)
**Owner:** Anas (Project Owner)
**Duration:** 2-3 hours (small, focused sprint)
**Deliverable:** ONE PR (`feature/sprint-6-post-demo-hardening` → develop)
**Goal:** Post-demo cleanup, code quality, test coverage, and small UX polish. **NOT** new features — the demo already shipped (Sprint 5 "Wow" version).

---

## 🎯 Why this sprint

The Sprint 5 demo V2 shipped. We are now in the **post-demo hardening phase** for the 2-day pause window. This sprint focuses on:

1. **Code quality** — clean up tech debt introduced by the rapid 5-sprint push
2. **Test coverage** — fill gaps, especially in the modules touched in Sprints 4-5
3. **Documentation** — finalize the constitutional setup (this PR) + sprint retrospective
4. **Stale-branch cleanup** — delete merged feature branches, prune remote refs
5. **Small UX polish** — bugs found in the demo that didn't block the demo

**What's NOT in this sprint:**
- ❌ New features (the demo is shipped; new features = Sprint 7+)
- ❌ Architectural changes (sticking to Article 3)
- ❌ Refactors of the finance/accounting core (too risky for a demo+1 sprint)

---

## 🏛️ Architectural Constraints (Mavis Local, self-imposed)

> These mirror the constraints in the paused CONSTITUTION.md (Article 3 + Article 8).
> Since the active WORKFLOW.md supersedes for 2 days, these still apply (we don't weaken DOX).

### 1. Constitution Compliance
| Article | Rule | How to verify |
|---------|------|---------------|
| **Article 3** | **Multi-Company, NO Multi-Tenant** | `grep -r tenant_id src/` → 0 |
| **Article 8 Rule 5** | `company_id` Only | All queries filter on `company_id` |
| **Article 8 Rule 6** | No EF Core | Dapper + FluentMigrator only |
| **Article 8 Rule 9** | Frontend-First Errors (AR + EN) | Bilingual errors |
| **Article 8 Rule 10** | Document in AGENTS.md | Update nearest AGENTS.md per DOX |
| **Article 11** | One Test Per Endpoint | Each new/modified endpoint has a smoke test |

### 2. Stack Discipline
- **Backend:** C# / .NET 9 / Dapper / FluentMigrator (no EF Core)
- **Frontend:** TypeScript / Next.js 14 / Tailwind / shadcn/ui
- **DB:** PostgreSQL (local Docker for Mavis Local dev, Supabase for cloud)
- **Auth:** JWT HS256 + BCrypt cost 12

### 3. Security
- **No secrets** in code, chat, or PRs
- **Env vars only** for sensitive config
- **All new endpoints** under `[Authorize]` + `CompanyContext` filter

---

## 📋 Tasks (T0 inventory + planned work)

### T0 — Inventory (mandatory, before any work)

Per the T0 pattern from previous sprints, Mavis Local will:
- `git log origin/develop --oneline -30` — see what's already in
- `git branch -a` — see all branches (identify stale ones)
- Scan recent CHANGELOG entries — see what was actually shipped vs planned
- Confirm no Sprint 6 hand-off exists from سيتی (none found, hence self-plan)

### T1 — Constitutional Setup (this PR) ✅ DONE
- Promote constitution to `WORKFLOW.md` at root
- Activate `.mavis/AGENTS.md` (worker contract for Jimis)
- Update root `AGENTS.md` (governance banner + child DOX index + Sprint Model + Crons)
- Update `CHANGELOG.md` (this entry)
- Self-merge per DEC-070

### T2 — Stale-branch cleanup
- Delete local feature branches that are MERGED on origin/develop:
  - `feature/sprint-4-polish-demo-data` (PR #168 MERGED)
  - `feature/sprint-5-demo-v2` (PR #172 MERGED)
  - `fix/local-docker-setup` (PR #170 MERGED)
  - `fix/local-docker-p1-architecture` (PR #171 MERGED)
  - `fix/sprint3-merge-conflicts` (if merged)
  - `fix/sprint4-merge-conflicts` (if merged)
- Prune remote refs: `git fetch --prune origin`
- Keep: `develop`, `main`, `feature/sprint-6-post-demo-hardening`, `feature/abdo-team` (unknown), `feature/phase6-1c-auth-jwt` (unknown)
- Note: per "لا تحذف شي الان" earlier + "وحدف الزائد منها" latest — confirm with Anas before deleting

### T3 — Test coverage gap analysis (BE Jimi, ~1h)
- Identify modules touched in Sprints 4-5 that have < 1 test per endpoint
- Add 1 smoke test per missing endpoint
- Files likely touched:
  - `src/backend/Modules/Activity/` (Sprint 3 — added activity feed)
  - `src/backend/Modules/Search/` (Sprint 5 — new module)
  - `src/backend/Modules/Dashboard/` (Sprint 5 — added chart endpoints)
  - `src/backend/Modules/Finance/Accounts/` (Sprint 5 — CoA tree)

### T4 — Frontend polish (FE Jimi, ~1h)
- Fix any TypeScript warnings (should be 0, but verify)
- Add missing loading.tsx / error.tsx for routes that don't have them
- Verify all errors in `lib/errors.ts` are bilingual (AR + EN)
- Mobile responsive check on key pages: `/dashboard`, `/finance/accounts`, `/finance/sales-invoices`

### T5 — Documentation polish (Doc Jimi or Mavis Local, ~30 min)
- Update `docs/workflow/demo-roadmap.md` to mark Sprints 1-5 as done
- Add a brief "post-demo" section to the roadmap
- Verify all AGENTS.md files reflect current state
- Cross-check CHANGELOG entries are accurate

### T6 — Verification (Mavis Local, ~15 min)
- `dotnet build` (0 errors)
- `dotnet test` (all green)
- `npm run typecheck` (0 errors)
- `npm run build` (success)
- `grep -r tenant_id src/` (0 matches)
- `grep -r "password\s*=" src/` (0 secrets in code)

### T7 — Open PR + self-merge
- Branch: `feature/sprint-6-post-demo-hardening`
- Commit with Conventional Commits format
- Push + open PR
- Self-merge per DEC-070
- Update `state.json`: `ball_location = "mavis-local"`, drain pending_signals

---

## 📊 Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Build status** | 0 errors | `dotnet build` + `npm run build` |
| **Test count** | +N tests (gap-fill) | `dotnet test` count |
| **TypeScript errors** | 0 | `npm run typecheck` |
| **`tenant_id` count** | 0 | `grep -r tenant_id src/` |
| **Secrets in code** | 0 | `grep -r "password\s*=" src/` |
| **Stale branches** | -N | `git branch -a \| grep -c "feature/"` (before vs after) |
| **CHANGELOG accuracy** | Sprints 1-5 marked MERGED | Visual review |
| **Constitutional setup** | WORKFLOW.md at root, .mavis/AGENTS.md active | File presence |

---

## 🚨 Risks

| Risk | Mitigation |
|------|------------|
| **Stale-branch deletion is irreversible** | Use `mavis-trash` for local branches; keep remote branches untouched unless confirmed |
| **Test gap-fill introduces flaky tests** | Stick to smoke tests (no DB integration); defer integration tests to a later sprint |
| **Frontend polish churn** | Limit to loading/error/responsive only; no new components |
| **Documentation drift** | Re-verify all AGENTS.md after edits |

---

## 🏃 Coordination Protocol

### Mavis Local's role (this sprint)
- **Plan internally** (this hand-off is the plan)
- **Delegate to 2 Jimis** if T3/T4 need them (BE + FE parallel)
- **Verify** (T6) before PR
- **Open PR** + **self-merge** per DEC-070
- **Update state.json** (drain pending_signals, ball back to mavis-local)

### Admin Team's role (Siti / Dev)
- **Available as Cron Jobs** via state.json (no Telegram ping-pong)
- **Review** the PR within 15 min of opening (if they choose to; Mavis Local can self-merge)
- **Watch** for `blocked` state in state.json

### Communication
- **Primary:** `state.json` updates
- **Secondary:** PR comments for code review
- **Tertiary:** Telegram for urgent issues only

---

## 📌 Out of Scope (defer to Sprint 7+)

- New features (e.g., bank reconciliation UI, vendor portal)
- Architectural refactors (e.g., folder rename `MultiTenancy/` → `CompanyContext/`)
- Production deploy (production is FROZEN per legacy CONSTITUTION Article 10)
- Staging env reactivation
- E2E test suite expansion (Playwright on `feature/abdo-team` continues independently)

---

*Hand-off created: 2026-07-29 19:15 UTC by Mavis Local (self-planned, ball in mavis-local court per Anas's 2026-07-29 19:13 UTC directive)*
*Last updated: 2026-07-29 19:15 UTC*
