# 🧠 Lessons Learned — Mavis Local's Perspective (Cycles 0-3)

> **Purpose:** Consolidate the hard-won experience from the first 3 work cycles
> (Cycles 0-3) so that future cycles run smoother and Siti can write more
> accurate hand-offs.
>
> **Source:** Real experience in 3-Tier & Dual-Agent model, written by Mavis
> Local (Tech Lead, Windows side) for Siti (Cloud Coordinator).
>
> **Cycle:** 4 / 20 — governance improvement sprint.

---

## 🎯 What this document is

This is **not** a generic postmortem. It captures concrete patterns that worked,
patterns that didn't, and the specific situations where the hand-off ↔ work loop
broke down. Future cycles should reference this when designing hand-offs.

---

## ✅ What worked

### 1. The 3-Tier & Dual-Agent model (DEC-070 / DEC-068)

- **Tier 1 (local, this machine):** Can do fast work — code, tests, docs,
  commits, PRs. No async dependencies.
- **Tier 2 (cloud, handoff coordination):** Siti does governance, crons,
  async ops. Can wait for her; she doesn't block local work.
- **Tier 3 (production/HF Space):** Frozen per DEC-070. Don't touch without
  explicit Anas approval.

**Lesson:** When the model is clear, Mavis Local doesn't wait for Siti to
acknowledge a hand-off — she does the work, opens the PR, sets a cron to
self-merge when CI is green. Siti reviews the merged PR when she comes back.
This keeps momentum.

### 2. The cron self-reminder pattern

Pattern: `mavis cron self` → "check CI status, self-merge when green, delete me."

- Cycle 1: `check-pr-153-ci` (cronId `232b6706-...`) — used, self-deleted on merge.
- Cycle 2: `check-pr-154-ci` (cronId `9c75738e-...`) — used, self-deleted.
- Cycle 3: `check-pr-155-ci` (cronId `8b716b6f-...`) — used, self-deleted.

**Lesson:** Works well when CI runs reliably. The cron tick should:
1. Check the PR state
2. If all green → `gh pr merge <N> --squash --delete-branch --admin` (per DEC-070)
3. Delete itself
4. If still running → wrap status in `<mavis-progress>` and exit (per gate discipline)

### 3. The smart rebase pattern (git reset + cherry-pick)

When a feature branch has commits that ALREADY EXIST on the target (e.g., via
merged PRs), the `git rebase --skip` loop is tedious.

**Cleaner pattern:**
```bash
# Backup tag first
git tag backup/pre-rebase-feature-$(date +%Y%m%d-%H%M) <current-HEAD>

# Reset to target
git reset --hard origin/develop

# Cherry-pick only the new commits in chronological order
git cherry-pick <commit-1> <commit-2>

# Force-push safely (--force-with-lease, not plain --force)
git push --force-with-lease origin feature/<branch>
```

**Why `--force-with-lease`:** Checks that the remote hasn't moved since your last
fetch. Safe even on a shared branch.

