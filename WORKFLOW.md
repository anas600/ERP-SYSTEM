# 📜 WORKFLOW — Mavis Coordination Constitution (Active)

> **The single source of truth for the work-coordination workflow.**
> This file is the **active** constitution for the 2-day window (until 2026-07-31 18:25 UTC).
> After that, the legacy `CONSTITUTION.md` (currently PAUSED) resumes as primary.

**Last amended:** 2026-07-31 03:35 UTC (Admin Team v1.8 — محمد, approved by Anas)
- v1.8.3: Added Article 9 (3-Layer Deploy Model) + Article 5 update (Event-Driven Crons preferred)
- v1.8.2: Role separation (Admin = planner, Local = executor)
- v1.8.1: Auto-escalation (R1+R2+R3 from lessons-learned-sync-issues.md)
- v1.8.0: Initial WORKFLOW.md promotion to project root
**Status:** 🟢 **ACTIVE — TEMPORARY PERMANENT** (supersedes `CONSTITUTION.md` for 2 days)
**Owner:** Anas (Project Owner) — amendments require his approval
**Implementer:** Mavis Local (Tech Lead + Coordinator) for the 2-day window
**Previous location:** `.github/workflows/mavis-coordination/constitution.md` (kept for the cron path; this file at root is the canonical source)

---

## 🗂️ Related files

| Purpose | Path |
|---------|------|
| **Sprint hand-offs (T0–Tn)** | [`docs/workflow/sprint-N.md`](./docs/workflow/) |
| **Architecture + 10 soft rules** | [`docs/workflow/architecture.md`](./docs/workflow/architecture.md) |
| **Demo roadmap** | [`docs/workflow/demo-roadmap.md`](./docs/workflow/demo-roadmap.md) |
| **State machine (the ping-pong point)** | [`.github/workflows/mavis-coordination/state.json`](./.github/workflows/mavis-coordination/state.json) |
| **State cron (GitHub Action)** | [`.github/workflows/mavis-coordination/state-cron.yml`](./.github/workflows/mavis-coordination/state-cron.yml) |
| **State schema** | [`.github/workflows/mavis-coordination/state-schema.json`](./.github/workflows/mavis-coordination/state-schema.json) |
| **Paused legacy constitution** | [`CONSTITUTION.md`](./CONSTITUTION.md) (restored 2026-07-31 18:25 UTC) |
| **DOX root (binding contracts)** | [`AGENTS.md`](./AGENTS.md) |
| **Worker instructions (Jimis)** | [`.mavis/AGENTS.md`](./.mavis/AGENTS.md) |

---

## 🎯 Article 1 — Purpose

The Mavis Coordination Workflow exists to:

1. **Eliminate ping-pong** messaging between Admin Team (Mavis Cloud) and Mavis Local
2. **Provide a single source of truth** for "where the ball is" at any moment
3. **Enable smart async** coordination without constant interruptions
4. **Be token-safe** — if a token expires, the state file shows the last known position
5. **Enable audit trail** — every state change is a git commit
6. **Be visible to all actors** — by living at the project root, the constitution is always in the team's mind

**Mechanism:** A `state.json` file (committed to develop) + a smart GitHub Action cron (silent on no-change).

**Non-goals:**
- ❌ This is NOT a chat system (use Telegram for that)
- ❌ This is NOT a build system (use existing CI)
- ❌ This is NOT a deployment system (legacy CONSTITUTION Article 10 keeps production FROZEN)

**Hand-offs (Sprint scope):**
- ✅ Sprint hand-offs live in `docs/workflow/sprint-N.md` (سيتی writes them, Mavis Local executes)
- ✅ For small tasks in the 2-day window, Mavis Local can decide directly without a hand-off
- ✅ The single ping-pong point for "where is the ball" = `state.json`

---

## 🔄 Article 2 — Ball Locations (State Machine)

The `ball_location` field in `state.json` has **5 possible values**:

