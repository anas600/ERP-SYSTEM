# Workflows — ERP-SYSTEM

> **Audience:** Client stakeholders, future contributors, and the support team.
> **Sprint:** 19 (P0 docs) + 20 (P1 docs).
> **Purpose:** One document per function explaining the **business purpose**, **user roles**, **user journey**, **API contract**, **UI pages**, **state transitions**, **edge cases**, and **bilingual labels** — in language a non-developer can follow.

---

## P0 Functions (Client Demo — Sprint 19)

| # | Function | Arabic | Document | UI Path |
|---|---|---|---|---|
| 1 | **Customer** | العملاء | [`customer.md`](./customer.md) | `/finance/customers` |
| 2 | **Vendor** | الموردين | [`vendor.md`](./vendor.md) | `/procurement/vendors` |
| 3 | **Item** | الأصناف | [`item.md`](./item.md) | `/inventory/items` |
| 4 | **Sales Invoice** | فواتير المبيعات | [`sales-invoice.md`](./sales-invoice.md) | `/finance/sales-invoices` |

## P1 Functions (Sprint 20 — Demo 2)

| # | Function | Arabic | Document | UI Path |
|---|---|---|---|---|
| 5 | **Purchase Order** | أوامر الشراء | [`purchase-order.md`](./purchase-order.md) | `/procurement/purchase-orders` |
| 6 | **Goods Receipt** | استلامات البضاعة | [`goods-receipt.md`](./goods-receipt.md) | `/procurement/goods-receipts` |
| 7 | **Vendor Bill** | فواتير الموردين | [`vendor-bill.md`](./vendor-bill.md) | `/procurement/bills` |
| 8 | **Receipt (AR)** | سندات القبض | [`receipt.md`](./receipt.md) | `/finance/receipts` |
| 9 | **Chart of Accounts** | دليل الحسابات | [`chart-of-accounts.md`](./chart-of-accounts.md) | `/finance/accounts` |
| 10 | **Journal Entry** | قيود اليومية | [`journal-entry.md`](./journal-entry.md) | `/finance/journal-entries` |
| 11 | **Employee** | الموظفين | [`employee.md`](./employee.md) | `/hr/employees` |
| 12 | **Payroll Run** | دورة الرواتب | [`payroll-run.md`](./payroll-run.md) | `/hr/payroll` |
| 13 | **Project** | المشاريع | [`project.md`](./project.md) | `/projects` |

**Total coverage:** 13 of 13 demo-grade functions documented.

## How to use this directory

1. **For a client demo:** open the document for the function you want to demo. Each doc has a **User Journey** section that walks through the page step-by-step.
2. **For a new contributor:** each doc has a **User Roles** table (who can do what), **API Contract** (the HTTP endpoints), and **State Transitions** (the workflow).
3. **For support:** each doc has an **Edge Cases** table and a **Bilingual Labels** table for translating UI text.
4. **For the legal team:** the Sales Invoice and Vendor Bill docs have the most detail because they drive tax filings.

## Document template

Every workflow document follows the same 9-section template:

1. **Business Purpose** (2-3 sentences in Arabic + English)
2. **User Roles** (who can do what, with reasoning)
3. **User Journey** (step-by-step: open → list → action → result)
4. **API Contract** (HTTP methods, request/response bodies, error codes)
5. **UI Pages** (the Next.js app router paths)
6. **State Transitions** (ASCII diagram + rules)
7. **Edge Cases** (table of "what if X happens?")
8. **Bilingual Labels** (Arabic ↔ English UI text)
9. **Related Workflows** (cross-references)

## P2 Functions (post-Sprint 20 backlog)

These functions are implemented but not yet documented in this directory:

- Attendance (HR)
- Leave Request (HR)
- Department (HR)
- Cost Center (Finance)
- Posting Rules (Finance)
- Stock Movement (Inventory)
- Warehouse (Inventory)
- Item Category (Inventory)
- Unit of Measure (Inventory)
- User & Role (Identity)
- Audit Log (Admin)
- Holding & Company (Companies)
- Notification (cross-cutting)
- Activity Feed (Activity)

Documentation for P2 functions is planned for Sprint 21+.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
