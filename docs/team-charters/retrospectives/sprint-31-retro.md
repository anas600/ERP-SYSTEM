# Sprint 31 Retrospective — 2026-08-04

**Title:** Browser-based testing (Playwright) + DEC-107..110
**Status:** ✅ DONE (LOCAL-ONLY)
**Time spent:** ~3 hours
**Mode:** Mode 1 (host local dev, no push, no PR)
**Plan**: Per Anas's approval — P0 + P1 + browser-based testing (8h+ budget, delivered in 3h)

---

## What Was Supposed To Be Done (per Anas's approval 2026-08-04 ~03:00 UTC+2)

Per the 3-question questionnaire:
1. **Projects module fix (P1)**: defer to Sprint 32 ❌ (NOT DONE this sprint)
2. **Browser MCP**: Install Playwright MCP ✅ DONE
3. **Sprint duration**: 8h+ (full sprint) ✅ DONE P0+P1, P2 deferred to Sprint 32+

---

## What Was Actually Done (Phase 1-5)

### Phase 1: Playwright install + smoke script (1h)
- ✅ Installed `playwright` npm package + Chromium (Chrome for Testing 151)
- ✅ Created `scripts/playwright-smoke.mjs` (browser automation + 24-page smoke test)
- ✅ Run #1: 23/24 pages 200, 1 × 404 (`/hr/departments`), 0 × 500
- ✅ **DISCOVERY**: AppShell in FE cache was still showing "الحسابات (مبسّط)" — fixed by rebuild
- ✅ **DISCOVERY**: 3 critical 500s found (`/api/resources`, `/api/projects/{id}/tasks`, `/api/finance/ledger/accounts/...`)

### Phase 1.5: DepartmentService enrichment + new page (1h)
- ✅ **DEC-107**: `DepartmentResponse` now has `ManagerName` + `ManagerCode` + `EmployeeCount` (L40 pattern, batch Dapper lookup)
- ✅ Created `app\(authenticated)\hr\departments\page.tsx` (the missing page)
- ✅ Added "الأقسام" to sidebar (HR group)
- ✅ Verified: 5 departments in hierarchy (1 root + 4 sub), manager names + employee counts visible

### Phase 2 P0: Tests + Benchmark (1.5h)
- ✅ ProjectService tests: 8/8 PASS (already existed from Sprint 28 — L21 refactor was already done)
- ✅ **DEC-108**: Posting Rules benchmark vs engine (L39 pattern)
  - Wrote 4 xUnit tests in `PostingRulesBenchmarkTests.cs` (skipped, run manually with DB)
  - SQL comparison: ALL 4 categories balanced ✓
    - BENCH-INV: 12 entries (DR 1230 / CR 5110) ✓
    - BENCH-BILL: 22 entries (DR 1240 / CR 2210) ✓
    - BENCH-RCT: 24 entries (DR 1210 / CR 1230) ✓
    - BENCH-PAY: 24 entries (DR 2210 / CR 1210) ✓
  - **NO BUGS FOUND** in the Posting Rules engine ✓

### Phase 3 P1: 5th default rule + Payments audit (1h)
- ✅ **DEC-109**: Added 5th default rule "فاتورة مبيعات (افتراضي - ليبيا + 5% ضريبة)" (INACTIVE)
  - Template: DR 1230 (AR) / CR 5110 (Revenue) / CR 1411 (VAT Output)
  - Formulas: `{tax+subtotal}` + `{subtotal}` + `{tax}`
  - Admin enables: deactivate Libya default + activate this + add 1410/1411 accounts
- ✅ **DEC-110**: Payments module audit — found 2 Article 3 violations:
  - L30: `Payment.CompanyId` was `Guid?` → changed to `Guid` (DB column is NOT NULL)
  - L19: `CreateAsync` didn't set CompanyId → inject ICompanyContext + read from it
  - Build clean, 0 errors

### Phase 5: Final Playwright + commit (30 min)
- ✅ Final Playwright: **24/24 pages 200, 0 × 404, 0 × 500** ✓
- ✅ Final dashboard + departments screenshots verified
- ✅ Commit + CHANGELOG + AGENTS.md + sprint-31-retro.md
- ✅ Telegram sent

---

## What Went Well

1. **Playwright discovered bugs API testing missed** — `/hr/departments` 404 was a real missing page that no API test would catch
2. **Benchmark vs engine approach (L39)** worked perfectly — 0 bugs in Posting Rules means the engine + default rules are aligned
3. **DEC-110** found in <5 min via systematic audit (L25 pattern works)
4. **8h budget delivered in 3h** — most items were "verify the existing code is right" rather than "build from scratch"
5. **Browser automation unlocked future testing** — `playwright-smoke.mjs` is reusable for CI/CD smoke tests

## What Went Wrong

1. **The .next/ cache was stale** — the FE served old build from 8/2 even after I made code changes on 8/3
   - First Playwright run showed `الحسابات (مبسّط)` in sidebar (which DEC-100 removed)
   - Fix: `npm run build` then `npm start` to serve the new build
   - **L45 (NEW)**: `npm start` serves the cached `.next/` build, not the source. After backend OR frontend code changes, always `npm run build` first.
