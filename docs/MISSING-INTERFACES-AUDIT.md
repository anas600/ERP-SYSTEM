# 🔍 Audit شامل: كل الواجهات المفقودة في ERP-SYSTEM

**المشروع:** `F:\minimaxDescktop2\ERP-SYstem`
**التاريخ:** 2026-07-26
**المنهجية:** مسح شامل للـ frontend pages + AppShell sidebar + api.ts + backend endpoints

---

## 📊 الملخص التنفيذي

| المؤشر | العدد |
|---|---|
| ✅ Pages موجودة فعلياً | 77 صفحة |
| 📋 روابط في الـ AppShell sidebar | 16 رابط |
| 🔗 روابط داخلية مستخدمة (href) | 45 رابط |
| 🔧 API methods معرّفة في api.ts | 81 method |
| 📞 API methods مستدعاة من pages | 40 method |
| ❌ **API methods معرّفة لكن بدون page** | **39 method** |
| ❌ **Pages موجودة لكن مخفية (مش في nav)** | **60+ page** |
| 🌐 Backend endpoints | 188 endpoint |

**الخلاصة الكبيرة:** فيه **عشرات الواجهات المفقودة** مقسّمة على 3 أنواع:
1. **API موجود بدون UI** (39 method)
2. **Pages موجودة لكن مش في الـ sidebar** (60+ page)
3. **Pages مفقودة بالكامل** (التفاصيل أدناه)

---

## 🚨 الفئة 1: واجهات API معرّفة بدون UI (39 method)

### Reports (11 API method مفقود page)

| # | الـ API method | الـ URL | الـ Page المطلوب | الحالة |
|---|---|---|---|---|
| 1 | `reportsApi.cashFlow` | `/api/finance/reports/cash-flow` | `/reports/financial/cash-flow` | ❌ مفقود |
| 2 | `reportsApi.generalLedger` | `/api/finance/reports/general-ledger` | `/reports/financial/general-ledger` | ❌ مفقود |
| 3 | `reportsApi.accountActivity` | `/api/finance/reports/account-activity` | `/reports/financial/account-activity` | ❌ مفقود |
| 4 | `reportsApi.journalEntries` | `/api/finance/reports/journal-entries` | `/reports/financial/journal-entries` | ❌ مفقود |
| 5 | `reportsApi.apAging` | `/api/finance/reports/ap-aging` | `/reports/financial/ap-aging` | ❌ مفقود |
| 6 | `reportsApi.collections` | `/api/finance/reports/collections` | `/reports/financial/collections` | ❌ مفقود |
| 7 | `reportsApi.costCenterPerformance` | `/api/finance/reports/cost-center-performance` | `/reports/financial/cost-center-performance` | ❌ مفقود |
| 8 | `reportsApi.salesByItem` | `/api/ar/reports/sales-by-item` | `/reports/sales/sales-by-item` | ❌ مفقود |
| 9 | `reportsApi.topCustomers` | `/api/ar/reports/top-customers` | `/reports/sales/top-customers` | ❌ مفقود |
| 10 | `reportsApi.purchasesByVendor` | `/api/procurement/reports/purchases-by-vendor` | `/reports/procurement/purchases-by-vendor` | ❌ مفقود |
| 11 | `reportsApi.topVendors` | `/api/procurement/reports/top-vendors` | `/reports/procurement/top-vendors` | ❌ مفقود |

### Detail / Get APIs (8 مفقود page)

| # | الـ API method | الـ Page المطلوب | الحالة |
|---|---|---|---|
| 12 | `arApi.getCustomer` | `/finance/customers/[id]` | ❌ مفقود |
| 13 | `arApi.getInvoice` | `/finance/sales-invoices/[id]` | ⚠️ موجود لكن ما يستدعي getInvoice |
| 14 | `arApi.getReceipt` | `/finance/receipts/[id]` | ❌ مفقود |
| 15 | `procurementApi.getPO` | `/procurement/purchase-orders/[id]` | ⚠️ موجود لكن ما يستدعي getPO |
| 16 | `procurementApi.getGR` | `/procurement/goods-receipts/[id]` | ⚠️ موجود لكن ما يستدعي getGR |
| 17 | `procurementApi.getBill` | `/procurement/bills/[id]` | ❌ مفقود (لا page) |
| 18 | `arApi.getPayslip` | `/hr/payroll/[id]/payslip/[empId]` | ⚠️ موجود لكن ما يستدعي getPayslip |
| 19 | `hrApi.getPayrollRun` | `/hr/payroll/[id]` | ⚠️ موجود لكن ما يستدعي getPayrollRun |
| 20 | `hrApi.getPayrollRunItems` | `/hr/payroll/[id]` (نفس) | ⚠️ نفس المشكلة |
| 21 | `hrApi.getEos` | `/hr/payroll/eos/[empId]` | ❌ مفقود (لا page) |

