# Workflow: Vendor Bill (فواتير الموردين)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/Procurement`.

---

## 1. Business Purpose (الغرض التجاري)

The **Vendor Bill** function records what the vendor invoiced for goods received. Each bill is linked to a Goods Receipt (and transitively to a PO + vendor). The bill is the anchor for:

- **AP Ledger** — what the company owes the vendor (credit).
- **Expense Recognition** — Dr Inventory/Expense / Cr Accounts Payable.
- **AP Aging** — outstanding bills flow into 0-30 / 31-60 / 61-90 / 91+ buckets.
- **Vendor Statement** — bill is a credit line.
- **Procurement Reports** — Purchases by Vendor, Top Vendors, VAT.

A vendor bill is the **most-tracked document in payables** — every Libyan business uses bills to track VAT inputs and payment obligations.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can post? | Can cancel? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Procurement** (مشتريات) | ✅ | ✅ | ✅ (Draft) | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:** Only Accountant and Admin can post a bill (posting creates the journal entry — Dr Inventory/Expense / Cr 2110 AP). Procurement creates the draft.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **فواتير الموردين** (`/procurement/bills`) from the sidebar.
2. The page loads all bills in a sortable table.
3. Each row shows: bill number, GR number, vendor, bill date, due date, total, paid, outstanding, status (Draft/Posted/Paid/Cancelled).

