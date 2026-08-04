# ERP-SYSTEM — Current State (Sprint 30 Baseline, 2026-08-04)

> **Author:** Muhammad (Mavis mode), Architect/Strategic Advisor
> **Audience:** Anas (Project Owner), Admin Team, Local Team
> **Sprint 30 commit:** `0803aee` — feature/sprint-21-posting-rules-engine
> **Mode:** Mode 1 (host local dev, no push, no PR)

---

## 1. النظام شغّال على الهوست (الحالة)

| Component | Status | Details |
|---|---|---|
| **Backend (BE)** | ✅ UP | http://127.0.0.1:5001 — ASP.NET 9, Dapper, FluentMigrator |
| **Frontend (FE)** | ✅ UP | http://localhost:3000 — Next.js 14 (App Router) + Tailwind + shadcn/ui |
| **Database (DB)** | ✅ UP | PostgreSQL 17 local — `erp_system` db, user `erp` |
| **Docker** | ⏸ PAUSED | Anas's choice to save RAM. mvp-docker not running. |
| **Login** | ✅ | `admin@erp.local` / `ChangeMe1234!` |
| **Environment** | ✅ | `Development` (enables all 5 seeders) |

### الـ Processes (اللي شغّالين الآن)
- BE: PID 14788, port 5001 (started 03:12 today)
- FE: PID 6400, port 3000 (started 03:13 today)
- PostgreSQL: PID 5224 (auto-started as Windows service)

---

## 2. هيكل النظام (9 modules + Holding)

```
ERP-SYSTEM
├── Holding (000 + N subsidiaries)
│   ├── 1 holding + 0 subsidiaries seeded (single-deployment target)
│   └── multi-company model kept (per Constitution Article 3)
│
├── 9 Modules (Sprint 22: 15 → 9):
│   ├── Identity       (users, roles, JWT auth, company assignment)
│   ├── Companies      (holding tree, current company context)
│   ├── Finance        (CoA, Journal, Ledger, Posting Rules)
│   ├── Inventory      (items, warehouses, stock, movements, UoM, cost centers)
│   ├── Procurement    (vendors, POs, GRs, bills)
│   ├── AR             (customers, sales invoices, receipts) — under /api/ar
│   ├── AP / Payments  (vendor payments) — under /api/payments (NOT /api/ar)
│   ├── HR             (employees, departments, attendance, leaves, payroll)
│   ├── Projects       (projects, tasks, resources — ⚠️ partial, see §6)
│   └── Dashboard      (single page, KPIs)
│
└── Removed (Sprint 22): Activity, Notifications, Search, Reports
```

---

## 3. الـ Endpoints (35 working, 3 broken)

### ✅ 35 endpoints return 200

| Group | Endpoint | Records | Status |
|---|---|---|---|
| **Auth** | `/api/auth/me` | — | ✅ |
| **Identity** | `/api/identity/users` | 1 | ✅ |
| | `/api/identity/roles` | 1 | ✅ |
| | `/api/users` | 1 | ✅ |
| **Companies** | `/api/companies` | 1 | ✅ |
| **Finance** | `/api/finance/accounts` | 47 | ✅ |
| | `/api/finance/journal-entries` | 50 | ✅ |
| | `/api/finance/posting-rules` | 5 | ✅ |
| | `/api/finance/ledger/trial-balance` | 30 | ✅ |
| **AR** | `/api/ar/customers` | 13 | ✅ |
| | `/api/ar/sales-invoices` | 12 | ✅ (DEC-106 fix) |
| | `/api/ar/receipts` | 24 | ✅ |
| | `/api/ar/aging` | — | ✅ |
| **AP** | `/api/payments` | 24 | ✅ |
| **Procurement** | `/api/procurement/vendors` | 13 | ✅ |
| | `/api/procurement/pos` | 10 | ✅ (DEC-105a enrichment) |
| | `/api/procurement/grs` | 10 | ✅ (DEC-105c) |
| | `/api/procurement/bills` | 22 | ✅ (12 year + 10 procurement) |
| **Inventory** | `/api/inventory/items` | 20 | ✅ |
| | `/api/inventory/categories` | 5 | ✅ |
| | `/api/inventory/warehouses` | 1 | ✅ (DEC-101) |
| | `/api/inventory/uom` | 6 | ✅ |
| | `/api/cost-centers` | 1 | ✅ (DEC-101) |
| | `/api/inventory/movements` | 0 | ✅ (table empty) |
| | `/api/inventory/levels` | 0 | ✅ (table empty) |
| **HR** | `/api/hr/employees` | 10 | ✅ |
| | `/api/hr/departments` | 5 | ✅ |
| | `/api/hr/attendance` | 0 | ✅ (table empty) |
| | `/api/hr/leaves` | 0 | ✅ (table empty) |
| | `/api/hr/payroll/runs` | 0 | ✅ (table empty) |
| **Projects** | `/api/projects` | 0 | ✅ (table empty) |
| **Audit** | `/api/audit` | 0 | ✅ |
| | `/api/audit/summary` | 0 | ✅ |
| **Health** | `/api/health/full` | — | ✅ |
| **Dashboard** | `/api/dashboard/holding` | — | ✅ |

