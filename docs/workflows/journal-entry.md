# Workflow: Journal Entry (قيود اليومية)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/Finance`.

---

## 1. Business Purpose (الغرض التجاري)

The **Journal Entry (JE)** is the atomic record of any financial transaction in the system. Every posted invoice, bill, payment, or adjustment creates a journal entry with one or more lines (each line is a Dr or Cr to a CoA account). The journal entry is the **source of truth** for all financial reporting.

- **General Ledger (دفتر الأستاذ)** — every JE line is recorded per account.
- **Trial Balance (ميزان المراجعة)** — sum of all lines per account.
- **Financial Statements (القوائم المالية)** — Income Statement, Balance Sheet, Cash Flow.
- **Audit Trail** — every financial event has a JE ID.

A balanced journal entry (total Debit = total Credit) is the **fundamental equation of accounting**.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can post? | Can reverse? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ (Draft) | ✅ | ✅ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:** Manual JEs are powerful and risky. Only Admin and Accountant can post. Most JEs are created **automatically** by Sales Invoices, Vendor Bills, Receipts, etc. — manual JEs are for adjustments only.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **قيود اليومية** (`/finance/journal-entries`) from the sidebar.
2. The page loads all JEs in a sortable table.
3. Each row shows: entry number, date, description, reference, total, status (Draft/Posted/Reversed).

### 3.2 Create a new manual JE (adjustment)
1. Click **قيد جديد** (top-right button).
2. Fill the form:
   - **Entry Date** (default today).
   - **Description** (required) — e.g. "تسوية جرد".
   - **Reference** (optional) — external reference (e.g. inventory count sheet).
   - **Lines** (at least 2) — each line has:
     - **Account** (required, dropdown) — from CoA.
     - **Debit** (≥ 0).
     - **Credit** (≥ 0).
     - **Description** (optional) — line description.
3. The form auto-validates: **total Debit = total Credit** (must be balanced).
4. The system shows the imbalance (in red) if any.
5. Click **حفظ كمسودة** (Save as Draft).
6. Click **ترحيل** (Post) — JE is posted, lines hit the GL.

### 3.3 View a JE
1. Click on a row.
2. The view page shows: header, all lines, totals, status, posted-at, journal entry ID.
3. From the view page you can:
   - Click **تعديل** (if Draft) to edit.
   - Click **عكس** (if Posted) to reverse.

### 3.4 Automatic JEs (most common)
The system creates JEs **automatically** for:
- **Sales Invoice Post** → Dr 1230 AR / Cr 5110 Sales + Cr 2120 VAT Output
- **Vendor Bill Post** → Dr 1410 Inventory / Cr 2110 AP + Dr 1411 VAT Input
- **Receipt Post** → Dr 1210 Cash (or Bank) / Cr 1230 AR
- **Payment (AP)** → Dr 2110 AP / Cr 1210 Cash (or Bank)
- **Payroll Post** → Dr 5210 Salaries Expense / Cr 2210 Salaries Payable + liabilities

These JEs are **read-only** (created by the system, not editable).

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/finance/journal-entries`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/finance/journal-entries` | List all JEs | `JournalEntryResponse[]` |
| `POST` | `/api/finance/journal-entries` | Create a manual JE (Draft) | `JournalEntryResponse` (201) |
| `PUT` | `/api/finance/journal-entries/{id}/post` | Post a Draft JE | `JournalEntryResponse` |
| `PUT` | `/api/finance/journal-entries/{id}/reverse` | Reverse a Posted JE | `JournalEntryResponse` |

### Request body — `PostJournalEntryRequest`
```json
{
  "entryDate": "2026-08-15T00:00:00Z",
  "description": "تسوية جرد شهر أغسطس",
  "reference": "INV-COUNT-2026-08",
  "lines": [
    {
      "accountId": "guid-cash",
      "debit": 0,
      "credit": 250.00,
      "description": "عجز في الصندوق"
    },
    {
      "accountId": "guid-inventory-shrinkage",
      "debit": 250.00,
      "credit": 0,
      "description": "تسجيل العجز كمصروف"
    }
  ]
}
```

