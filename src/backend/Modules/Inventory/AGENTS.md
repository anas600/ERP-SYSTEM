# 📦 src/backend/Modules/Inventory/AGENTS.md

> Inventory Module — 📋 Phase 2.

## شو فيه (مخطط)

- **Item** — منتج/صنف (SKU, name, unit, valuation method)
- **Warehouse** — مخزن
- **Stock Movement** — حركة مخزون (Receive, Issue, Transfer, Adjust)
- **Stock Level** — read model (CQRS projection): الكمية المتاحة لكل Item-Warehouse

## Planned Structure (Phase 2)

```
Inventory/
├── Entities/
│   ├── Item.cs
│   ├── Warehouse.cs
│   ├── StockMovement.cs
│   └── StockLevel.cs
├── Application/
│   ├── Items/
│   ├── Warehouses/
│   └── Movements/
└── Infrastructure/
    └── Repositories
```

## Event Integration (مستقبلي)

- **StockReceived** event → Finance ينشئ Journal Entry
  - مدين: Inventory Asset
  - دائن: Accounts Payable / Cash
- **ProjectMaterialRequested** → Inventory يحجز الكمية
- **StockIssued** → Finance يسجّل COGS

## Conventions

- يتبع نفس أنماط Identity + Finance
- `TenantId` على كل entity
- StockLevel = CQRS projection (لا نخزنه مباشرة، يُحسب من Movements)

## ملاحظة

ملفات `StockEvents.cs` موجودة في `Shared/Events/` حالياً — هذا الـ contracts المؤقتة.

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../Identity/AGENTS.md`](../Identity/AGENTS.md)
- [`../Projects/AGENTS.md`](../Projects/AGENTS.md)
- [`../../Shared/AGENTS.md`](../../Shared/AGENTS.md) — events
