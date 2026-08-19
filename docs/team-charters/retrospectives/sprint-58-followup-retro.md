# Sprint 58 Follow-up Retro — DEC-173..176

**Date:** 2026-08-19
**Branch:** `feature/sprint-52-v0-polish` (continuation of Sprint 58)
**Status:** ✅ DONE (LOCAL-ONLY) — awaiting "ادفع" for DEC-176
**Duration:** ~2 hours (start to verified)

---

## Goal

> Per Anas directive (2026-08-19 ~02:25 UTC+2): "نكمّل Sprint 58 follow-up كامل"

Three known issues from Sprint 58 carry-over that needed fixing before client delivery:
1. WIP `GetWipAsync` line 336/340 (alleged 42883 varchar/integer error)
2. Sidebar version label stale (showed "Sprint 39")
3. BS variance ~720K LYD from legacy seeders coexisting with new 2026 scenario

---

## What was done

### DEC-173 — WIP `GetWipAsync` verification (no-op)
- **Investigation:** Confirmed `progress_billings.status` is `varchar(20)` (per data-types JSON), not integer.
- **Conclusion:** Line 340 `AND status = 'INVOICED'` is correct (string vs varchar). No 42883 error.
- **Verified live:** `GET /api/projects/{id}/wip` returns 200 with `status: "BALANCED"`.
- **Lesson L140:** The Sprint 58 hotfix1 was a misdiagnosis (or a fix for a different issue). Status columns can be varchar even when API returns string enums — always check `data-types/*.json` first.

### DEC-174 — Sidebar version label (cosmetic)
- **File:** `src/frontend/components/layout/AppShell.tsx` line 210
- **Change:** `v1.0.12 · Sprint 39` → `v1.0.13 · Sprint 58`
- **Why:** Version label was stale from Sprint 39. Minor version bump for 7 new sprint commits.

### DEC-175 — Disable legacy seeders (DEC-110 final fix)
- **File:** `src/backend/Host/appsettings.Development.json` (gitignored, local only)
- **Change:** Set 6 legacy scenario seeders to `false`. Kept: `SeedDemoData` + `SeedProfessionalCoA` + `SeedScenario2026`.
- **Destructive step:** Dropped `erp_system` DB + re-ran BE → clean 2026 data only.
- **Result:** BS variance **720K → 0**. Dr 14,565,614 = Cr 14,565,614.
- **Verified:**
  - Trial Balance: 35 L4 accounts with balances from 2026 scenario
  - Dashboard 8 KPIs: Revenue 2.885M / Net 2.260M / Cash 3.454M
  - 3 projects, 6 customers (5 active), 5 vendors (all 5 active)
  - 56 journal entries, 133 journal lines, all balanced
- **Lesson L141:** `DefaultCoASeed.cs` (the 4-digit 1000/2000/3000/... baseline) is NOT gated by a seeder toggle — always runs via `DefaultHoldingBootstrapHostedService`. The BS variance wasn't from these accounts (no journal lines). It was from the 6 legacy *scenario* seeders posting to mixed code sets.

### DEC-176 — Push 7 commits via "ادفع" (deferred)
- **Status:** ⏳ Waiting for Anas's "ادفع" command
- **Will be tagged:** `v1.0.13-sprint58` after merge
- **Workflow:** push → PR → CI 6/6 → relax → merge → tag → restore → mvp-docker rebuild → Telegram ping

---

## Commits

| Commit | Description |
|---|---|
| `9cc66d5` | Sprint 58 follow-up: sidebar v1.0.13/Sprint 58 + plan docs |
| (pending) | CHANGELOG entry for DEC-173..176 |

---

## Lessons Learned

- **L140:** When status column is varchar + default `'DRAFT'`, string literal `'INVOICED'` is correct. Don't assume integer because the API response stringifies enums. Always check `data-types/*.json` schema definition before changing SQL.
- **L141:** `DefaultCoASeed.cs` is the always-on baseline CoA (4-digit codes 1000/2000/...). Not gated by a toggle. Designed that way for new company bootstrap. The "variance" in Sprint 58 was from legacy *scenario* seeders, not from this baseline.

---

## What worked well

- **Plan-first** (per Anas Sprint 52 directive): Wrote `docs/plans/sprint-58-followup.md` before any code change. Helped catch that DEC-173 was a no-op before spending time fixing a non-bug.
- **Direct SQL verification:** Checking `progress_billings` schema via psql before assuming integer type. Saved time vs trial-and-error.
- **Live API verification:** Tested WIP and Dashboard endpoints with real JWT + project IDs from fresh DB.

## What didn't work

- **dotnet run logging:** `RedirectStandardOutput` with `ProcessStartInfo` doesn't capture output reliably when the process binds to a port. Logs from new BE process weren't written to `be-sprint58b.log`. Switched to checking `Get-NetTCPConnection` for liveness instead.
- **PowerShell `Select-String -Recurse`:** Not a valid flag. Used `grep` tool instead.

## Pending carry-over

- **DEC-176:** Push via "ادفع" (only Anas can trigger)
- **WIP test data:** progress_billings table is still empty (0 rows). When real billings are posted, WIP will show non-zero values. The endpoint works correctly, just no data to demonstrate.
- **Sidebar "Sprint 39" comment:** Line 309 still has `Sprint 39 (DEC-125)` comment. Not user-facing — left as-is.
- **Sprint 59 planning:** Variation Orders (DEC-166..167) + remaining modules' Modern UI redesign (Finance/HR/Admin/Procurement/Resources/Transactions).

---

**Status:** Sprint 58 fully closed. System clean for client demo. Ready for "ادفع".
**Next action:** Awaiting Anas's "ادفع" (DEC-176) OR Sprint 59 kickoff.