| State | Meaning | Responsibility |
|-------|---------|----------------|
| **`mavis-local`** | Mavis Local is the active executor | Mavis Local works, opens PR, sets ball to `mavis-cloud` |
| **`mavis-cloud`** | Admin Team is reviewing/merging | Mavis (Siti/Dev) reviews PR, merges, sets ball to `mavis-local` |
| **`anas`** | Waiting for Anas's decision | Anas responds, sets ball to `mavis-local` or `mavis-cloud` |
| **`waiting`** | Async operation in progress (CI, build, external API) | Cron monitors, sets ball when ready |
| **`blocked`** | Problem detected (token expired, CI failed, etc.) | Team investigates, resolves, sets ball to `mavis-local` or `anas` |

> **🚨 Critical (per Anas, 2026-07-29 18:50 UTC):**
> The ball is in the **ACTOR's** court (mavis-local / mavis-cloud / anas), **NOT** the cron's.
> The cron is a **tool** that helps Mavis Local stay updated — it does not "own" the ball.

**State transitions (valid):**

```
mavis-local → mavis-cloud      (Mavis Local opens PR)
mavis-cloud → mavis-local      (Mavis Cloud merges PR)
mavis-cloud → anas             (Mavis Cloud needs Anas's decision)
anas → mavis-local             (Anas decides: continue)
anas → mavis-cloud             (Anas decides: review needed)
waiting → mavis-local          (async op complete)
waiting → mavis-cloud          (async op complete + needs review)
blocked → mavis-local          (issue resolved by Mavis Local)
blocked → mavis-cloud          (issue resolved by Mavis Cloud)
blocked → anas                 (issue needs Anas's intervention)
```

**Invalid transitions:** Anything not listed above.

---

## 📝 Article 3 — Update Rules

### Who can update the state:

| Actor | Can update `ball_location`? | Can update other fields? |
|-------|-----------------------------|--------------------------|
| **Mavis Local** | ✅ (`→ mavis-cloud` after PR) | ✅ (any field) |
| **Mavis Cloud (Siti/Dev)** | ✅ (`→ mavis-local` after merge) | ✅ (any field) |
| **Cron (automated)** | ✅ (any state, **per cron logic**, not as an actor) | ✅ (any field) |
| **Anas (Owner)** | ✅ (any state, override) | ✅ (any field, override) |
| **Muhammad (Strategic)** | ⚠️ read-only (no updates) | ⚠️ read-only |

### When to update:

- **Mavis Local opens a PR** → update `ball_location = "mavis-cloud"` and `next_action`
- **Mavis Cloud merges a PR** → update `ball_location = "mavis-local"` (or "waiting" if CI pending)
- **Anas asks a question** → update `ball_location = "anas"` and `next_action`
- **CI fails** → update `issues[]` and `ball_location = "blocked"`
- **Token expired** → update `issues[]` and `ball_location = "blocked"`
- **Sprint starts** → update `active_sprint`, `sprint_started_at`, `sprint_eta`
- **Sprint ends** → update `active_sprint = null` or next sprint number
- **Cron tick with no change** → bump `last_updated`, but **DO NOT** move the ball (cron is not an actor)

### How to update:

```bash
# 1. Read current state
cat .github/workflows/mavis-coordination/state.json

# 2. Edit (use jq, sed, or editor)
# Example with jq:
jq '.ball_location = "mavis-local" | .last_updated = "2026-07-29T17:30:00Z"' \
  .github/workflows/mavis-coordination/state.json > state.new.json
mv state.new.json .github/workflows/mavis-coordination/state.json

# 3. Commit
git add .github/workflows/mavis-coordination/state.json
git commit -m "coordination: ball → mavis-local (PR #172 merged)"
git push
```

### Conflict resolution:

- **Last write wins** (the cron is the most frequent writer)
- **Mavis Local** should not commit if cron is updating (will cause conflicts)
- **Solution:** Always `git pull --rebase` before manual updates

---

## 📊 Article 4 — State Schema

The `state.json` file MUST follow this schema (see `state-schema.json` for JSON Schema):

