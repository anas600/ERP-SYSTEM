# Ononms Workflow — M3 Trust Mode Verification

**Muhammad's M3 Trust Mode workflow for ERP-SYSTEM**

**Owner:** Muhammad (Mavis mode) &nbsp;·&nbsp; **Created:** 2026-08-27 &nbsp;·&nbsp; **Status:** Active
**Trigger:** After every successful M2 push (sprint closure) OR when Anas requests deep verification
**Distinguishes from previous workflows:**
- M2 push workflow (Sprint 61→65) = mechanical git operations
- **Ononms workflow (this)** = strategic verification from 4 expert hats + العميل البشيري persona

---

## 🎯 الهدف

التحقق من أن السبرنت المكتمل فعلياً:
1. **يعمل** (ما فيه bug في الـ integration)
2. **مفيد** (يخدم الـ 4 شخصيات في الواقع)
3. **آمن** (RBAC + L19 + L95 صحيحة)
4. **جاهز للعميل** (العميل البشيري يفهمه بدون تدريب)

**الناتج:** Notion Trust Mode Report page + 1+ lessons جديدة + (عند الحاجة) quick fixes

---

## 👥 4 شخصيات التحقق (Expert Hats)

### 1. 💰 **المحاسب (Accountant) — IFRS/IAS Expert**
- يتفقد: CoA + Journal + PostingRules + Reports
- السيناريو: مستخلص معتمد → قيد محاسبي → قائمة دخل → ميزان مراجعة
- يفحص: NDB 1.5% + Stamps + CIT + SS (DEC-NEW-5)
- علامة: AR auto-posts + Project Revenue auto-posts (Sprint 65)

### 2. 🏗️ **مهندس المشاريع (Project Engineer) — Construction Expert**
- يتفقد: Projects + Tasks + BOQ + Billing + Subcontractor
- السيناريو: مشروع جديد → BOQ → Variation Order → Progress Billing → Sub-Payment
- يفحص: NDB regional premium + retention + advance deduction
- علامة: P&L per project (Sprint 65) + Sub-Statement (Sprint 64)

### 3. 🎨 **خبير التصميم (Design Expert) — UX Architect**
- يتفقد: SmartSidebar (Sprint 63) + module visibility + navigation + info architecture
- السيناريو: يتصفح كمحاسب → ثم كمدير → ثم كمدخل بيانات
- يفحص: هل sidebar يخفي modules حسب الدور؟ هل navigation منطقي؟
- علامة: 0 modules ظاهرة بدون داعي (RBAC) + breadcrumbs صحيحة

### 4. 🖥️ **خبير الـ UI/Frontend — Accessibility & Components**
- يتفقد: components (Card, Badge, StatusPill, SectionCard) + responsive + Arabic RTL + fonts
- السيناريو: يفتح على mobile + desktop + tablet
- يفحص: خط Cairo (بديل Outfit للعربية) + spacing + colors + dark mode
- علامة: 0 horizontal scroll + 0 broken layout

---

## 👨‍💼 الـ 5th Persona: العميل البشيري (Al-Bashiri Client)

**السيناريو الواقعي الكامل** (الـ acceptance test):
- Anas يفتح النظام كـ `admin@erp.local`
- يدخل 1 Holding + 3 شركات تابعة (شركة الإعمار + البركة + السلام)
- يدخل 5 مشاريع (مبنى 1 + مبنى 2 + طريق + جسر + صيانة)
- يدخل 10 بنود BOQ
- يدخل 3 عملاء + 3 موردين
- يولّد 1 progress billing → يوافق → يستلم القيد
- يصدر PDF
- يطابق receipt مع sub-payment

**النتيجة:** لو كل هذا يعمل من المحاولة الأولى بدون مساعدة → "Sprint demo-ready".

---

## 🔄 الـ Loop (دورة Ononms)