### Update APIs (4 مفقود action)

| # | الـ API method | الـ Page المطلوب | الحالة |
|---|---|---|---|
| 22 | `arApi.updateCustomer` | `/finance/customers/[id]/edit` | ❌ مفقود |
| 23 | `arApi.updateInvoice` | `/finance/sales-invoices/[id]/edit` | ❌ مفقود |
| 24 | `arApi.cancelInvoice` | action في detail page | ❌ مفقود |
| 25 | `arApi.postInvoice` | action في detail page | ❌ مفقود |

### Create APIs (4 مفقود)

| # | الـ API method | الـ Page المطلوب | الحالة |
|---|---|---|---|
| 26 | `arApi.createAccount` | `/finance/accounts/new` | ⚠️ page موجود لكن ما يستدعي createAccount |
| 27 | `hrApi.createPayrollRun` | `/hr/payroll/new` | ⚠️ page موجود لكن ما يستدعي createPayrollRun |
| 28 | `hrApi.processPayrollRun` | action في detail | ❌ مفقود |
| 29 | `hrApi.postPayrollRun` | action في detail | ❌ مفقود |
| 30 | `identityApi.createUser` | modal في `/admin/users` | ⚠️ page موجود لكن ما يستدعي createUser |
| 31 | `identityApi.updateUser` | modal في `/admin/users` | ❌ مفقود |
| 32 | `identityApi.deactivateUser` | action في `/admin/users` | ❌ مفقود |
| 33 | `arApi.deactivateCustomer` | action في detail | ❌ مفقود |

### Auth APIs (3 مفقود UI)

| # | الـ API method | الـ Page المطلوب | الحالة |
|---|---|---|---|
| 34 | `authApi.me` | (مستخدم ضمني في useAuth) | ⚠️ مستخدم ضمنياً |
| 35 | `authApi.getUserCompanies` | (مستخدم ضمني) | ⚠️ مستخدم ضمنياً |
| 36 | `authApi.changePassword` | `/profile` أو `/settings/change-password` | ❌ مفقود (لا page) |
| 37 | `authApi.forgotPassword` | `/login/forgot` | ✅ موجود |
| 38 | `authApi.resetPasswordWithToken` | `/login/reset/[token]` | ✅ موجود |

### Other (1)

| # | الـ API method | الـ Page المطلوب | الحالة |
|---|---|---|---|
| 39 | `arApi.listPayrollRuns` | (مدمج في `/hr/payroll`) | ⚠️ ضمني |

---

## 🚨 الفئة 2: صفحات موجودة لكن مش في الـ Sidebar (60+ page)

### Admin (12 page مخفي)

| الـ Page | الوصف |
|---|---|
| `/admin/users` | إدارة المستخدمين + CRUD + reset password |
| `/admin/companies` | إدارة الشركات (Holding + Subsidiaries) |
| `/admin/health` | System health dashboard |
| `/admin/audit` | Audit log viewer |
| `/admin/item-categories` | فئات الأصناف CRUD |
| `/admin/item-categories/new` | إضافة فئة |
| `/admin/item-categories/[id]/edit` | تعديل فئة |
| `/admin/posting-rules` | Posting rules CRUD |
| `/admin/posting-rules/new` | إضافة rule |
| `/admin/posting-rules/[id]` | تفاصيل rule |
| `/admin/notifications` | قائمة الإشعارات |
| `/admin/notifications/[id]` | تفاصيل إشعار |

### Finance (15 page مخفي)

| الـ Page | الوصف |
|---|---|
| `/finance/cost-centers` | Cost centers list |
| `/finance/cost-centers/new` | إضافة cost center |
| `/finance/cost-centers/[id]/edit` | تعديل cost center |
| `/finance/journal-entries` | Journal entries list |
| `/finance/journal-entries/new` | إضافة journal entry |
| `/finance/journal-entries/[id]` | تفاصيل journal entry |
| `/finance/sales-invoices/[id]` | تفاصيل فاتورة (موجود لكن مكسور) |
| `/finance/accounts/new` | إضافة حساب (موجود لكن ما يستدعي createAccount) |
| `/finance/accounts/[id]/edit` | تعديل حساب (موجود لكن مكسور) |

