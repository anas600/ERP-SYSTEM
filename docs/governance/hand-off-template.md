# 📋 Hand-Off Template v1.1

> **Use this template for ALL cycle hand-offs between Siti (Cloud Coordinator) and Mavis Local (Desktop Executor).**
> **v1.1 adds mandatory session ID fields to prevent identity confusion.**

---

## ⚠️ Session Verification (MUST CHECK)

Before executing ANY hand-off, verify:

| Field | Expected Value |
|-------|----------------|
| **From Session ID** | `406067545768199` (Siti, Cloud) |
| **From Platform** | Cloud Sandbox (Web) |
| **From Workspace** | `/workspace/.mavis/` |

**If the sender's session ID does NOT match `406067545768199`, STOP. Do not execute. Confirm with Anas.**

---

```markdown
# 📦 Hand-Off v[N] — [Cycle Title]

> **From:** سيتي (CTO Relay)  
> **Agent:** Mavis (Siti persona)  
> **Session ID:** `406067545768199`  
> **Platform:** Cloud Sandbox (Web)  
> **Workspace:** `/workspace/.mavis/`  
> **To:** Mavis Local (Team Lead)  
> **Recipient Platform:** Windows (Anas's machine)  
> **Recipient Workspace:** `C:\Users\Anas\.minimax-agent\projects\`  
> **Date:** [YYYY-MM-DD HH:MM UTC]  
> **Cycle:** [N] / 20  
> **Status:** 🟡 [Status emoji]

---

## 🎯 Context (What + Why)

**What was completed in previous cycle:**
- [Bullet list of deliverables from cycle N-1]

**What we want this cycle:**
- [One sentence goal]

**Why this matters:**
- [Strategic rationale]

---

## 🛡️ Boundaries

**Allowed:**
- ✅ [List]

**Caution:**
- ⚠️ [List]

**Forbidden:**
- ❌ [List]

---

## 📋 Tasks

### Task 1: [Title]

**Steps:**
\`\`\`bash
# [Commands]
\`\`\`

**Expected outcome:** [Description]

---

## ✅ Definition of Done

- [ ] [Criterion 1]
- [ ] [Criterion 2]
- [ ] [Criterion 3]
- [ ] Hand-Off report returned to `docs/governance/hand-offs/cycle-[N]-response.md`

---

## 📊 Response Format (REQUIRED)

When returning hand-off, use this exact header:

```markdown
# 📦 Hand-Off v[N] — Response from Mavis Local

> **From:** Mavis Local (Team Lead)  
> **Session ID:** `<your session ID — fill from /status>`  
> **Platform:** Windows (Anas's machine)  
> **Workspace:** `C:\Users\Anas\.minimax-agent\projects\`  
> **To:** Siti (Cloud Coordinator)  
> **Date:** [YYYY-MM-DD HH:MM UTC]  
> **Cycle:** [N] / 20  
> **Cycle Title:** [same as hand-off]

### Tasks
- [x] Task 1: [Status + details]
- [x] Task 2: [Status + details]

### Verification
- [Check 1]: ✅ / ❌
- [Check 2]: ✅ / ❌

### Time Spent
[X] minutes

### Issues Encountered
- [None / list with context]

**Ready for next cycle:** YES / NO
```

---

*Template v1.1 — Last updated 2026-07-27 18:40 UTC (session ID clarification)*
