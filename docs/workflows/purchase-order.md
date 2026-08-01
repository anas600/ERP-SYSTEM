# Workflow: Purchase Order (أوامر الشراء)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/Procurement`.

---

## 1. Business Purpose (الغرض التجاري)

The **Purchase Order (PO)** function records every order the company places with a vendor. Each PO carries the vendor, order date, expected delivery date, line items (item + quantity + price), subtotal, tax, and total. The PO is the anchor for:

- **Goods Receipts (GR)** — what was actually received against the PO.
- **Vendor Bills** — what the vendor invoiced for the PO.
- **Procurement Reports** — Purchases by Vendor, Top Vendors.
- **AP Aging** — bills linked to POs drive the vendor outstanding balance.

Without a PO, the company cannot track what was ordered, what was received, and what is owed.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can approve? | Can send? | Can cancel? |
|---|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ (Draft/Pending) | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ (Draft/Pending) | ✅ | ✅ | ✅ |
| **Procurement** (مشتريات) | ✅ | ✅ | ✅ (Draft/Pending) | ❌ | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:** Only Admin and Accountant can approve and send a PO (sending notifies the vendor and commits the company financially). Procurement creates the draft but must hand off to accounting for approval.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **أوامر الشراء** (`/procurement/purchase-orders`) from the sidebar.
2. The page loads all POs in a sortable table.
3. Use the search box to filter by PO number or vendor name.
4. Each row shows: PO number, vendor, order date, expected date, total, status (Draft/Pending/Approved/Sent/Received/Cancelled).

### 3.2 Create a new PO
1. Click **أمر شراء جديد** (top-right button).
2. Fill the form:
   - **Vendor** (required, dropdown) — must be an active vendor.
   - **Order Date** (default today).
   - **Expected Date** (optional) — when goods are expected.
   - **Currency** (default LYD).
   - **Notes** (optional).
   - **Lines** (at least 1) — each line has:
     - **Item** (required, dropdown) — must be an active item.
     - **Quantity** (required, > 0).
     - **Unit Price** (required, ≥ 0).
     - **Tax Rate** (default 0).
3. The form auto-calculates: line total, subtotal, tax, total.
4. Click **حفظ كمسودة** (Save as Draft) — PO is in `Draft` status.
5. **Optional:** Click **إرسال للموافقة** (Send for Approval) — PO moves to `Pending`.

### 3.3 View a PO
1. Click on a row in the list (or open `/procurement/purchase-orders/{id}`).
2. The view page shows:
   - PO header (number, vendor, dates, status).
   - Line items.
   - Totals.
3. From the view page you can:
   - Click **تعديل** (if Draft/Pending) to edit.
   - Click **موافقة** (if Pending) to approve → status `Approved`.
   - Click **إرسال للمورّد** (if Approved) to send → status `Sent` (vendor is notified).
   - Click **استلام بضاعة** to create a Goods Receipt.
   - Click **إلغاء** to cancel.

### 3.4 Approve a PO
1. Open a Pending PO's view page.
2. Click **موافقة** (Approve).
3. The status changes to `Approved`. The PO is now ready to send to the vendor.

### 3.5 Send a PO
1. Open an Approved PO's view page.
2. Click **إرسال للمورّد** (Send to Vendor).
3. The status changes to `Sent`. The vendor is now expected to fulfill the order.

### 3.6 Cancel a PO
1. Open the PO's view page (any status except Received).
2. Click **إلغاء**.
3. The status changes to `Cancelled`. No GR or Bill can be created from it.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/procurement/pos`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/procurement/pos` | List all POs | `PurchaseOrderResponse[]` |
| `GET` | `/api/procurement/pos/{id}` | Get one PO | `PurchaseOrderResponse` |
| `POST` | `/api/procurement/pos` | Create a PO (Draft) | `PurchaseOrderResponse` (201) |

