# 📜 CHANGELOG — ERP-SYSTEM

> **Per-sprint changelog.** Newest first. Concise.

**Format:**
```
## Sprint N — Title (YYYY-MM-DD)
### Added
### Changed
### Fixed
### Removed
```

---

## Sprint 9 — Demo Polish (2026-07-31) 🟡 IN PROGRESS

**Goal:** Final UI/UX polish on the holding-company demo — error boundary, loading skeletons, AR+EN i18n foundation, company-switcher feedback, plus BE-FE contract alignment so the demo's FE can call the BE with confidence.

### Added (FE Jimi 3 — T3)
- `src/frontend/lib/i18n.ts` — i18n foundation: `useTranslation()` hook + `t(key, locale)` helper + AR/EN dictionary (6 keys: `error.{unexpected,network,unauthorized,forbidden}`, `loading.{companies,dashboard,holding}`). Default locale `ar` (Arabic primary per Constitution).
- `src/frontend/components/ui/ErrorBoundary.tsx` — React class-based error boundary with bilingual default fallback. Composable wrapper for client component trees (complements Next.js route-level `error.tsx` files).

### Changed (FE Jimi 3 — T3)
- `src/frontend/app/(authenticated)/layout.tsx` — wraps `AppShell` in `<ErrorBoundary>`. `SessionTimeoutModal` kept as sibling (outside boundary) so a page-tree crash doesn't disable the session-timeout safety net.
- `src/frontend/components/layout/CompanySwitcher.tsx` — added `switching` state with visual feedback (`Loader2` spinner + `aria-busy`) during `router.refresh()`. Trigger button is disabled while the refresh is in flight, so the user sees a spinner instead of a frozen UI between "click company" and "data reload".
- `src/frontend/app/(authenticated)/holding/page.tsx` — replaced inline `CompanyCardSkeleton` with the shared `<SkeletonCard />` from `@/components/ui` (consistency win; removes a duplicate definition).

### Notes
- T1 (BE Jimi 1) — done; see entry below.
- T2 (BE Jimi 2 — this Jimi) — done; see entry below.
- T3 (FE Jimi 3 — this entry) — done; awaiting `npm run type-check` + `npm run build` verification.
- Existing `SkeletonCard`, `SkeletonTable`, `SkeletonPage`, `TableSkeleton` and route-level `error.tsx` files were already in place from prior sprints — T3 adds the composable `<ErrorBoundary>` and i18n foundation on top.

### Added (BE Jimi 2 — T2, BE-FE contracts)
- `src/frontend/lib/api-types.ts` (NEW, 400 lines) — TypeScript types mirroring C# DTOs (Guid→string, DateTime→string, nullable markers). Hand-written (not NSwag-generated) to keep the demo simple.
- `src/backend/Host/Program.cs` — Swashbuckle registered with `IncludeXmlComments` for Swagger UI docs.
- `src/backend/Host/Controllers/AccountsController.cs` — `[ProducesResponseType]` attributes for OpenAPI annotation.
- `src/backend/Host/Controllers/CompaniesController.cs` — `[ProducesResponseType]` attributes.
- `src/backend/Host/Controllers/UsersController.cs` — `[ProducesResponseType]` attributes.
- `src/backend/Host/ERP-SYSTEM.csproj` — `Swashbuckle.AspNetCore` package reference.
- `src/backend/Modules/Companies/Application/DTOs/CompanyDto.cs` (NEW) — explicit C# DTO with XML doc comments.
- `src/backend/Modules/Finance/Application/FinanceDtos.cs` — XML doc comments + typed result wrappers.

### Notes (BE Jimi 2 — T2)
- No new dependencies beyond `Swashbuckle.AspNetCore`.
- Contract is hand-maintained (not auto-generated). Future: NSwag for full automation.
- `api-types.ts` and the C# DTOs must be kept in sync manually until NSwag is wired.