### ❌ 3 endpoints broken (أخطاء حقيقية)

| Endpoint | Status | Error | Root cause |
|---|---|---|---|
| `/api/resources` | **500** | `relation "resources" does not exist` | `data-types/resources.json` missing — table never created |
| `/api/projects/{id}/tasks` | **500** | `relation "tasks" does not exist` | `data-types/tasks.json` + `project_assignments.json` + `project_budgets.json` missing |
| `/api/finance/ledger/accounts/{id}` | **404** | (when id doesn't exist; expected) | Test artifact — used 00000000-... id. Real account ids work. |

**L44 (NEW)**: Projects module is **partially implemented**. The entities + services + controllers exist, but the underlying tables (`resources`, `tasks`, `project_assignments`, `project_budgets`) were never registered in `data-types/`. This is a Sprint 22+ bug that needs a dedicated sprint.

---

## 4. الـ DB State (44 tables, 407 records)

### 44 tables exist
- Master: companies, users, roles, user_companies, user_roles, refresh_tokens, password_reset_tokens, audit_log
- Finance: accounts, journal_entries, journal_lines, posting_rules
- AR: customers, sales_invoices, sales_invoice_lines, receipts, receipt_allocations
- AP: payments, payment_allocations
- Procurement: vendors, purchase_orders, purchase_order_lines, goods_receipts, goods_receipt_lines, vendor_bills, vendor_bill_lines
- Inventory: items, item_categories, units_of_measure, warehouses, cost_centers, stock_levels, stock_movements, stock_reservations
- HR: departments, employees, attendance, leave_requests, salary_structures, salary_structure_lines, payroll_runs, payroll_items, payslip_components

### 4 tables MISSING (causing the 500s)
- `resources` (Projects module)
- `tasks` (Projects module)
- `project_assignments` (Projects module)
- `project_budgets` (Projects module)

### Records (after wipe+reseed, Sprint 30 baseline)
- 13 customers + 13 vendors + 20 items (Sprint 26)
- 5 departments + 10 employees (Sprint 27)
- 1 warehouse + 1 cost center (DEC-101 — always seeded)
- 10 POs + 10 GRs + 10 Procurement Bills (Sprint 28+30)
- 12 sales invoices + 12 vendor bills + 24 receipts + 24 payments (Sprint 29)
- 83 journal entries + 169 lines (74 benchmark JEs + OB-2025-001)
- 5 posting rules (Libya default — no tax)
- 47 accounts (full CoA)

---

## 5. الـ Seeders (5 POC pattern, established)

All 5 run on `IsDevelopment() + flag` double-gate. Idempotent UPSERT.

| # | Seeder | Flag | Records | Notes |
|---|---|---|---|---|
| 1 | `DefaultHoldingBootstrap` | always-on (no flag) | 1 holding + admin + bootstrap | DEC-101: WH-001 + CC-001 always |
| 2 | `ArabicDevSeeder` | `SeedArabicScenario=true` | 13+13+20 | Sprint 26 (master data) |
| 3 | `ArabicHrDevSeeder` | `SeedHrScenario=true` | 5+10 | Sprint 27 (HR, 3-pass UPSERT) |
| 4 | `ArabicProcurementDevSeeder` | `SeedProcurementScenario=true` | 10+10+10 | Sprint 28+30 (PO+GR+Bill) |
| 5 | `ArabicYearScenarioDevSeeder` | `SeedYearScenario=true` | 73 records + 74 JEs | Sprint 29 (year scenario + benchmark JEs) |

**Pattern (L17, L36):** JSON + IHostedService + UPSERT + Dapper + double-gate + Content include + appsettings flag. 4th POC = <1.5h (vs 4-6h for first).

---

## 6. الـ Bugs / Carry-over من Sprint 30

| Priority | Bug | Impact | Fix scope |
|---|---|---|---|
| **P0** | Projects module incomplete (no resources/tasks tables) | 2 endpoints return 500 | Add 4 data-types JSONs + migrations + seeder |
| **P0** | `Account.CompanyId` audit carry-over (4 modules) | Future tenant_id leak | L19 + L30 (refactor req.CompanyId → _companyContext.CompanyId) |
| **P1** | `data-types/` doesn't auto-discover | Manual JSON file needed for every new entity | Build-time test that enforces DEC-085 + L44 |
| **P1** | `await using IDbConnection` fails with CS8417 | Inconsistent patterns | L43: use `using var` always |
| **P1** | `task create` (UI workflow) not yet built | Can't create new tasks via UI | Sprint 31+ |
| **P2** | Multi-currency (only LYD) | Libya-only, can't handle FX | Config + accounts |
| **P2** | Manual JEs (depreciation, accruals, year-end) | No end-of-period workflow | Config + UI |
| **P2** | 5th default rule "Sale with VAT 5%" (inactive) | For demo, not yet added | posting_rules seeder |
| **P2** | Pre-push script: scan for `?` in user-visible columns | Would have caught Sprint 25/26 bugs | Bash + Python script |
| **P2** | Build-time test that enforces DEC-085 | Prevents Article 3 violations | xUnit test |

---

## 7. الـ Architecture decisions المؤكدة (Constitution v2.0)

### Article 3: `company_id` everywhere
- ✅ 8/13 modules audited (Sprints 23, 24, 25, 27, 28)
- ⚠️ 5 still-pending: Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService

### Cross-module communication
- **Old (event-driven):** `_eventBus.PublishAsync(...)` → OutboxProcessor → Handler
- **New (Sprint 22):** Direct service call (synchronous, same transaction)
- Example: `SalesInvoiceService.PostAsync` → `PostingRulesService.ApplyRulesAsync` + `ProjectsService.UpdateCostAsync`

### Branch architecture
- `develop` = DEFAULT (active work)
- `main` = LOCKED archive (no merges)
- Tags: `v0.0.0-pre-branch-reset` (safety) + `v1.0.0-archive` to `v1.0.10-sprint24-28-audit` (11 work anchors)

### 2-Mode Workflow (CONSTITUTION Article 10)
- **Mode 1 (default):** Local work, no push, no CI. Admin merges locally.
- **Mode 2 (release):** Anas says "ادفع" → push + PR + CI + merge + tag + restore.

### 3-Layer Model
- **Layer 1 (active):** Local host BE+FE on Windows, local PostgreSQL
- **Layer 2 (paused):** mvp-docker (clean install)
- **Layer 3 (frozen):** Supabase production

---

## 8. الـ Lessons L25-L43 (accumulated)

Most relevant for Mode 1 admin work:
- **L40**: API must return human-readable names, not raw GUIDs (DEC-104, DEC-105a pattern)
- **L41**: Seeders must compute totals from line items
- **L42**: Seeder cross-pass dependencies need explicit lookup maps
- **L43**: Before `dotnet build`, ensure no `dotnet` process is running
- **L44 (NEW)**: Projects module tables missing — `data-types/` doesn't auto-discover. **Need a `data-types.json` for every new entity.**

---

## 9. الـ Code Standards (active)

- **Backend:** C# / .NET 9 / Dapper (NO EF Core) / FluentMigrator / xUnit
- **Frontend:** TypeScript / Next.js 14 (App Router) / Tailwind / shadcn/ui / Jest
- **Migrations:** Idempotent (`CREATE TABLE IF NOT EXISTS`, `DO $$ ... IF EXISTS ... $$`)
- **Batch inserts:** Postgres `unnest()` for ≥ 10 rows. No N+1.
- **Atomicity:** Multi-insert in single transaction
- **API-First:** Backend before Frontend. One test per endpoint.

---

## 10. الـ Open Questions (للـ Admin)

1. هل تريد `/api/projects/{id}/tasks` و `/api/resources` تشتغل في Sprint 31؟ (P0 — missing tables)
2. هل نضيف Manual JEs (depreciation, accruals)؟ (P2 — config + UI)
3. هل نبني 5th default rule "Sale with VAT 5%"؟ (P2 — posting_rules seeder)
4. هل نضيف Playwright e2e tests؟ (P1 — اختبار آلي)
5. هل في مشكلة معينة اكتشفت في الـ browser test أمس؟

---

_Last updated: 2026-08-04 by Muhammad (Mavis mode) for Sprint 31 planning_
