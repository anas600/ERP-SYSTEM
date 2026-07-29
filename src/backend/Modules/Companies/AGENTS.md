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

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