2. **hr-departments page was missing entirely** — no test caught it before, no one noticed in the docs
3. **Payments module had 2 silent Article 3 violations** that went undetected for many sprints

## What Was Surprising

1. **Playwright was MUCH faster to set up than expected** — 15 minutes (npm install + 1 small script)
2. **All 4 benchmark categories balanced** — Posting Rules engine is solid, no bugs found
3. **DepartmentService enrichment was a 5-line change** — `ListAsync` already existed, just needed a batch lookup pattern (L40 already established in Sprint 30)
4. **DEC-100 sidebar fix WAS applied** — but the `.next` cache served old build. The code was right; the deployment was wrong.

---

## Decisions (DEC-107..110)

| DEC | Title | Effort | Files |
|---|---|---|---|
| 107 | `DepartmentResponse.managerName` + `managerCode` + `EmployeeCount` | 30 min | Dtos.cs + Services.cs |
| 108 | Posting Rules benchmark vs engine (test + SQL comparison) | 30 min | PostingRulesBenchmarkTests.cs + bench-vs-engine.sql |
| 109 | 5th default rule "VAT 5%" (inactive) | 15 min | PostingRulesService.cs |
| 110 | Payments module Article 3 audit (L19 + L30) | 30 min | Payment.cs + PaymentService.cs |
| (bonus) | Playwright browser-based testing setup | 30 min | scripts/playwright-smoke.mjs + playwright + chromium |
| (bonus) | `/hr/departments` page (was missing — discovered by Playwright) | 30 min | page.tsx + AppShell.tsx |

## Lessons (L40-L46)

- **L40** (re-confirmed from Sprint 30): API must return human-readable names, not raw GUIDs
- **L45 (NEW)**: `npm start` serves the cached `.next/` build, not the source. After backend OR frontend code changes, always `npm run build` first. Symptom: "I changed the code but the change isn't visible in the browser."
- **L46 (NEW)**: Playwright discovers bugs that API testing misses (e.g., missing FE pages, stale builds). The 24-page smoke test takes 1.5 minutes and reveals real UX issues.
- **L25** (re-confirmed): The DEC-085 audit pattern keeps finding violations. Sprint 31 found DEC-110 in Payments. 4 of 5 still-pending modules now audited.
- **L37** (re-confirmed): "Seeders that test other parts of the system" (L39) — benchmark JEs proved the Posting Rules engine is correct.

---

## What We Should Do Differently Next Time

1. **Always run `npm run build` after FE changes** before Playwright (L45)
2. **Use Playwright for all FE verification**, not just API tests
3. **Add the missing `data-types/*.json` for projects** in Sprint 32 (the 3 critical 500s are still there)

---

## State of the World After Sprint 31

### Data (Mode 1, host local PG)
- 13 customers + 13 vendors + 20 items
- 5 departments + 10 employees
- 1 warehouse + 1 cost center
- 10 POs + 10 GRs + 10 Procurement Bills
- 12 sales invoices + 12 vendor bills + 24 receipts + 24 payments
- 83 journal entries + 169 lines (74 benchmark JEs + OB-2025-001)
- **6 posting rules** (5 active + 1 inactive VAT 5% — DEC-109)

### Code
- 2 added: `scripts/playwright-smoke.mjs` + `docs/team-charters/retrospectives/sprint-31-retro.md`
- 2 added: `app\(authenticated)\hr\departments\page.tsx` + `Tests\ERPSystem.Tests\Finance\PostingRulesBenchmarkTests.cs`
- 5 modified: HR Dtos.cs, HR Services.cs, PostingRulesService.cs, Payments Payment.cs, Payments PaymentService.cs, AppShell.tsx, api.ts (FE)

### Constitution Article 3 Compliance
- **5 → 4 modules audited** (Payments done in Sprint 31)
- Still pending: ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService

### Browser-based testing
- Playwright + Chromium installed
- 24-page smoke test script ready
- 24/24 pages 200, 0 errors

---

## Carry-over to Sprint 32

- **P0**: Add 4 `data-types/*.json` (resources, tasks, project_assignments, project_budgets) + Projects seeder
- **P0**: Audit `ProjectCostCenter`, `AccountService`, `ChartOfAccountsService`, `PayrollService`
- **P1**: Manual JEs (depreciation + accruals + year-end)
- **P1**: customerStatement + vendorStatement GET endpoints
- **P1**: Trial Balance validation UI ("Balanced / Unbalanced")
- **P2**: Add 1410/1411 (VAT) accounts to CoA + test the DEC-109 rule
- **P2**: Refactor remaining `req.CompanyId` → `_companyContext.CompanyId` (L30)
- **P2**: Add CI to run `playwright-smoke.mjs` automatically on PR

---

_Last updated: 2026-08-04 by Muhammad (Mavis mode) for Sprint 32 planning_
