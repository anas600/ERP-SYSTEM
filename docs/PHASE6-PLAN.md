# Phase 6: Multi-Tenant → Multi-Company Refactor — Execution Plan

> **Status:** Awaiting owner sign-off (9 items below)
> **Prepared by:** Mavis (orchestrator) based on Jamie التحليلي analysis
> **Constitutional basis:** `CONSTITUTION.md` §3 (Multi-Company architecture, no Multi-Tenancy)
> **Analysis reference:** `docs/PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md` (801 lines, 8 sections)

---

## 🎯 Goal

**Eliminate multi-tenancy entirely.** Move from a `tenant_id` outer-isolation model to a single Holding Company per deployment with a `company_id` inner-isolation model. Clean Slate for the database schema.

---

## 🛑 Open Decisions (need Anas approval before Phase 6.1)

> These are Mavis's recommendations. Owner override at any point.

### Decision 1: Roles — global or per-company?
- **Recommendation:** **Global** for Phase 6.0. The 4 default roles (Admin, Accountant, ProjectManager, Viewer) become system-wide.
- **Rationale:** Simpler bootstrap (no per-company role creation), matches the "single Holding" model. If per-company role customization is needed later, add `user_company_roles` join table.
- **Override:** `user_company_roles` from start (1 extra day)

### Decision 2: Email uniqueness — global or per-company?
- **Recommendation:** **Global** for Phase 6.0. `UNIQUE(email)` on `users` table.
- **Rationale:** Standard SaaS pattern. The user has one identity across all companies. If multi-company email is needed later, add `ix_users_company_email` composite.
- **Override:** `UNIQUE(company_id, email)` instead

### Decision 3: `X-Company-Id` header — yes or no?
- **Recommendation:** **Yes** + JWT `company_ids[]` claim.
- **Rationale:** JWT has the full list of companies the user can access; the `X-Company-Id` header picks the active one per request. Frontend `<CompanySwitcher />` updates the header on click. Backend validates header is in JWT list.
- **Override:** No header (active company always = JWT default) — simpler but less flexible

### Decision 4: `user_companies` schema — final shape
- **Recommendation:**
  ```sql
  CREATE TABLE user_companies (
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    company_id  uuid NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    is_default  boolean NOT NULL DEFAULT true,
    assigned_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, company_id)
  );
  CREATE INDEX ix_user_companies_company ON user_companies(company_id);
  CREATE INDEX ix_user_companies_default ON user_companies(user_id) WHERE is_default;
  ```
- **Override:** Add `role_in_company` text column (for per-company roles)

### Decision 5: API contract for new auth flow
- **Recommendation:** See Section "New API Contracts" below. Default Holding means the first user is automatically an Admin.
- **Override:** Add Holding selection step in Register UI (1 extra day)

### Decision 6: Production data audit
- **Status:** Per `Program.cs:359-379` (Fresh Build Mode) + the analysis report (Section 5.4), HF Space is empty.
- **Action needed:** Owner to **explicitly confirm** "HF Space has no production data; clean slate is safe."

### Decision 7: Migration deployment strategy
- **Recommendation:** Single PR develop → main with all Phase 6.0 + 6.1 changes. 1 atomic deploy.
- **Rationale:** Avoid half-migrated state. HF Space goes down for 3-5 min during rebuild, then comes back on the new schema.
- **Override:** Phase 6.0 (schema) first, then 6.1 (code) in 2 PRs (more conservative)

