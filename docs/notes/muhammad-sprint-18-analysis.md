# محمد — تحليل Sprint 18 (Governance Cleanup)

> **المصدر:** تحليل محمد الاستراتيجي (2026-08-01 08:11 UTC) للملفات اللي تكسر الـ Two-Mode Workflow.
> **الغرض:** مرجع سريع لأنس، يرجعله كل ما احتاج يتفقد اقتراحات محمد.
> **الحالة:** وثيقة تاريخية — Sprint 18 governance cleanup tasks بنتها بناءً على هذا التحليل.

---

## ✅ نتيجة الـ Mode 1 → Mode 2 cycle (Sprint 17)

| Phase | النتيجة |
|-------|---------|
| Mode 1 (development محلي) | ✅ Sprint 17 — 8 files, +422/-9 lines |
| Mode 2 (push تلقائي) | ✅ PR #192 → CI 6/6 → merge → tag → cron → Telegram |
| **T2 verify** | ✅ Anas دخل localhost:3000، login، dashboard فيه demo data |

**Workflow مثبت على الريموت (develop + cron).** Sprint 18+ يقدر يستخدم نفس الـ pattern.

---

## ❌ ملاحظات تكسر الـ Work الجديد (Sprint 18 cleanup target)

| الملف | المشكلة | الحل المقترح |
|------|---------|------------|
| **`CONSTITUTION.md`** (header) | "Status: ⏸️ PAUSED" + "End of pause: 2026-07-31 18:25 UTC" — إحنا 2026-08-01، الـ pause انتهى. لكن ما أحد بدّل الـ status. **يكسر**: يعطي Admin إحساس إن الـ constitution مش active. | أزل "PAUSED" header. استبدل بـ "**Status: ✅ ACTIVE** (Two-Mode Workflow per Sprint 17)". |
| **`CONSTITUTION.md`** Article 14 | "**Merge to main** after Phase completion" — يخالف branch architecture reset (main = LOCKED). | استبدل بـ "main = LOCKED archive per branch architecture reset (2026-07-31)". |
| **`CONSTITUTION.md`** Article 15 | "Communication Protocol" — يرجع لـ "Cloud Team + Telegram ping-pong" pattern اللي انتهى. | استبدل بـ "Two-Mode Workflow + cron + Telegram auto-ping". |
| **`AGENTS.md`** (header) | "Last updated: 2026-07-29 19:15 UTC (Constitutional update per Anas mandate: WORKFLOW.md promoted to project root...)" — ما أحد حدّثه لـ Sprint 17. | Update لـ "Last updated: 2026-08-01 (Sprint 17: Two-Mode Workflow codified)". |
| **`AGENTS.md`** "Active governance" section (top) | "**Active workflow constitution:** [`WORKFLOW.md`](./WORKFLOW.md)" — الـ WORKFLOW.md نفسه مش محدّث + الـ pause انتهى. | أزل الـ "WORKFLOW.md" reference. Two-Mode Workflow section في AGENTS.md نفسه هو المرجع. |
| **`AGENTS.md`** (mavis-coordination) | "**.github/workflows/mavis-coordination/state.json**" كـ "single source of truth" — ما عاد مستخدم. | أزل. |
| **`WORKFLOW.md`** (root) | الـ 2-day pause constitution. **Ended 2026-07-31**. الآن obsolete. | **أمسحه** (الـ Two-Mode Workflow هاجر لـ AGENTS.md + CONSTITUTION.md). |
| **`.github/workflows/mavis-coordination/`** | المجلد كله + state.json + state-cron.yml. **Obsolete** بعد الـ pause. | **احذفه بالكامل** (Directory + all files). |
| **`docs/personas/mavis.md`** (أو أي ملف في المجلد) | "**Mavis Cloud (Siti/Muhammad mode)** | Cloud Coordinator + Architect | Plan, write hand-offs, verify PRs, merge, governance" — هذا الـ role القديم. الآن Cloud team منتهية. | حدّث الـ roles table: Mavis Local = sole Tech Lead + Admin + Coordinator. Muhammad = Strategic Advisor only. |
| **`docs/personas/dev.md`** | "**DevOps** | Dev (Mavis mode) | CI, infra, crons" — الـ role ما عاد مستخدم بنفس الشكل. | ادمجه مع Mavis Local role. |
| **`docs/personas/siti.md`** | "Cloud Coordinator" — defunct. | علّمه archived. |
| **`docs/team-charters/team-charter.md`** (لو موجود) | ما أحد راجعه بعد الـ 3-Layer Model. | اقرأه + حدّث لو في conflict. |
| **`mvp-docker/.env.example`** (موجود) | نضيف `BOOTSTRAP_SEED_DEMO_DATA=true` كـ default → قد يخالف الـ "no demo in production" rule. | أضف warning صريح: "demo only — set false in production". |
| **`.mavis/plans/`** (gitignored) | يحتوي على scratchpads قديمة من قبل الـ pause. | مو GitHub issue — يترك محلياً. |

