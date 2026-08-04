# Sprint 32 Retro — Projects module tables fix (DEC-112)

**Date:** 2026-08-04
**Branch:** `feature/sprint-32-projects-module`
**Status:** ✅ DONE (LOCAL-ONLY)
**Duration:** ~2 hours (after Anas's power-restart recovery, ~30 min of which was verification)

---

## Goal

Per Anas's Q1 from Sprint 31 (defer to Sprint 32): fix the Projects module's 4 missing `data-types/*.json` files so the tables get created. Close the loop on L44 ("Projects module is partially implemented").

**Stretch**: also clean up the Sprint 31 PostingRulesBenchmarkTests.cs collateral (test build was broken from Sprint 31's ctor change).

---

## What was done

### DEC-112 — 4 data-types JSONs + quoted identifier support
1. **Added 4 `data-types/*.json` files**:
   - `resources.json` (9 fields, 1 unique index)
   - `project_tasks.json` (13 fields, 2 indexes, 2 FKs)
   - `resource_assignments.json` (11 fields, **quoted `from` + `to`**, 2 indexes, 6 FKs)
   - `project_budgets.json` (10 fields, 1 index, 5 FKs)
2. **Added `quoted: true` flag to `FieldDefinition`** (C# + JSON property). When true, `DataTypeMigrator.CreateTableAsync` and `AddColumnAsync` force double-quoted SQL identifier. Needed for `from` and `to` (Postgres SQL reserved words).
3. **Updated `ResourceAssignmentRepository.Sel + InsertAsync`** to use `"from" AS "From"` and `"to" AS "To"` in SELECT, and `"from"`, `"to"` in the INSERT column list. Every reference must be quoted once the column is created quoted.

### Collateral — Sprint 31 test build fix
- **PostingRulesBenchmarkTests.cs** had 4 broken tests (Sprint 31): `NpgsqlConnectionFactory(string)` ctor doesn't exist anymore (Sprint 22-23 added `IOptions<NpgsqlConnectionOptions>` + `ILogger<NpgsqlConnectionFactory>`) + `await using IDbConnection` is C# error CS8417 (IDbConnection doesn't implement IAsyncDisposable).
- **Fix**: use `Options.Create(new NpgsqlConnectionOptions { OltpConnectionString = ... })` + `NullLogger<NpgsqlConnectionFactory>.Instance` + `using var conn = await ...`.
- Tests stay `[Fact(Skip = "Integration test — needs live DB")]` so behavior unchanged. Just made the file compilable.

---

## Verification

### Schema (DB introspection)
```
$ psql -c "\dt" → 48 tables (44 existing + 4 new Projects tables)
$ psql -c "\d resource_assignments"
   id          | uuid                     | not null
   company_id  | uuid                     | not null
   project_id  | uuid                     | not null
   task_id     | uuid                     | not null
   resource_id | uuid                     | not null
   user_id     | uuid                     | not null
   from        | timestamp with time zone | not null    ← quoted
   to          | timestamp with time zone | not null    ← quoted
   hourly_rate | numeric(18,4)            | not null | 0
   created_at  | timestamp with time zone | not null | now()
   3 indexes + 6 FKs all in place
```

### End-to-end smoke (admin login → POST → GET roundtrip)
| Endpoint | Result |
|---|---|
| POST /api/resources | 201 — id=c5896f3b... |
| POST /api/projects | 201 — id=44fd023e... (PRJ-001 "مشروع اختبار") |
| POST /api/tasks | 201 — id=a8068d72... ("حفر الأساس") |
| POST /api/projects/{id}/assignments | 201 — id=75c4a111..., **from/to preserved, estimatedHours=10, estimatedCost=500** ✅ |
| GET /api/projects/{id}/tasks | 1 task listed |
| GET /api/projects/{id}/assignments | 1 assignment listed, hours=10 |
| GET /api/resources | 1 resource listed, code=RES-001 rate=50 |
| GET /api/finance/ledger/accounts/{guid} (1230 AR) | 37 ledger lines |

### 18-endpoint regression smoke
All 200 OK, 0 × 500, 0 × 404:
- HR: employees, departments
- Finance: accounts, ledger/trial-balance, posting-rules
- AR: customers, sales-invoices, receipts, payments (`/api/payments` — was confused with `/api/ar/payments` initially; it's the PaymentsController route)
- Procurement: pos, grs, bills, vendors
- Inventory: items, warehouses
- Projects, Resources, Cost-centers

### Test suite
- `dotnet test --filter Projects` → **24/24 PASS** (8 ProjectService + 5 Task + 3 Resource + 6 Budget + 2 ResourceAssignmentComputed)
- `dotnet test` (full) → **378/403 PASS, 2 fail (pre-existing retention integration), 23 [Skip]**
  - 2 failing: `RetentionTests.PartitionedAuditLog_AcceptsInserts` + `ArchiveMetadata_InsertAndQuery` — connect as `postgres` user which doesn't exist on local. Pre-existing (Sprint 30 era), not from my changes.

---

## Files changed (8 files, +248/-13)

| File | Type | Lines | Description |
|---|---|---|---|
| `src/backend/Host/data-types/resources.json` | NEW | 27 | Resource entity table def |
| `src/backend/Host/data-types/project_tasks.json` | NEW | 34 | ProjectTask entity table def |
| `src/backend/Host/data-types/resource_assignments.json` | NEW | 32 | ResourceAssignment entity (with quoted from/to) |
| `src/backend/Host/data-types/project_budgets.json` | NEW | 29 | ProjectBudget entity table def |
| `src/backend/Shared/DataTypes/FieldDefinition.cs` | MOD | +8 | Added `Quoted` bool prop + DEC-112 comment |
| `src/backend/Shared/DataTypes/DataTypeMigrator.cs` | MOD | +4/-4 | `CreateTableAsync` + `AddColumnAsync` honor `Quoted` flag |
| `src/backend/Modules/Projects/Infrastructure/ResourceAssignmentRepository.cs` | MOD | +5/-3 | Quoted `from`/`to` in SELECT + INSERT + ORDER BY |
| `src/backend/Tests/ERPSystem.Tests/Finance/PostingRulesBenchmarkTests.cs` | MOD | +28/-11 | Sprint 31 test collateral — fixed ctor + `await using` (CS8417) |
| `CHANGELOG.md` | MOD | +27 | Sprint 32 entry |
| `AGENTS.md` | MOD | +17 | Sprint 32 decisions + lessons L47..L49 |
| `docs/team-charters/retrospectives/sprint-32-retro.md` | NEW | (this) | Retro |

**Commit:** pending (not yet committed — waiting for "ادفع")

---

## Lessons (L47..L49)

### L47 — Always check the actual table name from `\d` or the repo's SQL
The entity name alone can be misleading:
- `ProjectTask` entity → `project_tasks` table (NOT `tasks`)
- `ResourceAssignment` entity → `resource_assignments` table (NOT `project_assignments`)

The repos confirmed the real table names. The summary in my Sprint 32 context block had the wrong table names. Reading the repos' INSERT/SELECT statements is the source of truth.

### L48 — SQL reserved words require `quoted: true` + repo updates
When a column would be a SQL reserved word (`from`, `to`, `user`, `order`, `table`), the migrator needs `quoted: true` to create the column as `"from"`, and every reference in the repo (SELECT, INSERT, ORDER BY) must also be quoted. Postgres is strict — once a column is created quoted, no unquoted reference works.

The cleanest path was: add `quoted: true` to JSON, force-quote in DataTypeMigrator, then update the repo. 4 small changes vs. renaming the entity property (which would have broken every place that uses the property name).

### L49 — Best to avoid SQL reserved words in entity column names from day 1
For fresh entity creation: prefer `start_at`/`end_at` over `from`/`to`, `display_name` over `name` (reserved in some contexts), etc. For existing entities that use reserved words (like `ResourceAssignment.From`/`To`): use `quoted: true` + repo updates.

---

## Carry-over to Sprint 33+

| Priority | Item | Notes |
|---|---|---|
| P0 | Audit `ProjectCostCenter`, `AccountService`, `ChartOfAccountsService`, `PayrollService` | Last 4 unchecked modules from DEC-085 cycle |
| P0 | Refactor remaining `req.CompanyId` → `_companyContext.CompanyId` (L30) | Search-wide audit |
| P1 | Manual JEs (depreciation + accruals + year-end) | 12 P1 JEs from CHANGELOG |
| P1 | customer/vendor statement GET endpoints | Carry-over from Sprint 30 |
| P1 | Trial Balance validation UI ("Balanced / Unbalanced") | |
| P2 | Activate 5th VAT rule (DEC-109) | Add 1410/1411 (VAT Input/Output) accounts to CoA |
| P2 | Add Playwright e2e as CI gate (DEC-111 follow-up) | scripts/playwright-smoke.mjs |
| P2 | Fix Health Ping + Daily Status workflows | DEC-111 disabled them |
| P2 | Fix 2 retention integration test failures | Need `postgres` user setup or skip-by-environment |

---

## Sprint health

- **Velocity**: ~2 hours (3 hours with test collateral)
- **Blockers encountered**: 0 (Anas's power-restart was a forced pause, not a blocker)
- **Collateral damage**: 1 test file fixed (Sprint 31 PostingRulesBenchmarkTests — was broken since Sprint 31 merge but didn't show up because `dotnet build` wasn't run in Sprint 31)
- **L26 lesson reinforced**: bulk replace + commit without `dotnet build` = broken test compile. Sprint 31 missed the test compile; Sprint 32 caught it.
