# Phase 6 Release Notes — Multi-Company Refactor

> **Release:** Phase 6.0–6.4 (June–July 2026)
> **Owner:** Anas (anas600)
> **Authors:** Mavis (Anas's local team) + Abdo's team (parallel branch)
> **Status:** ✅ Phase 6.2 merged to `develop` (2026-07-27)
> **Next:** Phase 6.3 (Frontend polish) + 6.5 (CI hardening)

---

## 🎯 TL;DR

**ERP-SYSTEM v6** is now a **Multi-Company** system, not Multi-Tenant. The old `tenant_id` partition key is **gone** — every business row is partitioned by `company_id` instead. Users are **global** (one email = one user, scoped to one or more companies via the new `user_companies` join table).

This release closes the multi-tenant chapter that was vestigial from the SaaS-billing concept (per `tenants.subscription_expires_at` which was never used). It's the foundation for Phase 7+ (per-company dashboards, intercompany transactions, multi-jurisdiction compliance).

---

## 🏛️ Architectural Shift

| | Before (v5, multi-tenant) | After (v6, multi-company) |
|---|---|---|
| **Partition key** | `tenant_id` (UUID) on every row | `company_id` (UUID) on every row |
| **Root entity** | `Tenant` (subdomain-based) | `Company` with `is_group=true, parent_company_id=NULL` (the Holding) |
| **User scoping** | `User.TenantId` (1:1) | `user_companies` join (M:N — user can belong to many companies) |
| **Auth claim** | `tenant_id` (single Guid) | `default_company_id` + `company_ids[]` (array) |
| **Isolation** | Per-tenant (SaaS billing model) | Per-company (subsidiary model) |
| **Cache key prefix** | `t:{tenantId}:` | `c:{companyId}:` (planned) |
| **Middleware** | `TenantMiddleware` (read `tenant_id` claim) | `CompanyContextMiddleware` (read `X-Company-Id` header) |
| **DI scoped** | `ITenantContext` → `TenantContext` | `ICompanyContext` → `CompanyContext` |
| **Authorization** | `[Authorize]` + manual tenant check | `[CompanyAuthorize(companyId)]` + `[Authorize]` |
| **Migrations** | 24 (multi-tenant schema history) | 10 (clean schema, no `tenant_id` references anywhere) |

---

## 📦 What's in this Release

### Phase 6.0 — Schema Reset
- **Deleted:** `tenants` table + `Tenant.cs` entity
- **Created:** `user_companies` join table (composite PK: `user_id + company_id`)
- **Updated:** All 41 business tables — `tenant_id` column removed; `company_id` retained
- **New:** `DefaultHoldingBootstrapHostedService` — seeds the Holding + 47 Chart of Accounts + 6 UoMs + 5 ItemCategories at app startup (idempotent)
- **Migration:** `Phase6_InitialSchema_20260725_120000` (clean-slate per Constitution Article 3.4)

### Phase 6.1 — Backend Code Refactor
- `AuthService.RegisterAsync` — atomic (DEC-091), 1 connection + 1 transaction, no orphan users
- `JwtTokenService` — issues `default_company_id` + `company_ids[]` claims
- `ICompanyContext` / `CompanyContext` / `CompanyContextMiddleware` — replaces multi-tenancy trio
- All 50+ repositories: dropped `WHERE tenant_id = @TenantId`
- All 25+ services: dropped `Guid tenantId` first parameter
- 24 generated `*.g.cs` files regenerated
- 23 test files updated

### Phase 6.2 — Reports + User Management
- **20 accounting reports** added (Trial Balance, Income Statement, Balance Sheet, Cash Flow, GL, Journal, Account Activity, AR/AP Aging, Collections, Sales by Customer/Item, Purchases by Vendor, Top Customers/Vendors, Cost Center Performance, Project P&L, Budget vs Actual, VAT 15%, Inventory Valuation)
- **User Management:** CRUD + admin password reset + role assignment
- **Self-service change-password** endpoint (`/api/auth/change-password`)
- **1-year seed data** (`docs/seed-one-year-data.sql` + `docs/seed-phases-2-8.sql` + `docs/seed-phases-6-8.sql` — ~50 KB total) on Multi-Company architecture
- **Functional Specification** (`docs/SYSTEM-FUNCTIONAL-SPECIFICATION.pdf` — 31 pages)

### Phase 6.3 — Frontend
- 7 new report pages (Trial Balance + 6 more)
- `formatCurrency` / `formatPercent` utilities (`src/frontend/lib/utils.ts`)
- Updated `admin/users` page (migrated to `identityApi`)
- AppShell with CompanySwitcher + notification bell + admin shortcuts
- Next.js `/api/*` proxy rewrites to backend

### Phase 6.4 — Documentation (this cycle)
- Root `AGENTS.md` updated (Multi-Tenant → Multi-Company)
- 11 module `AGENTS.md` files updated with Phase 6 banner
- `docs/CHANGELOG.md` — Phase 6 release entry
- This file: `docs/PHASE6-RELEASE-NOTES.md`
- `docs/PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md` — Outcome section added

---

## 🔐 Auth Flow (v6)

### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "admin@alfajr.local",
  "password": "Demo1234",
  "fullName": "System Administrator",
  "baseCurrency": "LYD"   // optional, default LYD
}
```

**Server flow:**
1. Find (or create) the default Holding Company (idempotent)
2. Check global email uniqueness
3. INSERT `users` (default_company_id = holdingId, no tenantId)
4. INSERT `user_companies` (user ↔ holding)
5. Assign Admin role (global, seeded at boot)
6. Issue access + refresh tokens
7. Return `UserInfo` + `AuthResponse`

**Response shape:**
```json
{
  "user": {
    "id": "uuid",
    "email": "admin@alfajr.local",
    "fullName": "System Administrator",
    "defaultCompanyId": "00000000-0000-0000-0000-000000000001",
    "companyIds": ["00000000-0000-0000-0000-000000000001"],
    "roles": ["Admin"]
  },
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "holdingCompanyId": "00000000-0000-0000-0000-000000000001"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@alfajr.local",
  "password": "Demo1234"
}
```

(NO `tenantId` — users are global)

### Multi-company in practice

```http
GET /api/finance/accounts
Authorization: Bearer <jwt with company_ids[]>
X-Company-Id: 11111111-1111-1111-1111-111111111111  # current subsidiary
```

The frontend's `CompanySwitcher` updates `X-Company-Id` per request. The backend's `CompanyContextMiddleware`:
1. Reads `X-Company-Id` header
2. Falls back to `default_company_id` JWT claim if header absent
3. Validates the company is in the user's `company_ids[]` (else 403)
4. Populates `ICompanyContext.CompanyId` for downstream code

---

## 🗃️ New Schema (v6)

### 41 business tables (no `tenant_id`)

```
Identity:     users, roles, user_roles, user_companies, refresh_tokens, password_reset_tokens
Companies:    companies (Holding + subsidiaries), cost_centers
Finance:      accounts, journal_entries, journal_lines, posting_rules
Projects:     projects, project_tasks, resources, resource_assignments, project_budgets
Inventory:    items, item_categories, warehouses, units_of_measure,
              stock_levels, stock_movements, stock_reservations
