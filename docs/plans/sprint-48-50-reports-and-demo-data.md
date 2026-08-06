# Sprint 48-50: Financial Reports + Demo Data Scenario

**Per Anas's directive (2026-08-06 ~22:00 UTC+2):**
> "تشرف وتوجه الادمن عشان اتقومو بتنفيد Sprint 48 + Sprint 49 ، وبعدها قم بالاسبرينت 50 لكي تتحقق محاسبيا ومن صحه النتائج"

**Goal:** Add full financial reporting suite (Balance Sheet, P&L, Cash Flow, AP Aging) to V0 (port 3000), then seed realistic Libyan SME demo data so the client can browse and test.

---

## Sprint 48 — Reports Backend (BE) — ~2-3h

**Base branch:** `feature/sprint-41-p0-fixes` (so we get the DEC-127 CoA fix + default reference data seeder)

**Branch:** `feature/sprint-48-reports-backend`

### DEC-130: Balance Sheet service + endpoint
- File: `src/backend/Modules/Finance/Application/Services/GeneralLedgerReportService.cs`
- Add `GetBalanceSheetAsync(companyId, asOfDate, ct) → FinanceResult<BalanceSheetResponse>`
- SQL: group accounts by AccountType (1=Asset, 2=Liability, 3=Equity), sum balances where entry_date <= asOfDate
- Asset/Liability/Equity classification from AccountType enum
- Verify: TotalAssets == TotalLiabilities + TotalEquity (±0.01)
- Endpoint: `GET /api/finance/ledger/balance-sheet?asOf=2026-06-30`
- L19 fix: company_id filter on all JOINs (DEC-124 pattern)

### DEC-131: Income Statement (P&L) — new service + DTO + endpoint
- New file: `src/backend/Modules/Finance/Application/Services/IncomeStatementService.cs`
- New DTOs in `FinanceReportDtos.cs`:
  - `IncomeStatementResponse { From, To, Revenue, Expenses, NetIncome }`
  - `IncomeStatementSection { Title, Rows[], Subtotal }`
  - `IncomeStatementRow { AccountCode, AccountName, Amount }`
- SQL: 
  - Revenue = sum of credits − debits on AccountType=4 (Revenue) in period
  - Expenses = sum of debits − credits on AccountType=5 (Expense) in period
  - NetIncome = Revenue − Expenses
- Endpoint: `GET /api/finance/ledger/income-statement?from=2025-01-01&to=2026-06-30`

### DEC-132: Cash Flow (Indirect Method) — service + endpoint
- DTO already exists: `CashFlowResponse` in `FinanceReportDtos.cs`
- Add `GetCashFlowAsync(companyId, from, to, ct)` to `GeneralLedgerReportService`
- Method (Indirect):
  - **Operating** = NetIncome + non-cash items (depreciation) ± Δ in working capital (AR, AP, Inventory)
  - **Investing** = ± Cash from asset sales/purchases (fixed assets from AccountType=1 with subtype FixedAsset)
  - **Financing** = ± Cash from loans/equity (AccountType=2 for loans, AccountType=3 for equity changes)
  - NetChangeInCash = Operating + Investing + Financing
- Verify: Cash at end of period − Cash at start of period = NetChangeInCash
- Endpoint: `GET /api/finance/ledger/cash-flow?from=2025-01-01&to=2026-06-30`

### DEC-133: AP Aging — endpoint only (service + DTO exist)
- Add endpoint to existing controller (ProcurementController or new AgingController)
- `IAPAgingService` + `APAgingReportResponse` already exist in `Modules/Finance/Application/Services/APAgingService.cs`
- Endpoint: `GET /api/procurement/ap-aging?asOf=2026-06-30`

### Tests (xUnit)
- 4 tests verifying accounting equations:
  - `BalanceSheet_Balances`: ΣAssets == ΣLiab + ΣEq
  - `IncomeStatement_NetIncome`: Revenue − Expenses == NetIncome
  - `CashFlow_NetChangeInCash`: matches cash account delta
  - `APAging_OutstandingMath`: sum of buckets == sum of (bill.total − paid)

### Verification
- `dotnet build` 0 errors
- `dotnet test` all green
- Manual curl test of all 4 new endpoints

---

## Sprint 49 — Reports Frontend (FE) — ~2h

**Branch:** `feature/sprint-49-reports-frontend` (continues from Sprint 48)

### Group "التقارير المالية" in sidebar
- Edit `src/frontend/components/layout/app-shell.tsx`
- Add collapsible group under Finance
- Icon: `FileBarChart` from lucide-react

### 6 New pages
All under `src/frontend/app/(authenticated)/finance/reports/`:

1. **/finance/reports/trial-balance** (already exists at /finance/trial-balance, MOVE here + add date picker + export button)
2. **/finance/reports/general-ledger** (new — account picker + date range + running balance)
3. **/finance/reports/balance-sheet** (new — asOf date + sections Assets/Liab/Equity + ✅/❌ balanced badge)
4. **/finance/reports/income-statement** (new — from/to + Revenue/Expenses/NetIncome + comparison to prior period)
5. **/finance/reports/cash-flow** (new — from/to + 3 sections + NetChangeInCash card)
6. **/finance/reports/aging-summary** (new — AR + AP combined, with tabs for AR/AP)