### Procurement (5 page مخفي)

| الـ Page | الوصف |
|---|---|
| `/procurement/purchase-orders/new` | إنشاء PO |
| `/procurement/goods-receipts/new` | إنشاء GR |
| `/procurement/goods-receipts/[id]` | تفاصيل GR (موجود لكن مكسور) |
| `/procurement/bills/new` | إنشاء bill |

### HR (6 page مخفي)

| الـ Page | الوصف |
|---|---|
| `/hr/employees/new` | إضافة موظف |
| `/hr/leaves/new` | طلب إجازة |
| `/hr/payroll/new` | إنشاء payroll run (موجود لكن مكسور) |
| `/hr/payroll/[id]` | تفاصيل payroll (موجود لكن مكسور) |
| `/hr/payroll/[id]/payslip/[empId]` | Payslip (موجود لكن مكسور) |

### Inventory (8 page مخفي)

| الـ Page | الوصف |
|---|---|
| `/inventory/items/new` | إضافة صنف |
| `/inventory/items/[id]/edit` | تعديل صنف |
| `/inventory/movements` | Stock movements list |
| `/inventory/movements/new` | إضافة movement |
| `/inventory/movements/[id]` | تفاصيل movement |
| `/inventory/reservations` | Stock reservations |
| `/inventory/reservations/new` | إضافة reservation |
| `/inventory/reservations/[id]` | تفاصيل reservation |
| `/inventory/stock-levels` | Stock levels list |
| `/inventory/stock-levels/[id]` | Stock level details |

### Projects (3 page مخفي)

| الـ Page | الوصف |
|---|---|
| `/projects/new` | إنشاء مشروع |
| `/projects/[id]/edit` | تعديل مشروع |

### Reports (8 page موجود لكن الـ Sidebar ما يوصله)

| الـ Page | الـ Sidebar | الحالة |
|---|---|---|
| `/reports/financial` (category) | ❌ | موجود لكن بدون nav |
| `/reports/inventory` (category) | ❌ | موجود لكن بدون nav |
| `/reports/projects` (category) | ❌ | موجود لكن بدون nav |
| `/reports/sales` (category) | ❌ | موجود لكن بدون nav |

### Notifications (1 page)

| الـ Page | الوصف |
|---|---|
| `/notifications` | User notifications (موجود لكن ما في nav له) |

---

## 🚨 الفئة 3: واجهات مفقودة بالكامل (Pages لا بد من إنشائها)

### Profile & Settings (مفقود كلياً)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/profile` | Profile page (full name, email, roles, current company) | `authApi.me` |
| `/profile/change-password` | Change password form | `authApi.changePassword` |
| `/settings` | User preferences (language, theme) | لا API (frontend only) |
| `/admin/settings` | System settings (base currency, fiscal year) | جديد |

### User Management (CRUD UI)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/admin/users/new` | Create user form | `identityApi.createUser` |
| `/admin/users/[id]` | User details + edit | `identityApi.getUser` |
| `/admin/users/[id]/edit` | Edit user form | `identityApi.updateUser` |
| `/admin/users/[id]/reset-password` | Admin reset password modal | `PUT /api/identity/users/{id}/password` |
| `/admin/roles` | Roles list + permissions | `identityApi.listRoles` |
| `/admin/roles/[id]` | Role details + users | جديد |

### Companies (CRUD)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/admin/companies/new` | Create subsidiary | `POST /api/companies/subsidiary` |
| `/admin/companies/[id]` | Company details | `GET /api/companies/{id}` |
| `/admin/companies/[id]/edit` | Edit company | جديد |

### Customers (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/finance/customers/[id]` | Customer details + balance + invoices | `arApi.getCustomer` |
| `/finance/customers/[id]/edit` | Edit customer | `arApi.updateCustomer` |
| `/finance/customers/[id]/invoices` | Customer's invoices | listInvoices (filter by customer) |
| `/finance/customers/[id]/transactions` | Customer transaction history | جديد |

### Vendors (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/procurement/vendors/[id]` | Vendor details + balance + bills | جديد |
| `/procurement/vendors/[id]/edit` | Edit vendor | جديد |
| `/procurement/vendors/[id]/bills` | Vendor's bills | listBills (filter) |
| `/procurement/vendors/[id]/transactions` | Vendor transaction history | جديد |

