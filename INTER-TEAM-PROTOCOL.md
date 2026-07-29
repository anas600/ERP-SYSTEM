# 🤝 بروتوكول التواصل بين الفريقين (Inter-Team Protocol)

> **How Admin Team and Local Team communicate, hand off, and coordinate.**
> **Sub-document to: [CONSTITUTION.md](/CONSTITUTION.md) (root)**

**Last amended:** 2026-07-29
**Status:** 🟢 ACTIVE
**Authors:** سيتی + محمد
**Approved by:** Anas (Project Owner)

---

## 🎯 المادة 1 — المبدأ الأساسي

> **"Single Ping-Pong Per Cycle" — كل cycle = خطوة واحدة فقط بين الفريقين.**

**Why:** تقليل الرسائل، تركيز الانتباه، وضوح المسؤولية.

**How:** الـ `state.json` (per Mavis Coordination Workflow) هو الـ single source of truth.

---

## 📞 المادة 2 — الـ Channels

| من → إلى | Channel | متى | Format |
|----------|---------|-----|--------|
| **Local → Admin** | Session message (Mavis `communicate`) | عند فتح PR | "PR #N opened, ready for review" |
| **Admin → Local** | Session message (Mavis `communicate`) | عند hand-off للـ Sprint التالي | Hand-off doc + state update |
| **Local → Admin** | GitHub PR comment | عند سؤال على review | "Why this approach?" |
| **Admin → Local** | GitHub PR comment | عند review feedback | Inline comments + approval |
| **Admin → Anas** | Telegram | عند urgent issue | ⚠️ Format |
| **Anas → Admin** | Telegram | عند decision | OK / No / Modified |
| **Anas → Admin** | state.json edit | عند override | Manual edit on GitHub |

**Default channel:** state.json (read first, ask later)

---

## 🔄 المادة 3 — الـ Hand-off Cycle (Per Sprint)

### Phase 1: Admin → Local (Hand-off)

**Trigger:** Anas approves new sprint

**Actor:** سيتی (Cloud Coordinator)

**Format:**
```markdown
## Sprint N: <Title>

**Goal:** <what to achieve>

**Time estimate:** <hours>

**Scope:**
- Block A (BE): <tasks>
- Block B (FE): <tasks>
- Block C (Local): <tasks>

**Branch:** `feature/sprint-N-...`

**Constraints:**
- Constitution compliance
- No `tenant_id`
- One test per endpoint

**Definition of Done:**
- [ ] Tests pass
- [ ] 0 regressions
- [ ] Architecture clean
- [ ] PR open + CI green
- [ ] Demo verified

**Next action:** Mavis Local: start work
```

**Side effect:** سيتی يحدّث `state.json`:
```json
{
  "ball_location": "mavis-local",
  "active_sprint": N,
  "next_action": "Mavis Local: start work on Sprint N"
}
```

### Phase 2: Local Work (Implementation)

**Trigger:** state.json.ball_location = "mavis-local"

**Actor:** Mavis Local + Jimis (BE + FE parallel)

**Activity:**
- git pull + branch
- Spawn Jimis
- Code + tests
- Build + verify
- Open PR

**Side effect (when PR opens):** Mavis Local يحدّث state.json:
```json
{
  "ball_location": "mavis-cloud",
  "next_action": "Siti: review PR #N"
}
```

### Phase 3: Admin Review (Merge)

**Trigger:** state.json.ball_location = "mavis-cloud"

**Actor:** سيتی

**Activity:**
- Read state.json
- Review PR
- If CI green + architecture clean: merge (--admin)
- Update state.json: `ball_location = "mavis-local"` (or "waiting" for next sprint)

**Side effect:**
- state.json updated
- Branch deleted
- CHANGELOG.md updated

### Phase 4: Loop (Next Sprint)

**Trigger:** Anas approves next sprint

**Loop back to Phase 1.**

---

## 📋 المادة 4 — Hand-off Templates

### Template 1: Hand-off (Admin → Local)

```markdown
# Sprint N: <Title>

**Goal:** <achieve X>

**Branch:** `feature/sprint-N-...` (off `develop` @ `<sha>`)

## Scope

### Block A (Backend Jimi)
- T1: <task>
- T2: <task>

### Block B (Frontend Jimi)
- T3: <task>
- T4: <task>

### Block C (Local verification)
- T5: <task>

## Constraints
- Constitution (15 articles)
- Architecture: company_id only (no tenant_id)
- One test per endpoint
- 0 source code regressions

## Time estimate
- BE: 1.5h
- FE: 1.5h
- Local: 30m verify + 15m PR

**Total:** ~3.5h

## Definition of Done
- [ ] PR open
- [ ] CI green (6 required checks)
- [ ] 19/19 tests pass
- [ ] Architecture clean
- [ ] Demo verified locally

## Next action
Mavis Local: start Block A + B in parallel, then verify + open PR.
```

### Template 2: PR Open Notification (Local → Admin)

```markdown
## Sprint N done

✅ PR #N opened: <title>
✅ Branch: `feature/sprint-N-...`
✅ Files: <N> (+<X>/-<Y>)
✅ Tests: <N>/<N> pass
✅ CI: <N>/<N> green

**State:** ball_location = "mavis-cloud"

**Next:** سيتی: review + merge.
```

