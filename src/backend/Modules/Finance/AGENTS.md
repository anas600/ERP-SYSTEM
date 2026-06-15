# 💰 src/backend/Modules/Finance/AGENTS.md

> Finance Module — 📋 Phase 1 (لم يبدأ بعد).

## شو فيه (مخطط)

- **Chart of Accounts** — CoA tree (Assets, Liabilities, Equity, Revenue, Expenses)
- **Journal Entries** — Double-entry bookkeeping
- **General Ledger** — Read model للقيود
- **Invoices & Payments** — Customer/Vendor
- **Rules Engine** — Auto-posting (مثال: StockReceived → InventoryAsset)

## Planned Structure (Phase 1)

```
Finance/
├── Entities/
│   ├── Account.cs              # CoA
│   ├── JournalEntry.cs         # رأس القيد
│   ├── JournalLine.cs          # سطور القيد (debit/credit)
│   ├── Customer.cs
│   ├── Vendor.cs
│   ├── Invoice.cs
│   └── Payment.cs
├── Application/
│   ├── Accounts/
│   ├── JournalEntries/
│   ├── Invoices/
│   ├── Payments/
│   └── Rules/
└── Infrastructure/
    └── Repositories
```

## Conventions (مستوحاة من Identity)

- `TenantId` على كل entity
- Dapper + FluentMigrator
- Pub/Sub events عبر MartenDB
- 2-step validation: FluentValidation + double-entry check

## لما يبدأ Phase 1

1. اقرأ [`../Identity/AGENTS.md`](../Identity/AGENTS.md) لـ patterns
2. أنشئ `2026XXXX_CreateFinanceTables.cs` migration
3. ابدأ بـ Chart of Accounts (أبسط aggregate)

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../Identity/AGENTS.md`](../Identity/AGENTS.md) — أنماط متبعة
- [`../../Shared/AGENTS.md`](../../Shared/AGENTS.md)
