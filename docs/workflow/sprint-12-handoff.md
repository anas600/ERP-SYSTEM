# Sprint 12 Hand-off — Local Test Infra + Holding Affirmation (2026-07-31)

**Author:** Mavis Coordinator (v1.8 governance)
**Audience:** Mavis Local + Admin Team
**Branch target:** `feature/sprint-12-local-test-psql`
**Mode:** LOCAL-ONLY (no push, no PR until Anas says "ادفع")

---

## 🎯 Sprint 12 Goal

> **"تطوير نظام الشركة القابضة، وليس مالتي تينانت. ويجب العمل على قاعدة البيانات psql."**
> — Anas, 2026-07-31 07:46 UTC

**Two parallel deliverables:**

1. **P0 — Local test infrastructure: real psql**
   - Wire `dotnet test` to use a real local PostgreSQL (Mavis Local's machine has psql installed).
   - Eliminate the 2 pre-existing `RetentionTests` failures that exist because of in-memory mock DB.
   - **Acceptance:** `dotnet test` runs against `localhost:5432` (or a Testcontainers-managed Postgres), all 441+ tests pass.

2. **P0 (parallel) — Architecture reaffirmation + CI guard**
   - Reaffirm Article 3 (`company_id` only, NO `tenant_id`).
   - Add a CI check: `grep -r "tenant_id" src/ --exclude-dir=node_modules --exclude-dir=bin` must return 0 hits.
   - **Acceptance:** PRs without the check pass through `.github/workflows/no-tenant-id.yml` automatically. Failing check blocks the PR.

---

## 📐 Architecture Affirmation (Article 3, NON-NEGOTIABLE)

**Anas's directive (2026-07-31 07:46 UTC):** "أنا الآن أحتاج أن أؤكد على شي — تطوير نظام الشركة القابضة وليس مالتي تينانت أي ليس متعدد المستأجرين"

**Sprint 12 must enforce:**

| Rule | Status | CI Check |
|---|---|---|
| `company_id` everywhere, NO `tenant_id` | ✅ Active since Article 3 | `grep -r "tenant_id" src/` → 0 hits |
| `Company` entity, NO `Tenant` entity | ✅ Active | `grep -r "class Tenant" src/backend/` → 0 hits |
| `CompanyContext`, `CompanyMiddleware`, `[CompanyAuthorize]` | ✅ Active since Sprint 10 Phase 2/3 | n/a (file existence) |
| `user_companies` join table | ✅ Active | n/a |
| JWT carries `company_ids[]` + `X-Company-Id` header | ✅ Active | n/a |
| Holding-level queries require `holding_admin` role | ✅ Active | role-based middleware |

**Sprint 12 deliverable:** add `.github/workflows/no-tenant-id.yml` that runs the grep on every PR. Failure blocks the merge.

---

## 🛠 P0 Task 1 — Local Test Infrastructure (psql)

### Problem
- `dotnet test` currently uses in-memory mocks for most tests.
- 2 pre-existing tests (`ArchiveMetadata_InsertAndQuery`, `PartitionedAuditLog_AcceptsInserts`) fail because they require real PostgreSQL features (table partitioning, JSONB, `unnest()`).
- Anas: "أخطاء تظهر عند كتابة اختبارات محلية وتفشل بسبب عدم وجود قاعدة بيانات حقيقية. فأذكركم أنها مثبتة لدي."

### Solution Options

**Option A (recommended):** Wire tests to Mavis Local's `local-docker` Postgres.
- **Pros:** reuses existing infra (Local Docker Postgres at `localhost:5432`), fastest test cycles, no extra services.
- **Cons:** requires the local Docker Postgres to be running during tests. CI may not have this.
- **Implementation:**
  1. Add `src/backend/Tests/ERPSystem.Tests/appsettings.Test.json` (gitignored) with `ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=erp_test;Username=erp;Password=erp`.
  2. Update `WebApplicationFactory<Program>` to load this config in `Testing` environment.
  3. Update the 2 failing tests to use the real DB (they already do — just need the connection).
  4. Add a fixture that creates + drops the test schema per test run.

**Option B:** Testcontainers (spin up Postgres per test run).
- **Pros:** works in CI without external services.
- **Cons:** slower (10-20s container startup per test run), more complex.
- **Implementation:** add `Testcontainers.PostgreSql` NuGet, write a `PostgresContainerFixture` that boots a container, applies migrations, runs tests, tears down.

**Option C:** Hybrid — Local uses A (fast), CI uses B (portable).
- **Pros:** best of both worlds.
- **Cons:** more code to maintain.

**Recommendation:** Option A for Sprint 12. If CI integration is needed in Sprint 13, add Option B as a parallel path.

### Acceptance Criteria
- [ ] `src/backend/Tests/ERPSystem.Tests/appsettings.Test.json.example` committed (the `.example` is committed, `.json` is gitignored).
- [ ] `WebApplicationFactory` reads from `appsettings.Test.json` when `ASPNETCORE_ENVIRONMENT=Testing`.
- [ ] `dotnet test` with `ASPNETCORE_ENVIRONMENT=Testing` runs against the real local Postgres.
- [ ] All 2 previously-failing `RetentionTests` now pass.
- [ ] Total test count: 441+ pass, 0 fail, 30 skip (unchanged skip count).
- [ ] Test run time: < 30s (Local Docker Postgres should be 5-10x faster than Supabase).

### Out of Scope (Sprint 13+)
- Testcontainers integration for CI.
- Migration auto-application in test setup.
- Test data seeding for retention/partitioning tests.

---

## 🛠 P0 Task 2 — `no-tenant-id` CI Guard

### Problem
- No automated check prevents a developer from introducing `tenant_id` in a new file.
- Current protection is manual (PR review + AGENTS.md rule).
- Risk: a single PR could regress the architecture.

### Solution
- Add `.github/workflows/no-tenant-id.yml` that runs on every PR.
- Steps:
  1. Checkout code.
  2. Run `grep -r "tenant_id" src/ --exclude-dir=node_modules --exclude-dir=bin || true`.
  3. If any hits, fail the check.
- **Exclusions:** `docs/`, `CHANGELOG.md`, `AGENTS.md` (these may reference `tenant_id` historically).

### Acceptance Criteria
- [ ] `.github/workflows/no-tenant-id.yml` exists and is valid YAML.
- [ ] PR with a new `tenant_id` introduction fails the check.
- [ ] PR with only `company_id` additions passes the check.
- [ ] Check is added to the 6 required checks list in `AGENTS.md` (optional, can be added in Sprint 13).

---

## 🧪 Verification (T2 for Sprint 12)

Before opening the PR (when Anas says "ادفع"):

```bash
cd C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-12
dotnet build "src/backend/Tests/ERPSystem.Tests/ERPSystem.Tests.csproj"   # 0 errors
ASPNETCORE_ENVIRONMENT=Testing dotnet test "src/backend/Tests/ERPSystem.Tests/ERPSystem.Tests.csproj" --no-build  # 441+ pass, 0 fail
cd src/frontend
npm run type-check   # 0 errors
npm run build        # success
```

Manual check:
```bash
grep -r "tenant_id" src/ --exclude-dir=node_modules --exclude-dir=bin  # 0 hits
```

---

## 🎭 Worker Allocation

**1 Jimi (BE/Infra focused)** — recommended, since this is a small, focused sprint:
- **Jimi BE+Infra:** implements both P0 tasks (test infra + CI guard).
- Estimated time: 1.5-2 hours.

**Could split into 2 Jimis** if scope grows:
- **Jimi 1 (BE):** test infra (Option A) + appsettings.Test.json + fixture.
- **Jimi 2 (DevOps):** CI workflow YAML + AGENTS.md update.

For Sprint 12, **1 Jimi is sufficient**. The tasks are tightly coupled (both touch `ERPSystem.Tests`).

---

## 📚 Reference

- **Sprint 11 retrospective:** `docs/team-charters/retrospectives/sprint-11-retro.md`
- **Sprint 11 hand-off:** `docs/workflow/sprint-11-fe-be-parallel.md`
- **Architecture (Article 3):** `docs/architecture/holding-company-architecture.md`
- **Constitution (Article 3, 8, 10):** `/AGENTS.md`
- **Anas 07:46 UTC directive:** "تطوير نظام الشركة القابضة وليس مالتي تينانت. والعمل على psql."

---

## 🏁 Sprint 12 Definition of Done

- [ ] `dotnet test` runs against real local psql.
- [ ] All 441+ tests pass.
- [ ] `.github/workflows/no-tenant-id.yml` exists and runs on PRs.
- [ ] `grep -r "tenant_id" src/` returns 0 hits.
- [ ] `dotnet build` 0 errors, `npm run build` success, `npm run type-check` 0 errors.
- [ ] CHANGELOG updated.
- [ ] Retrospective written: `docs/team-charters/retrospectives/sprint-12-retro.md`.
- [ ] LOCAL-ONLY mode maintained. No push, no PR until Anas says "ادفع".

**Stop. Wait for Anas. Do not push. Do not open PR.**
