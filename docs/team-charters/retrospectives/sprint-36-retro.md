# Sprint 36 Retrospective — Customer/Vendor Statements + Trial Balance FE

**Date:** 2026-08-05
**Branch:** `feature/sprint-36-statements-tb`
**Commit:** `33879f5` (after 3 commits in the chain)
**Sprint Goal:** كشف حساب العميل + كشف حساب المورّد + ميزان المراجعة (FE).
Per Anas: "خطط مسبقاً، نفّذ، تحقق، وثّق." (Sprint 35 directive)

---

## What Shipped

### BE (commit `004d93b`)
- `AccountsReceivable/Application/Services/CustomerStatementService.cs` (new) — opening + invoices + receipts + running balance
- `AccountsReceivable/Application/StatementDtos.cs` (new) — CustomerStatement + StatementLine + VendorStatement DTOs
- `Procurement/Application/Services/VendorStatementService.cs` (new) — opening + bills + payments + running balance
- `Procurement/Application/StatementDtos.cs` (new) — VendorStatement
- `Host/Controllers/FinanceArController.cs` — `GET /api/ar/customers/{id}/statement`
- `Host/Controllers/ProcurementController.cs` — `GET /api/procurement/vendors/{id}/statement`
- `Host/Program.cs` — DI registrations

### BE Bug Fixes (commit `d651977`)
- `VendorRepository.Sel` missing `company_id AS CompanyId` — caused every vendor to fail L19 check (CompanyId = Guid.Empty from Dapper)
- `CustomerStatementService` — removed `status` from receipts SELECT (table has no such column)
- `VendorStatementService` — removed `paid_amount` from vendor_bills SELECT (no such column), removed `status` from payments SELECT (it's INT, not string), fixed opening balance formula

### FE (commits `d651977` + `33879f5`)
- 3 new pages: `/finance/customers/[id]/statement`, `/procurement/vendors/[id]/statement`, `/finance/trial-balance`
- `lib/api.ts` — new types + 3 new API methods (`arApi.getCustomerStatement`, `procurementApi.getVendorStatement`, `financeApi.getTrialBalance`)
- `AppShell.tsx` — new "ميزان المراجعة" sidebar entry
- 1 detail page + 2 list pages: quick "كشف حساب" links

### Verification
- `dotnet build`: 0 errors, 17 warnings (17 pre-existing in ArabicYearScenarioDevSeeder)
- `npm run type-check`: 0 errors
- `npm run build`: 0 errors, 3 new routes appear
- BE smoke: customer statement 200 (opening=21,850, closing=21,850), vendor statement 200 (opening=33,600, closing=33,600), trial balance 200 (30 accounts, balanced: 833,005 = 833,005 ✓)
- Playwright smoke: 6/6 pass (TB balanced bar + 30 accounts, customer/vendor list links, customer/vendor statement summary cards)

---

## L-Lessons (L59..L60)

### L59 — ALWAYS run the actual endpoint with a real seed before declaring "BE done"
DEC-122 BE work was "done" after `dotnet build` + tests, but had 3 column-name bugs that only surfaced when hitting the live endpoint:
1. `VendorRepository.Sel` missing `company_id` (L19 violation) — every vendor returned "not found"
2. `receipts.status` — column doesn't exist
3. `vendor_bills.paid_amount` — column doesn't exist

**Rule:** After building BE SQL queries, run them against the actual DB. The Postgres error "column X does not exist" is the source of truth. Typecheck + tests don't catch this. Use `psql` or `Invoke-RestMethod` with a real seed before declaring done.

### L60 — Dapper EnumStringTypeHandler = string types in FE
The BE registers `SqlMapper.AddTypeHandler(new EnumStringTypeHandler<...>())` in Program.cs, which silently converts enum values to their string names on read. So the API returns `type: "Asset"`, not `type: 1`.

The TB page initially had `type: 1 | 2 | 3 | 4 | 5` and `typeOrder: [1, 2, 3, 4, 5]` — the filter `grouped.has(t)` always returned false at runtime, so the per-type tables didn't render (only the balanced bar showed).

**Rule:** When the BE uses EnumStringTypeHandler, the matching FE interface MUST use string literal unions, not int enums. Affects: `AccountType`, `NormalBalance`, `PaymentStatus`, `POStatus`, etc. When in doubt, `Invoke-RestMethod` the endpoint and check the actual JSON shape — the type is the source of truth.

---

## Misc Observations

### L34 (Sprint 34) audit missed `VendorRepository.L19`
The Sprint 34 audit (DEC-114..117) audited CostCenterService, PayrollService, ChartOfAccountsService, and AccountService. It did NOT audit `VendorRepository.GetByIdAsync` / `GetByCodeAsync` / `ListAsync` — which all had the same `Sel` missing `company_id` bug. The `GetByIdAsync` is the entry point for `VendorStatementService`, which is what triggered the discovery.

**Carry-over:** Sprint 37 should re-audit all 4 procurement/finance repos for L19 SELECT patterns:
- `VendorRepository.Sel` ✓ fixed
- `VendorBillRepository.SelVb` — same pattern, also missing `company_id`? need to check
- `CustomerRepository.Sel` — has it ✓ (confirmed during this sprint)
- Other `IRepository` SQL projections

### L45 (Sprint 33) reinforced — `npm start` serves cached `.next/`
After adding the new pages, the first Playwright run hit `Minified React error #423` (chunk loading failed). The fix: `npm run build` first, THEN `npm start`. The `dev` server would have picked up changes, but `start` reads from the static build output.

### Performance
- Statement SQL: single connection, 4 queries (1 for opening, 2 for period rows, 1 for combined lines)
- TB: single SELECT, sorted client-side
- No N+1, no batching issues at this volume (30 accounts, 6 lines per statement)

---

## Carry-over (Sprint 37+)

### P0 — L19 audit (carry-over from Sprint 34)
- `VendorBillRepository.SelVb` — verify it includes `company_id AS CompanyId` (or service-level filter is in place)
- Other `IRepository` SQL projections (L19 sweep across all repos)

### P1 — UI feedback from Anas (Sprint 32 + 33 carry-over)
- Receipt/invoice detail view (click row → expand with lines + paid/unpaid breakdown) — partially done in Sprint 33 (table renderExpanded)
- Overall UI polish per Anas: "تحسين لشكل ui للنظام ككل"

### P2 — Manual JEs (Sprint 34 carry-over)
- 4 of 12 templates shipped. 8 more if needed (Sprint 38+).

### P2 — VAT 5% (Sprint 35)
- Activate 5th VAT rule (DEC-118) — needs 1410/1411 accounts (already seeded in Sprint 35)
- 2 of 3 workflows fixed (DEC-111)

---

## Sprint Quality Metrics

| Metric | Value |
|---|---|
| Branch | `feature/sprint-36-statements-tb` |
| Commits | 3 (`004d93b`, `d651977`, `33879f5`) |
| Files | 11 (3 new pages + 4 modified C# + 2 modified FE + 2 new scripts) |
| Lines | +1009 / -18 |
| BE build | 0 errors |
| FE type-check | 0 errors |
| FE build | success |
| Playwright smoke | 6/6 |
| BE smoke (curl) | 3/3 endpoints work |
| Lessons learned | L59, L60 |
| Constitution compliance | Article 3 (L19 filter in services) ✓ |
| Telegram ping | msg_id=??? (TBD) |
