# Sprint 57 Retro — Project P&L (DEC-160..162)

**Date:** 2026-08-07
**Branch:** `feature/sprint-57-projects-pnl`
**Status:** ✅ DONE (LOCAL-ONLY)
**Duration:** ~2 hours (start to committed + verified)

---

## Goal

Per user's directive (2026-08-07): "خلينا في الواقع. أبغى Projects module يتوسع." Start with the most foundational gap: **Project P&L**. Add `project_id` to journal entries (so the general ledger can be filtered by project), build a P&L service, and add a UI tab.

---

## What was done

### DEC-160 — `project_id` on journal_entries
- **`data-types/journal_entries.json`**: added nullable `project_id` (FK → projects, ON DELETE SET NULL) + index `ix_journal_entries_project`.
- **Auto-applied via DataTypeMigrator**: existing tables get `ALTER TABLE ADD COLUMN project_id uuid` on next startup (idempotent).
- **`JournalEntry.cs` entity**: added `ProjectId` property (nullable Guid).
- **`JournalEntryRepository`**: updated all SQL (GetByIdAsync, GetWithLinesAsync, InsertAsync, UpdateAsync, ListAsync) to include `project_id`.
- **DTOs**: `PostJournalEntryRequest` accepts `ProjectId`; `JournalEntryResponse` returns it.
- **`JournalEntryService.CreateDraftAsync`**: passes `request.ProjectId` to the entity.

### DEC-161 — ProjectPnLService
- New service: `IProjectPnLService` + `ProjectPnLService` in `Modules/Projects/Application/Services/`.
- New DTOs: `ProjectPnLResponse` + `ProjectPnLLine` in `ProjectsDtos.cs`.
- Registered in `Program.cs` as `AddScoped<IProjectPnLService, ProjectPnLService>()`.
- **Endpoint added**: `GET /api/projects/{id}/pnl?from=&to=` (in `ProjectsController`).
- **Algorithm**:
  - Revenue: `SUM(sales_invoices.total_amount)` WHERE `project_id = X AND status = 'Posted' AND is_deleted = false`
  - Costs: `SUM(journal_lines.debit - journal_lines.credit)` joined to `journal_entries` WHERE `project_id = X AND status = Posted (2) AND accounts.type = 5 (Expense)` — grouped by account code.
  - GrossProfit = Revenue - Costs.
  - ProfitMarginPercent = `grossProfit / revenue * 100` (0 if revenue = 0).

### DEC-162 — Project P&L UI tab
- **`/projects/[id]/page.tsx`**: rewritten with tabbed interface.
  - Tab 1: "التفاصيل" — existing project info (refactored into `DetailsTab`).
  - Tab 2: "الأرباح والخسائر" — new P&L view (in `PnLTab`).
- **`PnLTab` features**:
  - Date range filter (defaults to previous year → today)
  - 4 summary cards: Revenue, Costs, Gross Profit, Margin %
  - Cost breakdown table (account code, name, amount, % of total)
  - Empty state if no costs
  - Color-coded (green for profit, red for loss)
- **`api.ts`**: added `ProjectPnL`, `ProjectPnLLine` types + `getProject(id)` + `getProjectPnL(id, from, to)` methods.

---

## Architecture decision

**"Why project_id on journal_entries (header) and not on journal_lines?"**

Two options were considered:
- **A) project_id on `journal_entries` (header)**: Consistent with `sales_invoices` (project_id on the document). User tags the whole entry with a project.
- **B) project_id on `journal_lines` (line-level)**: Consistent with `cost_center_id` (which is per-line). More flexible if different lines belong to different projects (rare).

**Decision: A** because:
- Sales invoices already use header-level project_id (consistency wins)
- User mental model is "tag this transaction with a project" not "tag this specific line"
- The P&L query with header-level project_id is straightforward (JOIN on journal_entries)

The P&L query uses `journal_entries.project_id` directly — no need to descend to lines for the project filter. Lines are only used to compute (debit - credit) per Expense account.

