# Workflow: Sales Invoice (فواتير المبيعات)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 19 (Client Demo Sprint).
> **Backend module:** `Modules/AccountsReceivable`.

---

## 1. Business Purpose (الغرض التجاري)

The **Sales Invoice** function records every credit sale to a customer. Each invoice carries the customer, invoice date, due date, line items, subtotal, tax, and total. The invoice is the anchor for:

- **AR Ledger** — what the customer owes (debit).
- **Revenue Recognition** — sales + output VAT (credit).
- **Customer Statement** — invoice is a debit line.
- **AR Aging** — invoices flow into 0-30 / 31-60 / 61-90 / 91-120 / 120+ buckets.
- **Reports** — Sales by Customer, Sales by Item, Top Customers, VAT.

A sales invoice is the **most-tracked document** in any ERP — every Libyan business files tax returns and chases receivables based on it.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can post? | Can cancel? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ (Draft only) | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ (Draft only) | ✅ | ✅ |
| **Sales** (مبيعات) | ✅ | ✅ | ✅ (Draft only) | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:**
- Only Accountant and Admin can **post** an invoice (posting creates the journal entry — Dr 1230 AR / Cr 5110 Sales + Cr 2120 VAT).
- Only Accountant and Admin can **cancel** a posted invoice (cancellation reverses the journal entry).
- Sales can create and edit Draft invoices, but cannot post them — they hand off to the Accountant.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **فواتير المبيعات** (`/finance/sales-invoices`) from the sidebar.
2. The page loads all invoices in a sortable table.
3. Use the search box to filter by invoice number, customer name, or status.
4. Each row shows: invoice number, date, due date, customer, total, paid, outstanding, status (Draft / Sent / Paid / Overdue / Cancelled).

### 3.2 Create a new invoice
1. Click **فاتورة جديدة** (top-right button).
2. Fill the form:
   - **Customer** (required, dropdown) — must be an active customer.
   - **Invoice Date** (default today) — date the invoice is issued.
   - **Due Date** (default +30 days) — when payment is expected.
   - **Currency** (default LYD) — invoice currency.
   - **Exchange Rate** (default 1) — only relevant for non-LYD currencies (post-Sprint 19).
   - **Notes** (optional) — internal notes.
   - **Lines** (at least 1) — each line has:
     - **Item** (optional, dropdown) — pick from the item list.
     - **Description** (required if no item) — free-form description.
     - **Quantity** (required, > 0).
     - **Unit Price** (required, ≥ 0).
     - **Tax Rate** (default 0) — VAT % (0%, 5%, 10%, etc.).
3. The form auto-calculates: line total = quantity × unit price, subtotal = sum of line totals, tax = sum of (line total × tax rate), total = subtotal + tax.
4. Click **حفظ كمسودة** (Save as Draft) — the invoice is saved with status `Draft`.
5. **Optional:** Click **حفظ وترحيل** (Save and Post) — the invoice is saved AND posted in one step (status → `Sent`).
6. The new invoice appears in the list.

### 3.3 View an invoice
1. Click on a row in the list (or open `/finance/sales-invoices/{id}`).
2. The view page shows:
   - Invoice header (number, dates, customer, status).
   - Line items (description, qty, price, tax, total).
   - Totals (subtotal, tax, total, paid, outstanding).
   - **Journal entry ID** (if posted) — link to the underlying journal entry.
   - **Receipt allocations** (if any receipts have been applied).
3. From the view page you can:
   - Click **تعديل** (if Draft) to open the edit form.
   - Click **ترحيل** (if Draft) to post the invoice.
   - Click **إلغاء** (if Sent/Paid) to cancel the invoice.
   - Click **سند قبض** to create a receipt that allocates to this invoice.
   - Click **رجوع** to go back to the list.

### 3.4 Edit an invoice
1. Open the invoice's view page, then click **تعديل**.
2. The same form as create, pre-filled.
3. Edit any field except **Invoice Number** (auto-generated) and **Status** (changed via Post / Cancel actions).
4. Click **حفظ كمسودة**.

### 3.5 Post an invoice
1. Open a Draft invoice's view page.
2. Click **ترحيل** (Post).
3. The system:
   - Validates that all lines are valid.
   - Creates a journal entry (Dr 1230 AR / Cr 5110 Sales + Cr 2120 VAT).
   - Updates the invoice status to `Sent`.
   - Sets the `postedAt` and `journalEntryId` fields.
