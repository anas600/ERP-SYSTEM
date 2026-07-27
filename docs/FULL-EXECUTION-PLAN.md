# Plan الكامل: تنفيذ A+B+C مع دقة بيانات عالية

**الهدف:** تسليم المشروع كاملاً — backend مستقر، frontend كامل، بيانات حقيقية، تقارير تعطي أرقام منطقية.

**المشروع:** `F:\minimaxDescktop2\ERP-SYstem` (المشروع الجديد، متطابق مع القديم)
**التاريخ:** 2026-07-26
**الـ Holding UUID:** `ec6b98ee-221c-410e-a690-192245314a68` (auto-seeded)

**التحديث (2026-07-26):** بناءً على طلب المستخدم:
- ✅ **Database:** PostgreSQL محلي (مش Supabase) — `C:\Program Files\PostgreSQL\18\bin\psql.exe`
- ✅ **Environment:** بيئة تطوير محلية (مش deploy)
- ✅ **Focus:** صحة البيانات + صفر أخطاء برمجية (الأولوية الأولى)

---

## 🎯 معايير النجاح (Definition of Done)

لازم تتحقق **كلها** قبل ما نسلم:

### Backend
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] `dotnet run` → يبدأ في < 30s بدون exceptions
- [ ] كل الـ 39 API endpoint ترجع 2xx (مش 500/404)
- [ ] كل استعلام SQL < 2 ثانية
- [ ] CORS يسمح لـ localhost:3000

### Frontend
- [ ] `npm run dev` → يبدأ في < 30s
- [ ] `npx tsc --noEmit` → 0 errors
- [ ] كل الـ 77 page تعمل render
- [ ] الـ sidebar يعرض ≥ 30 رابط
- [ ] لا توجد console errors في DevTools بعد login

### Data
- [ ] كل Journal Entry: SUM(debit) = SUM(credit)
- [ ] كل Sales Invoice: total = subtotal + VAT (15%)
- [ ] كل Vendor Bill: نفس الـ rule
- [ ] كل Attendance: داخل فترة عمل الموظف
- [ ] كل Leave: ما يتعارض مع leaves ثانية
- [ ] كل Stock Movement: كمية > 0 + item + warehouse موجودين
- [ ] كل FK يشير لسجل موجود

### Reports
- [ ] كل الـ 20 reports ترجع بيانات (مش فاضية)
- [ ] الأرقام منطقية (Revenue > 0, COGS < Revenue, etc.)
- [ ] Total Assets = Total Liabilities + Equity
- [ ] Net Income = Revenue - Expenses
- [ ] Trial Balance متوازن (debits = credits)

---

## 📋 قواعد دقة البيانات (Mandatory)

كل transaction في الـ seed لازم يلتزم بالقواعد دي:

### 1. Accounting Equation
```
Assets = Liabilities + Equity
```
→ أي عملية بتكسر المعادلة = خطأ في الـ seed.

### 2. Journal Entry Rule
```
SUM(debit lines) = SUM(credit lines) — بالهللة
```
→ مثال: Salary 5000 LYD = Dr Salary Expense 5000, Cr Cash 4250, Cr Tax Payable 750

### 3. VAT Rule (ليبيا 15%)
```
Subtotal = sum(line.qty * line.price)
VAT = Subtotal * 0.15
Total = Subtotal + VAT
```
→ تطبق على Sales Invoices و Vendor Bills

### 4. Stock Movement Rule
```
qty > 0
item exists
warehouse exists
```
→ Issue لازم يكون متبوع بـ Receipt (لا negative stock)

### 5. Attendance Rule
```
employee exists
work_date BETWEEN employee.hire_date AND today
clock_in <= clock_out
```

### 6. Leave Rule
```
employee exists
start_date <= end_date
days > 0
not overlapping with another approved leave
```

### 7. Date Coherence
```
PO.date <= GR.date <= Bill.date <= Payment.date
Sale.date <= Receipt.date
Payroll.start <= Payroll.end
```

### 8. Reference Coherence
```
Every FK points to existing record
Every total = sum of its lines
Every count matches expected
```

---

## 🚀 خطة التنفيذ (10 مراحل)

### المرحلة 0: Foundation Check (15 دقيقة)

**الهدف:** تأكيد إن كل شي شغّال قبل ما نبدأ.

