# RBAC Test Matrix

_Generated: 2026-07-22 13:40 UTC_
_Source: scripts/generate-rbac-matrix.py + Host/Auth/PolicyNames.cs_

## Summary
- **Total controllers**: 28
- **Unique policies used**: 6
- **Test combinations**: 28 × 4 = 112 (plus anonymous = 140)

## Policy Definitions

| Policy | Allowed Roles |
|--------|---------------|
| `AdminOnly` | Admin |
| `AdminOrAccountant` | Admin, Accountant |
| `AdminOrProjectManager` | Admin, ProjectManager |
| `AnyAuthenticated` | Admin, Accountant, ProjectManager, Viewer |
| `Audit.Read` | Admin, Accountant |
| `Events.Write` | Admin, Accountant |
| `Finance.Write` | Admin, Accountant |
| `HR.Write` | Admin |
| `Inventory.Write` | Admin, Accountant, ProjectManager |
| `Procurement.Write` | Admin, Accountant |
| `ReadAccess` | Admin, Accountant, ProjectManager, Viewer |
| `WriteAdmin` | Admin |
| `WriteFinance` | Admin, Accountant |
| `WriteMasterData` | Admin |
| `WriteProjects` | Admin, ProjectManager |
| `WriteStock` | Admin, Accountant, ProjectManager |

## Per-Policy × Role Matrix

| Policy | Admin | Accountant | ProjectManager | Viewer | Anonymous |
|--------|-------|------------|----------------|--------|-----------|
| AdminOnly | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny | ❌ Deny |
| AdminOrAccountant | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny |
| AdminOrProjectManager | ✅ Allow | ❌ Deny | ✅ Allow | ❌ Deny | ❌ Deny |
| AnyAuthenticated | ✅ Allow | ✅ Allow | ✅ Allow | ✅ Allow | ❌ Deny |
| Audit.Read | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny |
| Events.Write | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny |
| Finance.Write | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny |
| HR.Write | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny | ❌ Deny |
| Inventory.Write | ✅ Allow | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny |
| Procurement.Write | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny |
| ReadAccess | ✅ Allow | ✅ Allow | ✅ Allow | ✅ Allow | ❌ Deny |
| WriteAdmin | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny | ❌ Deny |
| WriteFinance | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny |
| WriteMasterData | ✅ Allow | ❌ Deny | ❌ Deny | ❌ Deny | ❌ Deny |
| WriteProjects | ✅ Allow | ❌ Deny | ✅ Allow | ❌ Deny | ❌ Deny |
| WriteStock | ✅ Allow | ✅ Allow | ✅ Allow | ❌ Deny | ❌ Deny |

## Per-Controller Policy Mapping

All 28 controllers with their policy:

| Controller | Route | Policy | Admin | Accountant | PM | Viewer |
|------------|-------|--------|-------|------------|-----|--------|
| Accounts | api/finance/accounts | WriteFinance | ✅ | ✅ | ❌ | ❌ |
| Admin | api/admin | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Auth | api/auth | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Companies | api/companies | WriteMasterData | ✅ | ❌ | ❌ | ❌ |
| CostCenters | api/cost-centers | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Debug | api/debug | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Events | api/events | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| FinanceAr | ? | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| FinanceReports | api/finance | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Health | api/health | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Hr | ? | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| ItemCategories | api/inventory/categories | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Items | api/inventory/items | WriteStock | ✅ | ✅ | ✅ | ❌ |
| JournalEntries | api/finance/journal-entries | WriteFinance | ✅ | ✅ | ❌ | ❌ |
| Ledger | api/finance/ledger | ReadAccess | ✅ | ✅ | ✅ | ✅ |
| Notifications | api/inventory/notifications | ReadAccess | ✅ | ✅ | ✅ | ✅ |
| Payments | ? | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| PostingRules | api/finance/posting-rules | WriteMasterData | ✅ | ❌ | ❌ | ❌ |
| Procurement | ? | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Projects | api/projects | WriteProjects | ✅ | ❌ | ✅ | ❌ |
| Reports | api/reports | ReadAccess | ✅ | ✅ | ✅ | ✅ |
| Resources | api/resources | WriteProjects | ✅ | ❌ | ✅ | ❌ |
| StockLevels | api/inventory/levels | ReadAccess | ✅ | ✅ | ✅ | ✅ |
| StockMovements | api/inventory/movements | WriteStock | ✅ | ✅ | ✅ | ❌ |
| StockReservations | api/inventory/reservations | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Tasks | api/tasks | WriteProjects | ✅ | ❌ | ✅ | ❌ |
| UnitOfMeasures | api/inventory/uom | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Warehouses | api/inventory/warehouses | (no policy — [Authorize] only) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |

## Audit Findings

### ⚠️ Controllers without specific policy (15)
- `Admin` (api/admin) — only `[Authorize]` (any authenticated user)
- `Auth` (api/auth) — only `[Authorize]` (any authenticated user)
- `CostCenters` (api/cost-centers) — only `[Authorize]` (any authenticated user)
- `Debug` (api/debug) — only `[Authorize]` (any authenticated user)
- `Events` (api/events) — only `[Authorize]` (any authenticated user)
- `FinanceAr` (?) — only `[Authorize]` (any authenticated user)
- `FinanceReports` (api/finance) — only `[Authorize]` (any authenticated user)
- `Health` (api/health) — only `[Authorize]` (any authenticated user)
- `Hr` (?) — only `[Authorize]` (any authenticated user)
- `ItemCategories` (api/inventory/categories) — only `[Authorize]` (any authenticated user)
- `Payments` (?) — only `[Authorize]` (any authenticated user)
- `Procurement` (?) — only `[Authorize]` (any authenticated user)
- `StockReservations` (api/inventory/reservations) — only `[Authorize]` (any authenticated user)
- `UnitOfMeasures` (api/inventory/uom) — only `[Authorize]` (any authenticated user)
- `Warehouses` (api/inventory/warehouses) — only `[Authorize]` (any authenticated user)