4. The invoice is now visible in financial reports and AR aging.

### 3.6 Cancel an invoice
1. Open a Sent or Paid invoice's view page.
2. Click **إلغاء** (Cancel).
3. The system:
   - Creates a reversing journal entry (Cr 1230 AR / Dr 5110 Sales + Dr 2120 VAT).
   - Reverses any linked receipts.
   - Updates the invoice status to `Cancelled`.
4. The invoice is no longer counted in AR aging or revenue reports.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/ar/sales-invoices`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/ar/sales-invoices` | List all invoices | `SalesInvoiceResponse[]` |
| `GET` | `/api/ar/sales-invoices/{id}` | Get one invoice | `SalesInvoiceResponse` |
| `POST` | `/api/ar/sales-invoices` | Create an invoice (with optional `postImmediately`) | `SalesInvoiceResponse` (201) |
| `PUT` | `/api/ar/sales-invoices/{id}` | Update a Draft invoice | `SalesInvoiceResponse` |
| `PUT` | `/api/ar/sales-invoices/{id}/post` | Post a Draft invoice | `SalesInvoiceResponse` |
| `PUT` | `/api/ar/sales-invoices/{id}/cancel` | Cancel a Sent/Paid invoice | `SalesInvoiceResponse` |

### Request body — `CreateSalesInvoiceRequest`
```json
{
  "customerId": "guid",
  "invoiceDate": "2026-08-01T00:00:00Z",
  "dueDate": "2026-08-31T00:00:00Z",
  "currencyCode": "LYD",
  "exchangeRate": 1.0,
  "notes": "فاتورة شهر أغسطس",
  "projectId": null,
  "lines": [
    {
      "description": "أرز بسمتي 5 كغ",
      "itemId": "guid",
      "quantity": 10,
      "unitPrice": 25.00,
      "taxRate": 0
    }
  ],
  "postImmediately": false
}
```

### Response — `SalesInvoiceResponse`
```json
{
  "id": "guid",
  "customerId": "guid",
  "customerName": "شركة الفجر للتجارة",
  "invoiceNumber": "INV-2026-0001",
  "invoiceDate": "2026-08-01T00:00:00Z",
  "dueDate": "2026-08-31T00:00:00Z",
  "currencyCode": "LYD",
  "exchangeRate": 1.0,
  "subtotal": 250.00,
  "taxAmount": 0.00,
  "totalAmount": 250.00,
  "paidAmount": 0.00,
  "outstanding": 250.00,
  "status": 1,
  "notes": "فاتورة شهر أغسطس",
  "postedAt": null,
  "journalEntryId": null,
  "createdAt": "2026-08-01T09:00:00Z",
  "lines": [
    {
      "id": "guid",
      "lineNumber": 1,
      "description": "أرز بسمتي 5 كغ",
      "itemId": "guid",
      "quantity": 10,
      "unitPrice": 25.00,
      "taxRate": 0,
      "lineTotal": 250.00
    }
  ],
  "allocations": []
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | مسودة | Draft |
| 2 | مُرسل | Sent |
| 3 | مدفوع جزئياً | Partially Paid |
| 4 | مدفوع | Paid |
| 5 | متأخر | Overdue |
| 6 | ملغي | Cancelled |

### Error codes
- `400 Bad Request` — missing required field, invalid customer/item ID, line with quantity ≤ 0 or unit price < 0.
- `404 Not Found` — invoice ID, customer ID, or item ID does not exist.
- `409 Conflict` — invoice number already exists; cannot post a non-Draft invoice.
- `422 Unprocessable Entity` — total is 0; line items are empty.

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/finance/sales-invoices` | `app/(authenticated)/finance/sales-invoices/page.tsx` | List + search + filter |
| `/finance/sales-invoices/new` | `app/(authenticated)/finance/sales-invoices/new/page.tsx` | Create form |
| `/finance/sales-invoices/{id}` | `app/(authenticated)/finance/sales-invoices/[id]/page.tsx` | View (read-only) |
| `/finance/sales-invoices/{id}/edit` | `app/(authenticated)/finance/sales-invoices/[id]/edit/page.tsx` | Edit form (Draft only) |

---

## 6. State Transitions (تحولات الحالة)

