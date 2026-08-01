# Workflow: Item (الأصناف)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 19 (Client Demo Sprint).
> **Backend module:** `Modules/Inventory`.

---

## 1. Business Purpose (الغرض التجاري)

The **Item** function manages the products and raw materials the company buys, sells, or stocks. Each item record carries SKU, name, unit of measure, costing method, reorder thresholds, and GL account mappings. The item is the anchor for:

- **Stock Movements** (حركات المخزون) — receipts, issues, adjustments.
- **Sales Invoice Lines** — what was sold.
- **Purchase Order Lines** — what was ordered.
- **Goods Receipt Lines** — what was received.
- **Inventory Valuation Report** — total stock value per item.

Without an item record, you cannot sell, buy, or stock anything.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can deactivate? |
|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ |
| **Inventory Clerk** (أمين مخزن) | ✅ | ✅ | ✅ | ❌ |
| **Sales** (مبيعات) | ✅ | ❌ | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ |

**Why roles matter:** Only Admin and Accountant can deactivate an item. Deactivation is a soft delete that hides the item from new transactions but keeps history. Inventory Clerks can create and edit but not deactivate, so they cannot accidentally remove a record referenced by posted invoices or POs.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **الأصناف** (`/inventory/items`) from the sidebar.
2. The page loads all items (active and inactive) in a sortable table.
3. Use the search box to filter by name, SKU, or barcode.
4. Each row shows: SKU, name, category, unit, costing method, average cost, stock level, reorder info, status.

### 3.2 Create a new item
1. Click **صنف جديد** (top-right button).
2. Fill the form:
   - **SKU** (required, unique, e.g. `ITEM-001`) — auto-suggested but editable.
   - **Barcode** (optional) — for barcode scanner integration.
   - **Name** (required) — Arabic item name.
   - **Description** (optional) — for the invoice line.
   - **Category** (optional, dropdown) — item category (e.g. "مواد غذائية", "مواد تنظيف").
   - **Unit of Measure** (required, dropdown) — e.g. "كيلو", "لتر", "قطعة", "كرتون".
   - **Item Type** (default Raw Material) — Raw Material, Finished Good, Service, etc.
   - **Costing Method** (default Average) — Average, Standard, FIFO.
   - **Standard Cost** (default 0) — LYD per unit, used if Costing Method = Standard.
   - **Reorder Level** (default 0) — when stock falls below this, the system raises a low-stock notification.
   - **Reorder Quantity** (default 0) — suggested reorder amount.
3. Click **حفظ** (Save).
4. The new item appears in the list and is selectable for new invoices and POs.

### 3.3 View an item
1. Click on a row in the list (or open `/inventory/items/{id}`).
2. The view page shows:
   - All fields read-only.
   - **Current stock level** across all warehouses.
   - **Recent stock movements** (last 20).
   - **Average cost** (computed).
3. From the view page you can:
   - Click **تعديل** to open the edit form.
   - Click **رجوع** to go back to the list.