```powershell
# 1. تأكد من العمليات شغّالة
netstat -ano | Select-String ":5000|:3000"

# 2. تأكد من .env.local موجود
Test-Path F:\minimaxDescktop2\ERP-SYstem\src\frontend\.env.local

# 3. Test login
$body = '{"email":"admin@alfajr.local","password":"Demo1234"}'
$r = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method POST -Body $body -ContentType "application/json"
# → يجب يرجع 200 + accessToken
```

**✅ معيار النجاح:** Login يرجع 200 + token، backend و frontend شغّالين.

---

### المرحلة 1: Reference Data (يوم — 8 ساعات)

**الهدف:** كل الـ master data في الـ DB.

**الخطوات:**

1. **تحويل SQL** من `tenant_id` → `company_id` (multi-company)
2. **تطبيق الـ seed** على الـ Holding UUID:
   - Chart of Accounts (47) — موجود ✅
   - Customers (16) — موجود في SQL
   - Vendors (12) — موجود في SQL
   - Items (21) — موجود في SQL
   - Warehouses (4) — موجود
   - Cost Centers (14) — موجود
   - Projects (4) — موجود
   - Departments (6) — موجود
   - Item Categories (5) — موجود
   - UoMs (6) — موجود

3. **التحقق:**
   ```sql
   SELECT 'customers' AS t, count(*) FROM customers WHERE company_id = 'ec6b98ee-...';
   -- Expected: 16
   SELECT 'vendors', count(*) FROM vendors WHERE company_id = 'ec6b98ee-...';
   -- Expected: 12
   -- ... باقي الجداول
   ```

**✅ معيار النجاح:** كل العدّادات تطابق المتوقع، لا FK violations.

---

### المرحلة 2: HR Data (يوم — 8 ساعات)

**الهدف:** بيانات HR واقعية.

**الخطوات:**

1. **Employees (17):**
   - كل موظف له: dept, job_title, hire_date, base_salary
   - مرتبات واقعية (Libyan market): 800-8000 LYD/شهر
   - Hire dates موزعة على آخر 5 سنوات

2. **Salary Structures (3):**
   - SS-MGR: managers (allowances + bonus)
   - SS-ENG: engineers
   - SS-WKR: workers (basic only)

3. **Attendance (6427 records):**
   - كل موظف ~378 يوم حضور في السنة (12 شهر × 30 يوم)
   - Mix of: Present (90%), Absent (5%), Leave (3%), Sick (2%)
   - clock_in: 7:30-9:00 AM
   - clock_out: 15:00-17:30 PM
   - لا حضور في الجمعة والسبت (weekend off)
   - لا حضور في الإجازات الموافق عليها

4. **Leave Requests (617):**
   - 5-10 leaves per employee
   - Mix: Approved (70%), Pending (15%), Rejected (15%)
   - لا تعارض بين leaves
   - Varied: Annual, Sick, Unpaid, Maternity

5. **Payroll Runs (12 — واحد لكل شهر):**
   - Gross = base + allowances
   - Tax = gross * 10%
   - Net = gross - tax
   - Posted status
   - Journal entry لكل run (Dr Salary, Cr Cash + Cr Tax)

**✅ معيار النجاح:** كل الموظف عنده حضور و leaves، كل payroll له journal entry متوازن.

---

### المرحلة 3: Operations Data (يومين — 16 ساعة)

**الهدف:** كل الـ operations flows واقعية.

**الخطوات:**

#### 3.1 Procurement Flow (يوم)
```
PO → GR → Bill → Payment
```
- **43 Purchase Orders** (آخر 12 شهر)
  - Status: 30 Approved, 10 Sent, 3 Draft
  - لكل PO: 1-3 line items
  - Total = sum(lines.qty * lines.price)
  - Currency: LYD
  - Date: موزعة على 12 شهر
- **31 Goods Receipts** (80% من الـ POs)
  - كل GR مرتبط بـ PO
  - GR.date >= PO.date
  - Status: Received
  - يحدث Inventory تلقائياً
- **30 Vendor Bills** (من الـ GRs)
  - Bill.date >= GR.date
  - Subtotal + VAT (15%) = Total
  - Status: Posted (50%) / Draft (30%) / Approved (20%)
- **27 Payments** (من الـ Bills)
  - Payment.date >= Bill.date
  - Amount ≤ Bill outstanding
  - Status: Completed

