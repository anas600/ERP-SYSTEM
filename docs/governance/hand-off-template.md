# 📦 Hand-Off Template v1.0

> **Use this template for ALL cycle hand-offs between Siti (Coordinator) and Mavis Local (Executor).**

---

```markdown
# 📦 Hand-Off v[N] — [Cycle Title]

> **From:** سيتي (CTO Relay)
> **To:** Mavis + Local Team
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

### Task 2: [Title]
...

---

## ✅ Definition of Done

- [ ] [Criterion 1]
- [ ] [Criterion 2]
- [ ] [Criterion 3]
- [ ] Hand-Off report returned

---

## ⏱️ Time Estimate

| Task | Time |
|------|------|
| Task 1 | X min |
| Task 2 | X min |
| ... | ... |
| Reporting | X min |

**Note:** Times are estimates. Quality > speed.

---

## 🔓 Technical Freedom

**Team is empowered to:**
- [Decisions they can make]

**Stop only if:**
- ❌ [Show-stoppers]

---

## 📊 Reporting Template

\`\`\`markdown
## Cycle [N] — Done

### Summary
- [1-2 sentence summary]

### Tasks
- [ ] Task 1: [Status]
- [ ] Task 2: [Status]

### Verification
- [Check 1]: ✅ / ❌
- [Check 2]: ✅ / ❌

### Issues Encountered
- [None / list]

### Time Spent
- [X] minutes

**Ready for next cycle:** YES / NO
\`\`\`

---

**📡 After completion, return hand-off to docs/governance/hand-offs/cycle-[N]-response.md**

**— سيتي، CTO Relay 🛰️**
```

---

## 🔧 Usage

1. **Siti** copies this template to `docs/governance/hand-offs/cycle-N.md`
2. **Siti** fills in cycle-specific details
3. **Siti** pushes to develop branch
4. **Mavis Local** reads from develop
5. **Mavis Local** executes
6. **Mavis Local** writes response to `docs/governance/hand-offs/cycle-N-response.md`
7. **Mavis Local** pushes response + PR (if code changes)
8. **Siti** merges, analyzes, prepares cycle N+1

