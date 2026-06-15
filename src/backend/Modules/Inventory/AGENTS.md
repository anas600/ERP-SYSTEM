# 📦 src/backend/Modules/Inventory/AGENTS.md

> Inventory Module — ✅ Phase 2.2 (مكتمل)

## شو فيه

```
Inventory/
├── Entities/
│   ├── Item.cs              # Item + ItemType + CostingMethod
│   ├── Warehouse.cs         # Warehouse (multi-company)
│   ├── UnitOfMeasure.cs     # UoM
│   └── ItemCategory.cs      # Category (hierarchy)
├── Application/
│   ├── InventoryDtos.cs     # كل DTOs
│   ├── Validators.cs
│   └── Services/
│       ├── InventoryServices.cs       # 4 services (Item, Warehouse, UoM, Category)
│       └── InventoryBootstrapper.cs   # seeds UoMs + Categories on tenant create
└── Infrastructure/
    ├── IRepositories.cs
    ├── ItemRepository.cs
    ├── WarehouseRepository.cs
    ├── UnitOfMeasureRepository.cs
    └── ItemCategoryRepository.cs
```

## Domain Model

### Item
- **SKU** unique per tenant
- **ItemType**: RawMaterial, FinishedGood, Consumable, Service
- **CostingMethod** (default Average): FIFO, LIFO, Average, Standard
  - Average = moving weighted average (يُحدّث في PR #6 Stock Movements)
- **AverageCost + StandardCost**: الأول يُحدّث تلقائياً، الثاني manual
- **3 account FKs**: Inventory (1300), COGS (5100), Sales (4100)
  - **تستخدم في PR #6/7** لإنشاء Journal Entry عند stock movement
- **ReorderLevel + ReorderQuantity** (0 = disabled) — يُستخدم في PR #6 لإنشاء LowStock notifications

### Warehouse
- Multi-company (company_id)
- Manager (optional user FK)
- Soft delete via IsActive

### UnitOfMeasure
- 6 seeded: pcs, kg, m, m², m³, liter
- Code: alphanum + ²³ (e.g., "m2" with symbol "m²")

### ItemCategory
- Self-referencing hierarchy (parent_id)
- 5 seeded: RM, FG, CON, SVC, OFF (all root-level, ready for sub-categories)

## Default Seed
يُستدعى من `CompanyService.OnTenantCreatedAsync`:
- 6 UoMs (pcs, kg, m, m², m³, liter)
- 5 Categories (RM, FG, CON, SVC, OFF)
- Idempotent (checks if 'pcs' / 'RM' already exist)

## Endpoints (12)

| Method | Path | الغرض |
|--------|------|-------|
| GET/POST/PUT/DELETE | /api/inventory/items | CRUD |
| GET/POST/PUT/DELETE | /api/inventory/warehouses | CRUD |
| GET/POST | /api/inventory/uom | List + Create (Read-mostly) |
| GET/POST/PUT | /api/inventory/categories | CRUD + hierarchy children |

## لما تشتغل هنا

- إضافة cost method: عدّل `CostingMethod` enum + default behavior في `ItemService.CreateAsync`
- إضافة UoM default: عدّل `DefaultInventorySeed.DefaultUoMs` (الـ bootstrap تلقائي)
- ربط Item بـ Project: في PR #2.3 Stock Movements — `StockMovement.ProjectId` و `Material Requested` flow

## بعد التعديل

- شغّل `dotnet test` (16 tests جديد)
- إذا غيّرت الـ seed: تأكد من `InventoryBootstrapper` idempotent
- إذا أضفت field للـ Item: migration جديدة + entity + DTO + repo

## تكامل مع الموديولات الأخرى

- **Finance** (Phase 1): Item → accounts (1300/5100/4100) — يُستخدم في Journal Entry على stock receipt/issue
- **Projects** (Phase 2.1): StockMovement.ProjectId — ربط المخزون بمشروع
- **Notifications** (Phase 2.3 PR #6): Reorder alerts عبر جدول `notifications`

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../Identity/AGENTS.md`](../Identity/AGENTS.md)
- [`../Finance/AGENTS.md`](../Finance/AGENTS.md) — account FKs
- [`../Companies/AGENTS.md`](../Companies/AGENTS.md) — multi-company + tenant bootstrap
- [`../Projects/AGENTS.md`](../Projects/AGENTS.md) — project context
