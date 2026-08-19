# Sprint 58 Follow-up — Plan (DEC-173..176)

**Date:** 2026-08-19
**Branch:** `feature/sprint-52-v0-polish` (continuation of Sprint 58)
**Status:** 🟡 PLANNING
**Goal:** System 100% clean for client demo (no known issues, BS variance = 0)

---

## Goal (from Anas directive)

> "نكمّل Sprint 58 follow-up كامل"

Three known issues from Sprint 58 retro carry-over that need fixing before client delivery:

1. **WIP `GetWipAsync` line 336 throws 42883** (varchar/integer) — Sprint 58 hotfix1 fixed `ApproveAsync` but missed `GetWipAsync`. Same root cause, same fix.
2. **Sidebar version label stale** — shows "v1.0.12 · Sprint 39" but current code is Sprint 58. Cosmetic but client-facing.
3. **BS variance ~720K LYD** from legacy seeders (Sprint 50-55) coexisting with new 2026 scenario data. Fix: disable legacy seeders + drop DB + re-seed → clean 2026 data only.

Item 4 ("ادفع" push) is separate — triggered by Anas at the end.

---

## DEC-173 — WIP GetWipAsync fix (DEC carry-over from Sprint 58)

**File:** `src/backend/Modules/Projects/Application/Services/BillingService.cs`
**Line:** 340
**Issue:** `AND status = 'INVOICED'` — string literal compared to integer column.
**Root cause:** Per L125, `progress_billings.status` is **integer** in DB (1=Draft, 2=Invoiced, 3=Cancelled). The BE serializes as string on API response (L120), but raw SQL against DB needs integer.
**Fix:** Change `status = 'INVOICED'` → `status = 2` (integer).
**Verification:** `GET /api/projects/{id}/wip` returns 200 + WipResponse (not 500).
**Estimated time:** 5 min edit + 1 min build + 30 sec API test.

## DEC-174 — Sidebar version label (cosmetic)

**File:** `src/frontend/components/layout/AppShell.tsx`
**Line:** 210
**Issue:** `v1.0.12 · Sprint 39` — version label not updated since Sprint 39.
**Fix:** Change to `v1.0.13 · Sprint 58` (or just `Sprint 58` — depends on whether we bump version).
**Decision:** Use `v1.0.13 · Sprint 58` (minor bump since 7 sprints added).
**Verification:** Visual inspection of sidebar in browser.
**Estimated time:** 2 min edit + 30 sec browser refresh.

## DEC-175 — Disable legacy seeders (DEC-110 final fix)

**File:** `src/backend/Host/appsettings.Development.json` (gitignored, local only)
**Issue:** `SeedArabicScenario`, `SeedHrScenario`, `SeedProcurementScenario`, `SeedYearScenario`, `SeedLibyanSme`, `SeedProperTransactional` all = `true`. They add data on top of the new 2026 scenario, causing the BS variance.
**Fix:** Set 6 legacy seeders to `false`. Keep:
  - `SeedDemoData: true` (default admin + default holding)
  - `SeedProfessionalCoA: true` (Sprint 58b)
  - `SeedScenario2026: true` (Sprint 58c)
**Destructive step:** Drop `erp_system` DB to start fresh. Re-run BE → only ProfessionalCoA + Scenario2026 + DemoData seed.
**Verification:** 
- `GET /api/finance/ledger/trial-balance?asOf=2026-08-31` → variance should be 0
- `GET /api/finance/ledger/balance-sheet?asOf=2026-08-31` → Assets = Liab + Equity
- Total CoA accounts: 153 (10 L1 + 25-36 L2 + 56-68 L3 + 64 L4) + 0 legacy
- Journal entries: should be the 2026 scenario count only (no duplicate legacy data)
**Estimated time:** 5 min config + 2 min drop DB + 2 min restart + 2 min API test = ~11 min.

## DEC-176 — Push 7 commits via "ادفع" (Mode 2 transition)

**Owner:** Anas (only he can say "ادفع")
**Action:** Admin does git push + PR + CI 6/6 + merge + tag + mvp-docker rebuild + Telegram ping.
**Not in this sprint's scope** — triggered separately at the end.
**Will be tagged:** `v1.0.13-sprint58` after merge.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| WIP fix breaks something else | Very low | Single line change, same pattern as `ApproveAsync` fix |
| Sidebar cosmetic wrong | None | Visual check |
| Drop DB loses useful data | None | All data is demo/seed data, no production data on this DB |
| New seeders fail on fresh DB | Low | Already verified in Sprint 58 (1,321 entries balanced) |
| mvp-docker not affected | — | mvp-docker has its own config (no seeders in clean install) |

---

## Execution Order

1. **DEC-173** (WIP fix) — first, smallest, self-contained
2. **DEC-174** (sidebar label) — second, cosmetic, no DB interaction
3. **DEC-175** (disable seeders + re-seed) — third, destructive step
4. **DEC-176** (push) — only after Anas says "ادفع"

## Pre-flight Checklist

- [ ] Git working tree clean
- [ ] Branch `feature/sprint-52-v0-polish` @ `e47068c`
- [ ] No uncommitted changes
- [ ] BE + FE currently off (DB up)

## Post-sprint Verification

- [ ] `dotnet build` — 0 errors
- [ ] `GET /api/projects/{id}/wip` — returns 200
- [ ] `GET /api/finance/ledger/trial-balance?asOf=2026-08-31` — variance = 0
- [ ] `GET /api/finance/ledger/balance-sheet?asOf=2026-08-31` — A = L + E
- [ ] Browser: `/finance/accounts` renders, sidebar shows "Sprint 58"
- [ ] Update CHANGELOG.md
- [ ] Update AGENTS.md if contracts/rules changed
- [ ] Write sprint-58-followup-retro.md

---

**Status:** Ready to execute. Will commit each DEC separately for clean history.