**Lesson:** Used in cycle 1 (PR #153) to resolve CONFLICTING with develop. Worked
in <5 minutes. Saved the entire cycle 1 from being blocked.

### 4. The self-merge authority (DEC-070)

`gh pr merge <N> --squash --delete-branch --admin` works around the branch
protection wall. The `--admin` flag is REQUIRED even with admin token — without
it you get "the base branch policy prohibits the merge" error.

**Lesson:** DEC-070 + `--admin` flag is the unlock. Without it, even a personal
access token with `repo` + `admin:org` scopes hits the branch protection wall.

### 5. The pre-commit hook (POSIX bash, NOT PowerShell)

**Why bash, not PowerShell:** Cross-platform from day one. `#!/usr/bin/env pwsh`
fails on Git Bash (no `pwsh` binary). Bash is universally available.

**Pattern:**
```bash
# Use grep -E for deny-pattern scan (portable, fast)
# Use `set -euo pipefail` for strict error handling
# Auto-detect TruffleHog in PATH or .venv
# Skip silently if not installed (no false blocks, but warn)
```

**Lesson:** When writing cross-platform tools, default to bash unless there's a
strong reason for PowerShell. Test on both before committing.

---

## ❌ What didn't work (and what to do instead)

### 1. PowerShell -ireplace silent destruction (cycle 1, Mavis's first attempt)

**What happened:** A PowerShell script with `Get-Content | ForEach-Object { $_.Line -ireplace $pattern, $replacement }` silently failed with "operator allows only two elements". The script printed "✓" after a file existence check, masking the failure. 11 module `AGENTS.md` files were wiped (1663 lines deleted, 11 added).

**Root cause:** PowerShell `-ireplace` is picky about arg count, and the "success" check was on existence, not on actual replace success.

**Fix pattern:**
- Use the `edit` tool (Mavis's built-in) with exact `old_string` + `new_string`, one file at a time
- Verify with `Select-String -Pattern "<expected marker>"` after each change
- Never trust batch regex replace without explicit per-file verification

**Lesson for hand-offs:** If a hand-off says "31 test files need refactor", that's a
risk for a tool-induced bug. Better: write tests, run them, fix the 2-3 that
actually fail, rather than blind-rewriting 31 files.

### 2. Cron ticks that spam the user (cycle 1, post-merge)

**What happened:** After PR #153 was CONFLICTING, I set a `check-pr-153` cron
that ran every 5 minutes. Even when the user explicitly said "wait for Siti",
the cron kept pinging.

**Fix:** When user says "wait for X" (and X is a long wait), DELETE the cron,
not throttle it. Throttling = still pinging, just less often.

**Lesson:** Cron throttling is for cases where you want to keep watching. Cron
deletion is for "the user said stop pinging."

### 3. Hand-off inaccuracies (cycles 2-3, the T1 was-already-done case)

**What happened (cycle 3):** The cycle-3 hand-off (T1) asked to add 2 new test
cases that were already in the codebase from cycle 2. The hand-off had been
written BEFORE cycle 2's PR #154 was merged, so the work was already done.

**Lesson for Siti when writing hand-offs:**
- BEFORE writing a hand-off, check `git log origin/develop --oneline` for the
  most recent merged PRs
- If a previous cycle added tests, mention them by file path (e.g.,
  "Tests already in `src/backend/Tests/ERPSystem.Tests/Auth/CompanyContextTests.cs`
  from cycle 2, no new work needed")
- Don't repeat scope across cycles

**Lesson for Mavis Local when receiving hand-offs:**
- ALWAYS do a quick inventory first (T1 of any hand-off)
- Document the inventory result in the response (T1 status: "already done in
  cycle X, see [file]")
- Don't blindly execute T1

### 4. Hand-off T4 inaccuracy (cycle 3, "Sync to HF Space failed in PR #154")

**What happened:** The cycle-3 hand-off T4 said "Sync to HF Space failed in
PR #154". Investigation showed `build-and-deploy-hf.yml` only runs on
`branches: [main]`, NOT on PRs to develop. PR #154 was merged to develop, so
no HF sync was ever attempted.

**Lesson for Siti:** Verify your claims by reading the actual workflow files
before writing hand-offs. If you can't, mark the task as "investigate" not
"fix" — let the executor figure out whether a fix is even needed.

**Lesson for Mavis Local:** Treat T-style hand-off tasks as hypotheses to
verify, not commands to execute. "T4: Fix HF Space sync" became "T4:
Investigation showed no fix needed; documented in §3 of response."

### 5. Branch protection vs admin authority (cycle 1, before DEC-070)

**What happened (cycle 1):** PR #153 was ready to merge (CI green, 5/5 passing),
but `gh pr merge 153 --squash --delete-branch` failed with "the base branch
policy prohibits the merge." The user is an admin on the repo but the API
still required explicit `--admin` flag.

**Fix:** Add `--admin` to all `gh pr merge` calls when using DEC-070 authority.

**Lesson:** GitHub API and CLI have subtle behavior around admin override. When
DEC-070 says "you have full admin", the CLI needs to be told explicitly.

---

## 🔄 Workflow patterns that emerged

### Pattern 1: Hand-off → Inventory → Work → PR → Cron → Self-merge

```
Siti writes hand-off (develop) → Mavis Local reads
  ↓
Mavis Local: T1 inventory (verify scope against develop HEAD)
  ↓
Mavis Local: T2-Tn work (file-by-file Edit tool, no bulk regex)
  ↓
Mavis Local: Verification (tsc + dotnet build + dotnet test locally)
  ↓
Mavis Local: Commit + push + open PR (gh pr create)
  ↓
Mavis Local: Set cron (cron self, every 5m)
  ↓
CI runs (5 checks, ~6 min total)
  ↓
Cron tick: all green → gh pr merge --squash --delete-branch --admin
  ↓
Cron tick: merge succeeded → delete self
  ↓
Siti reviews the merged PR when she comes back (board updated by Mavis Local)
```

### Pattern 2: "Wait for Siti" mode

When user says "wait for Siti", the Mavis Local state is:

```yaml
status: WAITING_FOR_SITI
crons: NONE  (delete all, don't throttle)
ping_policy: silent
self_initiated_work: only Tier 1 (no PRs, no commits)
exception: TRULY_BLOCKED → ping once, then wait
```

### Pattern 3: "Wait for Anas" mode

When user says "wait for me" or is in interactive mode:

```yaml
status: WAITING_FOR_ANAS
crons: may keep ONE (CI monitoring, since CI is async)
ping_policy: minimal — only on state changes
self_initiated_work: only Tier 1 if explicitly asked
exception: TIER_1_PROBLEM → ask once, then wait
```

---

## 📡 Async communication patterns

### Cron naming convention

```
check-pr-<N>-ci    → waits for PR N's CI to be green, then self-merges
monitor-<X>-<Y>    → generic monitoring (e.g. monitor-cycle-3-pr-merge)
```

### Cron self-deleting on success

Every cron I created in cycles 1-3 self-deleted when its goal was achieved
(merge complete, file detected, etc.). This is the correct pattern — never
leave crons running indefinitely.

### Cron tick content (per gate discipline)

```markdown
<mavis-progress>[brief 1-line status, e.g. "PR #155 → CI all green, self-merging"]</mavis-progress>
```

If something needs human attention:

```markdown
[full explanation of the problem + recommended action]
```

### When to NOT set a cron

- User explicitly said "wait for X" (X is a long wait, not CI)
- The state is already terminal (no async)
- The check would have to call a paid API (use the network-failure case pattern instead)

---

## 🛡️ Failure modes the protocol should document

### Failure Mode 1: Network / Cloud Outage (Anas's smart cron proposal)

**Symptom:** No commits, no hand-off responses, no board updates from the
analytical team (Siti) for >N hours.

**Detection (current, cycle 1-3):** Human (Anas) happens to notice the team
screen is offline.

**Detection (proposed, cycle 4+):** Smart cron with token-free health-ping.
See `docs/DEC-072-presence-protocol.md` for the proposed mechanism.

**Workaround:** Continue Tier 1 work locally; document infra failures in
hand-off; defer cloud-dependent work to dedicated sessions.

**Hard limit:** No direct agent-to-agent messaging. All sync via docs + git.

### Failure Mode 2: Hand-off inaccuracy

**Symptom:** Task scope in hand-off doesn't match the actual state (e.g.,
"add tests" but tests already exist; "fix workflow" but workflow doesn't apply
to the branch).

**Detection:** Mavis Local's T1 inventory always catches this before work begins.

**Workaround:** Treat hand-off tasks as hypotheses, not commands. Verify
against `git log origin/develop` first.

### Failure Mode 3: PowerShell -ireplace silent failure (this session, cycle 1)

**Symptom:** A script prints "✓" but the actual replace failed; data loss.

**Detection:** `git status --short` after batch ops, plus `Select-String`
verification of expected markers.

**Workaround:** Never use PowerShell `-ireplace` for multi-line structural
edits. Use the `edit` tool (Mavis's built-in) one file at a time.

---

## 📋 Recommendations for cycle 5+ hand-offs

1. **Check `git log origin/develop` before writing** — see if previous cycles
   already did parts of the proposed scope.
2. **Be specific about test file paths** — if a test already exists, name
   the file. Don't say "add 2 new test cases" if they already exist.
3. **Distinguish "investigate" from "fix"** — for ambiguous tasks, ask
   Mavis Local to investigate first, then propose a fix.
4. **Use the failure modes above as a checklist** — when planning a cycle,
   check if any known failure modes apply.
5. **Update the board after cycle closure** — don't leave the board showing
   the old cycle as ACTIVE. (Cycle 3 closure was delayed by ~1 hour; this
   could have been minutes with a board-update-on-merge pattern.)
6. **Write the cycle-log.md entry as soon as the PR merges** — same as above,
   a "merge → log entry" cron or pattern would help.

---

## 🛰️ Final note

Cycles 0-3 took ~4 hours of work, 3 PRs (153, 154, 155), and 3 self-merges.
The 3-Tier & Dual-Agent model + DEC-070 empowerment made this possible in a
short time. The bottleneck is now coordination latency (waiting for hand-offs
to be accurate, waiting for board updates), not execution speed.

Improving coordination is cycle 4's purpose. Once this cycle's improvements
land, cycles 5-20 should run faster and with fewer surprises.

— Mavis (Anas's local team), 2026-07-28
