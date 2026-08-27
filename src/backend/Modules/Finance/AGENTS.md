# 💰 AGENTS.md — src/backend/Modules/Finance/

> **Finance module** (Accounts, Transactions, CoA). Read all parent AGENTS.md files first.

**Last updated:** 2026-08-27 (Sprint 65 Wave 3A — DEC-235 + DEC-237)

---

## Purpose

Chart of Accounts (CoA), journal entries, transactions, bank accounts. The core financial engine.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي |
| **Schema review** | Anas |

## Local Contracts

### Schema
- `accounts` — `id`, `company_id`, `code`, `name`, `type` (Asset/Liability/Equity/Revenue/Expense), `parent_account_id`, `is_postable`, `is_intercompany`.
- `transactions` — `id`, `company_id`, `account_id`, `debit`, `credit`, `description`, `created_at`.
- `bank_accounts` — `id`, `company_id`, `bank_name`, `account_number`, `balance`.
- **All rows MUST have `company_id`** (per Constitution Article 3).

### Double-Entry Rule
- Every transaction has `debit = credit` (sum of debits = sum of credits).
- Enforced in `FinanceService.PostJournalEntryAsync()`.

## Work Guidance

### Adding a New Account Type
1. Update `AccountType` enum in `Domain/Entities/Account.cs`.
2. Add migration if needed.
3. Update CoA seed in `Shared/SeedData/DefaultCoASeed.cs`.
4. Add validation in `Application/Services/FinanceService.cs`.

## Verification

- [ ] `dotnet test --filter "Finance"` — all green.
- [ ] No `tenant_id`.
- [ ] All accounts have `company_id`.
- [ ] Double-entry enforced.

---

## 🧪 Test Pattern: SQL AS Alias Support (added 2026-07-31, Sprint 8 T2)

When writing tests that use `FakeDbConnectionFactory`, you can now use **real SQL with `AS` aliases** instead of the old "projected column names" workaround.

### Before T2 (workaround)

```csharp
// Test code: column names in AddRow must match SELECT column names
factory.AddRow("accounts",
    "AccountId", Guid.NewGuid(),   // <-- alias as base column name
    "AccountCode", "1000",        // <-- alias as base column name
    "AccountName", "Cash");
"SELECT AccountId, AccountCode, AccountName FROM accounts"  // <-- no AS
```

This is fragile: the DataTable column types are inferred from the AddRow values, and the SQL doesn't match production SQL (which uses `id AS "AccountId"`).

### After T2 (real SQL)

```csharp
// SQL (production-style):
"SELECT id AS \"AccountId\", code AS \"AccountCode\", name AS \"AccountName\" FROM accounts"

// AddRow uses BASE column names (the underlying DataTable schema):
factory.AddRow("accounts",
    "id", Guid.NewGuid(),
    "code", "1000",
    "name", "Cash");
```

The `FakeDbDataReader` parses the SELECT clause and projects the underlying DataTable's columns to the alias names. The reader's `GetName(i)` returns the alias, but the values are pulled from the source column with the matching base name. This aligns test SQL with production SQL.

### Edge cases supported

- **Mixed aliased + non-aliased columns** — `SELECT id, code AS "AccountCode", name FROM accounts` works.
- **Quoted identifiers** — `AS "AccountId"` is unquoted to `AccountId`.
- **Expression aliases** — `(code || '-' || name) AS "DisplayName"` creates the column with the alias name, but the value is `DBNull` (FakeDb does not simulate the SQL expression).
- **Multiple aliases per SELECT** — any number of aliased columns is fine.
- **Aggregate aliases** — `COUNT(*) AS total` parses correctly, but the value is `object` (use `ExecuteScalar` for real COUNT semantics; this change only affects the reader).
- **No `AS`** — falls back to the direct DataTable columns. Existing tests using the projected-name convention continue to work.

### Implementation

- `ProjectColumns(string sql, DataSet ds, string tableName)` — internal static helper in `FakeDbConnectionFactory.cs`
- `SplitColumns(string columnList)` — depth/quote-aware state machine for splitting the SELECT column list
- `Unquote(string s)` — strips surrounding double-quotes
- Modified `FakeDbDataReader` constructor to try projection first, fall back to direct table

### Tests

`src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactoryTests.cs` — 3 tests:
- `AsAlias_RenamesColumnsInReader` — happy path
- `NoAsAlias_FallsBackToDirectColumns` — backward compatibility
- `AsAlias_HandlesMultipleColumnsIncludingExpression` — expression alias edge case