Procurement:  vendors, purchase_orders, purchase_order_lines,
              goods_receipts, goods_receipt_lines,
              vendor_bills, vendor_bill_lines, document_sequences
Payments:     payments, payment_allocations, payment_sequences
AR:           customers, sales_invoices, sales_invoice_lines,
              receipts, receipt_allocations, ar_document_sequences
HR:           departments, employees, attendance, leave_requests, hr_document_sequences
Payroll:      salary_structures, salary_structure_lines,
              payroll_runs, payroll_items, payslip_components
Shared:       notifications, outbox_events, processed_events, audit_log, archive_metadata
```

### Companies table (Multi-Company tree)

```sql
CREATE TABLE companies (
  id                  UUID PRIMARY KEY,
  code                VARCHAR(20) UNIQUE,
  name                VARCHAR(200) NOT NULL,
  legal_name          VARCHAR(200),
  is_group            BOOLEAN NOT NULL DEFAULT FALSE,  -- true = Holding
  parent_company_id   UUID REFERENCES companies(id),    -- NULL for Holding
  base_currency       VARCHAR(3) NOT NULL DEFAULT 'LYD',
  is_active           BOOLEAN NOT NULL DEFAULT TRUE,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### The Holding

The "default Holding" is **deterministic**: `id = 00000000-0000-0000-0000-000000000001`, `code = '000'`, `is_group = true`, `parent_company_id = NULL`. Seeded by `DefaultHoldingBootstrapHostedService` on every app startup (idempotent `INSERT ... ON CONFLICT (code) DO NOTHING`).

### user_companies join

```sql
CREATE TABLE user_companies (
  user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  company_id  UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
  is_default  BOOLEAN NOT NULL DEFAULT TRUE,
  assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, company_id)
);
```

---

## 🔧 Migration Guide (for code authors)

### Backend

**Before (v5):**
```csharp
public class UserService : IUserService
{
    public async Task<UserDto> GetAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(tenantId, userId, ct);
        // ...
    }
}
```

**After (v6):**
```csharp
public class UserService : IUserService
{
    public async Task<UserDto> GetAsync(Guid userId, CancellationToken ct)
    {
        // tenantId is gone — use ICompanyContext for per-company scoping
        var companyId = _companyContext.CompanyId;
        var user = await _userRepo.GetByIdAsync(userId, ct);
        // ...
    }
}
```

**Repository signatures:**
```diff
- Task<X> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
+ Task<X> GetByIdAsync(Guid id, CancellationToken ct);
- Task<IEnumerable<X>> ListAsync(Guid tenantId, CancellationToken ct);
+ Task<IEnumerable<X>> ListAsync(Guid companyId, CancellationToken ct);  // scoped by company
```

**DI registration:**
```diff
- services.AddScoped<ITenantContext, TenantContext>();
- services.AddSingleton<ITenantCache, TenantCache>();
+ services.AddScoped<ICompanyContext, CompanyContext>();
```

**Middleware:**
```diff
- app.UseMiddleware<TenantMiddleware>();
+ app.UseMiddleware<CompanyContextMiddleware>();
```

**JWT claims:**
```diff
- new Claim("tenant_id", user.TenantId.ToString())
+ new Claim("default_company_id", user.DefaultCompanyId.ToString())
+ new Claim("company_ids", string.Join(",", userCompanyIds))  // array
```

**Authorization attribute:**
```diff
- [Authorize(Policy = "RequireTenant")]
+ [CompanyAuthorize]  // validates X-Company-Id is in user's company_ids[]
```

### Frontend

**`lib/api.ts` types:**
```diff
- interface UserInfo { id: string; email: string; tenantId: string; }
+ interface UserInfo { id: string; email: string; defaultCompanyId: string; companyIds: string[]; }

- interface LoginRequest { email: string; password: string; tenantId?: string; }
+ interface LoginRequest { email: string; password: string; }
```

**`api` axios interceptor (auto-injects `X-Company-Id`):**
```typescript
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  const companyId = localStorage.getItem('currentCompanyId') 
                 ?? JSON.parse(localStorage.getItem('user') || '{}').defaultCompanyId;
  if (companyId) config.headers['X-Company-Id'] = companyId;
  return config;
});
```

**Register form:** drop the `tenantName` field — the Holding is implicit.

---

## ❓ FAQ

### Q: What happened to my old tenant data?
**A:** It's gone. Per Constitution Article 3.4 (and "Fresh Build Mode" already in production), the v5 data was dev/test. v6 starts with an empty DB + the seeded default Holding.

### Q: Can I have multiple Holdings?
**A:** Not in v6 — the constitution allows only one Holding per deployment. If you need multi-Holding, that's a Phase 7+ feature (per `dec-103a/PERFORMANCE-AUDIT.md` notes).

### Q: Can a user belong to multiple companies?
**A:** Yes — `user_companies` is an M:N join. A single user can have rows for Holding + multiple subsidiaries. Use the `CompanySwitcher` in the UI.

### Q: What if I send an `X-Company-Id` that's not in the user's `company_ids[]`?
**A:** Backend returns 403 Forbidden with message "User does not have access to this company."

### Q: How do I add a new subsidiary?
**A:** `POST /api/companies` with `{ code, name, baseCurrency, parentCompanyId: <holding-id> }`. The new subsidiary is automatically available to users you add via `POST /api/admin/users` with `companyIds: [<subsidiary-id>]`.

### Q: Roles — global or per-company?
**A:** Global in v6. The 4 default roles (Admin, Accountant, ProjectManager, Viewer) are seeded once at app start. If you need per-company role customization, that's Phase 7+.

### Q: Email uniqueness — global or per-company?
**A:** Global in v6 (UNIQUE constraint at the DB level). If you need per-company email, that's Phase 7+.

### Q: How does the audit log work?
**A:** `audit_log` table (partitioned by year) records every write. The `tenant_id` column is replaced by `company_id`. Partitioning is preserved (DEC-052 P2).

### Q: What about the Marten event store?
**A:** Marten is installed but NOT wired up. The Outbox pattern in `Shared/Events/Infrastructure/OutboxRepository.cs` is the current implementation. The `outbox_events.tenant_id` column was updated to `company_id` (or removed — see migration `20260725_120000_Phase6_InitialSchema`).

### Q: Where do I find the new auth flow contract?
**A:** `src/backend/Modules/Identity/Application/Auth/AuthDtos.cs` and `src/frontend/lib/api.ts`.

### Q: How do I test multi-company?
**A:** Register user → switch to subsidiary via `CompanySwitcher` → all API calls now scope to that subsidiary. Backend's `UserCompanyAccessFilter` validates access.

### Q: Where's the canonical analysis doc?
**A:** [`PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md`](./PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md) — 50 KB analysis + this Outcome section.

### Q: What's next?
**A:** Phase 6.3 (Frontend polish) + 6.5 (CI hardening) + Phase 7 (per-company dashboards, intercompany transactions, multi-jurisdiction compliance).

---

## 📚 Reference

- **Root doc:** [AGENTS.md](../AGENTS.md#-multi-company-convention-per-constitution-article-3)
- **Constitution:** `CONSTITUTION.md` Article 3
- **Analysis:** [`PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md`](./PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md)
- **Hand-off:** [`HANDOFF-PHASE6-MIGRATE.md`](./HANDOFF-PHASE6-MIGRATE.md)
- **Changelog:** [`CHANGELOG.md`](./CHANGELOG.md)
- **Functional spec:** [`SYSTEM-FUNCTIONAL-SPECIFICATION.pdf`](./SYSTEM-FUNCTIONAL-SPECIFICATION.pdf)
- **Governance:** [`governance/README.md`](./governance/README.md)

---

**Last updated:** 2026-07-27 (Cycle 1 — Documentation Sprint 6.4)