```json
{
  "version": "1.0",
  "last_updated": "ISO-8601 timestamp (UTC)",
  "ball_location": "mavis-local | mavis-cloud | anas | waiting | blocked",
  "ball_owner_session": "session-id of the current actor",
  "active_sprint": "5 or null",
  "open_prs": [
    {
      "number": 172,
      "title": "Short PR title",
      "branch": "feature/sprint-5-demo-v2",
      "state": "open",
      "opened_by": "mavis-local | mavis-cloud | mephisto",
      "opened_at": "ISO-8601",
      "ci_status": "passing | failing | pending | null"
    }
  ],
  "pending_signals": [
    {
      "from": "session-id",
      "type": "presence | question | approval | directive",
      "message": "Free text",
      "created_at": "ISO-8601"
    }
  ],
  "issues": [
    {
      "type": "token_expired | ci_failed | merge_conflict | unclear_requirements",
      "message": "Description of the issue",
      "detected_at": "ISO-8601",
      "fix_instructions": "How to resolve"
    }
  ],
  "next_action": "Free text describing what should happen next",
  "last_communication": "ISO-8601 timestamp of last Telegram/Channel-5 message",
  "sprint_started_at": "ISO-8601 or null",
  "sprint_eta": "ISO-8601 or null"
}
```

**Required fields:** `version`, `last_updated`, `ball_location`, `next_action`

**Optional but recommended:** All others.

---

## 🤖 Article 5 — Smart Cron Behavior

The cron (`state-cron.yml`) is a GitHub Action that:

### Schedule

- **Every 5 minutes** during active hours
- **Every 15 minutes** during idle hours (configurable)
- **Disabled** during sprint closures (to avoid noise)

### Read

The cron reads:

1. **Open PRs** via GitHub API (`/repos/.../pulls?state=open`)
2. **Recent commits** via GitHub API (`/repos/.../commits?sha=develop&per_page=5`)
3. **Presence signals** via `docs/governance/presence-signal.json`
4. **CI status** for each open PR
5. **Current state.json** (to detect changes)

### Compute

The cron determines `ball_location` based on:

```
if issues[] is not empty:
    ball_location = "blocked"
elif open_prs > 0 AND last commit was by mavis-local:
    ball_location = "mavis-cloud"
elif last commit was by mavis-cloud (or mavis-cloud cron):
    ball_location = "mavis-local"
elif pending_signals > 0 AND has_anas_question:
    ball_location = "anas"
elif last_communication > 1 hour ago:
    ball_location = "waiting"
else:
    ball_location = "mavis-local"  (default)
```

> **🚨 Note:** Even though the cron *computes* the ball, the ball is still **owned by the actor**.
> The cron writes `ball_location`, but the *responsibility* stays with the actor named in `ball_owner_session`.

### Write (Smart)

- **Only if state changed** (compare new vs old)
- Updates `last_updated` timestamp
- Updates `open_prs[]` with current PR list
- Updates `pending_signals[]` (drains consumed signals)
- Updates `issues[]` (adds new, removes resolved)
- Commits to develop with message: `coordination: state updated @ <timestamp>`

### Post (Optional, Minimal)

- **1 comment on develop HEAD** if state changed
- Comment format: `🤖 State updated: <old> → <new>`
- **Never** posts if state is unchanged

### Silent Mode (Default)

- **No Telegram notifications** (the state file IS the notification)
- **No Slack/Discord** (out of scope)
- **No emails** (out of scope)

### Local Mavis Cron (separate from GitHub Action)

- Lives on the platform's Schedules tab (not in the project repo)
- **Tool only** — helps Mavis Local stay updated, not an actor
- Default schedule: every 5 min during active hours (08:00–22:00, Africa/Tripoli)

### Event-Driven Triggers (PREFERRED — per v1.8.3)

> **🚨 v1.8.3 update:** Time-driven crons are **deprecated** for new work. Use event-driven triggers (webhooks on `pull_request`, `push`, `workflow_dispatch`).

**Why event-driven:**
- ✅ Zero noise (no silent ticks)
- ✅ Real-time response (vs 0-10 min latency for time-driven)
- ✅ Single owner = root (the Mavis Coordinator watchdog) — fewer SPOFs
- ✅ Webhooks 99.9% reliability (with polling fallback every 30 min)

**Migration path:** Old time-driven crons (siti-responder, muhammad-responder, dev-responder, develop-pr-monitor) → replaced by event-driven handlers in `mavis-coordination/state-cron.yml` + `nightly-integration.yml`.

**Fallback strategy:** If webhook fails 3+ times, switch to 30-min polling (silent).

---

## 🚨 Article 6 — Emergency Protocols

### Token Expiry

