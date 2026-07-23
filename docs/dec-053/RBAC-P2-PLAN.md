# DEC-053 P2 — RBAC Closure Plan

**Date**: 2026-07-22
**Status**: ✅ COMPLETE — 13 controllers closed
**Goal**: Close the remaining 15 controllers with bare `[Authorize]`

## Inventory (15 controllers)

| # | Controller | Current | Target | Reason |
|---|---|---|---|---|
| 1 | AdminController | `[Authorize(Roles="Admin")]` | `[Authorize(Policy="AdminOnly")]` | Migrate to policy (consistency) |
| 2 | AuthController | Per-method `[Authorize]` | No change | Login/register must be public |
| 3 | CostCentersController | `[Authorize]` | `[Authorize(Policy="WriteMasterData")]` | Master data (read+write) |
| 4 | DebugController | `[Authorize(Roles="Admin")]` | `[Authorize(Policy="AdminOnly")]` | Admin only (consistency) |
| 5 | EventsController | `[Authorize]` | `[Authorize(Policy="ReadAccess")]` | Read-only (everyone) |
| 6 | FinanceArController | `[Authorize]` | `[Authorize(Policy="Finance.Write")]` | AR is financial |
| 7 | FinanceReportsController | `[Authorize]` | `[Authorize(Policy="ReadAccess")]` | Read-only |
| 8 | HealthController | No `[Authorize]` | No change | Public (liveness probe) |
| 9 | HrController | `[Authorize]` | `[Authorize(Policy="HR.Write")]` | HR module |
| 10 | ItemCategoriesController | `[Authorize]` | `[Authorize(Policy="WriteMasterData")]` | Master data |
| 11 | PaymentsController | `[Authorize]` | `[Authorize(Policy="Finance.Write")]` | Financial |
| 12 | ProcurementController | `[Authorize]` | `[Authorize(Policy="Procurement.Write")]` | Procurement |
| 13 | StockReservationsController | `[Authorize]` | `[Authorize(Policy="Inventory.Write")]` | Inventory |
| 14 | UnitOfMeasuresController | `[Authorize]` | `[Authorize(Policy="WriteMasterData")]` | Master data |
| 15 | WarehousesController | `[Authorize]` | `[Authorize(Policy="WriteMasterData")]` | Master data |

## ✅ Result

- **13 controllers closed** (12 added policy, 2 migrated from Roles= to Policy=)
- **2 remaining** (intentionally public):
  - **Auth**: per-method `[Authorize]` (login is public, /me requires auth)
  - **Health**: no `[Authorize]` (liveness probe must be public)

## Reused Policies (no new ones added)

- `AdminOnly` (Admin, Debug)
- `ReadAccess` (Events, FinanceReports)
- `WriteMasterData` (CostCenters, ItemCategories, UnitOfMeasures, Warehouses)
- `Finance.Write` (FinanceAr, Payments)
- `HR.Write` (Hr)
- `Procurement.Write` (Procurement)
- `Inventory.Write` (StockReservations)

## Defense Layers

- DL-192: RBAC P2 plan (this doc)
- DL-193-205: 13 controllers closed
- DL-206: RbacP2 tests
- DL-207: Updated matrix doc

## After P2: 11/12 policies actually used, 0 unguarded controllers

Final matrix:
- 28 controllers total
- 11 unique policies in use
- 112 test combinations
- Only Auth + Health intentionally public (documented)

