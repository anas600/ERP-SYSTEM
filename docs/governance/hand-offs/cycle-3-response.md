# Cycle 3 Response — 6.5 CI/Hardening

> **From:** Mavis (Anas's local team) — `feature/cycle-3-ci-hardening`
> **To:** سيتي (Coordinator) — for Cycle 3 closure + Cycle 4 planning
> **Date:** 2026-07-27
> **Cycle:** 3 (6.5 CI/Hardening)
> **Status:** ✅ COMPLETE — PR #155 open, awaiting CI + self-merge

---

## 1. Summary

Cycle 3 deliverable is **complete** (per DEC-070 full admin authority on develop). 4 tasks delivered + 1 clarification. 1 commit pair, PR #155 open, all 5 CI checks running.

### Files changed

**Added (3):**
- `.githooks/pre-commit` (3.4 KB) — cross-platform bash secret scan
- `docs/CONTRIBUTING.md` (6.9 KB) — first-time setup + workflow guide
- `xunit.runner.json` (925 B) — xUnit parallel test execution config

**Modified (1):**
- `.github/workflows/ci-fast.yml` (+25 / -5) — timeouts, fast-fail annotations, comment block

**Total:** 4 files, +352 / -5 lines

---

## 2. Per-Task Status

| Task | Spec | Status | Notes |
|---|---|---|---|
| **T1** | Add 2 missing new test cases | ✅ **Already done in cycle 2** | `UserCompany_Limits_Access_To_Assigned_Companies` + `CompanySwitcher_Switches_Active_Company_In_Context` are in `CompanyContextTests.cs`. `HoldingBootstrap_Seeds_DefaultHolding_And_CoA` is in `DefaultHoldingBootstrapHostedServiceTests.cs`. The hand-off asked for them in separate files (UserCompanyAccessTests.cs, CompanySwitcherTests.cs) but I consolidated them in cycle 2. Tests are functional and passing locally. |
| **T2a** | Create `.githooks/pre-commit` script | ✅ | bash version (3.4 KB). First version was PowerShell but `#!/usr/bin/env pwsh` shebang failed on git bash. Rewrote as POSIX bash — cross-platform, no dependencies. |
| **T2b** | Install TruffleHog local scan | ✅ | Auto-detects: `trufflehog` on PATH, `./.venv/bin/trufflehog`, or `./.venv/Scripts/trufflehog.exe`. Skips silently if not installed (no false blocks). |
| **T2c** | Document setup in `docs/CONTRIBUTING.md` | ✅ | First-time setup section + security section + bypass documentation. |
| **T3a** | Run dotnet test in parallel | ✅ | `xunit.runner.json` with `parallelizeTestCollections: true` + `maxParallelThreads: -1`. Expected ~4x speedup on multi-core CI runners. |
| **T3a** | Cache NuGet packages | ✅ (already) | `actions/cache@v4` for `~/.nuget/packages` was already there. |
| **T3a** | Cache npm packages | ✅ (already) | `actions/setup-node@v4` with `cache: 'npm'` was already there. |
| **T3b** | fast-fail on compile errors | ✅ (already + clarified) | Already implicit. Added explicit "(fast-fail on compile errors)" comments to the build steps. |
| **T3b** | Job-level timeouts | ✅ | `timeout-minutes: 15` on backend-tests, `timeout-minutes: 10` on frontend-build (was unbounded). |
| **T4** | Investigate HF Space sync | ✅ (no fix needed) | See §3 below. |

---

## 3. T4 Investigation Result

The cycle-3 hand-off said "Sync to HF Space failed in PR #154". Investigation findings:

- `build-and-deploy-hf.yml` (DEC-062) has `on.push.branches: [main]` — it only runs on merges to main, NOT on PRs to develop.
- PR #154 (cycle 2) was merged to develop → no HF sync was ever attempted.
- PR #154's 5 CI checks (Analyze, Backend Tests, Frontend Build, CodeQL, TruffleHog) all PASSED — no HF sync step exists in the develop pipeline.
- DEC-070 explicitly freezes HF Space production; sync is intentionally not part of develop PR CI.

**Conclusion:** The hand-off's T4 was based on inaccurate info. The HF sync mechanism (`build-and-deploy-hf.yml` → main → Docker → HF Space) is sound and not impacted by cycle 2/3 work. **No fix needed for T4.**

If Anas wants HF sync to also run on develop (e.g. for staging previews), that's a DEC-072 territory decision. Flagging for the next cycle if desired.

---

## 4. Verification

| Check | Result |
|---|---|
| `dotnet build Tests/ERPSystem.Tests/ERPSystem.Tests.csproj` | ✅ 0 errors, 12 pre-existing warnings |
| Pre-commit hook on this commit | ✅ Ran, allowed (clean) |
| Git push to `feature/cycle-3-ci-hardening` | ✅ Pushed (commits 9060682 + 8439d83) |
| PR #155 created | ✅ Open, MERGEABLE, 4 files +352/-5 |
| CI checks started | ✅ 5/5 queued (no Playwright per DEC-070) |

---

## 5. Deviations from Hand-off

1. **T1: Tests already exist in cycle 2.** Documented above. No new test work needed.
2. **T2: Hook is bash, not PowerShell.** The original PowerShell version failed at the `#!/usr/bin/env pwsh` shebang on Git Bash (no `pwsh` binary). Rewrote as POSIX bash — more portable, no dependency. Updated CONTRIBUTING.md to match.
3. **T4: No fix applied.** Investigation showed the issue described in the hand-off doesn't apply to develop PRs. Documented in §3 above. If Anas wants the fix, it's a DEC-072 decision.

---

## 6. Open Questions for Siti / Anas

1. **T4: Should HF sync run on develop?** Currently it doesn't. If yes, that's a DEC-072 (decide if we want staging previews from develop).
2. **TruffleHog install recommendation**: The hook works without TruffleHog (deny-pattern check only) but the deep scan requires it. Should we document an install step in the README? (Currently in CONTRIBUTING.md.)
3. **Concurrency in xunit**: I set `maxParallelThreads: -1` (use all cores). For integration tests sharing a Postgres, this could cause connection-pool pressure. Should we set a cap (e.g., 4)?

---

## 7. Sign-off

- [x] All 4 hand-off tasks addressed (T1-T4)
- [x] Verification: dotnet build 0 errors
- [x] Pre-commit hook tested on this commit
- [x] PR #155 open, MERGEABLE
- [x] Network failure case from cycle 1 captured in protocol (smart cron proposal still pending)
- [ ] CI green (5 checks running)
- [ ] Self-merged per DEC-070 (cron `check-pr-155-ci` is monitoring)

**Status: READY FOR SELF-MERGE WHEN CI GREEN**

---

_Sign-off by Mavis (Anas's local team) — 2026-07-27, end of cycle 3 (6.5 CI/Hardening)._
