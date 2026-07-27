# 📦 Hand-Off v1 — Cycle 2: 6.2 Tests Refactor + 3-Layer DB Setup

> **From:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
> **To:** Mavis Local (Team Lead) — your session, Windows  
> **Cycle:** 2 / 20 — **ACTIVE ✅ (Approved by Anas via Telegram)**  
> **Created:** 2026-07-27 21:14 UTC (DRAFT)
> **Activated:** 2026-07-27 21:38 UTC (per Anas's Telegram voice 'Cycle 2 go, system input')

---

## 🎯 Cycle 2 Scope (proposed)

### Block A: 6.2 Tests Refactor (signature migration tenant_id → company_id)

**Background:** Phase 6 dropped `tenant_id` and added `company_id`. Tests still use the old names. Need to update them so CI works on the new schema.

**Confirmed test count:** 31 C# test files in `src/backend/Tests/ERPSystem.Tests/` (verified via git tree)

**Tasks:**
- **T1**: Search for `tenant_id`, `TenantId`, `Tenant`, `ITenantContext`, `TenantContext` across all 31 test files
- **T2**: Rename `tenant_id` → `company_id` (case-sensitive) in test files
- **T3**: Update test signatures (constructor params, method args, variable names)
- **T4**: Update test fixtures/mocks (`FakeDbConnectionFactory`, `ErpWebApplicationFactory`, `TestJwtGenerator`)
- **T5**: Update assertion expectations (`tenantId` → `companyId`)
- **T6**: Run `dotnet test` → all green
- **T7**: Update e2e specs in `/tests/` (10 Playwright files: admin, finance, hr, inventory, etc.) — add multi-company scenarios if missing
- **T8**: Add 3 new test cases in `Auth/` folder:
  - `HoldingBootstrap_Seeds_DefaultHolding_And_CoA` (integration test)
  - `UserCompany_Limits_Access_To_Assigned_Companies` (unit test for RbacPolicy)
  - `CompanySwitcher_Switches_Active_Company_In_Context` (unit test for CompanyContext)

**Files likely affected:** ~31 xUnit files + 10 Playwright e2e specs

### Block B: 3-Layer DB Setup (Anas-dependent, can be parallel)

**Background:** Per Muhammad's analysis (3-layer DB architecture), we need:
- **Dev**: Local PG (Anas installs) + Cloud Supabase dev (smoke + Playwright)
- **Staging**: Separate Supabase STAGING project (clean + reset per cycle)
- **Production**: Deferred per DEC-068

**Tasks (for Anas to set up in parallel):**
- **T9 (Anas)**: Create Supabase STAGING project (different from dev)
- **T10 (Anas)**: Add STAGING_* secrets to GitHub:
  - `STAGING_SUPABASE_HOST`
  - `STAGING_SUPABASE_PORT`
  - `STAGING_SUPABASE_DB`
  - `STAGING_SUPABASE_USER`
  - `STAGING_SUPABASE_PASSWORD`
- **T11 (Anas)**: Add STAGING_DATABASE_URL to .NET backend
- **T12 (Mavis Local)**: Create `reset-staging-db.yml` workflow (manual trigger)
- **T13 (Mavis Local)**: Update `e2e.yml` to use STAGING_* secrets
- **T14 (Mavis Local)**: Add e2e.yml auto-screenshot step (per Muhammad's smoke spec)

---

## 📋 Acceptance Criteria

| Criterion | Verification |
|-----------|-------------|
| All xUnit tests pass with `company_id` (not `tenant_id`) | `dotnet test` green (31 test files) |
| All 10 Playwright e2e specs pass | `npx playwright test` green |
| 3 new test cases pass | `dotnet test --filter "HoldingBootstrap\|UserCompany\|CompanySwitcher"` green |
| STAGING project created (Anas) | Connection test from local |
| reset-staging-db.yml works | Manual trigger → clean + reset |
| e2e.yml uses STAGING secrets | `cat .github/workflows/e2e.yml \| grep STAGING` |
| tsc + dotnet build clean | 0 errors |

---

## ⏰ Estimated Time

- **Block A (Tests Refactor)**: 3-4 hours (31 files, more than initially estimated)
- **Block B (3-Layer DB)**: 1-2 hours (mostly Anas)
- **Total**: 4-6 hours (with parallelism)

---

## 🔧 Verification Plan

```bash
# 1. Build
npx tsc --noEmit
dotnet build Host/ERP-SYSTEM.csproj

# 2. xUnit tests
dotnet test --filter "FullyQualifiedName~ERP"

# 3. New test cases
dotnet test --filter "HoldingBootstrap_Seeds_DefaultHolding_And_CoA"
dotnet test --filter "UserCompany_Limits_Access_To_Assigned_Companies"
dotnet test --filter "CompanySwitcher_Switches_Active_Company_In_Context"

# 4. Playwright
npx playwright test
```

---

## 🚨 Risk Notes

- **R1**: If xUnit tests use `ITenantContext` interface (still in code), we keep it as deprecated alias and refactor in a separate cycle (avoid scope creep)
- **R2**: If Playwright can't connect to STAGING (network issue), fallback to dev Supabase + manual screenshot
- **R3**: If STAGING project creation is delayed, Block B is deferred and Block A proceeds standalone
- **R4**: 31 test files (not 23) — adjust time estimate, more migration surface

---

## 📡 Async Protocol (Reminder)

- Create cron `monitor-cycle-2-pr-merge` when PR opens (every 3 min, silent on no-change)
- Update cycle-2 hand-off as work progresses
- When PR ready, notify me (Siti) for merge
- After merge, self-delete cron

---

## 🤝 🟢 STATUS

> **Approved by Anas: 2026-07-27 21:38 UTC** (Telegram voice: 'Cycle 2 go, system input')  
> Hand-off pushed to develop. Mavis Local: read this file and begin execution.