### Shared components
- `src/frontend/components/reports/ReportHeader.tsx` — title + date picker(s) + export button
- `src/frontend/components/reports/ReportTable.tsx` — generic table with sticky header
- `src/frontend/components/reports/SectionCard.tsx` — wraps section title + subtotal
- `src/frontend/components/reports/BalanceBadge.tsx` — ✅ متوازن / ❌ غير متوازن (X.XX)

### Arabic design (Sprint 39 design tokens)
- RTL layout
- Tajawal font
- Soft shadows
- Indigo primary, emerald for "balanced", rose for "unbalanced"

### Verification
- `npm run typecheck` 0 errors
- `npm run build` success
- Playwright smoke: visit all 6 pages, verify 200 + correct data

---

## Sprint 50 — Verification + Demo Data — ~3-4h

**Branch:** `feature/sprint-50-reports-verification-data` (continues from Sprint 49)

### 50.1 — Accounting equation verification (automated tests)
- 6 xUnit tests in `src/backend/Tests/Finance/Reports/`:
  1. `BalanceSheet_BalancesEquation` — on seeded data
  2. `IncomeStatement_NetIncomeCorrect` — ΣRevenue − ΣExpenses
  3. `CashFlow_NetChangeMatchesCash` — Cash delta == sum of 3 sections
  4. `GeneralLedger_OpeningPlusMovementsEqualsClosing` — per account
  5. `TrialBalance_DebitsEqualCredits` — Σdebit == Σcredit
  6. `APAging_NoNegativeOutstanding` — bill total >= sum of payments

### 50.2 — Data cleanup
- Drop all existing journal entries, journal_lines, AR/AP, POs, GRs, Bills, Receipts
- Keep: companies, accounts (CoA), customers, vendors, items, employees, projects
- Goal: clean slate for the new scenario

### 50.3 — Unified CoA (per company)
- Reuse the existing seeder pattern: `DefaultHoldingBootstrapHostedService` already seeds 1 warehouse + 1 cost center
- For CoA: 1 unified CoA for all companies (holding + subsidiaries), 60-80 accounts covering:
  - **1000-1999 الأصول (Assets)** — Current Assets (Cash, Bank, AR, Inventory, Prepaid) + Fixed Assets (Equipment, Vehicles, Buildings, Accumulated Depreciation)
  - **2000-2999 الالتزامات (Liabilities)** — Current (AP, Accrued Expenses, VAT Payable, Short-term Loans) + Long-term (Long-term Loans)
  - **3000-3999 حقوق الملكية (Equity)** — Capital, Retained Earnings, Drawings, Current Year P&L
  - **4000-4999 الإيرادات (Revenue)** — Sales, Service Revenue, Other Income, Sales Returns
  - **5000-5999 المصروفات (Expenses)** — COGS, Salaries, Rent, Utilities, Depreciation, Marketing, Admin, Finance Costs
- Each account: code, name_ar, name_en, account_type, normal_balance, parent (for hierarchy)
- **Critical:** Verify chart-of-accounts account types match the Balance Sheet / P&L section logic

### 50.4 — Journal entries scenario (per Anas's directive)
- **Holding company:** ≤ 500 journal entries
- **Each subsidiary:** ≤ 200 journal entries
- **Period:** 2025-01-01 to 2026-06-30 (18 months)
- **Subsidiaries count:** 2-3 (typical Libyan holding structure)
- **Total target:** ~700-1100 journal entries, ~7000-11000 lines

### 50.5 — Scenario pattern (per month per company)
Each month generates:
- **Month start:**
  - Owner equity injection (DR Cash / CR Capital) — Q1 only
  - Customer invoices (DR AR / CR Sales + VAT)
- **During month:**
  - Customer receipts (DR Cash / CR AR) — 70% collection
  - Vendor bills (DR Inventory+Expense / CR AP + VAT)
  - Vendor payments (DR AP / CR Cash) — 60% payment
  - Salaries (DR Salaries Expense / CR Cash/Bank + accruals)
  - Rent, utilities, other expenses (DR Expense / CR Cash)
  - Bank loan installments (DR Loan / CR Cash)
- **Month end:**
  - Depreciation (DR Depreciation Expense / CR Accumulated Depreciation)
  - Inventory adjustment (if any)
  - Accruals (DR Expense / CR Accrued Liability)
  - VAT settlement (DR VAT Output / CR VAT Input / CR Cash to tax authority)

### 50.6 — Implementation
- New file: `src/backend/Shared/SeedData/LibyanSmeScenarioDevSeeder.cs` (IHostedService)
- New file: `src/backend/Shared/SeedData/scenario-2025-2026.json` (entries to seed — generated, then committed)
- Double-gate: enabled via `appsettings.Development.json` flag
- Idempotent: check existing entry_number before insert
- Verify after: Trial Balance debits == credits, Balance Sheet balanced, P&L NetIncome matches

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| CoA doesn't match report section logic | Verify in Sprint 50.1 BEFORE seeding data |
| Data generation produces unbalanced entries | Use the existing `PostingRulesService` to auto-post, not manual SQL |
| Number of journal entries too slow | Batch insert via unnest() (per DEC-041 pattern) |
| P&L NetIncome doesn't match Retained Earnings movement | Test explicitly in 50.1.6 |

---

## Communication
- Telegram ping after each sprint
- AGENTS.md + CHANGELOG.md updates
- Per-sprint retrospective
