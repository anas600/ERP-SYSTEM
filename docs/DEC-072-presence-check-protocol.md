# DEC-072: Presence Check Protocol via Develop Comments

> **Status:** ✅ ACTIVE (per Anas's directive)  
> **Date:** 2026-07-28 00:11 UTC (Europe/Berlin)  
> **Authority:** Anas (Project Owner)  
> **Context:** Mavis Local observed improving internal constitution/communication each cycle; Anas wants formal "reassurance" channel

---

## 📋 Summary

Anas has proposed a **Presence Check Protocol** — a low-noise, on-demand "heartbeat" mechanism that:
1. Posts a status comment to the latest develop commit when triggered
2. Shares the current board state between Mavis (Cloud) and Mavis (Local)
3. Goes silent when the cycle is working normally
4. Provides "reassurance" (I'm working, not waiting for instructions)
5. **Only fires from Anas's manual signal** (not scheduled automatically)

---

## 🎯 Design

### Trigger Mechanism

**Anas signals presence check by:**
- Pushing `docs/governance/presence-signal.json` to develop
- OR: Sending a specific Telegram message to Mavis (Cloud or Local)
- OR: Creating a commit with message `presence-check` (Mavis detects it)

**Signal file format (`docs/governance/presence-signal.json`):**
```json
{
  "from": "anas",
  "to": ["cloud", "local"],
  "timestamp": "2026-07-28T00:11:00Z",
  "message": "presence check"
}
```

### Cron Behavior

**`presence-check` cron (in BOTH Mavis Cloud + Mavis Local):**
- **Schedule:** Every 5 minutes (cheap check)
- **Action:** If signal file exists on develop:
  1. Read current `docs/governance/board.md` state
  2. Post a comment to the latest develop commit with:
     - Session ID (Cloud or Local)
     - Platform (Cloud/Windows)
     - Current cycle + status
     - Last action timestamp
     - Board state snapshot
  3. Delete the signal file
  4. Self-stop until next signal

### Comment Format

```markdown
## 📡 Presence Check — [session-id] ([platform])

**Time:** 2026-07-28 00:15 UTC
**Session:** 406067545768199 (Cloud)
**Cycle:** 3 / 20 — ACTIVE
**Last action:** 5 minutes ago
**Current task:** Watching for PR cycle-3

**Board snapshot:**
- Cycle 3: 6.5 CI/Hardening
- Status: 🟡 ACTIVE
- Cron: monitor-cycle-3-pr-merge (active)
- Mavis Local: working on cycle 3

✅ All systems normal. Continuing work.
```

### Response Mechanism

- When Mavis (Local) sees Mavis (Cloud)'s comment, Mavis (Local) posts its own comment (1 reply)
- When Mavis (Cloud) sees Mavis (Local)'s comment, no further action (already done)
- After 1 reply from each side, presence check is complete
- No further activity until next signal

### When to Use

✅ **Use when:**
- You (Anas) want to know both sides are alive
- You suspect a session may be stuck
- You want a quick "snapshot" of where both teams are
- Before making a decision (e.g., should I trigger cycle 4?)

❌ **Don't use when:**
- Cycle is progressing normally (no signal needed)
- You want detailed work updates (use cycle-2.md hand-offs)
- You want a real-time conversation (use Telegram)

---

## 🔄 Cross-Platform Notes

### Mavis (Cloud) Implementation
- ✅ Created cron `presence-check` in this session
- File: `/workspace/.mavis/governance/cron-presence-check.md`
- Monitors: `docs/governance/presence-signal.json` on develop

### Mavis (Local) Implementation
- Mavis Local should create the same cron in their session
- Read: `docs/governance/hand-offs/presence-protocol.md`
- Or: Use the cloud cron as template (it's documented)

---

## 📂 Files to Create

1. `docs/governance/hand-offs/presence-protocol.md` (NEW, on develop)
2. Cron `presence-check` in Mavis Cloud (DONE)
3. (Optional) Cron `presence-check` in Mavis Local (TBD by Mavis Local)

---

## ⚖️ Constitution Compliance

- ✅ **Article 3** (company_id): Not affected
- ✅ **Article 4** (Branch discipline): Comments on develop commit, no force-push
- ✅ **Article 7** (NO SECRETS): No secrets in comments (only session IDs, public state)

---

## 📅 Effective Period

**Start:** 2026-07-28 00:11 UTC (immediately)
**End:** Until cycle 20 completes OR Anas issues DEC-073

---

**Signed:** Anas (via Telegram voice)
**Witnessed by:** محمد (Strategic Advisor, session 406067545768199)
**Documented by:** سيتي (Cloud Coordinator, session 406067545768199)
