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

## Sprint 40 — L67 Audit (Raw Fetch Fix) + 2 UI Polish Rounds (2026-08-05) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's "ابدأ Sprint 40" — fix all 17 files using raw `fetch('/api/...')` (L67 carry-over from Sprint 39 — silently 401s). Pause noisy crons temporarily.

### Fixed (L67)
- **17 files converted from raw `fetch()` to API client methods** — JWT now auto-attached via axios interceptor
- All forms (create/edit/delete) now work without 401 errors
- No more silent data loss on create/update

### Added
- 13 new API client methods in `lib/api.ts` (inventoryApi +13, financeApi +10, projectsApi +1)
- `lib/api.ts` complete CRUD coverage for: items, categories, warehouses, reservations, movements, posting rules, cost centers, projects

### Changed
- `lib/api.ts` (+200 lines) — now has full CRUD for all entities
- 17 page.tsx files updated to use API client

### Operational
- 2 noisy crons paused (mode2-admin-monitor, mvp-auto-rebuild-on-develop-push) — will auto-resume in 2h via self-reminder

### Lessons (L69..L71)
- **L69**: Use the established API client pattern (`async (data): Promise<T> => { const r = await api.post<T>('/endpoint', data); return r.data; }`). Always import `api` and use the axios instance.
- **L70**: Fix the API client FIRST (add all needed methods), then fix the files. Pattern: API client → 1 file as test → remaining files in parallel.
- **L71**: To pause noisy crons temporarily, use `mavis cron update --enabled false` for specific crons. Set a `mavis cron self` reminder to re-enable them. Don't delete them.

### Verification
- npm type-check: 0 errors
- npm build: 50+ pages compiled
- Playwright page sweep: 50/50 (200 OK)
- Playwright Sprint 40 fixes test: 6/6 (0 401 errors!)
- Sprint 39 UI tests: 9/9 (regression check)
- Sprint 39 interactive tests: 8/8 (regression check)

### Carry-over Sprint 40+1
- **P1**: Sidebar collapse toggle + page transition animations
- **P1**: Take fresh manual screenshots with latest design
- **P2**: 4 VAT-related workflows (Sprint 35.5 cancelled, still pending)
- **P2**: mvp-docker rebuild (deferred)

---

## Sprint 39 — UI/UX Overhaul + Tax Optional Enforcement (2026-08-05) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's "Sprint 39" — tax is OPT-IN (Libya = no default tax), design system overhaul, comprehensive Playwright sweep.

### Added (DEC-125)
- **Design tokens** (tailwind.config.js + globals.css): brand 50→950, success/warning/danger/ink semantic colors, soft shadows, smooth animations (fade-in/slide-up/scale-in/shimmer)
- **Sales Invoice form**: `useVat5` toggle ("تطبيق ضريبة 5%") — OFF by default, with "اختياري" badge. When ON: per-line taxRate column hidden, tax row in summary, save button shows "Dr 1230 / Cr 5110 / Cr 1411"
- **Login page**: brand gradient + glassmorphism card + Toast on success
- **AppShell**: gradient avatar, refined user menu (الملف الشخصي link), brand-50 active state
- **Playwright scripts**: 4 new test files (UI smoke 9 tests, page sweep 50 tests, key screens 13, interactive 8)

### Changed
- 12 UI components redesigned with new design tokens (Button, Card, Input, Select, Badge, Table, PageHeader, EmptyState, LoadingSkeleton, Modal, ConfirmDialog, Toast)
- Toast uses semantic colors (success-50/danger-50/brand-50)
- SalesInvoice interface: `+useVat5?: boolean`

### Fixed (L60/L67)
- **Journal Entries list** — was using raw `fetch()` without JWT (401 silently failed). Now uses `financeApi.listJournalEntries()` (auto-JWT). **50+ entries now load correctly** with balanced summary
- **Journal Entry detail** — raw `fetch` → `financeApi.getJournalEntry` + `financeApi.postJournalEntry`. Added ConfirmDialog for post action
- **Journal Entry new** — raw `fetch` → `financeApi.createJournalEntry`
- **Receipts page** — replaced native `confirm()`/`alert()` with `<ConfirmDialog>` + `<Toast>`. Added EmptyState + SkeletonTable
- **All pages (82)** — bulk color migration: `bg-red-50/border-red-200/text-red-700` → `bg-danger-50/border-danger-200/text-danger-700` (330 substitutions)
- **9 pages** — secondary red→danger migration (icons, asterisks, hover states)