---

## ✅ ملاحظات صحيحة (ما تحتاج تعديل)

| الملف | الوضع |
|------|-------|
| `AGENTS.md` (Two-Mode Workflow section) | ✅ Sprint 17 + 14 retro صحيح |
| `CONSTITUTION.md` Article 10 (Two-Mode + relax-restore pattern) | ✅ Sprint 17 صحيح |
| `mvp-docker/smoke-test.ps1` | ✅ 9/9 checks (Sprint 17) |
| `mvp-docker/docker-compose.yml` | ✅ Sprint 17 |
| `src/frontend/Dockerfile` (NEXT_PUBLIC_API_URL build-time) | ✅ Sprint 14 |
| `scripts/` (Sprint 15-17) | ✅ |
| `docs/architecture/holding-company-architecture.md` | ✅ (referenced in AGENTS.md as the single source of truth) |
| `docs/workflow/sprint-N.md` (historical) | ✅ (سجل تاريخي — ما يحتاج تعديل) |
| `docs/team-charters/retrospectives/sprint-N-retro.md` | ✅ (historical record) |

---

## 🎯 المسار المقترح: 3 خيارات

### Option A (الأفضل): "Single Active Governance" — كل الـ governance في CONSTITUTION.md
- ابدأ بـ CONSTITUTION.md كامل (الـ Two-Mode Workflow + relax-restore + Active status)
- AGENTS.md: رابط لـ CONSTITUTION.md (لا duplicate)
- WORKFLOW.md: **احذفه** (obsolete)
- `.github/workflows/mavis-coordination/`: **احذفه**
- Personas: حدّث فقط mavis.md (الـ admin role الجديد)

### Option B (بسيط): "Keep AGENTS.md As-Is, Fix CONSTITUTION.md Only"
- حدّث CONSTITUTION.md header + Article 14/15
- خلّي AGENTS.md زي ما هو
- احذف WORKFLOW.md و mavis-coordination
- اعمل archive لـ personas القديمة

### Option C (الأكثر تحفظاً): "Just Update Headers, Archive Old Stuff"
- حدّث CONSTITUTION.md header فقط
- احذف WORKFLOW.md + mavis-coordination
- خلّي personas كـ "historical reference"

**توصية محمد = Option A.** السبب: الدستور يبقى "الدستور"، AGENTS.md يبقى "دليل العمل اليومي"، ما فيش duplicate. المجلدات المنتهية (.github/workflows/mavis-coordination/) تُحذف لأنها **تكسر الـ mental model** (Admin يقدر يظن إن الـ state.json لسا مستخدم).

---

## 📋 Sprint 18 — Task List للـ Admin (في Mode 1)

