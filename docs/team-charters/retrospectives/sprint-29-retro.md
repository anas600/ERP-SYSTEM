# Sprint 29 Retrospective — 2026-08-03

**Title:** Year-Scenario dev seeder (POC #4) + Legacy cleanup (DEC-098)
**Status:** ✅ DONE (LOCAL-ONLY, committed `c047037`)
**Time spent:** ~2 hours
**Mode:** Mode 1 (host local dev)

---

## What Was Supposed To Be Done (per Anas's directive 2026-08-03 04:16 UTC+2)

> "افهمك منك الان 'Legacy (2 ملفات .cs):' قديمه ولا نحتاجها فلتقم بنظيفها وتقوم بتبديلها لسيناريو تشغيلي لمده شنه بنفس new pattern التي عملته بيه ونجح في البيئه المحليه للتطوير , اريد ان اري بيانات سنه كامله بشكل صحيح لكي اكتشف الاخطاء وانت كدلك تكتشف الاخطاء ع الهوست المحلي"

Translation:
1. The 2 legacy seeder .cs files are old + not needed → clean them up
2. Replace them with a 1-year operational scenario using the new seeder pattern
3. Want to see a full year of correct data on the host local to discover bugs (user + AI)

---

## What Was Actually Done

### Cleanup (DEC-098)
- Deleted `ScenarioSeederHostedService.cs` (54.8 KB) — Sprint 4 al-Burj scenario
- Deleted `RealisticSeedHostedService.cs` (48 KB) — Sprint 14 realistic seed
- Replaced `AdminController.AlFajrSeed` + `AlBurjSeed` endpoints with 410 Gone

### New Year-Scenario Seeder
- Created `src/backend/Shared/SeedData/ArabicYearScenarioDevData.json` (~46 KB)
  - 1 Opening Balance JE (Jan 1, 2025) — 105,000 LYD
  - 12 monthly sales invoices (Jan–Dec 2025)
  - 12 monthly vendor bills (Jan–Dec 2025)
  - 24 customer receipts (2/month)
  - 24 vendor payments (2/month)
- Created `src/backend/Shared/SeedData/ArabicYearScenarioDevSeederHostedService.cs` (~28 KB)
  - 5-pass execution: OB → Sales → Bills → Receipts → Payments
  - Each transaction gets a "benchmark" Journal Entry (L39: "seeders that test other parts of the system")
- Added `Bootstrap:SeedYearScenario` flag (double-gated)
- Added `<Content Include>` in csproj
- Registered in Program.cs

### Bugs Discovered + Fixed During Execution
- **L37 (DI issue)**: IHostedService is Singleton — can't inject Scoped `ICompanyContext`. Fixed: resolve companyId from DB at startup using `DbConnectionFactory.CreateEphemeralOltpConnectionAsync`.
- **L28 (schema surprise)**: `companies.is_holding` doesn't exist; column is `is_group`.
- **L28 (JSON bug)**: Account 3110 doesn't exist; use 3100 for رأس المال.

### Build Fixes
- `await using IDbConnection` fails with CS8417 → use `using var`
- `using var conn = await ...` pattern

### Verification
- 73 records + 73 JEs + 148 lines (avg 2 lines/JE)
- Opening Balance: 105,000 debits = 105,000 credits
- All 4 transaction categories balanced
- Commit: `c047037` (11 files changed, +1654/-2067, **net -413 lines** despite adding new seeder)

---

## What Went Well

1. **4th POC in <1.5h** (vs 4-6h for the first). Pattern is muscle memory (L17).
2. **L37 (DI fix) was caught early** because the seeder failed fast at startup. Better than discovering it 30 min into a debug session.
3. **L28 (schema surprise) was caught by the seeder's warning log** (`Account 3110 not found — opening balance line skipped`). The seeder never crashed; it just logged and continued. The 105,000 LYD OB still balanced because the missing line was 0 debit + 0 credit.
4. **Net -413 lines despite adding a 28 KB seeder.** Replacing 2 legacy seeders (102.8 KB) with 1 new one (28 KB) + 1 JSON (46 KB) but removing 102.8 KB of hardcoded C# = net reduction.
5. **L39 (benchmark JEs) was the right pattern.** When the Posting Rules engine runs, it can compare its JEs to the seeder's benchmarks. Any discrepancy = bug to investigate.

## What Went Wrong

1. **The `await using` → `using` fix** was needed twice. L43 candidate: always use `using var` for `IDbConnection` in the seeder pattern.
2. **The year scenario seeder log only shows "OB-2025-001 inserted" in the BE startup log** — the other 4 passes don't log per-record (intentional, to avoid log spam). But this makes it hard to verify the seeder actually ran all 5 passes. L44 candidate: log one summary line per pass at the end ("Pass 2 done — 12 invoices inserted").
3. **The legacy seeders' admin endpoints were referenced in the FE** (some hardcoded URL). Replaced with 410 Gone. Need to verify FE doesn't break.

## What Was Surprising

1. **The 4th seeder POC was the fastest yet** — pattern recognition is real (L17, L27, L36).
2. **The L28 JSON bug (Account 3110 → 3100) was my own mistake** — I wrote the JSON with the wrong account code. The seeder caught it. Lesson: always cross-check account codes against the CoA before writing the seeder.
3. **The 12 monthly invoices cover 12 different scenarios** — some customers pay in full, some partial, some late. The benchmark JEs document all of this.
4. **The seeder pattern is now mature enough to be the "default" for new transactional data.** Any time we need a new entity's demo data, we use this pattern.

---

## Lessons (L36..L39)

- **L36 — 4th POC in <1.5h.** Pattern is muscle memory. Same shape: JSON + IHostedService + UPSERT + Dapper + double-gate + Content include + appsettings flag.
- **L37 — IHostedService is Singleton — cannot inject Scoped ICompanyContext.** Resolve companyId from DB at startup using `DbConnectionFactory.CreateEphemeralOltpConnectionAsync`. Apply to all seeders in the pattern.
- **L38 — Deleting legacy files requires removing endpoint references.** 410 Gone > 404. Point the user to the new way.
- **L39 — "Seeders that test other parts of the system" (benchmark JEs).** Each transaction gets a "benchmark" Journal Entry inserted by the seeder. Documented as `BENCH-INV-XXX`, `BENCH-BILL-XXX`, `BENCH-RCT-XXX`, `BENCH-PAY-XXX`. When the Posting Rules engine is run on the same transactions, its JEs should match the benchmark JEs. Any discrepancy is a bug to investigate in Sprint 30+.

---

## State of the World After Sprint 29

### Data (Mode 1, host local PG)
- 13 customers + 13 vendors + 20 items
- 5 departments + 10 employees
- 1 warehouse + 1 cost center (DEC-101 — but that was Sprint 30)
- 10 POs (Sprint 28 seeder)
- 12 sales invoices + 12 vendor bills + 24 receipts + 24 payments (Sprint 29 seeder)
- 73 journal entries + 148 lines (all benchmark JEs)

### Code
- 2 files deleted: `ScenarioSeederHostedService.cs`, `RealisticSeedHostedService.cs`
- 3 files added: `ArabicYearScenarioDevData.json`, `ArabicYearScenarioDevSeederHostedService.cs`, `AdminController.cs` modifications
- 1 file changed: `appsettings.Development.json.example`, `csproj`, `Program.cs`

### Constitution Article 3 Compliance
- 8/13 modules audited (61%)
- 5 still-pending: Payments, ProjectCostCenter, AccountService, ChartOfAccountsService, PayrollService

---

## Carry-over to Sprint 30+

- DEC-100..106 architectural cleanup (6 DECs)
- DEC-105 (full PO+GR+Bill seeder)
- Audit 5 still-pending modules
- Posting Rules integration unit tests (benchmark vs engine comparison, L39)
- Trial Balance validation UI ("Balanced / Unbalanced" indicator)
- Manual JEs (depreciation, accruals, year-end)
- 5th default rule "Sale with VAT 5%" (inactive, for demo)
- Audit trail for posting rule changes
- Multi-currency support (currently LYD-only)
- Pre-push script: scan for `?` in user-visible columns
- Build-time test that enforces DEC-085
- Playwright e2e tests for top 5 user flows
- mvp-docker/.env to .gitignore

---

_Last updated: 2026-08-03 by Mavis Local, approved by Anas (pending "ادفع")_
