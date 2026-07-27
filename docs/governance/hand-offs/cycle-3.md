# 📦 Hand-Off v1 — Cycle 3: 6.5 CI/Hardening + Complete Phase 6 Tests

> **From:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
> **To:** Mavis Local (Tech Lead) — your session, Windows  
> **Cycle:** 3 / 20 — **ACTIVE ✅**  
> **Created:** 2026-07-27 23:25 UTC

---

## 🎯 Cycle 3 Scope

### Block A (Mavis Local) — Complete Phase 6 Tests + CI Hardening

**Background:** Cycle 2 did 1 of 3 new test cases (`HoldingBootstrap`). This cycle completes the test suite + adds CI hardening.

**Tasks:**

#### T1: Add 2 missing new test cases
- **T1a**: `UserCompany_Limits_Access_To_Assigned_Companies` (unit, ~1h)
  - File: `src/backend/Tests/ERPSystem.Tests/Auth/UserCompanyAccessTests.cs`
  - Verifies: User can only access companies via `user_companies` table
- **T1b**: `CompanySwitcher_Switches_Active_Company_In_Context` (unit, ~1h)
  - File: `src/backend/Tests/ERPSystem.Tests/Auth/CompanySwitcherTests.cs`
  - Verifies: Switching active company updates CompanyContext

#### T2: Add Pre-Commit Hook for Secrets
- **T2a**: Create `.githooks/pre-commit` script
- **T2b**: Install TruffleHog local scan (lightweight, ~5s)
- **T2c**: Document setup in `docs/CONTRIBUTING.md` (or `AGENTS.md`)
- Estimated: 1-2 hours

#### T3: CI Workflow Improvements
- **T3a**: Update `.github/workflows/ci.yml` (or create) to:
  - Run dotnet test in parallel where possible
  - Cache NuGet packages
  - Cache npm packages
- **T3b**: Add fast-fail: if compilation fails, skip tests
- Estimated: 1 hour

#### T4: Fix HF Space Sync Workflow (Optional)
- **T4a**: Investigate why `Sync to HF Space` failed in PR #154
- **T4b**: Either fix or add `[skip-hf-sync]` skip mechanism
- Note: HF Space is "Cloud Sandbox" per DEC-068, not production
- Estimated: 30 min - 1 hour

---

## 🛡️ Permissions (from DEC-070 + DEC-071)

You have full Tech Lead authority:
- ✅ Self-merge (admin bypass)
- ✅ `--force-with-lease`
- ✅ Skip Playwright (not required)
- ✅ Risk tolerance on develop
- ✅ Lead Jimis
- ❌ DO NOT touch HF Space production app (only the workflow)
- ❌ DO NOT touch Supabase prod
- ❌ DO NOT touch main branch
- ❌ NO staging/production setup (DEC-070 freeze)

---

## 🔧 Verification (minimal per DEC-071)

```bash
# 1. Build (REQUIRED)
npx tsc --noEmit
dotnet build Host/ERP-SYSTEM.csproj

# 2. The new tests (REQUIRED)
dotnet test --filter "UserCompany_Limits_Access_To_Assigned_Companies"
dotnet test --filter "CompanySwitcher_Switches_Active_Company_In_Context"
dotnet test --filter "HoldingBootstrap_Seeds_DefaultHolding_And_CoA"  # Re-verify

# 3. CI fast-fail test (manual)
# Modify a .cs file with syntax error, run dotnet build, verify it fails fast
```

---

## 🚨 Risk Notes

- **R1**: Pre-commit hook may slow down local commits (only scan staged files)
- **R2**: CI cache might serve stale packages (use cache-key versioning)
- **R3**: HF Space sync fix may need repo settings (LFS, secrets) — ask if blocked
- **R4**: Don't introduce new dependencies without flagging

---

## 📡 Async Protocol (Reminder)

- New cron `monitor-cycle-3-pr-merge` will be created when you start
- Silent on no-change
- Notify on state-change
- Self-delete on merge
- I'll merge your PR when CI green + you say "ready"

---

## 🚀 When Ready to Start

1. Read this hand-off (you're doing it now)
2. Verify branch state: `git fetch && git status`
3. Create feature branch: `git checkout -b feature/cycle-3-ci-hardening`
4. Do the work (3-4 hours total estimate)
5. Open PR to develop
6. Say "ready for merge"
7. I (سيتي) merge

**You have full authority. Go. 🎯**

---

## 📊 Estimated Time

| Block | Tasks | Time |
|-------|-------|------|
| T1 (2 new tests) | 2 unit tests | 2 hours |
| T2 (pre-commit hook) | script + setup | 1-2 hours |
| T3 (CI improvements) | workflow updates | 1 hour |
| T4 (HF sync fix, optional) | investigation + fix | 30-60 min |
| **Total** | | **3-4 hours** |

---

**Signed:** سيتي (Cloud Coordinator)  
**Authority:** DEC-070 (admin) + DEC-071 (basic, risk tolerance)  
**Date:** 2026-07-27 23:25 UTC