### 3.2 Create a new bill (from a GR)
1. Open a `Received` GR's view page.
2. Click **إنشاء فاتورة مورّد** (Create Vendor Bill).
3. The new bill form is pre-filled with:
   - Source GR (locked)
   - Vendor (auto from GR)
   - Currency (auto from GR)
   - Lines (from GR, editable for unit price to match the vendor's actual invoice)
4. Fill the additional fields:
   - **Bill Number** (vendor's invoice number, e.g. `INV-V-2026-123`)
   - **Bill Date** (when the vendor issued the invoice, default today)
   - **Due Date** (when payment is due, computed from vendor payment terms or manual)
   - **Notes** (optional)
5. Click **حفظ كمسودة** (Save as Draft) — bill is in `Draft` status.
6. Click **ترحيل** (Post) — bill is posted, journal entry is created, status → `Posted`.

### 3.3 View a bill
1. Click on a row in the list.
2. The view page shows: bill header, source GR, vendor, lines, totals, payment allocations.
3. From the view page you can:
   - Click **تعديل** (if Draft) to edit.
   - Click **دفع** to record a payment (allocated to this bill).
   - Click **إلغاء** to cancel.

### 3.4 Post a bill
1. Open a Draft bill's view page.
2. Click **ترحيل** (Post).
3. The system:
   - Validates that the bill matches the GR (quantity, total).
   - Creates a journal entry (Dr Inventory/Expense / Cr 2110 AP + Dr 1410 Input VAT if tax).
   - Updates the bill status to `Posted`.
   - Sets the `postedAt` and `journalEntryId` fields.

### 3.5 Record a payment
1. Open a Posted bill's view page.
2. Click **دفع** (Pay).
3. The payment form opens (uses a Bank Payment or Cash Payment voucher).
4. The bill's `paidAmount` increases, `outstanding` decreases. When `outstanding = 0`, status becomes `Paid`.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/procurement/bills`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/procurement/bills` | List all bills | `VendorBillResponse[]` |
| `POST` | `/api/procurement/bills` | Create a bill (from a GR) | `VendorBillResponse` (201) |

### Request body — `CreateVendorBillRequest`
```json
{
  "goodsReceiptId": "guid",
  "billNumber": "INV-V-2026-123",
  "billDate": "2026-08-06T00:00:00Z",
  "dueDate": "2026-09-05T00:00:00Z",
  "notes": "فاتورة المورّد لشهر أغسطس",
  "lines": [
    {
      "itemId": "guid",
      "quantity": 50,
      "unitPrice": 25.00,
      "taxRate": 0
    }
  ]
}
```

### Response — `VendorBillResponse`
```json
{
  "id": "guid",
  "billNumber": "INV-V-2026-123",
  "goodsReceiptId": "guid",
  "grNumber": "GR-2026-0001",
  "vendorId": "guid",
  "vendorName": "مورد البضائع العامة",
  "status": 1,
  "billDate": "2026-08-06T00:00:00Z",
  "dueDate": "2026-09-05T00:00:00Z",
  "currency": "LYD",
  "subTotal": 1250.00,
  "taxAmount": 0.00,
  "totalAmount": 1250.00,
  "notes": "فاتورة المورّد لشهر أغسطس",
  "lines": [
    {
      "id": "guid",
      "itemId": "guid",
      "itemName": "أرز بسمتي 5 كغ",
      "quantity": 50,
      "unitPrice": 25.00,
      "taxRate": 0,
      "subTotal": 1250.00
    }
  ],
  "createdAt": "2026-08-06T11:00:00Z"
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | مسودة | Draft |
| 2 | مُرحَّل | Posted |
| 3 | مُدفوع | Paid |
| 4 | ملغي | Cancelled |

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/procurement/bills` | `app/(authenticated)/procurement/bills/page.tsx` | List |
| `/procurement/bills/new` | `app/(authenticated)/procurement/bills/new/page.tsx` | Create (pre-fills from GR) |
| `/procurement/bills/{id}` | `app/(authenticated)/procurement/bills/[id]/page.tsx` | View |

---

## 6. State Transitions (تحولات الحالة)

```
                  save (from GR)
    ┌─────────┐ ───────────────────▶ ┌─────────┐
    │  (new)  │                       │  Draft  │ ◀─── update (Draft only)
    └─────────┘                       └─────────┘
                                         │
                                         │ post
                                         ▼
                                    ┌─────────┐
                                    │ Posted  │ ──── payment applied ────▶ ┌─────────┐
                                    └─────────┘                              │  Paid   │
                                         │                                  └─────────┘
                                         │ cancel (Draft/Posted)
                                         ▼
                                    ┌─────────┐
                                    │Cancelled│
                                    └─────────┘
```

**Important rules:**
- A bill is always created from a Received GR.
- `Draft` is editable.
- `Posted` is the source of AP liability and aging.
- `Paid` is automatic when the total of payment allocations equals the bill total.
- `Cancel` reverses the journal entry.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Empty line items** | Form rejects. Backend rejects with `422`. |
| **Bill quantity > GR quantity** | The system rejects (you cannot bill for more than was received). |
| **Duplicate bill number** | The backend rejects with `409 Conflict`. |
| **Bill for a Cancelled GR** | The system rejects. |
| **Edit after Posted** | The form is read-only. To fix, Cancel the bill and create a new one. |
| **Cancel after payment** | The system reverses the payment first, then the bill. |
| **Currency mismatch with GR** | The system warns (currency should match the source GR). |
| **Tax rate 0** | Allowed. The system still records the bill but no input VAT. |
| **Bill date before GR date** | The system warns (unusual but allowed for retroactive entry). |
| **Cross-company** | Bills are scoped to the active company. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| فواتير الموردين | Vendor Bills | Sidebar, page title |
| فاتورة جديدة | New Bill | Button |
| رقم الفاتورة | Bill Number | Form label, table column |
| المورّد | Vendor | Table column |
| تاريخ الفاتورة | Bill Date | Form label |
| تاريخ الاستحقاق | Due Date | Form label |
| الإجمالي | Total | Table column |
| المدفوع | Paid | Table column |
| المتبقي | Outstanding | Table column |
| مسودة | Draft | Badge |
| مُرحَّل | Posted | Badge |
| مُدفوع | Paid | Badge |
| ملغي | Cancelled | Badge |
| ترحيل | Post | Button |
| دفع | Pay | Button |
| إلغاء | Cancel | Button |
| تعديل | Edit | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Vendor** (`docs/workflows/vendor.md`) — every bill references a vendor.
- **Goods Receipt** (`docs/workflows/goods-receipt.md`) — bills are created from GRs.
- **Purchase Order** (`docs/workflows/purchase-order.md`) — the source of the GR.
- **AP Aging** — outstanding bills flow into aging buckets.
- **VAT Report** — input VAT from posted bills.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
