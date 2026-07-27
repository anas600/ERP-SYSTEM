# Plan: ERP-SYSTEM — Gap Analysis & Feature Recovery

**تاريخ:** 2026-07-26
**المشروعان:** `F:\minimaxDescktop` (القديم) و `F:\minimaxDescktop2\ERP-SYstem` (الجديد)
**النتيجة:** المشروعان متطابقان 100% في الكود. كل المشاكل اللي تشوفها سببها **نقص في البيانات + نقص في الـ UI wirings**، مش اختلاف بين المشروعين.

---

## 1. ملخص الوضع الفعلي (Runtime Audit)

اختبرت 39 endpoint من الـ backend بـ admin user + JWT. النتيجة:

### ✅ شغّال (24 endpoint)
| الفئة | Endpoints |
|---|---|
| **Master data** | users, roles, companies, departments, employees, items, warehouses, accounts (47), cost-centers, customers, vendors, projects, sales-invoices, bills, journal-entries, payroll-runs, attendance, leaves |
| **Reports** | ar-aging, top-customers, top-vendors, budget-vs-actual, inventory-valuation |

### ❌ مكسور (15 endpoint)
| Endpoint | السبب | الإصلاح |
|---|---|---|
| `GET /api/procurement/pos` | 500 | ناقص seed/خدمة مسجلة خطأ |
| `GET /api/procurement/grs` | 500 | ناقص seed/خدمة |
| `PUT /api/ar/receipts/{id}/post` | 500 | ناقص implementation |
| `GET /api/finance/reports/trial-balance` | 500 | SQL alias `TotalDebit/TotalCredit` bug معروف |
| `GET /api/finance/reports/balance-sheet` | 500 | ناقص بيانات (لا journal entries) |
| `GET /api/finance/reports/income-statement` | 500 | ناقص بيانات |
| `GET /api/finance/reports/cash-flow` | 500 | ناقص بيانات |
| `GET /api/finance/reports/general-ledger` | 500 | ناقص بيانات |
| `GET /api/finance/reports/vat` | 500 | ناقص بيانات |
| `GET /api/finance/reports/ap-aging` | 500 | ناقص بيانات |
| `GET /api/finance/reports/collections` | 500 | ناقص بيانات |
| `GET /api/finance/reports/cost-center-performance` | 500 | ناقص بيانات |
| `GET /api/ar/reports/sales-by-customer` | 500 | ناقص بيانات |
| `GET /api/ar/reports/sales-by-item` | 500 | ناقص بيانات |
| `GET /api/admin/posting-rules` | **404** | الـ endpoint مش متعرّف أصلاً |

---

## 2. الناقص فعلاً (Gap Matrix)

### A. الـ Frontend عنده 39 API method معرّف لكن مش مربوط بأي صفحة

| الـ API method | موجود في الـ backend | موجود page؟ | الحالة |
|---|---|---|---|
| `reportsApi.cashFlow` | ✅ | ❌ | محتاج page |
| `reportsApi.generalLedger` | ✅ | ❌ | محتاج page |
| `reportsApi.accountActivity` | ✅ | ❌ | محتاج page |
| `reportsApi.journalEntries` | ✅ | ❌ | محتاج page |
| `reportsApi.apAging` | ✅ | ❌ | محتاج page |
| `reportsApi.collections` | ✅ | ❌ | محتاج page |
| `reportsApi.costCenterPerformance` | ✅ | ❌ | محتاج page |
| `reportsApi.salesByItem` | ✅ | ❌ | محتاج page |
| `reportsApi.purchasesByVendor` | ✅ | ❌ | محتاج page |
| `reportsApi.topCustomers` | ✅ | ❌ | محتاج page |
| `reportsApi.topVendors` | ✅ | ❌ | محتاج page |
| `procurementApi.getPO` | ✅ | ❌ | محتاج page `/procurement/purchase-orders/[id]` |
| `procurementApi.getGR` | ✅ | ❌ | محتاج page `/procurement/goods-receipts/[id]` |
| `arApi.getCustomer` | ✅ | ❌ | محتاج page `/finance/customers/[id]` |
| `arApi.getInvoice` | ✅ | ❌ | محتاج page `/finance/sales-invoices/[id]` (موجود لكن ما يقرأ بيانات) |
| `arApi.updateCustomer` | ✅ | ❌ | محتاج edit page |
| `arApi.updateInvoice` | ✅ | ❌ | محتاج edit page |
| `arApi.cancelInvoice` | ✅ | ❌ | محتاج action |
| `arApi.postInvoice` | ✅ | ❌ | محتاج action |
| `arApi.createAccount` | ✅ | ❌ | محتاج action (موجود page /finance/accounts/new لكن ما يستدعي) |
| `arApi.createPayrollRun` | ✅ | ❌ | محتاج action |
| `arApi.processPayrollRun` | ✅ | ❌ | محتاج action |
| `arApi.postPayrollRun` | ✅ | ❌ | محتاج action |
| `arApi.getEos` | ✅ | ❌ | محتاج page |
| `arApi.getPayrollRun` | ✅ | ❌ | محتاج page |
| `arApi.getPayrollRunItems` | ✅ | ❌ | محتاج page |
| `arApi.getPayslip` | ✅ | ❌ | محتاج page |
| `arApi.listPayrollRuns` | ✅ | ❌ | محتاج page |
| `identityApi.createUser` | ✅ | ❌ | محتاج modal/page |
| `identityApi.updateUser` | ✅ | ❌ | محتاج edit |
| `identityApi.deactivateUser` | ✅ | ❌ | محتاج action |
| `identityApi.getUser` | ✅ | ❌ | محتاج detail |
| `authApi.changePassword` | ✅ | ❌ | محتاج UI |
| `authApi.forgotPassword` | ✅ | ✅ موجود `/login/forgot` | ✅ شغّال |
| `authApi.resetPasswordWithToken` | ✅ | ✅ موجود `/login/reset/[token]` | ✅ شغّال |
| `authApi.me` | ✅ | ❌ | مستخدم ضمني في useAuth |
| `authApi.getUserCompanies` | ✅ | ❌ | مستخدم ضمني |
| `arApi.getReceipt` | ✅ | ❌ | محتاج page `/finance/receipts/[id]` |
| `arApi.reverseReceipt` | ✅ | ❌ | محتاج action |

