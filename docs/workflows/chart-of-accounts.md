# Workflow: Chart of Accounts (دليل الحسابات)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/Finance`.

---

## 1. Business Purpose (الغرض التجاري)

The **Chart of Accounts (CoA)** is the master list of every account the company uses to record financial transactions. Each account has a code, name, type (Asset / Liability / Equity / Revenue / Expense), normal balance (Debit / Credit), and an optional parent. The CoA is the anchor for:

- **Journal Entries** — every journal line references a CoA account.
- **General Ledger** — per-account history of all transactions.
- **Trial Balance** — summary of all account balances.
- **Financial Statements** — Income Statement, Balance Sheet, Cash Flow.

The CoA is the **skeleton of the entire accounting system** — without accounts, no journal entry, no invoice, no payment can be recorded.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can deactivate? |
|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ |

**Why roles matter:** CoA changes are high-impact (they affect every transaction). Only Admin and Accountant can modify.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the CoA
1. Open **دليل الحسابات** (`/finance/accounts`) from the sidebar.
2. The page loads the full CoA in a sortable table.
3. The CoA is displayed as a flat list with an indent column showing the parent-child hierarchy.
4. Each row shows: code, name, type (with color badge), normal balance, is postable, is active.

### 3.2 View an account
1. Click on a row.
2. The view page shows: full account details, parent account, child accounts, recent journal lines (general ledger view), current balance.

### 3.3 Create a new account
1. Click **حساب جديد** (top-right button).
2. Fill the form:
   - **Code** (required, unique, e.g. `5110`) — numeric convention by type:
     - 1xxx = Assets
     - 2xxx = Liabilities
     - 3xxx = Equity
     - 4xxx = Revenue
     - 5xxx = Expenses
   - **Name** (required) — Arabic account name.
   - **Description** (optional).
   - **Type** (required, dropdown) — Asset, Liability, Equity, Revenue, Expense.
   - **Parent Account** (optional, dropdown) — for sub-accounts (e.g. `5110 Sales` → parent `5100 Revenue`).
   - **Is Postable** (default true) — false for header accounts (used for grouping only).
3. The **Normal Balance** is auto-set based on Type (Debit for Asset/Expense, Credit for Liability/Equity/Revenue).
4. Click **حفظ**.

### 3.4 Edit an account
1. Open the account's view page, then click **تعديل**.
2. Edit any field except **Code** (immutable — referenced by journal lines).
3. Click **حفظ**.

### 3.5 Deactivate an account
1. Open the account's edit page.
2. Uncheck **نشط** and save.
3. The account is hidden from new journal entry dropdowns but remains in historical entries.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/finance/accounts`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/finance/accounts` | List all accounts | `AccountResponse[]` |
| `POST` | `/api/finance/accounts` | Create an account | `AccountResponse` (201) |

### Request body — `CreateAccountRequest`
```json
{
  "code": "5110",
  "name": "إيرادات المبيعات",
  "description": "إيرادات بيع البضاعة للعملاء",
  "type": "Revenue",
  "parentAccountId": "guid-of-5100",
  "isPostable": true
}
```

### Response — `AccountResponse`
```json
{
  "id": "guid",
  "code": "5110",
  "name": "إيرادات المبيعات",
  "description": "إيرادات بيع البضاعة للعملاء",
  "type": "Revenue",
  "normalBalance": "Credit",
  "parentAccountId": "guid-of-5100",
  "isPostable": true,
  "isActive": true
}
```

### Account types
| Type | Normal Balance | Code range (Libyan convention) | Examples |
|---|---|---|---|
| **Asset** (أصول) | Debit | 1xxx | 1210 Cash, 1230 AR, 1410 Inventory |
| **Liability** (خصوم) | Credit | 2xxx | 2110 AP, 2120 VAT Output |
| **Equity** (حقوق ملكية) | Credit | 3xxx | 3100 Capital, 3200 Retained Earnings |
| **Revenue** (إيرادات) | Credit | 4xxx | 4110 Sales, 4200 Service Revenue |
| **Expense** (مصروفات) | Debit | 5xxx | 5110 COGS, 5210 Salaries |

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/finance/accounts` | `app/(authenticated)/finance/accounts/page.tsx` | List (hierarchical) |
| `/finance/accounts/new` | `app/(authenticated)/finance/accounts/new/page.tsx` | Create form |
| `/finance/accounts/{id}` | `app/(authenticated)/finance/accounts/[id]/page.tsx` | View + GL |
| `/finance/accounts/{id}/edit` | `app/(authenticated)/finance/accounts/[id]/edit/page.tsx` | Edit form |

---

## 6. State Transitions (تحولات الحالة)

An account has only one state: **Active** or **Inactive**.

```
┌─────────┐  save(isActive=true)   ┌─────────┐
│ Inactive│ ──────────────────────▶│ Active  │
└─────────┘                         └─────────┘
     ▲                                  │
     │       save(isActive=false)       │
     └──────────────────────────────────┘
```

**Effect on other documents:**
- Inactive accounts **cannot be selected** in Journal Entry forms.
- Inactive accounts **remain visible** in historical reports (Trial Balance, GL).

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Duplicate code** | The backend rejects with `409 Conflict`. |
| **Code is locked** | Once created, `code` cannot be changed (it's the external reference). |
| **Type change** | Allowed but does not retroactively change the account's balance. Future journal lines will use the new type. |
| **Parent deletion** | Not allowed — accounts with children cannot be deleted. |
| **Account with journal lines** | Deactivation is allowed; existing journal lines remain. |
| **IsPostable = false** | The account cannot be used in journal lines; it's a header for grouping (e.g. `1000 Current Assets` → `1210 Cash`). |
| **Circular parent** | The system rejects (A cannot be parent of B if B is already an ancestor of A). |
| **Normal balance mismatch** | The system auto-sets based on Type. Manual override is not allowed. |
| **Account types** | The legacy numeric enum (`type: 1..5`) and the new string union (`type: 'Asset' | 'Liability' | ...`) are both supported. The new one is recommended for the demo pages. |
| **Cross-company** | The CoA is shared across all companies in the Holding (no per-company CoA). |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| دليل الحسابات | Chart of Accounts | Sidebar, page title |
| حساب جديد | New Account | Button |
| الكود | Code | Form label, table column |
| اسم الحساب | Account Name | Form label, table column |
| النوع | Type | Form label, table column |
| نوع الرصيد | Normal Balance | Form label |
| الحساب الأب | Parent Account | Form label |
| قابل للترحيل | Postable | Form label |
| نشط / غير نشط | Active / Inactive | Badge, form checkbox |
| أصول | Asset | Badge |
| خصوم | Liability | Badge |
| حقوق ملكية | Equity | Badge |
| إيرادات | Revenue | Badge |
| مصروفات | Expense | Badge |
| مدين | Debit | Badge |
| دائن | Credit | Badge |
| حفظ | Save | Button |
| تعديل | Edit | Button |
| إلغاء | Cancel | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Journal Entry** (`docs/workflows/journal-entry.md`) — every line references a CoA account.
- **Sales Invoice / Vendor Bill** — post creates a journal entry using CoA accounts.
- **Trial Balance** — aggregates all CoA balances.
- **Financial Statements** — Income Statement (Revenue + Expense), Balance Sheet (Asset + Liability + Equity).

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