If the cron detects a 401 from GitHub API:

```json
{
  "ball_location": "blocked",
  "issues": [{
    "type": "token_expired",
    "message": "GitHub token expired or revoked",
    "detected_at": "ISO-8601",
    "fix_instructions": "Regenerate token in GitHub Settings → Developer settings → Personal access tokens. Update the workflow secret."
  }]
}
```

**Who fixes:** Whoever owns the token (Mavis Local or Mavis Cloud).

### CI Failure

If a PR's CI fails:

```json
{
  "ball_location": "blocked",
  "open_prs": [{
    "number": 172,
    "ci_status": "failing"
  }],
  "issues": [{
    "type": "ci_failed",
    "message": "PR #172 has failing CI checks",
    "detected_at": "ISO-8601",
    "fix_instructions": "Check the Actions tab, fix the failing checks, push again"
  }]
}
```

**Who fixes:** Mavis Local (the PR author).

### Merge Conflicts

If a PR has merge conflicts:

```json
{
  "ball_location": "blocked",
  "issues": [{
    "type": "merge_conflict",
    "message": "PR #172 has merge conflicts with develop",
    "detected_at": "ISO-8601",
    "fix_instructions": "Rebase on develop: git fetch && git rebase origin/develop"
  }]
}
```

**Who fixes:** Mavis Local.

### Idle > 30 minutes

If no activity for 30+ minutes during an active sprint:

```json
{
  "ball_location": "waiting",
  "next_action": "Cron detected no activity for 30+ min. Check if Mavis Local is blocked or idle."
}
```

**Who investigates:** Mavis Cloud (Siti).

### Anas Override

Anas can **always** override any state by manually editing `state.json` on GitHub. The cron will respect the override and not change `ball_location` until the next legitimate transition.

---

## ✏️ Article 7 — Amendment Process

1. **Proposal** by Mavis (any mode) or Anas, with rationale.
2. **Review** by Anas (Project Owner) — explicit approval required.
3. **Update** this file with `[Amended YYYY-MM-DD: <reason>]` in the relevant article.
4. **Commit** on develop with `docs(workflow): amend Article N — <reason>`.
5. **Notify** Mavis Local (the next time they read the state).

**No silent amendments. No retroactive changes.**

---

## 📞 Article 8 — Communication Channels

| Channel | Used for |
|---------|----------|
| **`state.json`** | Async coordination (PRIMARY) |
| **`WORKFLOW.md` (this file)** | Governance — always in mind, at project root |
| **`docs/workflow/sprint-N.md`** | Sprint hand-offs (Siti writes → Mavis Local executes) |
| **`AGENTS.md` + child AGENTS.md** | DOX contracts (binding) |
| **`.mavis/AGENTS.md`** | Worker (Jimi) instructions |
| **`CHANGELOG.md`** | Per-sprint record |
| **Telegram** | Urgent issues, Anas's questions |
| **Channel 5** (in-session) | Direct agent-to-agent |
| **PR comments** | Code review discussions |
| **GitHub Issues** | Long-term tracking (out of scope for this workflow) |

**Default channel = state.json + WORKFLOW.md.** Everything else is for exceptions.

---

## 🏗️ Article 9 — 3-Layer Deploy Model (per v1.8.3)

> **🚨 Added 2026-07-31 per Admin Team v1.8 proposal, approved by Anas.** Replaces the previous single-layer model (where `ci-deploy.yml` auto-deployed on every develop push — anti-pattern).

### The 3 Layers

```
┌─────────────────────────────────────────────────────────────┐
│ Layer 1: DEVELOPMENT (develop branch)                       │
│ - CI: ci-fast.yml (PR + push)                                │
│ - NO deploy (code integration + verification only)          │
│ - Tests: unit + lint + architecture-compliance (~3-5 min)   │
│ - Owner: Local Team (Mavis Local)                            │
├─────────────────────────────────────────────────────────────┤
│ Layer 2: STAGING (nightly + manual)                          │
│ - CI: nightly-integration.yml (cron 0 2 * * *)             │
│ - Full integration tests (Postgres + Redis)                  │
│ - Includes Retention + Edge categories (32 tests)           │
│ - Owner: Nightly cron (auto), Local Team (manual)            │
├─────────────────────────────────────────────────────────────┤
│ Layer 3: PRODUCTION (main branch, manual approval)           │
│ - CI: ci-deploy-prod.yml                                     │
│ - Manual approval via GitHub environment `production`        │
│ - Full test suite + HF Space deploy + auto-rollback          │
│ - Owner: Anas (approval) + Admin Team (merge flow)          │
└─────────────────────────────────────────────────────────────┘
```