## Jimi Scope — 2026-07-31 (Sprint 11 T2)

**Jimi type:** BE
**Sprint / Cycle:** Sprint 11 (Full Demo Coverage)
**T# tasks:** T2 (BE endpoints matching FE contract)
**Branch:** `feature/sprint-11-fe-be-parallel` (off `origin/develop @ 64efaac`)

**Files I added/touched in this module:**
- `src/backend/Modules/Finance/Application/FinanceDtos.cs` (MODIFIED) — added `HoldingDashboardDto`, `AccountDto` (string enums), `TransactionDto`
- `src/backend/Modules/Finance/Application/Services/FinanceService.cs` (NEW) — `IFinanceService` with `GetConsolidatedKpisAsync` / `GetRecentTransactionsAsync` / `ListAccountsAsync` / `GetAccountByIdAsync`

**Constitution articles I respected:**
- Article 3 — `company_id` only (no `tenant_id`)
- Article 6 (Dapper only, no EF Core)
- Article 10 (LOCAL-ONLY commit, no push/PR per Anas mandate)

---

## Sprint 60 Wave 1 — DB Foundation (2026-08-25) ✅ DONE (LOCAL-ONLY)

**Goal:** per Anas's CoA-Final-Proposal-2026-08-24, lay the DB foundation (schema + master data) for the upcoming canonical-4-level CoA migration. **No code reads the new columns yet** — Wave 2 will write the migration job that consumes them.

### DEC-184 — 6 new columns on `accounts`

Added 6 Financial-Statement metadata columns to the `accounts` table:

| Column | Type | Default | Purpose |
|---|---|---|---|
| `fs_type` | TEXT | NULL | 'BS' (Balance Sheet) or 'PL' (Profit & Loss) |
| `section` | TEXT | NULL | e.g. 'Current Asset', 'COGS', 'Tax' |
| `is_canonical` | BOOLEAN | TRUE | FALSE for legacy rows; TRUE for new canonical-coded rows |
| `new_code` | TEXT | NULL | the canonical 4-level code (e.g. '1.1.01.002') |
| `migration_status` | TEXT | 'pending' | 'pending' \| 'migrated' \| 'new' \| 'deprecated' |
| `migrated_at` | TIMESTAMPTZ | NULL | when the account was migrated to canonical |

- **Migration:** `src/backend/Shared/Migrations/Sprint60_AddAccountFsMetadata_20260825_001.cs`
- **Idempotency:** every `ALTER TABLE ... ADD COLUMN` uses `IF NOT EXISTS`; the backfill UPDATE is a no-op on already-flagged rows.
- **Existing rows:** backfilled to `is_canonical = FALSE`, `migration_status = 'pending'`. All other new columns = NULL.
- **Down():** drops the 6 columns in reverse order (each `DROP COLUMN IF EXISTS`).

### DEC-NEW-14 — 4 foundation cost centers (idempotent seed)

Added 4 cost centers for the default holding company (`companies.code = '000'`):

| Code | Name (AR) | Division |
|---|---|---|
| `CC-CONSTR` | قسم المقاولات | Construction |
| `CC-REST`  | قسم المطاعم  | Restaurant / Catering |
| `CC-ADMIN` | الإدارة     | Admin / Shared |
| `CC-WORKSHOP` | الورشة    | Workshop |

- **Migration:** `src/backend/Shared/Migrations/Sprint60_FoundationDataSeed_20260825_002.cs` (shared with DEC-NEW-15)
- **Idempotency:** every INSERT uses `ON CONFLICT (company_id, code) DO NOTHING`.
- **Note:** `cost_centers` table already existed via `data-types/cost_centers.json` (L147 — auto-migrated by DataTypeMigrator). No schema change required.
- **Coexistence:** the 6 Sprint 58c cost centers (`CC-001/002/003/101/102/103`) remain untouched.

### DEC-NEW-15 — 5 new foundation projects (idempotent seed)

Added 5 new projects for the default holding company (3 Sprint 58c `PRJ-2026-*` projects stay → total = 3 + 5 = **8 projects**):