#### 3.2 Sales Flow (يوم)
```
Sales Invoice → Receipt
```
- **61 Sales Invoices**
  - Status: 45 Paid, 10 Partial, 6 Draft
  - Subtotal + VAT = Total
  - Issue لكل invoice: stock decrease + AR increase
- **55 Receipts** (90% من invoices)
  - Receipt.date >= Invoice.date
  - Amount ≤ Invoice outstanding
  - Status: Posted

#### 3.3 Stock (نصف يوم)
- **25 Stock Levels** (initial state: 5-6 items per warehouse)
- **79 Stock Movements**:
  - 50 Receipts (from GRs)
  - 20 Issues (from sales)
  - 9 Transfers (between warehouses)
  - All qty > 0
  - Never negative final stock

**✅ معيار النجاح:** كل PO → GR → Bill → Payment يتبع التاريخ الصحيح، الـ inventory متوازن.

---

### المرحلة 4: Journal Entries (يومين — 16 ساعة)

**الهدف:** كل عملية مالية لها journal entry متوازن.

**القاعدة الذهبية:** SUM(debit) = SUM(credit) — بالهللة.

**~250-300 Journal Entries متوقعة:**

#### 4.1 Opening Balances (يوم 1 صباحاً)
- Cash: 500,000 LYD
- Bank: 2,000,000 LYD
- Inventory: 1,500,000 LYD
- Capital: 4,000,000 LYD

#### 4.2 Auto-generated from Operations (يوم 1-2)
كل عملية تولد 1+ journal entries:

| Operation | Journal Entry |
|---|---|
| PO Approved | (No journal — just a commitment) |
| GR Received | Dr Inventory, Cr Accrued Purchases |
| Bill Posted | Dr Accrued Purchases, Dr VAT Input, Cr Accounts Payable |
| Payment | Dr Accounts Payable, Cr Cash/Bank |
| Sale Posted | Dr Accounts Receivable, Cr Revenue, Cr VAT Output |
| Receipt | Dr Cash/Bank, Cr Accounts Receivable |
| Payroll Posted | Dr Salary Expense, Cr Cash, Cr Tax Payable |
| Stock Issue | Dr COGS, Cr Inventory |

#### 4.3 Period-end adjustments (يوم 2)
- Depreciation
- Accruals
- Closing entries

**الخطوات:**

1. اكتب SQL function يولّد journal entries من الـ operations:
   ```sql
   CREATE OR REPLACE FUNCTION generate_journal_entries()
   RETURNS void AS $$
   -- لكل bill: create journal entry
   -- لكل sale: create journal entry
   -- ... etc
   $$;
   ```

2. شغّل الـ function بعد ما الـ operations تخلص

3. **التحقق الإلزامي:**
   ```sql
   SELECT 
     je.id, 
     je.entry_number,
     SUM(jl.debit) AS total_debit,
     SUM(jl.credit) AS total_credit,
     ABS(SUM(jl.debit) - SUM(jl.credit)) AS diff
   FROM journal_entries je
   JOIN journal_lines jl ON jl.journal_entry_id = je.id
   WHERE je.company_id = 'ec6b98ee-...'
   GROUP BY je.id, je.entry_number
   HAVING ABS(SUM(jl.debit) - SUM(jl.credit)) > 0.01;
   -- Expected: 0 rows
   ```

**✅ معيار النجاح:** كل الـ journal entries متوازنة، Total Assets = Total Liabilities + Equity.

---

### المرحلة 5: Fix Broken Endpoints (نصف يوم — 4 ساعات)

**الهدف:** كل الـ 15 endpoint المكسور يصلح.

**الإصلاحات المتوقعة:**

