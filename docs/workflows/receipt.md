# Workflow: Receipt (سندات القبض)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/AccountsReceivable`.

---

## 1. Business Purpose (الغرض التجاري)

The **Receipt** function records money received from a customer, applied to one or more sales invoices. Each receipt carries the customer, receipt date, amount, payment method, and an **allocation** (which invoice(s) the payment is applied to). The receipt is the anchor for:

- **AR Ledger** — what the customer paid (credit).
- **Cash/Bank Recognition** — Dr Cash/Bank / Cr 1230 AR.
- **Customer Statement** — receipt is a credit line.
- **Collections Report** — KPI report fed by receipts.
- **AR Aging** — receipt reduces the customer's outstanding balance.

A receipt is what closes the sales invoice loop. Without receipts, invoices stay outstanding forever.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can post? | Can reverse? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Sales** (مبيعات) | ✅ | ✅ | ✅ (Draft) | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:** Only Accountant and Admin can post a receipt (posting creates the journal entry). Sales creates the draft.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **سندات القبض** (`/finance/receipts`) from the sidebar.
2. The page loads all receipts in a sortable table.
3. Each row shows: receipt number, customer, receipt date, amount, payment method, status.

### 3.2 Create a new receipt
1. Click **سند قبض جديد** (top-right button).
2. Fill the form:
   - **Customer** (required, dropdown) — must be an active customer.
   - **Receipt Date** (default today).
   - **Amount** (required, > 0) — total money received.
   - **Currency** (default LYD).
   - **Payment Method** (dropdown) — Cash, Bank, Transfer, Check.
   - **Notes** (optional).
   - **Allocations** (at least 1) — each line has:
     - **Invoice** (required, dropdown) — only shows the selected customer's outstanding invoices.
     - **Amount Applied** (required, > 0, ≤ invoice outstanding).
3. The system auto-validates: sum of allocations must equal the receipt amount.
4. Click **حفظ كمسودة** (Save as Draft).
5. **Optional:** Click **حفظ وترحيل** (Save and Post) — receipt is posted, journal entry created, status → `Posted`.

### 3.3 Create a receipt from an invoice
1. Open a Sent/Overdue/PartiallyPaid invoice's view page.
2. Click **سند قبض** (Create Receipt).
3. The form is pre-filled: customer, currency, single allocation to this invoice.
4. Enter the amount received (≤ invoice outstanding).
5. Save and post.

### 3.4 View a receipt
1. Click on a row in the list.
2. The view page shows: receipt header, customer, allocations (which invoices), totals.
3. From the view page you can:
   - Click **تعديل** (if Draft) to edit.
   - Click **عكس** (if Posted) to reverse (creates a reversing journal entry).
   - Click **القيد المحاسبي** to view the journal entry.

### 3.5 Post a receipt
1. Open a Draft receipt's view page.
2. Click **ترحيل** (Post).
3. The system:
   - Validates allocations sum = amount.
   - Creates a journal entry (Dr Cash/Bank / Cr 1230 AR).
   - Updates the receipt status to `Posted`.
   - Updates each allocated invoice: `paidAmount += allocation`, status may become `PartiallyPaid` or `Paid`.

### 3.6 Reverse a receipt
1. Open a Posted receipt's view page.
2. Click **عكس** (Reverse).
3. The system:
   - Creates a reversing journal entry (Cr Cash/Bank / Dr 1230 AR).
   - Updates the receipt status to `Reversed`.
   - Updates each allocated invoice: `paidAmount -= allocation`, outstanding restored.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/ar/receipts`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/ar/receipts` | List all receipts | `ReceiptResponse[]` |
| `GET` | `/api/ar/receipts/{id}` | Get one receipt | `ReceiptResponse` |
| `POST` | `/api/ar/receipts` | Create a receipt | `ReceiptResponse` (201) |
| `PUT` | `/api/ar/receipts/{id}/post` | Post a Draft receipt | `ReceiptResponse` |
| `PUT` | `/api/ar/receipts/{id}/reverse` | Reverse a Posted receipt | `ReceiptResponse` |

