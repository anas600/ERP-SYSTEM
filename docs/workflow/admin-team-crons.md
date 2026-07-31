# 🛠️ Admin Team Internal Crons (Per Anas 2026-07-31 05:42 UTC)

> **Author:** Mavis (سيتی mode) — Cloud Coordinator
> **Per:** Anas directive — Admin Team writes internal crons for review + merge
> **Status:** 🟡 DESIGN (awaiting platform Schedule UI setup)

---

## 🎯 Why

Anas's directive: "Admin Team has a task — write internal crons for review + merge." Always merge to `develop` (3-Layer Model Layer 1). PRs accumulate while develop-pr-monitor is paused.

**Per the v2.0 governance + WORKFLOW.md Article 2:** Crons are TOOLS, not ACTORS. They live on the platform's Schedule tab, not in the project repo. They help the team stay updated, not own the ball.

**Note:** The `mavis` CLI is currently broken (per lessons-learned-sync-issues.md: `resources\resources\daemon\cli.js missing`). These crons are designed to be set up via the platform's Schedule UI, not via CLI.

---

## 📋 The 4 Internal Crons

### Cron 1: `sprint-pr-review`

**Purpose:** Wake up سيتی to review new PRs on develop.

| Field | Value |
|-------|-------|
| **Name** | `sprint-pr-review` |
| **Schedule** | `*/15 * * * *` (every 15 min) |
| **Active hours** | 08:00-22:00 Africa/Tripoli |
| **Session** | `mvs_a1a821a951504cce80ee1fddb98053be` (Admin Team v1.8) |
| **Prompt** | `Sprint PR review check. Run: gh pr list --base develop --json number,title,headRefName,createdAt,author. For each new PR (created in last 15 min), review per Template 1 v2: (1) Read PR description + diff stat, (2) Run T6 verify if not done, (3) Check no `tenant_id`, no EF Core, no secrets, (4) Post review comment. If green AND no concerns, merge: gh pr merge <num> --squash --admin --delete-branch. If concerns, post review + ping Local Team via session message. Report findings.` |

**When it fires:** every 15 min during active hours.

**What it does:** Wakes سيتی to do the review+merge work that previously needed human attention.

---

### Cron 2: `state-cron`

**Purpose:** Update `state.json` (the ping-pong point) every 5 min.

| Field | Value |
|-------|-------|
| **Name** | `state-cron` |
| **Schedule** | `*/5 * * * *` (every 5 min) |
| **Active hours** | 24/7 (silent no-op when nothing changed) |
| **Session** | `mvs_a1a821a951504cce80ee1fddb98053be` (Admin Team v1.8) |
| **Prompt** | `state-cron tick. Read current .github/workflows/mavis-coordination/state.json. If no changes needed (no new PRs, no merge, no escalation), commit a no-op tick: {"last_updated": <now>}. If changes (PR opened, PR merged, escalation triggered), update state.json and commit to develop. Silent on no-change per WORKFLOW.md Article 5.` |

**When it fires:** every 5 min.

**What it does:** Keeps `state.json` fresh for visibility (per Article 5: "cron is a tool, not an actor").

---

### Cron 3: `coordinator-watchdog`

**Purpose:** Auto-escalate if actors are stuck > 60 min (per v1.8.1 R1+R2+R3).

| Field | Value |
|-------|-------|
| **Name** | `coordinator-watchdog` |
| **Schedule** | `*/10 * * * *` (every 10 min) |
| **Active hours** | 24/7 |
| **Session** | `mvs_4d7d32af36994449a90f0103f38f341f` (Mavis Coordinator) |
| **Prompt** | `coordinator-watchdog tick. Read state.json. Check stalled_actors{}: if any actor stuck > 60 min, ping that actor's session. Check jimi_status[]: if any Jimi status=failed for > 30 min, ping the actor + escalate to Anas via Telegram (if critical). Otherwise silent.` |

**When it fires:** every 10 min.