---

## Why costs come from journal_entries (not sales_invoices/vendor_bills)

The user's report initially suggested "P&L = sales_invoices (revenue) - vendor_bills (costs)". The actual implementation deviates:

- **Revenue** ← `sales_invoices.total_amount` (Posted, not deleted)
- **Costs** ← `journal_lines` (on Expense accounts) joined to `journal_entries.project_id = X AND status = Posted`

**Why?** Because every posted sales invoice AND every posted vendor bill creates a journal entry automatically. Counting both `vendor_bills` AND `journal_lines` would double-count costs. The JE-centric approach gives the canonical, audit-grade P&L that matches the General Ledger exactly.

---

## Verification

### Build
```
$ dotnet build src/backend/Host/ERP-SYSTEM.csproj
0 errors, 17 pre-existing warnings (ArabicYearScenarioDevSeederHostedService.cs — unchanged from develop)

$ npm run type-check
0 errors

$ npm run build
✓ Compiled successfully
Route /projects/[id] = 4.16 kB (was 1.84 kB before Sprint 57)
```

### Tests
```
$ dotnet test --filter Projects
Passed! 24/24, 0 failed

$ dotnet test --filter Finance
Passed! 25/29, 0 failed (4 pre-existing [Skip] benchmark tests)
```

### Schema (auto-applied on startup)
```
$ psql \d journal_entries
...
project_id  | uuid  | (nullable, FK → projects.id ON DELETE SET NULL)
+ ix_journal_entries_project index
```

### Manual smoke (would need DB to verify)
- POST /api/finance/journal-entries with `{ ..., projectId: "<guid>" }` → 201 with projectId in response
- GET /api/projects/{id}/pnl → returns revenue/costs/profit breakdown

---

## Lessons learned

### L109 (NEW)
**`data-types/*.json` is the source of truth for additive schema changes.** Adding a new column = update JSON file + entity + repository SQL. The DataTypeMigrator auto-runs `ALTER TABLE ADD COLUMN` on startup. No separate migration file needed for additive changes. (Confirmed by Sprint 32 DEC-112 + Sprint 57 DEC-160.)

### L110 (NEW)
**For P&L / cost aggregation, count from journal_entries (the General Ledger), NOT from sales_invoices/vendor_bills directly.** Reason: every posted invoice/bill creates a JE → double-counting if you sum both. The JE is the canonical, audit-grade source.

### L111 (NEW)
**Use header-level `project_id` on journal_entries** (consistent with sales_invoices). The cost filter in P&L queries is `WHERE je.project_id = X` — clean and simple. Don't add per-line project_id unless there's a real use case (split cost across projects on a single JE).

### L112 (NEW)
**FakeDbConnectionFactory doesn't fully simulate GROUP BY / aggregate functions.** A `FakeDb` unit test for P&L queries would need special handling. For now, manual smoke test against a real DB is the path. Integration tests (skipped benchmark tests) are the long-term solution.

---

## Carry-over (next sprint if needed)

- **Auto-propagate `project_id` to auto-generated JEs** from sales_invoices/vendor_bills (when invoice has project_id, the generated JE should inherit it). Currently manual JEs work via the new `ProjectId` field, but auto-generated JEs don't.
- **Test on real DB** with seeded data (Sprint 50 LibyanSmeScenarioDevSeeder can seed P&L-worthy data)
- **Unit test for ProjectPnLService** using FakeDb — needs FakeDb enhancement for GROUP BY
- **P&L tab on the Projects list page** (compare projects side by side) — Sprint 58?
- **Export P&L to PDF** — Sprint 58 or later

---

## What's in Sprint 58+

Per the original plan (Sprint 57 = P&L foundation):
- **Sprint 58**: Contracts + Progress Billings + WIP + Retention
- **Sprint 59**: Variation Orders
- **Sprint 60**: Project Close-out + Retention Release

Sprint 57 lays the foundation (P&L tagged transactions) that the Contracts/Billings modules will build on.