### Items (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/inventory/items/[id]` | Item details + stock + movements | `GET /api/inventory/items/{id}` |
| `/inventory/items/[id]/movements` | Item's stock movements | list movements (filter) |
| `/inventory/items/[id]/transactions` | Item transaction history | جديد |

### Sales Invoices (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/finance/sales-invoices/[id]` | Invoice details + lines + payments | `arApi.getInvoice` |
| `/finance/sales-invoices/[id]/edit` | Edit draft invoice | `arApi.updateInvoice` |
| `/finance/sales-invoices/[id]/payments` | Invoice's payments | list payments (filter) |

### Receipts (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/finance/receipts/[id]` | Receipt details + allocations | `arApi.getReceipt` |
| `/finance/receipts/new` (موجود لكن مكسور) | Create receipt | `arApi.createReceipt` |

### Purchase Orders (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/procurement/purchase-orders/[id]` | PO details + lines + approval | `procurementApi.getPO` |
| `/procurement/purchase-orders/[id]/approve` | Approve PO action | `PUT /api/procurement/pos/{id}/approve` |
| `/procurement/purchase-orders/[id]/send` | Send to vendor action | `PUT /api/procurement/pos/{id}/send` |

### Goods Receipts (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/procurement/goods-receipts/[id]` | GR details + lines + receive action | `procurementApi.getGR` |
| `/procurement/goods-receipts/[id]/receive` | Mark as received action | `PUT /api/procurement/grs/{id}/receive` |

### Vendor Bills (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/procurement/bills/[id]` | Bill details + lines + post | جديد |
| `/procurement/bills/[id]/edit` | Edit bill | جديد |
| `/procurement/bills/[id]/post` | Post bill action | `PUT /api/procurement/bills/{id}/post` |
| `/procurement/bills/[id]/payments` | Bill's payments | list payments |

### Journal Entries (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/finance/journal-entries/[id]` | (موجود) لكن ما يستدعي API | `GET /api/finance/journal-entries/{id}` |
| `/finance/journal-entries/[id]/post` | Post JE action | جديد |

### Accounts (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/finance/accounts/[id]` | Account details + transactions | `GET /api/finance/accounts/{id}` |
| `/finance/accounts/[id]/transactions` | Account's GL entries | `reportsApi.generalLedger` |
| `/finance/accounts/new` (موجود لكن مكسور) | Create account | `arApi.createAccount` |

### Employees (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/hr/employees/[id]` | Employee details | `GET /api/hr/employees/{id}` |
| `/hr/employees/[id]/edit` | Edit employee | جديد |
| `/hr/employees/[id]/salary` | Salary history | جديد |
| `/hr/employees/[id]/attendance` | Attendance history | `hrApi.listAttendance` |
| `/hr/employees/[id]/leaves` | Leave history | `hrApi.listLeaves` |
| `/hr/employees/[id]/payslips` | Payslip history | `reportsApi.getPayslip` |

### Departments (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/hr/departments/new` | Create department | جديد |
| `/hr/departments/[id]` | Department details + employees | `GET /api/hr/departments/{id}` |
| `/hr/departments/[id]/edit` | Edit department | جديد |

### Leaves (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/hr/leaves/[id]` | Leave details + history | `GET /api/hr/leaves/{id}` |
| `/hr/leaves/[id]/edit` | Edit leave (only if pending) | جديد |
| `/hr/leaves/calendar` | Calendar view of team leaves | جديد |

### Payroll (Detail/Edit)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/hr/payroll/[id]` (موجود لكن مكسور) | Payroll run details | `hrApi.getPayrollRun` |
| `/hr/payroll/[id]/items` (موجود لكن مكسور) | Payroll items list | `hrApi.getPayrollRunItems` |
| `/hr/payroll/[id]/process` | Process payroll action | `hrApi.processPayrollRun` |
| `/hr/payroll/[id]/post` | Post payroll action | `hrApi.postPayrollRun` |
| `/hr/payroll/[id]/payslip/[empId]` (موجود لكن مكسور) | Payslip details | `hrApi.getPayslip` |
| `/hr/payroll/eos/[empId]` | EOS calculation | `hrApi.getEos` |

### Projects (Detail)

| الـ Page المقترح | الوصف | API المطلوب |
|---|---|---|
| `/projects/[id]` | Project details + tasks + budget | `GET /api/projects/{id}` |
| `/projects/[id]/tasks` | Project tasks | `GET /api/projects/{id}/tasks` |
| `/projects/[id]/budget` | Budget details | `GET /api/projects/{id}/budget` |
| `/projects/[id]/pnl` | Project P&L | `GET /api/reports/projects/{id}/pnl` |
| `/projects/[id]/assignments` | Resource assignments | `GET /api/projects/{id}/assignments` |