### 3.4 Edit an item
1. Open the item's view page, then click **تعديل**.
2. The same form as create, pre-filled.
3. Edit any field except **SKU** (SKU is immutable — it's the external reference).
4. Click **حفظ**.

### 3.5 Deactivate an item
1. Open the item's edit page.
2. Uncheck **نشط** (Active) and save.
3. The item is hidden from new transaction dropdowns but remains visible in historical reports.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/inventory/items`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/inventory/items` | List all items (active + inactive) | `ItemResponse[]` |
| `GET` | `/api/inventory/items/{id}` | Get one item | `ItemResponse` |
| `POST` | `/api/inventory/items` | Create an item | `ItemResponse` (201) |
| `PUT` | `/api/inventory/items/{id}` | Update an item (incl. active flag) | `ItemResponse` |

### Supporting endpoints (for dropdowns in the form)
| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/inventory/categories` | List item categories |
| `GET` | `/api/inventory/units` | List units of measure |
| `GET` | `/api/inventory/warehouses` | List warehouses |

### Request body — `CreateItemRequest`
```json
{
  "companyId": "guid",
  "sku": "ITEM-001",
  "barcode": "6251234567890",
  "name": "أرز بسمتي 5 كغ",
  "description": "أرز بسمتي فاخر - عبوة 5 كغ",
  "categoryId": "guid-or-null",
  "unitOfMeasureId": "guid",
  "itemType": 1,
  "costingMethod": 1,
  "standardCost": 25.00,
  "reorderLevel": 10,
  "reorderQuantity": 50
}
```

### Response — `ItemResponse`
```json
{
  "id": "guid",
  "companyId": "guid",
  "sku": "ITEM-001",
  "barcode": "6251234567890",
  "name": "أرز بسمتي 5 كغ",
  "description": "أرز بسمتي فاخر - عبوة 5 كغ",
  "categoryId": "guid-or-null",
  "unitOfMeasureId": "guid",
  "itemType": 1,
  "costingMethod": 1,
  "averageCost": 25.00,
  "standardCost": 25.00,
  "reorderLevel": 10,
  "reorderQuantity": 50,
  "isActive": true
}
```

### Error codes
- `400 Bad Request` — missing required field, duplicate SKU, invalid UoM ID.
- `404 Not Found` — item ID does not exist.
- `409 Conflict` — SKU already used by another item.

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/inventory/items` | `app/(authenticated)/inventory/items/page.tsx` | List + search + filter |
| `/inventory/items/new` | `app/(authenticated)/inventory/items/new/page.tsx` | Create form |
| `/inventory/items/{id}` | `app/(authenticated)/inventory/items/[id]/page.tsx` | View (read-only) |
| `/inventory/items/{id}/edit` | `app/(authenticated)/inventory/items/[id]/edit/page.tsx` | Edit form |

---

## 6. State Transitions (تحولات الحالة)

An item has only one state: **Active** or **Inactive**.

```
┌─────────┐  save(isActive=true)   ┌─────────┐
│ Inactive│ ──────────────────────▶│ Active  │
└─────────┘                         └─────────┘
     ▲                                  │
     │       save(isActive=false)       │
     └──────────────────────────────────┘
```

**Effect on other documents:**
- Inactive items **cannot be selected** in the Sales Invoice or Purchase Order forms.
- Inactive items **remain visible** in historical invoices, POs, and stock movements.
- Inactive items **can be reactivated** at any time.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Duplicate SKU** | The backend rejects with `409 Conflict`. The form shows "هذا الـ SKU مستخدم". |
| **Item has open stock** | Deactivation is allowed; existing stock remains. Reactivation brings it back. |
| **Item is in a posted invoice** | Deactivation does not affect posted invoices. The item remains visible in the invoice line for historical reference. |
| **SKU is locked** | Once an item is created, the `SKU` field cannot be changed (it is the external reference for invoices, POs, and reports). |
| **Reorder level is reached** | The system creates a `LowStock` notification for users with access to the item's company. See `docs/workflows/notification.md` (post-Sprint 19). |
| **Unit of Measure change** | Changing the UoM is allowed but does not convert existing stock. Use Stock Adjustment to convert manually. |
| **Costing method change** | The costing method is captured at the item level. Changing it does not retroactively recalculate historical costs. |
| **Empty optional fields** | Barcode, description, category, reorder level, reorder quantity are all optional. The item is still valid. |
| **Search is case-insensitive** | Searching `item` matches `ITEM`, `Item`, and `صنف`. |
| **Cross-company** | Items are scoped to the active company. Switching companies via the company switcher shows a different list. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| الأصناف | Items | Sidebar, page title |
| صنف جديد | New Item | Button |
| كود الصنف (SKU) | SKU | Form label, table column |
| الباركود | Barcode | Form label |
| اسم الصنف | Item Name | Form label, table column |
| الفئة | Category | Form label, table column |
| وحدة القياس | Unit of Measure | Form label, table column |
| طريقة التكلفة | Costing Method | Form label |
| متوسط التكلفة | Average Cost | Table column |
| حد إعادة الطلب | Reorder Level | Form label |
| كمية إعادة الطلب | Reorder Quantity | Form label |
| نشط / غير نشط | Active / Inactive | Badge, form checkbox |
| حفظ | Save | Button |
| تعديل | Edit | Button |
| إلغاء | Cancel | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Sales Invoice** — every invoice line references an item (optional, can be free-form).
- **Purchase Order** — every PO line references an item.
- **Goods Receipt** — every GR line references an item.
- **Stock Movement** — every movement references an item and a warehouse.
- **Inventory Valuation Report** — total stock value per item across all warehouses.
- **Low-Stock Notification** — automatic when stock falls below reorder level.

---

_Last updated: 2026-08-01 — Sprint 19 (Client Demo Sprint)._
