# 🚀 Sprint 9: Demo Version (Holding Company + Docker)

> **Date:** 2026-07-31
> **Architect:** Mavis (محمد mode, then سيتی mode, then ديف mode)
> **Owner:** Anas (Project Owner) — resting 2-3 hours
> **Status:** 🟡 HAND-OFF ready (Coordinators + Admin approved)
> **Source:** Per Anas mandate 2026-07-31 05:42 UTC

---

## 🎯 Goal

Ship a **demo-ready, Docker-packaged** version of ERP-SYSTEM that:
1. **Architecturally clean** — Holding Company refactor (Phase 1: docs)
2. **Clear BE↔FE contracts** — typed DTOs, OpenAPI/swagger, validation
3. **UI smooth and responsive** — RTL, AR + EN, loading states, error boundaries
4. **Works locally on Docker** — single `docker compose up`
5. **Functional for a Holding Company** — multi-company, consolidated reports

**Deliverable:** ONE PR (`feature/sprint-9-demo` → develop). Tagged demo-ready. Docker file tested locally.

---

## 🏛️ Architectural Constraints (binding)

Per `WORKFLOW.md` + `AGENTS.md` + Sprint 8 T4 proposal:

1. **Article 3 — `company_id` Only**: 0 `tenant_id` references
2. **Article 8 Rule 6 — No EF Core**: Dapper + FluentMigrator only
3. **Article 8 Rule 10 — Document in AGENTS.md**: update nearest AGENTS.md
4. **Article 11 — One Test Per Endpoint**: smoke test minimum
5. **0 source code regressions**: all 436 existing tests must still pass (after Sprint 8 T2)
6. **0 secrets in code**: env vars only

---

## 📋 Tasks (T0–T5)

### T0 — Inventory (Coordinator + Dev)

**Done by ديف (Dev) at 05:45 UTC** — see `docs/workflow/dev-environment-analysis.md`.

**Key findings:**
- ✅ dotnet 10/9/7 SDKs available
- ✅ node 24, npm 11, git 2.52
- ✅ Docker 29 + Compose 5.3 (full paths in PATH scan)
- ✅ PostgreSQL 17.10 (full install, all tools)
- ⚠️ PATH not auto-set — need full paths in scripts

---

### T1 — Local Team Jimis (3 in parallel, per R7 + Anas "max 3 parallel")

#### 🎯 Jimi 1 (BE) — Holding Company Refactor (Phase 1: docs)

**Scope (3 files, ≤ 1.5h):**
- `src/backend/Modules/Companies/AGENTS.md` — Rewrite to match actual schema (parent_company_id, is_group, slug, base_currency)
- `docs/architecture/holding-company-architecture.md` — Update Sections 5+7 to reflect the self-ref model
- `CHANGELOG.md` — Add Sprint 9 entry

**No code changes. Just docs.**

---

#### 🎯 Jimi 2 (BE) — BE-FE Contracts (typed DTOs + OpenAPI)

**Scope (5 files, ≤ 1.5h):**
- `src/backend/Host/Program.cs` — Ensure Swashbuckle is registered
- `src/backend/Host/Controllers/*.cs` — Add `[ProducesResponseType]` attributes
- `src/backend/Modules/Companies/Application/DTOs/CompanyDto.cs` — Add XML doc comments
- `src/backend/Modules/Finance/Application/DTOs/AccountDto.cs` — Same
- `src/frontend/lib/api-types.ts` — NEW file: TypeScript types matching the C# DTOs (manual or generated)

**Pattern:** Single source of truth via OpenAPI generation. The FE imports `api-types.ts` (regenerated when BE changes).

---

#### 🎯 Jimi 3 (FE) — UI Polish (RTL + AR/EN + loading states + error boundaries)

**Scope (8 files, ≤ 2h):**
- `src/frontend/app/(authenticated)/layout.tsx` — Add error boundary
- `src/frontend/app/(authenticated)/companies/page.tsx` — Add loading skeleton
- `src/frontend/app/(authenticated)/holding/dashboard/page.tsx` — Same
- `src/frontend/components/ui/LoadingSkeleton.tsx` — NEW reusable component
- `src/frontend/components/ui/ErrorBoundary.tsx` — NEW
- `src/frontend/lib/i18n.ts` — Add missing error messages
- `src/frontend/lib/errors.ts` — Already exists per Sprint 6, ensure AR/EN parity
- `src/frontend/components/layout/CompanySwitcher.tsx` — Add "loading" state

**Pattern:** All user-facing strings via `t(key, locale)` helper. RTL by default.

