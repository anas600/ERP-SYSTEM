# 📦 Hand-Off v1 — Cycle 5: Smart Cron + Phase 6 Polish (Real Features)

> **From:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
> **To:** Mavis Local (Tech Lead) — your session, Windows  
> **Cycle:** 5 / 20 — **ACTIVE ✅**  
> **Created:** 2026-07-28 00:55 UTC  
> **Inspired by:** Mavis Local's lessons-learned.md (cycle 4)

---

## 🎯 Cycle 5 Scope (Real Features, Not Governance)

Per Mavis Local's recommendation: "Cycle 5 should be a 'real feature' cycle (not governance). The protocol is now mature. Time to do something that shows the user."

### Block A (Mavis Local) — Smart Cron Implementation

**Background:** Mavis Local recommended in cycle 4 lessons-learned:
> "Smart cron for cloud failure detection (DEC-072 implementation). The protocol is documented; needs implementation. A token-free health-ping that writes to `docs/governance/board.md` would solve the cycle 1 case."

**Tasks:**

- **T1**: Create `scripts/health-ping.sh` (POSIX bash, no secrets)
  - Token-free: just check if the cloud session is alive
  - Write status to `docs/governance/internal/health-ping.json`
  - Format: `{ "last_check": "ISO timestamp", "status": "alive|idle|stuck" }`
  - Schedule: cron in cloud session, every 10 min (cheap)
  
- **T2**: Update `docs/governance/board.md` to show last health-ping
  - Add a small section: "## 💓 Health (last 10 min)"
  - If `status: stuck` for >30 min → show 🔴 alert
  - If `status: alive` → show 🟢 normal
  - If no ping in 1 hour → show ⚪ silent

- **T3**: Add `health-ping.yml` GitHub Action (optional)
  - Runs every 10 min
  - Writes timestamp to a file (token-free, no secrets)
  - Self-cleanup

**Estimated time:** 2-3 hours

### Block B (Mavis Local) — Phase 6 Polish

**Background:** Phase 6 (Multi-Company) is done. Some polish items remain that don't require staging/prod.

**Tasks:**

- **T4**: Add user-facing "Company Switcher" UI test (Playwright, optional per DEC-070)
  - Test: user clicks switcher, dropdown shows assigned companies
  - File: `tests/company-switcher.spec.ts` (new)
  - Note: Playwright is OPTIONAL, but nice to have

- **T5**: Add `CompanySwitcher` README section
  - File: `src/frontend/app/admin/companies/README.md` (new, brief)
  - 1-page doc: what it does, how to use, screenshot (placeholder)

- **T6**: Add Holding bootstrap smoke test (C#, optional)
  - File: `src/backend/Tests/ERPSystem.Tests/Companies/HoldingSmokeTest.cs` (new)
  - Verifies: when app starts, Holding exists in DB
  - Estimated: 30 min

**Estimated time:** 1-2 hours

---

## 🛡️ Permissions (DEC-070 + DEC-071 + DEC-072)

- ✅ Self-merge (with `--admin` flag per lessons-learned)
- ✅ `--force-with-lease`
- ✅ Skip Playwright (optional, not required)
- ✅ Risk tolerance on develop
- ✅ Lead Jimis
- ❌ NO staging/production (frozen)
- ❌ NO HF Space production app
- ❌ NO main branch

---

## 🔧 Verification (per DEC-071 + lessons-learned)

```bash
# 1. Build (REQUIRED)
npx tsc --noEmit
dotnet build Host/ERP-SYSTEM.csproj

# 2. The new tests (if added)
dotnet test --filter "HoldingSmokeTest"
npx playwright test tests/company-switcher.spec.ts  # OPTIONAL

# 3. Health-ping test
bash scripts/health-ping.sh
cat docs/governance/internal/health-ping.json  # Should show recent timestamp
```

---

## 🚨 Pre-Hand-off Verification (per lessons-learned)

**Before starting, Mavis Local should:**

1. **Inventory check** — run `git log origin/develop --oneline | head -20` to confirm what's already done
2. **Check existing health-ping** — is there already a script? (If yes, extend it; don't create duplicate)
3. **Check existing CompanySwitcher** — is the UI component already there? (Verify scope)
4. **Document inventory in response** — list what was found + what was added

---

## 📋 Lessons-learned applied

✅ **From cycle 4 lessons:**
- Smart cron pattern (Block A) — implements the recommendation
- POSIX bash over PowerShell (T1)
- Document inventory in response (verification section)
- Self-merge with `--admin` flag (per DEC-070)
- No new dependencies without flagging (T1-T6 use only existing tools)

---

## 📡 Async Protocol (Reminder)

- `monitor-cycle-3-pr-merge` is still running (cycle 3 was earlier) — may need to update
- `presence-check` cron is active (DEC-072)
- New cron for cycle 5 PR: I'll create `monitor-cycle-5-pr-merge` when you start
- Silent on no-change
- Self-delete on merge

---

## 🚀 When Ready to Start

1. Read this hand-off ✅
2. Verify with `git log origin/develop --oneline | head -10` (per lessons-learned)
3. Create feature branch: `git checkout -b feature/cycle-5-smart-cron`
4. Do the work (3-4 hours total)
5. Open PR to develop
6. Say "ready for merge"
7. I (سيتي) merge

**You have full authority. Go. 🎯**

---

**Signed:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
**Authority:** DEC-070 (admin) + DEC-071 (basic) + DEC-072 (presence)  
**Date:** 2026-07-28 00:55 UTC
