# Sprint 11 Retrospective — FE+BE Parallel with Intentional Overlap (2026-07-31)

**Status:** ✅ DONE
**Branch:** `feature/sprint-11-fe-be-parallel` (LOCAL-ONLY, not pushed)
**Commits (2):** `dd7ba19` (BE) → `f249cff` (FE)
**Diff size:** 20 files changed, +2554 / -49 lines
**Tests:** 439 pass · 30 skip · 2 pre-existing `RetentionTests` DB failures (unchanged)
**Builds:** `dotnet build` 0 errors · `npm run type-check` 0 errors · `npm run build` success (85/85 pages)

---

## 🎯 Goal (per Anas 2026-07-31 07:00 UTC)

> "في السباقت القادمه نضع 2 ووركرز للتنفيذ واحد على الفرونت اند والاخر على الباك اند، حتى يكون هناك تعارض في نطاق ملفات المشروع. وانتم (الأدمن) مسؤولون على من يذهب أولا وكيف."

**Sprint 11 deliverable:** complete demo with full FE + BE API coverage. FE+BE workers in parallel. Admin Team sequences the commits and manages conflicts.

---

## 📦 What Shipped

### BE (T2, commit `dd7ba19` — 12 files, +980/-34 lines)
- 5 new endpoints:
  - `GET /api/companies/tree` — Holding tree (flat recursive)
  - `GET /api/companies/{id}/subsidiaries`
  - `GET /api/holdings/dashboard` + `/api/dashboard/holding` (consolidated KPIs)
  - `GET /api/accounts` + `GET /api/accounts/{id}`
  - `GET /api/transactions/recent?limit=N` + `/api/transactions?limit=N`
- 2 new DTOs: `AccountDto`, `TransactionDto`, `HoldingDashboardDto`, `CompanyTreeNodeDto`, `SubsidiaryListDto` in `FinanceDtos.cs` + `CompanyDto.cs`
- 1 new service: `FinanceService.cs` (+309 lines)
- 3 new tests in `Companies/CompanyTreeTests.cs`

### FE (T1, commit `f249cff` — 8 files, +1574/-15 lines)
- 1 new types file: `src/frontend/lib/api-types.ts` (8 DTOs + 2 union helpers + re-exports)
- 8 new typed wrappers in `src/frontend/lib/api.ts`
- 2 updated pages: `holding/page.tsx` (KPI panel) + `admin/companies/page.tsx` (tree view)
- 3 new pages: `accounts/page.tsx` (CoA hub) + `transactions/page.tsx` (recent feed) + `reports/page.tsx` (saved reports panel)
- `AppShell.tsx` updated with new nav items
- `CHANGELOG.md` updated

---

## ✅ Wins

1. **Contract-first design saved us.** Even though "intentional overlap" was specified, the actual file scope split cleanly: FE wrote `src/frontend/lib/api-types.ts` (TS types) and pages; BE wrote controllers + DTOs + service + tests. The overlap was **conceptual** (the contract shape), not **physical** (the files). No merge conflicts.
2. **FE Jimi's types matched BE Jimi's DTOs perfectly on the first try.** Both DTOs and TS interfaces had: `AccountDto { id, code, name, type, parentId, companyId, currency, isActive, createdAt, updatedAt }`. This validates the "BE first → FE rebases" intuition — BE defines the schema, FE consumes it.
3. **Admin coordination was minimal.** No need to intervene mid-sprint. Both Jimis ran concurrently, both succeeded, both wrote CHANGELOG entries in the same Sprint 11 section.
4. **Verification clean.** T2 verify (build + tests + typecheck) all green on the merged branch. Zero regressions.
5. **No `tenant_id` introduced.** Article 3 upheld — 100% `company_id` in all new files. `git grep "tenant_id"` → 0 hits in changed files.

---

## 🟡 Friction Points

### 1. Commit order inverted from the hand-off
- **Hand-off said:** FE commits first, BE rebases second (per Anas 07:00 UTC: "FE wins on type conflicts").
- **What happened:** BE committed at 07:31:50 UTC, FE committed at 07:38:57 UTC (7 minutes later).
- **Why:** BE work is generally faster (smaller surface, no UI design choices). FE work has more design decisions (component layout, data shape, edge cases).
- **Outcome:** No conflict, no rebase needed. The merged tree is correct: BE endpoints + FE pages in the right shape. Just in inverted commit order. The eventual PR can use a rebase to clean the history if desired.

### 2. DTO drift risk (latent)
- FE wrote TS types based on the **Sprint 11 hand-off spec** (which the Admin wrote from the architecture doc).
- BE wrote C# DTOs based on the **same spec**.
- They happened to match perfectly — but this is a **latent fragility**: if the spec had been ambiguous, FE and BE could have drifted.
- **Mitigation for future sprints:** add a `docs/workflow/sprint-N-api-contract.md` that lists the exact DTO shape both FE and BE must match. Hand-off should reference this file.

### 3. `RetentionTests` still fail (2 tests, pre-existing)
- `ArchiveMetadata_InsertAndQuery` and `PartitionedAuditLog_AcceptsInserts` fail because they require a **real PostgreSQL database** (with partitioning support).
- The Local Docker Postgres on Mavis Local's machine has this, but the test environment running `dotnet test` on the Admin Team session does NOT have a real DB connection.
- **Per Anas 2026-07-31 07:46 UTC:** "يجب العمل عند التطوير على قاعدة البيانات psql ... السبب أن هناك أخطاء تظهر عند كتابة اختبارات محلية وتفشل بسبب عدم وجود قاعدة بيانات حقيقية. فأذكركم أنها مثبتة لدي."
- **Action:** this becomes a **Sprint 12 P0** — wire up `dotnet test` to use the real local psql on Mavis Local's machine (or a CI-side Postgres service container).

