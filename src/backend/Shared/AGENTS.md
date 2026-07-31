# 🔄 AGENTS.md — src/backend/Shared/

> **Cross-cutting concerns.** Read `/AGENTS.md`, `/src/AGENTS.md`, and `/src/backend/AGENTS.md` first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

Cross-cutting code shared across modules: DataTypes, Events, Infrastructure, Migrations, SeedData.

## Ownership

| Subtree | Owner | Authority |
|---------|-------|-----------|
| `src/backend/Shared/DataTypes/` | Jimi تنفيذي | JSON schema, data types |
| `src/backend/Shared/Events/` | Jimi تنفيذي | Domain events |
| `src/backend/Shared/Infrastructure/` | Jimi تنفيذي | Cross-cutting infra (auth, logging) |
| `src/backend/Shared/Migrations/` | Jimi تنفيذي | FluentMigrator migrations |
| `src/backend/Shared/SeedData/` | Jimi تنفيذي | Default seed data |
| `src/backend/Shared/MultiTenancy/` | Jimi تنفيذي | ⚠️ **Misleading folder name** — contains `CompanyContext.cs`. Rename to `CompanyContext/` in future refactor. |
| `src/backend/Shared/Audit/` | Jimi تنفيذي | Audit logging |

## Local Contracts

- **No business logic** in Shared. Only cross-cutting concerns.
- **All modules** depend on Shared, not vice versa.
- **No `tenant_id` anywhere.** Use `company_id`.

## ⚠️ Folder Rename Needed

`src/backend/Shared/MultiTenancy/` contains:
- `CompanyContext.cs` (correct content)
- `CompanyContextMiddleware.cs` (correct content)
- `ICompanyContext.cs` (correct content)

**The folder name is misleading** (per Constitution Article 3 — NO Multi-Tenancy). Future refactor should rename to `CompanyContext/`. Tracked but out of scope for current sprints.

## Work Guidance

### Adding to Shared
- Only if the code is **truly cross-cutting** (used by 3+ modules).
- Otherwise, put it in the specific module.
- Update this AGENTS.md Child DOX Index.

## Verification

- [ ] `dotnet build` — zero errors.
- [ ] No business logic in Shared.
- [ ] No `tenant_id`: `grep -r "tenant" src/backend/Shared/`.
- [ ] All changes documented here.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| `src/backend/Shared/DataTypes/` | JSON schema, data types | Active |
| `src/backend/Shared/Events/` | Domain events | Active |
| `src/backend/Shared/Infrastructure/` | Cross-cutting infra | Active |
| `src/backend/Shared/Migrations/` | FluentMigrator migrations | Active |
| `src/backend/Shared/SeedData/` | Default seed data | Active |
| `src/backend/Shared/MultiTenancy/` | ⚠️ CompanyContext (rename needed) | Active |
| `src/backend/Shared/Audit/` | Audit logging | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
