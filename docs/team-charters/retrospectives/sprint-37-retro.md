# Sprint 37 Retrospective — L19 audit + 4 Manual JE Templates

**Date:** 2026-08-05
**Branch:** `feature/sprint-37-l19-audit-je-templates`
**Commit:** `3b7c82f`
**Sprint Goal:** L19 audit sweep across remaining repos (Sprint 34 audit missed several) + 4 more manual JE templates (Sprint 34 shipped 4, this sprint ships 4 more, total 8 of 12 planned).
Per Anas's "Sprint 37" directive (auto-continue per "نتقدم في تنفيد الاسبرينت التالي ادا لم يكن هناك ملاحظات").

---

## What Shipped

### BE (DEC-123, L19 audit)
- 5 repos fixed for `company_id AS CompanyId` in their `Sel` / `SelVb` / `SelPo` / `SelGr` constants:
  - `StockReservationRepository.Sel` (Inventory)
  - `ItemCategoryRepository.Sel` (Inventory)
  - `VendorBillRepository.SelVb` (Procurement)
  - `PurchaseOrderRepository.SelPo` (Procurement)
  - `GoodsReceiptRepository.SelGr` (Procurement)
- All other repos verified clean (have `company_id` or don't need it — line entities, Company itself)

### BE (DEC-123, CoA seed)
- 5 new accounts added to `DefaultCoASeed.cs`:
  - `1300` مجمع إهلاك الأصول الثابتة (Asset, parent 1100)
  - `1410` سلف الموظفين (Asset, parent 1200)
  - `2110` مصروفات مستحقة (Liability, parent 2200)
  - `5410` ديون معدومة (Expense, parent 4200)
  - `5500` إهلاك الأصول الثابتة (Expense, parent 4200)
- Total CoA: 47 → 52 accounts
- Topological order preserved (parent before child in array)

### FE (DEC-123, 4 new manual JE templates)
- **رواتب (salary)**: Dr 4112 (Direct Labor) / Cr 1210 (Cash)
- **سلفة موظف (loan)**: Dr 1410 (Loans Receivable) / Cr 1210 (Cash)
- **ديون معدومة (bad-debt)**: Dr 5410 (Bad Debt Expense) / Cr 1230 (AR)
- **تسوية مخزون (inventory-adjust)**: Dr/Cr 1240 (Inventory) for variance
- Templates dropdown in `/finance/journal-entries/new` (now 8 total: 4 Sprint 34 + 4 Sprint 37)
- "تطبيق القالب" button pre-fills description + reference + account IDs

### FE (pre-existing bug fix)
- `journal-entries/new/page.tsx` was using raw `fetch('/api/finance/accounts')` WITHOUT JWT → 401 silently
- Users couldn't load accounts in the JE form (this was a Sprint 11/12 bug never caught)
- Now uses `financeApi.listAccounts()` which attaches the auth token
- Smoke test caught this — L59 lesson (run actual endpoint with real seed)

### Verification
- `dotnet build`: 0 errors, 17 warnings (17 pre-existing)
- `npm run type-check`: 0 errors
- `npm run build`: success
- Playwright smoke: 14/14 (CoA 52 accounts, all 8 templates in dropdown, 4 new templates apply correctly with right account codes)
- BE smoke: 5 new accounts present in /api/finance/accounts (1300, 1410, 2110, 5410, 5500)

---

## L-Lessons (L61, L62)

### L61 — L19 audit focus on `Sel` / `SelVb` / `SelX` constants
When auditing L19 across all repos, focus on the `Sel` / `SelVb` / `SelX` constants in the repository classes. Each one is a string constant used in multiple queries (GetByIdAsync, ListAsync, GetByCodeAsync, etc.) — fixing once in the constant fixes everywhere.

**Audit pattern:**
1. `grep -rn "private const string Sel" src/backend/Modules/`
2. For each `Sel*` constant, check if it includes `company_id AS CompanyId`
3. For the entity that IS the tenant table (Company), company_id is not needed
4. For line entities (no company_id column on the table), the parent's Sel must include company_id

This pattern caught 5 more bugs in Sprint 37 (1 Inventory, 1 Item Categories, 3 Procurement). The L19 violations are now likely 100% cleared across all standard repos.

### L62 — Check CoA first before adding JE templates
When adding account codes for a new JE template, check the existing CoA first. If a needed account doesn't exist (e.g., 1300 accumulated depreciation, 5410 bad debt), add it to `DefaultCoASeed.cs` in the correct topological order. **Don't add templates that require missing accounts** — the user picks an account from the dropdown and the right code might not be there.

**Pattern:** Before writing a template:
1. List the account codes you'll need
2. `grep` for each in `DefaultCoASeed.cs`
3. For missing ones, add them with the right parent + correct topological position (parent code before child in array)

Sprint 34 retro mentioned "8 more manual JE templates (Sprint 34 shipped 4 of 12)" but I couldn't ship 4 more in Sprint 37 without the 5 missing CoA accounts. So I added both in Sprint 37.

---

## Misc Observations

### L59 reinforced (smoke test catches pre-existing bug)
The Playwright smoke test caught a bug in `journal-entries/new/page.tsx` that's been there since Sprint 11/12: raw `fetch()` without JWT. The accounts dropdown was always empty for this page. **The smoke test on the new feature revealed a pre-existing bug** — exactly L59 in action.

### CoA topological order (L56 reinforced)
Adding 5 new accounts to `DefaultCoASeed.cs` had to follow the 2-pass topological sort: parent before child in array. L56 (Sprint 35) reinforced this for VAT 5%. The 5 new accounts were placed:
- `1300` after `1103` (parent 1100 already there)
- `1410` after `1250` (parent 1200 already there)
- `2110` after `2230` (parent 2200 already there)
- `5410` after `4200` (parent 4200 is the row right before)
- `5500` after `5410` (same parent 4200)

### L30 reinforcement (no `req.CompanyId` in DTOs)
All 5 L19 fixes were at the repository level (the SQL `SELECT` clause). The services that use these repos (PurchaseOrderService, VendorBillService, etc.) were already correctly using `_companyContext.CompanyId` (per Sprint 28-30 audits). So the L19 violation was only at the SQL projection level, not the filter level — the Dapper would map `CompanyId=Guid.Empty` for the entity, but the service `if (entity.CompanyId != companyId) return Fail(...)` check would reject the entity as "not found". Same symptom as Sprint 36 (VendorRepository).

### Sprint 34 templates integration
The 4 Sprint 34 templates (manual, depreciation, accrual, prepaid) live on `feature/sprint-34-audit-manual-jes` which hasn't been merged to develop yet. I duplicated them in Sprint 37 to have a complete working set on this branch. When all 4 branches merge, the Sprint 34 templates will already be there (no conflict, additive).

---

## Carry-over (Sprint 38+)

### P0 — L19 audit verification on remaining repos
- `IRepository` interfaces that don't have `Sel` constants but use direct SQL (e.g., manual SQL in service layer for things like aging-ar, account ledger)
- Manual SQL queries in service layer (e.g., JournalEntryService direct SQL)

### P1 — UI feedback from Anas (Sprint 32 + 33 + 37 carry-over)
- Receipt/invoice click-to-expand (partially done in Sprint 33)
- Overall UI polish

### P2 — 4 more manual JE templates
- 8 of 12 done. Remaining 4: Tax payment, Bank reconciliation, Year-end closing, Foreign currency revaluation

### P2 — 4 VAT-related workflows (Sprint 35.5)
- Currently deferred per Anas's "لا اهتم بتفعيل الاكشن التي تخص طبقه الدبلو"
- Can be done in Sprint 38+ if requested

---

## Sprint Quality Metrics

| Metric | Value |
|---|---|
| Branch | `feature/sprint-37-l19-audit-je-templates` |
| Commits | 1 (`3b7c82f`) |
| Files | 8 (5 BE repos + 1 CoA seed + 1 FE JE page + 1 new smoke test) |
| Lines | +340 / -10 |
| BE build | 0 errors |
| FE type-check | 0 errors |
| FE build | success |
| Playwright smoke | 14/14 |
| BE smoke | 5 new accounts present |
| L19 repos fixed | 5 (StockReservation, ItemCategory, VendorBill, PurchaseOrder, GoodsReceipt) |
| CoA accounts added | 5 (1300, 1410, 2110, 5410, 5500) |
| Manual JE templates | 4 new (Salary, Loan, Bad Debt, Inventory) |
| Lessons learned | L61, L62 |
| Constitution compliance | Article 3 (L19) ✓ |
| Telegram ping | msg_id=??? (TBD) |