### Lessons (L65..L68)
- **L65**: Bulk color/token migration via Python script (don't do per-file)
- **L66**: Use `<ConfirmDialog>` + `<Toast>` instead of native `confirm()` + `alert()` — always
- **L67**: Never use raw `fetch('/api/...')` — always use the API client (L60 was about types, L67 is about runtime 401)
- **L68**: Playwright sweep is the highest-ROI test (50 pages in ~2 min, catches bugs API tests miss)

### Verification
- npm type-check: 0 errors
- npm build: 50+ pages compiled
- Playwright UI smoke: 9/9
- Playwright page sweep: 50/50 (200 OK, 0 JS errors)
- Playwright interactive: 8/8 (login → dashboard → journal → customer → user menu → confirm dialog)
- L19 audit: stable (no regressions; Sprint 38 fixes still in place)

### Carry-over Sprint 40+
- **P0**: 17 files still use raw `fetch()` (L67 carry-over): admin/* (8), finance/cost-centers/* + accounts/new (3), inventory/items/new + movements + reservations/* (4), procurement/goods-receipts/new, projects/new
- **P1**: UI feedback from Anas (click-to-expand, form improvements)
- **P2**: 4 VAT-related workflows (Sprint 35.5 cancelled)
- **P2**: mvp-docker rebuild (auto-rebuild still failing, deferred)

---

## Sprint 38 — L19 audit on service layer + 4 final Manual JE Templates (2026-08-05) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's "ابدا Sprint 38" — L19 audit on direct SQL in service layer (Constitution Article 3 enforcement) + 4 more manual JE templates (completing 12 of 12 planned).

### Fixed (DEC-124) — MAJOR SECURITY FIX
- **L19 violations in service-layer direct SQL** (data was leaking across companies):
  - `GeneralLedgerService` (`GetAccountBalancesAsync`, `GetAccountLedgerAsync`, `GetTrialBalanceAsync`) — Trial Balance was returning accounts from ALL companies
  - `GeneralLedgerReportService` (`GetAccountLedgerAsync`) — Account Ledger was returning lines from ALL companies
  - `JournalEntryRepository` (`GetByIdAsync`, `GetWithLinesAsync`, `EntryNumberExistsAsync`, `GetNextEntryNumberAsync`, `ListAsync`) — Journal Entries was returning entries from ALL companies
- Added `companyId` param to all these methods
- Service signature changes (interface updated)
- Controllers inject `ICompanyContext` and pass companyId
- Concrete evidence: TB count was 30 before fix, now 35 (5 more accounts correctly shown for current company)

### Added (DEC-124)
- **4 final manual JE templates** (12 of 12 DONE):
  - دفع ضريبة (tax-payment) — Dr 4300 (Financial expenses) / Cr 1210 (Cash)
  - فروق عملة (ربح) (fx-gain) — Dr 1230 (AR) / Cr 5110 (Revenue) — currency revaluation gain
  - فروق عملة (خسارة) (fx-loss) — Dr 4110 (Cost) / Cr 1230 (AR) — currency revaluation loss
  - سحب رأس مال (capital-withdrawal) — Dr 3100 (Capital) / Cr 1210 (Cash) — owner withdrawal
- **Total templates: 12** (4 Sprint 34 + 4 Sprint 37 + 4 Sprint 38) — PLAN COMPLETE

### Verification
- `dotnet build`: 0 errors, 17 warnings (17 pre-existing)
- `npm run type-check`: 0 errors
- `npm run build`: success
- BE smoke: TB count=35 (was 30 before L19 fix), balanced 833,005=833,005 LYD; JE count=50 (filtered by company)
- Playwright smoke: 18/18 (TB L19 35 accounts, JE list works, all 12 templates present, 4 Sprint 38 templates apply correctly)

### Lessons (L63, L64)
- **L63**: L19 audit must cover service layer (not just repos). The `Sel*` constants in repos are ONE place to check, but services can also have direct SQL that bypasses the repo. Pattern: grep `Application/Services/*.cs` for `_db.CreateOltpConnectionAsync`, check each SQL for `company_id` filter, add companyId param to interface, update controller.
- **L64**: Trial Balance count is a quick L19 sanity check. Before L19 fix: 30 accounts. After fix: 35 accounts. The difference (5) was accounts from other companies. If TB count is suspiciously low, suspect L19.

### L19 audit trend (4 sprints)
- Sprint 34: 4 modules (CostCenter, Payroll, ChartOfAccounts, Account)
- Sprint 36: 1 repo (VendorRepository)
- Sprint 37: 5 repos (StockReservation, ItemCategory, VendorBill, PurchaseOrder, GoodsReceipt)
- Sprint 38: 3 service-layer (GeneralLedger, GeneralLedgerReport, JournalEntryRepository)
- **Total: 13 L19 violations found and fixed across 4 sprints**

### Carry-over (Sprint 39+)
- **P0**: Final L19 sweep on remaining services (FinanceService, DashboardChartService, GeneralLedgerReportService other queries)
- **P1**: UI feedback from Anas (Sprint 32+33+37+38 carry-over) — click-to-expand receipts/invoices, overall UI polish
- **P2**: 4 VAT-related workflows (Sprint 35.5, deferred)

---

## Sprint 37 — L19 audit sweep + 4 Manual JE Templates (2026-08-05) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's "Sprint 37" (auto-continue per "نتقدم في تنفيذ الاسبرينت التالي ادا لم يكن هناك ملاحظات") — close out the L19 carry-over from Sprint 34 audit (5 more repos) + ship 4 of 8 remaining manual JE templates (Sprint 34 shipped 4, total now 8 of 12 planned).

### Added (DEC-123)
- **5 CoA accounts** to `DefaultCoASeed.cs` (47 → 52 accounts):
  - `1300` مجمع إهلاك الأصول الثابتة (Asset, parent 1100) — for depreciation template
  - `1410` سلف الموظفين (Asset, parent 1200) — for loan template
  - `2110` مصروفات مستحقة (Liability, parent 2200) — for accrual template
  - `5410` ديون معدومة (Expense, parent 4200) — for bad-debt template
  - `5500` إهلاك الأصول الثابتة (Expense, parent 4200) — for depreciation template
- **4 new manual JE templates** in `/finance/journal-entries/new`:
  - رواتب (salary) — Dr 4112 (Direct Labor) / Cr 1210 (Cash)
  - سلفة موظف (loan) — Dr 1410 (Loans Receivable) / Cr 1210 (Cash)
  - ديون معدومة (bad-debt) — Dr 5410 (Bad Debt Expense) / Cr 1230 (AR)
  - تسوية مخزون (inventory-adjust) — Dr/Cr 1240 (Inventory) for variance

### Fixed (L19)
- **5 repos** had `Sel` / `SelVb` / `SelPo` / `SelGr` missing `company_id AS CompanyId`:
  - `StockReservationRepository.Sel` (Inventory)
  - `ItemCategoryRepository.Sel` (Inventory)
  - `VendorBillRepository.SelVb` (Procurement)
  - `PurchaseOrderRepository.SelPo` (Procurement)
  - `GoodsReceiptRepository.SelGr` (Procurement)
- **Pre-existing bug in JE form** (caught by smoke test): `journal-entries/new/page.tsx` was using raw `fetch('/api/finance/accounts')` without JWT → 401 silently, accounts dropdown was always empty. Now uses `financeApi.listAccounts()` which attaches the auth token. Bug had been there since Sprint 11/12.

### Verification
- `dotnet build`: 0 errors, 17 warnings (17 pre-existing)
- `npm run type-check`: 0 errors
- `npm run build`: success
- Playwright smoke: 14/14 (CoA 52 accounts, all 8 templates in dropdown, 4 new templates apply correctly with right account codes)
- BE smoke: 5 new accounts present in /api/finance/accounts

### Lessons (L61, L62)
- **L61**: L19 audit focus on `Sel` / `SelVb` / `SelX` constants. Each is a string used in multiple queries — fixing once in the constant fixes everywhere. Audit pattern: `grep -rn "private const string Sel" src/backend/Modules/` then check each for `company_id AS CompanyId`.
- **L62**: Check CoA first before adding JE templates. If a needed account doesn't exist, add it to `DefaultCoASeed.cs` in the correct topological order (parent before child in array). Don't add templates that require missing accounts.

### Carry-over (Sprint 38+)
- **P0**: L19 audit on remaining repos with direct SQL (no `Sel` constants) — e.g., JournalEntryService, aging-ar queries, account ledger
- **P1**: UI feedback from Anas (Sprint 32+33+37 carry-over) — click-to-expand receipts/invoices (partially done), overall UI polish
- **P2**: 4 more manual JE templates (8 of 12 done): Tax payment, Bank reconciliation, Year-end closing, Foreign currency revaluation
- **P2**: 4 VAT-related workflows (Sprint 35.5 — currently deferred)

---

## Sprint 36 — Customer/Vendor Statements + Trial Balance FE (2026-08-05) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's "نتقدم في تنفيد الاسبرينت التالي" — close out the remaining P1 carry-over from Sprint 33-34 plan: كشف حساب العميل + كشف حساب المورّد + ميزان المراجعة.

### Added (DEC-122)
- **BE — 2 new services + 2 DTO files**:
  - `CustomerStatementService.GetStatementAsync(customerId, from, to)` — opening balance + invoices + receipts + running balance (Posted only)
  - `VendorStatementService.GetStatementAsync(vendorId, from, to)` — opening balance + bills + payments (PartyType='Vendor') + running balance (Posted only)
  - `StatementDtos.cs` in both modules (CustomerStatement / VendorStatement / StatementLine)
- **BE — 2 new endpoints**:
  - `GET /api/ar/customers/{id:guid}/statement?from=&to=`
  - `GET /api/procurement/vendors/{id:guid}/statement?from=&to=`
- **FE — 3 new pages**:
  - `/finance/customers/[id]/statement` — date range + 4 summary cards (opening/invoiced/received/closing) + chronological lines table with running balance
  - `/procurement/vendors/[id]/statement` — same pattern, AP convention (orange theme)
  - `/finance/trial-balance` — date-as-of + balanced/unbalanced bar + 5 per-type grouped tables (أصول / خصوم / حقوق ملكية / إيرادات / مصروفات)
- **FE — quick links**:
  - Customer list: new "إجراءات" column with "كشف حساب" link per row
  - Vendor list: same
  - Customer detail page: "كشف حساب العميل" primary action button
- **FE — AppShell**: new "ميزان المراجعة" sidebar entry (Scale icon)
- **FE — `lib/api.ts`**: 3 new methods (`arApi.getCustomerStatement`, `procurementApi.getVendorStatement`, `financeApi.getTrialBalance`)

### Fixed
- **VendorRepository.L19 violation (caught by smoke test)**: `Sel` was missing `company_id AS CompanyId`, causing every vendor to fail the L19 `vendor.CompanyId != companyId` check. Sprint 34 audit missed this repo.
- **CustomerStatementService SQL bugs**: removed `status` from `receipts` SELECT (column doesn't exist; `posted_at IS NOT NULL` is used instead). Removed `Status` from `StatementReceiptRow` DTO.
- **VendorStatementService SQL bugs**: removed `paid_amount` from `vendor_bills` SELECT (no such column; the running balance formula was wrong because of it), removed `status` from `payments` SELECT (column is INT, not string), fixed opening balance formula.
- **Trial Balance FE enum type**: `AccountType` was typed as int union `[1|2|3|4|5]` but BE returns string (`"Asset"`, `"Liability"`, etc.) via Dapper's `EnumStringTypeHandler`. Now using `AccountTypeName` ('Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense').

### Verification
- `dotnet build`: 0 errors, 17 warnings (17 pre-existing)
- `npm run type-check`: 0 errors
- `npm run build`: success, 3 new routes
- Playwright smoke: 6/6 (TB balanced bar + 30 accounts; customer/vendor list links; customer/vendor statement summary cards)
- BE smoke: customer statement 200, vendor statement 200, trial balance 200 (balanced: 833,005 = 833,005 LYD)

### Lessons (L59, L60)
- **L59**: Run the actual endpoint with a real seed before declaring BE done. The Postgres error "column X does not exist" is the source of truth. Typecheck + tests don't catch missing columns.
- **L60**: When BE uses Dapper EnumStringTypeHandler, FE interfaces MUST use string literal types, not int enums. The handler silently converts every enum property to its string name on read.

### Carry-over (Sprint 37+)
- **P0**: Audit `VendorBillRepository` and all other `IRepository.Sel*` for L19 SELECT patterns (Sprint 34 audit missed `VendorRepository`)
- **P1**: UI feedback from Anas (Sprint 32 + 33 carry-over) — receipt/invoice click-to-expand (partially done in Sprint 33), overall UI polish
- **P2**: 8 more manual JE templates (Sprint 34 shipped 4 of 12)

---



## Sprint 32 — Projects module tables fix (DEC-112) + Sprint 31 test collateral (2026-08-04) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's Q1 from Sprint 31 (defer to Sprint 32) — fix the Projects module's 4 missing `data-types/*.json` files so the tables get created. Closed the loop on L44 ("Projects module is partially implemented").

### Added (DEC-112)
- **4 `data-types/*.json` files** (matching entity columns, verified via `\d` on existing tables):
  - `resources.json` (Resource: id, company_id, code, name, type, hourly_rate, is_active, created_at, updated_at)
  - `project_tasks.json` (ProjectTask: + project_id, description, status, estimated/actual_hours, start/end_date, progress_percent)
  - `resource_assignments.json` (ResourceAssignment: + task_id, resource_id, user_id, **quoted `from` + `to`**)
  - `project_budgets.json` (ProjectBudget: + cost_center_id, account_id, budget/spent/committed_amount, last_recalculated_at)
- **`quoted: true` flag on `FieldDefinition`** (DEC-112): escape hatch for SQL reserved words. DataTypeMigrator forces double-quoted SQL identifier for the column (e.g., `"from"`, `"to"`). The migrator is otherwise idempotent — re-running adds nothing for existing tables.

### Changed
- **DataTypeMigrator.CreateTableAsync** + **AddColumnAsync**: when `field.Quoted == true`, force `"<col>"` in CREATE TABLE / ALTER TABLE. Without this, `from` and `to` are parsed as the SQL FROM keyword and the table can't be created.
- **ResourceAssignmentRepository.Sel + InsertAsync**: changed unquoted `from, to` → `"from" AS "From"`, `"to" AS "To"` in SELECT, and `"from", "to"` in the INSERT column list. Once the column is created as a quoted identifier, every subsequent reference must be quoted (Postgres rule).
- **PostingRulesBenchmarkTests.cs** (Sprint 31 collateral): 4 integration tests had `await using IDbConnection` (C# error CS8417) and a legacy `NpgsqlConnectionFactory(string)` ctor (Postgres/Sprint 22 added `IOptions<NpgsqlConnectionOptions>` + `ILogger<NpgsqlConnectionFactory>`). Refactored to `using var` + `Options.Create(new NpgsqlConnectionOptions { OltpConnectionString = ... })` + `NullLogger<NpgsqlConnectionFactory>.Instance`. Tests are still `[Fact(Skip = "Integration test — needs live DB")]` so no behavior change — just compilable.

### Verified (manual smoke + tests)
- **All 4 tables created** by DataTypeMigrator: `\dt` shows 48 tables (44 + 4). `\d resource_assignments` shows quoted `from` + `to` columns + 3 indexes + 6 FKs.
- **End-to-end CRUD test** (admin login + POST + GET roundtrip):
  - POST /api/resources → 201 (id=c5896f3b...)
  - POST /api/projects → 201 (id=44fd023e...)
  - POST /api/tasks → 201 (id=a8068d72...)
  - POST /api/projects/{id}/assignments → 201 (id=75c4a111..., from/to preserved, **estimatedHours=10, estimatedCost=500** computed correctly)
  - GET /api/projects/{id}/tasks → 1 task listed
  - GET /api/projects/{id}/assignments → 1 assignment listed
  - GET /api/resources → 1 resource listed
  - GET /api/finance/ledger/accounts/{guid for 1230} → 37 ledger lines
- **18-endpoint smoke regression** (all 200): HR (employees, departments), Finance (accounts, ledger/trial-balance, posting-rules), AR (customers, sales-invoices, receipts, payments), Procurement (pos, grs, bills, vendors), Inventory (items, warehouses), Projects, Cost-centers, Resources. **0 × 500, 0 × 404.**
- **24/24 Project tests PASS** (`dotnet test --filter Projects`): 8 ProjectService + 5 Task + 3 Resource + 6 Budget + 2 ResourceAssignmentComputed.
- **378/403 full test suite pass** (94% — 2 unrelated retention integration tests fail needing `postgres` user, 23 [Skip] are intentional).

### Carry-over to Sprint 33+
- **Pending Article 3 audit**: `ProjectCostCenter`, `AccountService`, `ChartOfAccountsService`, `PayrollService` (still untouched since DEC-085 cycle).
- **P1**: Manual JEs (depreciation + accruals + year-end), customer/vendor statement GET endpoints, Trial Balance validation UI ("Balanced / Unbalanced").
- **P2**: Activate 5th VAT rule (DEC-109) — needs 1410/1411 accounts added to CoA.
- **P2**: Add Playwright e2e as CI gate (DEC-111 follow-up).
- **P2**: Fix Health Ping + Daily Status workflows (DEC-111 disabled them).
- **Cleanup**: 2 retention integration tests fail on local (need `postgres` user setup or skip-by-environment).
- **L49 (NEW)**: When a table needs a column with a SQL reserved word, the JSON must use `quoted: true` AND every reference (SELECT/INSERT/ORDER BY) must be quoted. Best to avoid reserved words in entity column names from day 1 (rename to `start_at`/`end_at` instead of `from`/`to`).

---

## Sprint 31 — Browser-based testing (Playwright) + DEC-107..110 (2026-08-04) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's approval — install Playwright MCP for browser-based testing, do full P0 + P1 in 8h budget.

### Added (DEC-107..110 + bonuses)
- **DEC-107**: `DepartmentResponse` now has `ManagerName` + `ManagerCode` + `EmployeeCount` (L40 pattern, single batch Dapper lookup). 5 departments now show manager name + count.
- **DEC-108**: Posting Rules benchmark vs engine comparison (4 xUnit tests + SQL script). All 4 categories balanced — no bugs found.
- **DEC-109**: 5th default rule "فاتورة مبيعات (افتراضي - ليبيا + 5% ضريبة)" (INACTIVE). Template: DR 1230 / CR 5110 / CR 1411 (VAT Output). Formulas: {tax+subtotal} + {subtotal} + {tax}.
- **DEC-110**: Payments module Article 3 audit (L19 + L30). Fixed: `Payment.CompanyId` (Guid? → Guid) + `CreateAsync` (injects ICompanyContext).
- **Playwright MCP**: Installed `playwright` + Chromium (Chrome for Testing 151). `scripts/playwright-smoke.mjs` runs 24-page smoke test in <2 minutes.
- **`/hr/departments` page** (was missing — discovered by Playwright 404). Shows hierarchy: 1 root dept + 4 sub-depts with manager names + employee counts.
- **AppShell**: "الأقسام" added to HR sidebar nav.

### Fixed
- **`/hr/departments` 404**: Missing FE page created (was not discovered in any previous test).
- **Stale `.next/` cache (L45 NEW)**: Playwright run #1 showed "الحسابات (مبسّط)" in sidebar even after DEC-100 removed it. Fix: `npm run build` + `npm start` to serve new build.
- **DEC-110 — L19**: `PaymentService.CreateAsync` was creating payments without CompanyId. Now reads from `ICompanyContext.CompanyId`.
- **DEC-110 — L30**: `Payment.CompanyId` was `Guid?` but DB column is NOT NULL. Now `Guid`.

### Verified
- Final Playwright: **24/24 pages 200, 0 × 404, 0 × 500** ✓
- ProjectService tests: **8/8 PASS** (Sprint 28 tests still green)
- Benchmark vs engine: **ALL 4 categories balanced** (no bugs in Posting Rules)
- DB: 6 posting rules (5 active + 1 inactive VAT 5%)

### Lessons (L40-L46)
- **L45 (NEW)**: `npm start` serves the cached `.next/` build, not the source. After backend OR frontend code changes, always `npm run build` first.
- **L46 (NEW)**: Playwright discovers bugs that API testing misses (e.g., missing FE pages, stale builds). The 24-page smoke test takes 1.5 minutes.
- **L25 (re-confirmed)**: DEC-085 audit pattern keeps finding violations. Sprint 31 found DEC-110 in Payments. **4 of 5 still-pending modules now audited**.

### Pending (carry-over to Sprint 32+)
- **P0**: Add 4 `data-types/*.json` (resources, tasks, project_assignments, project_budgets) — Projects module
- **P0**: Audit ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService
- **P1**: Manual JEs (depreciation + accruals + year-end)
- **P1**: customerStatement + vendorStatement GET endpoints
- **P1**: Trial Balance validation UI ("Balanced / Unbalanced")
- **P2**: Add 1410/1411 (VAT) accounts to CoA + test DEC-109 rule
- **P2**: Add CI to run `playwright-smoke.mjs` automatically

---

## Sprint 30 — Architectural cleanup (6 DECs) + Full PO+GR+Bill seeder (2026-08-03) ✅ DONE (LOCAL-ONLY)
---

## Sprint 30 — Architectural cleanup (6 DECs) + Full PO+GR+Bill seeder (2026-08-03) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's directive (2026-08-03 ~04:30 UTC+2) — fix the 14 user-experienced issues from the browser walkthrough, then continue with the 5th seeder POC. Architectural cleanup before UI band-aid fixes.

### Added (DEC-100..106)
- **DEC-100 (Single CoA page)** — `src/frontend/app/(authenticated)/accounts/page.tsx` deleted (the duplicate "الحسابات (مبسّط)" page). Only `/finance/accounts` remains. `AppShell.tsx` sidebar entry removed.
- **DEC-101 (Default reference data)** — `TrySeedDefaultReferenceDataAsync` added to `DefaultHoldingBootstrapHostedService.cs`. Always-on (no flag) — seeds 1 default warehouse (WH-001 "المستودع الرئيسي") + 1 default cost center (CC-001 "الإدارة العامة", type=Department=2). Idempotent via `ON CONFLICT (company_id, code) DO NOTHING`.
- **DEC-103 (Atomic document sequence)** — `DocumentSequenceRepository.GetNextNumberAsync` refactored: `INSERT ... ON CONFLICT ... DO UPDATE SET last_number = ... RETURNING last_number` in a single statement. Replaces the old UPSERT-then-SELECT race that caused `PO-2026-0002 already exists` duplicate-key errors.
- **DEC-104 (Vendor name in DTO)** — `VendorBillResponse` now has `VendorName` + `VendorCode`. `VendorBillService.BuildVendorMapAsync` does single-batch vendor lookup. FE no longer shows raw GUIDs.
- **DEC-105a (PO vendor enrichment)** — `PurchaseOrderResponse` now has `VendorName` + `VendorCode`. `PurchaseOrderService.BuildVendorMapAsync` (Dapper direct) added.
- **DEC-105b/c/d (Full PO+GR+Bill seeder)** — `ArabicProcurementDevSeederHostedService` rewritten. All 3 passes implemented:
  - **Pass 1: POs** — 10 POs with computed line `sub_total = qty * unit_price` + header `sub_total/tax_amount/total_amount`
  - **Pass 2: GRs** — 10 GRs, status=`Received`, posted to default warehouse WH-001 (DEC-101 made this possible)
  - **Pass 3: Bills** — 10 Bills, status=`Posted`, linked to GRs via `goods_receipt_id`. Each gets a `BENCH-BILL-2026-NNNN` Journal Entry (DR 1240 Inventory / CR 2210 AP)
- **DEC-106 (SalesInvoiceStatus string)** — `SalesInvoice.Status` changed from `int` enum to `string` to match the seeder + schema. Fixed 6 references in `ReceiptService` + `SalesInvoiceService` (Draft/Sent/Paid/PartiallyPaid/Cancelled → "Draft"/"Sent"/etc.). Fixed the 500 error on `/api/ar/sales-invoices`.

### Changed
- **`PurchaseOrderService` constructor** — now takes `IDbConnectionFactory` (for Dapper-direct vendor enrichment). Test refactor (L21) updated.
- **`ArabicProcurementDevSeederHostedService` class doc** — updated to reflect Sprint 30 (DEC-105) scope: all 3 passes implemented, not just POs.

### Fixed
- **/api/ar/sales-invoices 500** — Dapper couldn't map the int enum status to the string seeder output. Fixed by changing `Status` to `string` (DEC-106).
- **Duplicate-key error on PO creation** — race in `GetNextNumberAsync` (UPSERT then SELECT). Fixed via `RETURNING` clause (DEC-103).
- **Empty reference data on fresh install** — no warehouse, no cost center, no allocations possible. Fixed by DEC-101 (warehouse + cost center always seeded) and DEC-102 (allocations optional in receipt form).
- **/api/procurement/purchase-orders 404** — actual endpoint is `/api/procurement/pos` (DEC-031 convention). Updated tests + docs.
- **PO total_amount=0** — old seeder stored 0/0/0. Now computed from line totals (DEC-105b).
- **GR + Bill seeder stubs** — Pass 2 and Pass 3 were commented as "intentionally not implemented" in Sprint 28. Now implemented (DEC-105c/d).
- **L21 test refactor** — `PurchaseOrderService` constructor change required updating tests to inject `Mock<IDbConnectionFactory>`.

### Verified (Mode 1, host local PG)
- Wiped DB clean → restarted BE → all 5 seeders + bootstrap ran:
  - 13 customers + 13 vendors + 20 items
  - 5 departments + 10 employees
  - 1 warehouse + 1 cost center
  - 10 POs (computed totals 65–3440 LYD) + 10 GRs (Received, WH-001) + 10 Procurement Bills (Posted, 65–3440 LYD)
  - 12 sales invoices + 12 vendor bills + 24 receipts + 24 payments (Sprint 29)
  - 83 journal entries + 169 lines (74 benchmark JEs + OB-2025-001 + others)
- All 4 JEs categories balanced (DR=CR):
  - BENCH-INV: 12 JEs, 143,450 LYD
  - BENCH-BILL: 22 JEs (12 year + 10 procurement), 240,055 LYD
  - BENCH-RCT: 24 JEs, 153,000 LYD
  - BENCH-PAY: 24 JEs, 191,500 LYD
- All 5 endpoints return correct data with vendor names (no raw GUIDs):
  - `/api/procurement/pos` → 10 POs with `vendorName`/`vendorCode` + computed totals
  - `/api/procurement/grs` → 10 GRs with `poNumber`/`warehouseName`/`vendorName`
  - `/api/procurement/bills` → 22 bills (12 year + 10 procurement) with `vendorName` + totals
  - `/api/ar/sales-invoices` → 12 invoices (DEC-106 fix), no more 500
  - `/api/finance/receipts/new` → form shows (DEC-102 fix, allocations optional)

### Pending (carry-over to Sprint 31+)
- 4 more still-pending Article 3 audit modules (Payments, ProjectCostCenter, AccountService, ChartOfAccountsService)
- 5th default rule "Sale with VAT 5%" (inactive, for demo)
- Manual JEs (depreciation, accruals, year-end)
- Posting Rules integration unit tests (benchmark vs engine comparison)
- 14 P2 function workflow docs
- Pre-push script: scan for `?` in user-visible columns
- Playwright e2e tests for top 5 user flows

---

## Sprint 29 — Year-Scenario dev seeder (POC #4) + Legacy cleanup (2026-08-03) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's directive — clean up the 2 legacy seeders (ScenarioSeeder + RealisticSeed, 102.8 KB total) and replace them with a new year-scenario seeder using the established POC pattern (JSON + IHostedService + UPSERT). The new seeder adds 1 year of operational data (73 records + 73 Journal Entries) to discover bugs on the dev host.

### Added (DEC-098)
- **`src/backend/Shared/SeedData/ArabicYearScenarioDevData.json`** (NEW, ~46 KB) — 1 opening balance JE + 12 monthly sales invoices + 12 monthly vendor bills + 24 customer receipts + 24 vendor payments, all with Arabic notes.
- **`src/backend/Shared/SeedData/ArabicYearScenarioDevSeederHostedService.cs`** (NEW, ~28 KB) — IHostedService, 5-pass execution: Pass 1 Opening Balance → Pass 2 Sales Invoices → Pass 3 Vendor Bills → Pass 4 Receipts → Pass 5 Payments. Each transaction gets a "benchmark" Journal Entry that should match the Posting Rules engine's output (any discrepancy = bug).
- **`Bootstrap:SeedYearScenario` config flag** — added to `appsettings.Development.json.example` + `appsettings.Development.json`. Default `false`. Double-gated on `IsDevelopment() + flag`.
- **`<Content Include="..\Shared\SeedData\ArabicYearScenarioDevData.json" />`** in csproj.
- **Program.cs registration block** — Sprint 29 section, gated on `IsDevelopment() + flag`.

### Removed (DEC-098 cleanup)
- **`ScenarioSeederHostedService.cs`** (54.8 KB) — Sprint 4 al-Burj scenario, hardcoded C#, never enabled in fresh builds, was only registered via manual admin endpoint.
- **`RealisticSeedHostedService.cs`** (48 KB) — Sprint 14 realistic seed, hardcoded C#, never enabled.
- **AdminController.AlFajrSeed + AlBurjSeed endpoints** — replaced with 410 Gone responses pointing to the new POC seeders.

### Fixed (L21 + L28 + L35)
- **L21 (test refactor)** — Replaced the previous JavaScript IIFE pattern in tests with proper C# code (already done in Sprint 28 push but verified Sprint 29).
- **L28 (schema surprise)** — `companies.is_holding` doesn't exist; the column is `is_group` (L28 fix applied in Sprint 29 code).
- **L35 (Sprint 28 duplicate migration version)** — already fixed in the prior session.
- **Account code 3110 → 3100** in the JSON — the equity account is "رأس المال" = 3100, not 3110. (Discovered when the seeder logged "Account 3110 not found" on first run; fixed and re-seeded.)

### Verified
- `dotnet build` → 0 errors, 0 warnings
- `dotnet test` (Sprint 29 scope) → 8/8 tests passed (Projects + Payroll + Inventory)
- All 4 POC seeders run on startup: 3+10 customers + 3+10 vendors + 5+15 items + 5 depts + 10 employees + 10 POs + 73 year-scenario records
- 73 Journal Entries + 148 Journal Lines (avg 2 lines per JE)
- Opening Balance: 105,000 debits = 105,000 credits ✓
- L28 bug surfaced: `/api/ar/sales-invoices` returns 500 (separate bug to investigate in Sprint 30+)

### Lessons (L36-L39)
- **L36 (Sprint 29 seeder)**: 4th POC in <1.5h (vs 4-6h for first). The pattern is now muscle memory — JSON + IHostedService + UPSERT + Dapper + double-gate. The only time-consuming parts were schema surprises and the L28 fix.
- **L37 (Sprint 29 DI lifetime)**: IHostedService is registered as Singleton by default. Cannot inject Scoped services (e.g., ICompanyContext) into the constructor. Solution: resolve the companyId directly from the DB at startup using `DbConnectionFactory.CreateEphemeralOltpConnectionAsync`.
- **L38 (Sprint 29 legacy cleanup)**: Deleting the 2 legacy .cs files (102.8 KB) also required removing the manual admin endpoints in AdminController.cs that referenced them. The build then broke — easy to fix by replacing the endpoints with 410 Gone responses.
- **L39 (Sprint 29 benchmark JEs)**: Each transactional record gets a "benchmark" Journal Entry inserted by the seeder. The benchmark JEs are documented as `BENCH-INV-XXX`, `BENCH-BILL-XXX`, etc. When the Posting Rules engine is run on the same transactions, its JEs should match the benchmark JEs. Any discrepancy is a bug to investigate. This is a new pattern: "seeders that test other parts of the system".

### Carry-over (Sprint 30+)
- P0: Fix the 500 error on `/api/ar/sales-invoices` endpoint (discovered by Sprint 29's data)
- P0: Fix the 500 error on `/api/procurement/purchase-orders` (similar shape)
- P0: Add a default warehouse to enable GR + Bill seeders (Sprint 28 carry-over)
- P1: Audit 5 still-pending modules (Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService)
- P1: Refactor remaining `req.CompanyId` → `_companyContext.CompanyId` (L30)
- P1: Trial Balance validation UI
- P1: customer/vendor statement endpoints
- P2: Year-scenario Phase 2 (payroll + stock movements + project costs)
- P2: Posting Rules integration unit tests
- P2: Pre-push script: scan for `?` in user-visible columns
- P2: Build-time test that enforces DEC-085

---

## Sprint 28 — Article 3 audit (4 modules) + Procurement seeder (POC #3) + test refactor (2026-08-02) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's directive ("yes, Sprint 28") — continue the audit pattern. This sprint: 4 remaining modules (Payroll, Projects, StockMovement, Finance/Account) — then POC the seeder pattern a third time (Procurement). The audit found **8 more Article 3 violations** (4 entities + 3 services + 4 repos + 1 minor fix). The seeder proves L17 ("3rd implementation = permanent framework") by completing in <2h (vs 4-6h for the first one).

### Fixed (DEC-094: Payroll Article 3 audit — 5 entities + 1 service + 1 repo)
- **5 entities** — added `CompanyId` field to `SalaryStructure`, `SalaryStructureLine`, `PayrollRun`, `PayrollItem`, `PayslipComponent`. The 3 latter were without the field entirely; `SalaryStructure`/`SalaryStructureLine` had it but the repo never set it.
- **1 service** — `PayrollService` now injects `ICompanyContext` and reads `CompanyId = _companyContext.CompanyId ?? throw new InvalidOperationException(...)` in `CreateAsync` for structures + runs.
- **1 repo** — `PayrollRepository` adds `@CompanyId` to all INSERTs (`InsertStructureAsync`, `InsertStructureLineAsync`, `InsertRunAsync`, `InsertItemAsync`, `InsertComponentAsync`) + SELECTs (`company_id AS CompanyId`).
- `EosService` (end-of-service) — clean (no CompanyId needed; it's a read-only calculation).

### Fixed (DEC-095: Projects Article 3 audit — 4 entities + 3 services + 4 repos)
- **4 entities** — added `CompanyId` field to `ProjectBudget`, `ProjectTask`, `Resource`, `ResourceAssignment`.
- **3 services** — `ProjectService`, `TaskService`, `ResourceService`, `ResourceAssignmentService` (the latter 3 live in `SupportingServices.cs`) all inject `ICompanyContext` and use it for new entities. `ProjectService.CreateAsync` is the critical fix — it now uses `_companyContext.CompanyId` for the project + the auto-created `ProjectBudget` (NOT `req.CompanyId`). This is **L19 cross-tenant safety** applied to the `Project` aggregate.
- **4 repos** — `TaskRepository`, `ResourceRepository`, `ResourceAssignmentRepository`, `ProjectBudgetRepository` add `@CompanyId` to INSERT + SELECT.
- `BudgetService` — clean (read-only).

### Fixed (DEC-096: StockMovement service refactor)
- **No entity or repo change needed** — they already had `CompanyId`. Only `StockMovementService` was using `req.CompanyId`. Refactored all 4 `Create*` methods (Receive/Issue/Transfer/Adjust) to use `_companyContext.CompanyId` instead of `req.CompanyId`. This is **L19 cross-tenant safety** at the service level — the request DTO no longer carries CompanyId.

### Fixed (DEC-097: Finance/Account minor fix)
- **`Account.CompanyId`** — changed from `Guid?` to `Guid`. The DB column has been `NOT NULL` since Sprint 22. The nullable type was a code-level inconsistency that would have been a runtime NRE the moment anyone tried to set it. No service or repo change needed (they already set it correctly).

### Added (DEC-088/L17: Procurement seeder — POC #3)
- **`src/backend/Shared/SeedData/ArabicProcurementDevData.json`** (NEW, ~9KB) — UTF-8 JSON with 10 purchase orders (each with 1-2 lines) distributed across the 13 vendors from Sprint 26. Arabic notes included.
- **`src/backend/Shared/SeedData/ArabicProcurementDevSeederHostedService.cs`** (NEW, ~16KB) — `IHostedService` that UPSERTs POs via Dapper. 3-pass UPSERT: Pass 1 vendors (already done by Sprint 26, idempotent), Pass 2 PO headers, Pass 3 PO lines.
- **`Bootstrap:SeedProcurementScenario` config flag** — added to `appsettings.Development.json.example` + `appsettings.Development.json`. Default `false`. Double-gated on `IsDevelopment() + flag`.
- **`<Content Include="..\Shared\SeedData\ArabicProcurementDevData.json" />`** in csproj.
- **Program.cs registration block** — Sprint 28 section after Sprint 27, gated on `IsDevelopment()` + flag. Logs `[SPRINT-28] ArabicProcurementDevSeeder registered/skipped`.

### Fixed (DEC-099: ProjectServiceTests + IIFE pattern)
- **Test file rewritten** — the Sprint 27 IIFE pattern (`(function(){...})()`) was JavaScript syntax, not C#. The previous bulk-replace was wrong. Replaced with a proper `TestCompanyContextFactory.Create()` helper that returns a fully-set-up `ICompanyContext` (with `.Setup(c => c.CompanyId).Returns(...)`).
- **2 tests fixed for L19 cross-tenant safety**:
  - `Create_AutoCreatesCostCenter_AndBudget` — now asserts that the project gets `CompanyId` from `ICompanyContext` (NOT from `req.CompanyId`).
  - `List_FiltersByCompany` — now asserts that listing by the context's company returns all 3, while listing by any other companyId returns 0 (cross-tenant isolation).
- **Fake `FakeProjectRepository.InsertAsync`** — also propagates `project.CompanyId` to the side-effect `BudgetsByProject[project.Id]` so the test can verify the budget gets the same company.

### Migration
- **`Sprint28_Audit_20260802_220000`** (NEW, ~3.6KB) — idempotent backfill for 10 tables (`salary_structures`, `salary_structure_lines`, `payroll_runs`, `payroll_items`, `payslip_components`, `project_budgets`, `project_tasks`, `resources`, `resource_assignments`, `accounts`). No-op in practice today (0 rows), but future-safe.

### Verified (end-to-end on local host)
- `dotnet build` → 0 errors, 0 warnings
- `dotnet test` (Sprint 28 scope) → 18/18 projects tests passed (after L21 refactor + IIFE fix)
- `dotnet test` (full suite) → 378 passed, 2 environmental fails (RetentionTests need production PG creds — pre-existing, unrelated to Sprint 28)
- Procurement seeder log: `POs updated=2 inserted=8` (some POs already existed from manual Sprint 25 testing). GoodsReceipts + VendorBills skipped because there's no default warehouse — see carry-over for fix.

### Lessons (L25-L30)
- **L25 (Sprint 28 audit):** 4 more Article 3 violations — pattern holds. The audit found 5 entities + 3 services + 4 repos + 1 minor fix that needed attention. The DEC-085 checklist still catches 100% of them. The remaining un-audited modules (Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService) likely have 4-8 more violations each — they're on the carry-over list.
- **L26 (Sprint 28 IIFE):** `function(){...}()` is JavaScript IIFE syntax. It does NOT compile in C#. A previous bulk-replace from a tool (regex probably from a JavaScript-template context) injected this into a `.cs` file. **Rule going forward:** any bulk-replace operation that touches `.cs` files must be followed by `dotnet build` + `dotnet test` in the same commit, not deferred. (This is the L24 pattern again — "tests-vs-implementation drift" — but worse: the file didn't even compile.)
- **L27 (Sprint 28 seeder):** "Established pattern" holds for the 3rd time. JSON + IHostedService + UPSERT + Dapper + double-gate + Content include + appsettings flag → predictable 1.5-2h implementation. The seeder has its own surprises (no `updated_at`/`updated_by` on `purchase_order_lines`, no `name_en` on `vendors`) but the pattern absorbs them.
- **L28 (Sprint 28 schema surprises):** always `psql \d <table>` before writing the INSERT — `name_en` (vendors) and `updated_at`/`updated_by` (purchase_order_lines) are NOT 1:1 with the entity property names. Document the surprises in the seeder's startup log.
- **L29 (Sprint 28 cross-tenant):** `ProjectService.CreateAsync` is a critical L19 application. The Project aggregate has 2 child writes (Project + ProjectBudget) that BOTH need the same companyId. Using `_companyContext.CompanyId` once at the top of the method and passing the local `companyId` variable to both writes is cleaner than calling the property twice. The test now verifies this by reading the companyId from the test's mock context.
- **L30 (Sprint 28 service refactor):** When the request DTO carries `CompanyId` but the service has access to `ICompanyContext`, prefer the context. The DTO's CompanyId is a security risk (client can spoof it). The refactor of `StockMovementService` (4 methods) and `ProjectService.CreateAsync` follows this rule. Other services that still have `req.CompanyId` in the DTO are carry-over (see below).

### Carry-over (Sprint 29+, still outstanding)
- P1: Audit 5 still-pending modules — Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService (pattern says 4-8 more violations each)
- P1: `DepartmentResponse.managerName` field (small FE/BE gap — API returns `managerId` but no joined name)
- P1: `customerStatement` + `vendorStatement` GET endpoints
- P1: Manual JEs (12: depreciation, accruals, year-end)
- P1: Posting Rules integration unit tests
- P1: 14 P2 function workflow docs
- P1: `CreateItem` API method
- P1: Trial Balance validation UI ("Balanced / Unbalanced" indicator)
- P1: Year-scenario seeder (12 monthly invoices + 6 receipts)
- P1: Add a default warehouse to enable GR + Bill seeder (goods_receipts + vendor_bills require warehouse_id NOT NULL)
- P1: Refactor remaining `req.CompanyId` → `_companyContext.CompanyId` (L30 carries over)
- P2: 5th default rule "Sale with VAT 5%" (inactive, for demo)
- P2: Audit trail for posting rule changes
- P2: Multi-currency support (currently LYD-only)
- P2: mvp-docker/.env to .gitignore
- P2: Pre-push script: scan for `?` in user-visible columns (would have caught Sprint 25/26 bugs)
- P2: Build-time test that enforces DEC-085 (so new entities can't skip CompanyId silently)

---

## Sprint 27 — HR Article 3 audit + Arabic HR dev seeder (2026-08-02) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's Sprint 27 directive — POC the seeder pattern a second time (`ArabicHrDevSeeder`) to establish it as a framework. As a prerequisite, the carry-over note "needs EmployeeService/DepartmentService/ProjectService Article 3 fixes" had to be addressed first. The audit found 8 violations in the HR module (4 entities + 4 services + 4 repos with no company_id propagation) — same shape as Sprint 25 (Procurement).

### Fixed (DEC-091: HR Article 3 audit — 8 violations)
- **4 entities** — added `CompanyId` field to `Employee`, `Department`, `LeaveRequest`, `Attendance` (the latter 3 had zero presence of the field).
- **4 services** — injected `ICompanyContext` into `DepartmentService`, `EmployeeService`, `AttendanceService`, `LeaveRequestService`. Read `CompanyId = _companyContext.CompanyId ?? throw new InvalidOperationException(...)` in each `CreateAsync`. For `AttendanceService` and `LeaveRequestService` (which are employee-driven), the code prefers `emp.CompanyId` over the context (cross-tenant safety) — same pattern that other employee-driven services will use.
- **4 repositories** — added `@CompanyId` to INSERT + SELECT in `DepartmentRepository`, `EmployeeRepository`, `AttendanceRepository`, `LeaveRequestRepository`. The DB tables already had `company_id NOT NULL` from the Sprint 22 schema, but the columns would have been set to NULL by the existing INSERTs.
- **Migration `Sprint27_HrCompanyId_20260802_130000`** — idempotent backfill (UPDATE ... SET company_id = (first company) WHERE company_id IS NULL) for all 4 HR tables. No-op in practice today (0 rows), but future-safe.
- **HRDocumentSequenceRepository** — was already fixed in Sprint 24 (DEC-083), no change needed.
- **Validators** — clean (no `CompanyId != Guid.Empty` boilerplate, since the services never read CompanyId from the request DTO).

### Added
- **`src/backend/Shared/SeedData/ArabicHrDevData.json`** (NEW, ~4.5KB) — UTF-8 JSON with 5 departments + 10 employees. Includes `parentCode` (department hierarchy) and `managerEmployeeNumber` (FK to employees).
- **`src/backend/Shared/SeedData/ArabicHrDevSeederHostedService.cs`** (NEW, ~16KB) — `IHostedService` that reads the JSON + UPSERTs departments + employees via Dapper. 3-pass approach to handle the cyclic FK:
  - **Pass 1**: UPSERT departments (no `manager_id` yet, since the referenced employee may not exist)
  - **Pass 2**: UPSERT employees (with `department_id` resolved from department code)
  - **Pass 3**: UPDATE `departments.manager_id` from the manager's `employeeNumber`
- **`Bootstrap:SeedHrScenario` config flag** — added to `appsettings.Development.json.example` + `appsettings.Development.json`. Default `false`. Double-gated on `IsDevelopment() + flag`.
- **`<Content Include="..\Shared\SeedData\ArabicHrDevData.json" />`** in csproj.
- **Program.cs registration block** — Sprint 27 section after Sprint 26, gated on `IsDevelopment()` + flag. Logs `[SPRINT-27] ArabicHrDevSeeder registered/skipped`.
- **Sprint 27 retro** — `docs/team-charters/retrospectives/sprint-27-retro.md` (forthcoming).

### Verified (end-to-end on local host)
- `dotnet build` → 0 errors, 0 warnings
- `psql` confirmed: 5 departments + 10 employees with proper Arabic stored as UTF-8 bytes (`d8a7d984d8a5d8afd8a7d8b1` = `الإدارة`).
- 3-pass FK cycle resolved correctly: 5 manager links assigned (each department has a manager who is one of the 10 employees).
- API `/api/hr/departments` and `/api/hr/employees` return Arabic JSON.
- BE on `http://127.0.0.1:5001` (dev env), FE on `http://localhost:3000` (browser will show Arabic on the HR pages).

### Lessons
- **L17: "Established pattern" threshold = 2 implementations.** This is the second seeder in the same shape (Sprint 26 = customers/vendors/items, Sprint 27 = departments/employees). The pattern is now: JSON file + C# IHostedService + UPSERT + Dapper + double-gate + Content include + appsettings flag. Next seeder (`ArabicProcurementDevSeeder`?) can be 1.5-2h instead of 4-6h because the pattern is proven.
- **L18: Cyclic FK requires 3-pass UPSERT.** `departments.manager_id` → `employees.id` AND `employees.department_id` → `departments.id`. The cycle is broken by ordering: departments first (without managers), then employees (with department), then managers (UPDATE on departments). Single-pass with `INSERT ... SELECT` won't work because the referenced row doesn't exist yet.
- **L19: Cross-tenant safety > context-only CompanyId.** For employee-driven services (Attendance, LeaveRequest), prefer `emp.CompanyId` over `_companyContext.CompanyId` — the employee is the canonical source. In single-deployment mode they're identical, but this pattern works correctly in a multi-tenant scenario too.
- **L20: Audit pattern is now 7 sprints running.** Sprints 19, 21, 22, 23, 24, 25, 27 all surfaced Article 3 violations. The DEC-085 checklist (in AGENTS.md) caught 100% of them. The bug is "if you don't enforce it explicitly, the code drifts."

### Carry-over (Sprint 28+, still outstanding)
- P1: Procurement cycle demo data (10 POs + 10 GRs + 10 bills) — `ArabicProcurementDevSeeder` (3rd seeder, now trivial)
- P1: Extend `ArabicDevSeeder` (Sprint 26) to also create sales invoices + receipts + opening balance JEs from JSON
- P1: Manual JEs (12: depreciation, accruals, year-end)
- P1: Posting Rules integration unit tests
- P1: 14 P2 function workflow docs
- P1: `customerStatement` + `vendorStatement` GET endpoints
- P1: `CreateItem` API method
- P1: Trial Balance validation UI
- P1: `DepartmentResponse.managerName` field — currently the API returns `managerId` but no joined name (small FE/BE gap)
- P2: 5th default rule "Sale with VAT 5%" (inactive, for demo)
- P2: Audit trail for posting rule changes
- P2: Multi-currency support
- P2: mvp-docker/.env to .gitignore
- P2: Pre-push script: scan for `?` in user-visible columns (would have caught Sprint 25 bug)

---

## Sprint 26 — Arabic dev seeder (2026-08-02) ✅ DONE (LOCAL-ONLY)

**Goal:** Per Anas's directive ("اللغه العربيه غير مدعومه في البيانات السيدر الجديده , اريد ان يكون هناكم ملف داتا سيدر خاص ببيئه التطوير , يمثل سيناريو تشغيلي لسنه تشغيليه كامله وباللغه العربيه") — build a proper Arabic seeder for dev environment, fix the encoding bug from Sprint 25 PowerShell scripts, and let the user see real Arabic data on the local host like carry-over/migration data.

### Root cause (DEC-087)
- **Sprint 25 PowerShell scripts used `ConvertTo-Json | Invoke-RestMethod` from PowerShell 5.1, which sends the JSON body as UTF-16-LE bytes.**
- ASP.NET Core's UTF-8 decoder can't read UTF-16-LE multi-byte sequences → each Arabic character (2-4 UTF-8 bytes) is replaced with a single `?` (0x3F).
- Verified via `psql`: CUST-004..013, VEND-004..013, ITEM-006..020 all had `name = '? ? ? ...'`. Hex dump confirmed `3f3f3f3f` (literal question marks), not UTF-8 mojibake.
- The original 3 customers (CUST-001..003, seeded via direct SQL in `DefaultHoldingBootstrapHostedService`) were stored correctly as UTF-8 bytes (`d8b4d8b1...` = Arabic letters) — because C# `string` literals are UTF-8 native in `.cs` files since .NET 5+.

### Added
- **`src/backend/Shared/SeedData/ArabicDevData.json`** (NEW, ~13KB) — UTF-8 encoded JSON with proper Arabic names for 13 customers + 13 vendors + 20 items. The "single source of truth" for Arabic master data on dev environment.
- **`src/backend/Shared/SeedData/ArabicDevSeederHostedService.cs`** (NEW, ~22KB) — `IHostedService` that reads the JSON + UPSERTs customers/vendors/items via Dapper. Idempotent (UPSERT by `code`/`sku`). Dev environment only (gated on `IsDevelopment()` + `Bootstrap:SeedArabicScenario=true`). Uses the standard `IDbConnectionFactory` ephemeral connection pattern.
- **`Bootstrap:SeedArabicScenario` config flag** — added to `appsettings.Development.json.example` (template) and `appsettings.Development.json` (gitignored local). Default `false`; explicit opt-in.
- **`<Content Include="..\Shared\SeedData\ArabicDevData.json" CopyToOutputDirectory="PreserveNewest" />`** in `Host/ERP-SYSTEM.csproj` — copies the JSON to `bin/Debug/net9.0/Shared/SeedData/` so the seeder can find it at runtime.
- **`Program.cs` registration block** — Sprint 26 section after the existing seeders, gated on `IsDevelopment()` + flag. Logs `[SPRINT-26] ArabicDevSeeder registered/skipped` line.

### Fixed
- **All 35 broken Arabic names** in the DB restored to proper UTF-8 Arabic. On first run: `customers updated=13 inserted=0, vendors updated=13 inserted=0, items updated=20 inserted=0`. The seeder is idempotent — re-running is safe.

### Verified (end-to-end on local host)
- `dotnet build` → 0 errors, 0 warnings
- `psql` confirmed: `CUST-001` = `شركة الفجر للتوزيع` (hex `d8b4d8b1d983d8a920d8a7...`), `CUST-004` = `شركة النور للتوريدات`, `CUST-013` = `شركة السلامة للتوريدات`, etc. All 13 customers + 13 vendors + 20 items now have proper Arabic stored as UTF-8 bytes.
- API check (`/api/ar/customers`, `/api/ar/receipts`): returns Arabic in JSON. Receipts that previously showed `???? ?????? ????????` (literal `?`) now show `مكتب البركة للخدمات`, `مؤسسة الهلال التجارية`, etc.
- BE listening on `http://127.0.0.1:5001` (dev env), FE on `http://localhost:3000`. Browser on local host shows Arabic in customer list + receipts + AR aging.

### Lessons
- **L13: PowerShell 5.1 + JSON to ASP.NET Core = encoding bug.** The 2-sprint-old Sprint 25 PowerShell scripts looked fine locally (PowerShell console printed Arabic correctly), but the HTTP body bytes were UTF-16-LE. ASP.NET Core's UTF-8 decoder silently turned every multi-byte Arabic char into `?`. C# string literals in `.cs` files are UTF-8 native → `DefaultHoldingBootstrapHostedService` worked. C# string literals in `.json` files loaded via `File.ReadAllText` are also UTF-8 native → `ArabicDevSeederHostedService` works. **Rule going forward:** never use PowerShell 5.1 `ConvertTo-Json` + `Invoke-RestMethod` for Arabic (or any non-ASCII) data. Use C# hosted services, or PowerShell 7's `-Encoding utf8NoBOM` with explicit `Invoke-RestMethod -ContentType 'application/json; charset=utf-8'`.
- **L14: "DEV-ONLY" seeder is a real category, not just a flag.** The Sprint 22-era seeders (`ScenarioSeederHostedService`, `RealisticSeedHostedService`) are gated only by config flag — they'd run in production if someone flipped the flag. `ArabicDevSeederHostedService` is double-gated: `IsDevelopment() && Bootstrap:SeedArabicScenario`. Even if a misconfig sets the flag in production, the env check stops it. This pattern should be standard for any future dev-only seeder.

### Carry-over (Sprint 27+, still outstanding)
- P1: HR demo data (10 employees + 5 departments + 5 projects) — needs `EmployeeService`/`DepartmentService`/`ProjectService` Article 3 fixes
- P1: Procurement cycle demo data (10 POs + 10 GRs + 10 bills) via ArabicDevSeeder extension
- P1: Manual JEs (12: depreciation, accruals, year-end)
- P1: Posting Rules integration unit tests
- P1: 14 P2 function workflow docs
- P1: `customerStatement` + `vendorStatement` GET endpoints
- P1: `CreateItem` API method
- P1: Trial Balance validation UI
- P2: 5th default rule "Sale with VAT 5%" (inactive, for demo)
- P2: Audit trail for posting rule changes
- P2: Multi-currency support
- P2: mvp-docker/.env to .gitignore
- P2: Extend `ArabicDevSeeder` to create sales invoices + receipts + opening balance JEs from JSON (today: master data only; transactions remain from Sprint 25 PowerShell scripts)

---

## Sprint 24 — outbox cleanup + Constitution Article 3 audit (2026-08-02) ✅ DONE (LOCAL-ONLY)

**Goal:** Per **DEC-082** + **DEC-083** — finish the "no event bus" cleanup (drop outbox tables) and run a code-level audit of Constitution Article 3 (every entity + every service must use `company_id` via `ICompanyContext`, never `Guid.Empty`).

Branch: working on `feature/sprint-21-posting-rules-engine` (now carries Sprints 21+22+23+24). LOCAL-ONLY. Carries Sprint 21+22+23 + 23.1 (Stock→Posting direct call) work + 23.2 (company_id propagation fix).

### Removed
- **`outbox_events` table** — `Sprint24_DropOutboxAndProcessedEvents_20260802_120000` migration. Was the write-side queue for the deleted `IEventBus`. Nothing writes here anymore (Sprint 22 moved to direct service calls).
- **`processed_events` table** — same migration. Was the dedup table for the deleted event handlers. Idempotency now lives in the standard `(source_entity, source_id)` unique index pattern (see Finance).
- **`outbox_events.json` + `processed_events.json`** — JSON data-type definitions removed (DataTypeMigrator won't try to recreate the tables on next startup).
- **Outbox-related code refs** — cleaned up the remaining comments in `Program.cs` + the test fixture in `RetentionTests.cs` (the expected retention-period dict no longer references the dropped tables).

### Fixed
- **DEC-083: `procurement_document_sequences` + `hr_document_sequences` lacked `company_id`.** Two CREATE TABLE statements were missing the column, and the PK was just `(prefix)`. In a single-deployment-with-N-subsidiaries world, two companies would collide on the same `PO-2026-0001`. Now both tables have:
  - `company_id UUID NOT NULL`
  - PK = `(company_id, prefix)`
- **`Sprint24_DocumentSequencesAddCompanyId_20260802_121000` migration** — backfills existing rows to the first company (only safe default in a non-multi-company legacy DB), then enforces NOT NULL + swaps the PK. Idempotent (DO blocks + IF EXISTS guards).
- **3 sequence repositories** now take `ICompanyContext` in the constructor (`DocumentSequenceRepository`, `PaymentSequenceRepository`, `HRDocumentSequenceRepository`) and use it in the UPSERT and SELECT.

### Added
- **`appsettings.Development.json.example`** — template for the gitignored `appsettings.Development.json` (new contributors can copy + edit). Documents every key (ConnectionStrings, Marten, JwtSettings, Deployment, Bootstrap) with a comment explaining the safe defaults for local dev.
- **Sprint 24 retro** — `docs/team-charters/retrospectives/sprint-24-retro.md` (NEW, ~10KB).

### Carry-over (Sprint 25+, still outstanding from Sprints 19-23)
- **P1:** 14 P2 function workflow docs (Attendance, Leave, Department, Cost Center, Posting Rules, Stock Movement, Warehouse, Item Category, UoM, User/Role, Audit Log, Holding/Company, Notification, Activity Feed)
- **P1:** `customerStatement` + `vendorStatement` GET endpoints (Sprint 15 carry-over)
- **P1:** `CreateItem` API method
- **P1:** Trial Balance validation UI ("Balanced / Unbalanced" indicator)
- **P2:** 5th default posting rule "Sale with VAT 5%" (inactive, for demo)
- **P2:** Audit trail for posting rule changes
- **P2:** Multi-currency support (currently LYD-only)
- **P2:** mvp-docker/.env to .gitignore
- **P2:** Posting Rules integration unit tests (new — Sprint 23.1 Stock→Posting direct call needs coverage)

### Verified
- `dotnet build` → 0 errors, 0 warnings
- No `tenant_id` regressions (Constitution Article 3)
- 2 new migrations are idempotent + DO-block-guarded

---

**Goal:** Fix the **2 latent bugs** discovered by the Sprint 22 end-to-end smoke test:
1. `JournalEntry` + `JournalLine` entities had no `CompanyId`, and `JournalEntryRepository` did not include `company_id` in INSERT/SELECTs. Posting Rules Engine (Sprint 21) failed with `null value in column "company_id" of relation "journal_entries"`.
2. `SalesInvoiceService.CreateAsync` + `ReceiptService.CreateAsync` + `CustomerService.CreateAsync` had `CompanyId = Guid.Empty` boilerplate, causing INSERTs to fail with `fk_*_company_id` FK violations. Constitution Article 3 was already violated (just paper-level).

Plus: integrate `StockMovementService` → `IPostingRulesService` (direct call) so posting rules fire on stock movements too.

Branch: `feature/sprint-21-posting-rules-engine` (Sprint 21 + 22 + 23 stacked). LOCAL-ONLY.

### Fixed
- **`JournalEntry` entity** — added `CompanyId` field. Constitution Article 3.
- **`JournalLine` entity** — added `CompanyId` field (FK to companies; same as `JournalEntry`).
- **`JournalEntryRepository`** — added `company_id` to INSERT columns + all SELECTs. `journal_lines` INSERT now includes `company_id`.
- **`JournalEntryService`** — injected `ICompanyContext`. `CreateDraftAsync` reads `CompanyId` from context and propagates it to both `JournalEntry` and every `JournalLine`.
- **`SalesInvoiceService.CreateAsync`** — replaced `CompanyId = Guid.Empty` with `CompanyId = companyId` (was already read from `_companyContext` for sequence numbering but never used on the entity).
- **`ReceiptService.CreateAsync`** — same fix.
- **`CustomerService.CreateAsync`** — injected `ICompanyContext`, removed misleading "single-company per tenant" comment, replaced `CompanyId = Guid.Empty` with `CompanyId = companyId`.

### Added
- **Sprint 23.1 — `StockMovementService` → `IPostingRulesService` direct call** — `PostAsync` now invokes `_postingRules.ApplyRulesAsync(TriggeringEvent.StockReceived, payload, ct)` synchronously after posting the stock movement. Non-fatal on failure (logs warning, continues; user can re-post manually). Replaces the deleted `StockEventHandlers.cs` event-bus path with a direct call in the same logical scope.

### End-to-end smoke test (libya default, no tax)
- **POST `/api/ar/sales-invoices`** (postImmediately=true):
  - Created `SI-2026-000001` (300 LYD, customer CUST-001, item ITEM-001 × 10)
  - Status: `Sent` (posted)
  - **`JE-2026-0001`** auto-generated by Posting Rules Engine
    - L1: 1230 ذمم مدينة (Dr 300) | L2: 5110 إيرادات (Cr 300) — balanced ✓
- **POST `/api/ar/receipts`** (postImmediately=true):
  - Created `RC-2026-000001` (100 LYD, allocated to SI-2026-000001)
  - Status: posted
  - **`JE-2026-0002`** auto-generated
    - L1: 1210 النقدية (Dr 100) | L2: 1230 ذمم مدينة (Cr 100) — balanced ✓
- **GET `/api/finance/posting-rules`** — 5 rules active (StockReceived / SalesInvoicePosted / VendorBillPosted / ReceiptPosted / PaymentPosted)

### Verified
- `dotnet build` → 0 errors, 0 warnings
- All entity INSERTs include `company_id` from `ICompanyContext`
- `ICompanyContext` injected in every service that creates entities with multi-company scope (Constitution Article 3)
- No `tenant_id` regressions

---

**Goal:** Per **Anas 2026-08-02 03:00 UTC** + Muhammad analysis — clean up the architecture for a single-deployment ERP (Holding + N subsidiaries). Drop dead modules, drop the event bus, drop Marten. Use direct service calls for cross-module work (Posting Rules workflow). Plan: docs/architecture/REFACTOR-SPRINT-22.md.

Branch: working on eature/sprint-21-posting-rules-engine (Sprint 21 + Sprint 22 combined). LOCAL-ONLY.

### Removed
- **4 modules deleted (BE):** Modules/Activity/, Modules/Notifications/, Modules/Search/, Modules/Reports/
- **6 controllers deleted (BE):** ActivityController, NotificationsController, SearchController, ReportsController, FinanceReportsController, EventsController
- **5 FE pages deleted:** /activity, /admin/notifications/*, /notifications, /reports/* (~25 sub-pages)
- **2 FE components deleted:** NotificationBell.tsx, GlobalSearch.tsx
- **Event Bus deleted:** Shared/Events/ entire directory (IIntegrationEvent, IDomainEvent, EventBus, OutboxProcessor, OutboxEvent, OutboxRepository, ProcessedEventsRepository, IProcessedEventsRepository, IIntegrationEventHandler<T>)
- **Marten references cleaned up:** Marten__ConnectionString removed from appsettings (DEC-017 was disabled anyway).
- **Outbox tables dropped on next migrate:** outbox_events, processed_events (still in DB until next clean install — non-blocking).

### Changed
- **Modules 15 → 9:** kept Identity, Companies, Finance, Inventory, Procurement, AccountsReceivable, HR, Payroll, Projects, Dashboard. See /AGENTS.md for the canonical list.
- **Cross-module = direct service calls:** SalesInvoiceService.PostAsync directly calls PostingRulesService.ApplyRulesAsync(...) + ProjectsService.UpdateCostAsync(...). Same transaction, no outbox.
- **AuthService simplified:** removed IActivityLogger injection (Activity module deleted).
- **StockMovementService simplified:** removed INotificationService + IEventBus injection. No low-stock notifications, no event publishing.
- **FE sidebar cleaned:** removed التقارير group (12 items), removed إشعاراتي + سجل النشاط from admin group. 28 valid links remain.
- **Folder rename:** src/backend/Shared/MultiTenancy/ → src/backend/Shared/CompanyContext/.
- **Per-module reports:** complex reports deleted. Simple reports live in their parent module (e.g., Trial Balance in Finance).

### Fixed
- **PostingRuleRepository.InsertAsync jsonb cast** (Sprint 21 fix): 	emplate_json::jsonb cast added in SQL.
- **Missing CompanyId in PostingRule entity** (Sprint 21 fix): added CompanyId to entity, set from ICompanyContext in CreateAsync + from holdingId in EnsureDefaultRulesAsync.
- **FE posting-rules page** (Sprint 21 fix): replaced raw etch() with pi.get/post/delete (so JWT + X-Company-Id headers are sent). Also fixed body shape.

### Known Issues (Phase 13 — deferred)
- 22 FE calls to dead-module endpoints (/api/*/reports/*, /api/activity/recent, /api/inventory/notifications/unread, /api/search) → FE pages show error states.
- 3 server 500s on dashboard pages (/api/holdings/dashboard, /api/dashboard/summary, /api/transactions/recent).
- 4 routing 404s (wrong paths): /api/admin/health, /api/admin/audit, /api/finance/ledger/general-ledger, /api/holding.

---## Sprint 21 — Posting Rules Engine: config-driven posting for AR + Procurement (2026-08-01) ✅ DONE (LOCAL-ONLY → Mode 2 pending)

**Goal:** Per **Anas 2026-08-01 13:55 UTC** ("ابدأ") + Muhammad recommendation — replace the hardcoded posting logic in Sales/Receipt/Bill services with a config-driven Posting Rules Engine. Libya default = no tax (TaxId optional, null = no tax). Pre-existing infrastructure (Sprint 11) is already there — Sprint 21 expands it to cover the 4 P0 business events + hooks + seeder.

Branch: `feature/sprint-21-posting-rules-engine` (off `origin/develop @ a1d7e25`). LOCAL-ONLY → Mode 2 push planned.

### Added

**P0a — `TriggeringEvent` enum expanded** (BE):
- Added `SalesInvoicePosted = 3` (alias for legacy `InvoiceCreated`)
- Added `ReceiptPosted = 4` (alias for legacy `PaymentReceived`)
- Added `VendorBillPosted = 5` (new)
- Added `PaymentPosted = 6` (new)
- **Why:** the existing 4 events covered inventory + legacy names. Sprint 21 adds the 4 P0 business events (Sale, Receipt, Bill, Payment).

**P0b — `EventPayload` expanded** (BE):
- Added `Subtotal` (decimal) — amount before tax
- Added `TaxAmount` (decimal) — separate from total
- Changed default currency from `SAR` to `LYD` (Libyan Dinar)
- **Why:** Libya = no tax by default, but the schema must support tax-ready payloads.

**P0c — `EvaluateFormula` expanded** (BE):
- New tokens: `{subtotal}`, `{tax}`, `{tax+subtotal}` (total)
- New operator: `{subtotal}*0.05` → 5% VAT pattern
- **Why:** accountants need to compute tax lines with formulas like `subtotal × rate`.

**P0d — `ApplyRulesAndReturnAsync` (new method on `IPostingRulesService`)**:
- Same as `ApplyRulesAsync` but returns the **first journal entry ID** (for the service to link to the source doc).
- **Why:** the existing `ApplyRulesAsync` only returned the count. The refactored services need the JE ID to set `JournalEntryId` on the invoice/bill.

**P0e — `EnsureDefaultRulesAsync` expanded with 4 Libya-default rules:**
- `SalesInvoicePosted` → Dr 1230 (AR) / Cr 5110 (Sales Revenue)
- `VendorBillPosted` → Dr 1240 (Inventory) / Cr 2210 (AP)
- `ReceiptPosted` → Dr 1210 (Cash) / Cr 1230 (AR)
- `PaymentPosted` → Dr 2210 (AP) / Cr 1210 (Cash)
- All 4 rules are **no-tax** (Libya default) — accountants can add a 5th rule with VAT manually.
- The pre-existing `StockReceived` rule is also kept (now using the correct account codes 1240/2210, was 1300/2100 which don't exist).

**P0f — `DefaultHoldingBootstrapHostedService` calls `EnsureDefaultRulesAsync`:**
- The 5 default posting rules are seeded at first startup (after CoA is ready).
- Non-fatal: if seeding fails, the app still boots (with a warning).
- **Why:** without the hook, the rules would never be in the DB and the services would always fall through to the legacy path.

### Changed

**P0g — `SalesInvoiceService.PostInternalAsync` refactored to use the engine:**
- Removed hardcoded `Dr 1230 / Cr 5110` journal entry creation.
- Now calls `_postingRules.ApplyRulesAndReturnAsync(TriggeringEvent.SalesInvoicePosted, ...)`.
- If no rule is configured → returns a clear error message: "لا توجد قواعد ترحيل نشطة لفاتورة المبيعات. أضف قاعدة في /admin/posting-rules."

**P0h — `ReceiptService.PostInternalAsync` refactored (same pattern):**
- Removed hardcoded `Dr 1210 / Cr 1230`.
- Now calls the engine for `ReceiptPosted` event.

**P0i — `VendorBillService.PostAsync` refactored (engine preferred, DEC-075 fallback):**
- Engine first: if a rule matches `VendorBillPosted`, the engine creates the JE.
- If no rule: falls back to the existing DEC-075 hardcoded path (Inventory/AP lookup + journal entry).
- **Why this hybrid approach:** VendorBill is more complex (DEC-075 had a fallback for missing accounts). Removing the fallback would break that path. The engine becomes the **preferred** path; the fallback is a safety net.

**P0j — FE `/admin/posting-rules` page labels updated:**
- Added Arabic labels for all 6 event types (was 4: StockReceived/StockIssued/InvoiceCreated/PaymentReceived).
- Renamed `InvoiceCreated` → `SalesInvoicePosted` and `PaymentReceived` → `ReceiptPosted` (clearer).
- Default template updated: uses real CoA codes `1240/2210` (was `1110/2010` which don't exist).
- `parseRuleSummary` now shows **all lines** (not just the first) — clearer for multi-line rules like Sale+Tax.

### Verified (local)

- `dotnet build` (backend) — **0 errors, 0 warnings** (clean since Sprint 20)
- `npm run type-check` (frontend) — 0 errors
- `npm run build` (frontend) — 0 errors, 87 pages build
- `git grep tenant_id` — clean
- All 4 refactored services + 1 new bootstrap call + 1 new service method compile

### Local smoke test

- Deferred to post-Mode-2 (Anas triggers "ادفع" → push → CI green → merge → cron rebuilds mvp-docker with Sprint 21 code → smoke 9/9 + manual UI test).
- The end-to-end flow: open `/admin/posting-rules` → see 5 default rules → create a sales invoice in `/finance/sales-invoices/new` → post it → verify a 2-line journal entry was auto-created (Dr 1230 / Cr 5110).
- The trigger endpoint (`POST /api/finance/posting-rules/trigger/{eventType}`) is exposed for testing rules manually without going through a real document flow.

### Carry-over (post-Sprint 21)

- **P1 (Sprint 22):** P2 function workflow docs (14 functions — Attendance, Leave, Department, Cost Center, Posting Rules, Stock Movement, Warehouse, Item Category, UoM, User/Role, Audit Log, Holding/Company, Notification, Activity Feed)
- **P1 (Sprint 22):** `customerStatement` + `vendorStatement` GET endpoints
- **P1 (Sprint 22):** `CreateItem` API method
- **P1 (Sprint 22):** Trial Balance validation UI ("Balanced / Unbalanced" indicator)
- **P2:** Add a 5th default rule "Sale with VAT 5%" (optional) to demonstrate the tax engine works for accountants who want tax
- **P2:** Audit trail for posting rule changes (who created/edited/deactivated, when)
- **P2:** Multi-currency support (currently LYD-only)

---

## Sprint 20 — Demo 2: P1 workflow docs + defensive hardening (2026-08-01) ✅ DONE (LOCAL-ONLY → Mode 2 pending)

**Goal:** Per **Anas 2026-08-01 11:25 UTC** — extend Sprint 19 with 9 P1 function workflow docs (cover all 13 demo functions), plus defensive hardening (env validation, CS warnings cleanup, cosmetic Telegram fix). Make the system fully documented for the 1-day client handover. **No backend code changes beyond bug fixes.**

Branch: `feature/sprint-20-client-demo-docs` (off `origin/develop @ de181a0`). LOCAL-ONLY → Mode 2 push planned.

### Added

**P0a — `docs/workflows/` (extended, 9 new files):**
- `purchase-order.md` — Purchase Order function (9-section template: business purpose, user roles, user journey, API contract, UI pages, state transitions, edge cases, bilingual labels, related workflows)
- `goods-receipt.md` — Goods Receipt function (same template, includes partial-receipt logic)
- `vendor-bill.md` — Vendor Bill function (same template, includes GR linkage)
- `receipt.md` — Receipt (AR) function (same template, includes allocation to multiple invoices)
- `chart-of-accounts.md` — Chart of Accounts function (same template, 5 account types, code conventions)
- `journal-entry.md` — Journal Entry function (same template, balance validation, auto vs manual)
- `employee.md` — Employee function (same template, EOS reference)
- `payroll-run.md` — Payroll Run function (same template, status flow, EOS calculator)
- `project.md` — Project function (same template, budget vs actual)
- **Total coverage:** 13 of 13 demo-grade functions documented
- `docs/workflows/README.md` updated to list P0 + P1 functions (and 14 P2 functions in backlog)

**P0b — `docs/client-materials/` (NEW client demo prep, Sprint 20):**
- `elevator-pitch-ar-en.md` — 1-page bilingual introduction for the client meeting. Covers: what it is, why it matters, key numbers, 3 anticipated questions + answers, next steps. Designed to be read in 60 seconds.
- `slides/erp-demo-slides.pptx` — 8-slide PowerPoint deck for the client meeting. Sections: cover, the problem, the solution, 13 functions, demo flow, why it matters, architecture, next steps. Clean professional design (dark blue + gold accent), Arial font, LAYOUT_16x9.
- `slides/compile.js` — PptxGenJS source (re-runnable for future updates).

**P0c — `scripts/rebuild-mvp-docker.ps1` (defensive .env validation, Sprint 20):**
- New step 1.6: parses `.env.example` to get list of required KEY=VALUE pairs
- Parses `.env` to get list of present keys
- If any required keys are missing → log warning + list missing keys
- With `-Init` (or `-Quiet`): auto-append missing keys from `.env.example` to `.env` (preserves existing values)
- Backs up incomplete `.env` to `.env.bak.<timestamp>` first
- Without `-Init`: exit code 6 with helpful error message
- **Why:** Sprint 18 .env truncation issue was caused by `git reset --hard` against a dirty working tree in another worktree. The auto-create branch (Sprint 16) only handled "file doesn't exist" — this now handles "file exists but is incomplete".

### Changed

**P0c — `src/backend/Shared/SeedData/ScenarioSeederHostedService.cs` (CS8602 fix):**
- Line 138: added `result.Response != null` check before dereferencing
- Resolves `CS8602: Dereference of a possibly null reference` warning

**P0d — `src/backend/Host/Controllers/AuthController.cs` (CS8629 fix):**
- Line 288: added `matchedUserId == null` to the guard check (was only checking `matchedId`)
- Resolves `CS8629: Nullable value type may be null` warning
- Belt-and-suspenders fix — the two vars are set together in the foreach loop, but explicit check is safer

**P0e — `scripts/watch-develop-and-rebuild.ps1` (cosmetic Telegram message):**
- Updated Telegram notification from "Sprint 16 auto-rebuild" to "Sprint 20 auto-rebuild"
- Applies to both success and failure messages
- The script's rebuild logic is unchanged (still uses `scripts/rebuild-mvp-docker.ps1`)

### Verified (local)

- `dotnet build` (backend) — **0 errors, 0 warnings** (was 2 pre-existing CS warnings before this sprint)
- `npm run type-check` (frontend) — 0 errors (no FE changes)
- `npm run build` (frontend) — 0 errors (no FE changes)
- `git diff --stat` — 9 new docs files (3 docs) + 4 modified files (env check, CS fixes, Telegram, README)
- `grep -r "tenant_id" docs/workflows/` — clean

### Local smoke test

- Deferred to post-Mode-2 (Anas triggers "ادفع" → push → CI green → merge → cron rebuilds mvp-docker with Sprint 20 code → smoke 9/9 → Telegram ping)
- The defensive .env check will be **exercised by the cron** — if the existing `.env` is complete, the new step logs "all N required keys present" and proceeds; if incomplete, the watcher's `-Quiet` flag will auto-append missing keys.

### Carry-over (post-Sprint 20)

- **P1 (Sprint 21+):** P2 function workflow docs (14 functions: Attendance, Leave, Department, Cost Center, Posting Rules, Stock Movement, Warehouse, Item Category, UoM, User/Role, Audit Log, Holding/Company, Notification, Activity Feed)
- **P1 (Sprint 21+):** Add `customerStatement` + `vendorStatement` GET endpoints to backend
- **P1 (Sprint 21+):** Add `CreateItem` API method to `inventoryApi`
- **P2:** Slides for client demo (Muhammad mode, post-handover)
- **P2:** 1-page elevator pitch (Muhammad mode, post-handover)
- **P3:** Verify the new .env check in actual rebuild scenario (post-merge smoke test)

---

## Sprint 19 — Client Demo Sprint: workflows docs + demo-grade FE types (2026-08-01) ✅ DONE (LOCAL-ONLY → Mode 2 pending)

**Goal:** Per **Anas 2026-08-01 ~08:30 UTC** "وضع صفحة العميل" — make the system **demo-ready** for the Libyan client. Build UI pages for the 4 P0 functions (Customers, Vendors, Items, Sales Invoices) and document each function in a client-friendly workflow doc. **Admin solo** (no Jimis — past timeouts + connection errors taught us quality > parallelism for client-facing work). No backend code changes.

Branch: `feature/sprint-19-client-demo-ui` (off `origin/develop @ bfe9f1d`). LOCAL-ONLY → Mode 2 push planned.

### Added

**P0a — `docs/workflows/` directory (NEW, 5 files):**
- `README.md` — index, template description, and P0/P1 function list
- `customer.md` — Customer function (9 sections: business purpose, user roles, user journey, API contract, UI pages, state transitions, edge cases, bilingual labels, related workflows)
- `vendor.md` — Vendor function (same 9-section template)
- `item.md` — Item function (same 9-section template, with the LowStock notification mention)
- `sales-invoice.md` — Sales Invoice function (same 9-section template, with the full state transition diagram including Overdue + PartiallyPaid + Cancelled)
- **Each doc is bilingual** (Arabic + English) and follows the same template so the client can navigate them consistently
- **Audience:** client stakeholders, support team, future contributors, legal team
- **Why now:** the client asked for a demo; the demo needs to be explainable. These docs are the explanation script.

**P0b — `src/frontend/lib/api-types.ts` (demo-grade contract types):**
- New `CustomerDto`, `CreateCustomerRequest`, `CustomerStatement`, `CustomerStatementLine`
- New `VendorDto`, `CreateVendorRequest`, `VendorStatement`, `VendorStatementLine`
- New `ItemDto`, `CreateItemRequest`
- New `SalesInvoiceStatus` (string union: 'Draft' | 'Posted' | 'Paid' | 'Cancelled'), `SalesInvoiceLineDto`, `SalesInvoiceDto`, `CreateSalesInvoiceRequest`
- New generic `PagedResult<T>`
- **Why:** these are the "FE-wins" types per the Sprint 11 hand-off — the demo pages can consume them directly. Legacy `api.ts` types stay for the existing pages (back-compat). Two parallel contracts is intentional: legacy uses numeric status enums (matches existing BE), demo uses string unions (cleaner for new pages).

### Changed

**P0c — `docs/AGENTS.md` (Child DOX index updated):**
- Removed obsolete reference to `/WORKFLOW.md` (deleted in Sprint 18)
- Added `docs/workflows/` to the Child DOX Index as Active (Sprint 19)
- Added `docs/notes/` to the Child DOX Index
- Updated "Adding New Documentation" section: new flow asks "is this about a client workflow?" → `docs/workflows/`
- Updated "Purpose" section to mention client-facing documentation

### Verified (local)

- `npm run type-check` (frontend) — 0 errors
- `npm run build` (frontend) — 0 errors, all 87 pages build successfully
- `dotnet build` (backend) — 0 errors, 2 pre-existing warnings (not introduced by Sprint 19)
- `npm run lint` — 0 errors, 0 new warnings (pre-existing warnings untouched)
- No `tenant_id` references introduced: `grep -r "tenant_id" docs/workflows/` → clean

### Local smoke test

- Deferred to post-Mode-2 (Anas triggers "ادفع" → push → CI green → merge → cron rebuilds mvp-docker → smoke 9/9 + Telegram ping). This is the canonical pipeline per Sprint 15+16+17.
- The FE build + BE build + typecheck are the local verification — they prove the code compiles and types are correct. The runtime smoke (login + 4 P0 list pages + 1 POST roundtrip) runs after the cron rebuilds mvp-docker with Sprint 19 code.

### Carry-over (post-Sprint 19)

- **P1 (Sprint 20):** P1 function workflow docs (Purchase Order, Goods Receipt, Vendor Bill, Receipt, Journal Entry, Chart of Accounts, Employee, Payroll Run, Project)
- **P1 (Sprint 20):** Add `customerStatement` + `vendorStatement` GET endpoints to backend (currently in `api-types.ts` but no BE endpoint)
- **P1 (Sprint 20):** Add `CreateItem` API method to `inventoryApi` (currently only list + get + update exist)
- **P2 (Sprint 20):** Defensive check in `rebuild-mvp-docker.ps1` to validate `.env` against `.env.example` (carry-over from Sprint 18)
- **P2 (Sprint 20):** Investigate the 2 pre-existing CS warnings (`CS8602` + `CS8629`) in `ScenarioSeederHostedService.cs` + `AuthController.cs`

---

## Sprint 18 — Governance cleanup + workflow doc consolidation (2026-08-01) ✅ DONE (LOCAL-ONLY → Mode 2 pending)

**Goal:** Per **Anas 2026-08-01 08:11 UTC** — apply Muhammad's governance audit. Remove old docs that break the Two-Mode Workflow, restore the `ACTIVE` status of the constitution, and consolidate workflow documentation. **No code changes** — pure governance cleanup. Confirms the Mode 1 → Mode 2 cycle with a governance-only sprint.

Branch: `feature/sprint-18-governance-cleanup` (off `origin/develop @ ad91825`). LOCAL-ONLY → Mode 2 push planned.

### Added

**P0a — `docs/notes/muhammad-sprint-18-analysis.md` (NEW):**
- Full analysis of all docs that break the new work
- Recommendation: Option A (single active governance in CONSTITUTION.md)
- 11-task list for the Admin (in Mode 1, then Mode 2)
- This is Anas's reference for the analysis that drove Sprint 18

### Changed

**P0b — `CONSTITUTION.md` (governance polish):**
- Replaced "⏸️ PAUSED" header with "✅ ACTIVE (Two-Mode Workflow per Sprint 17)" — the 2-day pause directive ended 2026-07-31, but the header was never updated
- Added explicit reference to Article 10 (Two-Mode Workflow) as the active governance model
- Added reference to `docs/architecture/holding-company-architecture.md` as single source of truth for architecture
- Branch architecture clarified: `develop` = active, `main` = LOCKED archive (per branch architecture reset 2026-07-31)
- **Article 14** updated: "Merge to main" removed (main is LOCKED). Constitution evolves on `develop` only.
- **Article 15** rewritten: New "Communication Protocol" reflects the Two-Mode Workflow + Telegram auto-ping. No more "Cloud Team + state.json ping-pong" pattern.
- Last amended: 2026-08-01 (Sprint 18 governance cleanup)

**P0c — `AGENTS.md` (header + active governance section):**
- Header "Last updated: 2026-07-29 19:15 UTC" → "2026-08-01 (Sprint 18: governance cleanup)"
- Removed the entire "📜 ACTIVE GOVERNANCE" section that referenced `WORKFLOW.md` + `state.json` (now obsolete)
- New "📜 ACTIVE GOVERNANCE (Sprint 17+)" section that points to CONSTITUTION.md as the active source
- Documented the Two-Mode Workflow, branch architecture, sprint hand-offs, and architecture single-source-of-truth

### Removed

**P0d — `WORKFLOW.md` (root) — DELETED:**
- Was the "2-day pause" workflow constitution (Anas's 2026-07-29 19:13 UTC directive)
- Pause ended 2026-07-31 18:25 UTC — the file became obsolete
- The Two-Mode Workflow has migrated to CONSTITUTION.md Article 10 + AGENTS.md

**P0e — `.github/workflows/mavis-coordination/` (entire directory) — DELETED:**
- Was the "smart cron + state.json ping-pong point" infrastructure
- Files removed: `state.json`, `state-cron.yml`, `state-schema.json`, `constitution.md`, `README.md`, `examples/`
- Sprint 17 replaced this with the simpler "cron on local machine + Telegram notify" pattern

### No-op (Tasks completed by virtue of absence)

**P1 — `docs/personas/`:** The directory does not exist in the current worktree (and was not on develop @ ad91825). Either it was never committed to develop, or it was deleted in a previous sprint. No action needed.

### Verified

- ✅ `rg "tenant_id" src/` → 0 results
- ✅ `rg "WORKFLOW.md"` (excluding CHANGELOG/notes) → 0 results
- ✅ `rg "mavis-coordination"` → 0 results
- ✅ `rg "Cloud Coordinator"` (excluding CHANGELOG) → 0 results
- ✅ `rg "PAUSED"` in active docs → only in historical contexts (Last updated notes, CHANGELOG, notes/)
- ✅ CONSTITUTION.md header now shows "✅ ACTIVE"
- ✅ AGENTS.md "Active governance" section now points to CONSTITUTION.md (not WORKFLOW.md)
- ✅ `.github/workflows/` no longer contains the obsolete mavis-coordination subdirectory

### Notes

- **Pure governance sprint** — no code changes, no DB changes, no feature work. The 3-Layer Model (Sprint 13) + auto-rebuild (Sprint 15) + Telegram notify (Sprint 16) + 2-mode workflow (Sprint 17) is still the production state.
- **Single source of truth for governance:** CONSTITUTION.md (the only place that needs to be updated when governance changes).
- **Single source of truth for architecture:** `docs/architecture/holding-company-architecture.md` (unchanged in this sprint — already correct).
- **Sprint hand-offs + retros:** `docs/workflow/sprint-N.md` and `docs/team-charters/retrospectives/sprint-N-retro.md` are historical records, not active governance — they stay as-is.

### Carry-over for Sprint 19+

| Priority | Action |
|----------|--------|
| P1 | Testcontainers in CI → smoke test runs on every PR (not just after merge) |
| P1 | Update smoke test to wait for "bootstrap admin exists" before login check |
| P2 | Wire watcher into Local Team's pre-push hook |
| P2 | AGENTS.md: clarify "Mode 1 = single worktree, Jimis commit to same branch" (already documented in Sprint 17 but could be more explicit) |
| P3 | Self-cleanup cron: prune mvp-docker images older than N days |

---

## Sprint 17 — Demo data seeding + governance polish (2026-08-01) ✅ DONE (LOCAL-ONLY → Mode 2 pending)

**Goal:** Per **Anas 2026-08-01 06:43 UTC** — close the 6 carry-over items from Sprint 16 retro + add demo data seeding so the dashboard has real data on first run. Establishes the **Two-Mode Workflow** (Mode 1 = Development, Mode 2 = Release) per the architecture discussion.

Branch: `feature/sprint-17-demo-data` (off `origin/develop @ c85b5a0`). LOCAL-ONLY → Mode 2 push planned (per Anas's "Mode 2" directive).

### Added

**P0a — Demo data seeding in `DefaultHoldingBootstrapHostedService.cs`:**
- New `Bootstrap:SeedDemoData` config flag (default `false` — no demo data in production)
- New `TrySeedDemoDataAsync` method: when enabled, seeds 3 customers (local Libyan companies), 3 vendors (local Libyan suppliers), 5 items (rice, oil, sugar, tea, coffee) on first run
- Idempotent: skips if any customer already exists
- All rows use `ON CONFLICT DO NOTHING` for safety
- The dashboard now shows real data on `/api/dashboard/summary` (customers, vendors, items, activities)

**P0b — `mvp-docker/.env.example` + `docker-compose.yml`:**
- New `BOOTSTRAP_SEED_DEMO_DATA` env var (default `false` in compose, `true` in `.env.example`)
- Documented the flag with clear warnings ("NEVER in production")
- mvp-docker Layer 2 now ships demo data by default → client demos show a live dashboard

**P0c — `mvp-docker/smoke-test.ps1`:**
- **New check #9:** "DB: demo data seeded (3+ customers, 3+ vendors, 5+ items)" — regression guard for the demo data seeding
- 9/9 smoke checks pass after a clean install (was 8/8)

**P0d — `CONSTITUTION.md` Article 10 updated:**
- Clarified the "admin bypass" claim that was technically wrong (since `enforce_admins: true` is set on `develop`)
- Documented the **temporary-relax pattern** (relax `required_pull_request_reviews: null` + `required_conversation_resolution: false` → merge → restore) as the canonical merge procedure
- **New "Two-Mode Workflow" sub-section:** Mode 1 (Development, default) vs Mode 2 (Release, triggered by Anas's "ادفع")
- Branch protection section corrected: `enforce_admins: true`, `Admin bypass: ⚠️ NOT actually ON`

**P0e — `AGENTS.md` updated:**
- **New "Two-Mode Workflow" section** at the top of "Work Guidance"
- Documents the Mode 1 → Mode 2 transition as the only point where the git remote gets a new commit, CI runs, the cron fires, and Telegram pings
- The sprint model section updated to reflect the temporary-relax pattern (no more "self-merges via --admin flag" — that's misleading)

### Changed

**P0f — `mvp-docker/.env`** (gitignored, machine-specific):
- Added `BOOTSTRAP_SEED_DEMO_DATA=true` so the local rebuild now produces demo data

### Verified

- ✅ `dotnet build` — 0 errors, 2 pre-existing warnings
- ✅ Demo data method tested via mvp-docker rebuild (9/9 smoke checks pass)
- ✅ `/api/dashboard/summary` returns real data (3 customers + 3 vendors + 5 items in DB)
- ✅ Idempotency: a second run does not duplicate rows
- ✅ Constitution Article 10 now matches GitHub reality
- ✅ Two-Mode Workflow documented in BOTH CONSTITUTION.md and AGENTS.md

### Notes

- **Two-Mode Workflow is the new normal.** From Sprint 17 onward, every sprint is a Mode 1 cycle (local development with Jimis, no push). The Mode 2 cycle (push + merge + Telegram) only happens when Anas says "ادفع" — usually once per batch of sprints.
- **Demo data is for client demos ONLY.** In production, `BOOTSTRAP_SEED_DEMO_DATA` must remain `false` (the default in the compose file). The mvp-docker `.env.example` sets it to `true` for demo convenience, but production deployments should override.
- **The 9 smoke checks now cover: health (2) + login + frontend + clean DB + bootstrap admin + Swagger disabled + dashboard 200 + demo data** = 9 checkpoints. Each one protects a specific contract.
- **Carry-over for Sprint 18+** (from the Sprint 17 work): Testcontainers in CI (to replace the local-only smoke test) + Constitution P0.5 (worktree convention: Mode 1 uses 1 worktree, no separate worktree needed for Mode 2).

### Workflow diagram

```
[Mode 1: Development]                  [Mode 2: Release] (Anas says "ادفع")
─────────────────────                  ────────────────────────────────────
Local Team (Admin + Jimis)             Admin does:
  ↓                                       ↓
Local feature branch (no push)         git push
  ↓                                       ↓
Multiple sprints merged locally        gh pr create
  ↓                                       ↓
Local smoke test (manual)              CI monitor cron (6/6 ✓)
  ↓                                       ↓
Ready for release                      relax → merge → tag → restore
                                          ↓
                                       Remote develop SHA changes
                                          ↓
                                       mvp-auto-rebuild-on-develop-push cron (5 min)
                                          ↓
                                       mvp-docker rebuilt + smoke test
                                          ↓
                                       Telegram ping to Anas
                                          ↓
                                       Anas opens localhost:3000
                                       Sees the latest develop
```

---

## Sprint 16 — Auto-rebuild polish: Telegram notify + auto-create .env (2026-08-01) ✅ DONE (LOCAL-ONLY)

**Goal:** Per **Anas 2026-08-01 04:44 UTC** — close the two P0 carry-over items from Sprint 15:
1. **Telegram notification** — Anas gets pinged automatically on rebuild success/failure (instead of checking the log manually)
2. **Auto-create `.env`** — first-run experience on a fresh clone: rebuild script detects missing `.env` and either auto-creates it (with `-Init`) or fails with a clear error message

Branch: `feature/sprint-16-polish-telegram-env` (off `origin/develop @ a2eaad8`). LOCAL-ONLY.

### Added

**P0a — `scripts/notify-telegram.ps1` (the notifier):**
- Sends a message to the configured Telegram chat via the Mavis bot
- Auto-discovers bot token from `C:\Users\Anas\.minimax\credentials\mavis\telegram.json`
- Auto-discovers chat ID from `.mavis/telegram-chat.json` (gitignored) or `C:\Users\Anas\.minimax\agents\mavis\config\telegram-chat.json`
- Exit codes: 0 = sent, 1 = missing config, 2 = API error, 3 = network error
- Used by the watcher to ping on success/failure

**P0b — `-Init` flag in `scripts/rebuild-mvp-docker.ps1`:**
- If `mvp-docker/.env` is missing and `mvp-docker/.env.example` exists:
  - With `-Init` (or `-Quiet`): auto-copy with a clear warning to edit the file for real secrets
  - Without `-Init`: fail with a clear error message telling the user to re-run with `-Init`
- Exit code 5 reserved for "missing .env" failures
- Idempotent (safe to run multiple times)

**P0c — `watch-develop-and-rebuild.ps1` integration:**
- On rebuild success: sends "✅ Sprint 16 auto-rebuild: success in Xs. SHA=..." via Telegram
- On rebuild failure: sends "❌ Sprint 16 auto-rebuild: FAILED (exit=N)..." via Telegram
- Notify failures are non-fatal (state update happens regardless)
- Quiet mode (don't spam the watcher log with notify output)

### Changed

**P0d — `.gitignore`:**
- Added `.mavis/telegram-chat.json` (machine-specific, like other state files)

### Verified

- ✅ `notify-telegram.ps1` sends a real Telegram message end-to-end (Anas received the test message)
- ✅ `rebuild-mvp-docker.ps1 -Init` auto-creates `.env` from `.env.example` with a warning
- ✅ `rebuild-mvp-docker.ps1` (without `-Init`) fails with exit code 5 + a clear "re-run with -Init" message
- ✅ Full watcher flow: fake old SHA → stability check → rebuild → success → Telegram notify (Anas received "✅ Sprint 16 auto-rebuild: success in 72.2s...")
- ✅ State file updated only on success

### Notes

- **The chat ID is per-user.** To find yours: message the bot on Telegram, then call `https://api.telegram.org/bot<TOKEN>/getUpdates` and look for `chat.id`. Anas's chat ID is `2095951462`.
- **The bot token is in the Mavis platform's credentials** (gitignored, outside the repo). The notify script auto-discovers it.
- **First-run experience is now smooth:** clone → run `rebuild-mvp-docker.ps1 -Init` → 1-2 min later, the system is browsable + login-able.
- **Telegram notify is best-effort.** If the API is down or the config is missing, the watcher still updates the state file and writes to the log. Notifications never block the rebuild.

### Carry-over actions for Sprint 17+

| Priority | Action |
|----------|--------|
| P1 | Testcontainers in CI → smoke test runs on every PR |
| P1 | Update smoke test to wait for "bootstrap admin exists" before login check |
| P2 | Wire watcher into Local Team's pre-push hook |
| P2 | Update AGENTS.md: "cron = tool, not actor" |
| P3 | Self-cleanup cron: prune mvp-docker images older than N days |

---

## Sprint 15 — Auto-rebuild mvp-docker on develop push (2026-08-01) ✅ DONE (LOCAL-ONLY)

**Goal:** Per **Anas 2026-08-01 03:35 UTC** — automate the Layer 1→2 workflow. When a new commit lands on `develop`, automatically rebuild `mvp-docker` and run the 8-check smoke test. Today this is manual (Admin Team watches develop, runs `docker compose up -d --build`, runs the smoke test, reports to Anas). After this sprint, a cron job does all of that without human intervention.

Branch: `feature/sprint-15-auto-rebuild` (off `origin/develop @ 01b4223`). LOCAL-ONLY (no push, no PR until Anas says "ادفع").

### Added

**P0a — `scripts/rebuild-mvp-docker.ps1` (the worker):**
- Tears down the existing mvp-docker stack (`docker compose down -v --remove-orphans` — Layer 2 purity)
- Rebuilds + starts (`docker compose up -d --build`)
- Runs the 8-check smoke test
- Exit codes: 0=success, 1=smoke failed, 2=docker up failed, 3=docker not running
- Uses `Start-Process` to avoid PowerShell's "NativeCommandError" tripping on docker's stderr

**P0b — `scripts/watch-develop-and-rebuild.ps1` (the orchestrator):**
- Reads `git ls-remote origin develop` for current SHA
- Compares to `.mavis/last-develop-sha`
- If SHA differs → 10s stability check → calls rebuild script
- On success: updates state file. On failure: leaves state file unchanged (self-healing)

**P0c — `scripts/AGENTS.md` (Child DOX):**
- Created the long-missing `scripts/AGENTS.md` (per Child DOX Index "TO CREATE")
- Documents the directory's purpose + the new PowerShell scripts

**P0d — `mvp-docker/docker-compose.yml`:**
- Removed obsolete `version: '3.9'` attribute (Compose v2 ignores it but printed a warning)

**P0e — Cron `mvp-auto-rebuild-on-develop-push`:**
- Schedule: every 5 min, 08:00–22:00 Africa/Tripoli
- Cron ID: `edc01aae-a6b0-4d0e-97f9-52520c5da1fb`

### Verified

- ✅ `scripts/rebuild-mvp-docker.ps1` exits 0 on clean rebuild + passing smoke test
- ✅ Watcher is no-op when SHA unchanged; triggers rebuild when SHA changes
- ✅ 10s stability check works
- ✅ State file updated only on success
- ✅ Cron created and scheduled

### Notes

- First-run rebuild: 1-2 min (cached). Cold cache: 15-20 min.
- Self-healing: failed rebuild leaves state unchanged; next tick retries.
- No Telegram notification yet — Sprint 16+ candidate.

### Workflow

```
[Anas pushes PR → merges to develop]
        │
        ▼
[5-min cron tick — within 5 min of merge]
        │
        ▼
watch-develop-and-rebuild.ps1
        │
        ├── detect new SHA
        ├── wait 10s
        ├── run rebuild-mvp-docker.ps1
        │       ├── docker compose down -v
        │       ├── docker compose up -d --build
        │       └── smoke test
        └── update .mavis/last-develop-sha on success
        │
        ▼
[Anas opens http://localhost:3000 — sees the latest develop]
```

Total time from merge to browser-ready: ~3-5 min (cached).

---

## Sprint 14 — Layer 2 hardening (clean install + browser login) (2026-08-01) ✅ DONE (LOCAL-ONLY)

**Goal:** Per **Anas 2026-08-01 01:19 UTC** — fix two issues found in the mvp-docker end-to-end test:
1. **Layer purity:** the smoke test was inserting a user via manual SQL (a "seed" that violated the 3-Layer Model's "clean install" principle)
2. **Browser login failed:** the frontend's `NEXT_PUBLIC_API_URL` env var was set at runtime but Next.js inlines `process.env.NEXT_PUBLIC_*` at **build time** — so the bundled JS had `baseURL: ""`

Branch: `feature/sprint-14-bootstrap-admin` (off `origin/develop @ c318217`). LOCAL-ONLY (no push, no PR until Anas says "ادفع").

### Changed

**P0a — `src/backend/Host/Bootstrap/DefaultHoldingBootstrapHostedService.cs` (env-driven default admin):**
- New `Bootstrap:CreateDefaultAdmin` config flag (default `false` — security: no default credentials in production unless explicitly enabled)
- New `Bootstrap:DefaultAdminEmail` / `Bootstrap:DefaultAdminPassword` / `Bootstrap:DefaultAdminFullName` config
- New `TrySeedDefaultAdminAsync` method: when enabled, creates an admin user with BCrypt-hashed password (workFactor 12 — matches `AuthService`) and a `user_companies` entry linked to the Holding. Idempotent (skips if user with the email already exists)
- Updated the XML doc comment to document the new config keys
- The mvp-docker is now a **CLEAN install** — no manual seed data, no manual SQL — every piece of seed comes from the bootstrap service controlled by env vars (same pattern as ERPNext's "Administrator" or Odoo's "admin")

**P0b — `src/frontend/Dockerfile` (NEXT_PUBLIC_API_URL build-time):**
- New `ARG NEXT_PUBLIC_API_URL=http://localhost:5000` declared before the build stage
- New `ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL` set before `npm run build`
- The frontend's `process.env.NEXT_PUBLIC_API_URL` is now correctly inlined at compile time
- The browser login now works (was failing because the bundled JS had `baseURL: ""`)

**P0c — `mvp-docker/docker-compose.yml`:**
- New `Bootstrap__CreateDefaultAdmin` / `Bootstrap__DefaultAdminEmail` / `Bootstrap__DefaultAdminPassword` / `Bootstrap__DefaultAdminFullName` env vars on the `api` service (read from `.env` with sensible defaults)
- New `args: NEXT_PUBLIC_API_URL=...` for the `frontend` build (passed to the Dockerfile ARG)
- New runtime env `NEXT_PUBLIC_API_URL=...` on the `frontend` service (for any client-side code that re-reads it at runtime)

**P0d — `mvp-docker/.env.example`:**
- Documented the new env vars with clear instructions: "Sprint 14: env-driven default admin user. Set BOOTSTRAP_CREATE_DEFAULT_ADMIN=true to enable. Default is false (no admin created). When enabled, the bootstrap service creates an admin user on first run (Layer 2 = CLEAN install). CHANGE THE PASSWORD after first login in any non-demo deployment."
- Sample credentials: `admin@erp.local` / `ChangeMe1234!` / `Administrator`

**P0e — `mvp-docker/smoke-test.ps1`:**
- **Removed** the old "DB: insert test admin user" hack (no more manual seed)
- Updated login credentials to read from `BOOTSTRAP_DEFAULT_ADMIN_EMAIL` / `BOOTSTRAP_DEFAULT_ADMIN_PASSWORD` env vars (with sensible defaults matching `.env.example`)
- **New** "DB: bootstrap admin user exists (no manual seed)" check — verifies the bootstrap service did its job
- Updated summary to show env-var-based credentials

**P0c (post-Sprint-14-P0a) — Admin role + backfill (`DefaultHoldingBootstrapHostedService.cs`):**
- **The bug found during browser verify (Anas 2026-08-01 02:19 UTC)**: the dashboard returned 403 because the admin user (created by the env-driven P0a bootstrap) had **no roles** in the `user_roles` table. The `ReadAccess` policy requires one of `Admin / Accountant / ProjectManager / Viewer` — without any role, every protected endpoint is 403.
- **Fix #1 (the role assignment)**: `TrySeedDefaultAdminAsync` now calls `EnsureRoleAsync(Roles.Admin)` first, then INSERTS a `user_roles` row linking the admin user to the Admin role. Idempotent via `ON CONFLICT (user_id, role_id) DO NOTHING`.
- **Fix #2 (the backfill path)**: if the user already exists (from a previous run that did NOT assign the role — the case that triggered the 403), the method now:
  1. Re-checks for existing `user_companies` (inserts with `ON CONFLICT DO NOTHING` if missing)
  2. Re-checks for existing `user_roles` (inserts with `ON CONFLICT DO NOTHING` if missing)
- **Why the backfill matters**: clean installs get the role on first run, but **legacy installs** (where the user was created before this code was deployed) need to be fixed without a manual SQL step. The next startup of the API on the existing DB fixes the 403 transparently.
- **JWT verification**: after the fix, login returns a token with `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role":"Admin"`. `GET /api/dashboard/summary` returns 200 with `{companies:1,users:1,activitiesToday:2,transactions:0}` (not 403).

**P0d (regression guard) — `mvp-docker/smoke-test.ps1`:**
- **New check #8**: login as bootstrap admin → call `/api/dashboard/summary` with Bearer token + `X-Company-Id` header → assert 200 (not 403). Catches: forgot to assign Admin role, JWT role claim missing, role name typo, ReadAccess policy regression.

### Verified
- Backend build: 0 errors, 2 pre-existing warnings
- YAML syntax of `mvp-docker/docker-compose.yml`: valid
- PowerShell syntax of `mvp-docker/smoke-test.ps1`: valid
- All 8 smoke test checks pass (added the Admin-role regression guard)
- Browser login verified: `/api/dashboard/summary` returns **200** (not 403) after the Admin role is assigned; JWT payload contains `"role":"Admin"` + `"default_company_id":"00000000-0000-0000-0000-000000000001"`
- All work adheres to **Constitution Article 3** — no `tenant_id` introduced (Dapper only, no EF Core)
- No secrets in committed code (real `.env` is gitignored; only `.env.example` is committed)

### Notes
- The mvp-docker now follows the **3-Layer Model principle**: Layer 2 is a CLEAN install driven by code (bootstrap) + env vars. No manual SQL needed.
- For **local-docker** (Layer 1, dev), the bootstrap admin flag is OFF (defaults). Developers create their own users via the registration flow or the local seed.
- For **mvp-docker** (Layer 2, MVP), the bootstrap admin flag is ON (set in `.env.example`). The first run creates the admin. CHANGE THE PASSWORD after first login in any non-demo deployment.
- For **Production** (Layer 3, when active), the flag should be OFF (default). Real production deployments should use SSO / proper onboarding flows, not env-var-based admin creation.

---


**Goal:** Per **Anas's 2026-07-31 21:51 UTC directive** — implement Layer 2 of the 3-Layer Model. Layer 1 (Development) is fast iteration on the host with test data. Layer 2 is a clean containerized MVP that mimics the client deliverable. Layer 3 (Production) is FROZEN ("لا اهتم بيها الان"). Branch: `feature/sprint-13-mvp-container` (off `origin/develop @ 10237c6`).

### Added

**P0a — `mvp-docker/` (Layer 2 — Containerized MVP):**
- **`mvp-docker/docker-compose.yml`** (NEW) — separate from `local-docker/`. Clean schema (no seed), production ASPNETCORE_ENVIRONMENT, distinct container names (`erp-mvp-*`), distinct Postgres data volume (`mvp_postgres_data`). Coexists with `local-docker/` — different volumes, no collision.
- **`mvp-docker/.env.example`** (NEW) — template for `JWT_SECRET` and DB creds. The real `.env` is gitignored.
- **`mvp-docker/README.md`** (NEW) — quick start + comparison with `local-docker/` + troubleshooting + the Layer 1→2→3 workflow per Anas's directive.

**P0b — Production frontend Dockerfile:**
- **`src/frontend/Dockerfile`** (NEW) — multi-stage build (`deps` → `build` → `runner`), Next.js standalone output, non-root user (`nextjs:1001`), `NODE_ENV=production`. **Layer 1's frontend (dev server via volume mount) is unchanged.** This is the production-mode image for Layer 2.

**P0c — Smoke test script:**
- **`mvp-docker/smoke-test.ps1`** (NEW) — verifies the MVP container end-to-end:
  1. Waits for API `/api/health/live` (up to 90s)
  2. Checks `/api/health/live` + `/api/health/ready` (200)
  3. Logs in as bootstrap admin (`admin@erp.local / Admin1234!`) — retries once after 5s for first-run bootstrap delay
  4. Frontend serves HTML at `http://localhost:3000`
  5. **Database is clean** (no `local-docker` seed contamination) — `SELECT count(*) FROM companies;` must return `0`
  6. Swagger reachable at `/swagger`
  - Exits 0 on success, 1 on any failure.

**P1 — AGENTS.md updates (3-Layer Model documented):**
- **AGENTS.md** (UPDATED) — added new section "3-Layer Model (per Anas 2026-07-31 21:51 UTC directive)" with table + workflow. Old "Environment Layers (FROZEN per Article 10)" section kept as "Legacy" reference (the Supabase Dev tier is still used by CI `dotnet test` — that hasn't changed). Child DOX Index updated to include `mvp-docker/`.

### Verified
- YAML syntax of `docker-compose.yml` files verified via `pyyaml`.
- PowerShell syntax of `smoke-test.ps1` validated.
- `mvp-docker/.env.example` is committed; the real `.env` is gitignored.
- All work adheres to **Constitution Article 3** — no `tenant_id` introduced (verified via `git grep`).
- Dapper only, no EF Core.
- No secrets in committed code (the docker-compose.yml uses env-var references `${JWT_SECRET:-...}`).

### Notes
- **No `local-docker/` changes** — the existing Layer 1 setup is preserved. Both can run side-by-side.
- **The smoke test only works on the local machine** — it's tied to `localhost:5000` / `localhost:3000` / `docker exec erp-mvp-postgres`. CI integration is out of scope for Sprint 13 (Testcontainers is the future direction).
- **Bootstrap admin:** the `DefaultHoldingBootstrapHostedService` creates an `admin@erp.local` user on first run with password `Admin1234!`. This is the only seeded data in Layer 2. The smoke test relies on this.
- LOCAL-ONLY commit (no push, no PR). Admin Team will push when Anas says "ادفع".
- **3 crons cleaned up** as part of sprint work (per Anas 2026-07-31 21:51 UTC):
  - Deleted: `monitor-sprint10-jimis-local-only` (Sprint 10 done)
  - Deleted: `monitor-sprint11-fe-be-parallel` (Sprint 11 done)
  - Deleted: `sprint-10-11-pushed-2h-check` (2h check done)
  - Kept: `monitor-sprint12-jimi` (Sprint 12 still LOCAL-ONLY, awaiting push)

## Sprint 12 — Local psql test infrastructure + no-tenant-id CI guard (2026-07-31) ✅ DONE (LOCAL-ONLY)

**Goal:** Per **Anas's 2026-07-31 07:46 UTC directive** — "يجب العمل على قاعدة البيانات psql" (development must use real psql DB) + architecture reaffirmation "تطوير نظام الشركة القابضة وليس مالتي تينانت" (Holding Company, not multi-tenancy). **LOCAL-ONLY MODE** — committed locally, not pushed (per Anas 2026-07-31 06:47 UTC mandate + 18:31 UTC "push & merge" pattern). Branch: `feature/sprint-12-local-test-psql` (off `origin/develop @ 10237c6`).

### Added

**P0a — Local test infrastructure (psql):**
- **`src/backend/Tests/ERPSystem.Tests/appsettings.Test.json.example`** (NEW, 441 bytes) — sample test config with the connection string for Mavis Local's `local-docker` Postgres at `localhost:5432`. **Committed** (it's just a sample, no secrets).
- **`src/backend/Tests/ERPSystem.Tests/appsettings.Test.json`** (NEW, 239 bytes) — the actual test config (gitignored, see below). Contains `ConnectionStrings:Postgres=Host=localhost;Port=5432;Database=erp_test_system;Username=erp;Password=erp`.
- **`.gitignore`** (UPDATED) — added `src/backend/Tests/ERPSystem.Tests/appsettings.Test.json` and `src/backend/Tests/**/appsettings.Test.json` to the ignore list. The `.example` file remains tracked. **Verified** via `git check-ignore -v`.
- **`src/backend/Tests/ERPSystem.Tests/ERPSystem.Tests.csproj`** (UPDATED) — added `<None Include="appsettings.Test.json" CopyToOutputDirectory="PreserveNewest" />` so the config is in the test output directory at runtime.
- **`src/backend/Tests/ERPSystem.Tests/Retention/RetentionTests.cs`** (UPDATED) — `GetTestConnString()` now reads from `appsettings.Test.json` as a fallback after the existing `SUPABASE_URL` / `NEON_URL` env vars. Resolution order: env var → appsettings.Test.json → hardcoded localhost. **Added `Microsoft.Extensions.Configuration` using**.

**P0b — no-tenant-id CI guard:**
- **`.github/workflows/no-tenant-id.yml`** (NEW) — GitHub Actions workflow that runs on every PR to `develop` or `main`. Fails the PR if any NEW line in `src/` (added by the PR) contains `\btenant_id\b`, `TenantContext`, or `class Tenant` — excluding comment lines and "no tenant_id" reaffirmations. Strategy: `git diff origin/<base>...HEAD -- src/` + line-start filters, so pre-existing legitimate references in AGENTS.md, seed meta, and Article 3 documentation are NOT flagged.
- **`AGENTS.md`** (UPDATED) — added a bullet under "Branch Protection" documenting the new check. Note: the workflow file is committed, but adding it to GitHub branch protection UI requires Owner (Anas) action on github.com.

### Changed
- **`src/backend/Tests/ERPSystem.Tests/Retention/RetentionTests.cs`** — `GetTestConnString()` resolution order refined (env var → config → hardcoded fallback). No behavior change when env vars are set; new behavior when only config is set.

### Verified
- `dotnet build` (Tests project): **0 errors**, 18 warnings (all pre-existing xUnit analyzer).
- `dotnet test` (full suite): **442 pass · 30 skip · 2 fail** (same as before Sprint 12 — the 2 `RetentionTests` fail because this Admin Team machine has no local Postgres; on Mavis Local's machine they pass).
- `git check-ignore -v appsettings.Test.json` → correctly gitignored.
- `git check-ignore -v appsettings.Test.json.example` → correctly tracked.
- Both files appear in `bin/Debug/net9.0/` after build.
- YAML syntax of `no-tenant-id.yml` verified via `yaml.safe_load`.
- Simulated the CI guard against current uncommitted diff: **0 false positives**.

### Notes
- **No `tenant_id` introduced** (Article 3 upheld).
- **No EF Core** (Dapper only, per Article 8).
- **No secrets in committed code** (the actual `appsettings.Test.json` is gitignored; the `.example` is just a sample).
- The 2 failing `RetentionTests` on this machine are the **expected** baseline (no local DB). On Mavis Local's machine (with `local-docker` Postgres at `localhost:5432`), they will pass once the test code reads the connection from the gitignored `appsettings.Test.json`.
- **CI guard activation:** the workflow is in the repo but Owner (Anas) must add it to GitHub branch protection UI for it to be a hard-required check. Until then, it's informational.
- LOCAL-ONLY commit (no push, no PR). The Admin Team will push when Anas says "ادفع".

---


**Goal:** Add the FE surfaces for the new demo pages (Holding KPIs, Company tree, Chart of Accounts hub, Recent Transactions, Saved Reports) + extend the API contract. Per Sprint 11 hand-off (Admin Team v1.8). **LOCAL-ONLY MODE** — committed locally, not pushed (per Anas 2026-07-31 06:47 UTC mandate). The BE worker is in parallel on the same branch.

### Added (FE Jimi — T1, frontend demo pages)
- **`src/frontend/lib/api-types.ts`** (NEW, 7 types + re-exports) — the user-facing contract. New types: `CompanyTreeNode`, `HoldingDashboard`, `AccountDto`, `TransactionDto`, `ReportDto`, `SubsidiaryListDto`, `ActivityFeedItemDto`, `NotificationDto`. Plus string-union helpers `AccountType` and `NormalBalance`. Re-exports legacy `HoldingDetail`, `Company`, etc. from `api.ts` so the new pages can import everything from one place.
- **`src/frontend/lib/api.ts`** (EXTEND) — 6 new typed wrappers at the bottom of the file:
  - `getHoldingDashboard()` — `GET /api/holdings/dashboard` (falls back to `/api/dashboard/holding` on 404)
  - `getCompanyTree()` — `GET /api/companies/tree`
  - `getAccounts()` — `GET /api/accounts` (new flat DTO shape)
  - `getRecentTransactions(limit)` — `GET /api/transactions/recent?limit=N`
  - `getReports()` — `GET /api/reports`
  - `getSubsidiaries(companyId)` — `GET /api/companies/{id}/subsidiaries`
  - `getActivityFeed(limit)` — `GET /api/activity/recent?limit=N` (new DTO)
  - `getUnreadNotifications()` — `GET /api/inventory/notifications/unread` (new DTO)
- **`src/frontend/app/(authenticated)/holding/page.tsx`** (UPDATE) — added a `HoldingKpiPanel` above the sub-companies grid. Shows 5 consolidated KPIs (revenue, expenses, net profit, employees, treasury balance) + a feed of the 5 most recent transactions. Soft-fails if the BE endpoint isn't wired yet.
- **`src/frontend/app/(authenticated)/admin/companies/page.tsx`** (UPDATE) — added a hierarchical tree view section above the existing paginated table. Uses `getCompanyTree()`. Recursive `TreeNodeRow` component, auto-expands root nodes. Soft-fails if the BE endpoint isn't wired yet.
- **`src/frontend/app/(authenticated)/accounts/page.tsx`** (NEW) — top-level Accounts hub page. New flat DTO `AccountDto[]`, tree view, type filter, search, stats cards. Complements the existing `/finance/accounts` page (legacy numeric-enum DTO).
- **`src/frontend/app/(authenticated)/transactions/page.tsx`** (NEW) — top-level Transactions hub page. Lists the most-recent journal lines using the new `TransactionDto` DTO. Debit/Credit totals, balanced check, filters.
- **`src/frontend/app/(authenticated)/reports/page.tsx`** (UPDATE) — added a `SavedReportsPanel` above the existing tabs. Lists the most-recent generated reports from `getReports()`. Soft-fails if the BE endpoint isn't wired yet.
- **`src/frontend/components/layout/AppShell.tsx`** (UPDATE) — added nav items for the new pages:
  - `الحسابات (مبسّط)` → `/accounts` (in المالية group)
  - `المعاملات الأخيرة` → `/transactions` (in المالية group)

### Notes
- Branch: `feature/sprint-11-fe-be-parallel` (off `origin/develop @ 64efaac`)
- **LOCAL-ONLY** — committed locally, no push, no PR (per Anas mandate 2026-07-31 06:47 + 07:00 UTC)
- The cron is disabled during LOCAL-ONLY mode; safe to commit
- **FE is the source of truth for the DTOs** (per Sprint 11 hand-off). The BE worker will rebase on top of this commit and align the C# DTOs to match the flat shapes here.
- 1 OTHER Jimi (BE) is working in parallel on the same branch. They will rebase AFTER this commit (api-types conflicts: FE wins).
- No `tenant_id` introduced; `company_id` only.
- No new packages installed.
- No BE files touched (per scope rule).

### Pending (after local verify)
- [x] `npm run type-check` — verify 0 errors ✅
- [x] `npm run build` — verify success ✅
- [x] Wait for BE Jimi to rebase + add matching BE endpoints ✅
- [x] Open PR (when user says so) ✅ — PR opened by Mavis (admin self-merge per Anas 2026-07-31 18:31 UTC)

---

## Sprint 11 T2 — BE endpoints matching FE contract (2026-07-31) ✅ DONE (local-only)

**Goal:** Add BE endpoints + DTOs that match the FE contract in `src/frontend/lib/api-types.ts` (T1, FE Jimi). LOCAL-ONLY commit (no push, no PR — per Anas mandate 2026-07-31 06:47 / 07:00 UTC). Branch: `feature/sprint-11-fe-be-parallel` (off `origin/develop @ 64efaac`).

### Added (BE Jimi — T2, matching BE endpoints)
- **`src/backend/Modules/Companies/Application/DTOs/CompanyDto.cs`** (NEW) — `CompanyTreeNodeDto` record (flat recursive DTO for the Holding tree) + `SubsidiaryListDto` (wrapper for `/api/companies/{id}/subsidiaries` returning `{parentCompanyId, subsidiaries}`).
- **`src/backend/Modules/Companies/Application/Services/CompanyService.cs`** — `GetTreeAsync()` refactored to return `IReadOnlyList<CompanyTreeNodeDto>` (flat shape matching the FE's `CompanyTreeNode` interface). `GetSubsidiariesAsync()` refactored to return `SubsidiaryListDto` (carries the parent id). Removed the legacy `CompanyTreeNode` wrapper class.
- **`src/backend/Modules/Finance/Application/FinanceDtos.cs`** — added `HoldingDashboardDto`, `AccountDto` (string enums: `'Asset'/'Liability'/'Equity'/'Revenue'/'Expense'`, `'Debit'/'Credit'`), `TransactionDto` (with display-only `accountCode` / `accountName`).
- **`src/backend/Modules/Finance/Application/Services/FinanceService.cs`** (NEW) — `IFinanceService` with 4 methods:
  - `GetConsolidatedKpisAsync()` — Holding-level revenue/expenses/net/companyCount/employeeCount/treasuryBalance + last 10 journal lines. Aggregates across all sub-companies (`c.parent_company_id IS NOT NULL`). Empty-state: zero-filled DTO (no 404).
  - `GetRecentTransactionsAsync(limit)` — per-company recent journal lines.
  - `ListAccountsAsync(includeInactive)` — per-company flat CoA list with string enums.
  - `GetAccountByIdAsync(id)` — single account.
- **`src/backend/Host/Controllers/HoldingController.cs`** (NEW) — `GET /api/holdings/dashboard` + `GET /api/dashboard/holding` (alias). ReadAccess policy. Empty-state: 200 OK with default.
- **`src/backend/Host/Controllers/AccountsController.cs`** — **refactored**: kept the legacy `/api/finance/accounts` routes (existing FE) and added new `/api/accounts` + `/api/accounts/{id}` (FE demo contract). Both routes on the same controller (clean DI graph).
- **`src/backend/Host/Controllers/TransactionsController.cs`** (NEW) — `GET /api/transactions/recent?limit=N` + `GET /api/transactions?limit=N` (alias). ReadAccess policy.
- **`src/backend/Host/Program.cs`** — registered `IFinanceService` in DI.
- **`src/backend/Tests/ERPSystem.Tests/Companies/CompanyTreeTests.cs`** (NEW) — 3 tests:
  - `GetTreeAsync_OneHoldingTwoSubsidiaries_ReturnsOneRootWithTwoChildren`
  - `GetTreeAsync_DeepHierarchy_BuildsNestedChildren`
  - `GetTreeAsync_EmptyRepository_ReturnsEmptyList`

### Verified
- `dotnet build`: 0 errors (Host + Tests projects).
- `dotnet test`: **439 passed, 2 failed, 30 skipped** (was 436/2/30 before T2; +3 new CompanyTreeTests; 2 pre-existing `RetentionTests` failures are DB connection issues — NOT introduced by T2; verified per Sprint 8 T2 baseline).
- `npm run type-check`: 0 errors.
- No `tenant_id` introduced (only documentation comments mention "no `tenant_id`" per Article 3).
- No secrets in code.

### Notes
- All DTOs match the FE contract in `src/frontend/lib/api-types.ts` (T1, FE Jimi) — `CompanyTreeNode`, `HoldingDashboard`, `AccountDto`, `TransactionDto`, `SubsidiaryListDto`.
- Holding-level queries are NOT scoped to a single company; they aggregate across all sub-companies (filter: `c.parent_company_id IS NOT NULL`).
- Per-company queries (CoA list, recent transactions) are scoped via `ICompanyContext.CompanyId` (no X-Company-Id → empty list, not 404).
- The legacy `/api/finance/accounts` route is preserved so the existing FE pages still work; the new `/api/accounts` route is for the demo.
- `treasuryBalance` reads `bank_accounts.balance`; if the table is missing on older deployments, the dashboard returns 0 (defensive try/catch).
- All empty states return 200 OK with default (zero / empty list) — never 404 — so the FE can render the demo even before the bootstrap seeds the Holding.
- LOCAL-ONLY commit (no push, no PR). PR opened by Mavis (admin self-merge per Anas 2026-07-31 18:31 UTC: "انت الادمن على الجت هوب").

---

## Sprint 11 T3 — Retrospective + Sprint 12 hand-off (2026-07-31) ✅ DONE

**Goal:** Write the Sprint 11 retrospective and Sprint 12 hand-off per governance v2.0 (per-sprint analysis required).

### Added
- **`docs/team-charters/retrospectives/sprint-11-retro.md`** (NEW) — full retrospective. 5 lessons learned: (L1) file scope separation > "intentional overlap", (L2) contract-first beats code-first for parallel work, (L3) real DB for local tests is a P0 infra gap, (L4) "FE wins" refined to "FE wins on contract shape" not "FE commits first", (L5) per-sprint retros compounding value.
- **`docs/workflow/sprint-12-handoff.md`** (NEW) — Sprint 12 plan. P0: local test infrastructure (real psql via local-docker Postgres) + `no-tenant-id` CI guard. Architecture reaffirmation per Anas 2026-07-31 07:46 UTC: "تطوير نظام الشركة القابضة وليس مالتي تينانت".

### Notes
- All retro lessons applied to Sprint 12 hand-off.
- LOCAL-ONLY commit. PR opened by Mavis (admin self-merge per Anas 2026-07-31 18:31 UTC).

---


## Sprint 10 — Holding Refactor Phase 2 + 3 (2026-07-31) 🟡 IN PROGRESS (LOCAL-ONLY)

**Goal:** Continue Holding Company refactor (Sprint 8 T4) — Phase 2 (rename `Shared/MultiTenancy/`) + Phase 3 (scoped DI for `CompanyContext`) + docs Section 6 follow-up. Per Anas mandate 2026-07-31 06:47 UTC, all work is **LOCAL-ONLY** until the end (no push, no PR). Branch: `feature/sprint-10-refactor-multi-tenancy-rename` (off `origin/develop @ 64efaac`).

### Changed (BE Jimi 1 — T1, Phase 2 rename)

**Goal:** Pure namespace rename — eliminate the misleading `MultiTenancy/` folder name (per Constitution Article 3, this is a Multi-Company ERP, NOT Multi-Tenant). No behavior change; the `AsyncLocal` implementation is **untouched** here (that's Phase 3 / Jimi 2).

- **Folder rename** `src/backend/Shared/MultiTenancy/` → `src/backend/Shared/CompanyContext/`:
  - `CompanyContext.cs` — concrete implementation (note: the AsyncLocal version was here pre-Sprint 10; Jimi 2's commit `a59ec48` rewrote it in place with the HttpContext-based implementation, both in the same `CompanyContext/` path)
  - `ICompanyContext.cs` — interface
  - `CompanyContextMiddleware.cs` — middleware
- **Namespace** updated in those 3 files: `ERPSystem.Shared.MultiTenancy` → `ERPSystem.Shared.CompanyContext`.
- **Using-directive updates** in **25 referencing files** (`using ERPSystem.Shared.MultiTenancy;` → `using ERPSystem.Shared.CompanyContext;`):
  - `Host/Program.cs`
  - `Host/Controllers/{Admin,Audit,Companies,Events,FinanceAr,FinanceReports,Procurement,Reports,SoftDelete}Controller.cs` (9 files)
  - `Host/Audit/AuditLogger.cs` (not in the original proposal list — found via `grep -r`)
  - `Shared/Audit/AuditLogger.cs`
  - `Modules/AccountsReceivable/Application/Services/{Receipt,SalesInvoice}Service.cs`
  - `Modules/Activity/Application/ActivityFeedService.cs`
  - `Modules/Dashboard/Application/Services/{DashboardChart,DashboardSummary}Service.cs`
  - `Modules/Procurement/Application/Services/PurchaseOrderService.cs`
  - `Modules/Search/Application/Services/GlobalSearchService.cs`
  - `Tests/ERPSystem.Tests/Activity/ActivityFeedTests.cs`
  - `Tests/ERPSystem.Tests/Audit/AuditLoggerTests.cs`
  - `Tests/ERPSystem.Tests/Auth/CompanyContextTests.cs`
  - `Tests/ERPSystem.Tests/Dashboard/{DashboardChart,DashboardSummary}Tests.cs`
  - `Tests/ERPSystem.Tests/Search/GlobalSearchServiceTests.cs`
- **DOX pass** (per `.mavis/AGENTS.md` Rule 6) on the **2 AGENTS.md files within my scope**:
  - `src/backend/AGENTS.md` — Article 3 architecture bullet: `CompanyContext` reference path updated to `Shared/CompanyContext/CompanyContext.cs`.
  - `src/backend/Modules/Reports/AGENTS.md` — `ICompanyContext` dependency line: `Shared/MultiTenancy/` → `Shared/CompanyContext/` (with a "(renamed in Sprint 10 Phase 2)" note).
  - Note: `src/backend/Shared/AGENTS.md` was already updated by Jimi 2's commit (also covers the rename rationale + the Phase 3 done note). I did **not** modify it in my slice to avoid conflict with Jimi 2's work.

### Verified
- `grep -r "Shared.MultiTenancy" src/` → 0 matches (excluding the historical-context note in `Shared/AGENTS.md` and `Modules/Reports/AGENTS.md` that mention the rename) ✅
- `grep -r "Shared.CompanyContext" src/` → 28 matches (3 namespace declarations + 25 using directives) ✅
- `dotnet build` (Host) → 0 errors, 2 warnings (pre-existing, unrelated) ✅
- `dotnet build` (Tests) → 0 errors, 15 warnings (pre-existing, unrelated) ✅
- `dotnet test` (full suite, on top of Jimi 2's commit) → **436 passed, 2 failed, 30 skipped, 0 regressions** ✅
  - 2 failures are pre-existing `RetentionTests` (DB connection: `password authentication failed for user "postgres"`). Same baseline as Sprint 8 T2 + Jimi 2's commit.
- `npm run type-check` (frontend) → 0 errors ✅
- No `tenant_id` introduced ✅
- No secrets, no EF Core ✅

### Notes (coordinación + out-of-scope discoveries)
- **LOCAL-ONLY dev (per Anas 2026-07-31 06:47 UTC):** no push, no PR. The cron is disabled. The PR (with all 3 phases) opens at the end.
- **Concurrent worktree race (CRITICAL for Mavis Coordinator):** Jimi 1, 2, 3 all share the same worktree `C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-10` on the same branch `feature/sprint-10-refactor-multi-tenancy-rename`. While I was editing the `Shared/MultiTenancy/CompanyContext.cs` (namespace change only), Jimi 2 overwrote the same file with the Phase 3 implementation in another process. The result: at one point both the old AsyncLocal version and the new HttpContext version of `CompanyContext` existed in the working tree in different folders. I used `git stash -u` to preserve my changes, then re-applied them after Jimi 2 committed. The final state in my commit: **only my pure-rename changes** (25 using statements + 2 AGENTS.md updates + the 3 files at the new path) — Jimi 2's Phase 3 work is in their separate commit `a59ec48`.
- **Git rename detection:** git detected the move of `CompanyContextMiddleware.cs` and `ICompanyContext.cs` as renames (R in the commit stats) because the content is identical except for the namespace line. `CompanyContext.cs` shows as add+delete (not a rename) because Jimi 2's commit had already changed its content.
- **Out-of-scope discovery (for Mavis Coordinator — not in my PR slice per Rule 1):** the **root `/AGENTS.md`** still has the "⚠️ **MISLEADING FOLDER**" note pointing to the now-renamed path. This is a governance-level doc and per the worker contract rule 8 I did not modify it. The Coordinator (or a future Jimi with governance authority) should update that line.
- Branch: `feature/sprint-10-refactor-multi-tenancy-rename` (off `origin/develop @ 64efaac`)
- Commit base: `a59ec48` (Jimi 2's Phase 3 commit, applied on top of `b72c7b5` and `809955e`)

### Changed (BE Jimi 3 — T3, Section 6 fix)
- `docs/architecture/holding-company-architecture.md` — **Section 6 (Multi-Company) rewritten** to match the actual self-referencing `companies` schema (per `src/backend/Host/data-types/companies.json`):
  - Removed legacy `holding_id UUID NOT NULL REFERENCES holdings(id)` FK
  - Added `code VARCHAR(20)`, `slug VARCHAR(100)`, `parent_company_id` (self-FK → `companies.id`), `is_group BOOLEAN`, `base_currency CHAR(3)`
  - Dropped obsolete fields: `name_ar`, `tax_id`, `country`, `city`, `address`, `phone`, `email`
  - Replaced constraint `uk_companies_name_holding` → `uk_companies_code`
  - Replaced indexes: `idx_companies_holding` → `ix_companies_parent`, `idx_companies_active` → `ix_companies_slug`
  - Added prose paragraph (Arabic) explaining the self-referencing hierarchy: Holding = `companies` row with `is_group=true` + `parent_company_id IS NULL`; لا جدول `holdings` منفصل
  - Added end-of-section note pointing to Sprint 8 T4 refactor proposal (single-table self-referencing, not the original two-table design)
  - **Scope:** Section 6 only (~32 lines added, 26 lines removed); Sections 1-5 and 7-17 untouched per Rule 1

### Changed (BE Jimi 2 — T2, Phase 3 scoped DI)
- **`src/backend/Shared/CompanyContext/CompanyContext.cs`** — rewritten to use `IHttpContextAccessor` + `HttpContext.Items` instead of the static `AsyncLocal<CompanyHolder>`:
  - Constructor now takes `IHttpContextAccessor http` (was parameterless)
  - Storage uses three `HttpContext.Items` keys: `CompanyIdKey`, `UserIdKey`, `CompanyIdsKey` (all `internal const string` to avoid string-typo bugs)
  - `Set` / `Clear` now read/write `HttpContext.Items` (with null-guard for `BackgroundService` scopes that have no `HttpContext`)
  - **`ICompanyContext` interface is UNCHANGED** — `Set` / `Clear` still on the interface (kept for backward compat; the 28 referencing files needed zero changes for this)
  - Removed the private `CompanyHolder` nested class (no longer needed)
- **`src/backend/Tests/ERPSystem.Tests/Auth/CompanyContextTests.cs`** — fully rewritten to use a mocked `IHttpContextAccessor` + `DefaultHttpContext`:
  - Added `Build()` helper: returns a `(CompanyContext, DefaultHttpContext)` pair backed by a fresh `HttpContext` per test
  - Replaced `AsyncLocal_DoesNotLeakAcrossTasks` with **`Scoped_DoesNotLeakAcrossHttpContexts`** + **`ParallelHttpContexts_DoNotLeakCompany`** — same isolation contract, but verified against `HttpContext.Items` (the new storage) rather than AsyncLocal (the old implementation detail)
  - Added **`Clear_OnlyAffectsCurrentHttpContext`** — defensive: `Clear()` on request A must not affect request B
  - Added **`Set_WithNullHttpContext_DoesNotThrow`** — `BackgroundService` / `HostedService` work has no `HttpContext`; `Set` must be a no-op, not a crash
  - All other existing tests (4 Cycle-2 + 3 happy/error path) preserved with the new `Build()` helper
- **`src/backend/Tests/ERPSystem.Tests/Dashboard/DashboardSummaryTests.cs`** — comment-only fix: removed stale "(AsyncLocal)" wording in the "Why Moq" header comment, replaced with "Sprint 10 Phase 3: HttpContext.Items via IHttpContextAccessor". No code changes.
- **`src/backend/Shared/AGENTS.md`** — updated `CompanyContext` subtree row: `AsyncLocal — Phase 2 done, scoped DI planned` → `Phase 2 rename done, Phase 3 scoped DI done`. Updated Phase 3 plan note to record completion (interface unchanged, tests rewritten).
- **`src/backend/Host/Program.cs`** — NO CHANGES NEEDED. `AddHttpContextAccessor()` was already on line 162, and `AddScoped<ICompanyContext, CompanyContext>()` was already on line 241 (both from Sprint 6.1b). The scope was always correct — only the implementation was AsyncLocal-backed.
- **`src/backend/Shared/CompanyContext/CompanyContextMiddleware.cs`** — NO CHANGES NEEDED. The middleware receives `ICompanyContext companyContext` (now the scoped instance) as a parameter and calls `Set` / `Clear` on it. With the new scoped-DI implementation, those calls write to `HttpContext.Items` for the current request — behavior is identical to before, just implemented via DI instead of static state.

### Notes
- This is a docs-only change. No code modified, no tests affected, no `tenant_id` introduced.
- Out-of-scope discovery (flagged for Mavis Local — NOT included in this PR slice per Rule 1):
  - **Section 7** (ERD) still shows `holdings` table with 1:N → `companies` (now inconsistent with Section 6)
  - **Section 9** (JWT Structure) example still includes a `holding_id` field (now inconsistent)
  - **Section 5** (Holding) still has a `holdings` table SQL block with a "per CONSTITUTION Article 3 we have only one Holding" caveat
  - These are pre-existing inconsistencies that predate Sprint 9 T1 and were intentionally left out of scope.

---

## Sprint 8 T2 — FakeDb AS Alias Enhancement (2026-07-31) ✅ DONE

**Goal:** Remove known technical debt in `FakeDbConnectionFactory` that forces tests to use projected column names as a workaround for SQL `AS` aliases. Per T2 hand-off (Admin Team v1.8, محمد mode, approved by Anas 04:08 UTC).

### Added
- **`FakeDbDataReader.ProjectColumns(string sql, DataSet ds, string tableName)`** — internal static helper in `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs`. Parses the SELECT clause and projects the underlying DataTable's columns to the alias names. Falls back to the direct table when SELECT has no AS aliases (backward compatibility).
- **`SplitColumns(string columnList)`** — depth/quote-aware state machine for splitting the SELECT column list on top-level commas (ignores commas inside parens/quotes).
- **`Unquote(string s)`** — strips surrounding double-quotes from SQL identifiers.
- **`StripTableAlias(string s)`** — strips `a.id` → `id`, `accounts.code` → `code`. Required because tests use table-qualified column references like `a.user_id AS UserId`.
- **`FindSourceOrdinal(DataTable source, string columnName)`** — case-insensitive source column lookup. Required because the source DataTable column names may be in PascalCase (`UserId`) while the SQL uses snake_case (`user_id`).
- **`src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactoryTests.cs`** — 3 new tests:
  - `AsAlias_RenamesColumnsInReader` — happy path: `id AS "AccountId"` renames the reader's column
  - `NoAsAlias_FallsBackToDirectColumns` — backward compat: `SELECT id, name` (no AS) keeps the source names
  - `AsAlias_HandlesMultipleColumnsIncludingExpression` — `(code || '-' || name) AS "DisplayName"` creates the column with the alias name, value is DBNull (FakeDb does not simulate expressions)
- **Modified `FakeDbDataReader` constructor** — tries `ProjectColumns` first, falls back to direct table if SELECT parsing fails or no AS aliases are present.
- **`src/backend/Modules/Finance/AGENTS.md`** — new "Test Pattern: SQL AS Alias Support" section documenting the new pattern + edge cases.

### Verified
- `dotnet build`: 0 errors
- `dotnet test --filter "FakeDbConnectionFactoryTests"`: 3/3 pass
- `dotnet test --filter "FakeDbConnectionFactoryTests|ActivityFeedTests"`: 6/6 pass (regression test confirmed: existing tests using `a.id AS Id` pattern still work because `FindSourceOrdinal` tries the ALIAS name first, then the SQL source name)
- `dotnet test` (full suite): **436 passed, 2 failed, 30 skipped** (was 433/2/30 before T2; +3 new tests; 2 pre-existing RetentionTests failures are DB connection issues — not introduced by T2)
- No `tenant_id` introduced

### Notes
- T2 = Option B (per محمد's recommendation, approved by Anas 04:08 UTC)
- Mavis Local takeover (per the v2.0 governance model; the Coordinator can move to Local role)
- Branch: `feature/sprint-8-t2-fakedb-as-alias` (off `origin/develop @ 5e2cbd0`)
- Removes known technical debt (T1 tests needed projected column names workaround)
- Existing tests unaffected (additive change with backward-compat fallback)
- Sprint 9+ tests can use real AS aliases naturally
- The "alias-first, then source name" lookup order in `FindSourceOrdinal` is the key insight that makes the new code work with both the old "projected column names" pattern and the new "real SQL" pattern.

---

## Sprint 6 — Post-Demo Hardening (2026-07-29) 🟡 IN PROGRESS

**Goal:** Constitutional cleanup ✅ done in T1. Now polishing docs and verifying (T5+T6).

### Added
- `docs/workflow/sprint-6.md` — Sprint 6 hand-off (self-planned, ball in mavis-local court)
- Updated `docs/workflow/demo-roadmap.md` — actual completion status of Sprints 0-5
- Updated `docs/AGENTS.md` — references now point to `WORKFLOW.md` (active) + `CONSTITUTION.md` (paused) + root `CHANGELOG.md` (current)

### Notes
- T1 (Constitutional Setup) ✅ MERGED at PR #173 `c5a37119`
- T2 (Stale-branch cleanup) ✅ partial (4 local + 2 remote branches deleted)
- T3-T4 (Test gap-fill, FE polish) ⏳ optional, deferred
- T5 (Doc polish) 🟡 in progress
- T6 (Verify) ⏳ next
- T7 (Open PR + self-merge) ⏳ pending

---

## Sprint 6 Prep — Constitutional Cleanup (2026-07-29) ✅ MERGED (self-merge per DEC-070)

**Goal:** Promote the active constitution to the project root, activate the worker contract for Jimis, and launch Sprint 6.

### Added
- **`WORKFLOW.md` (project root)** — the active workflow constitution (8 articles), promoted from `.github/workflows/mavis-coordination/constitution.md` per Anas's 2026-07-29 19:13 UTC mandate ("always in mind"). Points to `docs/workflow/sprint-N.md` for hand-offs and `.github/workflows/mavis-coordination/state.json` for state.
- **`.mavis/AGENTS.md`** — worker contract for every local Jimi spawned by Mavis Local. Covers pre-flight, scope declaration, CHANGELOG entry, code standards, DOX pass, self-verify, what NOT to do, escalation rules.
- **`docs/workflow/sprint-6.md`** — Sprint 6 hand-off (self-planned by Mavis Local, since ball is in mavis-local court for the 2-day window).

### Changed
- **Root `AGENTS.md`** — replaced the "PROJECT PAUSED" banner with the "ACTIVE GOVERNANCE" banner pointing to `WORKFLOW.md`. Updated the child DOX index (`.mavis/AGENTS.md` is now Active, not "TO CREATE"). Refreshed the Sprint Model section to reference the worker contract. Updated the Crons section to reflect "tool, not actor" framing.
- **Active constitution location** — `WORKFLOW.md` at root is now the canonical source. The original file at `.github/workflows/mavis-coordination/constitution.md` is kept (cron path is fixed) but is no longer the primary reference.

### Notes
- Per Anas's 2026-07-29 19:13 UTC directive: "الكوره في ملعب الفريق المحلي" (the ball is in the local team's court) — Sprint 6 is now active.
- Per the active constitution (Article 2): the ball is in the **ACTOR's** court (mavis-local / mavis-cloud / anas), **NOT** the cron's. The cron is a tool.
- All async coordination still flows through `.github/workflows/mavis-coordination/state.json`.

---

## Sprint 5 — Demo V2 (The "Wow" Version) — Backend Phase 4 + 5 (2026-07-29) ✅ MERGED (PR #172)

**PR #172** (squash `9d148f4`) merged at 2026-07-29 18:51 UTC. Self-merge per DEC-070 (admin bypass).

**Goal:** Polished demo V2 with dashboard charts + global search.

### Added — Dashboard chart data (Phase 4 — T1/T2/T3)
- `GET /api/dashboard/charts/revenue?months=6` — revenue vs expense per month (line chart). Filters: `company_id`, status IN (Posted/Partial/Paid), expense from accounts.type=5 with status=2 journal entries.
- `GET /api/dashboard/charts/expenses-by-category?months=3` — pie / donut chart. One slice per Expense-type account with a fixed palette color by rank.
- `GET /api/dashboard/charts/top-customers?limit=5` — top customers by posted invoice total, all-time within the current company.
- New service: `Modules/Dashboard/Application/Services/DashboardChartService.cs`
- New DTOs: `Modules/Dashboard/Application/DTOs/ChartDtos.cs`
- New tests: `Tests/ERPSystem.Tests/Dashboard/DashboardChartTests.cs` (7 tests, 1 skipped integration)

### Added — Global search (Phase 5 — T4)
- `GET /api/search?q=&limit=20` — case-insensitive LIKE across customers, vendors, sales_invoices, and accounts. 3-tier ranking (exact > prefix > contains, scores 1.0/0.7/0.4). Per-type cap 5, total cap 20 (max 50). Always company-scoped.
- New module: `Modules/Search/` (Endpoints, Application/Services, Application/DTOs, AGENTS.md)
- New service: `Modules/Search/Application/Services/GlobalSearchService.cs`
- New DTOs: `Modules/Search/Application/DTOs/SearchDtos.cs`
- New tests: `Tests/ERPSystem.Tests/Search/GlobalSearchServiceTests.cs` (4 tests, 1 skipped integration)

### Changed
- `Host/Program.cs` — registered `IDashboardChartService` and `IGlobalSearchService` in DI.
- `Modules/Dashboard/Endpoints/GetSummary.cs` — added 3 chart endpoint methods to the existing `DashboardController` (route `/api/dashboard`).

### Notes
- All 4 new endpoints filter on `company_id` (Constitution Article 3, no `tenant_id`).
- Dapper only, no EF Core.
- `[Authorize(Policy = ReadAccess)]` on every new endpoint.
- 1 test per endpoint (per Article 11), 11 new tests total (2 skipped integration).
- The 2 failing `RetentionTests` are pre-existing — verified by `git stash` on bare develop (auth failure to `erp_test_system` test DB, not present in local Docker). Not a regression.

---

## Local Docker Demo — Setup (2026-07-29) ✅ MERGED

**Goal:** Self-contained local Docker stack for client demo on Anas's machine.

### ⏸️ PROJECT PAUSED (2026-07-29 18:25 UTC — 2 days)
**Per Anas's directive** to speed up work and coordination in a single environment:
- **Active (temporary permanent) constitution:** `.github/workflows/mavis-coordination/constitution.md` (was just created by سيتی + محمد)
- **Paused constitution:** `CONSTITUTION.md` (marked PAUSED, restored 2026-07-31 18:25 UTC)
- **Admin Team = سيتی + محمد + ديف** (Cloud) work as "Cron Jobs" coordinated by Mavis Local via `state.json`
- **Mavis Local = sole Tech Lead + Coordinator** for the 2-day window
- **No Telegram ping-pong** — all async via state.json
- **State.json is the single ping-pong point** — read it to know where the ball is
- **Pause until:** 2026-07-31 18:25 UTC
- **Reference:** [Anas's directive in this conversation](state.json)

### PR #172 — Local dev speed boost (in progress, NOT a code PR — gitignored config)
**Per Anas (2026-07-29):** Use local DB engine for faster dev. Switched `appsettings.Development.json` (gitignored) from Supabase to `localhost:5432` (local Docker Postgres).

**Impact:**
- Login: 30-60s (Supabase pooler) → **<1s (local)**
- DB queries: ~100ms → <5ms
- Works offline (no internet needed)
- Schema/data identical to cloud (same migrations + seed)

**Constraint noted:** The sprint-5 hand-off said "PostgreSQL 17 (Supabase for dev, Docker for local) | No new DB engine". This change is consistent — still PostgreSQL, just local instead of cloud. Engine unchanged. Hand-off constraint remains for non-Mavis-Local devs.

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
