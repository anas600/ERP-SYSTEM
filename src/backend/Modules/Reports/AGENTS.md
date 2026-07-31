# 📊 src/backend/Modules/Reports/AGENTS.md

> Reports Module — ✅ Phase 6.2 (مكتمل — 20 Accounting Reports + Multi-Company).
>
> محدّث: 2026-07-27 — ترقية من Phase 2.5 إلى Phase 6.2 (Abdo's commits `1ac5aff` + `fbf5a02`)
>
> Hand-off Report: [`../../../docs/HANDOFF-PHASE6-MIGRATE.md`](../../../docs/HANDOFF-PHASE6-MIGRATE.md)
>
> **Phase 6 (2026-07-27) — Multi-Company update (doc-sprint 6.4):** Reports is already Phase 6.2 (filters by `company_id` via `ICompanyContext` — 20 reports migrated). This note acknowledges alignment with Constitution Article 3 and points to the canonical migration guide. See root [AGENTS.md](../../../../AGENTS.md#-multi-company-convention-per-constitution-article-3) and [docs/PHASE6-RELEASE-NOTES.md](../../../../PHASE6-RELEASE-NOTES.md).

## شو فيه

وحدة التقارير المالية والتشغيلية. تقرأ البيانات من DB عبر `IDbConnectionFactory` (Dapper) وتُرجع DTOs محسوبة. **لا تكتب** للـ DB — قراءة فقط (CQRS Read Side).

> **Per Constitution Article 3:** كل method يبدأ بـ `company_id` filter (من `ICompanyContext`). لا `tenant_id` في أي مكان.

---

## Services (15) + 30+ DTOs

### Phase 2.5 (Pre-existing — 3 services)

| Service | Methods | DTOs |
|---------|---------|------|
| **FinanceReportService** | `GetTrialBalanceAsync`<br>`GetIncomeStatementAsync`<br>`GetBalanceSheetAsync` | `TrialBalanceReport`, `TrialBalanceRow`<br>`IncomeStatement`<br>`BalanceSheet` |
| **InventoryReportService** | `GetStockValuationAsync`<br>`GetMovementHistoryAsync`<br>`GetLowStockAsync`<br>`GetStockAgingAsync` | `StockValuation`<br>`StockMovementHistory`<br>`LowStockItem`<br>`StockAging` |
| **ProjectReportService** | `GetProjectPnLAsync`<br>`GetBudgetVsActualAsync`<br>`GetProjectsSummaryAsync` | `ProjectPnL`<br>`ProjectBudgetVsActual`<br>`ProjectSummary` |

### Phase 6.2 additions (Abdo's commit `1ac5aff` — 8 new services)

| Service | Methods | DTOs (in `ReportDtos.cs`) |
|---------|---------|----------------------------|
| **AccountActivityService** | `GetAccountActivityAsync` | `AccountActivityReport`, `AccountActivityRow` |
| **CollectionsService** | `GetCollectionsAsync` | `CollectionsReport`, `CollectionsBucket` |
| **CostCenterReportService** | `GetCostCenterPerformanceAsync` | `CostCenterPerformanceReport`, `CostCenterRow` |
| **JournalEntryReportService** | `GetJournalEntriesAsync` | `JournalEntryReport`, `JournalEntryRow` |
| **VatReportService** | `GetVatReportAsync` | `VatReport`, `VatLine` |
| **SalesByCustomerService** | `GetSalesByCustomerAsync` | `SalesByCustomerReport`, `CustomerSalesRow` |
| **SalesByItemService** | `GetSalesByItemAsync` | `SalesByItemReport`, `ItemSalesRow` |
| **TopCustomersService** | `GetTopCustomersAsync` | `TopCustomersReport` |
| **PurchasesByVendorService** | `GetPurchasesByVendorAsync` | `PurchasesByVendorReport`, `VendorPurchaseRow` |
| **TopVendorsService** | `GetTopVendorsAsync` | `TopVendorsReport` |

> **Note:** `BudgetVsActualService` lives in `Modules/Projects/Application/Services/BudgetVsActualService.cs` (Projects module), not Reports. See [Projects/AGENTS.md](../Projects/AGENTS.md).

**المجموع: 15 services** (3 old + 10 new in Reports module + BudgetVsActual in Projects).

---

## HTTP Endpoints (20 Reports)

### Phase 2.5 endpoints (Pre-existing — 9 endpoints, in `Host/Controllers/ReportsController.cs`)

تحت `/api/reports`، تتطلب `[Authorize]`:

#### Project (3)
- `GET /api/reports/projects/{id:guid}/pnl?from=&to=` → ProjectPnL
- `GET /api/reports/projects/{id:guid}/budget-vs-actual` → ProjectBudgetVsActual
- `GET /api/reports/projects/summary?companyId=` → list of ProjectSummary

#### Inventory (4)
- `GET /api/reports/inventory/valuation?companyId=&warehouseId=` → { count, totalValue, items }
- `GET /api/reports/inventory/movements?itemId=&from=&to=&skip=&take=` → list of StockMovementHistory (paged 1-200, default 50)
- `GET /api/reports/inventory/low-stock?companyId=` → list of LowStockItem
- `GET /api/reports/inventory/aging?companyId=` → list of StockAging

#### Finance (1 — old) + Dashboard (1)
- `GET /api/reports/finance/trial-balance?companyId=&asOfDate=` → TrialBalanceReport
- `GET /api/reports/finance/income-statement?companyId=&from=&to=` → IncomeStatement
- `GET /api/reports/finance/balance-sheet?companyId=&asOfDate=` → BalanceSheet
- `GET /api/reports/dashboard` → dashboard stats

> **الـ 3 finance endpoints القديمة (تحت `/api/reports/finance/...`)** موجودة في `ReportsController`. الـ Phase 6.2 أضافت **11 endpoint جديد** تحت `/api/finance/reports/...` (راجع أدناه). الـ duplication بين الـ prefix و الـ controller path تم تنظيفه جزئياً — خطة: حذف القديم بعد smoke test كامل على الجديد.

### Phase 6.2 endpoints (Abdo's commits — 11 endpoints, in `Host/Controllers/FinanceReportsController.cs`)

تحت `/api/finance/reports`، تتطلب `[Authorize]`:

| # | Endpoint | Service | DTO | الوصف |
|---|----------|---------|-----|-------|
| 1 | `GET /api/finance/reports/trial-balance?asOfDate=` | `FinanceReportService` (existing) | `TrialBalanceReport` | ميزان المراجعة |
| 2 | `GET /api/finance/reports/income-statement?from=&to=` | `FinanceReportService` (existing) | `IncomeStatement` | قائمة الدخل |
| 3 | `GET /api/finance/reports/balance-sheet?asOfDate=` | `FinanceReportService` (existing) | `BalanceSheet` | الميزانية العمومية |
| 4 | `GET /api/finance/reports/cash-flow?from=&to=` | `FinanceReportService` (new) | `CashFlowReport` | التدفقات النقدية |
| 5 | `GET /api/finance/reports/general-ledger?accountId=&from=&to=` | `IGeneralLedgerReportService` | `GeneralLedgerReport` | دفتر الأستاذ |
| 6 | `GET /api/finance/reports/journal-entries?from=&to=&accountId=` | `JournalEntryReportService` | `JournalEntryReport` | القيود المحاسبية |
| 7 | `GET /api/finance/reports/account-activity?accountId=&from=&to=` | `AccountActivityService` | `AccountActivityReport` | حركة حساب |
| 8 | `GET /api/finance/reports/ap-aging?asOf=` | `IAPAgingService` | `APAgingReport` | أعمار الذمم الدائنة |
| 9 | `GET /api/finance/reports/collections?from=&to=` | `CollectionsService` | `CollectionsReport` | التحصيلات |
| 10 | `GET /api/finance/reports/cost-center-performance?from=&to=&costCenterId=` | `CostCenterReportService` | `CostCenterPerformanceReport` | أداء مراكز التكلفة |
| 11 | `GET /api/finance/reports/vat?from=&to=` | `VatReportService` | `VatReport` | تقرير ضريبة القيمة المضافة |

### Cross-module endpoints (4 reports in `ReportsController.cs`)

- `GET /api/reports/sales/sales-by-customer?companyId=&from=&to=` → SalesByCustomerReport
- `GET /api/reports/sales/sales-by-item?companyId=&from=&to=` → SalesByItemReport
- `GET /api/reports/sales/top-customers?companyId=&from=&to=&topN=` → TopCustomersReport (default topN=10)
- `GET /api/reports/procurement/purchases-by-vendor?companyId=&from=&to=` → PurchasesByVendorReport
- `GET /api/reports/procurement/top-vendors?companyId=&from=&to=&topN=` → TopVendorsReport (default topN=10)

**المجموع: 20 report endpoints** (11 in FinanceReportsController + 9 in ReportsController). الـ Pre-Prod Checklist يصف "20 mandatory accounting reports" — هذا هو الـ count النهائي.

---

## Frontend Pages (18 report pages)

Abdo's commit `fbf5a02` أضاف/حدّث 18 صفحة تقرير في `src/frontend/app/(authenticated)/reports/`:

| Path | الوصف | Status |
|------|-------|--------|
| `reports/financial/trial-balance/` | ميزان المراجعة | ✅ |
| `reports/financial/income-statement/` | قائمة الدخل | ✅ |
| `reports/financial/balance-sheet/` | الميزانية العمومية | ✅ |
| `reports/financial/cash-flow/` | التدفقات النقدية | ✅ |
| `reports/financial/general-ledger/` | دفتر الأستاذ | ✅ |
| `reports/financial/journal-entries/` | القيود المحاسبية | ✅ |
| `reports/financial/account-activity/` | حركة حساب | ✅ |
| `reports/financial/ap-aging/` | أعمار الذمم الدائنة | ✅ |
| `reports/financial/collections/` | التحصيلات | ✅ |
| `reports/financial/cost-center-performance/` | أداء مراكز التكلفة | ✅ |
| `reports/financial/vat/` | ضريبة القيمة المضافة | ✅ |
| `reports/inventory/valuation/` | تقييم المخزون | ✅ |
| `reports/sales/sales-by-customer/` | مبيعات حسب العميل | ✅ |
| `reports/sales/sales-by-item/` | مبيعات حسب الصنف | ✅ |
| `reports/sales/top-customers/` | أكبر العملاء | ✅ |
| `reports/procurement/purchases-by-vendor/` | مشتريات حسب المورد | ✅ |
| `reports/procurement/top-vendors/` | أكبر الموردين | ✅ |
| `reports/projects/budget-vs-actual/` | الموازنة مقابل الفعلي | ✅ |

**ملاحظة:** الـ 18 صفحة تستخدم `lib/api.ts` الجديدة (أضاف Abdo `reportApi.*` functions) و `lib/utils.ts` (أضاف `formatCurrency`, `formatPercent`).

---

## الـ DTO Computed Properties (مهمة للـ tests)

### Phase 2.5 (Pre-existing)
- `TrialBalanceRow.NetDebit` / `NetCredit` = `Debit - Credit` / `Credit - Debit`
- `TrialBalanceReport.IsBalanced` = `|TotalDebit - TotalCredit| < 0.01`
- `IncomeStatement.GrossProfit` = `Revenue - Cogs`
- `IncomeStatement.NetIncome` = `GrossProfit - OpEx + OtherIncome - OtherExpenses`
- `BalanceSheet.IsBalanced` = `|Assets - (Liab + Equity)| < 0.01`
- `StockValuation.TotalValue` = `QuantityOnHand * AverageCost`
- `LowStockItem.QuantityAvailable` = `QuantityOnHand - QuantityReserved` (computed في DB)
- `LowStockItem.Shortfall` = `ReorderLevel - QuantityAvailable`
- `LowStockItem.Status` = `"Critical"` if `QtyOnHand == 0`; else `"Warning"` if `QtyOnHand < ReorderLevel/2`; else `"Low"`
- `StockAging.AgeBucket` = `"0-30"` / `"31-60"` / `"61-90"` / `"90+"`
- `ProjectPnL.DirectCosts` = `MaterialCost + LaborCost + SubcontractorCost`
- `ProjectPnL.NetProfit` = `Revenue - DirectCosts - AllocatedOverhead`
- `ProjectBudgetVsActual.AvailableAmount` = `BudgetAmount - SpentAmount - CommittedAmount`
- `ProjectBudgetVsActual.Variance` = `BudgetAmount - SpentAmount`
- `ProjectBudgetVsActual.UtilizationPercent` = `SpentAmount / BudgetAmount * 100`

### Phase 6.2 additions (من `ReportDtos.cs`)
- `APAgingReport.Buckets[]` = `[{0-30, 31-60, 61-90, 90+}]` — مجموع الذمم الدائنة لكل bucket
- `CollectionsReport.TotalCollected` = `SUM(receipts.amount) WHERE posted_at IS NOT NULL`
- `CollectionsReport.AverageDaysToCollect` = `AVG(payment_date - invoice_date)` weighted
- `VatReport.OutputVat` / `InputVat` = `SUM(vat_amount)` from sales_invoices vs purchase_bills
- `VatReport.NetVat` = `OutputVat - InputVat`
- `CostCenterPerformanceReport.Allocated` / `Actual` / `Variance` per cost center
- `SalesByCustomerReport.TopNCustomers` (sorted by total DESC)
- `TopCustomersReport.Customers[]` (limited to topN param)
- `AccountActivityReport.OpeningBalance` / `ClosingBalance` = `SUM(lines) FROM startDate..endDate`

---

## Multi-Company Compliance (Constitution Article 3)

> **كل الـ report queries في Phase 6.2 مفلترة بـ `company_id` من `ICompanyContext.CompanyId` — لا `tenant_id` في أي مكان.**

| المبدأ | التطبيق في Reports |
|--------|-------------------|
| `ICompanyContext.CompanyId` | كل method يبدأ بـ `WHERE company_id = @CompanyId` filter |
| `X-Company-Id` header | الـ Frontend يرسل الـ header في `lib/api.ts` (auto من CompanySwitcher) |
| Holding UUID | كل Reports في الـ Holding الافتراضي (`00000000-0000-0000-0000-000000000001`) افتراضياً |
| Subsidiaries | عند اختيار شركة في CompanySwitcher، كل التقارير تتحدث تلقائياً |
| لا `tenant_id` | 0 references في Reports module (تم تنظيفه في Phase 6.1b) |

---

## P0 Fix History (مهم — Abdo's commit `d450dae`)

في الـ Phase 6.0 schema reset، 7 جداول ضاعت لكن الـ services بقيت تشير لها. عبده أضافها كـ JSON DataType definitions:

| JSON File | Table | Used By |
|-----------|-------|---------|
| `sales_invoice_lines.json` | `sales_invoice_lines` | SalesByItemService, CollectionsService |
| `receipts.json` | `receipts` | CollectionsService, SalesInvoiceRepository |
| `receipt_allocations.json` | `receipt_allocations` | CollectionsService |
| `purchase_orders.json` | `purchase_orders` | PurchasesByVendorService |
| `purchase_order_lines.json` | `purchase_order_lines` | PurchasesByVendorService |
| `goods_receipts.json` | `goods_receipts` | PurchasesByVendorService |
| `goods_receipt_lines.json` | `goods_receipt_lines` | PurchasesByVendorService |

عند next startup، الـ `DataTypeMigrator` ينشئ هذه الجداول من JSON على Supabase. **ملاحظة:** الـ DataTypeMigrator يحتاج `ConnectionStrings:Migrations` متاح — حالياً يشير لـ `localhost:5432` (غير متاح محلياً)، فالـ app يطبع error لكن يكمل (best-effort pattern). الـ schema في Supabase يأتي من runs السابقة.

### SQL column name fixes (P0 commit)
- `SalesByCustomerService`: `SUM(si.outstanding)` → `SUM(si.total_amount - si.paid_amount)` (computed)
- `SalesByItemService`: `sil.invoice_id` → `sil.sales_invoice_id`; `sil.sub_total` → `sil.line_total`; `sil.tax_amount` computed from `line_total * tax_rate`
- `SalesInvoiceRepository.GetTotalAllocatedAsync`: `r.status = 'Posted'` → `r.posted_at IS NOT NULL`

### DI fixes (P0 commit)
4 services كانت مفقودة من `Program.cs` (سبب 500 على `/api/finance/reports/*`):
- `IFinanceReportService`
- `IGeneralLedgerReportService`
- `IBalanceSheetService`
- `ICashFlowService`

أُضيفت في الـ P0 fix.

---

## Conventions

- **Read-only:** لا تكتب في DB. أي mutation → ارفع event واترك الـ module المختص يعالجها
- **CompanyId mandatory:** كل method يبدأ بـ `companyId` filter (من `ICompanyContext` — Constitution Article 3)
- **DateTime as UTC:** كل الـ dates بـ UTC
- **CancellationToken:** كل الـ async methods تأخذه وتُمرره للـ DB
- **No caching في MVP:** نتركه لـ Redis layer في Phase 3
- **Comments بالعربي** (موجودة في الـ services)
- **No tenant_id:** constitution §3 violation — ممنوع
- **No `tenantId` parameter** في أي method signature

---

## Test Pattern

يستخدم [`../../Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory`](../../Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs)
لـ in-memory simulation.

### Existing unit tests

- `Tests/Reports/FinanceReportServiceTests.cs` (7 tests)
- `Tests/Reports/InventoryReportServiceTests.cs` (7 tests)
- `Tests/Reports/ProjectReportServiceTests.cs` (6 tests)

**المجموع: 20 unit tests** (Pre-Phase 6.2).

### Phase 6.2 — TODO (known gap)

> ⚠️ **Abdo's Phase 6.2 لم يضف unit tests للـ 10 services الجديدة.** الـ 20 reports معتمدة على:
> - Playwright smoke (39/39 passing per `docs/PRE-PROD-CHECKLIST.md`)
> - Playwright security (9/9 passing)
> - Seed integrity (A=L+E-X, 765 JEs balance, 0 negative stock)
> - **Manual verification** عند الـ QA

**التوصية:** أضف unit tests للـ 10 services الجديدة. هذا gap سيُعالج في commit قادم (مش في هذا الـ PR).

### Playwright tests (e2e)

- `src/frontend/e2e/smoke.spec.ts` (39 tests) — includes happy paths for all 20 report endpoints
- `src/frontend/e2e/security.spec.ts` (9 tests) — auth, SQLi, multi-company isolation

---

## Dependencies

- `IDbConnectionFactory` (from `Shared/`)
- `ICompanyContext` (from `Shared/CompanyContext/` — renamed from `Shared/MultiTenancy/` in Sprint 10 Phase 2 to align folder name with the artifact)
- Dapper (raw SQL)
- `IProjectRepository`, `IProjectBudgetRepository` (Project module — للـ ProjectReportService فقط)
- `IAccountRepository`, `IJournalEntryRepository` (Finance module)
- `IInvoiceRepository`, `IReceiptRepository` (AccountsReceivable module — للـ AR/Collections reports)
- `IVendorBillRepository`, `IPurchaseOrderRepository`, `IGoodsReceiptRepository` (Procurement module)
- `IItemRepository`, `IStockLevelRepository` (Inventory module)
- `ICostCenterRepository` (Finance module — للـ CostCenterReportService)

---

## Integration مع الباقي

- **Finance:** يقرأ من `accounts`, `journal_lines`, `journal_entries`, `cost_centers`
- **Inventory:** يقرأ من `stock_levels`, `items`, `warehouses`, `stock_movements`
- **Projects:** يقرأ من `projects`, `project_budgets`, `journal_lines` (joined by `cost_center_id`)
- **AR (AccountsReceivable):** يقرأ من `sales_invoices`, `sales_invoice_lines`, `receipts`, `receipt_allocations`
- **AP (Procurement):** يقرأ من `purchase_orders`, `purchase_order_lines`, `goods_receipts`, `goods_receipt_lines`, `vendor_bills`
- **VAT:** computed from `sales_invoices.vat_amount` (output) و `vendor_bills.vat_amount` (input)

> **Cross-Company Reads:** الـ Reports يمكنها قراءة من جداول shared (مثل `items`, `customers` بدون `company_id` strict) عبر `is_shared` flag. لكن الـ financial tables (CoA, journals) مفصولة بـ `company_id`.

---

## Known Issues / Future Work

من `docs/PRE-PROD-CHECKLIST.md` "Known Limitations":

| Item | Severity | Notes |
|------|----------|-------|
| Duplicate `/api/reports/finance/*` (3 endpoints) vs `/api/finance/reports/*` (11 endpoints) | Low | TODO consolidate. الـ prefix الجديد أنظف (تحت `/api/finance/reports`). الـ القديم سيُحذف بعد smoke test كامل. |
| AR aging endpoint location (`/api/ar/aging` in `FinanceArController`) | Low | Single endpoint — الـ Pre-Prod Checklist ذكره بالخطأ كـ duplicate. |
| Unit tests للـ 10 services الجديدة | Medium | Gap في test coverage. سيُعالج في commit قادم. |
| Excel export for reports | Medium | حالياً JSON/HTML فقط |
| PDF export for reports | Medium | Not built |
| Charts (line/bar) for trend reports | Low | Time-series visualization not implemented |

---

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md) — root
- [`../../Host/AGENTS.md`](../../Host/AGENTS.md) — Host (DI, Controllers)
- [`../Finance/AGENTS.md`](../Finance/AGENTS.md) — Chart of Accounts, Journals
- [`../Inventory/AGENTS.md`](../Inventory/AGENTS.md) — Items, Stock, Warehouses
- [`../Projects/AGENTS.md`](../Projects/AGENTS.md) — Projects, Budgets (BudgetVsActualService هنا)
- [`../AccountsReceivable/AGENTS.md`](../AccountsReceivable/AGENTS.md) — Customers, Invoices, Receipts (AR/Collections)
- [`../Procurement/AGENTS.md`](../Procurement/AGENTS.md) — Vendors, POs, GRs, Bills (Purchases/Top Vendors)
- [`../../Shared/AGENTS.md`](../../Shared/AGENTS.md) — IDbConnectionFactory, ICompanyContext
- [`../../Tests/AGENTS.md`](../../Tests/AGENTS.md) — test patterns
- [`../HR/AGENTS.md`](../HR/AGENTS.md) — Phase 3.5 (Headcount report — مستقبلي)
- [`../Payroll/AGENTS.md`](../Payroll/AGENTS.md) — Phase 4 (Payroll summary, EOS liability — مستقبلي)
- [`docs/HANDOFF-PHASE6-MIGRATE.md`](../../../docs/HANDOFF-PHASE6-MIGRATE.md) — Hand-off report
- [`docs/PRE-PROD-CHECKLIST.md`](../../../docs/PRE-PROD-CHECKLIST.md) — Pre-Production checklist
- [`docs/SYSTEM-FUNCTIONAL-SPECIFICATION.md`](../../../docs/SYSTEM-FUNCTIONAL-SPECIFICATION.md) — Functional spec


---

## 🤝 Cross-Team Coordination (Brainstorming Lab)

This project works with an analytical team via the **Brainstorming Lab**.

- **When to read from hub**: ONLY when explicitly instructed by the analytical team
- **Default**: Work from local context (this file + root `AGENTS.md` + source code)
- **Hub repo**: https://github.com/anas600/brainstorming-lab/tree/main/portals/02-session-002/

See root [`AGENTS.md`](../../../../AGENTS.md) for full cross-team protocol.

Token-efficient: ~50 tokens per cross-team directive (vs 500+ for full re-paste).
