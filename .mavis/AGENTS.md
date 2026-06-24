# 🤖 `.mavis/AGENTS.md` — Project Mavis Workspace

> هذا المجلد مخصص لـ **Mavis (owner)** لإدارة الـ plans والـ orchestration للـ sub-agents.
> الـ sub-agents الفرعيون (backend-architect, frontend-engineer, ...) ما يقرؤون هذا المجلد مباشرة.

---

## شو فيه

```
.mavis/
├── AGENTS.md        # هذا الملف
└── plans/           # مراجع الـ plans (انظر plans/README.md)
```

---

## قاعدة Plan Storage (Single Source of Truth)

> **مهم:** كل الـ plans الرسمية تُكتب تلقائياً في `~/.mavis/plans/<plan-id>/` بواسطة `mavis team plan`.

| النوع | المسار | مين يكتب فيه | مين يقرأ |
|------|--------|-------------|--------|
| 🟢 **Plan الرسمي** | `~/.mavis/plans/<plan-id>/` | mavis daemon + المالك | الكل (عند الحاجة) |
| 🔵 **مرجع المشروع** | `<project>/.mavis/plans/` | المالك فقط | المراجعة المحلية |

### الـ Official plan dir يحوي:
- `plan.yaml` — تعريف الـ tasks
- `board.md` — timeline من events (يكتبها الـ daemon)
- `state.json` — engine state (cycle, results)
- `notes/cycle-*.md` — cycle reports
- `outputs/<task-id>/deliverable.md` — ناتج كل task
- `decisions/decision-*.json` — قرارات المالك (override_accept, manual_retry, etc.)

### مرجع المشروع يحوي فقط:
- `plans/README.md` — هذا الشرح
- `plans/<plan-name>.reference.yaml` — نسخة ثابتة (snapshot) من plan.yaml للقراءة
- **NO** board.md, state.json, outputs/, decisions/ — هذه فقط في المسار الرسمي

---

## كيف الـ sub-agents يعرفون المسار الرسمي

### 1. من الـ prompt (يكتبه المالك)
```yaml
prompt: |
  ... (your task description) ...
  
  Reference: see `~/.mavis/plans/plan_b5ae4fc0/board.md` for live status
  and `~/.mavis/plans/plan_b5ae4fc0/outputs/<task-id>/deliverable.md` for context.
```

### 2. من mavis CLI
```bash
# اعرف حالة الـ plan
mavis team plan status <plan-id>

# افتح outputs (auto-created by daemon)
ls ~/.mavis/plans/<plan-id>/outputs/

# اقرأ board
cat ~/.mavis/plans/<plan-id>/board.md
```

### 3. من AGENTS.md (root)
الـ root AGENTS.md يذكر المسار الرسمي في قسم "Plan storage convention".

---

## لما تشتغل هنا

- ✅ **اقرأ** مرجع المشروع (`<project>/.mavis/plans/*.reference.yaml`) للـ context السريع
- ✅ **اكتب** القرارات في `~/.mavis/plans/<plan-id>/decisions/` (المسار الرسمي)
- ✅ **اكتب** deliverables في `~/.mavis/plans/<plan-id>/outputs/<task-id>/` (الـ daemon يكتبها أحياناً)
- ✅ حدّث هذا الـ AGENTS.md إذا تغيرت الـ convention

## بعد التعديل

- تأكد أن المسار الرسمي ما فقد ملفات
- اعمل commit للمرجع في المشروع (`<project>/.mavis/plans/`) كـ snapshot فقط

---

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md) — root project documentation
- `~/.mavis/plans/` — official plan storage (user home)
- `mavis team plan` CLI — owner command surface