```
                  save
    ┌─────────┐ ────────▶ ┌─────────┐
    │  (new)  │            │  Draft  │ ◀─── update (Draft only)
    └─────────┘            └─────────┘
                                │
                                │ post
                                ▼
                           ┌─────────┐
                           │  Sent   │ ──── payment received ────▶ ┌──────────┐
                           └─────────┘                              │   Paid   │
                                │                                  └──────────┘
                                │ overdue (computed by date+status)
                                ▼
                           ┌─────────┐
                           │ Overdue │ ◀── (system-computed, not user-set)
                           └─────────┘

    Any of {Sent, Paid, PartiallyPaid, Overdue} ──── cancel ────▶ ┌───────────┐
                                                                   │ Cancelled │
                                                                   └───────────┘
```

**Important rules:**
- Only **Draft** can be edited.
- Only **Draft** can be deleted (full delete, not soft).
- **Post** transitions Draft → Sent (creates journal entry).
- **Cancel** transitions any of {Sent, Paid, PartiallyPaid, Overdue} → Cancelled (creates reversing journal entry).
- **Overdue** is a computed status (system checks daily: due date < today AND status = Sent/PartiallyPaid).
- **PartiallyPaid** is set when a receipt is applied that is less than the invoice total.
- **Paid** is set when receipts total equals the invoice total.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Empty line items** | The form rejects submit. The backend rejects with `422`. |
| **Quantity ≤ 0 or unit price < 0** | The form rejects submit. The backend rejects with `400`. |
| **Customer is inactive** | The customer dropdown only shows active customers. |
| **Item is inactive** | The item dropdown only shows active items. |
| **Invoice total is 0** | Allowed (e.g. free samples), but the system warns. |
| **Post fails** (e.g. unbalanced journal) | The invoice stays in Draft. The error is shown to the user. |
| **Cancel after receipts applied** | The system reverses the receipts first, then the invoice. |
| **Edit after posting** | The form is read-only. The user must Cancel and create a new invoice. |
| **Currency change** | Currency is captured at invoice time. New invoices use the customer's default currency. |
| **Tax rate 0** | Allowed (tax-exempt items). The tax line is shown as 0.00. |
| **Cross-company** | Invoices are scoped to the active company. Switching companies via the company switcher shows a different list. |
| **Invoice number generation** | Auto-generated as `INV-YYYY-NNNN` (sequential per company per year). |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| فواتير المبيعات | Sales Invoices | Sidebar, page title |
| فاتورة جديدة | New Invoice | Button |
| رقم الفاتورة | Invoice Number | Form label, table column |
| العميل | Customer | Form label, table column |
| تاريخ الفاتورة | Invoice Date | Form label, table column |
| تاريخ الاستحقاق | Due Date | Form label, table column |
| العملة | Currency | Form label |
| سعر الصرف | Exchange Rate | Form label |
| البضاعة / الصنف | Item | Form label |
| الوصف | Description | Form label, table column |
| الكمية | Quantity | Form label, table column |
| سعر الوحدة | Unit Price | Form label, table column |
| نسبة الضريبة | Tax Rate | Form label |
| المجموع الفرعي | Subtotal | Form label |
| الضريبة | Tax | Form label |
| الإجمالي | Total | Form label, table column |
| المدفوع | Paid | Table column |
| المتبقي | Outstanding | Table column |
| الحالة | Status | Table column |
| مسودة | Draft | Badge |
| مُرسل | Sent | Badge |
| مدفوع جزئياً | Partially Paid | Badge |
| مدفوع | Paid | Badge |
| متأخر | Overdue | Badge |
| ملغي | Cancelled | Badge |
| حفظ كمسودة | Save as Draft | Button |
| حفظ وترحيل | Save and Post | Button |
| ترحيل | Post | Button |
| إلغاء | Cancel | Button (action) / Cancel (modal) |
| تعديل | Edit | Button |
| رجوع | Back | Button |
| سند قبض | Receipt | Button (link to create receipt) |
| القيد المحاسبي | Journal Entry | View page (link) |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Customer** (`docs/workflows/customer.md`) — every invoice references a customer.
- **Item** (`docs/workflows/item.md`) — invoice lines may reference an item.
- **Receipt** — a receipt can be applied to one or more invoices (allocation).
- **AR Aging** — the aging report is grouped by invoice (then by customer).
- **VAT Report** — output VAT from posted invoices.
- **Sales by Customer / Item Reports** — KPI reports fed by posted invoices.

---

_Last updated: 2026-08-01 — Sprint 19 (Client Demo Sprint)._