```
┌────────────────────────────────────────────┐
│  PHASE 1: Setup                            │
│  ──────────                                │
│  - تأكد: mvp-docker CI smoke (9/9)         │
│  - تأكد: develop @ latest sprint tag        │
│  - تأكد: in-app browser → localhost:3000   │
└────────────────┬───────────────────────────┘
                 ▼
┌────────────────────────────────────────────┐
│  PHASE 2: Browse + Login                   │
│  ─────────────────────                     │
│  - افتح المتصفح (in-app أو خارجي)         │
│  - سجّل دخول كـ admin                       │
│  - تحقق من: sidebar يعرض كل الـ 9 modules │
└────────────────┬───────────────────────────┘
                 ▼
┌────────────────────────────────────────────┐
│  PHASE 3: Per-Hat Scenarios                │
│  ────────────────────────                  │
│  4 أدوار × 3-5 سيناريوهات لكل دور          │
│  (انظر أدناه)                              │
│  وثّق: pass / fail / blocked لكل سيناريو  │
└────────────────┬───────────────────────────┘
                 ▼
┌────────────────────────────────────────────┐
│  PHASE 4: العميل البشيري (Acceptance)     │
│  ─────────────────────────────────          │
│  سيناريو كامل end-to-end من الصفر          │
│  (انظر أدناه)                              │
└────────────────┬───────────────────────────┘
                 ▼
┌────────────────────────────────────────────┐
│  PHASE 5: Quick Fixes                      │
│  ────────────────────                       │
│  - FE hot-fix (≤ 30 min لكل fix)           │
│  - BE: سجّل كـ Sprint X+1 issue            │
│  - لا تغيّر schema في Ononms               │
└────────────────┬───────────────────────────┘
                 ▼
┌────────────────────────────────────────────┐
│  PHASE 6: Re-verify                        │
│  ───────────────────                       │
│  - أعد السيناريوهات الفاشلة               │
│  - وثّق: pass / fail                       │
└────────────────┬───────────────────────────┘
                 ▼
┌────────────────────────────────────────────┐
│  PHASE 7: Document                         │
│  ────────────────                          │
│  - أنشئ Notion Trust Mode Report page     │
│  - احفظ L### جديد في memory                │
│  - حدّث Notion Hub status                  │
│  - حدّث CHANGELOG.md (قسم Trust Mode)     │
└────────────────────────────────────────────┘
```

---

## 📋 Per-Hat Scenarios (Sprint 65 — العميل البشيري)

### 💰 المحاسب — 5 سيناريوهات

| # | السيناريو | المسار | النجاح المتوقع |
|---|---|---|---|
| A1 | Login + فتح Dashboard | `/auth/login → /dashboard` | KPIs ظاهرة (1 company, 1 user) |
| A2 | فتح شجرة الحسابات | `/finance/accounts-tree` | 79 حساب (52 + 27) مع L3/L4 tags |
| A3 | توليد تقرير P&L | `/finance/reports/income-statement` | فلاتر cost_center + project + date |
| A4 | اعتماد Progress Billing | API `POST /api/projects/{id}/billings/{id}/approve` | القيد يظهر في `/finance/journals` |
| A5 | تصدير PDF | `GET /api/projects/{id}/billings/{id}/pdf` | ملف PDF ينزل مع QR code |

### 🏗️ مهندس المشاريع — 5 سيناريوهات

| # | السيناريو | المسار | النجاح المتوقع |
|---|---|---|---|
| E1 | فتح قائمة المشاريع | `/projects` | 5+ مشاريع من Sprint 60 |
| E2 | BOQ لعناصر المشروع | `/projects/{id}/boq` | قائمة BOQ + إضافة/تعديل |
| E3 | تسجيل تقرير المهندس | `/projects/{id}/engineer-reports/new` | draft → submit → signoff |
| E4 | مستخلص باطن جديد | `/projects/{id}/subcontractors/{subId}/billings/new` | calc: gross - retention - advance = net |
| E5 | عرض P&L المشروع | `/projects/{id}/pnl` | 5-facet cost + profit margin |

### 🎨 خبير التصميم — 5 سيناريوهات

| # | السيناريو | المسار | النجاح المتوقع |
|---|---|---|---|
| D1 | تصفح كـ Admin | login → dashboard | 9 modules ظاهرة |
| D2 | تصفح كـ Accountant (مستخدم وهمي) | تبديل role | 6 modules ظاهرة (Companies مخفي) |
| D3 | تصفح كـ Project Engineer | تبديل role | 5 modules ظاهرة (Finance P&L only) |
| D4 | Breadcrumbs | أي صفحة detail | 3 levels (Dashboard > Projects > {id} > {name}) |
| D5 | Mobile responsive | تصغير viewport 375px | 0 horizontal scroll + sidebar collapsed |

### 🖥️ خبير الـ UI/Frontend — 5 سيناريوهات