| Code | Name (AR) | Cost Center | Status | Start | End |
|---|---|---|---|---|---|
| `REST-2026-001` | مطعم الأسماك - عقد NDB | CC-REST | Active (2) | 2026-09-01 | 2026-12-31 |
| `REST-2026-002` | خدمات الإعاشة - عقد catering | CC-REST | Planning (1) | 2026-09-15 | 2027-03-31 |
| `ADMN-2026-001` | ترقية نظام ERP - مشروع داخلي | CC-ADMIN | Active (2) | 2026-09-01 | 2026-11-30 |
| `TRNG-2026-001` | تدريب الموظفين - برنامج Q4 | CC-ADMIN | Planning (1) | 2026-10-01 | 2026-12-15 |
| `YRCL-2026-001` | إقفال السنة المالية 2026 | CC-ADMIN | Planning (1) | 2026-12-01 | 2026-12-31 |

- **Migration:** same as DEC-NEW-14 (`Sprint60_FoundationDataSeed_20260825_002.cs`)
- **Idempotency:** every INSERT uses `ON CONFLICT (company_id, code) DO NOTHING`.
- **FK resolution:** `cost_center_id` is looked up by `(company_id, code)` JOIN — no hardcoded UUIDs. This means the seed is portable across fresh DBs.
- **`created_by`:** resolved as the first active user; falls back to the deterministic `00000000-0000-0000-0000-000000000002` placeholder if no user exists yet.
- **Down():** DELETEs only the 5 new project codes. The 3 Sprint 58c projects are explicitly preserved.

### Tests added (23 total, 1 per deliverable × multiple assertions)

- `src/backend/Tests/ERPSystem.Tests/Finance/Sprint60AccountMetadataMigrationTests.cs` — 7 tests for DEC-184
  - Migration class + `[Migration(20260825_001)]` attribute
  - Up() + Down() methods exist
  - File references all 6 new columns
  - Idempotency via `IF NOT EXISTS`
  - Backfill defaults (`is_canonical = FALSE`, `migration_status = 'pending'`)
  - Down() drops all 6 columns
  - No `tenant_id` reference
