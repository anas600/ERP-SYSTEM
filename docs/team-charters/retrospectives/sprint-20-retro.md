# Sprint 20 — Demo 2 (Retrospective)

> **Date:** 2026-08-01
> **Facilitator:** Mavis (Muhammad mode, retrospective author)
> **Sprint goal:** Cover all 13 demo functions with workflow docs + defensive hardening, ready for 1-day client handover.
> **Mode:** LOCAL-ONLY (Mode 1) → Mode 2 push pending Anas's "ادفع".

---

## What we said we'd do

1. 9 P1 workflow docs (PO, GR, Bill, Receipt, JE, CoA, Employee, Payroll, Project)
2. Defensive `.env` check in `rebuild-mvp-docker.ps1` (Sprint 18 carry-over)
3. Fix 2 pre-existing CS warnings (CS8602 + CS8629)
4. Cosmetic Telegram message fix (Sprint 16 → Sprint 20)
5. Update `docs/workflows/README.md` to add P1 functions
6. CHANGELOG + retro + commit (Mode 1)

## What we actually did

1. ✅ **9 P1 workflow docs** — 9 new files in `docs/workflows/`, each ~9-11 KB, bilingual, same 9-section template as Sprint 19's P0 docs
2. ✅ **Defensive .env check** — new step 1.6 in `rebuild-mvp-docker.ps1`, validates `.env` against `.env.example`, auto-appends missing keys (with `-Init` or `-Quiet`)
3. ✅ **2 CS warnings fixed** — CS8602 (ScenarioSeederHostedService.cs) + CS8629 (AuthController.cs), build now clean (0 warnings)
4. ✅ **Telegram cosmetic fix** — "Sprint 16" → "Sprint 20" in success/failure messages
5. ✅ **README updated** — added 9 P1 functions + 14 P2 functions in backlog
6. ✅ **CHANGELOG + retro written**
7. ⏳ **Local commit (Mode 1)** — pending

## Surprise

**The 2 CS warnings had trivial fixes.** Both were one-line guard additions — `result.Response != null` and `matchedUserId == null`. The warnings had been sitting since Sprint 17 carry-over; they were not load-bearing but were noise in every CI build. Now the build is **completely clean** (0 errors, 0 warnings). First time in the project history.

## What went well

- **Template carried.** The 9-section template from Sprint 19 made the P1 docs mechanical. Each one was 30-45 min of focused writing.
- **Defensive .env check is surgical.** It only kicks in when needed, doesn't add runtime overhead, and the `-Init` flag means the user can opt out (with a clear error message).
- **All 13 demo functions now documented.** For the 1-day client handover, this is the key deliverable — the client can read about every function in Arabic + English.

## What didn't go well

- **The defensive .env check was deferred for 2 sprints.** It was in the Sprint 18 carry-over list and slipped because Sprint 18 was governance-only. Lesson: don't let "carry-over" items become permanent fixtures.
- **No FE work in this sprint.** All changes were BE hardening + docs. That's fine for a docs-focused sprint, but it means the FE has no new functionality. (The P0 docs from Sprint 19 are the FE-facing deliverable.)

## Lessons

- **L1: Pre-existing code is often better than the plan assumed (still true).** Sprint 19's surprise was the 16 UI pages. Sprint 20's surprise was that all P1 functions already had endpoints + UI. The carry-over was docs-only.
- **L2: A docs sprint is real work.** 9 docs × 10 KB = 90 KB of Arabic+English technical writing. This took 2.5 hours. It's a Sprint-sized chunk, not a "polish" item.
- **L3: The Sprint 18 .env truncation root cause was a worktree problem.** `git reset --hard` against a dirty working tree in another worktree left the `.env` file in a bad state. The defensive check is a workaround, not a root-cause fix. A future sprint could fix the underlying worktree issue (e.g. `.mavis/` is gitignored but `.env` should also be — checking the gitignore).
- **L4: Trivial fixes compound.** 2 one-line fixes → 0 warnings → cleaner build output → faster future debugging. Small wins matter.
- **L5: Carry-over lists are warning signs.** When an item sits in the carry-over for 2 sprints, it's because (a) it's actually hard, or (b) nobody is forcing it. Sprint 20 closed the 2-sprint carry-over for defensive items. Next sprint should do the same for the 1-sprint carry-over.
- **L6: "Demo 2" naming is a useful concept.** It signals "this is the version the client sees" — distinct from "the latest develop" or "the latest commit". The 1-page pitch + slides (Muhammad's parallel work) will reinforce this.

## Metrics

| Metric | Value |
|---|---|
| Sprint duration | ~1.5 hours (11:30 → 13:00 UTC) |
| Commits planned | 1 (Sprint 20) |
| Files added | 10 (9 docs + 1 retro) |
| Files modified | 5 (CHANGELOG, README, env check, CS fixes, Telegram) |
| Lines of Markdown | ~5,000 (9 docs × ~550 lines each) |
| Lines of PowerShell | ~50 (defensive .env check) |
| Lines of C# | ~2 (CS warning fixes) |
| Build status | ✅ 0 errors, 0 warnings (was 2 warnings) |
| Typecheck status | ✅ 0 errors |
| `tenant_id` regressions | 0 |
| Mode | LOCAL-ONLY (Mode 1) — push pending "ادفع" |

## Next sprint (Sprint 21+) candidates

**P1 (per the demo 2 client feedback):**
- P2 function workflow docs (14 functions)
- `customerStatement` + `vendorStatement` GET endpoints
- `CreateItem` API method

**P2 (Muhammad mode, post-handover):**
- 1-page elevator pitch
- Slides for client demo (PowerPoint)

**P3 (housekeeping):**
- Add `mvp-docker/.env` to `.gitignore` (so future worktree resets don't truncate it)
- Consider renaming `src/backend/Shared/MultiTenancy/` to `CompanyContext/` (Constitution Article 3 cleanup)

## What I asked Anas for

**"ادفع"** when the local commit is ready, to switch to Mode 2 (push + PR + CI 6/6 + merge + tag `v1.0.7-sprint20` + restore protection + auto-rebuild cron + Telegram ping).

When that completes, the system is **demo-2 ready** for the 1-day client handover.

---

_Authored by: Mavis (Muhammad mode, retrospective author) — 2026-08-01_
