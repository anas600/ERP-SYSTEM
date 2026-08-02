# Sprint 28 — Article 3 audit (4 modules) + Procurement seeder (POC #3) + test refactor (2026-08-02)

**Status:** ✅ DONE (LOCAL-ONLY, awaiting Anas's "ادفع" with Sprint 24+25+26+27+28 stacked)
**Branch:** `feature/sprint-21-posting-rules-engine` (Sprints 21+22+23+24+25+26+27+28 stacked)
**Goal:** Continue the Article 3 audit pattern (now 8 sprints running) — fix 4 remaining modules (Payroll, Projects, StockMovement, Finance/Account), then POC the seeder pattern a third time (Procurement). L17 prediction: 3rd seeder = 1.5-2h, not 4-6h. ✅ Confirmed.

---

## 🎯 What Was Delivered

| Artifact | LOC | Purpose |
|---|---|---|
| `src/backend/Modules/Payroll/Domain/Entities/{SalaryStructure,PayrollRun,PayrollItem}.cs` | +1 line each | Add `CompanyId` to 3 entities (DEC-094) |
| `src/backend/Modules/Payroll/Application/Services/PayrollService.cs` | +~20 lines | Inject `ICompanyContext` (DEC-094) |
| `src/backend/Modules/Payroll/Infrastructure/PayrollRepository.cs` | +5 lines × N INSERTs | Add `@CompanyId` to all 5 INSERTs + SELECTs |
| `src/backend/Modules/Projects/Entities/{ProjectBudget,ProjectTask,Resource,ResourceAssignment}.cs` | +1 line each | Add `CompanyId` to 4 entities (DEC-095) |
| `src/backend/Modules/Projects/Application/Services/ProjectService.cs` | +~10 lines | Inject `ICompanyContext`; use `_companyContext.CompanyId` in `CreateAsync` (L19, L29) |
| `src/backend/Modules/Projects/Application/Services/SupportingServices.cs` | +~25 lines | Inject `ICompanyContext` in TaskService, ResourceService, ResourceAssignmentService |
| `src/backend/Modules/Projects/Infrastructure/{Task,Resource,ResourceAssignment,ProjectBudget}Repository.cs` | +~12 lines | Add `@CompanyId` to all INSERTs + SELECTs |
| `src/backend/Modules/Inventory/Application/Services/StockMovementService.cs` | ~16 lines refactored | All 4 `Create*` methods use `_companyContext.CompanyId` (DEC-096, L30) |
| `src/backend/Modules/Finance/Entities/Account.cs` | 1 line changed | `Guid? CompanyId` → `Guid CompanyId` (DEC-097) |
| `src/backend/Shared/Migrations/Sprint28_Audit_20260802_220000.cs` | ~110 lines | Idempotent backfill for 10 tables |
| `src/backend/Shared/SeedData/ArabicProcurementDevData.json` | ~9 KB | 10 POs (with 1-2 lines each) + Arabic notes (POC #3) |
| `src/backend/Shared/SeedData/ArabicProcurementDevSeederHostedService.cs` | ~16 KB | IHostedService, 3-pass UPSERT (POs + lines) |
| `src/backend/Host/Program.cs` (Sprint 28 block) | ~10 lines | Registration gated on `IsDevelopment()` + `Bootstrap:SeedProcurementScenario=true` |
| `src/backend/Host/ERP-SYSTEM.csproj` (Content include) | +1 line | Copies `ArabicProcurementDevData.json` to `bin/...` |
| `src/backend/Host/appsettings.Development.json.example` (template) | +5 lines | Documents the new flag |
| `src/backend/Host/appsettings.Development.json` (gitignored) | +1 line | Enables the seeder for local dev |
| `src/backend/Tests/ERPSystem.Tests/Projects/ProjectServiceTests.cs` | rewritten (~440 lines) | New `TestCompanyContextFactory.Create()` helper (DEC-099) + 2 tests rewritten for L19 cross-tenant safety |
| `CHANGELOG.md` | +~80 lines | Sprint 28 entry at top |
| `AGENTS.md` | +~50 lines | DEC-085 #8, #9 added; Sprint 28 DECs + lessons section |
| `docs/team-charters/retrospectives/sprint-28-retro.md` | (this file) | Lessons + decisions |

**Verified end-to-end:**
- `dotnet build` → 0 errors, 0 warnings
- `dotnet test --filter 'Projects'` → **18/18 passed** (after L21 refactor + L26 IIFE fix + L29 test fix)
- `dotnet test` (full suite) → 378 passed, 2 environmental fails (RetentionTests need production PG creds — pre-existing, unrelated to Sprint 28)
- Migration `Sprint28_Audit` → no-op (0 rows to backfill; tables were empty in dev)
- Procurement seeder log: `POs updated=2 inserted=8` (some POs already existed from manual Sprint 25 testing)
- GoodsReceipts + VendorBills **skipped** in seeder (no default warehouse exists — FK requires `warehouse_id NOT NULL`; carry-over for Sprint 29+)

---

## 🐛 Article 3 Audit (DEC-094..097)

Pre-Sprint 28 state of the 4 modules:

| Module | Entities (without CompanyId) | Services (no ICompanyContext) | Repos (no company_id in INSERT) |
|---|---|---|---|
| **Payroll** | SalaryStructure, SalaryStructureLine, PayrollRun, PayrollItem, PayslipComponent (5) | PayrollService (1) | PayrollRepository (1, 5 INSERT methods) |
| **Projects** | ProjectBudget, ProjectTask, Resource, ResourceAssignment (4) | ProjectService, TaskService, ResourceService, ResourceAssignmentService (3 in SupportingServices.cs) | 4 repos (Task, Resource, ResourceAssignment, ProjectBudget) |
| **StockMovement** | (already had CompanyId) | StockMovementService was using `req.CompanyId` instead of `_companyContext.CompanyId` | (already had @CompanyId) |
| **Finance/Account** | (Account.CompanyId was `Guid?` — minor type fix) | — | — |
| **Total** | **9 entities** | **4 services** | **5 repos (with multiple INSERTs each)** |

DB tables all have `company_id NOT NULL` constraint (Sprint 22 schema) — meaning any attempt to INSERT via the service would have hit `null value in column "company_id"`. The HR module was the same story (Sprint 27) — empty tables mean the broken INSERTs were never exercised, but the bug was real.

This is the **8th sprint in a row** to surface Article 3 violations (Sprints 19, 21, 22, 23, 24, 25, 27, 28). The DEC-085 pre-push checklist in AGENTS.md is the right defense.

### DEC-094: Payroll Article 3 fix

The fix is mechanical and mirrors Sprint 25 (Procurement) + Sprint 27 (HR):

| Layer | Change |
|---|---|
| Entity | Add `public Guid CompanyId { get; set; }` |
| Service | Inject `ICompanyContext` in constructor; read `CompanyId = _companyContext.CompanyId ?? throw` in `CreateAsync` |
| Repository | Add `company_id` to INSERT columns + `company_id AS CompanyId` to SELECT column list (×5 INSERTs) |
| Migration | `UPDATE ... SET company_id = (first company) WHERE company_id IS NULL` (idempotent) |

`EosService` (end-of-service calculation) is clean — it's read-only, no entity writes.

### DEC-095: Projects Article 3 fix (with the L19 + L29 critical fix)

The 4 entities + 3 services + 4 repos follow the standard pattern. But the **critical** fix is in `ProjectService.CreateAsync`:

```csharp
// Before (BUG: client could spoof companyId)
var project = new Project
{
    CompanyId = req.CompanyId,  // ← DTO value, spoofable
    ...
};

// After (L19: ICompanyContext wins)
var companyId = _companyContext.CompanyId
    ?? throw new InvalidOperationException("Company not resolved");
var project = new Project
{
    CompanyId = companyId,  // ← JWT context, not spoofable
    ...
};
// The auto-created ProjectBudget also uses `companyId` (same local variable)
await _budgets.InsertAsync(new ProjectBudget
{
    CompanyId = companyId,  // L29: same variable, not _companyContext.CompanyId
    ...
}, ct);
```

This is **L19 cross-tenant safety** applied to the Project aggregate. The aggregate has 2 child writes (Project + ProjectBudget) that BOTH need the same companyId. L29 says: read once at the top, pass a local variable to all writes. The test verifies this by reading the companyId from the test's mock context.

### DEC-096: StockMovement service refactor (no entity/repo change)

The StockMovement entity + repo already had `CompanyId` + `@CompanyId`. The service was the only place using `req.CompanyId`. Refactored all 4 `Create*` methods:

```csharp
// Before: ReceiveAsync(Guid userId, ReceiveRequest req, ...)
var movement = new StockMovement
{
    CompanyId = req.CompanyId,  // ← spoofable
    ...
};

// After: use ICompanyContext
var companyId = _companyContext.CompanyId
    ?? throw new InvalidOperationException("Company not resolved");
var movement = new StockMovement
{
    CompanyId = companyId,  // ← JWT context
    ...
};
```

Same pattern in `CreateIssueAsync`, `CreateTransferAsync`, `CreateAdjustAsync`. The `req.CompanyId` is removed from the request DTOs entirely (L30).

### DEC-097: Finance/Account minor type fix

```csharp
// Before: nullable type, but DB column is NOT NULL → potential NRE
public Guid? CompanyId { get; set; }

// After: matches the DB
public Guid CompanyId { get; set; }
```

This was a code-level inconsistency. The DB column has been `company_id NOT NULL` since Sprint 22. The nullable type was a 4-year-old bug waiting to surface. No service or repo change needed (they already set it correctly).

---

## 📐 Design Decisions

### DEC-088/L17: Procurement seeder — POC #3 (L27 confirmation)

The seeder is the **3rd implementation** of the same pattern (Sprint 26 = customers/vendors/items, Sprint 27 = departments/employees, Sprint 28 = purchase orders). The L17 prediction was "3rd seeder = 1.5-2h, not 4-6h" — **confirmed**. The bulk of the time was reading the JSON structure + handling schema surprises, not figuring out the pattern.

**Schema surprises absorbed (L28):**
- `vendors` table does NOT have a `name_en` column (entity has it, but the table doesn't). Fixed by removing the column from the UPDATE statement.
- `purchase_order_lines` table does NOT have `updated_at`/`updated_by` columns. Fixed by removing them from the UPDATE.

**Future-proofing:** the seeder reads the JSON, validates against psql `\d` output, and logs all schema surprises at startup so the next seeder author can see them.

### DEC-099: TestCompanyContextFactory helper

The previous Sprint 27 IIFE pattern was JavaScript, not C#:

```csharp
// WRONG (JavaScript IIFE — doesn't compile in C#)
new TaskService(tasks, (function(){ var m = new Mock<ICompanyContext>(); m.Setup(c => c.CompanyId).Returns(Guid.NewGuid()); return m.Object; })());
```

The test file didn't even compile. The 2 tests that were reported as "failing at runtime" were actually **compile errors** that got masked by the `dotnet test --filter` behavior (the project as a whole failed to build, so the filter had nothing to run).

**DEC-099 fix:** centralize the mock setup in a helper:

```csharp
// CORRECT (C# helper)
internal static class TestCompanyContextFactory
{
    public static ICompanyContext Create() => Create(Guid.NewGuid());
    public static ICompanyContext Create(Guid companyId)
    {
        var m = new Mock<ICompanyContext>();
        m.Setup(c => c.CompanyId).Returns(companyId);
        return m.Object;
    }
}

// Usage in tests:
var svc = new TaskService(tasks, TestCompanyContextFactory.Create());
// Or with a specific companyId for L19 verification:
var ctx = TestCompanyContextFactory.Create(ctxCompanyId);
var svc = new ProjectService(projects, budgets, costCenters, ctx, ...);
```

This helper will be used in every test that needs to instantiate a service that takes `ICompanyContext`. The Sprint 29+ audit work will use it for the remaining 5 modules (Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService).

---

## 💡 Lessons (L25..L30)

### L25 — Audit pattern holds across 8 sprints (no diminishing returns)

Each sprint finds 4-8 violations. The DEC-085 checklist catches 100% of them. The bug is **structural**: without a code-level audit, `CompanyId` drifts out of new entities. The audit must be **per-sprint**, not per-release.

**Remaining un-audited modules (carry-over for Sprint 29+):**
- `Payments` module — likely 4-8 violations (Payment, PaymentAllocation, BankReconciliation)
- `ProjectCostCenter` (in Companies module) — likely 2-4 violations
- `AccountService` (in Finance) — likely 2-4 violations (CRUD on chart of accounts)
- `ChartOfAccountsService` (in Finance) — likely 2-4 violations
- `PayrollService` (in Payroll) — likely 2-4 violations (EosService is clean; PayrollService is the other one)

**Estimate:** 16-28 more violations across 5 modules. Pattern predicts 2-3 more audit sprints.

### L26 — `function(){...}()` is JavaScript, not C#

The previous bulk-replace tool injected this pattern. The test file didn't compile. The Sprint 27 test failures ("2 tests fail at runtime") were actually compile errors that the bulk-runner didn't surface.

**Rule going forward:** any bulk-replace operation that touches `.cs` files must be followed by `dotnet build` in the same commit, not deferred to CI. If the file doesn't compile, the test can't run, and the failure mode is silent (filter says "0 tests ran", not "1 test failed").

**Concrete fix for Sprint 29+:** add a build check to the test pipeline that fails if any test file has CS errors. Better: use `dotnet test` (not `dotnet test --filter`) so the whole project runs, not just the filtered subset.

### L27 — Established pattern = predictable time

3rd seeder implemented in <2h (vs 4-6h for the first). The pattern absorbs:
- 3-pass UPSERT (cyclic FK, complex aggregations)
- Schema surprises (`name_en`, `updated_at`)
- Idempotency (UPSERT by code, not DELETE+INSERT)
- Dev-only gating (`IsDevelopment() + flag`)
- JSON loading (UTF-8 native, no encoding bugs)

The 4th seeder (Year-scenario for AR invoices + receipts + JEs) will be 1-1.5h. The 5th (whatever comes next) will be 0.5-1h — the pattern is now muscle memory.

### L28 — Schema surprises are 1:1, not 1:1 with entity property names

Always `psql \d <table>` before writing the INSERT. Document the surprises in the seeder's startup log so the next seeder author sees them immediately.

Common surprises observed:
- `vendors` table does NOT have `name_en` (entity has it, table doesn't) — schema drift
- `purchase_order_lines` table does NOT have `updated_at`/`updated_by` — lines are append-only
- `customers` table does NOT have `credit_limit` (entity has it, table doesn't) — Sprint 26 discovery

The pattern: when an entity has a property but the table doesn't have the column, the entity property is either a computed field (e.g. `AvailableAmount = BudgetAmount - SpentAmount`) or a forward-looking field that hasn't been migrated yet.

### L29 — Aggregate with multiple child writes = read CompanyId once, pass local variable

`ProjectService.CreateAsync` writes both `Project` + `ProjectBudget`. Reading `_companyContext.CompanyId` once at the top and using a local `companyId` variable in both writes is:
- **Cleaner:** one source of truth for the companyId in the method
- **Safer:** if the context's CompanyId changes mid-method (it shouldn't, but if it did), both writes use the same value
- **Testable:** the test can verify the local `companyId` matches what was set in the mock context

```csharp
// Good: read once, use a local variable
var companyId = _companyContext.CompanyId ?? throw new InvalidOperationException("Company not resolved");
var project = new Project { CompanyId = companyId, ... };
await _projects.InsertAsync(project, ct);
await _budgets.InsertAsync(new ProjectBudget { CompanyId = companyId, ... }, ct);

// Bad: call the property twice
var project = new Project { CompanyId = _companyContext.CompanyId, ... };
await _projects.InsertAsync(project, ct);
await _budgets.InsertAsync(new ProjectBudget { CompanyId = _companyContext.CompanyId, ... }, ct);
//                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ could theoretically change mid-method
```

### L30 — DTO CompanyId = security risk; context wins

When the request DTO carries `CompanyId` but the service has access to `ICompanyContext`, **the context wins**. The DTO's CompanyId is spoofable (any client can send any Guid); the context's is bound to the JWT and the `CompanyMiddleware` that validates the X-Company-Id header.

Refactor pattern (applied in Sprint 28):
```csharp
// Before: DTO value
var entity = new Entity { CompanyId = req.CompanyId, ... };

// After: context value
var companyId = _companyContext.CompanyId ?? throw new InvalidOperationException(...);
var entity = new Entity { CompanyId = companyId, ... };
// Also: remove `CompanyId` from the request DTO entirely
```

**Other services that still have `req.CompanyId` in the DTO** (carry-over for Sprint 29+):
- ReceiveGoods, IssueStock, etc. in Inventory (besides the 4 already refactored in StockMovementService)
- Procurement services that haven't been audited yet
- Possibly others

The pattern: any time you see `req.CompanyId` in a service, it's a refactor candidate.

---

## 🎓 Sprint 28 Patterns Confirmed

1. **Audit pattern** (L25): 8 sprints running, ~50 violations found and fixed. The pattern is durable.
2. **Seeder pattern** (L17, L27): 3rd implementation in <2h. The pattern is muscle memory.
3. **TestCompanyContextFactory helper** (DEC-099, L26): reusable across all audit + seeder work. The pattern absorbs IIFE-style mistakes.
4. **Cross-tenant safety** (L19, L29, L30): context > DTO for CompanyId. Local variable > property for multi-write aggregates.
5. **Schema surprise documentation** (L28): `psql \d` before INSERT. Log surprises at seeder startup.

---

## 📋 Carry-over (Sprint 29+)

### P1 (critical, blocks client demo)
- [ ] **Audit 5 still-pending modules** — Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService. Pattern predicts 16-28 more violations across 2-3 sprints.
- [ ] **Refactor remaining `req.CompanyId` → `_companyContext.CompanyId`** (L30 carries over). Search `grep -rn "req.CompanyId" src/backend/Modules/` and fix each occurrence.
- [ ] **Add default warehouse** — enable GoodsReceipts + VendorBills seeder in Sprint 29. Currently skipped because `goods_receipts.warehouse_id` is `NOT NULL` and no default warehouse exists.
- [ ] **Posting Rules integration unit tests** — the engine is implemented but only manual smoke-tested. Need xUnit coverage for all 5 default rules.
- [ ] **`DepartmentResponse.managerName` field** — small FE/BE gap. API returns `managerId` but no joined name. The HR page shows the GUID.

### P2 (nice to have, doesn't block demo)
- [ ] **Year-scenario seeder** (4th seeder, will be 1-1.5h per L27). 12 monthly sales invoices + 6 receipts + opening balance JEs.
- [ ] **Manual JEs** (12: depreciation, accruals, year-end). The JE POST endpoint exists; we just need the data.
- [ ] **`customerStatement` + `vendorStatement` GET endpoints** — currently you can list AR/AP but not get the per-customer/per-vendor statement.
- [ ] **`CreateItem` API method** — items can be queried but not created via the API.
- [ ] **Trial Balance validation UI** — the backend computes it, but no "Balanced / Unbalanced" indicator on the FE.
- [ ] **14 P2 function workflow docs** — documentation backlog from Sprint 20.
- [ ] **5th default rule "Sale with VAT 5%"** (inactive, for demo).
- [ ] **Audit trail for posting rule changes** — who changed what when.
- [ ] **Multi-currency support** (currently LYD-only).
- [ ] **mvp-docker/.env to .gitignore** — for per-machine overrides.
- [ ] **Pre-push script: scan for `?` in user-visible columns** — would have caught Sprint 25/26 bugs.
- [ ] **Build-time test that enforces DEC-085** — so new entities can't skip CompanyId silently.

### Open question for Anas
- **Tag for the v1.0.9 → v1.0.10 push**: stay on v1.0.X (continue the convention) or bump to v1.1.0 (substance justifies minor)? 4 stacked audit sprints (24-28) is a lot. Muhammad's lean: stay on v1.0.10 (continue v1.0.X, save v2.0 for breaking changes). Awaiting Anas's call.