- `src/backend/Tests/ERPSystem.Tests/Companies/Sprint60FoundationDataMigrationTests.cs` — 8 tests for DEC-NEW-14
  - Migration class + `[Migration(20260825_002)]` attribute
  - Up() + Down() methods exist
  - 4 cost center codes are seeded (CC-CONSTR/REST/ADMIN/WORKSHOP)
  - Arabic names are present
  - Idempotency via `ON CONFLICT (company_id, code) DO NOTHING` (4 INSERTs matched)
  - Down() removes the 4 codes
  - No `tenant_id` reference
  - References default holding by constitutional code '000'
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint60FoundationProjectsMigrationTests.cs` — 8 tests for DEC-NEW-15
  - 5 project codes are seeded (REST-2026-001/002, ADMN/TRNG/YRCL-2026-001)
  - Arabic names are present
  - Idempotency via `ON CONFLICT (company_id, code) DO NOTHING` (5 INSERTs matched)
  - cost_center_id is resolved by `(company_id, code)` JOIN (CC-REST, CC-ADMIN)
  - Status values: 3 Planning (1) + 2 Active (2)
  - Down() removes only the 5 new codes (preserves Sprint 58c `PRJ-2026-*`)
  - No `tenant_id` reference
  - Sanity: 3 + 5 = 8 total projects after migration

### Architectural compliance

- ✅ Constitution Article 3 — `company_id` only, ZERO `tenant_id` references in any new file
- ✅ Idempotent Migrations — `IF NOT EXISTS` (schema) + `ON CONFLICT DO NOTHING` (data)
- ✅ FluentMigrator pattern — matches existing Sprint 24/25/27/28 migrations
- ✅ Dapper / no EF Core
- ✅ Reversible (Down() for both migrations)
- ✅ No secrets in code
- ✅ All cost center + project inserts resolve the holding company by `code = '000'` (constitutional marker), not hardcoded UUIDs
- ✅ No code currently reads the new `accounts` columns — this is **schema-only**; Wave 2 will add the migration job

### Branch

- `feature/sprint-60-wave-1-foundation` (off `origin/develop @ c7ce7be`)
- **LOCAL-ONLY** (Mode 1) — no push, no PR yet (Wave 2 will merge into this branch first)

---

## Sprint 60 Wave 3A — Balance Migration + CoA Validation (2026-08-25) ✅ DONE (LOCAL-ONLY)

**Goal:** per Anas's CoA-Final-Proposal-2026-08-24, after Wave 1+2A+2B laid the foundation and migrated 27 new + 9 renamed accounts, Wave 3A adds the data-integrity checks (DEC-189) and the C# service that exposes them to the FE (DEC-190).

### DEC-189 — Balance migration + validation (read-only + safe UPDATE)

Added a FluentMigrator migration that runs the same data-integrity checks as the Wave 2B migration, but on a *post*-migration DB:

| Check | How |
|---|---|
| **Journal-line integrity** (no orphans) | `LEFT JOIN accounts` on `journal_lines.account_id` + `company_id`; count rows where the account is missing. RAISE NOTICE. |
| **Trial balance per company** | `INNER JOIN journal_entries` filtered to `status=2` (Posted), `SUM(debit)` vs `SUM(credit)` per company, RAISE NOTICE on variance. |
| **(company_id, code) UNIQUE** | `GROUP BY company_id, code HAVING COUNT(*) > 1`, RAISE NOTICE. |
| **Deprecated-with-postings report** | For each `migration_status='deprecated'` account that still has `journal_lines`, print the count. (Not an error — historical postings remain valid for audit.) |
| **Promote 27 'new' → 'migrated'** | `UPDATE accounts SET migration_status='migrated', migrated_at=now() WHERE migration_status='new' AND is_canonical=TRUE`. Idempotent. |
| **Final status tally** | RAISE NOTICE with count per `migration_status` for the holding company. |

- **Migration:** `src/backend/Shared/Migrations/Sprint60_BalanceMigrationValidation_20260825_004.cs`
- **Idempotency:** every check is RAISE NOTICE (no side effects); the promote UPDATE is guarded with `WHERE migration_status='new'` (no-op on re-run).
- **Down():** reverts the promote using `migrated_at >= NOW() - INTERVAL '1 hour'` guard so we only undo *this* migration's work.

### DEC-190 — `CoAValidationService` (C# API for FE / ops dashboard)

New service + types under `src/backend/Modules/Finance/Application/Services/CoAValidationService.cs`:

| Type | Purpose |
|---|---|
| `ICoAValidationService` / `CoAValidationService` | Runs all 5 checks. Constructor takes `IDbConnectionFactory` + `ILogger<>`. |
| `CoAValidationResult` | `{ bool IsValid; List<ValidationIssue> Issues; int ErrorCount; int WarningCount; }`. `IsValid` is `true` iff no error-severity issues. |
| `ValidationIssue` | `{ string Severity; string Code; string Message; Guid? AccountId; string? AccountCode; }`. |
| `ValidationSeverity` | `"Error"` \| `"Warning"` \| `"Info"`. |
| `ValidationCode` | `DUPLICATE_CODE` \| `ORPHAN_JOURNAL_LINE` \| `TRIAL_BALANCE_MISMATCH` \| `INVALID_CODE_FORMAT` \| `LEGACY_ACCOUNT`. |

**Checks (in evaluation order):**
1. **DUPLICATE_CODE** (Error) — `(company_id, code)` UNIQUE violation on `accounts`. Excludes `migration_status='deprecated'`.
2. **ORPHAN_JOURNAL_LINE** (Error) — `journal_lines` with `account_id` not in `accounts` for the same `company_id`. FK should prevent, defensive check.
3. **TRIAL_BALANCE_MISMATCH** (Error) — `Σ debit ≠ Σ credit` on posted lines (status=2) per company.
4. **INVALID_CODE_FORMAT** (Error) — account code matches neither the canonical 4-level dot pattern (`1.1.01`, `1.1.01.001`) nor a recognized legacy 4-digit shape (`1101`, `1101-001`, `71`, `9201`).
5. **LEGACY_ACCOUNT** (Warning) — count of `is_canonical=FALSE + migration_status='pending'` accounts (the 131 keep accounts from Wave 2B that haven't been migrated to canonical code yet).

**Code format regex:**
- Canonical: `^\d+(\.\d+){2,3}$` (3 or 4 dot-separated numeric parts)
- Legacy: `^\d{2,4}(-\d{3})?$` (2-4 digit root, optional `-NNN` suffix)

**Implementation note:** the service pre-fetches all accounts + all (posted) journal_lines for the company in two `QueryAsync` calls, then runs all 5 checks in C# memory. This trades a small bit of efficiency for clean, deterministic testing with `FakeDbConnectionFactory` (which cannot simulate SQL aggregations). Production-scale data is small (< 1000 accounts per Holding), so the in-memory pass is fine.

**DI registration:** `src/backend/Host/Program.cs` adds `builder.Services.AddScoped<ICoAValidationService, CoAValidationService>();`.

### Tests added (15 total)

`src/backend/Tests/ERPSystem.Tests/Finance/Sprint60BalanceMigrationValidationTests.cs` — 15 tests:

**Migration class shape (9 tests):**
- `Migration_Class_Exists_With_Stable_Attribute` — `[Migration(20260825_004)]` attribute present
- `Migration_Overrides_Up_And_Down`
- `Migration_Validates_Journal_Line_Integrity` — file contains `LEFT JOIN accounts a`, `a.id IS NULL`, `orphan_count`
- `Migration_Validates_Trial_Balance_Per_Company` — file contains `SUM(jl.debit)`, `SUM(jl.credit)`, `je.status = 2`, `GROUP BY jl.company_id`
- `Migration_Promotes_New_Canonical_Accounts_To_Migrated` — file contains `UPDATE accounts`, `migration_status = 'migrated'`, `migrated_at = now()`, guarded with `migration_status = 'new'`
- `Migration_Down_Reverts_New_To_Migrated_Promotions` — Down() uses `INTERVAL '1 hour'` guard
- `Migration_Resolves_Company_By_Constitutional_Code` — uses `code = '000'`, not hardcoded UUID
- `Migration_File_Contains_No_Tenant_Id_Reference`
- `Migration_File_Contains_No_Hardcoded_Account_UUIDs`

**CoAValidationService tests (6 tests):**
- `Service_HappyPath_NoIssues_IsValidTrue` — 2 canonical accounts + 2 balanced journal_lines → `IsValid=true, Errors=0, Warnings=0`
- `Service_DuplicateCodes_ProducesError` — same code twice → `IsValid=false, Issues` includes `DUPLICATE_CODE`
- `Service_OrphanJournalLine_ProducesError` — journal_line with unknown account_id → `IsValid=false, Issues` includes `ORPHAN_JOURNAL_LINE`
- `Service_TrialBalanceMismatch_ProducesError` — Dr=1000, Cr=200 → `IsValid=false, Issues` includes `TRIAL_BALANCE_MISMATCH` with variance
- `Service_LegacyAccount_ProducesWarningNotError` — 1 legacy account → `IsValid=true, Warnings=1, Issues` includes `LEGACY_ACCOUNT`
- `Service_InvalidCodeFormat_ProducesError` — code "abc" → `IsValid=false, Issues` includes `INVALID_CODE_FORMAT`

### Architectural compliance

- ✅ Constitution Article 3 — `company_id` only, ZERO `tenant_id` references in any new file
- ✅ Idempotent Migration — RAISE NOTICE for reads, guarded UPDATEs for writes
- ✅ FluentMigrator pattern — matches existing Sprint 24/25/27/28/Wave 1+2 migrations
- ✅ Dapper / no EF Core
- ✅ Reversible (Down() for the migration, IDbConnectionFactory abstraction for the service)
- ✅ No secrets in code
- ✅ All validation queries resolve the holding company by `code = '000'` (constitutional marker), not hardcoded UUIDs
- ✅ All validation queries filter by `company_id` (Article 3)
- ✅ Severity hierarchy: Error flips `IsValid`; Warning does not (so legacy accounts don't fail validation)

### Branch

- `feature/sprint-60-wave-3a-balance-validation` (off `feature/sprint-60-wave-2-merged @ a453606`)
- **LOCAL-ONLY** (Mode 1) — no push, no PR yet (Wave 3B will merge into this branch first)

---

## Sprint 65 Wave 3A — Bank Reconciliation (2026-08-27) ✅ DONE (LOCAL-ONLY)

**Goal:** per Sprint 65 hand-off (`docs/workflow/sprint-65.md`, Wave 3A), deliver the
Receipt ↔ Sub-Payment matching algorithm + the controller endpoints + the FE page so
that when a subcontractor's bank credit appears in our account, the system suggests
which of our sub-payment obligations it satisfies, and the accountant confirms the
match.

**DECs delivered (2):** DEC-235 (BE matching + endpoints), DEC-237 (FE page).

### New files

| Path | Purpose |
|---|---|
| `src/backend/Modules/Finance/Application/Services/BankReconciliationService.cs` | Service + matching algorithm + DTOs (`SubPaymentMatch`, `UnmatchedReceipt`, `BankReconciliationResult<T>`, `ISubPaymentMatcher`, `NoOpSubPaymentMatcher`). |
| `src/backend/Host/Controllers/BankReconciliationsController.cs` | 3 endpoints under `/api/receipts/*/suggest-matches`, `/api/receipts/*/confirm-match/*`, `/api/reconciliation/queue`. |
| `src/backend/Tests/ERPSystem.Tests/Finance/Sprint65BankReconciliationServiceTests.cs` | 7 tests (6 contract + 1 pure-function score). |
| `src/backend/Tests/ERPSystem.Tests/Finance/Sprint65BankReconciliationsControllerTests.cs` | 5 tests (3 contract + 2 error-path). |

### Modified files

| Path | Change |
|---|---|
| `src/backend/Host/Program.cs` | Added `AddScoped<IBankReconciliationService, BankReconciliationService>()` + `AddScoped<ISubPaymentMatcher, NoOpSubPaymentMatcher>()`. |

### Matching algorithm (DEC-235 contract)

The pure-function `BankReconciliationService.ComputeScore` produces a 0-100 score by
summing the best-fit amount bucket + the best-fit date bucket:

| Bucket | Amount score | Date score |
|---|---|---|
| exact | +80 | +20 |
| ±1% / ±7 days | +50 | +10 |
| ±5% / ±30 days | +20 | +5 |
| out of range | 0 | 0 |

The EXCELLENT (>80) / GOOD (50-80) / FAIR (20-50) / POOR (<20) bucket is applied at
the call site after the sort. The function is `public static` so the test assembly
can call it directly without `InternalsVisibleTo`.

### L19 / DEC-095 compliance

- `CompanyId` is read from `ICompanyContext.CompanyId` at the top of every public
  method. The controller never reads companyId from a DTO.
- `UserId` is read from the JWT `sub`/`NameIdentifier` claim in the controller
  (`BankReconciliationsController.UserId`) and passed explicitly to
  `ConfirmMatchAsync`. The service does not extract userId from any request DTO.

### Sprint 64 pre-merge posture

The `sub_payments` table is on the `feature/sprint-64-subcontractor` branch and
has not yet merged into `develop`. The service depends on a pluggable
`ISubPaymentMatcher` interface (mirroring `NoOpSubPaymentRepository` from
Sprint 65 Wave 2A). The default `NoOpSubPaymentMatcher` returns an empty
candidate list. When Sprint 64 merges, a Dapper-backed `ISubPaymentMatcher`
replaces the no-op in `Program.cs` and the unit tests continue to pass without
changes (because they mock the interface).

### Tests (12 total)

**Service tests (7):**
1. `SuggestMatchesAsync_FindsExactMatch_ReturnsScore100` — exact amount + exact date = 100 → EXCELLENT
2. `SuggestMatchesAsync_FindsWithin5Percent_ReturnsWithDiscountedScore` — ±2% amount + ±7d date = 30 → FAIR
3. `SuggestMatchesAsync_NoMatches_ReturnsEmpty` — empty candidate list (Sprint 64 pre-merge)
4. `SuggestMatchesAsync_OrdersByScoreDesc` — perfect (100) > decent (60) > weak (5)
5. `ConfirmMatchAsync_ValidPair_UpdatesBoth` — confirm passes the JWT userId to the matcher
6. `ConfirmMatchAsync_DuplicateConfirm_ThrowsConflict` — InvalidOperationException → CONFLICT
7. `ComputeScore_PureFunction_BucketsWorkAsExpected` — 5 cases of the scoring buckets

**Controller tests (5):**
1. `SuggestMatches_Returns200_WithList`
2. `SuggestMatches_Returns404_WhenServiceReturnsNotFound`
3. `ConfirmMatch_Returns200_WithUpdatedMatch`
4. `ConfirmMatch_Returns409_OnConflict`
5. `GetQueue_Returns200_WithUnmatchedReceipts`

### Branch

- `feature/sprint-65-finance-projects` (off `develop`)
- **LOCAL-ONLY** (Mode 1) — no push, no PR yet

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
_2026-07-31: Sprint 8 T2 — added Test Pattern: SQL AS Alias Support (Local Team takeover)_
_2026-07-31: Sprint 11 T2 — added BE Jimi scope declaration (Mavis Local)_
_2026-08-25: Sprint 60 Wave 1 — DEC-184, DEC-NEW-14, DEC-NEW-15 (DB Foundation, schema + master data)_
_2026-08-27: Sprint 65 Wave 3A — DEC-235 + DEC-237 (Bank Reconciliation)_