**What it does:** Auto-escalation per the v1.8.1 R3 rule (60-min threshold). Prevents deadlocks.

---

### Cron 4: `sprint-archive-cleanup`

**Purpose:** Clean up merged feature branches + close stale PRs nightly.

| Field | Value |
|-------|-------|
| **Name** | `sprint-archive-cleanup` |
| **Schedule** | `0 2 * * *` (2 AM UTC daily) |
| **Active hours** | 24/7 |
| **Session** | `mvs_a1a821a951504cce80ee1fddb98053be` (Admin Team v1.8) |
| **Prompt** | `Nightly cleanup. Run: (1) git fetch origin, (2) gh pr list --state closed --json number,headRefName --jq '.[] \| select(.headRefName \| startswith("feature/")) \| .headRefName' to find merged feature branches still in local refs, (3) git branch -d <merged> for each. Skip active branches (where sprint is ongoing). Report deletions.` |

**When it fires:** 2 AM UTC daily.

**What it does:** Keeps the branch namespace clean (per AGENTS.md "delete stale notes instead of explaining history").

---

## 🔧 Setup Instructions (Platform Schedule UI)

Since `mavis` CLI is broken, these crons are set up via the platform's Schedule UI:

1. Open the platform's Schedules tab
2. Click "New Cron"
3. For each cron above, fill the fields
4. Test by clicking "Run Now" (verify the prompt fires correctly)
5. Enable the schedule

**Alternative (until CLI is fixed):** use the platform's Schedule UI directly. The schedules persist in the platform's config, not in the project repo (per the v2.0 governance + WORKFLOW.md Article 5).

---

## 🚦 Migration Plan

| Step | Action | Owner | Status |
|------|--------|-------|--------|
| 1 | Document the 4 crons (this file) | سيتی (me) | ✅ Done |
| 2 | Set up the 4 crons in platform Schedule UI | Admin Team v1.8 | 🟡 In progress (1/4 done via MCP) |
| 3 | Test each cron (run-now + verify output) | Admin Team v1.8 | ⏳ Pending |
| 4 | Enable the schedules | Admin Team v1.8 | 🟡 1 enabled (`sprint-pr-review-v1.8`) |
| 5 | Monitor for 1 week, then adjust schedules | Coordinator + Admin | ⏳ Pending |

## ✅ Crons Already Created (per Anas 2026-07-31 05:42 UTC directive — full authority granted)

| Cron | Schedule | Status | Cron ID |
|------|----------|--------|---------|
| `sprint-pr-review-v1.8` | `*/15 * * * *` (08:00-22:00 Africa/Tripoli) | ✅ ENABLED | `28e88987-10d6-43c6-abad-654e68a867d5` |
| `state-cron` | `*/5 * * * *` | ⏳ Pending (next) | — |
| `coordinator-watchdog` | `*/10 * * * *` | ⏳ Pending | — |
| `sprint-archive-cleanup` | `0 2 * * *` | ⏳ Pending | — |

**Note on `sprint-pr-review-v1.8`:** Runs every 15 min, checks for new PRs on develop, reviews them, merges if green (squash, --admin, --delete-branch). Per the v2.0 governance, Mavis Local can self-merge with --admin when needed.

---

## 🎯 Success Criteria

- ✅ `sprint-pr-review` fires every 15 min, no missed PRs
- ✅ `state-cron` keeps `state.json` fresh (last_updated within 10 min)
- ✅ `coordinator-watchdog` catches stuck actors within 60 min
- ✅ `sprint-archive-cleanup` deletes merged branches nightly
- ✅ Zero `ball_location: mavis-local` for > 60 min without progress

---

_Author: Mavis (سيتی mode) — Cloud Coordinator_
_Date: 2026-07-31_
_Status: 🟡 DESIGN — awaiting platform Schedule UI setup_
_Refs: WORKFLOW.md Article 5 (event-driven crons), v1.8.1 R1+R2+R3 (auto-escalation)_
