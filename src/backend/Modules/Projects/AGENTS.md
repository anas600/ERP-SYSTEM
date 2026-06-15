# 📊 src/backend/Modules/Projects/AGENTS.md

> Projects Module — 📋 Phase 2.

## شو فيه (مخطط)

- **Project** — مشروع (اسم، عميل، ميزانية، تاريخ بدء/انتهاء)
- **Task** — مهمة داخل مشروع (تعيين، حالة، تقدير vs فعلي)
- **Resource** — موارد (موظفون / معدات)
- **Project Budget** — ميزانية مرتبطة بـ Finance module
- **Time tracking** — اختياري للمرحلة الأولى

## Planned Structure (Phase 2)

```
Projects/
├── Entities/
│   ├── Project.cs
│   ├── Task.cs
│   ├── Resource.cs
│   └── ProjectBudget.cs
├── Application/
│   ├── Projects/
│   ├── Tasks/
│   └── Resources/
└── Infrastructure/
    └── Repositories
```

## Event Integration (مستقبلي)

- `ProjectMaterialRequested` → Inventory يحجز الكمية
- `ResourceAssigned` → Finance يحتسب التكلفة
- يُنشر عبر `Shared/Events/`

## Conventions

- يتبع نفس أنماط Identity + Finance
- `TenantId` على كل entity
- Task تتبع Project (FK) — Cascade delete

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../Identity/AGENTS.md`](../Identity/AGENTS.md)
- [`../Inventory/AGENTS.md`](../Inventory/AGENTS.md) — التكامل المستقبلي
