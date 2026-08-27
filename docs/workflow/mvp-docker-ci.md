# MVP Docker CI Build (Layer 2 of 3-Layer Model)

**Sprint 66 (DEC-241)** — per Anas 2026-08-27 21:30 UTC+2 directive.

## ما الجديد

GitHub Actions workflow يبني ويختبر `mvp-docker` تلقائياً بعد كل merge إلى `develop`.
يحل مشكلة الـ Docker daemon المتوقف على جهاز Anas المحلي.

## كيف يعمل

| المرحلة | ما يحدث | الوقت |
|---|---|---|
| 1. **Build API image** | `docker compose build api` (Release config) | ~2 min |
| 2. **Build Frontend image** | `docker compose build frontend` (production build) | ~2 min |
| 3. **Start the stack** | postgres + api + frontend (clean volumes) | ~30s |
| 4. **Run smoke test** | نفس `smoke-test.ps1` (9 checks) | ~30s |
| 5. **Tear down** | `docker compose down -v` (drop test volume) | ~5s |

**Total: ~5 min** على GitHub runners (vs 10-15 min على جهاز Anas المحلي).

## الفحوصات التسعة (من smoke-test.ps1)

1. **API health: `/api/health/live`** (200)
2. **API health: `/api/health/ready`** (200)
3. **Login**: `POST /api/auth/login` (admin bootstrap user)
4. **Frontend**: `GET /` (يعرض HTML)
5. **DB clean**: 1 company فقط (bootstrap Holding، لا seed)
6. **DB admin user**: موجود في `users` table (created by `DefaultHoldingBootstrapHostedService`)
7. **Swagger disabled**: في Production (intentional)
8. **Dashboard 200**: `/api/dashboard/summary` (Admin role assigned)
9. **Demo data seeded**: ≥3 customers, ≥3 vendors, ≥5 items

## Triggers

| Trigger | متى يعمل |
|---|---|
| `push: branches: [develop]` | بعد كل M2 merge (Mode 2) |
| `workflow_dispatch` | يدوي (لـ ad-hoc verification) |

## Sprint Closure Lock

Per **Constitution Article 9 + 3-Layer Model**, الـ Layer 2 verification أصبح الآن
**automatic sprint closure lock**:
- قبل: يدوي — Anas يشغّل `cd mvp-docker && ./smoke-test.ps1` محلياً
- بعد: تلقائي — GitHub Actions يبني ويختبر بعد كل merge

**النتيجة**:
- ✅ Sprint مُقفل → workflow green → Anas يحصل على رابط الـ successful run
- ❌ Sprint NOT مُقفل → workflow red → Anas يراجع الـ logs

## استبدل

- ❌ `mvp-auto-rebuild-on-develop-push` cron (Docker daemon issues)
- ❌ `cd mvp-docker && docker compose up -d --build && ./smoke-test.ps1` يدوياً

## ملاحظات

1. **PowerShell في CI**: نثبّت `pwsh` على `ubuntu-latest` ونشغّل `smoke-test.ps1` نفسه
   (single source of truth — نفس الـ script يعمل محلياً وفي CI).
2. **Service container بدل docker-in-docker**: نستخدم GitHub runner's Docker
   (لا حاجة لـ `docker-in-docker` أو DinD).
3. **Volume cleanup**: نعمل `down -v` دائماً (حتى على الفشل) لتفريغ disk GH runner.
4. **JWT secret**: CI يستخدم `CI_MVP_DOCKER_SECRET_*` ثابت — آمن للـ testing فقط.
5. **No data persistence**: كل run يبدأ بـ volume جديد — يضمن الـ "clean install"
   المطلوب من Sprint 14 (Layer 2 = client deliverable، لا data تتراكم).

## كيف تتابع

- **GitHub UI**: `Actions` tab → workflow "MVP Docker Build (Layer 2)"
- **Status badge** (قريباً): يمكن إضافة badge في README لـ mvp-docker/
- **Telegram ping**: عند الفشل، workflow يطبع `::error::` (يمكن ربطه بـ Telegram bot
  لاحقاً)

## Related

- `.github/workflows/mvp-docker-build.yml` — الـ workflow نفسه
- `mvp-docker/smoke-test.ps1` — الـ 9 checks (single source of truth)
- `mvp-docker/docker-compose.yml` — الـ stack (postgres + api + frontend)
- `AGENTS.md` (root) — 3-Layer Model section

---

*Added 2026-08-27 by Mavis (Admin Team) — Sprint 66 (DEC-241) — per Anas directive.*