### Reports (13 page مفقود)

| الـ Page | الوصف | API |
|---|---|---|
| `/reports/financial/cash-flow` | Cash flow statement | `reportsApi.cashFlow` |
| `/reports/financial/general-ledger` | General ledger (per account) | `reportsApi.generalLedger` |
| `/reports/financial/account-activity` | Account activity report | `reportsApi.accountActivity` |
| `/reports/financial/journal-entries` | Journal entries list | `reportsApi.journalEntries` |
| `/reports/financial/ap-aging` | AP aging | `reportsApi.apAging` |
| `/reports/financial/collections` | Customer collections | `reportsApi.collections` |
| `/reports/financial/cost-center-performance` | Cost center performance | `reportsApi.costCenterPerformance` |
| `/reports/sales/sales-by-item` | Sales by item | `reportsApi.salesByItem` |
| `/reports/sales/top-customers` | Top customers | `reportsApi.topCustomers` |
| `/reports/procurement/purchases-by-vendor` | Purchases by vendor | `reportsApi.purchasesByVendor` |
| `/reports/procurement/top-vendors` | Top vendors | `reportsApi.topVendors` |
| `/reports/inventory/low-stock` | Low stock alerts | جديد |
| `/reports/inventory/stock-aging` | Stock aging | جديد |

### Admin Reports (مفقود)

| الـ Page | الوصف | API المطلوب |
|---|---|---|
| `/admin/audit` (موجود لكن ما في nav) | Audit log viewer | `GET /api/audit` |
| `/admin/posting-rules/[id]/edit` (مفقود) | Edit posting rule | جديد |
| `/admin/posting-rules/[id]/delete` (مفقود) | Delete posting rule | جديد |
| `/admin/posting-rules/[id]/test` (مفقود) | Test posting rule | جديد |

### Other (Top Bar UI)

| الـ Component | الوصف | الحالة |
|---|---|---|
| UserMenu (top-right) | Dropdown with: Profile, Change Password, Logout | ❌ مفقود |
| Logout button | في UserMenu | ❌ مفقود |
| Notifications bell | Top bar with unread count | ❌ مفقود |
| Search bar | Global search | ❌ مفقود |
| Breadcrumbs | Current location | ❌ مفقود |

---

## 🚨 الفئة 4: Backend Endpoints بدون Frontend

### Admin Controllers (الـ backend عند endpoints مش موجودة frontend)

| الـ Endpoint | الوصف | Page موجود؟ |
|---|---|---|
| `GET /api/admin/posting-rules` | List posting rules | ❌ 404 |
| `GET /api/finance/posting-rules` | List posting rules (alt path) | ❌ مفقود |
| `GET /api/audit` | Audit log | ❌ مفقود (page موجود لكن ما يستدعيه) |
| `GET /api/health/full` | Full health report | ❌ مفقود (page موجود لكن ما يستدعيه) |
| `GET /api/inventory/categories` | Item categories | ⚠️ admin page موجود |
| `GET /api/inventory/uom` | Units of measure | ❌ مفقود |
| `GET /api/inventory/warehouses` | Warehouses | ❌ مفقود (page مفقود كلياً) |
| `GET /api/inventory/movements` | Stock movements list | ❌ مفقود (page موجود لكن ما يستدعيه) |
| `GET /api/inventory/low-stock` | Low stock items | ❌ مفقود |
| `GET /api/inventory/aging` | Stock aging | ❌ مفقود |
| `GET /api/inventory/levels` | Stock levels | ❌ مفقود (page موجود لكن ما يستدعيه) |
| `GET /api/inventory/reservations` | Stock reservations | ❌ مفقود (page موجود لكن ما يستدعيه) |
| `GET /api/resources` | Resources (HR resources) | ❌ مفقود (page مفقود) |
| `GET /api/resources/{id}/assignments` | Resource assignments | ❌ مفقود |
| `GET /api/projects/{id}/tasks` | Project tasks | ❌ مفقود |
| `GET /api/projects/{id}/budget` | Project budget | ❌ مفقود |
| `GET /api/projects/{id}/pnl` | Project P&L | ❌ مفقود |
| `GET /api/projects/summary` | All projects summary | ❌ مفقود |
| `GET /api/payments` | Payments list | ❌ مفقود (لا page) |
| `GET /api/companies/tree` | Companies tree | ❌ مفقود (admin companies page موجود لكن ما يستدعيه) |
| `GET /api/events/outbox` | Outbox events | ❌ مفقود |
| `GET /api/events/processed` | Processed events | ❌ مفقود |
| `GET /api/notifications/unread` | Unread count | ❌ مفقود |

