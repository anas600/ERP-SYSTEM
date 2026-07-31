# 🔧 AGENTS.md — src/backend/

> **Backend (.NET 9) source.** Read `/AGENTS.md` and `/src/AGENTS.md` first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

The .NET 9 backend API for ERP-SYSTEM. Hosts the application, exposes REST endpoints, and persists to PostgreSQL via Dapper.

## Ownership

| Subtree | Owner | Authority |
|---------|-------|-----------|
| `src/backend/Host/` | Jimi تنفيذي (Backend) | Entry point, controllers, middleware, hosted services |
| `src/backend/Modules/` | Jimi تنفيذي (per module) | Business modules (13 modules) |
| `src/backend/Shared/` | Jimi تن执行官 (shared) | Cross-cutting concerns (DataTypes, Events, Infrastructure, Migrations, SeedData) |
| `src/backend/Tests/` | Jimi تنفيذي (QA) | xUnit tests |

Tech Lead (Mavis Local) coordinates. Cloud (Siti) verifies.

## Local Contracts

### Stack
- **.NET 9** / **ASP.NET Core** / **Dapper** (NO EF Core) / **FluentMigrator** / **xUnit**.
- **PostgreSQL 17** via Npgsql.
- **BCrypt** for password hashing.
- **JWT** (HS256) for auth.

### Architecture (Constitution Article 3)
- ✅ `company_id` everywhere, NO `tenant_id`.
- ✅ `Company` entity, `user_companies` join table.
- ✅ `CompanyContext` (in `Shared/MultiTenancy/CompanyContext.cs` — folder should be renamed in future refactor).
- ✅ JWT `company_ids[]` + `X-Company-Id` header.
- ✅ Idempotent migrations.
- ✅ Batch inserts via `unnest()` for ≥ 10 rows.
- ✅ Atomic transactions for multi-row inserts.

### Code Standards
- Async/await for all I/O.
- Repository pattern via `Modules/<Module>/Infrastructure/Repositories/`.
- DTOs in `Modules/<Module>/Application/DTOs/`.
- Services in `Modules/<Module>/Application/Services/`.
- Domain entities in `Modules/<Module>/Domain/Entities/`.
- Migrations in `Modules/<Module>/Infrastructure/Migrations/`.
- One test per endpoint (per Constitution Article 11).

## Work Guidance

### Commands
```bash
cd src/backend
dotnet restore
dotnet build                       # Build
dotnet test                        # All tests
dotnet test --filter "ClassName"   # Specific test
dotnet run --project Host          # Run API on :5001
```

### Adding a New Module
1. Create `src/backend/Modules/<ModuleName>/` with:
   - `Domain/Entities/<Entity>.cs`
   - `Application/Services/<Service>.cs`
   - `Application/DTOs/<Dto>.cs`
   - `Infrastructure/Repositories/<Repository>.cs`
   - `Infrastructure/Migrations/<NNN>_<Description>.cs`
   - `AGENTS.md` (DOX format)
2. Register DI in `Host/Program.cs`.
3. Add migration to `Shared/Migrations/`.
4. Add controller in `Host/Controllers/`.
5. Add unit test in `Tests/ERPSystem.Tests/`.

### Migration Pattern
```csharp
[Migration(20260729120000)]
public class AddCompaniesTable : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE TABLE IF NOT EXISTS companies (id UUID PRIMARY KEY ...)");
    }
    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS companies");
    }
}
```

## Verification

- [ ] `dotnet build` — zero errors, zero warnings.
- [ ] `dotnet test` — all green.
- [ ] No `tenant_id`: `grep -r "tenant_id" src/backend/`.
- [ ] No secrets: `grep -rE "(password|connection).*=" src/backend/Host/appsettings.json`.
- [ ] Migration is idempotent (uses `IF NOT EXISTS` / `IF EXISTS`).
- [ ] All entities have `company_id` (if company-scoped).
- [ ] AGENTS.md updated in this scope.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`src/backend/Host/`](./Host/) | Entry point + controllers + middleware | Active |
| [`src/backend/Modules/`](./Modules/) | 13 business modules | Active |
| [`src/backend/Shared/`](./Shared/) | Cross-cutting concerns | Active |
| [`src/backend/Tests/`](./Tests/) | xUnit tests | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