| # | Endpoint | Bug | Fix |
|---|---|---|---|
| 1 | `GET /api/finance/reports/trial-balance` | SQL alias `TotalDebit` | غير إلى `Debit` |
| 2 | `GET /api/finance/reports/balance-sheet` | Empty data | Will work after seed |
| 3 | `GET /api/finance/reports/income-statement` | Empty data | Will work after seed |
| 4 | `GET /api/finance/reports/cash-flow` | Empty data | Will work after seed |
| 5 | `GET /api/finance/reports/general-ledger` | DI: `IGeneralLedgerReportService` not registered | Add to Program.cs |
| 6 | `GET /api/finance/reports/vat` | Empty data | Will work after seed |
| 7 | `GET /api/finance/reports/ap-aging` | Empty data | Will work after seed |
| 8 | `GET /api/finance/reports/collections` | Empty data | Will work after seed |
| 9 | `GET /api/finance/reports/cost-center-performance` | Empty data | Will work after seed |
| 10 | `GET /api/ar/reports/sales-by-customer` | Empty data | Will work after seed |
| 11 | `GET /api/ar/reports/sales-by-item` | Empty data | Will work after seed |
| 12 | `GET /api/procurement/pos` | DI / schema | Investigate |
| 13 | `GET /api/procurement/grs` | DI / schema | Investigate |
| 14 | `PUT /api/ar/receipts/{id}/post` | Implementation missing | Implement or fix |
| 15 | `GET /api/admin/posting-rules` | **404** — endpoint doesn't exist | Add controller |

**الخطوات:**

1. شغّل كل endpoint، اقرأ الـ stack trace في الـ backend log
2. أصلح كل واحد، اختبر، وثّق
3. شغّل smoke test script للتأكد من كلهم

**✅ معيار النجاح:** كل الـ 15 endpoint يرجع 200 مع بيانات.

---

### المرحلة 6: UI Reports (يوم — 8 ساعات)

**الهدف:** 11 reports page مفقودة.

**الصفحات المطلوب إضافتها:**

| # | Page | Method موجود | حجم |
|---|---|---|---|
| 1 | `/reports/financial/cash-flow` | `reportsApi.cashFlow` | 80 سطر |
| 2 | `/reports/financial/general-ledger` | `reportsApi.generalLedger` | 120 سطر |
| 3 | `/reports/financial/account-activity` | `reportsApi.accountActivity` | 100 سطر |
| 4 | `/reports/financial/journal-entries` | `reportsApi.journalEntries` | 100 سطر |
| 5 | `/reports/financial/ap-aging` | `reportsApi.apAging` | 80 سطر |
| 6 | `/reports/financial/collections` | `reportsApi.collections` | 80 سطر |
| 7 | `/reports/financial/cost-center-performance` | `reportsApi.costCenterPerformance` | 100 سطر |
| 8 | `/reports/sales/sales-by-item` | `reportsApi.salesByItem` | 80 سطر |
| 9 | `/reports/sales/top-customers` | `reportsApi.topCustomers` | 80 سطر |
| 10 | `/reports/procurement/purchases-by-vendor` | `reportsApi.purchasesByVendor` | 80 سطر |
| 11 | `/reports/procurement/top-vendors` | `reportsApi.topVendors` | 80 سطر |

**نمط الصفحة:** انسخ من `trial-balance/page.tsx`، غيّر الـ endpoint والـ columns.

**✅ معيار النجاح:** كل report page يفتح ويعرض البيانات الصحيحة.

---

### المرحلة 7: UI Detail Pages (يوم — 8 ساعات)

**الهدف:** 8 detail/edit pages مفقودة.

| Page | المطلوب |
|---|---|
| `/procurement/purchase-orders/[id]` | Read PO, display lines, post/cancel actions |
| `/procurement/goods-receipts/[id]` | Read GR, display received lines |
| `/finance/customers/[id]` | Read customer, show balance + recent invoices |
| `/finance/sales-invoices/[id]` | Read invoice, show lines + payments + actions |
| `/finance/receipts/[id]` | Read receipt, show allocation |
| `/hr/payroll/[id]` | Read payroll run, show items + process action |
| `/hr/payroll/[id]/payslip/[empId]` | Read payslip, show breakdown |
| `/finance/accounts/[id]/edit` | Read account, edit form |

**✅ معيار النجاح:** كل detail page يفتح ويعرض بيانات السجل.

---

### المرحلة 8: Admin & Identity (نصف يوم — 4 ساعات)

**الهدف:** صفحات admin شغّالة.

| Page | المطلوب |
|---|---|
| `/admin/users` | List users + create/edit/deactivate |
| `/admin/roles` | List roles |
| `/admin/companies` | List + tree view |
| `/admin/audit` | Audit log viewer |
| `/admin/health` | System health dashboard |
| `/admin/item-categories/*` | CRUD |
| `/admin/posting-rules/*` | CRUD |
| `/admin/notifications/*` | List + detail |
| Profile page + change password | Top bar |

