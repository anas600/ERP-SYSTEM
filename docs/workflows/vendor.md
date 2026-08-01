# Workflow: Vendor (الموردين)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 19 (Client Demo Sprint).
> **Backend module:** `Modules/Procurement`.

---

## 1. Business Purpose (الغرض التجاري)

The **Vendor** function manages every party the company buys from on credit. Each vendor record carries contact details, tax information, currency, and payment terms. The vendor is the anchor for:

- **Purchase Orders** (أوامر الشراء) — what the company ordered.
- **Goods Receipts** (استلامات البضاعة) — what was actually received.
- **Vendor Bills** (فواتير الموردين) — what the company owes.
- **AP Aging** (أعمار الذمم الدائنة) — overdue buckets per vendor.
- **Reports** — Purchases by Vendor, Top Vendors, Vendor Statement.

Without a vendor record, you cannot issue a purchase order or track payables.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can deactivate? | Can view statement? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Procurement** (مشتريات) | ✅ | ✅ | ✅ | ❌ | ✅ (read-only) |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ✅ |

**Why roles matter:** Only Admin and Accountant can deactivate a vendor. Deactivation is a soft delete that hides the vendor from new POs but keeps history. Procurement can create and edit but not deactivate.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **الموردين** (`/procurement/vendors`) from the sidebar.
2. The page loads all vendors (active and inactive) in a sortable table.
3. Use the search box to filter by name, code, or tax number.
4. Each row shows: code, name, contact info, currency, payment terms, status.

### 3.2 Create a new vendor
1. Click **مورد جديد** (top-right button).
2. Fill the form:
   - **Code** (required, unique, e.g. `VEND-001`) — auto-suggested but editable.
   - **Name** (required) — supplier company name.
   - **Email / Phone / Address** (optional but recommended).
   - **Tax Number** (optional) — Libyan tax number.
   - **Website** (optional) — vendor's website.
   - **Currency** (default LYD) — currency the vendor invoices in.
   - **Payment Terms** (default Net30) — payment terms string (Cash, Net15, Net30, Net60, Net90).
3. Click **حفظ** (Save).
4. The new vendor appears in the list and is selectable for new POs.

### 3.3 View a vendor
1. Click on a row in the list (or open `/procurement/vendors/{id}`).
2. The view page shows:
   - All fields read-only.
   - A list of **recent purchase orders** for this vendor.
   - A list of **recent goods receipts**.
   - A list of **recent vendor bills**.
   - **Outstanding balance** (computed).
3. From the view page you can:
   - Click **تعديل** to open the edit form.
   - Click **رجوع** to go back to the list.