### Template 3: Merge Confirmation (Admin → Local)

```markdown
## Sprint N merged ✅

✅ PR #N merged @ `<sha>`
✅ develop updated
✅ Branch deleted
✅ CHANGELOG.md updated

**State:** ball_location = "mavis-local" (for Sprint N+1)

**Next:** Mavis Local: stand by for next hand-off.
```

### Template 4: Blocked (Either → Other)

```markdown
## 🚨 Blocked on Sprint N

**Issue:** <type>
**Description:** <what happened>
**Detected at:** <timestamp>

**Fix instructions:** <how to resolve>

**State:** ball_location = "blocked"

**Next:** <who> to fix.
```

---

## 🚫 المادة 5 — ما لا يعمله (Anti-Patterns)

| ❌ Anti-Pattern | ✅ Correct |
|----------------|------------|
| "هل راجعت PR؟" (every 5 min) | Read `state.json` |
| "متى تدمج؟" (every 5 min) | Read `state.json` |
| "شنو الجديد؟" (general) | Read `CHANGELOG.md` |
| Multiple messages for same PR | 1 message per state change |
| Asking without context | Always include state + PR + action |
| Skipping Mavis Local | Always go through Mavis Local |
| Editing files outside scope | Use the right constitution |

---

## 🎯 المادة 6 — Session ID Reference

| Team | Role | Session ID |
|------|------|-----------|
| **Admin (Cloud)** | سيتی (primary) | `406067545768199` |
| **Admin (Cloud)** | محمد (read-only) | (same session, mode switch) |
| **Admin (Cloud)** | ديف (DevOps) | (same session, mode switch) |
| **Local** | Mavis Local | `mvs_c39a4f3aaa474a9899f87a4cd49d3645` |
| **Local** | Jimi BE/FE | (spawned per sprint) |
| **External** | Mephisto (sandbox) | (own session, per Article 13) |
| **External** | Abdo's team | (own session) |
| **Owner** | Anas | (Telegram relay) |

---

## 📊 المادة 7 — Decision Authority Matrix

| Decision | Who Decides | Who Consults | Approval |
|----------|-------------|--------------|----------|
| **Code changes** | Mavis Local | Admin (review) | Self-merge with --admin |
| **PR merge** | سيتی | Mavis Local (PR author) | --admin |
| **Hand-off docs** | سيتی | Mavis Local (consumer) | n/a |
| **state.json** | سيتی + Mavis Local | Cron (auto) | n/a |
| **Crons** | ديف | سيتی | n/a |
| **Strategic advice** | محمد | Anas (consumer) | n/a |
| **Architecture** | Anas | محمد (recommendation) | Anas only |
| **Constitution** | Anas | Mavis (any) | Anas only |
| **Staging/Production** | Anas | Mavis (any) | Anas only |

---

## 📅 المادة 8 — الـ Cadence

| Activity | Frequency | Who |
|----------|-----------|-----|
| **state.json update** | Every 5 min (cron) | state-cron.yml |
| **PR review** | When PR opens | سيتی |
| **Sprint hand-off** | Per sprint start | سيتی |
| **Sprint closure** | Per sprint end | سيتی |
| **Branch cleanup** | Per sprint end | سيتی |
| **CHANGELOG update** | Per sprint | سيتی |
| **Strategic review** | On request | محمد |
| **Constitution review** | On request | Anas + سيتی |

---

## 🛡️ المادة 9 — Quality Standards (Communication)

### Every message MUST include:

1. **Context:** what triggered this message
2. **Action:** what the receiver should do
3. **Deadline:** when it's needed (if any)
4. **Reference:** PR #, state.json field, file path

### Examples:

❌ **Bad:** "PR ready"

✅ **Good:**
```
## PR #172 ready

**Context:** Sprint 5 complete (Demo V2)
**Action:** Review + merge with --admin per Constitution Article 10
**Deadline:** Within 2 hours
**Reference:** PR #172, state.json.ball_location = "mavis-cloud"
```

❌ **Bad:** "Token issue"

✅ **Good:**
```
## 🚨 Token expired

**Context:** $GITHUB_TOKEN 401'd on cron
**Action:** Renew in GitHub Settings → Tokens, update MAVIS_GITHUB_TOKEN secret
**Deadline:** ASAP (Sprint 6 blocked)
**Reference:** state.json.issues[0]
```

---

## 🔔 المادة 10 — Emergency Escalation

```
Level 1: Mavis Local ↔ سيتی
         (operational, normal)
         ↓
Level 2: سيتی ↔ محمد
         (strategic, on request)
         ↓
Level 3: سيتی ↔ Anas (Telegram)
         (urgent, exceptional)
         ↓
Level 4: Anas (decision)
         (Constitution, architecture, scope)
```

**Default:** Level 1 (Mavis Local ↔ سيتی)
**Escalate when:** unclear, blocked, Constitution violation, scope change

---

## 📜 المادة 11 — التعديل (Amendment)

1. **Proposal:** سيتی + Mavis Local + محمد
2. **Review:** Anas (Project Owner)
3. **Update:** Add `[Amended YYYY-MM-DD: reason]`
4. **Commit:** `docs(governance): amend Inter-Team Protocol Article N`
5. **Merge:** بعد Anas approval

---

_Last amended: 2026-07-29 by سيتی + محمد, approved by Anas_
