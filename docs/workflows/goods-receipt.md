# Workflow: Goods Receipt (استلامات البضاعة)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/Procurement`.

---

## 1. Business Purpose (الغرض التجاري)

The **Goods Receipt (GR)** function records what was actually received from a vendor against a Purchase Order. Each GR carries the source PO, the warehouse, the received date, and the actual quantities (which may differ from the PO). The GR is the anchor for:

- **Stock Movements** — receipts increase on-hand quantity.
- **Vendor Bills** — bills are created from a GR (not directly from a PO).
- **Inventory Valuation** — average cost updates from received goods.
- **Procurement Reports** — PO vs. GR variance.

Without a GR, the system cannot track what was actually delivered vs. what was ordered, and stock quantities stay out of sync.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can post? | Can cancel? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Inventory Clerk** (أمين مخزن) | ✅ | ✅ | ✅ (Draft) | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:** Only Admin and Accountant can post a GR (posting creates the stock movement + updates inventory). Inventory Clerk creates the draft but must hand off to accounting for posting.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **استلامات البضاعة** (`/procurement/goods-receipts`) from the sidebar.
2. The page loads all GRs in a sortable table.
3. Each row shows: GR number, PO number, vendor, received date, warehouse, status.

### 3.2 Create a new GR (from a PO)
1. Open a `Sent` PO's view page.
2. Click **استلام بضاعة** (Create Goods Receipt).
3. The new GR form is pre-filled with:
   - Source PO (locked)
   - Vendor (auto from PO)
   - Warehouse (selectable)
   - Default received date = today
4. Edit the **lines**: actual received quantity per item (may be less than PO quantity for partial receipts).
5. Click **حفظ كمسودة** (Save as Draft) — GR is in `Draft` status.
6. Click **ترحيل** (Post) — GR is posted, stock movements are created, status → `Received`.

### 3.3 View a GR
1. Click on a row in the list.
2. The view page shows: GR header, source PO, vendor, warehouse, lines (PO quantity vs. received quantity), notes.
3. From the view page you can:
   - Click **تعديل** (if Draft) to edit.
   - Click **إنشاء فاتورة مورّد** to create a Vendor Bill from this GR.
   - Click **إلغاء** to cancel.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/procurement/grs`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/procurement/grs` | List all GRs | `GoodsReceiptResponse[]` |
| `GET` | `/api/procurement/grs/{id}` | Get one GR | `GoodsReceiptResponse` |
| `POST` | `/api/procurement/grs` | Create a GR (from a PO) | `GoodsReceiptResponse` (201) |

### Request body — `CreateGoodsReceiptRequest`
```json
{
  "purchaseOrderId": "guid",
  "warehouseId": "guid",
  "receivedDate": "2026-08-05T00:00:00Z",
  "notes": "استلام كامل حسب الأمر",
  "lines": [
    {
      "itemId": "guid",
      "quantity": 50,
      "notes": "صناديق سليمة"
    }
  ]
}
```

### Response — `GoodsReceiptResponse`
```json
{
  "id": "guid",
  "grNumber": "GR-2026-0001",
  "purchaseOrderId": "guid",
  "poNumber": "PO-2026-0001",
  "poStatus": "Sent",
  "vendorName": "مورد البضائع العامة",
  "vendorId": "guid",
  "vendorCode": "VEND-001",
  "status": 1,
  "receivedDate": "2026-08-05T00:00:00Z",
  "warehouseId": "guid",
  "warehouseName": "المخزن الرئيسي",
  "warehouseCode": "WH-001",
  "notes": "استلام كامل حسب الأمر",
  "currency": "LYD",
  "lines": [
    {
      "id": "guid",
      "itemId": "guid",
      "itemName": "أرز بسمتي 5 كغ",
      "quantity": 50,
      "notes": "صناديق سليمة"
    }
  ],
  "createdAt": "2026-08-05T10:00:00Z"
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | مسودة | Draft |
| 2 | مُستلَم | Received |
| 3 | ملغي | Cancelled |

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/procurement/goods-receipts` | `app/(authenticated)/procurement/goods-receipts/page.tsx` | List |
| `/procurement/goods-receipts/new` | `app/(authenticated)/procurement/goods-receipts/new/page.tsx` | Create (pre-fills from PO) |
| `/procurement/goods-receipts/{id}` | `app/(authenticated)/procurement/goods-receipts/[id]/page.tsx` | View |

---

## 6. State Transitions (تحولات الحالة)

```
                  save (from PO)
    ┌─────────┐ ───────────────────▶ ┌─────────┐
    │  (new)  │                       │  Draft  │ ◀─── update (Draft only)
    └─────────┘                       └─────────┘
                                         │
                                         │ post
                                         ▼
                                    ┌─────────┐ ──── Bill created ────▶ (stays Received)
                                    │Received │
                                    └─────────┘
                                         │
                                         │ cancel (Draft only)
                                         ▼
                                    ┌─────────┐
                                    │Cancelled│
                                    └─────────┘
```

**Important rules:**
- A GR is always created from a Sent PO (the form pre-fills the PO lines).
- `Draft` is editable.
- `Received` (after post) is terminal for stock; you cannot edit a received GR — you must reverse it via a Stock Movement.
- `Cancel` only works in `Draft` status.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **GR quantity > PO quantity** | The system warns but allows (the vendor may have shipped extra). The variance is logged. |
| **Partial receipt** | Allowed. The remaining PO quantity stays open. Multiple GRs can be created against one PO. |
| **GR for a non-Sent PO** | The system rejects (a PO must be Sent before a GR can be created). |
| **GR for a Cancelled PO** | The system rejects. |
| **GR for a PO with a Bill** | The system warns (you may be double-receiving). |
| **Warehouse inactive** | The warehouse dropdown only shows active warehouses. |
| **Item inactive** | The item is still allowed (you may be receiving discontinued stock). |
| **Edit after Received** | The form is read-only. To fix, create a Stock Adjustment. |
| **Multiple GRs per PO** | Supported. Common for partial deliveries over time. |
| **GR number generation** | Auto-generated as `GR-YYYY-NNNN`. |
| **Cross-company** | GRs are scoped to the active company. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| استلامات البضاعة | Goods Receipts | Sidebar, page title |
| استلام جديد | New Receipt | Button |
| رقم الاستلام | GR Number | Table column |
| أمر الشراء | Purchase Order | Form label, table column |
| المورّد | Vendor | Table column |
| تاريخ الاستلام | Received Date | Form label |
| المخزن | Warehouse | Form label, table column |
| الكمية المُستلمة | Received Quantity | Form label, table column |
| الكمية المطلوبة | PO Quantity | Form label (read-only) |
| الفرق | Variance | Table column (PO - GR) |
| مسودة | Draft | Badge |
| مُستلَم | Received | Badge |
| ملغي | Cancelled | Badge |
| ترحيل | Post | Button |
| إنشاء فاتورة مورّد | Create Vendor Bill | Button |
| إلغاء | Cancel | Button |
| تعديل | Edit | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Purchase Order** (`docs/workflows/purchase-order.md`) — GR is created from a Sent PO.
- **Vendor Bill** (`docs/workflows/vendor-bill.md`) — Bill is created from a Received GR.
- **Item / Stock Movement** — GR triggers a stock movement (in) and updates average cost.
- **Inventory Valuation Report** — average cost uses the most recent GR price.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