### B. الـ Sidebar عنده 16 رابط فقط من 77 صفحة

**موجود في الـ sidebar:**
- /dashboard, /finance/{accounts,aging-ar,customers,receipts,sales-invoices}
- /hr/{attendance,employees,leaves,payroll}
- /inventory/items
- /procurement/{bills,goods-receipts,purchase-orders,vendors}
- /projects

**موجود لكن مش في الـ sidebar (مخفي):**
- `/admin/users` + 11 admin pages
- `/admin/health`
- `/admin/companies`
- `/admin/audit`
- `/admin/item-categories/*` (3 pages)
- `/admin/notifications/*` (2 pages)
- `/admin/posting-rules/*` (3 pages)
- `/finance/cost-centers/*` (3 pages)
- `/finance/journal-entries/*` (3 pages)
- `/inventory/movements/*` (3 pages)
- `/inventory/reservations/*` (3 pages)
- `/inventory/stock-levels/*` (2 pages)
- `/notifications`
- كل الـ `/reports/*` (5+ pages)

### C. شاشات كاملة بس فاضية (مفيش data)

- **Customers**: 0 customer (الـ page بيفتح، الـ data array فاضي)
- **Vendors**: 0 vendor
- **Employees**: 0 employee
- **Items**: 0 item
- **Departments**: 0 department
- **Cost centers**: 0 cost center
- **Projects**: 0 project
- **Journal entries**: 0 (الـ reports كلها بتعتمد عليه)

→ كل الـ procurement/sales/finance reports مش شغّالة بسبب عدم وجود بيانات.

---

## 3. الإصلاح المقترح (Prioritized)

### المرحلة 1: بيانات أساسية (يوم واحد) — **بدونها مش هتشوف حاجة في الـ UI**
- [ ] **seed الـ reference data** من `docs/seed-one-year-data.sql` (16 customer, 12 vendor, 21 item, 4 warehouses, 14 cost center, 4 projects, 6 departments, 5 categories, 6 UoM)
- [ ] **تحويل الـ seed SQL** من `tenant_id` لـ `company_id` (multi-company)
- [ ] **تطبيق الـ seed** على `ec6b98ee-...` holding
- [ ] **تأكيد أن الـ UI تظهر الأرقام**: Customers list (16) + Vendors (12) + Items (21)

### المرحلة 2: إصلاح الـ 15 endpoint مكسور (يوم واحد)
- [ ] **إصلاح Trial Balance** (alias `Debit/Credit` بدل `TotalDebit/TotalCredit`)
- [ ] **إصلاح POST/PUT receipts** (DI missing or schema mismatch)
- [ ] **إصلاح purchase-orders/goods-receipts** (نفس السبب غالباً)
- [ ] **إضافة endpoint** لـ `/api/admin/posting-rules` (404 → لازم نضيف controller أو نغير path)
- [ ] **اختبار 15 reports** بعد ما يكون في journal entries

### المرحلة 3: شاشات reports المفقودة (يومين) — **11 API method معرّف بدون page**
- [ ] `/reports/financial/cash-flow` — Cash Flow Statement
- [ ] `/reports/financial/general-ledger` — General Ledger (per account)
- [ ] `/reports/financial/account-activity` — Account Activity
- [ ] `/reports/financial/journal-entries` — Journal Entries List
- [ ] `/reports/financial/ap-aging` — AP Aging (frontend فقط؛ الـ backend شغّال)
- [ ] `/reports/financial/collections` — Collections Report
- [ ] `/reports/financial/cost-center-performance` — Cost Center Performance
- [ ] `/reports/sales/sales-by-item` — Sales by Item
- [ ] `/reports/sales/top-customers` — Top Customers
- [ ] `/reports/procurement/purchases-by-vendor` — Purchases by Vendor
- [ ] `/reports/procurement/top-vendors` — Top Vendors
- [ ] إضافة كلهم في الـ sidebar تحت Reports menu