### 3.4 Edit a vendor
1. Open the vendor's view page, then click **تعديل**.
2. The same form as create, pre-filled.
3. Edit any field except **Code** (code is immutable — it's the external reference).
4. Click **حفظ**.

### 3.5 Deactivate a vendor
1. Open the vendor's edit page.
2. Uncheck **نشط** (Active) and save.
3. The vendor is hidden from new PO dropdowns but remains visible in historical reports.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/procurement/vendors`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/procurement/vendors` | List all vendors (active + inactive) | `VendorResponse[]` |
| `GET` | `/api/procurement/vendors/{id}` | Get one vendor | `VendorResponse` |
| `POST` | `/api/procurement/vendors` | Create a vendor | `VendorResponse` (201) |
| `PUT` | `/api/procurement/vendors/{id}` | Update a vendor (incl. active flag) | `VendorResponse` |

> **Note:** Vendors are deactivated via `PUT` with `isActive: false` (no separate DELETE endpoint — this preserves referential integrity with POs and bills).

### Request body — `CreateVendorRequest`
```json
{
  "code": "VEND-001",
  "name": "مورد البضائع العامة",
  "email": "sales@vendor.ly",
  "phone": "+218 92 7654321",
  "address": "شارع الشط، بنغازي",
  "taxNumber": "987654321",
  "website": "https://vendor.ly",
  "currency": "LYD",
  "paymentTerms": "Net30"
}
```

### Response — `VendorResponse`
```json
{
  "id": "guid",
  "code": "VEND-001",
  "name": "مورد البضائع العامة",
  "email": "sales@vendor.ly",
  "phone": "+218 92 7654321",
  "address": "شارع الشط، بنغازي",
  "taxNumber": "987654321",
  "website": "https://vendor.ly",
  "currency": "LYD",
  "paymentTerms": "Net30",
  "isActive": true
}
```

### Error codes
- `400 Bad Request` — missing required field, duplicate code, invalid email.
- `404 Not Found` — vendor ID does not exist.
- `409 Conflict` — code already used by another vendor.

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/procurement/vendors` | `app/(authenticated)/procurement/vendors/page.tsx` | List + search + filter |
| `/procurement/vendors/new` | `app/(authenticated)/procurement/vendors/new/page.tsx` | Create form |
| `/procurement/vendors/{id}` | `app/(authenticated)/procurement/vendors/[id]/page.tsx` | View (read-only) |
| `/procurement/vendors/{id}/edit` | `app/(authenticated)/procurement/vendors/[id]/edit/page.tsx` | Edit form |

---

## 6. State Transitions (تحولات الحالة)

A vendor has only one state: **Active** or **Inactive**. There is no workflow approval.

```
┌─────────┐  save(isActive=true)   ┌─────────┐
│ Inactive│ ──────────────────────▶│ Active  │
└─────────┘                         └─────────┘
     ▲                                  │
     │       save(isActive=false)       │
     └──────────────────────────────────┘
```

**Effect on other documents:**
- Inactive vendors **cannot be selected** in the Purchase Order form.
- Inactive vendors **remain visible** in historical POs, GRs, and bills.
- Inactive vendors **can be reactivated** at any time.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Duplicate code** | The backend rejects with `409 Conflict`. The form shows "هذا الكود مستخدم". |
| **Vendor has open POs** | Deactivation is allowed; existing POs remain unaffected. |
| **Vendor has open bills** | Same — deactivation does not touch posted bills. |
| **Code is locked** | Once a vendor is created, the `code` field cannot be changed (it is the external reference for POs, GRs, bills, and reports). |
| **Currency change** | Currency is captured per-vendor. Historical bills retain the currency at the time of posting. New POs use the new currency. |
| **Payment terms change** | Payment terms is a free-form string (`Cash`, `Net15`, `Net30`, `Net60`, `Net90`, or any custom value). The system does not validate it. |
| **Empty optional fields** | Email, phone, address, tax number, website are all optional. The vendor is still valid. |
| **Search is case-insensitive** | Searching `vendor` matches `Vendor` and `مورد`. |
| **Cross-company** | Vendors are scoped to the active company. Switching companies via the company switcher shows a different list. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| الموردين | Vendors | Sidebar, page title |
| مورد جديد | New Vendor | Button |
| كود المورد | Vendor Code | Form label |
| اسم المورد | Vendor Name | Form label |
| الرقم الضريبي | Tax Number | Form label |
| شروط الدفع | Payment Terms | Form label, table column |
| العملة | Currency | Form label, table column |
| نشط / غير نشط | Active / Inactive | Badge, form checkbox |
| حفظ | Save | Button |
| تعديل | Edit | Button |
| إلغاء | Cancel | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Purchase Order** (`docs/workflows/purchase-order.md`) — every PO references a vendor.
- **Goods Receipt** — every GR is linked to a PO (and thus a vendor).
- **Vendor Bill** — every bill is linked to a GR (and thus a vendor).
- **AP Aging** — the aging report is grouped by vendor.
- **Purchases by Vendor Report** — KPI report fed by vendor bills.

---

_Last updated: 2026-08-01 — Sprint 19 (Client Demo Sprint)._
