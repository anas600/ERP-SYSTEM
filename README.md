# ERP-SYSTEM v1.0.34-hotfix2

**Multi-Company Edition** — A complete ERP for Libyan small/medium businesses with full Arabic UI, double-entry accounting, multi-tenant-safe architecture, and 20+ financial reports.

> **Status:** ✅ Pre-Production Review (Phase 6 complete)
> **Stack:** Next.js 14 + .NET 9 + PostgreSQL 18
> **Architecture:** Multi-Company (`company_id` everywhere, NOT `tenant_id`)

---

## 🎯 Quick Start

### 1. Database (PostgreSQL 18)
```powershell
# Create database (one-time)
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432 `
  -c "CREATE DATABASE erp_system_demo OWNER erp_user ENCODING 'UTF8';"

# Reset erp_user password
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432 `
  -c "ALTER USER erp_user WITH PASSWORD 'Demo1234';"
```

### 2. Backend (.NET 9)
```powershell
Set-Location "F:\minimaxDescktop2\ERP-SYstem\src\backend\Host"
dotnet build -c Debug
Set-Location "bin\Debug\net9.0"
.\ERPSystem.Host.exe
# → http://localhost:5000
```

The backend auto-creates 47 accounts (chart of accounts) + holding company on first run.

### 3. Frontend (Next.js 14)
```powershell
Set-Location "F:\minimaxDescktop2\ERP-SYstem\src\frontend"
# .env.local must contain: NEXT_PUBLIC_API_URL=http://localhost:5000
npm install
npm run dev
# → http://localhost:3000
```

### 4. Seed 1 Year of Demo Data (optional, ~30s)
```powershell
Set-Location "F:\minimaxDescktop2\ERP-SYstem"
npm run seed:1year
# Generates: 15 customers, 10 vendors, 20 items, 491 invoices, 262 bills, 12 payroll
# All journal entries balance (debit = credit), accounting equation holds
```

### 5. Login
- **URL:** http://localhost:3000
- **Email:** `admin@alfajr.local`
- **Password:** `Demo1234`

---

## 📂 Project Structure

```
ERP-SYstem/
├── src/
│   ├── backend/                  # .NET 9 Web API
│   │   ├── Host/                 # Entry point, Program.cs, appsettings.json
│   │   ├── Modules/              # Feature modules (Identity, AR, AP, HR, Inventory, ...)
│   │   └── Shared/               # Cross-cutting (Migrations, DataTypes, SeedData, Events)
│   └── frontend/                 # Next.js 14 (App Router, RSC, React 18.3)
│       ├── app/                  # Routes (login, (authenticated), api)
│       ├── components/           # Reusable UI (Card, Button, Modal, Toast, ...)
│       └── lib/                  # API client, useAuth, useToast, utils
├── docs/                         # Spec, audits, guides
├── tests/                        # Playwright E2E suite
└── scripts/                      # DB seed + check
```

---

## 🧪 Testing (Playwright)

```powershell
# One-time: install browsers
npx playwright install chromium chromium-headless-shell

# Run modes
npm run test:e2e:smoke          # Fast smoke test (39 endpoints, ~50s)
npm run test:e2e               # All tests (smoke + security + flow)
npm run test:e2e:headed        # See Chrome open
npm run test:e2e:ui            # Interactive UI
npm run test:e2e:report        # View HTML report
```

**Test coverage:**
- `smoke.spec.ts` — 39 backend endpoints (200 OK + JSON shape)
- `security.spec.ts` — 401, SQL injection, NoSQL injection, multi-company isolation
- `finance.spec.ts` — Customers, invoices, all 20 reports UI
- `procurement.spec.ts` — Vendors, bills, POs, GRs
- `hr.spec.ts` — Employees, payroll, leaves, attendance
- `inventory.spec.ts` — Items, warehouses, stock
- `projects.spec.ts` — Project list, detail, budget
- `admin.spec.ts` — Users, profile, change password, notifications

---

## 🏛️ Constitution (الـ Constitution)

The system follows the **Project Constitution** (`CONSTITUTION.md` in repo root). Key articles:

- **§3 Multi-Company, NO Multi-Tenancy** — all tables have `company_id`, all SQL filters by it
- **§4 Dapper (not EF Core)** — backend uses Dapper micro-ORM
- **§5 No merge to develop without owner review** — Anas reviews and merges manually
- **§6 Data integrity** — every Journal Entry balances (D=C), accounting equation holds

---

## 🔐 Multi-Company Architecture

```
Holding (00000000-...)
   ├── Subsidiary A (libya oil co)
   ├── Subsidiary B (Benghazi construction)
   └── Subsidiary C (Family mart)
```

- **All API requests** must include `X-Company-Id` header (the active company)
- **JWT** carries `company_id` and `company_ids[]` claims
- **User can belong to multiple companies** via `user_companies` join table
- **No `tenant_id`** anywhere in the codebase (Constitution §3.1)

---

## 📊 Reports (20+)

| # | Report | Endpoint |
|---|--------|----------|
| 1 | Trial Balance | `GET /api/finance/reports/trial-balance` |
| 2 | Balance Sheet | `GET /api/finance/reports/balance-sheet` |
| 3 | Income Statement | `GET /api/finance/reports/income-statement` |
| 4 | Cash Flow | `GET /api/finance/reports/cash-flow` |
| 5 | VAT (15%) | `GET /api/finance/reports/vat` |
| 6 | AP Aging | `GET /api/finance/reports/ap-aging` |
| 7 | Cost Center Performance | `GET /api/finance/reports/cost-center-performance` |
| 8 | General Ledger | `GET /api/finance/reports/general-ledger` |
| 9 | Account Activity | `GET /api/finance/reports/account-activity` |
| 10 | Journal Entries | `GET /api/finance/reports/journal-entries` |
| 11 | Collections | `GET /api/finance/reports/collections` |
| 12 | AR Aging | `GET /api/ar/aging` |
| 13 | Top Customers | `GET /api/ar/reports/top-customers` |
| 14 | Sales by Customer | `GET /api/ar/reports/sales-by-customer` |
| 15 | Sales by Item | `GET /api/ar/reports/sales-by-item` |
| 16 | Top Vendors | `GET /api/procurement/reports/top-vendors` |
| 17 | Purchases by Vendor | `GET /api/procurement/reports/purchases-by-vendor` |
| 18 | Budget vs Actual | `GET /api/reports/projects/budget-vs-actual` |
| 19 | Inventory Valuation | `GET /api/reports/inventory/valuation` |
| 20 | Project P&L | `GET /api/reports/projects/{id}/pnl` |

---

## 🐛 Known Issues

- **React 19 vs 18.3** — Project uses React 18.3. Do NOT use `use(params)` (React 19). Use `useParams()` from `next/navigation`.
- **DB schema drift** — Some tables (e.g. `items`) have older schemas than data-types JSON. The seed script (`scripts/gen_seed_1year.js`) handles these.
- **VAT report from/to** — some reports may show `0001-01-01` if date params are not provided; pass `?from=YYYY-MM-DD&to=YYYY-MM-DD`.

---

## 📞 Support

For issues, contact the development team or open a GitHub issue.
See `docs/PRE-PROD-CHECKLIST.md` for the current delivery status.
