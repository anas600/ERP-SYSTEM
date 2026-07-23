# 🗺️ خطة العمل المحلية — ERP-SYSTEM (2026-07-23 FINAL)

> **المالك:** anas600 (anasassaket@gmail.com)
> **المنسق:** Mavis (root session)
> **الفريق:** Jamie Executive (coder) + Jamie Analytical (verifier)
> **الحالة:** ✅ Release v5.0 deployed to main (HF Space fresh build live)

---

## 🎯 الإنجاز (TL;DR)

| الخطوة | الحالة | ملاحظة |
|--------|--------|--------|
| **Local sync** (232 commits) | ✅ | develop = `48d61ed` → `4be8f4c` |
| **PR #126** (CI trigger) | ✅ merged | develop → main trigger |
| **PR #128** (CodeQL cleanup) | ✅ merged | SoftDeleteController refactor + seeders OFF |
| **PR #127** (Release v5.0) | ✅ merged | 250 commits → main (`c9c662c`) |
| **PR #129** (docs sync) | ✅ merged | AGENTS.md + CHANGELOG.md updated |
| **HF Space deploy** | ✅ | Run #16 success (3.5 min) |
| **Branch protection** | ✅ restored | `Build and Deploy to HF` + 1 approval |

---

## 🌐 ما الجديد في v5.0

**من `AGENTS.md` (Phase Status):**

| Phase | الحالة |
|-------|--------|
| Phase 0–4 (Identity → Payroll + EOS) | ✅ مكتمل |
| **Phase 5.A Sprint 1** (AR: Customers + SalesInvoices + Receipts) | ✅ |
| **Phase 5.A Sprint 2** (AP Payments + Finance Reports rebuild + **Fresh Build Mode**) | ✅ |

**الـ Fresh Build Mode (جديد):**
- HF Space يبدأ بـ DB فارغ (لا AlFajr/AlBurj/Realistic)
- فقط DefaultCoASeed + DefaultInventorySeed كمرجع
- المالك يسجل أول مستخدم حقيقي عبر `/api/auth/register`

---

## 🏗️ البنية الحالية

| | قبل | بعد |
|---|-----|-----|
| **develop branch** | local-only (Mavis + team tests) | ✅ local-only |
| **main branch** | عند Phase 4 (24 يونيو) | ✅ عند Release v5.0 |
| **CI trigger** | `push: develop` | ✅ `push: main` |
| **HF Space** | AlFajr demo data | ✅ Fresh empty (owner registers first) |
| **Seeders** | auto-run AlFajr | ✅ disabled (default OFF) |
| **CodeQL** | 7 high-sev SQL injection FP | ✅ clean (switch/case refactor) |

---

## 👥 الفريق (Team Pattern)

| الدور | الـ Agent | المهام |
|------|---------|--------|
| **Mavis (orchestrator)** | `mavis` (root session) | Plan, review PRs, merge to main, oversee team, manage work tree |
| **Jamie Executive** (تنفيذي) | `coder` (worker) | Refactor, features, bug fixes, commit + push + open PR |
| **Jamie Analytical** (تحليلي) | `verifier` (worker) | PR review, smoke tests, audit, build verify, deploy monitoring |

**القاعدة:** Mavis يدير العمل + يدمج. Jamie ينفّذ + يفتح PR. Jamie يراجع + يـ verify.

---

## 🖥️ الـ Local Dev Workflow

**الـ script الأساسي:** `start-dev.ps1` (root level)
- 10s cold start (محسّن للـ 6GB RAM)
- Backend + Frontend يبدأان parallel (detached processes)
- PostgreSQL 15 محلي (Windows service)
- بدون Docker (توفير RAM)

**خطوات التشغيل:**
```powershell
net start postgresql-x64-15  # لو مش شغّال
cd "C:\Users\Anas\.minimax-agent\projects\ERP-SYSTEM"
.\start-dev.ps1
```

