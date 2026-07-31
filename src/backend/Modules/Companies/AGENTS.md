# 🏢 AGENTS.md — src/backend/Modules/Companies/

> **Companies module** (Multi-Company). Read all parent AGENTS.md files first.

**Last updated:** 2026-07-31 (Sprint 9 T1 — docs aligned with actual code)

---

## Purpose

Manages the `companies` table and related entities. The "C" in Multi-Company. The Holding itself is also a row in this table (single-table self-referencing hierarchy).

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي |
| **Schema review** | Anas (changes to holding/company boundaries) |

## Local Contracts

### Schema
- `companies` — `id`, `code` (unique), `name`, `slug` (unique), `legal_name`, `parent_company_id` (self-FK), `is_group` (boolean), `base_currency`, `is_active`.
- **Holding identification:** A company with `is_group = true` AND `parent_company_id IS NULL` is THE Holding (single-row per the architecture, code = '000').
- **Constraint:** `UNIQUE (code)` and `UNIQUE (slug)`.
- **Self-referencing hierarchy:** `parent_company_id → companies.id` allows any-depth tree. The Holding is the root (parent = null).
- **Soft delete** via `is_active = false`. No hard deletes.
- **No `holding_id` column** — the architecture doc describes a two-table model; the actual code is single-table self-referencing. See `docs/architecture/holding-company-refactor-proposal.md` for Phase 2 (rename MultiTenancy) and Phase 3 (scoped DI).
- **Each company has exactly one parent** (which may be the Holding, or another company for nested structures).
- **Each user has 0..N companies** (via `user_companies`).
- **No company is a tenant.** All data is `company_id` scoped.

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

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
