# 🏢 AGENTS.md — src/backend/Modules/Companies/

> **Companies module** (Multi-Company). Read all parent AGENTS.md files first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

Manages the `companies` table and related entities. The "C" in Multi-Company. Companies belong to exactly one Holding.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي |
| **Schema review** | Anas (changes to holding/company boundaries) |

## Local Contracts

### Schema
- `companies` — `id`, `holding_id` (FK), `name`, `name_ar`, `legal_name`, `tax_id`, `currency`, `is_active`.
- **Constraint:** `UNIQUE (holding_id, name)`.
- **Soft delete** via `is_active = false`. No hard deletes.

### Boundaries
- **Each company has exactly ONE `holding_id`.**
- **Each user has 0..N companies** (via `user_companies`).
- **No company is a tenant.** All data is `holding_id + company_id` scoped.

## Work Guidance

### Adding a Company Field
1. Add to `Domain/Entities/Company.cs`.
2. Add migration in `Infrastructure/Migrations/`.
3. Update DTO in `Application/DTOs/CompanyDto.cs`.
4. Update service in `Application/Services/CompanyService.cs`.
5. Add controller in `Host/Controllers/CompaniesController.cs`.
6. Add test in `Tests/ERPSystem.Tests/Companies/`.

## Verification

- [ ] `dotnet test --filter "Companies"` — all green.
- [ ] No `tenant_id`: `grep -r "tenant" src/backend/Modules/Companies/`.
- [ ] Migration is idempotent.
- [ ] DTO has Arabic + English names.

---

## Jimi Scope — 2026-07-31 (Sprint 11 T2)

**Jimi type:** BE
**Sprint / Cycle:** Sprint 11 (Full Demo Coverage)
**T# tasks:** T2 (BE endpoints matching FE contract)
**Branch:** `feature/sprint-11-fe-be-parallel` (off `origin/develop @ 64efaac`)

**Files I added/touched (BE only):**
- `src/backend/Modules/Companies/Application/DTOs/CompanyDto.cs` (NEW) — `CompanyTreeNodeDto` + `SubsidiaryListDto`
- `src/backend/Modules/Companies/Application/Services/CompanyService.cs` (MODIFIED) — `GetTreeAsync` now returns flat `IReadOnlyList<CompanyTreeNodeDto>`; `GetSubsidiariesAsync` returns `SubsidiaryListDto`; removed legacy `CompanyTreeNode` wrapper
- `src/backend/Modules/Finance/Application/FinanceDtos.cs` (MODIFIED) — added `HoldingDashboardDto`, `AccountDto`, `TransactionDto`
- `src/backend/Modules/Finance/Application/Services/FinanceService.cs` (NEW) — `IFinanceService` with 4 methods
- `src/backend/Host/Controllers/HoldingController.cs` (NEW) — `GET /api/holdings/dashboard` + alias
- `src/backend/Host/Controllers/AccountsController.cs` (MODIFIED) — refactored: kept legacy `/api/finance/accounts` + added new `/api/accounts` and `/api/accounts/{id}`
- `src/backend/Host/Controllers/TransactionsController.cs` (NEW) — `GET /api/transactions/recent?limit=N` + alias
- `src/backend/Host/Program.cs` (MODIFIED) — registered `IFinanceService`
- `src/backend/Tests/ERPSystem.Tests/Companies/CompanyTreeTests.cs` (NEW) — 3 tests
- `CHANGELOG.md` (MODIFIED) — Sprint 11 T2 entry

**Files I will NOT touch (FE Jimi's scope):**
- `src/frontend/lib/api-types.ts` (T1, FE contract — FE wins)
- `src/frontend/lib/api.ts` (FE wrapper)
- `src/frontend/app/(authenticated)/holding/page.tsx` (FE page)
- `src/frontend/app/(authenticated)/accounts/**` (FE page)
- `src/frontend/app/(authenticated)/transactions/**` (FE page)
- `src/frontend/app/(authenticated)/admin/companies/page.tsx` (FE page)
- `src/frontend/app/(authenticated)/reports/page.tsx` (FE page)
- `src/frontend/components/layout/AppShell.tsx` (FE layout)

**Tests I added:** 3 in `CompanyTreeTests.cs` (per Article 11, 1 test per new endpoint for `GetTreeAsync`).

**Constitution articles I respected:**
- Article 3 — `company_id` only (no `tenant_id` introduced)
- Article 6 (Dapper only, no EF Core)
- Article 8 (one test per endpoint)
- Article 10 (no push, no PR — LOCAL-ONLY mode per Anas mandate 2026-07-31 06:47 / 07:00 UTC)

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
_2026-07-31: Sprint 11 T2 — BE Jimi scope declaration (Mavis Local)_
