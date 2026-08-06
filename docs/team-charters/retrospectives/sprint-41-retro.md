# Sprint 41 Retrospective — P0 Fixes (Phase 0 of 3-Version UI Exploration)

**Branch:** `feature/sprint-41-p0-fixes`
**Commit:** `24157f6` (on top of `1fc2a86`)
**Date:** 2026-08-06
**Duration:** ~1.5h
**Status:** DONE (local-only, awaiting "ادفع")

## Goal

Fix 2 P0 bugs Anas hit while browsing the live system, before starting the
3-Version UI Exploration (Sprints 42-50).

## What Shipped

### Fix #1 (DEC-127) — FK violation on sub-account creation
- **File:** `src/backend/Modules/Finance/Application/Services/ChartOfAccountsService.cs`
- **Issue:** `CreateAsync` did not inject `ICompanyContext`; `Account.CompanyId`
  defaulted to `Guid.Empty` → Postgres FK violation `fk_accounts_company_id`.
- **Fix:** Inject `ICompanyContext`. Resolve `companyId` at the top of
  `CreateAsync`. Refuse the call with `FinanceErrorCode.ValidationError` if
  `!_companyContext.IsResolved` (defense-in-depth, though the controller
  already filters to authenticated users). Set `acc.CompanyId = companyId`
  in the entity. Validate that the parent account (if any) also belongs to
  the same company (`TenantMismatch` otherwise).

### Fix #2 (DEC-127) — `IAccountRepository.GetByCodeAsync` company-scoped overload
- **File:** `src/backend/Modules/Finance/Infrastructure/IRepositories.cs`,
  `src/backend/Modules/Finance/Infrastructure/AccountRepository.cs`
- **Issue:** Uniqueness of account code is per-company, not global. The legacy
  single-arg overload `GetByCodeAsync(string code)` returned the first match
  across all companies.
- **Fix:** Added overload `GetByCodeAsync(string code, Guid companyId, ct)`.
  Old overload kept for 4 other callers (PaymentService, PostingRulesService,
  PayrollService, ReceiptService) — out of scope for this sprint, will be
  addressed in the L19 sweep for those services.

### Fix #3 (DEC-128) — Friendly FK / NOT NULL error message
- **File:** `src/backend/Host/Controllers/AccountsController.cs`
- **Issue:** Raw `insert or update on table "accounts" violates foreign key
  constraint "fk_accounts_company_id"...` was leaking to the user.
- **Fix:** Wrapped `_legacy.CreateAsync` call in try/catch on
  `PostgresException` with `SqlState == "23503" || "23502"`. Map to a
  `ProblemDetails` with friendly Arabic text:
  > "لا يمكن إنشاء الحساب — تحقق من اختيار الشركة النشطة ومن صحة بيانات الحساب الأب."

  The real exception is still logged via `ILogger<AccountsController>` for ops.

### Fix #4 (L76) — Tree view icons actually expand/collapse
- **File:** `src/frontend/app/(authenticated)/finance/accounts/page.tsx:121`
- **Issue:** `flattenTree(roots)` walked all children regardless of the
  `expanded` Set state — the `toggleNode` was wired up but the rendered
  `flatRows` ignored it.
- **Fix:** `flattenTree(roots, expanded: Set<string>)` — only walks a
  parent's children if that parent is in the `expanded` Set. Root nodes are
  always included.

## Verification

- `dotnet build Host/ERP-SYSTEM.csproj` — 0 errors, 0 warnings
- `npm run build` — 50+ pages compiled, 0 errors
- `node scripts/verify-sprint41-p0.mjs`:
  - Initial CoA rows: 19 (was 52 before — tree now properly filters)
  - Click collapse on "1000 الأصول" → "1100 أصول غير متداولة" hidden ✓
  - Click again → "1100" visible again ✓
  - Add-child modal: opens, submits, no error ✓
  - 0 console errors
- BE smoke: `POST /api/finance/accounts` with `code=9999, parentAccountId=1200`
  → 201 Created, no FK error.

## What Worked

- **The sprint plan document** (`docs/plans/sprint-41-ui-exploration-3-versions.md`)
  written BEFORE any code made the work much more focused. Anas had a clear
  picture of where the P0 fixes fit in the bigger picture.
- **Pattern from Sprint 28 (DEC-095)**: `ProjectService.CreateAsync` already
  uses `_companyContext.CompanyId` for both the project + auto-created budget.
  Applied the same pattern to `ChartOfAccountsService.CreateAsync`.
- **L43 (kill dotnet before build)**: Hit the locked .exe error immediately
  on first build. Stopped process, retried, succeeded.
- **L45 (npm run build after FE changes)**: First Playwright run reported
  "BUG NOT FIXED" — realized the FE was serving the cached `.next/`. Rebuilt
  and re-tested, all green.

## Lessons

- **L77 (NEW)**: When the user reports a UI bug, **always check whether the
  bug is in state management vs. rendering**. The CoA "icons don't work" bug
  was actually the rendering ignoring the state — `flattenTree` didn't take
  the `expanded` Set. The state was correct; the render was wrong.
- **L78 (NEW)**: Sub-account creates had an FK violation that was actually a
  silent CompanyId=Guid.Empty issue. The DTO didn't carry companyId, the
  service didn't inject ICompanyContext, the controller didn't add an
  X-Company-Id path, and the entity default was Guid.Empty. **4 layers of
  indirection, 1 missing field** = silent cross-tenant data corruption. The
  DEC-085 audit pattern is the only thing that catches this.
- **L79 (NEW)**: When migrating a service to inject ICompanyContext, the
  defensive `!_companyContext.IsResolved` check at the top of the method is
  worth adding — it surfaces "authenticated but no company" requests as
  400 with a friendly message instead of a 500 from a null-deref or
  Guid.Empty FK violation. The controller layer already prevents this state,
  but the check makes the service safer to reuse outside controllers
  (e.g., from a hosted seeder or background job).

## Carry-over (Out of Scope for This Sprint)

- L19 audit on `ChartOfAccountsService.GetByCodeAsync` (the 4 other
  callers: PaymentService, PostingRulesService, PayrollService,
  ReceiptService) — they still use the legacy `GetByCodeAsync(string code)`
  without companyId.
- `ChartOfAccountsService.GetByIdAsync` and `ListAsync` should also be
  company-scoped (currently they return data from all companies).
- Global `PostgresException` middleware (defense-in-depth for similar
  bugs in other modules). Per-controller try/catch is sufficient for now.

## Next Steps (Per the Sprint 41+ Plan)

- **Sprint 42**: Version A scaffold (`http://localhost:3001`)
- **Sprint 43-47**: 3 Versions A/B/C built in parallel
- **Sprint 48**: User picks one
- **Sprint 49-50**: Refine + prepare for Mode 2

The plan is in `docs/plans/sprint-41-ui-exploration-3-versions.md`.

## Telegram Ping

Sent at end of sprint. See logs.
