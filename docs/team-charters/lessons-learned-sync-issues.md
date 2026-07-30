# 📚 دروس من مشاكل التزامن الأخيرة (Lessons Learned — Sync Issues)

> **تحليل أسباب الـ 5+ ساعات من stagnation في Sprint 7 (Test Coverage Deepening).**
> **تاريخ الحدث:** 2026-07-30
> **الهدف:** توثيق الـ root cause + توصيات لتجنب التكرار في أي مشروع مستقبلي.

---

## 🚨 ملخص الحدث

- **التاريخ:** 2026-07-29 23:30 → 2026-07-30 13:00 (≈ 13.5 ساعة)
- **الـ Sprint:** Sprint 7 (Test Coverage Deepening)
- **الـ Task:** T1 — test coverage deepening لـ 4 methods في ChartOfAccountsService
- **الحالة:** BE Jimi `bg_fa2d2471-1f23-49d0-bcc4-df7c95b0d591` فشل بـ token limit
- **النتيجة:** الفريق المحلي + الفريق الإداري كلهم في "standby" — لا أحد يعمل

---

## 🔍 الـ Root Cause Analysis

### المشكلة 1: لا يوجد actor مركزي

```
كل فريق له crons مستقلة:
├─ Local team: check-sprint7-be-jimi (كل ساعة)
├─ Admin team: develop-pr-monitor (كل 10 min)
├─ Admin team: siti-responder (كل 30 min)
├─ Admin team: muhammad-responder (كل 30 min)
└─ Admin team: dev-responder (كل 30 min)

كل cron يقرأ state.json لكن:
- Local cron يقرأ: "BE Jimi FAILED" → ما يعمل شي
- Admin cron يقرأ: "no open PR" → ما يعمل شي
- مفيش cron يقول: "BE Jimi is dead, action needed"
```

**الـ Single Point of Failure:** لا يوجد actor مركزي يربط بين "BE Jimi failed" + "no PR" + "needs takeover".

### المشكلة 2: mavis-local-coordinator معطّل

```
mavis-local-coordinator (cron) — كان يفعل كل 5 min
- يقارن state.json
- يعمل self-merge للـ PRs الجاهزة
- يكتب state.json updated
- يرسل Telegram alerts

**DISABLED since 2026-07-29 19:55** (per AGENTS.md notes)
→ لا أحد يعمل الدور المركزي
```

### المشكلة 3: قناة التواصل المعطلة (mavis communication send)

```
كل فريق في session مختلف:
- mvs_4a1f6064397f4440bac82e3f36602646 (Admin)
- mvs_c39a4f3aaa474a9899f87a4cd49d3645 (Local)

mavis communication send يعمل في الـ CLI:
"C:\Users\Anas\AppData\Local\Programs\MiniMax Code\MiniMax Code.exe"
   "C:\Users\Anas\AppData\Local\Programs\MiniMax Code\resources\resources\daemon\cli.js"

المسار مكسور: resources\resources\daemon\cli.js → غير موجود
→ القناة البديلة الوحيدة: state.json
```

### المشكلة 4: state.json لا يعكس الحقيقة

```json
{
  "ball_location": "mavis-local",
  "active_sprint": "7",
  "next_action": "Awaiting Anas's Sprint 7 scope directive..."
}
```

- ❌ لا يوجد حقل `jimi_status` يعكس إن BE Jimi مات
- ❌ لا يوجد حقل `stalled_since` يحدد متى بدأت المشكلة
- ❌ لا يوجد escalation trigger للـ Mavis Coordinator (root session)

---

## 🩺 الدروس (Lessons)

### Lesson 1: Crons وحدها ما تكفي

> **كل cron يقرأ state.json بزاوية مختلفة.** بدون actor مركزي يجمع الزوايا، الـ crons ما تكتشف deadlock.

**التطبيق المستقبلي:** حقل جديد في `state.json`:
```json
{
  "stalled_actors": {
    "local": {"last_action_at": "2026-07-29T23:30:00Z", "stuck_for_minutes": 810},
    "admin": {"last_action_at": "2026-07-30T13:00:00Z", "stuck_for_minutes": 5}
  },
  "auto_escalate_after_minutes": 60
}
```

### Lesson 2: mavis-local-coordinator كان حاسم

> **لما كان شغّال**، كان يعمل self-merge + يحذف branches + يحدّث state.json. معطّل = لا أحد يعمل.

**التطبيق المستقبلي:** لا تعطّل mavis-local-coordinator إلا بإذن أنس. أو أنقل الدور لـ Mavis Coordinator (root session).

### Lesson 3: Jimi status لازم يكون first-class في state.json

