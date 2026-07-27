# 📡 Presence Check Protocol

> **Status:** ✅ ACTIVE (per DEC-072, 2026-07-28 00:11 UTC)  
> **Authority:** Anas (Project Owner)  
> **Use case:** On-demand "are you alive?" between Mavis (Cloud) and Mavis (Local)

---

## 🎯 What is this?

A **low-noise, on-demand presence check** between Mavis (Cloud) and Mavis (Local):
- Posts a status comment to the latest develop commit
- Shares the current board state
- Goes silent when cycle is working
- Provides "reassurance" not "request for work"

---

## 🚀 How to Use

### Step 1: Anas triggers a presence check

**Option A: Push a signal file to develop**
Create `docs/governance/presence-signal.json`:
```json
{
  "from": "anas",
  "to": ["cloud", "local"],
  "timestamp": "2026-07-28T00:11:00Z",
  "message": "presence check"
}
```

**Option B: Send a Telegram message**
- "presence check now" → Mavis (Cloud) detects and acts
- "presence check local" → Mavis (Local) detects and acts

**Option C: Commit message**
- Any commit with `[presence-check]` in the message will trigger the cron

### Step 2: The cron posts a comment

Within 5 minutes (max), the cron (in Cloud or Local) will:
1. Detect the signal
2. Read the board file
3. Post a comment on the latest develop commit with:
   - Session ID + platform
   - Current cycle + status
   - Last action timestamp
   - Board state snapshot
4. Delete the signal file

### Step 3: The other side responds

When Mavis (Local) sees Mavis (Cloud)'s comment (or vice versa), it posts ONE reply with its own state.

After 1 reply from each side, the presence check is complete. No further activity until next signal.

---

## 📋 Comment Format

```markdown
## 📡 Presence Check — [session-id] ([platform])

**Time:** [ISO timestamp]
**Session:** [session-id]
**Platform:** [Cloud/Windows]
**Cycle:** [N] / 20 — [status]
**Last action:** [time ago]
**Current task:** [what doing now]

**Board snapshot:**
- Cycle [N]: [title]
- Status: [emoji status]
- Cron: [cron name] ([active/silent])
- Other party: [status]

✅ All systems normal. Continuing work.
```

---

## 🔧 Cron Setup (for Mavis Local team)

If you're Mavis Local, set up the same cron in your session:

```yaml
# Cron configuration
name: presence-check
schedule: "*/5 * * * *"  # every 5 minutes
action: |
  1. GET /repos/anas600/ERP-SYSTEM/contents/docs/governance/presence-signal.json
  2. If 200 OK (file exists):
     a. Parse signal JSON
     b. Read docs/governance/board.md
     c. POST comment to latest develop commit
     d. DELETE presence-signal.json
  3. If 404 (no signal): stay silent
```

---

## 🛑 When NOT to Use

- ❌ Cycle is progressing normally (no signal needed)
- ❌ You want detailed work updates (use cycle-N.md hand-offs)
- ❌ You want real-time conversation (use Telegram)
- ❌ Multiple times in a row (wait at least 1 hour between checks)

---

## 📊 Example Flow

```
T+0:00  Anas: "presence check" (Telegram to Mavis Cloud)
T+0:01  Mavis Cloud: detects signal
T+0:02  Mavis Cloud: reads board, posts comment on develop commit
T+0:03  Mavis Cloud: deletes signal file
T+0:05  Mavis Local: cron runs, sees comment from Cloud
T+0:06  Mavis Local: reads board, posts reply comment
T+0:10  Mavis Cloud: cron runs, sees reply from Local, no action
T+0:15  Both silent. Presence check complete.
```

---

## 🎯 Benefits

1. **Low noise** — Only on demand, not scheduled
2. **Audit trail** — Comments are on git history
3. **Cross-platform** — Works on Cloud and Local
4. **Reassurance** — Not request for work
5. **Self-cleaning** — Signal file deleted after use
6. **State snapshot** — Both sides see the same board

---

**Signed:** سيتي (Cloud Coordinator)
**Date:** 2026-07-28 00:11 UTC
**Authority:** DEC-072
