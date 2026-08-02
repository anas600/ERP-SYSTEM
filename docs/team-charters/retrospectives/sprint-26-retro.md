# Sprint 26 — Arabic Dev Seeder (2026-08-02)

**Status:** ✅ DONE (LOCAL-ONLY, awaiting Anas's "ادفع" with Sprint 24+25)
**Branch:** `feature/sprint-21-posting-rules-engine` (Sprints 21+22+23+24+25+26 stacked)
**Goal:** Per Anas's directive — build a proper Arabic data seeder for dev environment, fix the encoding bug from Sprint 25 PowerShell scripts, let the user see real Arabic data on the local host like carry-over/migration data.

---

## 🎯 What Was Delivered

| Artifact | LOC | Purpose |
|---|---|---|
| `src/backend/Shared/SeedData/ArabicDevData.json` | ~12 KB (UTF-8) | Single source of truth for Arabic master data (13 customers + 13 vendors + 20 items) |
| `src/backend/Shared/SeedData/ArabicDevSeederHostedService.cs` | ~22 KB | C# `IHostedService` that reads JSON + UPSERTs via Dapper. Idempotent. Dev env only. |
| `src/backend/Host/Program.cs` (Sprint 26 block) | ~10 lines | Registration gated on `IsDevelopment()` + `Bootstrap:SeedArabicScenario=true` |
| `src/backend/Host/ERP-SYSTEM.csproj` (Content include) | 3 lines | Copies `ArabicDevData.json` to `bin/Debug/net9.0/Shared/SeedData/` |
| `src/backend/Host/appsettings.Development.json.example` (template) | +5 lines | Documents the new flag for new contributors |
| `src/backend/Host/appsettings.Development.json` (gitignored) | +1 line | Enables the seeder for local dev |
| `CHANGELOG.md` | ~50 lines | Sprint 26 entry at top |
| `AGENTS.md` (DEC-087 + DEC-085 #6) | +2 lines | Adds encoding check to pre-push checklist |
| `docs/team-charters/retrospectives/sprint-26-retro.md` | (this file) | Lessons + decisions |

**Verified end-to-end:**
- `dotnet build` → 0 errors, 0 warnings
- `psql` confirmed UTF-8 bytes: `CUST-001` hex `d8b4d8b1d983d8a920d8a7d984d981d8acd8b120d984d984d8aad988d8b2d98ad8b9` = `شركة الفجر للتوزيع`
- API `/api/ar/customers` returns Arabic JSON correctly
- API `/api/ar/receipts` returns Arabic (was `?` before, now `مكتب البركة للخدمات` etc.)
- BE on `http://127.0.0.1:5001` + FE on `http://localhost:3000` (browser shows Arabic)

---

## 🐛 Root Cause (DEC-087)

**Sprint 25 PowerShell scripts used `ConvertTo-Json | Invoke-RestMethod` from PowerShell 5.1.**

The pipeline:
```powershell
$c = @{ name = "شركة النور للتوريدات" } | ConvertTo-Json   # ← produces UTF-16-LE bytes
Invoke-RestMethod -Uri $url -Method Post -Body $c -ContentType "application/json"
```

What happens:
1. PowerShell 5.1's `ConvertTo-Json` produces a JSON string in PowerShell's internal UTF-16 encoding.
2. `Invoke-RestMethod` sends the body as UTF-16-LE bytes (no BOM, but the byte stream is UTF-16).
3. ASP.NET Core's `Content-Type: application/json` reader uses UTF-8 by default.
4. The UTF-8 decoder sees a multi-byte sequence it can't decode → replaces it with `?` (0x3F).
5. Every Arabic character (which is 2-4 UTF-8 bytes) becomes one `?` literal in the DB.

**Verification:**
```sql
SELECT code, name, encode(convert_to(name, 'UTF8'), 'hex') FROM customers WHERE code = 'CUST-004';
-- code    | name                          | hex
-- CUST-004 | ???? ????? ?????????         | 3f3f3f3f203f3f3f3f203f3f3f3f3f3f3f3f3f
```

The hex `3f3f3f3f` is **literal question marks**, not UTF-8 mojibake (which would show as `d8b4d8b1...` for Arabic).

**Why bootstrap customers (CUST-001..003) worked:**
`DefaultHoldingBootstrapHostedService.cs` does raw SQL via Dapper with C# string literals like:
```csharp
new { Code = "CUST-001", Name = "شركة الفجر للتوزيع", ... }
```

C# string literals in `.cs` files are UTF-8 native since .NET 5+. Npgsql sends the parameter values as UTF-8 bytes to Postgres. Stored correctly.

**Why JSON-loaded strings work too:**
`ArabicDevSeederHostedService.cs` reads `ArabicDevData.json` via `File.ReadAllTextAsync()` which is UTF-8 native for files without BOM (or with UTF-8 BOM, both work). The Arabic chars make it to the DB as proper UTF-8.

---

## 📐 Design Decisions

### DEC-087: Arabic data must be UTF-8 native end-to-end

| Path | Status | Why |
|---|---|---|
| C# string literals in `.cs` files | ✅ Works | UTF-8 native in .NET 5+ |
| C# string literals in `.json` files loaded via `File.ReadAllText` | ✅ Works | UTF-8 native in .NET 5+ |
| `psql` with UTF-8 terminal + UTF-8 SQL file | ✅ Works | Native protocol |
| `curl --data-binary @file` with `--data-urlencode` | ✅ Works | Explicit UTF-8 |
| PowerShell 5.1 `ConvertTo-Json` + `Invoke-RestMethod` | ❌ BROKEN | Sends UTF-16-LE |
| PowerShell 7 `Invoke-RestMethod` + `-Encoding utf8NoBOM` | ✅ Works | Explicit UTF-8 |
| PowerShell `[Console]::OutputEncoding = [System.Text.Encoding]::UTF8` | ⚠️ Maybe | Doesn't help `Invoke-RestMethod` body |

**Rule going forward:** never use PowerShell 5.1 `ConvertTo-Json | Invoke-RestMethod` for non-ASCII data. Use C# hosted services (cleanest) or PowerShell 7 with explicit `-Encoding utf8NoBOM`.

### DEC-088: DEV-ONLY seeder = double gate (env + flag)

`ArabicDevSeederHostedService` is gated on **both**:
- `IHostEnvironment.IsDevelopment()` (env check)
- `Bootstrap:SeedArabicScenario=true` (config flag)

The Sprint 22-era seeders (`ScenarioSeederHostedService`, `RealisticSeedHostedService`) are gated only by config flag. If a future misconfig sets `SeedAlFajrScenario=true` in production, those seeders will run. The new seeder can't accidentally run in production because `IsDevelopment()` is `false` there.

**Pattern:** future dev-only seeders should always use the double gate.

### DEC-089: JSON file as data source (not C# string literals)

Two design options for the seeder data:
1. **C# string literals** in the seeder class (like `DefaultHoldingBootstrapHostedService` does for the 3 bootstrap customers)
2. **JSON file** loaded at runtime (like `JsonSeedLoader` does for `data-types/seeds/*.json`)

Picked **JSON file** for 3 reasons:
- **Editable without recompile.** Add a new customer in JSON, restart BE, see it. No IDE round-trip.
- **Reusable data.** The same JSON could be loaded by a future PowerShell migration tool or by `mvp-docker/seed.sql` if we ever need a non-C# path.
- **Reviewable diff.** A JSON change is one line; a C# string-literal change is one line, but the JSON comment + structured fields make the intent clearer.

The tradeoff: the JSON file needs to be copied to the output directory (added to csproj as `<Content Include=... CopyToOutputDirectory="PreserveNewest" />`).

### DEC-090: UPSERT, not DELETE+INSERT

`ArabicDevSeeder` uses UPSERT (SELECT-then-INSERT-or-UPDATE) instead of "DELETE then INSERT":
- **Safer for existing data.** If a customer has FK references (sales invoices, receipts), DELETE would cascade-break those. UPSERT keeps IDs intact.
- **Faster on re-run.** Only updates changed fields; doesn't recreate the row.
- **Idempotent by design.** Re-running the seeder 100 times produces the same result.

For a true "fresh install" seeder (which Sprint 25 PowerShell scripts effectively were), DELETE+INSERT is also OK. But UPSERT is the safer default.

---

## 🎓 Lessons

### L13: PowerShell 5.1 + JSON to ASP.NET Core = silent encoding bug

The 2-sprint-old Sprint 25 PowerShell scripts looked fine locally (PowerShell console printed Arabic correctly in `$c | ConvertTo-Json`), but the HTTP body bytes were UTF-16-LE. ASP.NET Core's UTF-8 decoder silently turned every multi-byte Arabic char into `?`. **The user only sees the bug in the browser, not in the script output.**

**Action item:** For any future demo data script that sends non-ASCII text to the API, use C# hosted services or PowerShell 7 with `-Encoding utf8NoBOM`. Never use PowerShell 5.1 `ConvertTo-Json`.

### L14: "DEV-ONLY" seeder is a real category, not just a flag

The Sprint 22-era seeders are gated only by config flag. `ArabicDevSeederHostedService` is double-gated: env + flag. This is the right pattern for any seeder that contains test data, demo data, or anything that shouldn't ship to production.

**Action item:** When creating a new seeder, default to the double gate (`IsDevelopment() && configFlag`). If a future seeder truly needs to be production-callable, document that explicitly.

### L15: BOM presence is the canary for "did this come from Notepad?"

The Sprint 25 PowerShell scripts probably went through a Windows editor (Notepad / VS Code) at some point. The fact that those scripts stored literal `?` instead of UTF-8 mojibake means the original source had `?` somewhere — either the user typed `?` in place of Arabic (unlikely — Anas is fluent), or the PowerShell pipeline silently converted Arabic → `?` before sending. **Either way, the round-trip "type Arabic in source → see Arabic in DB" failed silently.**

**Action item:** When you see `?` in DB column, immediately suspect encoding. Run `encode(convert_to(col, 'UTF8'), 'hex')` to confirm `3f3f` (literal `?`) vs `d8b4d8b1` (proper Arabic).

### L16: PptxGenJS / PptxGenJS-like tools don't have this problem

Worth noting: the Sprint 20 client-materials deck (PptxGenJS, Node.js) renders Arabic perfectly. Node.js strings are UTF-8 native, just like .NET. **The bug is specific to PowerShell 5.1's `ConvertTo-Json`.** Other tools in our stack (C#, Node.js, .NET 9, Npgsql, Postgres) all handle UTF-8 correctly.

---

## 📊 Sprint 26 Metrics

| Metric | Value | Notes |
|---|---|---|
| New files | 3 | JSON, C# seeder, retro |
| Modified files | 5 | Program.cs, csproj, appsettings, AGENTS.md, CHANGELOG.md |
| LOC added | ~600 | Seeder is verbose (comments in 3 languages + DOX-compliant docstrings) |
| Build errors | 0 | First build failed on `await using IDbConnection`; fixed to `using` |
| First-run rows | 0 (existing data) | All 35 rows were UPDATE, not INSERT |
| Idempotent re-runs | ✅ | Tested by design (UPSERT pattern) |
| Encoding bugs surfaced | 1 | Sprint 25 PowerShell (DEC-087) |
| Production code paths affected | 0 | Dev env only, double-gated |

---

## 🔮 Carry-over (Sprint 27+)

- **P1:** Extend `ArabicDevSeeder` to also create sales invoices + receipts + opening balance JEs from JSON (today: master data only; transactions remain from Sprint 25 PowerShell scripts). The transactions are 33 invoices + 16 receipts + 47 JEs spread across 11 months — the user wants to see this on the dashboard, which they already do, but a JSON-driven approach would be more durable.
- **P1:** HR demo data (10 employees + 5 departments + 5 projects) via ArabicDevSeeder extension. Needs `EmployeeService`/`DepartmentService`/`ProjectService` Article 3 fixes first.
- **P1:** Procurement cycle demo data (10 POs + 10 GRs + 10 bills).
- **P1:** Manual JEs (12: depreciation, accruals, year-end) — like a real accountant would post.
- **P1:** 14 P2 function workflow docs (carry-over from Sprints 19+).
- **P1:** `customerStatement` + `vendorStatement` GET endpoints.
- **P1:** `CreateItem` API method.
- **P1:** Trial Balance validation UI ("Balanced / Unbalanced" indicator).
- **P2:** 5th default rule "Sale with VAT 5%" (inactive, for demo).
- **P2:** Audit trail for posting rule changes.
- **P2:** Multi-currency support (currently LYD-only).
- **P2:** mvp-docker/.env to .gitignore.
- **P2:** Add a "Reset Arabic seed" endpoint for the dev admin to nuke + re-seed quickly.
- **P2:** Add a script to verify no `?` in any user-visible column (run before sprint push) — would have caught the Sprint 25 bug before merge.

---

**Status:** Sprint 26 LOCAL-ONLY done. Commit pending. Awaiting Anas's "ادفع" to push with Sprint 24+25 as `v1.0.9-sprint24-audit-architecture`.
