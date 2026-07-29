# 📜 Mavis Coordination Constitution

> **The 6 articles that govern the coordination workflow between Admin Team and Mavis Local.**

**Last amended:** 2026-07-29 (Initial)
**Status:** 🟢 ACTIVE
**Amendment requires:** Anas (Project Owner) approval

---

## 🎯 Article 1 — Purpose

The Mavis Coordination Workflow exists to:

1. **Eliminate ping-pong** messaging between Admin Team (Mavis Cloud) and Mavis Local
2. **Provide a single source of truth** for "where the ball is" at any moment
3. **Enable smart async** coordination without constant interruptions
4. **Be token-safe** — if a token expires, the state file shows the last known position
5. **Enable audit trail** — every state change is a git commit

**Mechanism:** A `state.json` file (committed to develop) + a smart GitHub Action cron (silent on no-change).

**Non-goals:**
- ❌ This is NOT a chat system (use Telegram for that)
- ❌ This is NOT a build system (use existing CI)
- ❌ This is NOT a deployment system (per Article 10, production is FROZEN)

---

## 🔄 Article 2 — Ball Locations (State Machine)

The `ball_location` field has 5 possible values:

| State | Meaning | Responsibility |
|-------|---------|----------------|
| **`mavis-local`** | Mavis Local is the active executor | Mavis Local works, opens PR, sets ball to `mavis-cloud` |
| **`mavis-cloud`** | Admin Team is reviewing/merging | Mavis (Siti/Dev) reviews PR, merges, sets ball to `mavis-local` |
| **`anas`** | Waiting for Anas's decision | Anas responds, sets ball to `mavis-local` or `mavis-cloud` |
| **`waiting`** | Async operation in progress (CI, build, external API) | Cron monitors, sets ball when ready |
| **`blocked`** | Problem detected (token expired, CI failed, etc.) | Team investigates, resolves, sets ball to `mavis-local` or `anas` |

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
| **Cron (automated)** | ✅ (any state) | ✅ (any field) |
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
      "type": "presence | question | approval",
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
4. **Commit** on develop with `docs(coordination): amend Article N — <reason>`.
5. **Notify** Mavis Local (the next time they read the state).

**No silent amendments. No retroactive changes.**

---

## 📞 Article 8 — Communication Channels

| Channel | Used for |
|---------|----------|
| **state.json** | Async coordination (PRIMARY) |
| **Telegram** | Urgent issues, Anas's questions |
| **Channel 5** (in-session) | Direct agent-to-agent |
| **PR comments** | Code review discussions |
| **GitHub Issues** | Long-term tracking (out of scope for this workflow) |

**Default channel = state.json.** Everything else is for exceptions.

---

_Last amended: 2026-07-29 by سيتی + محمد, approved by Anas — Initial Constitution_
