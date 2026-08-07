# Sprint 53-56 Plan — Path A + B + C Completion

## الهدف: إكمال النظام إلى مرحلة شبه جاهزة للتسليم (Demo-Ready for Client)

**Branch:** `feature/sprint-52-v0-polish` (current) — new commits
**Mode:** LOCAL-ONLY (no remote push until Anas says "ادفع")
**Tester:** Admin Team (mvs_b9474c7e816d4753b1925d5f96c292c2)
**Strategy:** Parallel via sub-agents (BE Jimi + FE Jimi)

---

## Sprint 53 — Path A.1: Year-End Closing (Backend Accounting Purity)

**DEC-140 — Year-End Closing Entry**:
- خدمة `YearEndClosingService` تنشئ قيد فعلي بتاريخ 2025-12-31:
  - DR كل حساب Revenue (4xxx) — مجموع الأرصدة
  - CR كل حساب Expense (5xxx) — مجموع الأرصدة
  - CR/DR صافي السنة → 3210 (Current Year P&L) — صفر
- يحذف الصف الافتراضي "صافي دخل السنة" من BS (اللي أضفناه في Sprint 52a)
- BS يستخدم القيد الحقيقي بدل الـ synthetic row
- الـ entry = `JOURNAL-2025-CLOSING` (idempotent — ON CONFLICT DO NOTHING)

**DEC-141 — Retained Earnings Roll**:
- بعد إغلاق السنة، NetIncome يُرحَّل من 3210 → 3200 (Retained Earnings)
- قيد تلقائي بتاريخ أول يوم في السنة الجديدة (2026-01-01)
- BS يصبح متوازن بدون أي صف افتراضي

**Acceptance**:
- BS asOf=2026-08-07 → IsBalanced=true, Variance=0
- بدون الـ synthetic NET row
- journal_entries row واحد جديد: `YE-2025-CLOSING`
- NetIncome في 3200 (Retained Earnings)

---

## Sprint 54 — Path A.2: Reports Traverse Hierarchy (TB/IS/CF)

**DEC-142 — Trial Balance with Hierarchy**:
- TB يعرض L3 (Control) كـ group rows مع L4 (Detail) expandable/collapsible
- أو: عرض L4 دائمًا + filter بالـ level
- ترقيم: 1000-ASSETS, 1100-Current Assets, 1101-Cash

**DEC-143 — Income Statement with L2 Sections**:
- IS يعرض L2 (Sub-class) كـ sections: "إيرادات المبيعات" + "إيرادات الخدمات" + "إيرادات أخرى"
- كل section يحتوي L4 (Detail) تحته
- 5xxx Expenses بنفس النمط: "تكلفة المبيعات" + "مصاريف تشغيلية" + "مصاريف مالية"

**DEC-144 — Cash Flow with L3 Lines**:
- CF يعرض L3 (Control accounts) كـ lines بدل L4 (Transactions)
- أمثلة: "Cash and bank" (1100s) + "Accounts Receivable" (1230) + "Inventory" (1300)

**Acceptance**:
- TB: 8-10 L3 rows + 50+ L4 details
- IS: 3 L2 Revenue sections + 3 L2 Expense sections
- CF: Operating/Investing/Financing sections مع L3 lines

---

## Sprint 55 — Path B: Scenario Seeder Refactor (Real Tables)

**DEC-145 — SalesInvoice seeder**:
- بدل كتابة `journal_lines` يدويًا لكل فاتورة مبيعات:
  - ينشئ `sales_invoices` row (status=Posted, paid_amount=0)
  - ينشئ `sales_invoice_lines` (1-2 items)
  - ينشئ `journal_entries` تلقائيًا عبر الـ Posting Rules (DR 1230 AR / CR 4110 Revenue)
- النتيجة: AR aging reports بيانات صحيحة بدون hack

**DEC-146 — VendorBill seeder**:
- نفس النمط لـ `vendor_bills`:
  - ينشئ `vendor_bills` row (status=Posted, paid_amount varies)
  - ينشئ `vendor_bill_lines`
  - ينشئ `journal_entries` عبر Posting Rules (DR 5110 Expense / CR 2210 AP)
- AP aging reports بيانات صحيحة

**DEC-147 — Payment seeder**:
- ينشئ `payments` + `payment_allocations` بدل journal_lines المباشر
- الـ payments تستخدم الـ posting rules للـ journal entry

**Acceptance**:
- AP aging: 13 vendors مع bills حديثة (0-30/31-60/61-90/91+)
- AR aging: 6 customers مع invoices حديثة
- BS لا يتأثر (الـ journal entries متطابقة)

---

## Sprint 56 — Path C.1: More Reports (Budget/Top/Sales)

**DEC-148 — Budget vs Actual Report**:
- صفحة جديدة `/finance/reports/budget-vs-actual`
- يعرض لكل AccountType 4: actual vs budgeted (من Project budgets)
- Variance % بالألوان (أحمر = over budget, أخضر = under)

**DEC-149 — Top Customers Report**:
- `/finance/reports/top-customers`
- Top 10 عملاء حسب المبيعات (آخر 12 شهر)
- Bar chart + table

**DEC-150 — Top Items Report**:
- `/finance/reports/top-items`
- Top 10 أصناف حسب المبيعات

**DEC-151 — Sales by Customer/Item Drill-down**:
- تقرير موجود في Sprint 22 لكن اتشال (DEC-removed)
- أعيد تفعيله للـ Layer 1 demo
- `/finance/reports/sales-by-customer`, `/sales-by-item`

**Acceptance**:
- 4 تقارير جديدة + Drill-down من BS rows للتقارير المتعلقة

---

## Sprint 57 — Path C.2: Dashboards + Charts

**DEC-152 — Executive Dashboard**:
- `/dashboard/executive` — Holding overview
- KPIs: Revenue YTD, Expenses YTD, Net Income, Cash position, AR/AP totals
- 4 charts: Revenue trend, Top customers, Expense breakdown, Cash flow

**DEC-153 — Subsidiary Dashboard**:
- `/dashboard/subsidiary/[id]` — per-company view
- مقارنة مع Holding + periods

**DEC-154 — AR/AP Aging Charts**:
- Add charts to aging-summary page
- Bar chart of AR vs AP, breakdown by aging bucket

**Acceptance**:
- صفحة `/dashboard` الحالية محسّنة (charts + KPIs)
- 2 dashboards جديدة (executive + subsidiary)
- AR/AP aging visualisations

---

## Verification

1. **dotnet build**: 0 errors
2. **npm type-check + build**: 0 errors
3. **Playwright tests**: 24 pages، each passes
4. **Accounting verification**:
   - BS balances بدون synthetic row
   - NetIncome = 0 بعد closing
   - AP/AR aging shows real data
5. **Screenshots**: docs/screenshots/sprint-53-56/

---

## Carry-Over (post Sprint 57)

- **Path D**: Production prep (CI, deployment, security) — deferred per Anas
- **L4 per-customer/vendor sub-accounts**: Future IFRS-compliant enhancement
- **V1/V2/V3 branches**: kept per Anas directive
