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
| `src/backend/Shared/CompanyContext/` | Jimi تنفيذي | `CompanyContext` (Phase 2 rename done, **Phase 3 scoped DI done** — `IHttpContextAccessor` + `HttpContext.Items`) |
| `src/backend/Shared/Audit/` | Jimi تنفيذي | Audit logging |

## Local Contracts

- **No business logic** in Shared. Only cross-cutting concerns.
- **All modules** depend on Shared, not vice versa.
- **No `tenant_id` anywhere.** Use `company_id`.

## CompanyContext (renamed in Sprint 10 Phase 2)

`src/backend/Shared/CompanyContext/` (formerly `MultiTenancy/`) contains:
- `CompanyContext.cs` — concrete implementation
- `ICompanyContext.cs` — interface
- `CompanyContextMiddleware.cs` — middleware that reads `X-Company-Id` + JWT claims

**Rename rationale (per Constitution Article 3):** the folder name `MultiTenancy/` was misleading because the system is **NOT** multi-tenant — it's a single Holding with many Companies. The misleading name was replaced with the actual artifact name in Sprint 10 Phase 2.

**Phase 3 (DONE — Sprint 10):** replaced the `AsyncLocal` static state with scoped DI + `IHttpContextAccessor` + `HttpContext.Items`. The interface (`ICompanyContext`) is unchanged (`Set`/`Clear` still on the interface for backward compat) — the implementation now reads/writes `HttpContext.Items`. DI registration is `AddScoped<ICompanyContext, CompanyContext>()` (already in `Host/Program.cs` since Phase 6.1b). Tests in `Tests/ERPSystem.Tests/Auth/CompanyContextTests.cs` were rewritten to mock `IHttpContextAccessor`.

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
| `src/backend/Shared/CompanyContext/` | `CompanyContext` + middleware + interface | Active |
| `src/backend/Shared/Audit/` | Audit logging | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