**✅ معيار النجاح:** كل admin page شغّال.

---

### المرحلة 9: UX Polish (نصف يوم — 4 ساعات)

**الهدف:** النظام يحس professional.

- [ ] Sidebar menu كامل (≥ 30 رابط مع icons)
- [ ] Toasts على كل action
- [ ] Loading skeletons
- [ ] Empty states مع icons
- [ ] Error boundaries
- [ ] Bulk approve لـ leaves
- [ ] Drill-down (Trial Balance → General Ledger للحساب)
- [ ] Date range picker موحد
- [ ] Currency formatter (LYD)

**✅ معيار النجاح:** النظام يحس نظيف وسريع.

---

### المرحلة 10: Final Verification (نصف يوم — 4 ساعات)

**الهدف:** كل شي يمر من الـ QA.

**Checklist:**

#### Backend
- [ ] `dotnet build` → 0 errors
- [ ] كل الـ 50+ endpoints ترجع 200 (test بـ PowerShell script)
- [ ] لا استعلام > 2s
- [ ] لا exceptions في الـ log

#### Frontend
- [ ] `npm run build` → success
- [ ] `npx tsc --noEmit` → 0 errors
- [ ] كل الـ 77+ page تعمل render
- [ ] لا console errors

#### Data integrity
- [ ] كل Journal Entry متوازن
- [ ] كل Sales Invoice: subtotal + VAT = total
- [ ] كل Vendor Bill: نفس الـ rule
- [ ] Total Assets = Total Liabilities + Equity
- [ ] Net Income = Revenue - Expenses
- [ ] Trial Balance متوازن
- [ ] كل FK valid
- [ ] كل تاريخ منطقي

#### Reports
- [ ] كل الـ 20 reports ترجع أرقام
- [ ] الأرقام منطقية (Revenue > 0)
- [ ] النسب معقولة (Gross margin 20-40%)

#### UX
- [ ] Login flow: success
- [ ] Navigation: كل sidebar link يفتح
- [ ] Forms: submit + validation
- [ ] No 404, no 500 في الـ console

**Deliverables:**

1. **Backend logs** نظيفة (لا errors)
2. **Frontend** يفتح بدون console errors
3. **Database** فيها بيانات واقعية
4. **تقرير نهائي** `docs/FINAL-VERIFICATION-REPORT.md` بكل النتائج

---

## ⏱️ تقدير الوقت

| المرحلة | الوقت |
|---|---|
| 0. Foundation | 15 min |
| 1. Reference Data | 8 h |
| 2. HR Data | 8 h |
| 3. Operations | 16 h |
| 4. Journal Entries | 16 h |
| 5. Fix Endpoints | 4 h |
| 6. UI Reports | 8 h |
| 7. UI Details | 8 h |
| 8. Admin | 4 h |
| 9. UX Polish | 4 h |
| 10. Verification | 4 h |
| **Total** | **~80 h = 8-10 أيام** |

---

## ⚠️ المخاطر والتخفيف

| المخاطرة | الاحتمال | التأثير | التخفيف |
|---|---|---|---|
| Supabase connection بطيء | عالي | متوسط | ~~استخدم direct connection~~ — تم التغيير لـ local PostgreSQL |
| Schema mismatch في Phase 6 | متوسط | عالي | تأكد من الأعمدة قبل seed (use information_schema) |
| Journal entries غير متوازنة | متوسط | عالي | اكتب function يولّد entries من operations، مع verification query |
| بيانات مكررة | منخفض | متوسط | استخدم ON CONFLICT DO NOTHING + UUIDs فريدة |
| Time overrun | عالي | منخفض | ابدأ بالـ essentials (المراحل 1-5) قبل الـ polish |

---

## 🎯 الـ Step الجاي

أنا جاهز أبدأ التنفيذ. الخطوات:

1. **تأكيد منك:** هل تبدأ بـ Phase 0 (Foundation) دلوقتي؟
2. لو عندك priorities مختلفة (مثلاً: Reports قبل البيانات) قولي

**لو موافق، أبدأ فوراً بـ:**
- Phase 0: Foundation check (15 min)
- Phase 1: Reference data seed (8 h)

→ بعد كل مرحلة، هاعرض لك النتيجة قبل ما أكمل.

---

**انتظر تأكيدك للبدء.** 🚀