---

## 🚨 الفئة 5: Pages موجودة لكن مكسورة (لا تستدعي API صح)

| الـ Page | المشكلة | ما يحتاج إصلاح |
|---|---|---|
| `/finance/sales-invoices/[id]` | ما يستدعي `arApi.getInvoice` | استدعاء API وعرض البيانات |
| `/procurement/purchase-orders/[id]` | ما يستدعي `procurementApi.getPO` | استدعاء API وعرض البيانات |
| `/procurement/goods-receipts/[id]` | ما يستدعي `procurementApi.getGR` | استدعاء API وعرض البيانات |
| `/hr/payroll/[id]` | ما يستدعي `hrApi.getPayrollRun` | استدعاء API وعرض البيانات |
| `/hr/payroll/[id]/payslip/[empId]` | ما يستدعي `hrApi.getPayslip` | استدعاء API وعرض البيانات |
| `/hr/payroll/new` | ما يستدعي `hrApi.createPayrollRun` | استدعاء API + action submit |
| `/finance/accounts/new` | ما يستدعي `arApi.createAccount` | استدعاء API + action submit |
| `/finance/accounts/[id]/edit` | ما يقرأ الحساب | استدعاء API لقراءة + تعديل |
| `/admin/users` | ما يستدعي `identityApi.createUser/updateUser/deactivateUser` | استدعاء API + modals |
| `/admin/notifications` | ما يستدعي notifications API | استدعاء API وعرض البيانات |
| `/finance/journal-entries/[id]` | ما يقرأ journal entry | استدعاء API لقراءة |

---

## 🚨 الفئة 6: Top Bar / Navigation UI مفقود

### Top Bar Components (مفقودة كلياً)

| الـ Component | الـ File المقترح | الـ API |
|---|---|---|
| UserMenu dropdown | `src/frontend/components/layout/UserMenu.tsx` | `authApi.me` + logout |
| Notifications bell | `src/frontend/components/layout/NotificationsBell.tsx` | `GET /api/notifications/unread` |
| Search bar (global) | `src/frontend/components/layout/GlobalSearch.tsx` | جديد |
| Breadcrumbs | `src/frontend/components/layout/Breadcrumbs.tsx` | (frontend only, from usePathname) |
| Logout button | (داخل UserMenu) | `authApi.logout` |

### Sidebar Groups (مفقودة من NAV_GROUPS)

| الـ Group | المفقود |
|---|---|
| Admin | كل admin pages (12 link) |
| Finance | cost-centers, journal-entries, AP aging, payments, post/reverse actions |
| Inventory | stock-movements, stock-levels, reservations, warehouses, UoMs, categories |
| Procurement | PO/GR/Bill details + reports |
| HR | departments, payroll details, EOS, leaves calendar |
| Projects | project details, tasks, assignments, budget |
| Reports | 13 تقرير مفقود (current sidebar ما يوصلها) |
| Sales | sales details, receipts, customers details |

---

## 🎯 التوصية

**الترتيب حسب الأولوية:**

### P0 (ضروري للتسليم) — يوم
- إصلاح الـ 11 page مكسورة في الفئة 5
- إضافة 13 Reports pages (الفئة 1)
- إضافة Top Bar (UserMenu + Logout + Notifications)
- إضافة Profile + Change Password
- إصلاح الـ backend DI المكسور (general-ledger, posting-rules)

### P1 (مهم للـ UX) — يوم
- إضافة 20+ Detail/Edit pages (الفئة 3)
- إضافة 12 Admin pages للـ sidebar
- إضافة 6 Inventory pages للـ sidebar
- إضافة 5 Procurement pages للـ sidebar
- إضافة 6 HR pages للـ sidebar

### P2 (تحسين) — يوم
- إضافة 3 Projects pages للـ sidebar
- إضافة Breadcrumbs
- Global Search
- Bulk actions

---

## 📌 الخطوة التالية

أنا جاهز أبدأ. اقترح:
1. **ابدأ بـ P0** (ضروري) عشان نسلم المشروع شغّال
2. **P0 + P1** عشان نخلّص كل اللي ضروري + UX

**أنتظر تأكيدك على الترتيب.**
