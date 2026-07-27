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

### ⚠️ Pre-Hand-off Verification (BEFORE writing tasks)

Before listing tasks, **verify your scope against current develop state**:

```bash
# Check what's already on develop
git fetch origin develop
git log origin/develop --oneline -10

# Check if tests/files referenced already exist
git show origin/develop:src/path/to/file.cs | head -5
```

**If the previous cycle's deliverable matches a proposed task, mark it as
"already done" and reference the existing file/commit. Don't repeat work.**

**Real example (cycle 3):** The hand-off said "Add 2 missing new test cases"
but cycle 2's PR #154 had already added them to
`src/backend/Tests/ERPSystem.Tests/Auth/CompanyContextTests.cs`. Mavis Local
caught this in T1 inventory and documented it in the response (T1 = "already
done, see cycle 2"). The investigation took 2 minutes, the actual work was
zero — saving the executor from rewriting code that was already there.

### Task 1: [Title]

**Steps:**
\`\`\`bash
# [Commands]
\`\`\`

**Expected outcome:** [Description]

### ⚠️ Use Specific File Paths (NOT generic "update tests")

**Bad:** "T1: Update the 31 C# test files to use company_id"

**Good:** "T1: Update the following 8 test files (verified by grep on cycle
N-1 HEAD): `src/backend/Tests/ERPSystem.Tests/Reports/FinanceReportServiceTests.cs`
(36 refs), `src/backend/Tests/ERPSystem.Tests/Reports/InventoryReportServiceTests.cs`
(24 refs), ..."

If you can't name the specific files, mark the task as "investigate" and let
Mavis Local figure out the scope.

### ⚠️ Distinguish "investigate" from "fix"

**Bad:** "T4: Fix HF Space sync workflow (failed in PR #154)"

**Good:** "T4: Investigate whether HF Space sync applies to develop PRs.
If yes, fix the workflow. If no, document why it doesn't apply."

This is critical for tasks that may not apply to the current state.
Mavis Local will not blindly fix something that may not be broken.

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