| # | السيناريو | المسار | النجاح المتوقع |
|---|---|---|---|
| U1 | خط Cairo للعربية | أي صفحة | text rendering صحيح، 0 tofu boxes |
| U2 | RTL layout | أرقام إنجليزية في صفحة عربية | أرقام LTR inline (per Unicode) |
| U3 | Card components | dashboard / projects | consistent spacing + borders |
| U4 | Status pills | billing + journal | green/blue/red semantic colors |
| U5 | Form validation | أي form | inline errors, no alert() popups |

### 👨‍💼 العميل البشيري — 7 خطوات end-to-end

| # | الخطوة | الإجراء | النجاح المتوقع |
|---|---|---|---|
| C1 | Bootstrap Holding | login as admin | admin user created by env |
| C2 | إضافة شركة تابعة | `/companies/new` | company saved, holding_parent_id set |
| C3 | إضافة مشروع | `/projects/new` | project with budget + cost center |
| C4 | إضافة بند BOQ | `/projects/{id}/boq` | item with quantity × unit price |
| C5 | إضافة عميل + فاتورة | `/ar/customers/new` + `/ar/invoices/new` | AR control + revenue auto-posted |
| C6 | اعتماد progress billing | `/projects/{id}/billings/{id}/approve` | journal entry created automatically |
| C7 | عرض Dashboard cross-module | `/dashboard/cross-module` | Project Profitability + AR + AP visible |

---

## 🔧 Quick Fixes — الحدود

✅ **مسموح في Ononms (FE hot-fix ≤ 30 min):**
- typo في labels / placeholders
- missing optional chaining (`x?.y` بدل `x.y`)
- wrong Arabic translation
- missing meta tags / page title
- broken link to another page
- color contrast issue
- mobile responsive breakage

❌ **غير مسموح (سجّل كـ Sprint X+1):**
- أي schema change (ALTER TABLE)
- أي migration جديدة
- أي feature جديد
- أي DEC غير مُعتمد
- breaking change في API
- تغيير RBAC permissions (DEC-NEW-X)

---

## 📊 Deliverables (لكل Ononms run)

| Deliverable | الموقع | Format |
|---|---|---|
| Notion Trust Mode Report | Hub > "✅ Trust Mode Report — Sprint N" | Notion page |
| Lessons جديدة | Memory + Notion Lessons DB | L### entry |
| Quick fixes (إن وُجدت) | `feature/ononms-sprint-N-quickfixes` branch | Git commits |
| CHANGELOG entry | `docs/CHANGELOG.md` | Markdown |
| Hub status update | Notion Hub page top | Edit |

---

## 🕐 متى يُنفَّذ Ononms؟

| Trigger | الوصف |
|---|---|
| بعد كل M2 push | Automatic — Cron يفحص develop HEAD tag، إذا سبرنت جديد، يبدأ Ononms |
| عند طلب Anas | "شغّل Ononms لـ Sprint N" → Muhammad يبدأ |
| بعد إصلاحات كبيرة | إذا تم إصلاح 3+ bugs، Muhammad يُعيد Ononms |

---

## 📜 Per-Sprint Reports (الـ archive)

- **Sprint 60 Ononms Report** → [Notion](https://app.notion.so/3c8c003bf39681218197fec2fd5db02e) (15/15 DEC verified + 1 bug fixed)
- **Sprint 65 Ononms Report** → (mvp-docker CI evidence: 9/9 smoke checks passed in 3m 9s — full browser test deferred to CI verification + manual spot check)
- **Sprint 66 Ononms Report** → (in progress: workflow definition)

---

## 🔗 Related Docs

- `docs/workflow/sprint-60.md` (Hand-off)
- `docs/CHANGELOG.md` (sprint history)
- `AGENTS.md` (root) — Constitution + 3-Layer Model
- `mvp-docker/smoke-test.ps1` (automated equivalent — 9 checks)
- `.github/workflows/mvp-docker-build.yml` (CI layer 2 verification)
- [Sprint 60 Trust Mode Report](https://app.notion.so/3c8c003bf39681218197fec2fd5db02e)

---

*Created 2026-08-27 by Muhammad (Mavis) — Sprint 66 (DEC-242) — per Anas directive.*
*Named "Ononms" to distinguish from previous M2 push workflow (Sprint 61→65).*
