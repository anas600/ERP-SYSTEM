# 📂 `.mavis/plans/` — Project Plan References

> **هذا المجلد يحوي فقط مراجع للخطط الرسمية، NOT الحالة الفعلية.**

---

## القاعدة (Single Source of Truth)

| النوع | المسار | من يكتب فيه |
|------|--------|-------------|
| **🟢 المسار الرسمي (Source of Truth)** | `~/.mavis/plans/<plan-id>/` | mavis daemon + المالك (Mavis) |
| **🔵 مرجع المشروع (هذا المجلد)** | `<project>/.mavis/plans/` | المالك فقط، للقراءة |

> **القاعدة:** كل الملفات الديناميكية (state.json, board.md, outputs/, decisions/, notes/)
> تُكتب فقط في المسار الرسمي. هذا المجلد للمشروع يحوي **نسخاً مرجعية ثابتة فقط**.

---

## الملفات في هذا المجلد

| الملف | الوصف | المصدر الرسمي |
|------|------|---------------|
| `phase3-plan.reference.yaml` | نسخة مرجعية من plan.yaml لـ Phase 3 | `~/.mavis/plans/plan_b5ae4fc0/plan.yaml` |

> لا تنشئ/تعدّل ملفات هنا إلا إذا كنت تريد **مرجعاً ثابتاً محلياً**. أي ملف ديناميكي
> (board, state, decisions, outputs) → المسار الرسمي فقط.

---

## كيف الـ sub-agents يعرفون المسار الرسمي

1. **من الـ prompt** (في كل task): المالك يكتب صراحةً "اطلب المسار الرسمي عند الحاجة"
2. **من AGENTS.md**: المشروع يحوي مرجع للمسار الرسمي في قسم "Workflow / Plans"
3. **من mavis CLI**: `mavis team plan status <plan-id>` يعيد الـ path الرسمي

---

## كيف تقرأ الملفات الرسمية

```powershell
# الحالة (state.json, board.md)
Get-Content "$HOME\.mavis\plans\<plan-id>\state.json"
Get-Content "$HOME\.mavis\plans\<plan-id>\board.md"

# المخرجات (deliverables)
Get-ChildItem "$HOME\.mavis\plans\<plan-id>\outputs"

# القرارات (decisions)
Get-ChildItem "$HOME\.mavis\plans\<plan-id>\decisions"
```

> 💡 تلميح: `~` في PowerShell = `$HOME` = `C:\Users\Anas`. على Linux/Mac = `/home/anas`.

---

## ⚠️ متى لا تكتب هنا

- ❌ لا تحفظ `decision-cycle*.json` هنا — دائماً في `~/.mavis/plans/<plan-id>/decisions/`
- ❌ لا تحفظ `deliverable.md` هنا — في `~/.mavis/plans/<plan-id>/outputs/<task-id>/`
- ❌ لا تنشئ `notes/`, `workspace/`, `state.json` هنا — للمسار الرسمي فقط

---

## ✅ متى تكتب هنا

- ✅ **نسخة مرجعية من plan.yaml** (snapshot، للقراءة فقط) — اسمها ينتهي بـ `.reference.yaml`
- ✅ **README/INDEX** يشرح العلاقة بالمسار الرسمي (مثل هذا الملف)
- ✅ **AGENTS.md override** خاص بالمشروع (لو في conventions مختلفة عن الـ root)

---

## مثال: Phase 3 (مكتمل)

```
~/.mavis/plans/plan_b5ae4fc0/                  # ← الرسمي (مصدر الحقيقة)
├── plan.yaml                  # the YAML
├── board.md                   # live timeline of all events
├── state.json                 # engine state (cycle, results)
├── notes/
│   ├── intro.md
│   └── cycle-5-report.md
├── outputs/
│   ├── research-daftra/deliverable.md
│   ├── research-erpnext/deliverable.md
│   ├── research-odoo-brief/deliverable.md
│   ├── gap-analysis/deliverable.md
│   ├── frontend-phase3/deliverable.md
│   └── backend-phase3/deliverable.md     ← أنشأه المالك (Mavis) بعد takeover
└── decisions/                                ← owner decisions (override_accept, manual_retry)
    ├── decision-cycle2.json
    ├── decision-cycle3.json
    └── decision-cycle3b.json

ERP-SYSTEM/.mavis/plans/                      # ← هذا المجلد (مرجع فقط)
├── README.md                                  # ← هذا الملف
└── phase3-plan.reference.yaml                 # snapshot من plan.yaml
```

---

**آخر تحديث:** 2026-06-24 — convention بعد Phase 3 completion.