### Changed (BE Jimi 1 — T1, docs alignment)
- `src/backend/Modules/Companies/AGENTS.md` — Schema section rewritten to match actual code (`parent_company_id`, `is_group`, `slug`, `base_currency`). Removed incorrect `holding_id` reference.
- `docs/architecture/holding-company-architecture.md` — Sections 5+7 (and Section 5 consolidated-report SQL example) updated to reflect the single-table self-referencing model. Added "Known discrepancy" note pointing to the Sprint 8 T4 refactor proposal. ERD replaced; `holdings` reference removed from the 34-table category list.

### Notes (BE Jimi 1 — T1)
- NO code changes (Phase 1 is docs-only).

### Added (BE Jimi 2 — T2, BE-FE contracts)
- `src/backend/Host/ERP-SYSTEM.csproj` — `<GenerateDocumentationFile>true</GenerateDocumentationFile>` so the build emits `ERPSystem.Host.xml` for Swashbuckle to consume. CS1591 stays suppressed (existing project policy).
- `src/backend/Host/Program.cs` — `c.IncludeXmlComments(...)` in the `AddSwaggerGen` block so controller `<summary>` and DTO property docs surface in `/swagger`. Security definitions (Bearer/JWT) were already in place; no behavior change.
- `src/backend/Modules/Companies/Application/DTOs/CompanyDto.cs` — NEW. Public-facing DTOs (`CompanyDto`, `CompanyPageDto`, `CompanyTreeNodeDto`, `HoldingDetailDto`, `HoldingCompanySummaryDto`, `CreateCompanyRequestDto`, `CreateHoldingRequestDto`, `AddSubsidiaryRequestDto`) with full XML doc comments. Mirrors the existing `Company` entity in a new `ERPSystem.Modules.Companies.Application.Dtos` namespace so the FE has a stable, single import path; the existing controller is unchanged (additive only).
- `src/frontend/lib/api-types.ts` — NEW (357 lines). Hand-written TypeScript mirror of the BE DTOs (`CompanyDto`, `AccountResponse`, `JournalEntryResponse`, `UserInfo`, `AuthResponse`, `UserWithRoles`, plus shared enums + display maps). Convention: `Guid → string`, `DateTime → string` (ISO 8601), nullable → `T | null`. Acts as the canonical FE contract until NSwag codegen is wired in a future sprint.
- XML doc comments added to `src/backend/Modules/Finance/Application/FinanceDtos.cs` (every public DTO: `CreateAccountRequest`, `AccountResponse`, `PostJournalEntryRequest`, `PostJournalLineRequest`, `JournalEntryResponse`, `JournalLineResponse`, `LedgerLineResponse`, `AccountBalanceResponse`, `CreatePostingRuleRequest`).

### Changed (BE Jimi 2 — T2, OpenAPI annotations)
- `src/backend/Host/Controllers/CompaniesController.cs` — Added `[ProducesResponseType]` for every endpoint (200/201/204/400/401/403/404 as applicable) and `[Produces("application/json")]` at the controller level. Existing routes, parameters, and return shapes unchanged (additive only).
- `src/backend/Host/Controllers/AccountsController.cs` — Same treatment: 5 endpoints annotated; the 2 existing `[ProducesResponseType]` (List 200 + Create 201) extended to 400/401/403 and the 3 missing endpoints (GetById 200/404, GetByCode 200/404, Delete 204/400) get the matching pair.
- `src/backend/Host/Controllers/UsersController.cs` — Same treatment: 4 endpoints annotated (List, GetById, ListRoles, GetCompanies) + 2 new typed response shapes (`UsersListResponse`, `UserCompaniesResponse`) replacing the previous `Ok(new { items, total, skip, take })` anonymous objects so the FE can import the shape.

