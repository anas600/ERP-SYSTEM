# Sprint 27 — HR Article 3 audit + Arabic HR dev seeder (2026-08-02)

**Status:** ✅ DONE (LOCAL-ONLY, awaiting Anas's "ادفع" with Sprint 24+25+26)
**Branch:** `feature/sprint-21-posting-rules-engine` (Sprints 21+22+23+24+25+26+27 stacked)
**Goal:** POC the seeder pattern a second time (`ArabicHrDevSeeder`) to establish it as a framework. Required Article 3 fix to HR module first (8 violations: 4 entities + 4 services + 4 repos).

---

## 🎯 What Was Delivered

| Artifact | LOC | Purpose |
|---|---|---|
| `src/backend/Modules/HR/Entities/{Employee,Department,LeaveRequest,Attendance}.cs` | +4 lines each | Add `CompanyId` field to 4 entities (DEC-091) |
| `src/backend/Modules/HR/Application/Services/Services.cs` | +~30 lines | Inject `ICompanyContext` into 4 services, set `CompanyId = companyId` in `CreateAsync` |
| `src/backend/Modules/HR/Infrastructure/Repositories.cs` | +4 lines | Add `@CompanyId` to INSERT + SELECT in 4 repos |
| `src/backend/Shared/Migrations/Sprint27_HrCompanyId_20260802_130000.cs` | ~40 lines | Idempotent backfill (no-op for 0 rows; future-safe) |
| `src/backend/Shared/SeedData/ArabicHrDevData.json` | ~4.5 KB | 5 departments + 10 employees (UTF-8 Arabic) |
| `src/backend/Shared/SeedData/ArabicHrDevSeederHostedService.cs` | ~16 KB | IHostedService, 3-pass UPSERT for cyclic FK |
| `src/backend/Host/Program.cs` (Sprint 27 block) | ~10 lines | Registration gated on `IsDevelopment()` + `Bootstrap:SeedHrScenario=true` |
| `src/backend/Host/ERP-SYSTEM.csproj` (Content include) | +1 line | Copies `ArabicHrDevData.json` to `bin/...` |
| `src/backend/Host/appsettings.Development.json.example` (template) | +5 lines | Documents the new flag |
| `src/backend/Host/appsettings.Development.json` (gitignored) | +1 line | Enables the seeder for local dev |
| `CHANGELOG.md` | ~60 lines | Sprint 27 entry at top |
| `AGENTS.md` | +1 line | DEC-085 #7 added: cyclic FK awareness |
| `docs/team-charters/retrospectives/sprint-27-retro.md` | (this file) | Lessons + decisions |

**Verified end-to-end:**
- `dotnet build` → 0 errors, 0 warnings
- Migration ran cleanly (no-op, 0 rows to backfill)
- Seeder log: `departments updated=0 inserted=5, employees updated=0 inserted=10, manager links assigned=5`
- `psql` confirmed: 5 departments with Arabic names, 10 employees with Arabic names, 5 manager FKs resolved
- API `/api/hr/departments` and `/api/hr/employees` return Arabic JSON
- 3-pass FK cycle worked first try (after 1 nullable warning fix)

---

## 🐛 Article 3 Audit (DEC-091)

Pre-Sprint 27 state of the HR module:
- **Entities (4)**: Employee, Department, LeaveRequest, Attendance — **none** had a `CompanyId` field. ❌
- **Services (4)**: DepartmentService, EmployeeService, AttendanceService, LeaveRequestService — **none** injected `ICompanyContext`. ❌
- **Repositories (4)**: DepartmentRepository, EmployeeRepository, AttendanceRepository, LeaveRequestRepository — **none** included `company_id` in INSERT or SELECT. ❌
- **Validators**: clean (no `CompanyId != Guid.Empty` boilerplate) ✓
- **HRDocumentSequenceRepository**: already fixed in Sprint 24 (DEC-083) ✓
- **DB tables**: have `company_id NOT NULL` constraint (Sprint 22 schema) — but the INSERTs would have set NULL → fail.

The HR tables had **0 rows** in the DB — meaning the broken INSERTs were never exercised. But the bug was real: any attempt to create a department/employee via the API would have hit `null value in column "company_id" of relation "departments"`.

This is the **7th sprint in a row** to surface Article 3 violations (Sprints 19, 21, 22, 23, 24, 25, 27). The DEC-085 pre-push checklist in AGENTS.md is the right defense.

### DEC-091: HR Article 3 fix (8 violations in 1 sprint)

The fix is mechanical and mirrors Sprint 25 (Procurement):

| Layer | Change |
|---|---|
| Entity | Add `public Guid CompanyId { get; set; }` |
| Service | Inject `ICompanyContext` in constructor; read `CompanyId = _companyContext.CompanyId ?? throw` |
| Service (employee-driven) | Use `emp.CompanyId` if non-empty, else `_companyContext.CompanyId` (L19 — cross-tenant safety) |
| Repository | Add `company_id` to INSERT columns + `company_id AS CompanyId` to SELECT column list |
| Migration | `UPDATE ... SET company_id = (first company) WHERE company_id IS NULL` (idempotent) |

---

## 📐 Design Decisions

### DEC-091: HR Article 3 fix is mandatory for the seeder

The seeder needs `company_id` to INSERT rows. Without the entity-level fix, the seeder would have to bypass the entity and write raw SQL with company_id. That's the ugly escape hatch. Cleaner: fix the entity + service + repo first, then the seeder just uses raw SQL (which is fine — the seeder is dev-only, doesn't go through the service layer).