### Request body — `CreateReceiptRequest`
```json
{
  "customerId": "guid",
  "receiptDate": "2026-08-10T00:00:00Z",
  "amount": 1500.00,
  "currencyCode": "LYD",
  "paymentMethod": "Bank",
  "notes": "تحويل بنكي من الفجر",
  "allocations": [
    {
      "salesInvoiceId": "guid",
      "amountApplied": 1500.00
    }
  ],
  "postImmediately": true
}
```

### Response — `ReceiptResponse`
```json
{
  "id": "guid",
  "customerId": "guid",
  "customerName": "شركة الفجر للتجارة",
  "receiptNumber": "RCT-2026-0001",
  "receiptDate": "2026-08-10T00:00:00Z",
  "amount": 1500.00,
  "currencyCode": "LYD",
  "paymentMethod": "Bank",
  "notes": "تحويل بنكي من الفجر",
  "postedAt": "2026-08-10T10:30:00Z",
  "journalEntryId": "guid",
  "createdAt": "2026-08-10T10:00:00Z",
  "allocations": [
    {
      "id": "guid",
      "salesInvoiceId": "guid",
      "salesInvoiceNumber": "INV-2026-0001",
      "amountApplied": 1500.00
    }
  ]
}
```

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/finance/receipts` | `app/(authenticated)/finance/receipts/page.tsx` | List |
| `/finance/receipts/new` | `app/(authenticated)/finance/receipts/new/page.tsx` | Create |
| `/finance/receipts/{id}` | `app/(authenticated)/finance/receipts/[id]/page.tsx` | View |

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
                           │ Posted  │ ──── reverse ────▶ ┌─────────┐
                           └─────────┘                    │Reversed │
                                                         └─────────┘
```

**Important rules:**
- Only `Draft` is editable.
- `Posted` creates the journal entry + updates each allocated invoice.
- `Reversed` is terminal — creates a reversing entry + restores invoice outstanding.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Allocations sum ≠ amount** | The form rejects. Backend rejects with `400`. |
| **Allocation amount > invoice outstanding** | The form rejects (cannot overpay an invoice). |
| **Invoice belongs to different customer** | The form rejects (invoices in allocation must belong to the receipt's customer). |
| **Invoice is Draft/Cancelled** | The form rejects (cannot allocate to non-posted invoices). |
| **Customer is inactive** | Customer dropdown only shows active customers. |
| **Edit after Posted** | The form is read-only. To fix, reverse the receipt and create a new one. |
| **Reverse a reversed receipt** | The system rejects (already reversed). |
| **Currency mismatch with invoice** | The system warns but allows (cross-currency receipts). |
| **Payment method is Check** | The system tracks check number in `notes` (no separate field). |
| **Receipt amount is 0** | The form rejects (must be > 0). |
| **Cross-company** | Receipts are scoped to the active company. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| سندات القبض | Receipts | Sidebar, page title |
| سند قبض جديد | New Receipt | Button |
| رقم السند | Receipt Number | Form label, table column |
| العميل | Customer | Form label, table column |
| تاريخ السند | Receipt Date | Form label |
| المبلغ | Amount | Form label, table column |
| طريقة الدفع | Payment Method | Form label, table column |
| نقدي | Cash | Option |
| بنك | Bank | Option |
| تحويل | Transfer | Option |
| شيك | Check | Option |
| التخصيص | Allocation | Form label |
| الفاتورة | Invoice | Form label |
| المبلغ المخصص | Amount Applied | Form label |
| مسودة | Draft | Badge |
| مُرحَّل | Posted | Badge |
| معكوس | Reversed | Badge |
| حفظ كمسودة | Save as Draft | Button |
| حفظ وترحيل | Save and Post | Button |
| ترحيل | Post | Button |
| عكس | Reverse | Button |
| تعديل | Edit | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Customer** (`docs/workflows/customer.md`) — every receipt references a customer.
- **Sales Invoice** (`docs/workflows/sales-invoice.md`) — receipts are allocated to one or more invoices.
- **Collections Report** — KPI report fed by posted receipts.
- **Customer Statement** — receipts appear as credit lines on the statement.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
