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

## Jimi Scope — 2026-07-29 (BE Jimi, Sprint 6 Wrap-up T3)

**Jimi type:** BE
**Sprint / Cycle:** Sprint 6 Wrap-up / Cycle N
**T# tasks:** T3 (Test Gap-Fill)
**Branch:** `feature/sprint-6-wrap-up`
**Off:** `origin/develop @ 7943f68`

### Files I created (4)
- `src/backend/Tests/ERPSystem.Tests/Companies/CompanyTreeTests.cs` (new) — 2 smoke tests for `GET /api/companies/tree` (CompanyService.GetTreeAsync). The endpoint had ZERO test coverage; this fills a real Sprint 4+5 gap.
- `src/backend/Tests/ERPSystem.Tests/Finance/ChartOfAccountsServiceTests.cs` (new) — 2 smoke tests for `GET /api/finance/accounts` (ChartOfAccountsService.ListAsync). The CoA service had ZERO test coverage in the Tests project; this starts the coverage.
- `CHANGELOG.md` (modified) — added `## Sprint 6 — Wrap-up (2026-07-29)` section per worker contract.
- `src/backend/AGENTS.md` (this file, modified) — added this scope block per worker contract.

### Files I will NOT touch (out of scope)
- `src/backend/Host/` — no production code changes for test gap-fill
- `src/backend/Modules/` — no production code changes
- `src/backend/Shared/` — no production code changes
- `src/frontend/**` — FE Jimi's scope (T4)
- `docs/workflow/**` — already in CHANGELOG and sprint hand-off
- `.github/workflows/mavis-coordination/state.json` — cron's territory (already modified by cron tick, NOT staged by me)
- `WORKFLOW.md`, `CONSTITUTION.md`, `CONSTITUTION-LOCAL-TEAM.md`, `INTER-TEAM-PROTOCOL.md` — governance files (Mavis Local only)

### Tests I added (4 new, 0 modified, 0 removed)
- `CompanyTreeTests.GetTreeAsync_HoldingAndTwoSubsidiaries_BuildsOneRootWithTwoChildren` — happy path: 1 Holding + 2 subsidiaries → tree has 1 root with 2 children
- `CompanyTreeTests.GetTreeAsync_EmptyRepository_ReturnsEmptyRootsList` — edge case: empty repo → 0 roots
- `ChartOfAccountsServiceTests.ListAsync_HappyPath_MapsAllAccountsToResponses` — happy path: 3 accounts → 3 mapped responses with all fields preserved
- `ChartOfAccountsServiceTests.ListAsync_EmptyRepository_ReturnsEmptyList` — edge case: empty repo → empty list (not null, not 404)

### Pre-existing test status
- Baseline before this scope: 465 tests, 433 passed, 2 failed (pre-existing RetentionTests), 30 skipped
- After this scope: 469 tests, 437 passed (+4), 2 failed (same pre-existing RetentionTests, NOT introduced by me), 30 skipped
- Zero regressions introduced.

### Out-of-scope discoveries (flag for Mavis Local, not absorbed)
- **CoA service has 4 of 5 endpoints still untested:** `GetByIdAsync`, `GetByCodeAsync`, `CreateAsync`, `DeleteAsync` on `ChartOfAccountsService` are still uncovered. Adding tests for them is a non-trivial follow-up (Create needs a happy + duplicate-code + missing-parent test; Delete needs has-postings + has-children + happy test). Recommended for a focused T3.5 task in a future sprint.
- **`FakeDbConnectionFactory` does not support SQL column aliases** — this is a project-wide limitation that silently breaks any test which asserts on columns aliased in SQL (e.g. `legal_name AS LegalName`, `parent_company_id AS ParentCompanyId`). My CompanyTreeTests works around this by using the projected column names in `AddRow`. A FakeDb enhancement to honor `AS` aliases would unlock property-level assertions for the existing CompaniesListTests (which currently only asserts on count, not on mapped values).
- **Two `RetentionTests` fail on local Docker** (`password authentication failed for user "postgres"`) — confirmed pre-existing per Sprint 5 + 6 CHANGELOG (the `erp_test_system` test DB is not in the local Docker stack). Not in my scope; not a regression. CI runs them against a real Postgres and they pass there.

### Constitution articles I respected
- Article 3 (`company_id` only) — no `tenant_id` added (verified by `grep`)
- Article 6 (one branch) — committed to `feature/sprint-6-wrap-up` only
- Article 8 Rule 4 (one test per endpoint) — 1 test per new endpoint covered
- Article 8 Rule 6 (no EF Core) — Dapper + xUnit only
- Article 8 Rule 10 (document in AGENTS.md) — this scope block

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