### Response — `JournalEntryResponse`
```json
{
  "id": "guid",
  "entryNumber": "JE-2026-0001",
  "entryDate": "2026-08-15T00:00:00Z",
  "description": "تسوية جرد شهر أغسطس",
  "reference": "INV-COUNT-2026-08",
  "status": 1,
  "postedAt": null,
  "lines": [
    {
      "lineNumber": 1,
      "accountId": "guid-cash",
      "accountCode": "1210",
      "accountName": "الصندوق",
      "debit": 0,
      "credit": 250.00,
      "description": "عجز في الصندوق"
    },
    {
      "lineNumber": 2,
      "accountId": "guid-inventory-shrinkage",
      "accountCode": "5290",
      "accountName": "عجز المخزون",
      "debit": 250.00,
      "credit": 0,
      "description": "تسجيل العجز كمصروف"
    }
  ],
  "totalDebit": 250.00,
  "totalCredit": 250.00
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | مسودة | Draft |
| 2 | مُرحَّل | Posted |
| 3 | معكوس | Reversed |

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/finance/journal-entries` | `app/(authenticated)/finance/journal-entries/page.tsx` | List |
| `/finance/journal-entries/new` | `app/(authenticated)/finance/journal-entries/new/page.tsx` | Create form |
| `/finance/journal-entries/{id}` | `app/(authenticated)/finance/journal-entries/[id]/page.tsx` | View (read-only after posted) |

---

## 6. State Transitions (تحولات الحالة)

```
                  save
    ┌─────────┐ ────────▶ ┌─────────┐
    │  (new)  │            │  Draft  │ ◀─── update (Draft only)
    └─────────┘            └─────────┘
                                │
                                │ post (validates balance)
                                ▼
                           ┌─────────┐
                           │ Posted  │ ──── reverse ────▶ ┌─────────┐
                           └─────────┘                    │Reversed │
                                                         └─────────┘
```

**Important rules:**
- **Balance is mandatory**: Total Debit must equal Total Credit.
- Only `Draft` is editable.
- `Posted` lines hit the General Ledger.
- `Reversed` is terminal — creates a reversing JE with opposite lines.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Imbalanced (Dr ≠ Cr)** | The form rejects save. Backend rejects with `400`. |
| **Empty lines** | The form rejects. |
| **Single line** | Allowed (e.g. one side has 0, other side has the amount). The form auto-balances. |
| **Account is non-postable** | The form rejects (cannot use a header account in a line). |
| **Account is inactive** | The form rejects. |
| **Edit after Posted** | The form is read-only. To fix, reverse and create a new JE. |
| **Reverse a reversed JE** | The system rejects (already reversed). |
| **Auto-created JE** | Read-only. Cannot be edited. To fix, post an offsetting JE. |
| **Entry date in the future** | Allowed but warns (unusual for accounting). |
| **Entry date too old** | The system warns if older than 1 year (closing period may be locked). |
| **Cross-company** | JEs are scoped to the active company. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| قيود اليومية | Journal Entries | Sidebar, page title |
| قيد جديد | New Entry | Button |
| رقم القيد | Entry Number | Table column |
| التاريخ | Date | Form label, table column |
| الوصف | Description | Form label, table column |
| المرجع | Reference | Form label, table column |
| الحساب | Account | Form label |
| مدين | Debit | Form label, table column |
| دائن | Credit | Form label, table column |
| إجمالي المدين | Total Debit | Form label |
| إجمالي الدائن | Total Credit | Form label |
| مسودة | Draft | Badge |
| مُرحَّل | Posted | Badge |
| معكوس | Reversed | Badge |
| حفظ كمسودة | Save as Draft | Button |
| ترحيل | Post | Button |
| عكس | Reverse | Button |
| تعديل | Edit | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Chart of Accounts** (`docs/workflows/chart-of-accounts.md`) — every line references a CoA account.
- **Sales Invoice** — posting creates a JE.
- **Vendor Bill** — posting creates a JE.
- **Receipt** — posting creates a JE.
- **General Ledger** — view of all JE lines per account.
- **Trial Balance** — summary of all account balances.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