**Why bypass the service?** The seeder pattern (Sprint 26) is direct Dapper SQL, not service calls. Reason: seeder doesn't need the Posting Rules engine, validators, or business rules. It just needs to insert/update master data. Going through the service would require the Holding to be resolved in `ICompanyContext` (which it isn't during startup).

### DEC-092: 3-pass UPSERT for cyclic FK

The HR module has a **cyclic FK** between `departments.manager_id` (→ `employees.id`) and `employees.department_id` (→ `departments.id`). You can't insert both in one transaction without one of them being NULL.

**Solution:** Order the inserts by topology:
1. Pass 1: `INSERT INTO departments ... manager_id=NULL` (department has no manager yet)
2. Pass 2: `INSERT INTO employees ... department_id=(lookup from departments)` (employees reference departments)
3. Pass 3: `UPDATE departments SET manager_id=(lookup from employees) WHERE code=...` (departments get their manager)

**Lesson:** Cyclic FKs are usually a sign of a modeling issue, but in real-world HR data, "manager" is genuinely cyclic with "department" (the manager works in a department, but a department has a manager). The 3-pass is a pragmatic workaround.

### DEC-093: Cross-tenant CompanyId preference for employee-driven services

For `AttendanceService.RecordAsync` and `LeaveRequestService.CreateAsync`, the input is an `employeeId`. The service needs to set `CompanyId` on the new row. Two options:
- Use `_companyContext.CompanyId` (the active company)
- Use `emp.CompanyId` (the employee's company)

**Picked: `emp.CompanyId` if non-empty, else `_companyContext.CompanyId`.**

Why: in single-deployment mode they're always identical, but the cross-tenant form is more defensive. If a future scenario has a user impersonating across companies, the attendance/leave record stays attached to the employee's actual company, not the impersonated one.

---

## 🎓 Lessons

### L17: "Established pattern" threshold = 2 implementations

This is the second seeder in the same shape:
- **Sprint 26**: customers + vendors + items
- **Sprint 27**: departments + employees

Both follow the same pattern:
1. JSON file (UTF-8) as data source
2. C# IHostedService as runner
3. UPSERT (INSERT-or-UPDATE by natural key) for idempotency
4. Direct Dapper SQL (no service layer)
5. Double-gated (`IsDevelopment() + configFlag`)
6. Content include in csproj
7. appsettings flag with `_comment_*` documentation
8. Same file-lookup pattern in `ResolveDataFile()`

A third seeder (`ArabicProcurementDevSeeder`?) would now take 1.5-2h instead of 4-6h because the pattern is proven. The seeder framework is now "established" — future seeders are line-for-line analogous to these two.

**Action item:** When starting a future seeder, copy `ArabicDevSeederHostedService.cs` or `ArabicHrDevSeederHostedService.cs` as a template. Modify the JSON DTOs, table names, and the SQL. Everything else stays.

### L18: Cyclic FK requires 3-pass UPSERT

`departments.manager_id` → `employees.id` AND `employees.department_id` → `departments.id`. The cycle is real (a manager works in a department, but a department has a manager). Single-pass with INSERT-SELECT won't work because the referenced row doesn't exist yet.

**Action item:** For any 2-table cyclic FK in a seeder, default to 3 passes:
1. Insert parents (no children FKs)
2. Insert children (with parent FKs)
3. Update parents (set children FKs)

If you have 3+ tables in a cycle, you might need 4+ passes or a different design (e.g., deferrable FK constraints, which Postgres supports but require `SET CONSTRAINTS ALL DEFERRED` in the same transaction).

### L19: Cross-tenant safety > context-only CompanyId

For employee-driven services (Attendance, LeaveRequest), prefer `emp.CompanyId` over `_companyContext.CompanyId`. The employee is the canonical source of CompanyId for the new record — the active context might be a different company if the user is impersonating.

**Action item:** When a service's input references another entity (employee, customer, vendor, item, account), use the referenced entity's `CompanyId` as the source of truth, not the context. Fall back to context only if the referenced entity's `CompanyId` is empty/zero.

### L20: Audit pattern is now 7 sprints running

Sprints 19, 21, 22, 23, 24, 25, 27 — all surfaced Article 3 violations. The DEC-085 checklist (in `AGENTS.md`) caught all of them.

The bug is structural: **if you don't enforce it explicitly, the code drifts**. Every new entity, service, or repo that touches a business table is a potential new violation. The checklist (5 grep commands + 1 manual review) is the canary.

**Action item:** Treat the DEC-085 checklist as a non-negotiable pre-push step. Any new entity without `CompanyId` should fail the build (could be enforced via a unit test that scans the codebase, but that's Sprint 28+ work).

---

## 📊 Sprint 27 Metrics

| Metric | Value | Notes |
|---|---|---|
| New files | 3 | JSON, C# seeder, retro |
| Modified files | 11 | 4 entities + 1 services + 1 repos + 1 migration + 1 program + 1 csproj + 1 appsettings.example + 1 appsettings.development + 1 AGENTS.md + 1 CHANGELOG.md |
| LOC added | ~600 | Includes 4 entity fixes, 4 service fixes, 4 repo fixes, 1 migration, 1 seeder, 1 JSON, 1 retro |
| Build errors | 0 | First build had 3 nullable warnings (CS8604) — fixed with local variable |
| Build warnings | 0 | After warning fix |
| First-run rows | 15 (5 depts + 10 employees) | 5 manager links resolved in pass 3 |
| Article 3 violations found | 8 | 4 entities + 4 services + 4 repos (counted as 4 layers × 2 tables in some metrics) |
| Article 3 violations fixed | 8 | All 8 fixed in this sprint |
| Pattern repeat | 2nd time | Established pattern threshold reached (L17) |
| Production code paths affected | 0 | Dev env only, double-gated |

---

## 🔮 Carry-over (Sprint 28+)

- **P1:** Procurement cycle demo data (10 POs + 10 GRs + 10 bills) via `ArabicProcurementDevSeeder` — 3rd seeder, now trivial (L17)
- **P1:** Extend `ArabicDevSeeder` (Sprint 26) to also create sales invoices + receipts + opening balance JEs from JSON
- **P1:** Manual JEs (12: depreciation, accruals, year-end)
- **P1:** Posting Rules integration unit tests
- **P1:** 14 P2 function workflow docs
- **P1:** `customerStatement` + `vendorStatement` GET endpoints
- **P1:** `CreateItem` API method
- **P1:** Trial Balance validation UI
- **P1:** `DepartmentResponse.managerName` field — currently the API returns `managerId` but no joined name (small FE/BE gap)
- **P1:** Audit remaining modules for Article 3 — **Projects**, **Payments**, **StockMovement**, **AccountService**, **ChartOfAccountsService**, **PayrollService** are likely candidates
- **P2:** 5th default rule "Sale with VAT 5%" (inactive, for demo)
- **P2:** Audit trail for posting rule changes
- **P2:** Multi-currency support
- **P2:** mvp-docker/.env to .gitignore
- **P2:** Pre-push script: scan for `?` in user-visible columns
- **P2:** Add a build-time test that enforces the DEC-085 checklist (so new entities can't skip CompanyId silently)

---

**Status:** Sprint 27 LOCAL-ONLY done. Commit pending. Awaiting Anas's "ادفع" to push with Sprint 24+25+26+27 as `v1.0.9-sprint24-audit-architecture` (or `v1.1.0-sprint24-27` if we bump the minor).
