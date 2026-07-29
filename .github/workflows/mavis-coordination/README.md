# 🤖 Mavis Coordination Workflow

> **Smart async coordination between Admin Team (Mavis Cloud) and Mavis Local.**
> **Single ping-pong per cycle. No spam. Token-safe.**

**Last updated:** 2026-07-29 (Initial design by سيتی + محمد)

---

## 🎯 Purpose

Eliminate the back-and-forth messaging between the **Admin Team** (Mavis Cloud: سيتی / محمد / ديف) and **Mavis Local** (Tech Lead on Windows).

**Mechanism:**
- A single state file (`state.json`) tracks "where the ball is"
- A smart GitHub Action cron updates the state automatically
- A constitution governs the rules

---

## 📂 Folder Structure

```
.github/workflows/mavis-coordination/
├── README.md              ← This file
├── constitution.md        ← The 6 articles (rules)
├── state-schema.json      ← JSON schema for state.json
├── state-cron.yml         ← GitHub Action (runs every 5-10 min)
├── state.json             ← Live state (committed to develop)
└── examples/
    └── state-initial.json ← Example initial state
```

---

## ⚡ Quick Start (5 steps)

### 1. Read the Constitution

```bash
cat .github/workflows/mavis-coordination/constitution.md
```

The 6 articles define the rules. Read first, edit second.

### 2. Initialize the State

Copy `examples/state-initial.json` to `state.json` and customize:

```bash
cp .github/workflows/mavis-coordination/examples/state-initial.json \
   .github/workflows/mavis-coordination/state.json
```

### 3. Commit the State

```bash
git add .github/workflows/mavis-coordination/
git commit -m "feat(coordination): initialize mavis-coordination workflow"
```

### 4. Enable the Cron

The cron is in `state-cron.yml` and uses `$GITHUB_TOKEN` (already in GitHub Secrets).

No additional setup needed. It runs automatically.

### 5. Verify

After 5-10 minutes, the state.json will be updated by the cron. Check the GitHub Actions tab for logs.

---

## 🎮 How to Use

### For Mavis Local (the executor)

**Before starting work:**
```bash
# 1. Check where the ball is
cat .github/workflows/mavis-coordination/state.json | jq .ball_location

# 2. If "mavis-local" → start working
# 3. If "mavis-cloud" → wait (Siti is reviewing)
# 4. If "anas" → wait (Anas is deciding)
# 5. If "blocked" → check state.issues[] for the problem
```

**When opening a PR:**
```bash
# Update state to hand off the ball
# (the cron will do this automatically, but you can do it manually too)
cat > .github/workflows/mavis-coordination/state.json << EOF
{
  "ball_location": "mavis-cloud",
  "next_action": "Siti: review PR #N",
  ...
}
EOF
git add state.json
git commit -m "coordination: PR #N opened, ball with cloud"
```

### For Mavis Cloud (Siti / Dev — the reviewer)

**Before reviewing:**
```bash
# 1. Check the state
cat .github/workflows/mavis-coordination/state.json | jq '{ball: .ball_location, prs: .open_prs}'
```

**When reviewing/merging:**
```bash
# After merge, the cron will auto-update state.ball = "mavis-local"
# Or manually if you want to be explicit
```

### For Anas (the owner)

**Just check the state:**
```bash
# From anywhere
curl -s https://raw.githubusercontent.com/anas600/ERP-SYSTEM/develop/.github/workflows/mavis-coordination/state.json | jq .
```

**Override the ball (if you want):**
```bash
# Edit state.json directly on GitHub web UI
# Or use git
```

---

## 🔔 Smart Cron Behavior

The cron (`state-cron.yml`):
- Runs every **5-10 minutes** (configurable)
- **Silent on no-change** (doesn't spam)
- Computes `ball_location` based on:
  - Open PRs (assigned to whom?)
  - Recent commits (who pushed last?)
  - Presence signals (any pending?)
  - Last activity timestamps
- Updates `state.json` only if state changed
- Posts **1 comment on develop HEAD** if state changed (with summary)

---

## 📊 State Schema (Quick View)

```json
{
  "version": "1.0",
  "last_updated": "ISO-8601",
  "ball_location": "mavis-local | mavis-cloud | anas | waiting | blocked",
  "ball_owner_session": "session-id",
  "active_sprint": 5,
  "open_prs": [{"number": 172, "state": "open"}],
  "pending_signals": [],
  "issues": [],
  "next_action": "Mavis Local: start Phase 2",
  "last_communication": "ISO-8601",
  "sprint_started_at": "ISO-8601",
  "sprint_eta": "ISO-8601"
}
```

See `state-schema.json` for the full schema.

---

## 🛡️ Architecture Compliance

This workflow follows the **Constitution** of ERP-SYSTEM:

- **Article 4 (Branch discipline):** State committed to develop only
- **Article 5 (Workflow discipline):** Single ping-pong per cycle
- **Article 6 (Delegation):** Clear ball ownership
- **Article 9 (Memory hygiene):** Cron is silent on no-change
- **Article 10 (Local Team):** Mavis Local has full admin on develop
- **Article 14 (Amendment):** Constitution can be amended with Anas's approval

---

## ❓ FAQ

**Q: What if the cron fails?**
A: The state will be stale. Manually update it. The cron will catch up next run.

**Q: What if the ball is stuck?**
A: Check `state.issues[]`. If empty, ping the team in channel 5.

**Q: Can the state be overridden?**
A: Yes. Anas always has override authority. Just edit state.json.

**Q: What if Mavis Local is offline?**
A: The cron still runs (GitHub Actions). It will set `ball_location = "blocked"` if no activity for >30 min.

---

## 🏷️ Tags

- `#Mavis-Coordination` `#Smart-Cron` `#Single-Ping-Pong`
- `#State-Machine` `#Token-Safe` `#Constitution-Article-6`

---

**Constitution author:** سيتی + محمد
**Workflow folder location:** `.github/workflows/mavis-coordination/`
**State file:** `.github/workflows/mavis-coordination/state.json` (committed to develop)