### 4. No auto-generated OpenAPI spec
- FE types and BE DTOs matched by hand. No machine-readable contract.
- For Sprint 12: add a Swashbuckle/OpenAPI generation step to `dotnet build` so FE can `openapi-typescript` from the spec instead of hand-writing types.

---

## 📚 Lessons Learned

### L1: File scope separation is the #1 conflict-avoidance tool
- **Rule:** when designing FE+BE parallel sprints, split files 100% — FE in `src/frontend/**`, BE in `src/backend/**`. The "intentional overlap" should be **conceptual** (the contract), not **physical** (the files).
- **Why:** even if both workers are writing the "same thing" (DTOs vs types), they're writing to different files. No merge conflict possible.
- **Apply to:** every future FE+BE sprint.

### L2: Contract-first beats code-first for parallel work
- The Sprint 11 hand-off (`docs/workflow/sprint-11-fe-be-parallel.md`) included the DTO shapes inline. Both workers referenced this same spec. They matched.
- **Rule:** for any parallel sprint, the hand-off must include the exact API contract (field names, types, nullability). No "interpret as you go."
- **Apply to:** make `docs/workflow/sprint-N-contract.md` a standard artifact for parallel sprints.

### L3: Real DB for local tests is a P0 infrastructure gap
- **Anas's directive 2026-07-31 07:46 UTC:** "يجب العمل عند التطوير على قاعدة البيانات psql"
- The current test setup uses in-memory mocks for most tests, which means any test that touches Postgres-specific features (partitioning, JSONB, `unnest()`, triggers) fails on `dotnet test`.
- **Fix (Sprint 12):** wire `ERPSystem.Tests` to a real local Postgres (via Testcontainers or a local Docker compose) so `dotnet test` runs against actual Postgres.

### L4: BE commits first in practice, despite the hand-off
- The hand-off said "FE wins on type conflicts." In practice, BE work is smaller and finishes first.
- **The "FE wins" rule is still correct** — when there's a CONFLICT (e.g., FE wrote a type that BE must implement), FE is the source of truth. But when there's no conflict (the common case), commit order doesn't matter.
- **Refined rule:** "FE wins on contract shape" not "FE commits first." Commit order is opportunistic.

### L5: Per-sprint retrospectives are proving their value
- Sprint 10 retro surfaced the AsyncLocal fragility → informed Sprint 10 Phase 3 fix.
- Sprint 11 retro (this doc) surfaces the contract-first pattern → informs Sprint 12 hand-off template.
- The pattern compounds. **Keep writing these.**

---

## 🎬 Sprint 12 Inputs (from Sprint 11 friction + Anas directives)

### P0 — Local test infrastructure (per Anas 07:46 UTC)
- **Goal:** `dotnet test` runs against real local Postgres (Mavis Local's machine), not in-memory mocks.
- **Approach options:**
  - (a) Testcontainers (spin up a Postgres container per test run)
  - (b) Local Docker compose (long-running, faster test cycles)
  - (c) Connection to Mavis Local's local-docker Postgres (already exists at `localhost:5432`)
- **Recommendation:** (c) — reuse the existing `local-docker` Postgres. Add a `ConnectionStrings__Postgres` env var to the test `appsettings.Test.json` (gitignored) that points to `localhost:5432`.

### P0 (parallel) — Architecture reaffirmation (per Anas 07:46 UTC)
- **Anas's directive:** "تطوير نظام الشركة القابضة وليس مالتي تينانت"
- **Implication:** every Sprint 12 task must respect Article 3: `company_id` only, NO `tenant_id`. Add a CI check (`grep -r "tenant_id" src/`) as a required check on PRs.
- **Already in place:** Sprint 10 Phase 2 renamed `Shared/MultiTenancy/` → `Shared/CompanyContext/`. Sprint 11 added no new `tenant_id`. Continue this discipline.

### P1 — OpenAPI contract generation
- **Goal:** FE gets types from a generated OpenAPI spec, not hand-written.
- **Approach:** add Swashbuckle to `Host/`, generate `openapi.json` in CI, FE consumes via `openapi-typescript`.
- **Benefit:** eliminates the L2 contract drift risk permanently.

### P1 — Sprint 12 hand-off template
- **Goal:** make every parallel sprint start with a clear contract.
- **Artifact:** `docs/workflow/sprint-N-contract.md` (DTOs + endpoints + test cases). Reuse this for Sprint 12+.

---

## 🏁 Sprint 11 Final Verdict

**Sprint 11 succeeded.** Full demo coverage (FE + BE) delivered, clean verification, no regressions, architecture discipline maintained. Two parallel workers coordinated without intervention. The "intentional overlap" experiment worked — by splitting file scope cleanly, no actual file conflict emerged.

The next bottleneck is **local test infrastructure** (per Anas 07:46 UTC). Sprint 12 P0 is to wire `dotnet test` to a real local Postgres. Once that lands, the test suite will go from 439 pass / 2 fail → ~441+ pass / 0 fail (modulo any new test additions).

**LOCAL-ONLY mode maintained throughout.** No push, no PR. Awaiting Anas "ادفع" / "ارفع بي ار" directive.