### المرحلة 4: شاشات التفاصيل (يومين) — **8 detail/edit pages**
- [ ] `/procurement/purchase-orders/[id]` (موجود ملف لكن ما يقرأ `getPO`)
- [ ] `/procurement/goods-receipts/[id]` (موجود ملف لكن ما يقرأ `getGR`)
- [ ] `/finance/customers/[id]` (غير موجود)
- [ ] `/finance/sales-invoices/[id]` (موجود لكن ما يقرأ `getInvoice`)
- [ ] `/finance/receipts/[id]` (غير موجود)
- [ ] `/hr/payroll/[id]` (موجود لكن ما يستدعي `getPayrollRun`)
- [ ] `/hr/payroll/[id]/payslip/[empId]` (موجود لكن ما يستدعي `getPayslip`)
- [ ] `/finance/accounts/[id]/edit` (موجود لكن ما يقرأ الحساب)

### المرحلة 5: شاشات Admin و Identity (يوم)
- [ ] `/admin/users` مفعل + create/edit modal
- [ ] `/admin/roles` (list فقط)
- [ ] `/admin/companies` (list + tree)
- [ ] `/admin/audit` (logs)
- [ ] `/admin/health` (system health)
- [ ] `/admin/item-categories/*` (CRUD)
- [ ] `/admin/posting-rules/*` (CRUD)
- [ ] `/admin/notifications/*` (CRUD)
- [ ] Profile page + change password

### المرحلة 6: UX Polish (يوم)
- [ ] إضافة 50+ رابط للـ sidebar (Admin menu + Reports submenu)
- [ ] Drill-down: من تقرير → تفصيل (مثلاً من Trial Balance → General Ledger للحساب)
- [ ] Toasts على كل action
- [ ] Loading skeletons
- [ ] Empty states مع illustrations
- [ ] Error boundaries
- [ ] Bulk approve لـ leaves

### المرحلة 7: HR & Payroll بقية (يوم)
- [ ] `/hr/payroll/new` (موجود لكن ما يعمل createPayrollRun)
- [ ] Process Payroll action
- [ ] Post Payroll action
- [ ] EOS calculation
- [ ] Payslip PDF export

---

## 4. خطة التنفيذ المقترحة (مرحلي)

### Sprint 1 — الأساس (يومين)
1. تطبيق كل الـ seed data (المرحلة 1)
2. إصلاح الـ 15 endpoint مكسور (المرحلة 2)

→ **النتيجة:** الـ UI هتعرض بيانات حقيقية، الـ 11 reports اللي شغّالة (top-customers, top-vendors, AR aging, inventory-valuation) هتظهر أرقام.

### Sprint 2 — Reports (يومين)
3. إضافة 11 reports page (المرحلة 3)
4. إضافتهم للـ sidebar

→ **النتيجة:** التقارير الـ 20 اللي في الـ SPEC كلها متاحة في الـ UI.

### Sprint 3 — Details (يومين)
5. شاشات التفاصيل والـ edit (المرحلة 4)
6. شاشات Admin (المرحلة 5)

→ **النتيجة:** يمكنك تعديل وحذف كل entity من الـ UI.

### Sprint 4 — Polish (يوم)
7. UX improvements (المرحلة 6+7)

→ **النتيجة:** النظام جاهز للـ demo.

---

## 5. القرار المطلوب منك

السؤال الرئيسي: **من فين تبي نبدأ؟**

| الخيار | الوصف | الوقت |
|---|---|---|
| **A. الأساس أولاً** (موصى به) | seed data + إصلاح endpoints → تشوف أرقام حقيقية | 1-2 يوم |
| **B. UI Reports** | إضافة الـ 11 reports page (بصرف النظر عن البيانات) | 1-2 يوم |
| **C. كله دفعة واحدة** | 7 مراحل في أسبوع | 5-7 أيام |

أنا أميل بقوة للخيار A كبداية. بدون بيانات حقيقية، أي UI work هيكون "فاضي" وممكن يعطي انطباع خاطئ. والـ 15 endpoint مكسور بتاع الـ reports هي السبب الرئيسي في إن التقارير "مش شغّالة" في الـ UI.

---

## 6. الخطوة التالية

بعد ما تختار الخيار:
1. لو A: هابدأ بـ seed data conversion + تطبيقه + إصلاح الـ 15 endpoint
2. لو B: هابدأ بـ 11 report pages (templates من الـ 7 الموجودين)
3. لو C: هابدأ بـ A ثم B ثم C

**انتظر ردك عشان أبدأ التنفيذ.**
