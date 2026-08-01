# Workflows — ERP-SYSTEM

> **Audience:** Client stakeholders, future contributors, and the support team.
> **Sprint:** 19 (Client Demo Sprint).
> **Purpose:** One document per P0 function explaining the **business purpose**, **user roles**, **user journey**, **API contract**, **UI pages**, **state transitions**, **edge cases**, and **bilingual labels** — in language a non-developer can follow.

---

## P0 Functions (Client Demo — Sprint 19)

| # | Function | Arabic | Document | UI Path |
|---|---|---|---|---|
| 1 | **Customer** | العملاء | [`customer.md`](./customer.md) | `/finance/customers` |
| 2 | **Vendor** | الموردين | [`vendor.md`](./vendor.md) | `/procurement/vendors` |
| 3 | **Item** | الأصناف | [`item.md`](./item.md) | `/inventory/items` |
| 4 | **Sales Invoice** | فواتير المبيعات | [`sales-invoice.md`](./sales-invoice.md) | `/finance/sales-invoices` |

## How to use this directory

1. **For a client demo:** open the document for the function you want to demo. Each doc has a **User Journey** section that walks through the page step-by-step.
2. **For a new contributor:** each doc has a **User Roles** table (who can do what), **API Contract** (the HTTP endpoints), and **State Transitions** (the workflow).
3. **For support:** each doc has an **Edge Cases** table and a **Bilingual Labels** table for translating UI text.
4. **For the legal team:** the Sales Invoice doc has the most detail because invoices drive tax filings.

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

## P1 Functions (post-Sprint 19 backlog)

These functions are implemented but not yet documented in this directory:

- Purchase Order
- Goods Receipt
- Vendor Bill
- Receipt (AR)
- Journal Entry
- Chart of Accounts
- Employee
- Payroll Run
- Project

Documentation for P1 functions is planned for Sprint 20+.

---

_Last updated: 2026-08-01 — Sprint 19 (Client Demo Sprint)._
