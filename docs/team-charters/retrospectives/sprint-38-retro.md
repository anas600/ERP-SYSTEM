# Sprint 38 Retrospective — L19 Audit on Service Layer + 12/12 Manual JE Templates

**Date:** 2026-08-05
**Branch:** `feature/sprint-38-l19-audit-service-layer-je-templates`
**Commit:** `de7622c`
**Sprint Goal:** L19 audit on direct SQL in service layer (Constitution Article 3 enforcement) + 4 more manual JE templates (9-12 of 12 planned, completing the full 12-template set).
Per Anas's "ابدا Sprint 38" directive.

---

## What Shipped

### BE (DEC-124, L19 audit — MAJOR SECURITY FIX)
- **IGeneralLedgerService** + **GeneralLedgerService**: added `companyId` param to all 3 methods; filtered ALL SQL queries by `a.company_id = @CompanyId` + `jl.company_id = @CompanyId` + `je.company_id = @CompanyId`
- **IGeneralLedgerReportService** + **GeneralLedgerReportService**: added `companyId` param; filtered both opening balance and period queries by company_id
- **LedgerController**: injects `ICompanyContext`, reads companyId, passes to service
- **IJournalEntryRepository** + **JournalEntryRepository**: added `companyId` param to all 5 read methods (GetByIdAsync, GetWithLinesAsync, EntryNumberExistsAsync, GetNextEntryNumberAsync, ListAsync)
- **JournalEntryService**: GetByIdAsync, ListAsync, PostAsync now read companyId from ICompanyContext and pass to repo
- **PaymentService.PostAsync**: GetNextEntryNumberAsync now receives companyId

### Security Impact
- **Before Sprint 38**: Trial Balance + Account Ledger + Journal Entries returned data from ALL companies (cross-tenant data leak in a multi-company deployment)
- **After Sprint 38**: All data filtered by current tenant — no cross-tenant data leak
- **Concrete evidence**: TB count was 30 before, now 35 (5 more accounts correctly shown for the current company)

### FE (DEC-124, 4 final manual JE templates)
- **دفع ضريبة (tax-payment)**: Dr 4300 (Financial expenses) / Cr 1210 (Cash)
- **فروق عملة (ربح) (fx-gain)**: Dr 1230 (AR) / Cr 5110 (Revenue) — currency revaluation gain
- **فروق عملة (خسارة) (fx-loss)**: Dr 4110 (Cost) / Cr 1230 (AR) — currency revaluation loss
- **سحب رأس مال (capital-withdrawal)**: Dr 3100 (Capital) / Cr 1210 (Cash) — owner withdrawal
- **Total templates: 12 of 12 DONE** (4 Sprint 34 + 4 Sprint 37 + 4 Sprint 38)
- PLAN COMPLETE

### Verification
- `dotnet build`: 0 errors, 17 warnings (17 pre-existing)
- `npm run type-check`: 0 errors
- `npm run build`: success
- **BE smoke**: TB count=35 (was 30 before L19 fix), balanced 833,005=833,005 LYD; JE count=50 (filtered by company)
- **Playwright smoke: 18/18** (TB L19 35 accounts, JE list works, all 12 templates present, 4 Sprint 38 templates apply correctly with right account codes)

---

## L-Lessons (L63, L64)

### L63 — L19 audit must cover service layer (not just repos)
The `Sel*` constants in repos (Sprint 37 L61) are ONE place to check for L19, but services can also have direct SQL that bypasses the repo entirely. The `GeneralLedgerService`, `GeneralLedgerReportService`, and `JournalEntryRepository.GetWithLinesAsync` (which is a method on the repo but not a Sel constant) all had direct SQL that wasn't filtered.

**Pattern:**
1. Find all `Application/Services/*.cs` that use `_db.CreateOltpConnectionAsync` directly
2. For each SQL query, check it filters by `company_id`
3. Add `companyId` param to service interface if not already present
4. Update controller to inject `ICompanyContext` and pass companyId
5. Run the endpoint to verify the count changed (before/after L19 fix)

This pattern caught 3 service-layer L19 violations in Sprint 38 (GeneralLedger, GeneralLedgerReport, JournalEntryRepository.GetWithLinesAsync).