### Notes (BE Jimi 2 — T2)
- `dotnet build` → 0 errors. 22 pre-existing CS1570/CS1573/CS1574/CS8629 warnings (Arabic text in comments, broken `cref`s in Inventory/Payments/Payroll modules) — none introduced by this Jimi. ERPSystem.Host.xml generated at 190 KB.
- `dotnet test` → 433 pass, 2 pre-existing failures (`RetentionTests.ArchiveMetadata_InsertAndQuery` + `PartitionedAuditLog_AcceptsInserts`, both `28P01 password authentication failed for user "postgres"` against the `erp_test_system` test DB). CHANGELOG already calls these out as a pre-existing environmental issue, not a regression. **No new tests added by this Jimi** (T2 is contract-annotation work; 1-test-per-endpoint was the BE feature work in prior sprints).
- `npm run type-check` → 0 errors. The new `lib/api-types.ts` is additive — `lib/api.ts` still carries the (slightly enriched) legacy types for backwards compatibility; a future sprint can rebase `api.ts` onto `api-types.ts`.
- Scope honored: NO `tenant_id` introduced (Article 3). NO `Companies/AGENTS.md` or architecture docs touched (Jimi 1's scope). NO new dependencies (no NSwag, no codegen tool). NO breaking changes to existing controllers — all annotations are additive.
- Files touched: 6 (1 csproj, 1 Program.cs, 3 controllers, 1 Finance DTOs file modified, 1 Companies DTOs file created, 1 api-types.ts created) + this CHANGELOG entry.
- The refactor proposal file `docs/architecture/holding-company-refactor-proposal.md` was referenced in the new docs but does not yet exist in the repo — out-of-scope follow-up for the Admin Team.
- Section 6 of the architecture doc still contains the legacy `companies` SQL with `holding_id` (FK → `holdings`); same fix pattern as T1 should be applied in a follow-up.

---

## Sprint 8 T2 — FakeDb AS Alias Enhancement (2026-07-31) ✅ DONE

**Goal:** Remove known technical debt in `FakeDbConnectionFactory` that forces tests to use projected column names as a workaround for SQL `AS` aliases. Per T2 hand-off (Admin Team v1.8, محمد mode, approved by Anas 04:08 UTC).

### Added
- **`FakeDbDataReader.ProjectColumns(string sql, DataSet ds, string tableName)`** — internal static helper in `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs`. Parses the SELECT clause and projects the underlying DataTable's columns to the alias names.
- **`SplitColumns(string columnList)`** — depth/quote-aware state machine for splitting the SELECT column list.
- **`Unquote(string s)`** — strips surrounding double-quotes from SQL identifiers.
- **`StripTableAlias(string s)`** — strips `a.id` → `id`, etc.
- **`FindSourceOrdinal(DataTable source, string columnName)`** — case-insensitive source column lookup.
- **`src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactoryTests.cs`** — 3 new tests (`AsAlias_RenamesColumnsInReader`, `NoAsAlias_FallsBackToDirectColumns`, `AsAlias_HandlesMultipleColumnsIncludingExpression`).
- **Modified `FakeDbDataReader` constructor** — tries `ProjectColumns` first, falls back to direct table.
- **`src/backend/Modules/Finance/AGENTS.md`** — "Test Pattern: SQL AS Alias Support" section.

### Verified
- `dotnet build`: 0 errors
- `dotnet test --filter "FakeDbConnectionFactoryTests"`: 3/3 pass
- `dotnet test` (full suite): **436 passed, 2 failed, 30 skipped** (2 pre-existing RetentionTests DB issues)
- No `tenant_id` introduced

### Notes
- T2 = Option B (per محمد's recommendation, approved by Anas 04:08 UTC)
- Mavis Local takeover (per the v2.0 governance model; the Coordinator can move to Local role)
- Branch: `feature/sprint-8-t2-fakedb-as-alias` (off `origin/develop @ 5e2cbd0`)
- Removes known technical debt (T1 tests needed projected column names workaround)
- Existing tests unaffected (additive change with backward-compat fallback)
- Sprint 9+ tests can use real AS aliases naturally
- The "alias-first, then source name" lookup order in `FindSourceOrdinal` is the key insight that makes the new code work with both the old "projected column names" pattern and the new "real SQL" pattern.

---

## Sprint 6 — Post-Demo Hardening (2026-07-29) 🟡 IN PROGRESS

**Goal:** Constitutional cleanup ✅ done in T1. Now polishing docs and verifying (T5+T6).

### Added
- `docs/workflow/sprint-6.md` — Sprint 6 hand-off (self-planned, ball in mavis-local court)
- Updated `docs/workflow/demo-roadmap.md` — actual completion status of Sprints 0-5
- Updated `docs/AGENTS.md` — references now point to `WORKFLOW.md` (active) + `CONSTITUTION.md` (paused) + root `CHANGELOG.md` (current)

### Notes
- T1 (Constitutional Setup) ✅ MERGED at PR #173 `c5a37119`
- T2 (Stale-branch cleanup) ✅ partial (4 local + 2 remote branches deleted)
- T3-T4 (Test gap-fill, FE polish) ⏳ optional, deferred
- T5 (Doc polish) 🟡 in progress
- T6 (Verify) ⏳ next
- T7 (Open PR + self-merge) ⏳ pending

---

## Sprint 6 Prep — Constitutional Cleanup (2026-07-29) ✅ MERGED (self-merge per DEC-070)

**Goal:** Promote the active constitution to the project root, activate the worker contract for Jimis, and launch Sprint 6.

### Added
- **`WORKFLOW.md` (project root)** — the active workflow constitution (8 articles), promoted from `.github/workflows/mavis-coordination/constitution.md` per Anas's 2026-07-29 19:13 UTC mandate ("always in mind"). Points to `docs/workflow/sprint-N.md` for hand-offs and `.github/workflows/mavis-coordination/state.json` for state.
- **`.mavis/AGENTS.md`** — worker contract for every local Jimi spawned by Mavis Local. Covers pre-flight, scope declaration, CHANGELOG entry, code standards, DOX pass, self-verify, what NOT to do, escalation rules.
- **`docs/workflow/sprint-6.md`** — Sprint 6 hand-off (self-planned by Mavis Local, since ball is in mavis-local court for the 2-day window).

### Changed
- **Root `AGENTS.md`** — replaced the "PROJECT PAUSED" banner with the "ACTIVE GOVERNANCE" banner pointing to `WORKFLOW.md`. Updated the child DOX index (`.mavis/AGENTS.md` is now Active, not "TO CREATE"). Refreshed the Sprint Model section to reference the worker contract. Updated the Crons section to reflect "tool, not actor" framing.
- **Active constitution location** — `WORKFLOW.md` at root is now the canonical source. The original file at `.github/workflows/mavis-coordination/constitution.md` is kept (cron path is fixed) but is no longer the primary reference.

### Notes
- Per Anas's 2026-07-29 19:13 UTC directive: "الكوره في ملعب الفريق المحلي" (the ball is in the local team's court) — Sprint 6 is now active.
- Per the active constitution (Article 2): the ball is in the **ACTOR's** court (mavis-local / mavis-cloud / anas), **NOT** the cron's. The cron is a tool.
- All async coordination still flows through `.github/workflows/mavis-coordination/state.json`.

---

## Sprint 5 — Demo V2 (The "Wow" Version) — Backend Phase 4 + 5 (2026-07-29) ✅ MERGED (PR #172)

**PR #172** (squash `9d148f4`) merged at 2026-07-29 18:51 UTC. Self-merge per DEC-070 (admin bypass).

**Goal:** Polished demo V2 with dashboard charts + global search.

### Added — Dashboard chart data (Phase 4 — T1/T2/T3)
- `GET /api/dashboard/charts/revenue?months=6` — revenue vs expense per month (line chart). Filters: `company_id`, status IN (Posted/Partial/Paid), expense from accounts.type=5 with status=2 journal entries.
- `GET /api/dashboard/charts/expenses-by-category?months=3` — pie / donut chart. One slice per Expense-type account with a fixed palette color by rank.
- `GET /api/dashboard/charts/top-customers?limit=5` — top customers by posted invoice total, all-time within the current company.
- New service: `Modules/Dashboard/Application/Services/DashboardChartService.cs`
- New DTOs: `Modules/Dashboard/Application/DTOs/ChartDtos.cs`
- New tests: `Tests/ERPSystem.Tests/Dashboard/DashboardChartTests.cs` (7 tests, 1 skipped integration)

### Added — Global search (Phase 5 — T4)
- `GET /api/search?q=&limit=20` — case-insensitive LIKE across customers, vendors, sales_invoices, and accounts. 3-tier ranking (exact > prefix > contains, scores 1.0/0.7/0.4). Per-type cap 5, total cap 20 (max 50). Always company-scoped.
- New module: `Modules/Search/` (Endpoints, Application/Services, Application/DTOs, AGENTS.md)
- New service: `Modules/Search/Application/Services/GlobalSearchService.cs`
- New DTOs: `Modules/Search/Application/DTOs/SearchDtos.cs`
- New tests: `Tests/ERPSystem.Tests/Search/GlobalSearchServiceTests.cs` (4 tests, 1 skipped integration)

### Changed
- `Host/Program.cs` — registered `IDashboardChartService` and `IGlobalSearchService` in DI.
- `Modules/Dashboard/Endpoints/GetSummary.cs` — added 3 chart endpoint methods to the existing `DashboardController` (route `/api/dashboard`).

### Notes
- All 4 new endpoints filter on `company_id` (Constitution Article 3, no `tenant_id`).
- Dapper only, no EF Core.
- `[Authorize(Policy = ReadAccess)]` on every new endpoint.
- 1 test per endpoint (per Article 11), 11 new tests total (2 skipped integration).
- The 2 failing `RetentionTests` are pre-existing — verified by `git stash` on bare develop (auth failure to `erp_test_system` test DB, not present in local Docker). Not a regression.

---

## Local Docker Demo — Setup (2026-07-29) ✅ MERGED

**Goal:** Self-contained local Docker stack for client demo on Anas's machine.

### ⏸️ PROJECT PAUSED (2026-07-29 18:25 UTC — 2 days)
**Per Anas's directive** to speed up work and coordination in a single environment:
- **Active (temporary permanent) constitution:** `.github/workflows/mavis-coordination/constitution.md` (was just created by سيتی + محمد)
- **Paused constitution:** `CONSTITUTION.md` (marked PAUSED, restored 2026-07-31 18:25 UTC)
- **Admin Team = سيتی + محمد + ديف** (Cloud) work as "Cron Jobs" coordinated by Mavis Local via `state.json`
- **Mavis Local = sole Tech Lead + Coordinator** for the 2-day window
- **No Telegram ping-pong** — all async via state.json
- **State.json is the single ping-pong point** — read it to know where the ball is
- **Pause until:** 2026-07-31 18:25 UTC
- **Reference:** [Anas's directive in this conversation](state.json)

### PR #172 — Local dev speed boost (in progress, NOT a code PR — gitignored config)
**Per Anas (2026-07-29):** Use local DB engine for faster dev. Switched `appsettings.Development.json` (gitignored) from Supabase to `localhost:5432` (local Docker Postgres).

**Impact:**
- Login: 30-60s (Supabase pooler) → **<1s (local)**
- DB queries: ~100ms → <5ms
- Works offline (no internet needed)
- Schema/data identical to cloud (same migrations + seed)

**Constraint noted:** The sprint-5 hand-off said "PostgreSQL 17 (Supabase for dev, Docker for local) | No new DB engine". This change is consistent — still PostgreSQL, just local instead of cloud. Engine unchanged. Hand-off constraint remains for non-Mavis-Local devs.

### PR #170 — Local Docker config fix (MERGED at `c57a25d`)
- Fixed 5 docker-compose.yml bugs that blocked any local Docker usage:
  1. Frontend volume mount: `./src/frontend` → `../src/frontend`
  2. API build context: `.` → `../src/backend`
  3. Added `ConnectionStrings__Migrations` + `Marten__ConnectionString` env vars
  4. Added `Database__JsonMigrationEnabled: "true"`
  5. Documented wget-not-in-image issue (deferred to PR #171)
- Added: `docs/workflow/local-docker-fixes-report.md` (full technical report)

### PR #171 — Local Docker P1 fixes + architecture (in progress)
**Branch:** `fix/local-docker-p1-architecture` (off `c57a25d`)

#### Fixed
- **P1 seed issues:**
  - **Issue A (cancelled):** `users` schema was already correct (no `is_email_verified`)
  - **Issue B:** Added `SECTION 7.5: Roles` before `SECTION 8: user_roles` (4 canonical roles: Admin, Accountant, ProjectManager, Viewer)
  - **Issue C:** Activity log now uses `array_agg(id) FROM users WHERE is_active` — no more hardcoded UUID collision with ALF-CONST company
  - **Issue D:** Admin user inserted explicitly with system UUID `00000000-...-0002` + user_companies (4 companies) + user_roles (Admin)
- **P2 docker:** Removed `wget` healthcheck (not in ASP.NET image)

#### Added (architecture improvements)
- `docs/workflow/local-docker.md` — Architecture doc (when to use, how it works)
- Improved `local-docker/README.md` (curl healthcheck, troubleshooting)
- Updated `AGENTS.md` (cross-link to local-docker)

#### Changed
- `v_admin_id` in seed now uses `00000000-...-0002` (was wrongly pointing to `11111111-...` = ALF-CONST company)
- Activity log loop uses dynamic `array_length(v_user_ids, 1)` (no magic number 10)

#### Verified
- `docker compose up -d --build` → all 3 containers running
- `psql -f docs/seed-sprint4-demo-data.sql` → no manual workarounds needed
- `POST /api/auth/login admin@alfajr.local / Demo1234` → 200 + JWT
- All 10 users can log in
- Browser: http://localhost:3000 → working

---

## Sprint 4 (in progress) — Polish + Demo Data (2026-07-29)

### Added
- `docs/architecture/holding-company-architecture.md` — Full architecture documentation
- `docs/seed-sprint4-demo-data.sql` — Demo data seed (3 companies, 10 users, 100+ transactions)
- `src/backend/Tests/ERPSystem.Tests/Seed/Sprint4SeedTests.cs` — 19 static tests (no DB) for the seed file
- `docs/workflow/sprint-4.md` — Sprint 4 hand-off documentation
- Child DOX entry: `src/backend/Tests/ERPSystem.Tests/Seed/` added to `Tests/AGENTS.md` index

### Changed
- **CLEANUP AMENDMENT 2026-07-29 (per Anas):**
  - 9 stale feature branches deleted (`feature/dec-088-*`, `feature/local-docker-setup*`, `feature/phase5b-*`, `feature/phase-5-ar`, `feature/sprint-2-companies-users`, `governance/setup-cycle-1`, `hotfix/v1.0.34-data-and-reports`)
  - 124 documentation files removed (old DECs, hand-offs, E2E reports, phase plans, seed SQLs, governance dumps)
  - CONSTITUTION.md restructured: 10 articles → 15 articles (added Articles 9-15: cleanup, local team, tests, presence, Mephisto, amendment, communication)
  - AGENTS.md simplified (root + docs/)
  - 4 branches remain: `main`, `develop`, `feature/abdo-team`, `feature/sprint-4-polish-demo-data`

### Fixed
- Hallucination reset: removed all `tenant_id`/`TenantContext` references (0 found in repo)
- Branch clutter: 13 → 4 branches

---

## Sprint 3 (2026-07-28) — Activity + Notifications ✅ MERGED

- PR #167: Activity feed + notification bell
- 8 files, +775/-0
- `GET /api/activity/recent?limit=20`
- `/activity` page + bell icon + `/notifications` page
- 53 min execution (well under 1.5h estimate)

---

## Sprint 2 (2026-07-28) — Companies + Users ✅ MERGED

- PR #166: Companies + Users admin
- 15 files, +2533/-340
- 5 backend endpoints + 4 frontend pages
- 2 unit tests added
- 58 min execution

---

## Sprint 1 (2026-07-28) — Dashboard + Holding ✅ MERGED

- PR #165: Dashboard + Holding
- 14 files, +1054/-270
- Holding dashboard with consolidated metrics
- 2h execution (within estimate)

---

## Phase 6 (2026-07-27) — Multi-Company Refactor ✅ MERGED

- PRs #139-#151: Phase 6.0-6.3 complete
- **Constitution Article 3 enforced:** `tenant_id` → `company_id`
- 13 backend modules restructured
- 34 tables migrated to Supabase

---

## Earlier (Pre-Constitution Era) — 2026-07-25 and before

- See git history for pre-2026-07-27 changes
- Phase 1-5 (initial build)
- DEC-002 through DEC-069 (decisions, now merged into CONSTITUTION)

---

_Last updated: 2026-07-29 by Mavis Local, approved by Anas_
