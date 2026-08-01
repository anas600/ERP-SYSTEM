# Workflow: Customer (العملاء)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 19 (Client Demo Sprint).
> **Backend module:** `Modules/AccountsReceivable`.

---

## 1. Business Purpose (الغرض التجاري)

The **Customer** function manages every party the company sells to on credit. Each customer record carries contact details, tax information, credit limit, and payment terms. The customer is the anchor for:

- **Sales Invoices** (الفواتير) — what the customer owes.
- **Receipts** (سندات القبض) — what the customer paid.
- **AR Aging** (أعمار الذمم) — overdue buckets per customer.
- **Reports** — Sales by Customer, Top Customers, Customer Statement.

Without a customer record, you cannot issue a credit invoice or track receivables.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can deactivate? | Can view statement? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Sales** (مبيعات) | ✅ | ✅ | ✅ | ❌ | ✅ (read-only) |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ✅ |

**Why roles matter:** Only Admin and Accountant can deactivate a customer (deactivation is a soft delete that hides the customer from new invoices but keeps history). Sales can create and edit but not deactivate, so a salesperson cannot accidentally remove a record referenced by posted invoices.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **العملاء** (`/finance/customers`) from the sidebar.
2. The page loads all customers (active and inactive) in a sortable table.
3. Use the search box to filter by name, code, or tax ID.
4. Each row shows: code, name (Arabic + English), tax ID, contact info, credit limit, payment terms, status.

### 3.2 Create a new customer
1. Click **عميل جديد** (top-right button).
2. Fill the form:
   - **Code** (required, unique, e.g. `CUST-001`) — auto-suggested but editable.
   - **Name** (required) — Arabic company name.
   - **Name (English)** (optional) — for invoices in English.
   - **Tax ID** (optional) — Libyan tax number.
   - **Email / Phone / Address** (optional but recommended).
   - **Credit Limit** (optional, LYD) — max outstanding balance.
   - **Payment Terms** (default 30 days) — net days for invoices.
3. Click **حفظ** (Save).
4. The new customer appears in the list and is selectable for new invoices.

### 3.3 View a customer
1. Click on a row in the list (or open `/finance/customers/{id}`).
2. The view page shows:
   - All fields read-only.
   - A list of **recent invoices** for this customer.
   - A list of **recent receipts**.
   - **Outstanding balance** (computed).
3. From the view page you can:
   - Click **تعديل** to open the edit form.
   - Click **رجوع** to go back to the list.
   - Click **كشف حساب** to see the full statement (all transactions).

### 3.4 Edit a customer
1. Open the customer's view page, then click **تعديل**.
2. The same form as create, pre-filled.
3. Edit any field except **Code** (code is immutable — it's the external reference).
4. Click **حفظ**.

### 3.5 Deactivate a customer
1. Open the customer's edit page.
2. Uncheck **نشط** (Active) and save.
3. The customer is hidden from new invoice dropdowns but remains visible in historical reports.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/ar/customers`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/ar/customers` | List all customers (active + inactive) | `CustomerResponse[]` |
| `GET` | `/api/ar/customers/{id}` | Get one customer | `CustomerResponse` |
| `POST` | `/api/ar/customers` | Create a customer | `CustomerResponse` (201) |
| `PUT` | `/api/ar/customers/{id}` | Update a customer (incl. active flag) | `CustomerResponse` |
| `DELETE` | `/api/ar/customers/{id}` | Soft-delete (deactivate) | `204 No Content` |

### Request body — `CreateCustomerRequest`
```json
{
  "code": "CUST-001",
  "name": "شركة الفجر للتجارة",
  "nameEn": "Alfajr Trading Co.",
  "taxId": "123456789",
  "email": "info@alfajr.ly",
  "phone": "+218 91 1234567",
  "address": "شارع الجمهورية، طرابلس",
  "creditLimit": 50000.00,
  "paymentTermsDays": 30
}
```

### Response — `CustomerResponse`
```json
{
  "id": "guid",
  "companyId": "guid",
  "code": "CUST-001",
  "name": "شركة الفجر للتجارة",
  "nameEn": "Alfajr Trading Co.",
  "taxId": "123456789",
  "email": "info@alfajr.ly",
  "phone": "+218 91 1234567",
  "address": "شارع الجمهورية، طرابلس",
  "creditLimit": 50000.00,
  "paymentTermsDays": 30,
  "isActive": true
}
```

### Error codes
- `400 Bad Request` — missing required field, duplicate code, invalid email.
- `404 Not Found` — customer ID does not exist.
- `409 Conflict` — code already used by another customer.

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/finance/customers` | `app/(authenticated)/finance/customers/page.tsx` | List + search + filter |
| `/finance/customers/new` | `app/(authenticated)/finance/customers/new/page.tsx` | Create form |
| `/finance/customers/{id}` | `app/(authenticated)/finance/customers/[id]/page.tsx` | View (read-only) |
| `/finance/customers/{id}/edit` | `app/(authenticated)/finance/customers/[id]/edit/page.tsx` | Edit form |

---

## 6. State Transitions (تحولات الحالة)

A customer has only one state: **Active** or **Inactive**. There is no workflow approval (unlike invoices or POs). The state is toggled by editing the customer record.

```
┌─────────┐  save(isActive=true)   ┌─────────┐
│ Inactive│ ──────────────────────▶│ Active  │
└─────────┘                         └─────────┘
     ▲                                  │
     │       save(isActive=false)       │
     └──────────────────────────────────┘
```

**Effect on other documents:**
- Inactive customers **cannot be selected** in the Sales Invoice form.
- Inactive customers **remain visible** in historical invoices and reports.
- Inactive customers **can be reactivated** at any time.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Duplicate code** | The backend rejects with `409 Conflict`. The form shows "هذا الكود مستخدم" (This code is already used). |
| **Customer has open invoices** | Deactivation is allowed; existing invoices remain unaffected. |
| **Customer has open receipts** | Same — deactivation does not touch posted receipts. |
| **Code is locked** | Once a customer is created, the `code` field cannot be changed (it is the external reference for invoices, reports, and tax filings). |
| **Empty optional fields** | Email, phone, address, credit limit, tax ID are all optional. The customer is still valid. |
| **Search is case-insensitive** | Searching `alfajr` matches `Alfajr` and `الفجر`. |
| **Currency** | Always LYD (Libyan Dinar). Multi-currency support is post-Sprint 19. |
| **Cross-company** | Customers are scoped to the active company. Switching companies via the company switcher shows a different list. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| العملاء | Customers | Sidebar, page title |
| عميل جديد | New Customer | Button |
| كود العميل | Customer Code | Form label |
| اسم العميل | Customer Name | Form label |
| الاسم بالإنجليزية | Name (English) | Form label |
| الرقم الضريبي | Tax ID | Form label |
| حد الائتمان | Credit Limit | Form label, table column |
| شروط الدفع | Payment Terms | Form label, table column |
| نشط / غير نشط | Active / Inactive | Badge, form checkbox |
| حفظ | Save | Button |
| تعديل | Edit | Button |
| إلغاء | Cancel | Button |
| رجوع | Back | Button |
| كشف حساب | Statement | Button (in view page) |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Sales Invoice** (`docs/workflows/sales-invoice.md`) — every invoice references a customer.
- **Receipt** (`docs/workflows/sales-invoice.md` §6) — every receipt is linked to a customer and may be allocated to one or more invoices.
- **AR Aging** — the aging report is grouped by customer.
- **Sales by Customer Report** — KPI report fed by customer invoices.

---

_Last updated: 2026-08-01 — Sprint 19 (Client Demo Sprint)._