```
Sprint 18: Governance Cleanup + Workflow Doc Consolidation
================================================================

[Mode 1 Tasks — local work, no push]
1. [P0] Update CONSTITUTION.md:
     a) Replace "⏸️ PAUSED" header with "✅ ACTIVE (Two-Mode Workflow per Sprint 17)"
     b) Update Article 14 (Merge to main) → "main = LOCKED per branch architecture reset"
     c) Update Article 15 (Communication) → "Two-Mode Workflow + cron + Telegram"
     d) Verify Article 10 (Two-Mode) is the canonical merge procedure

2. [P0] Delete WORKFLOW.md (root) — obsolete (pause ended 2026-07-31)
     - Verify no other file references it (rg "WORKFLOW.md" src/ docs/ AGENTS.md)

3. [P0] Delete .github/workflows/mavis-coordination/ (entire directory):
     - state.json
     - state-cron.yml
     - coordination.md (if exists)
     - Any other files

4. [P0] Update AGENTS.md:
     a) Header "Last updated: 2026-07-29" → "2026-08-01 (Sprint 17 + Sprint 18 governance cleanup)"
     b) Remove "Active governance" section (refs WORKFLOW.md + state.json)
     c) Add "**Active governance:** [CONSTITUTION.md Article 10](./CONSTITUTION.md#-article-10--local-team-empowerment--from-dec-070--sprint-17-update) — Two-Mode Workflow"
     d) Remove `.github/workflows/mavis-coordination/state.json` reference

5. [P1] Update docs/personas/:
     a) mavis.md: roles table → "Mavis Local = Admin + Tech Lead + Coordinator" (drop "Mavis Cloud" row)
     b) siti.md: "ARCHIVED — Cloud team merged into Local per 2-day pause directive (2026-07-29)"
     c) dev.md: "ARCHIVED — DevOps role merged into Mavis Local"
     d) Keep muhammad.md + general worker references (still active)

6. [P1] Verify no other file breaks:
     - `rg "tenant_id" src/` (should be 0 results in new code)
     - `rg "WORKFLOW.md" .` (should be 0 after deletion)
     - `rg "mavis-coordination" .` (should be 0)
     - `rg "Cloud Coordinator" .` (should be 0 in active docs)

7. [P1] Update CHANGELOG.md with Sprint 18 entry (P0/P1 governance cleanup)

8. [P1] Write Sprint 18 retro

[Mode 2 — push when ready]
9. Commit + push
10. CI 6/6 → merge → tag → cron → Telegram ping
11. Verify: open localhost:3000, login, dashboard still works (should be unchanged — governance only)
```

**الوقت المتوقع:** ~1.5 ساعة (majority mechanical edits + deletions).

---

## 🏗️ Architecture بعد Sprint 18

```
[Active governance — single source of truth]
CONSTITUTION.md
    │
    ├─ Article 1-9: Project identity, roles, architecture, branches, workflow
    ├─ Article 10: Two-Mode Workflow (Mode 1: Development, Mode 2: Release)
    └─ Article 11-15: Test strategy, communication, etc. (updated)

AGENTS.md (developer guide)
    │
    ├─ Child DOX Index (per directory)
    ├─ DOX framework + read-before-edit
    ├─ Work Guidance (Two-Mode Workflow, Commands, Sprint Model)
    └─ References CONSTITUTION.md for governance details

[Retired/Obsolete]
✗ WORKFLOW.md (deleted)
✗ .github/workflows/mavis-coordination/ (deleted)
✗ docs/personas/siti.md (archived)
✗ docs/personas/dev.md (archived)
```

---

## 🧪 Test للـ Mode 1 → Mode 2 بعد Sprint 18

بعد Admin يخلّص Mode 1 + يقول "ادفع"، نفس الـ T2 verify اللي عملته:

1. Mode 2 push → CI 6/6 → merge → tag
2. Cron يدور (الـ main worktree config) → rebuild + 9/9 smoke
3. **Telegram ping** — لو وصل، الـ cycle يثبت إن الـ governance cleanup ما كسر الـ workflow

---

## 📝 ملاحظات إضافية

1. **`docs/architecture/holding-company-architecture.md`** هو single source of truth للمعمارية (per AGENTS.md). ما يحتاج تعديل — هو canonical.

2. **Sprint 18 = governance-only sprint.** ما في كود backend/frontend. مفيش risk. بس يخلّي النظام نظيف ذهنياً للـ future sprints.

3. **خيار الـ "archive" vs "delete"**: للملفات اللي فيها historical value (Sprint 9 demo, sprints 12-13 hand-offs)، نخلّيها في مكانها. للملفات اللي **تكسر الـ mental model** (WORKFLOW.md, mavis-coordination, persona roles), نمسحها.

4. **التوقيت:** الـ delete operations + header updates ~ 30 دقاي. CI ~ 2 دقاي. Telegram ping ~ 5 دقاي. **الـ total ~40 دقاي من Mode 1 → Mode 2 done.**

5. **Sprint 18 = الـ 6th sprint في الـ workflow.** مفيش memory limit، كل شي بينحفظ.

---

## ❓ الخيارات أمام Anas

| الخيار | معناه |
|--------|-------|
| **"ابدأ Sprint 18 (Option A)"** | أبدأ Mode 1 الحين — CONSTITUTION + delete + AGENTS + personas |
| **"ابدأ بس Option C"** | Headers + delete فقط (أسرع، ~45 دقاي) |
| **"خليك، استنى"** | نأجل، نشتغل على feature جديد بدال governance |

**توصية محمد: ابدأ Sprint 18 (Option A)** — governance نظيف يجهّزنا للسبرنتات الجاية feature.
