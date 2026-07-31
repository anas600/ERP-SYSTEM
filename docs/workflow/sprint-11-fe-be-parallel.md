# 🛠️ Sprint 11: Full Demo Coverage (FE + BE Parallel, intentional overlap)

> **Date:** 2026-07-31
> **Architect:** Mavis (محمد + سيتی + ديف modes, coordinated)
> **Owner:** Anas (Project Owner) — resting, "انتم الادمن بدالي" (admin = me)
> **Status:** 🟡 HAND-OFF ready
> **Source:** Per Anas mandate 2026-07-31 07:00 UTC

---

## 🎯 Goal

Per Anas: "نسخه من الديمو جاهزه وكامله من حيث تغططيه الواجهات لل اي بي اي الخاص بالباك اند" (a complete demo version that covers the FE + BE API).

**Deliverable:** A complete demo with **full FE + BE API coverage** — every BE endpoint has a working FE page that calls it. Verified locally (no push, no PR per the new strategy).

**Constraint:** 2 parallel workers (FE + BE) with **intentional file overlap** to test our coordination patterns (per Anas: "لكي يحصل تعارض في السكوب ع ملفات المشروع").

---

## 📋 The Intentional Overlap Design

The 2 workers WILL conflict on **shared types/constants**. This is by design (per Anas).

**Shared files both will touch:**
- `src/shared/types.ts` (or similar) — TypeScript types mirroring C# DTOs
- `src/shared/constants.ts` (or similar) — status codes, error codes, etc.
- `src/backend/Host/Controllers/*.cs` (BE) ↔ `src/frontend/lib/api/*.ts` (FE calling code)

**Work sequence (Admin Team decides):**
- **FE worker (T1) starts first** — creates the TypeScript types and the FE pages
- **BE worker (T2) waits for FE commit OR runs in parallel on non-overlapping endpoints**

This creates a "contract-first" workflow:
- FE worker defines the types + the API calls
- BE worker (re-)implements the BE to match the FE types
- If they conflict, the FE types win (FE is the user-facing contract)

---

## 📋 Tasks (T0–T4)

### T0 — Inventory (Admin Team: Dev mode)