---

### T2 — Verify (Local Team Lead, Mavis Coordinator role)

```bash
# In worktree
dotnet build                                            # 0 errors
dotnet test                                             # 436 still pass + 3 new = 439
npm run typecheck                                       # 0 errors
npm run build                                           # success

# Check
grep -r "tenant_id" src/                                # 0
grep -r "password\s*=" src/ docs/                       # 0
ls -la local-docker/                                    # verify compose file
```

---

### T3 — Docker Test (Local Team Lead)

```bash
# Use full paths (per Dev's analysis)
cd local-docker
cp -n .env.example .env
docker compose up -d --build

# Wait for healthy
docker compose ps

# Apply demo seed
docker exec -i erp-postgres-local psql -U erp -d erp_system < ../docs/seed-sprint4-demo-data.sql

# Smoke test
curl http://localhost:5000/api/health/live
curl http://localhost:5000/api/health/ready
curl http://localhost:3000/

# Take screenshot of UI
```

---

### T4 — Open PR + Self-Merge (per Template 1 v2)

- Branch: `feature/sprint-9-demo` (off develop)
- PR title: `feat: Sprint 9 — demo version (Holding refactor + BE-FE contracts + UI polish + Docker)`
- Body: standard Template 1 v2 format
- **DO NOT self-merge** (per Template 1 v2; سيتی via crons handles it)

---

### T5 — Hand-off back to Admin (سيتی)

After PR opens:
- Ping سيتی via `state.json.pending_signals[]` (mavis CLI broken)
- Or via session message when CLI fixed
- سيتی reviews, merges to develop
- Updates state.json to v1.9
- CHANGELOG + branch cleanup

---

## 🏗️ Architecture Plan (Holding Refactor Phase 1 — Docs Only)

Per Sprint 8 T4 proposal, Phase 1 (docs only, zero risk):

1. **Update `Companies/AGENTS.md`** to reflect actual schema
2. **Update `holding-company-architecture.md` Section 5+7** to reflect self-ref model
3. **Add an ERD diagram** in the architecture doc showing the self-ref hierarchy
4. **Document the "Holding is a Company" convention** as canonical

No code changes. Just docs.

---

## 📊 Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **New tests** | ≥ 3 (1 per Jimi) | `dotnet test` count |
| **Test failures** | 0 | `dotnet test` |
| **Build errors** | 0 | `dotnet build` + `npm run build` |
| **Regressions** | 0 | All 439 tests pass |
| **Docker test** | Healthy + smoke test OK | `docker compose ps` + curl |
| **UI** | AR + EN parity | Manual check |
| **Cycle duration** | ≤ 2.5h | Start → PR open |

---

## ⚠️ Risks

| Risk | Mitigation |
|------|------------|
| **3 parallel Jimis merge conflicts** | Each Jimi works on a different file; explicit no-overlap in pre-flight |
| **Docker test wait time** | Start Docker test early in T1, not T3 |
| **UI changes break existing tests** | FE Jimi runs `npm run typecheck` + `npm run build` before commit |
| **Docs phase 1 incomplete** | Muhammad (me) reviews docs before merge |

---

## 🚦 Status Check (Anas's directive)

- ✅ **Always merge to develop** (3-Layer Model Layer 1)
- ✅ **Max 3 parallel Jimis** (per Anas's directive)
- ✅ **Local Docker testing** (per goal)
- ✅ **BE-FE contracts** (per goal)
- ✅ **UI smoothness** (per goal)
- ✅ **Architectural cleanliness** (per Sprint 8 T4 proposal)
- ⏸️ **Siti crons** — designed but not yet setup (CLI broken, need platform UI)

---

## 🔗 Reference Files

- `docs/workflow/architecture.md` — 10 soft rules
- `docs/workflow/holding-company-architecture.md` — current (stale, to be fixed in T1)
- `docs/architecture/holding-company-refactor-proposal.md` — Sprint 8 T4 proposal
- `docs/workflow/dev-environment-analysis.md` — Dev's pre-flight check
- `docs/workflow/admin-team-crons.md` — Siti's cron design
- `WORKFLOW.md` Article 9 — 3-Layer Deploy Model
- `INTER-TEAM-PROTOCOL.md` Template 1 v2 — Hand-off format

---

**Approval chain:**
- ✅ Anas (Owner) approved the goal at 2026-07-31 05:42 UTC
- ✅ Mavis (محمد + سيتی + ديف modes) drafted the hand-off
- ⏸️ Local Team v1.8 — your turn (spawn 3 Jimis in parallel)

🚀 Go.