**ما يعمله الـ script:**
- يـ cleanup ports 5000 + 3000
- يـ start backend + frontend (detached)
- يـ wait for ready (15s max)
- المالك يفتح `http://localhost:3000`

---

## 🐳 Docker (للـ Production / الفريق)

`infra/docker/docker-compose.dev.yml` موجود + Dockerfile في الـ root. **مش مستخدم محلياً** (الجهاز 6GB) لكن:
- يحتفظ به للـ production / CI
- يقدر يـ push كـ image مباشرة للـ HF Space عبر `build-and-deploy-hf.yml`

---

## 🔐 CI/CD Pipeline (DEC-062)

| Trigger | Workflow | Effect |
|---------|----------|--------|
| `push: main` | `build-and-deploy-hf.yml` | Auto-deploy to HF Space (staging) |
| `push: develop` | `ci-deploy.yml` | Auto-sync to HF Space (dev iteration) |
| `workflow_dispatch` | Both | Manual deploy |

**Branch protection على main:**
- Required status: `Build and Deploy to HF` ✅
- Required reviews: 1
- Linear history, no force push, enforce admins

---

## 📋 الـ Open PRs / Branches

| Branch | الـ status |
|--------|------------|
| `develop` | ✅ نظيف (في `114d104`) |
| `main` | ✅ نظيف (في `c9c662c` — Release v5.0) |
| `feature/phase-5-ar` | ⚠️ stale — يُحذف عند أول cleanup |
| `remotes/origin/feature/dec-088-4-entities` | ⚠️ unused |
| `remotes/origin/feature/dec-088-clean` | ⚠️ unused |
| `remotes/origin/feature/dec067-sprint-improvements` | ⚠️ deleted via PR #115 merge |

---

## ✅ Verification Checklist

- [x] Local develop = remote develop (`114d104`)
- [x] Local main = remote main (`c9c662c`)
- [x] PR #126 (CI trigger change) merged
- [x] PR #128 (CodeQL cleanup + seeders disable) merged
- [x] PR #127 (Release v5.0) merged
- [x] PR #129 (docs sync) merged
- [x] HF Space deploy Run #16 success
- [x] HF Space `/api/health/ready` → 200 healthy
- [x] HF Space login page appears in browser
- [x] AlFajr login fails (expected — fresh build)
- [x] CodeQL clean (7 high-sev SQL FP resolved)
- [x] All 7 CI checks passing on PR #129
- [x] Branch protection restored (status check + 1 review)
- [x] AGENTS.md + CHANGELOG.md updated

---

## 🎯 Next Steps (اقتراحات)

| # | المهمة | الـ priority | الحجم |
|---|--------|-------------|-------|
| 1 | المالك يسجل أول مستخدم عبر `/api/auth/register` | 🔴 high | 1 min |
| 2 | حذف branches القديمة (`feature/phase-5-ar`, `dec-088-4-entities`, إلخ) | 🟡 medium | 5 min |
| 3 | Stash review مع Jamie Analytical (`stash@{0}` فيه APAging fix) | 🟢 low | 10 min |
| 4 | تنظيف `appsettings.Development.json` للـ local (seeders = false) | ✅ done | — |
| 5 | إضافة `ci-deploy.yml` trigger change (develop → main) | 🟡 medium | 5 min |
| 6 | Phase 5.B (feature جديد) | 🔵 backlog | — |

---

## 🔗 روابط مهمة

- **HF Space:** https://anas-assaket-erp-system.hf.space
- **Repo:** https://github.com/anas600/ERP-SYSTEM
- **PRs الأخيرة:**
  - #129: docs sync
  - #128: cleanup
  - #127: Release v5.0
  - #126: CI trigger change
  - #125: ::text cast fix
- **AGENTS.md:** root + per-module
- **CHANGELOG.md:** `docs/CHANGELOG.md` (2026-07-23 entry)
- **.mavis/plans/phase-5-plan.yaml:** snapshot للـ plan الأصلي

---

**آخر تحديث:** 2026-07-23 22:50 EET — كتبها Mavis بعد Release v5.0 deploy