Already done (per Sprint 10's T0):
- `dotnet build` works
- `npm run type-check` works
- `dotnet test`: 439 pass baseline (3 from Sprint 8 T2 + 4 from Sprint 10 Phase 3)
- All dev tools available (per Dev's env analysis)

**New for Sprint 11:**
- **Identify the full list of BE endpoints** that don't have FE coverage
- **Identify the FE pages** that need new API calls
- **List the shared types** to be created/updated

### T1 — Worker 1 (FE) — Frontend Demo Coverage (3-4 hours)

**Scope (8-10 files, ≤ 1.5 days):**

| File | What |
|------|------|
| `src/frontend/lib/api-types.ts` (UPDATE) | Add 10-15 new TypeScript types matching BE DTOs (Company, Account, User, Transaction, etc.) |
| `src/frontend/lib/api.ts` (UPDATE) | Add typed wrappers for 10-15 new BE endpoints |
| `src/frontend/app/(authenticated)/companies/page.tsx` (UPDATE) | Show Company tree (Holding → subsidiaries) — uses new CompanyTree endpoint |
| `src/frontend/app/(authenticated)/holding/dashboard/page.tsx` (UPDATE) | Show consolidated KPIs (total revenue, total expenses, etc.) — uses new HoldingDashboard endpoint |
| `src/frontend/app/(authenticated)/accounts/page.tsx` (NEW) | Chart of Accounts list — uses new AccountList endpoint |
| `src/frontend/app/(authenticated)/transactions/page.tsx` (NEW) | Recent transactions list — uses new TransactionList endpoint |
| `src/frontend/app/(authenticated)/reports/financial/page.tsx` (NEW) | Financial reports hub — uses new ReportList endpoint |
| `src/frontend/components/layout/AppShell.tsx` (UPDATE) | Add new nav items for the new pages |
| `src/frontend/components/layout/Breadcrumbs.tsx` (UPDATE) | Show Holding context in breadcrumbs |

**Contract first:** FE worker creates the `api-types.ts` definitions + calls the BE endpoints. If the BE endpoints don't exist yet, the FE code will fail at runtime (but `npm run type-check` will pass if the types exist).

**Intentional overlap:** Both FE and BE may touch `api-types.ts` ↔ `CompanyDto.cs` (the contract).

### T2 — Worker 2 (BE) — Backend Demo Endpoints (3-4 hours)

**Scope (8-10 files, ≤ 1.5 days):**

| File | What |
|------|------|
| `src/backend/Host/Controllers/CompaniesController.cs` (UPDATE) | Add `GET /api/companies/tree` (holding tree) endpoint |
| `src/backend/Host/Controllers/CompaniesController.cs` (UPDATE) | Add `GET /api/companies/{id}/subsidiaries` endpoint |
| `src/backend/Host/Controllers/HoldingController.cs` (NEW) | Add `GET /api/holding/dashboard` (consolidated KPIs) endpoint |
| `src/backend/Host/Controllers/AccountsController.cs` (UPDATE) | Add `GET /api/accounts` (list) + `GET /api/accounts/{id}` endpoints |
| `src/backend/Host/Controllers/TransactionsController.cs` (NEW) | Add `GET /api/transactions` (recent) + filter by company |
| `src/backend/Host/Controllers/ReportsController.cs` (UPDATE) | Add `GET /api/reports` (list) + `GET /api/reports/{id}` |
| `src/backend/Modules/Companies/Application/Services/CompanyService.cs` (UPDATE) | Add `GetTreeAsync()` method |
| `src/backend/Modules/Companies/Application/DTOs/CompanyDto.cs` (UPDATE) | Add `CompanyTreeNodeDto` (id, name, parent_id, children[]) |
| `src/backend/Modules/Finance/Application/Services/FinanceService.cs` (UPDATE) | Add `GetConsolidatedKpisAsync()` for the holding dashboard |
| `src/backend/Tests/ERPSystem.Tests/Companies/` (UPDATE) | Add 3-4 new tests for the tree endpoint |

**Contract follower:** BE worker implements the endpoints to match the FE's `api-types.ts` (or vice versa — see T1 contract-first approach).

**Intentional overlap:** BE DTOs ↔ FE types must match. This is the test of our coordination.

### T3 — Verify (Coordinator role, after both workers finish)

```bash
cd C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-11
dotnet build                                            # 0 errors
dotnet test                                             # 442+ pass (no regressions)
npm run type-check                                      # 0 errors
npm run build                                           # success

# Critical: verify the BE↔FE contract matches
# (cross-check types in src/frontend/lib/api-types.ts against the BE DTOs in src/backend/Modules/*/Application/DTOs/)
```

### T4 — Local Demo Run (Coordinator role, after verify)

```bash
# Use full paths (per Dev's analysis)
cd local-docker
cp -n .env.example .env
docker compose up -d --build

# Wait for healthy
docker compose ps

# Apply demo seed (idempotent)
docker exec -i erp-postgres-local psql -U erp -d erp_system < ../docs/seed-sprint4-demo-data.sql

# Smoke test the new endpoints
curl http://localhost:5000/api/companies/tree
curl http://localhost:5000/api/holding/dashboard
curl http://localhost:5000/api/accounts
curl http://localhost:5000/api/transactions

# Smoke test the FE
open http://localhost:3000
```

### T5 — Sprint 11 Retrospective (Coordinator role)

Per the new "analyze each sprint" pattern:
- Write `docs/team-charters/retrospectives/sprint-11-retro.md`
- Capture: what worked, what didn't, what to improve
- Note the parallel worker conflict resolution patterns

---

## 🛠️ Coordination Strategy (Admin Team)

### Sequencing

**Recommended order** (per Sprint 10 lessons learned):
1. **T0** (Admin: Dev mode) — inventory the BE endpoints + FE pages
2. **T1** (FE worker) — creates `api-types.ts` with the contract + the FE pages (some endpoints may 404 at runtime, that's OK)
3. **T2** (BE worker) — implements the matching BE endpoints (so the FE calls work)
4. **T3** (Admin: Coordinator) — verify everything
5. **T4** (Admin: Dev mode) — local Docker run + smoke test
6. **T5** (Admin: Coordinator) — retrospective

### Conflict Resolution

If FE and BE both touch `api-types.ts` / `CompanyDto.cs`:
- The FE types win (FE is the user-facing contract)
- BE re-implements to match (or extends with BE-specific fields, ignored by FE)
- Use `git rebase` to clean up the commit order at PR-time

### Monitoring

- **Per-sprint cron:** `monitor-sprint11-jimis` (every 30 min) — check status only
- **Retrospective cron:** `sprint-retrospective` (end of each sprint) — write the retro doc
- **NO action crons** (per Anas 06:47 UTC: no auto-merge, no auto-push)

---

## 📊 Sprint 11 — Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **New BE endpoints** | ≥ 5 | New routes in `Controllers/*.cs` |
| **New FE pages** | ≥ 3 | New files in `app/(authenticated)/**/page.tsx` |
| **New tests** | ≥ 5 | `dotnet test` count + xunit count |
| **Test failures** | 0 new | `dotnet test` |
| **Build errors** | 0 | `dotnet build` + `npm run build` |
| **Regressions** | 0 | All 442 existing tests still pass |
| **Docker test** | Healthy + smoke test OK | `docker compose ps` + curl |
| **UI** | All new pages render | Manual check + screenshot |
| **Cycle duration** | ≤ 1.5 days | Start → last commit |

---

## 🚦 Status Check (Anas's new directives)

- ✅ **LOCAL-ONLY** for this sprint (per 06:47 UTC)
- ✅ **2 workers (FE + BE) with intentional overlap** (per 07:00 UTC)
- ✅ **Admin Team coordinates** who goes first + resolves conflicts
- ✅ **Use crons for management/coordination/analysis/verification**
- ✅ **Anas is proxy** — Admin acts on his behalf while he rests
- ✅ **Deliverable:** complete demo with full FE + BE API coverage
- ❌ **No push, no PR** (per 06:47 UTC) — until Anas explicitly says so

---

## 🔗 Reference Files

- `docs/team-charters/retrospectives/sprint-10-retro.md` (just written — parallel worker lessons)
- `docs/workflow/dev-environment-analysis.md` (Dev's env check)
- `docs/workflow/admin-team-crons.md` (cron design — action vs monitor)
- `docs/architecture/holding-company-refactor-proposal.md` (Sprint 8 T4 — context)
- `src/frontend/lib/api-types.ts` (Sprint 9 T2 — the existing types)
- `src/backend/Host/Controllers/*.cs` (existing endpoints)

---

**Approval chain:**
- ✅ Anas (Owner) approved the goal at 2026-07-31 07:00 UTC
- ✅ Mavis (Coordinator) drafted the hand-off
- ⏸️ Local Team v1.8 — your turn (spawn 2 Jimis in parallel, FE+BE)

🚀 Go.