### Decision 8: Atomicity E2E test rewrite
- **Recommendation:** Replace "no orphan tenants" with "no orphan users" in `e2e/auth.spec.ts`. Same pattern (abort 5 register requests, verify each email can't log in).
- **Override:** Drop the test entirely (less confidence in atomicity)

### Decision 9: Rollback strategy if Phase 6 breaks production
- **Recommendation:** Revert main to the last v5.0.4 commit (`e108f27`). The v5.0.4 schema is the last known-good state. The Initial Schema migration is destructive (DROP CASCADE) — there's no clean rollback. Reverting the merge commit re-deploys v5.0.4.
- **Override:** Pre-Phase 6 backup of Supabase (manual `pg_dump`)

---

## 📦 New API Contracts (post-Phase 6)

### `POST /api/auth/register`
```json
// Request
{
  "email": "user@example.com",
  "password": "P@ssword123!",
  "fullName": "Anas Owner",
  "holdingName": "Demo Holding Co."  // optional, defaults to "Holding Enterprise"
}

// Response 200
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "accessTokenExpiresAt": "2026-07-25T03:30:00Z",
  "refreshTokenExpiresAt": "2026-08-08T03:00:00Z",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "fullName": "Anas Owner",
    "defaultCompanyId": "uuid-of-holding",
    "companyIds": ["uuid-of-holding"],
    "roles": ["Admin"]
  },
  "holdingCompanyId": "uuid-of-holding"
}
```

### `POST /api/auth/login`
```json
// Request
{ "email": "user@example.com", "password": "P@ssword123!" }
// Response: same as register, no holdingName param
```

### `GET /api/auth/me` (Bearer token)
```json
// Response 200
{
  "id": "uuid",
  "email": "user@example.com",
  "fullName": "Anas Owner",
  "defaultCompanyId": "uuid-of-holding",
  "companyIds": ["uuid-of-holding", "uuid-of-subsidiary-1"],
  "roles": ["Admin"]
}
```

### `GET /api/companies` (Bearer token)
```json
// Response 200
[
  { "id": "uuid-holding", "code": "000", "name": "Demo Holding Co.", "isGroup": true, "isHolding": true, "parentCompanyId": null, "baseCurrency": "LYD", "isActive": true },
  { "id": "uuid-sub1", "code": "001", "name": "Branch 1", "isGroup": false, "isHolding": false, "parentCompanyId": "uuid-holding", "baseCurrency": "LYD", "isActive": true }
]
```

### `X-Company-Id` header
- **Required** for any endpoint that filters by company (e.g., `/api/finance/accounts`, `/api/inventory/items`)
- **Optional** for global endpoints (e.g., `/api/auth/me`, `/api/companies`)
- **Validation:** backend checks header value is in user's `companyIds` (returns 403 if not)

### JWT claims (post-Phase 6)
```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "default_company_id": "uuid-of-holding",
  "company_ids": ["uuid-of-holding", "uuid-of-sub1"],
  "role": "Admin",  // backward compat: the highest-privilege role
  "exp": 1234567890
}
```

---

## 📂 Phased Execution

### Phase 6.0 — Schema Reset (1 PR, ~1-2 dev days)
1. `Phase6_InitialSchema_20260725_120000.cs` migration
2. Update 35 DataType JSONs
3. Create `user_companies.json`
4. Delete `tenants.json`
5. Rewrite `seed_meta.json`
6. Delete seed files (AlFajr/AlBurj/Realistic)
7. Add `Deployment:DefaultHoldingName` + `DefaultCurrency` to `appsettings.json`
8. New `DefaultHoldingBootstrapHostedService` (idempotent Holding + CoA + UoMs seed at boot)
9. Build + E2E (smoke test: register creates user, no tenant)

**PR:** `feature/phase6-0-schema-reset` → develop (no auto-deploy to main)
**E2E:** 4 existing tests + 1 new `HoldingBootstrap_Seeds_DefaultHolding_And_CoA`

### Phase 6.1 — Backend Code (1-2 PRs, ~3-4 dev days)
1. Delete `src/backend/Shared/MultiTenancy/` (3 files)
2. Delete `src/backend/Host/Utilities/TenantCache.cs`
3. Update `Program.cs`: remove tenant DI + middleware
4. Update entities: remove `TenantId` from User, Role, Company, CostCenter, etc.
5. Delete `Tenant.cs`, `ITenantRepository`, `TenantRepository`
6. Update all 50+ repos: remove `WHERE tenant_id`
7. Update all 25+ services: drop `Guid tenantId` param
8. Add `ICompanyContext` + `CompanyContext` (header-based)
9. Update `AuthService.cs` (Register/Login/Refresh without tenant)
10. Update `AuthDtos.cs`, `JwtTokenService.cs`, `AuthDtos.cs`
11. Update `IAuditLogger` + `AuditLogger.cs`
12. Re-run `EntityRepoEnhance` to regenerate 24+ `*.g.cs`
13. Build + 23 xUnit tests pass

**PR:** `feature/phase6-1-backend-code` → develop (could be split into 6.1a entities+repos, 6.1b auth+context)

### Phase 6.2 — Tests + E2E (1 PR, ~1-2 dev days, parallel with 6.3)
1. Update 24 xUnit test files (signature changes)
2. Update `e2e/auth.spec.ts`: atomicity = "no orphan users"
3. Add `HoldingBootstrap_Seeds_DefaultHolding_And_CoA` test
4. Add `UserCompany_Limits_Access_To_Assigned_Companies` test
5. Add `CompanySwitcher_Switches_Active_Company_In_Context` test

**PR:** `feature/phase6-2-tests` → develop

### Phase 6.3 — Frontend (1 PR, ~1 dev day, parallel with 6.1)
1. Update `lib/api.ts`: remove `tenantId`, add `defaultCompanyId` + `companyIds`
2. Add `X-Company-Id` header in axios interceptor
3. Add new `<CompanySwitcher />` component in `AppShell`
4. Update `lib/companyContext.ts` + `useAuth.ts`
5. Update `app/register/page.tsx`: remove `tenantName` field
6. Update all 7 admin/finance/inventory/project pages

**PR:** `feature/phase6-3-frontend` → develop

### Phase 6.4 — Docs (1 PR, ~0.5-1 dev day, last)
1. Update root `AGENTS.md`: "Multi-Tenant" → "Multi-Company"
2. Update `docs/PLAN.md`: Phase 6 entry
3. Update `docs/CHANGELOG.md`: Phase 6 release notes
4. Update `src/backend/Modules/Identity/AGENTS.md` + 8 other module AGENTS.md
5. Update `src/frontend/AGENTS.md`
6. Update `docs/research/gap-analysis.md`
7. Update `docs/dec-103a/ARCHITECTURE.md` + `API.md`
8. Update `RUNBOOK.md` + `STATUS.md`

**PR:** `feature/phase6-4-docs` → develop

### Phase 6.5 — CI / Hardening (1 PR, ~0.5 dev day, last)
1. Add `phase6-migration-verify.yml` workflow (asserts zero `tenant_id` columns after fresh deploy)
2. Update HF Space env vars: drop `SUPABASE_TENANT_ID` if it exists
3. Add `Deployment:*` keys to `appsettings.json`

**PR:** `feature/phase6-5-ci` → develop

### Final: develop → main (1 PR, 1 HF deploy)
- All Phase 6 PRs merged to develop
- E2E all green
- 1 final PR develop → main → admin merge → HF deploy (~5 min)
- Watch HF logs for migration execution + 5-10 min smoke test

---

## 📊 Total Effort Estimate

| Path | Sequential | Parallelized (6.3 + 6.4 + 6.5 alongside 6.1) |
|------|---:|---:|
| 6.0 Schema | 1.5 days | 1.5 days |
| 6.1 Backend | 3.5 days | 3.5 days |
| 6.2 Tests | 1.5 days | 1.5 days |
| 6.3 Frontend | 1 day | **0 (parallel)** |
| 6.4 Docs | 0.5 day | **0 (parallel)** |
| 6.5 CI | 0.5 day | **0 (parallel)** |
| **Total** | **8.5 days** | **6.5 days** |

---

## ⚠️ Risk Register

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| HF deploy takes > 60s on cold start → 504 | Medium | Medium | Pre-warm container; or set `autosleep: false` on HF Space |
| Migration runs but DataTypeMigrator has residual `tenant_id` reference | Low | High | Phase 6.5 adds `phase6-migration-verify.yml` |
| Frontend deploy before backend → API mismatch | Medium | High | Phase 6.3 must be merged AFTER 6.1 (or use feature flag) |
| Production data exists on HF Space (contradicts Fresh Build Mode) | Low | Critical | Decision 6: explicit owner confirmation |
| Supabase auth circuit breaker (28P01) during heavy migration | Low | Medium | DEC-096 fix already applied (URL-decoded password) |

---

## ✅ Definition of Done (per phase)

- All files in phase scope changed + committed
- `dotnet build` → 0 errors, 0 new warnings
- `tsc --noEmit` → 0 errors
- `dotnet test` → 100% pass
- `npm run e2e` → 0 failures
- `docs/CHANGELOG.md` updated
- `AGENTS.md` (root + module) updated
- PR opened + reviewed + merged
- HF Space deployment verified (smoke test)

---

## 🎬 Next Action

**Anas/Siti review the 9 sign-off items at the top of this document.**

Once approved, Mavis delegates to **Jamie Executive** for Phase 6.0 (Schema Reset). Mavis reviews PR + approves before merge to develop.