### Request body — `CreatePurchaseOrderRequest`
```json
{
  "vendorId": "guid",
  "orderDate": "2026-08-01T00:00:00Z",
  "expectedDate": "2026-08-15T00:00:00Z",
  "currency": "LYD",
  "notes": "طلب شراء بضاعة لمحل الفجر",
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

### Response — `PurchaseOrderResponse`
```json
{
  "id": "guid",
  "poNumber": "PO-2026-0001",
  "vendorId": "guid",
  "vendorName": "مورد البضائع العامة",
  "status": 1,
  "orderDate": "2026-08-01T00:00:00Z",
  "expectedDate": "2026-08-15T00:00:00Z",
  "currency": "LYD",
  "subTotal": 1250.00,
  "taxAmount": 0.00,
  "totalAmount": 1250.00,
  "notes": "طلب شراء بضاعة لمحل الفجر",
  "createdAt": "2026-08-01T09:00:00Z",
  "lines": [
    {
      "id": "guid",
      "itemId": "guid",
      "quantity": 50,
      "unitPrice": 25.00,
      "taxRate": 0,
      "subTotal": 1250.00,
      "lineOrder": 1
    }
  ]
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | مسودة | Draft |
| 2 | بانتظار الموافقة | Pending |
| 3 | معتمد | Approved |
| 4 | مُرسل للمورّد | Sent |
| 5 | مُستلَم | Received |
| 6 | ملغي | Cancelled |

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/procurement/purchase-orders` | `app/(authenticated)/procurement/purchase-orders/page.tsx` | List + filter |
| `/procurement/purchase-orders/new` | `app/(authenticated)/procurement/purchase-orders/new/page.tsx` | Create form |
| `/procurement/purchase-orders/{id}` | `app/(authenticated)/procurement/purchase-orders/[id]/page.tsx` | View (read-only) |
| `/procurement/purchase-orders/{id}/edit` | `app/(authenticated)/procurement/purchase-orders/[id]/edit/page.tsx` | Edit form |

---

## 6. State Transitions (تحولات الحالة)

```
                  save
    ┌─────────┐ ────────▶ ┌─────────┐
    │  (new)  │            │  Draft  │ ◀─── update (Draft/Pending only)
    └─────────┘            └─────────┘
                                │
                                │ send-for-approval
                                ▼
                           ┌─────────┐
                           │ Pending │
                           └─────────┘
                                │
                                │ approve
                                ▼
                           ┌─────────┐
                           │Approved │
                           └─────────┘
                                │
                                │ send
                                ▼
                           ┌─────────┐ ──── GR created ────▶ ┌──────────┐
                           │   Sent  │                       │ Received │
                           └─────────┘                       └──────────┘
                                │
                                │ cancel (any non-Received status)
                                ▼
                           ┌─────────┐
                           │Cancelled│
                           └─────────┘
```

**Important rules:**
- Only `Draft` and `Pending` are editable.
- `Approved` → `Sent` requires Admin/Accountant role.
- `Sent` is the trigger for Goods Receipt creation.
- `Received` is automatic when a full Goods Receipt is posted against the PO.
- `Cancelled` is terminal — no further transitions.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Empty line items** | Form rejects submit. Backend rejects with `422`. |
| **Quantity ≤ 0** | Form rejects submit. Backend rejects with `400`. |
| **Vendor is inactive** | Vendor dropdown only shows active vendors. |
| **Item is inactive** | Item dropdown only shows active items. |
| **Edit after Sent** | The form is read-only. The user must Cancel and create a new PO. |
| **Cancel after Sent** | Allowed but warns the user that no GR can be created afterwards. |
| **Cancel after GR created** | NOT allowed — the PO is locked once a GR references it. |
| **PO number generation** | Auto-generated as `PO-YYYY-NNNN` (sequential per company per year). |
| **Currency** | Always LYD. Multi-currency support is post-Sprint 20. |
| **Tax rate 0** | Allowed (tax-exempt items). The tax line is shown as 0.00. |
| **Cross-company** | POs are scoped to the active company. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| أوامر الشراء | Purchase Orders | Sidebar, page title |
| أمر شراء جديد | New PO | Button |
| رقم الأمر | PO Number | Form label, table column |
| المورّد | Vendor | Form label, table column |
| تاريخ الطلب | Order Date | Form label |
| تاريخ التسليم المتوقع | Expected Date | Form label |
| البضاعة / الصنف | Item | Form label |
| الكمية | Quantity | Form label, table column |
| سعر الوحدة | Unit Price | Form label, table column |
| نسبة الضريبة | Tax Rate | Form label |
| المجموع الفرعي | Subtotal | Form label |
| الإجمالي | Total | Form label, table column |
| مسودة | Draft | Badge |
| بانتظار الموافقة | Pending | Badge |
| معتمد | Approved | Badge |
| مُرسل للمورّد | Sent | Badge |
| مُستلَم | Received | Badge |
| ملغي | Cancelled | Badge |
| حفظ كمسودة | Save as Draft | Button |
| إرسال للموافقة | Send for Approval | Button |
| موافقة | Approve | Button |
| إرسال للمورّد | Send to Vendor | Button |
| استلام بضاعة | Create GR | Button |
| إلغاء | Cancel | Button (action) / Cancel (modal) |
| تعديل | Edit | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Vendor** (`docs/workflows/vendor.md`) — every PO references a vendor.
- **Goods Receipt** (`docs/workflows/goods-receipt.md`) — created from a Sent PO.
- **Vendor Bill** (`docs/workflows/vendor-bill.md`) — created from a Goods Receipt.
- **Purchases by Vendor Report** — KPI report fed by posted Vendor Bills.
- **AP Aging** — outstanding bills flow into aging buckets.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