### Mapping (branch → layer → CI workflow)

| Branch | Layer | CI Workflow | Deploy? |
|--------|-------|--------------|---------|
| `develop` | 1 (Development) | `ci-fast.yml` | ❌ NO |
| `develop` | 2 (Staging, nightly) | `nightly-integration.yml` | ❌ NO |
| `main` | 1 (Development, also runs ci-fast) | `ci-fast.yml` | ❌ NO |
| `main` | 3 (Production, manual) | `ci-deploy-prod.yml` | ✅ YES (after approval) |

### Why 3 layers (vs 1)

**Before (anti-pattern):**
- `ci-deploy.yml` triggered on every `push to develop` → auto-deploy to HF Space (production)
- No manual approval
- No separation between "code integration" and "production release"
- Result: noisy, risky, hard to roll back

**After (3-layer model):**
- ✅ Develop = code integration only (no deploy)
- ✅ Nightly = full integration tests in isolation
- ✅ Main = production deploy with manual approval
- ✅ Auto-rollback if health check fails (DEC-051)
- ✅ Faster CI feedback (develop = unit tests only, fast)
- ✅ Safer production (manual gate + health check)

### When to use each layer

- **Layer 1 (develop + ci-fast):** every PR, every push. Fast feedback. ~3-5 min.
- **Layer 2 (nightly):** every night at 2 AM UTC. Full integration. ~15-30 min.
- **Layer 3 (main + ci-deploy-prod):** when ready to release. Manual approval + auto-rollback.

### Files

| File | Layer | Status |
|------|-------|--------|
| `.github/workflows/ci-fast.yml` | 1 | Active (v1.8.3 refined: architecture-compliance + test filter) |
| `.github/workflows/nightly-integration.yml` | 2 | Active (v1.8.3 new) |
| `.github/workflows/ci-deploy-prod.yml` | 3 | Active (v1.8.3 new, replaces ci-deploy.yml) |
| `.github/workflows/ci-deploy.yml` | — | ⚠️ **DEPRECATED** — should be deleted in follow-up PR |

---

## 🏛️ Leadership Cycles (Mavis Local's role as coordinator)

Per Anas (2026-07-29 19:13 UTC), Mavis Local is the **coordinator between**:

- **Admin Team** (سيتی + محمد + ديف) — Cloud, work as Cron Jobs via `state.json`
- **Local Workers** (Jimis: BE + FE in parallel) — Local, spawned by Mavis Local for each Sprint

**Cycle per Sprint:**

1. **Sprint hand-off arrives** at `docs/workflow/sprint-N.md` (from سيتی, Cloud)
   - OR: Mavis Local self-plans (if small task + ball is in mavis-local court)
2. **T0 inventory** — Mavis Local checks what's already there (avoid re-work)
3. **Spawn 2 Jimis in parallel** — BE + FE (see `.mavis/AGENTS.md`)
4. **Jimis execute** — they document in their module's AGENTS.md + CHANGELOG.md
5. **Mavis Local verifies** (T6) — build + test + typecheck
6. **Mavis Local opens PR** (`feature/sprint-N-*` → develop)
7. **Mavis Local self-merges** (per DEC-070 admin)
8. **Mavis Local updates state.json** (`ball_location = "mavis-local"`, drain pending_signals)
9. **Next sprint** (or wait for new directive if sprint is the final one)

**Each Jimi MUST:**
- Read `.mavis/AGENTS.md` before starting
- Document their scope in the nearest applicable AGENTS.md (per DOX)
- Add a CHANGELOG entry for their slice of work
- Follow the architecture constraints in `docs/workflow/architecture.md`

---

_Originally authored: 2026-07-29 by سيتی + محمد, approved by Anas_
_Promoted to project root: 2026-07-29 19:15 UTC by Mavis Local, per Anas directive (always-in-mind governance)_