```json
{
  "jimi_status": {
    "be_jimi_bg_fa2d2471": {
      "status": "failed_token_limit",
      "spawned_at": "2026-07-29T23:30:00Z",
      "last_check_at": "2026-07-30T13:00:00Z",
      "stuck_for_minutes": 810,
      "action_needed": "takeover"
    }
  }
}
```

### Lesson 4: mavis CLI needs repair

> **الـ path `resources\resources\daemon\cli.js` غير موجود.** يحتاج إصلاح في الـ installer.

**التطبيق:** إبلاغ أنس — بديل القناة (state.json) يعمل لكن أبطأ.

### Lesson 5: Watcher لا تحل محل Actor

> **Crons observers، ما يـact.** لما تشوف deadlock في state.json، الـ cron يلاحظ بس ما يعمل شي.

**التطبيق المستقبلي:** Crons تكتب `issues[]` في state.json، و Mavis Coordinator (root) يقرأ `issues[]` ويتخذ action.

---

## ✅ التوصيات (Recommendations)

### R1: إعادة تفعيل mavis-local-coordinator

```bash
mavis cron update 0557d4b1 --enabled true
```

### R2: إضافة حقل `jimi_status` في state.json

```json
{
  "jimi_status": [
    {
      "id": "bg_fa2d2471",
      "agent": "coder",
      "task": "T1 test coverage deepening",
      "status": "failed",
      "failure_reason": "token_limit",
      "spawned_at": "...",
      "action_needed": "takeover_or_respawn"
    }
  ]
}
```

### R3: Auto-escalation للـ Mavis Coordinator

```json
{
  "escalation_rules": {
    "stuck_minutes": 60,
    "action": "ping_mavis_coordinator",
    "coordinator_session": "mvs_4d7d32af36994449a90f0103f38f341f"
  }
}
```

### R4: Watchdog cron في Mavis Coordinator

> **ميفز root (أنا) — cron كل 30 دقيقة:**
> 1. اقرأ state.json
> 2. افحص `jimi_status[]` + `stalled_actors`
> 3. إذا أي حقل `action_needed != "none"`:
>    - أعمل takeover (spawn agent بديل)
>    - أو أحدّث state.json (escalate to anas)
>    - أو أعمل ping مباشر (state.json pending_signals[])

### R5: Worktree per sprint (مذكور في local-team.md)

> **كل sprint جديد = worktree محلي جديد.** هذا يحل:
> - merge conflicts بين Jimis
> - فقدان الـ uncommitted work
> - صعوبة التنسيق بين Local Lead و worktree

### R6: Sprint kickoff checklist

```
[Sprint N kickoff]
- [ ] Mavis Coordinator يتفقد state.json
- [ ] Local Lead يتفقد jimi_status[]
- [ ] إذا أي jimi_status.action_needed ≠ "none" → takeover أولاً
- [ ] spawn Jimis في worktree جديد
- [ ] ابدأ T1
```

### R7: Right-sizing الـ Jimis

> **المشكلة:** BE Jimi لـ T1 (test coverage) كلش كبير — scope = 4 service methods + tests + DI + build. ضرب token limit.

**التطبيق:** **قَسّم scope:**
- BE Jimi 1: methods GetById + GetByCode (1.5h)
- BE Jimi 2: methods Create + Delete (1.5h)
- بدل BE Jimi واحد ياخذ 4 methods + tests + build (3h+)

---

## 📋 الـ Action Plan المستقبلي

| # | الـ Action | المسؤول | الأولوية |
|---|------------|---------|----------|
| 1 | إعادة تفعيل `mavis-local-coordinator` | ميفز Coordinator | 🔴 High |
| 2 | إصلاح `mavis CLI` path (resources\resources) | أنس | 🔴 High |
| 3 | إضافة `jimi_status[]` في state.json schema | سيتی | 🟡 Med |
| 4 | إضافة `stalled_actors[]` في state.json schema | سيتی | 🟡 Med |
| 5 | إضافة watchdog cron في Mavis Coordinator | ميفز Coordinator | 🟡 Med |
| 6 | Right-size الـ Jimis في future plans (max 2 service methods per Jimi) | ميفز Coordinator | 🟢 Low |

---

## 🎯 الـ Success Metric

> **من هنا فصاعدًا:** أي deadlock في state.json لازم يكتشفه Mavis Coordinator خلال 30 دقيقة وياخذ action.

**Test pattern:** لو BE Jimi مات، خلال 30 دقيقة:
- Mavis Coordinator يكتشف
- إما spawn بديل
- أو يكمل المهمة بنفسه (takeover)
- أو يـescalate لأنس (لو scope كبير)

---

_هذا الـ lessons-learned يُحدّث بعد كل sync issue جديد._