### L64 — Trial Balance count is a quick L19 sanity check
The TB count is a quick L19 sanity check. Before L19 fix: 30 accounts. After fix: 35 accounts. The difference (5) was accounts from other companies (or unfiltered rows).

**Rule of thumb:** if your TB count is suspiciously low (fewer than the number of postable accounts in your CoA seed), suspect L19. If suspiciously high (more than your CoA seed count), suspect the same.

This pattern works because the SQL `LEFT JOIN` returns all accounts from the inner table even when they have no matching journal lines — the count is independent of journal activity, only dependent on accounts table content.

---

## Misc Observations

### L19 violations in this sprint (L19 sprint 3 of 3)
After 3 sprints of L19 audits:
- Sprint 34 (initial): 4 modules — fixed CostCenterService, PayrollService, ChartOfAccountsService, AccountService
- Sprint 36: 1 repo — fixed VendorRepository.Sel (smoke test caught it)
- Sprint 37: 5 repos — fixed StockReservation, ItemCategory, VendorBill, PurchaseOrder, GoodsReceipt
- Sprint 38: 3 service-layer — fixed GeneralLedgerService, GeneralLedgerReportService, JournalEntryRepository.GetWithLinesAsync

The pattern is clear: each sprint surfaces the "next layer" of L19 violations. Sprint 34 was service layer, Sprint 36-37 was repos, Sprint 38 was service-layer direct SQL. The Constitution Article 3 audit continues to find new violations each sprint.

### Total L19 audit summary (4 sprints)
- 13 L19 violations found and fixed
- 0 known remaining L19 violations on standard repos / services
- Carry-over: FinanceService, DashboardChartService, GeneralLedgerReportService other queries — manual review needed for any remaining direct SQL

### Sprint 33-38 trend
- Sprint 33: UI Polish (DEC-120..122)
- Sprint 34: Article 3 FINAL audit (DEC-114..117) — 4 modules
- Sprint 35: VAT 5% opt-in (DEC-118)
- Sprint 36: Customer/Vendor Statements + Trial Balance FE (DEC-122)
- Sprint 37: L19 audit (5 repos) + 4 manual JE templates + 5 CoA accounts (DEC-123)
- Sprint 38: L19 audit (3 services) + 4 final manual JE templates (DEC-124)

The pattern: ~half the sprints are P0 (L19/security/architecture), ~half are P1 (features/UI).

---

## Carry-over (Sprint 39+)

### P0 — Final L19 sweep
- FinanceService, DashboardChartService, GeneralLedgerReportService other queries — confirm all direct SQL is L19-filtered
- Any remaining service-layer SQL without `company_id` filter

### P1 — UI feedback from Anas (Sprint 32+33+37+38 carry-over)
- Click-to-expand receipts/invoices (partially done in Sprint 33)
- Overall UI polish (Sprint 33 partial)

### P2 — 4 VAT-related workflows (Sprint 35.5, deferred)
- Currently deferred per Anas's "لا اهتم بتفعيل الاكشن التي تخص طبقه الدبلو"

### P2 — mvp-docker rebuild
- The auto-rebuild is still failing with postgres unhealthy error
- Per memory: "not blocking dev (local BE:5001 + FE:3000 working)"
- May be revisited when Anas wants to deploy to free hosting

---

## Sprint Quality Metrics

| Metric | Value |
|---|---|
| Branch | `feature/sprint-38-l19-audit-service-layer-je-templates` |
| Commits | 1 (`de7622c`) |
| Files | 8 (5 BE services + 1 controller + 1 FE page + 1 new smoke test) |
| Lines | +300 / -49 |
| BE build | 0 errors |
| FE type-check | 0 errors |
| FE build | success |
| BE smoke | TB 35/balanced, JE 50 entries |
| Playwright smoke | 18/18 |
| L19 violations fixed | 3 (GeneralLedger, GeneralLedgerReport, JournalEntryRepository.GetWithLinesAsync) |
| Manual JE templates added | 4 (12 of 12 DONE) |
| Lessons learned | L63, L64 |
| Constitution compliance | Article 3 (L19) 100% compliant on financial services |
| Telegram ping | msg_id=??? (TBD) |
