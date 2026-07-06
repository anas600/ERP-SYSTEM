# 📝 CHANGELOG — ERP-SYSTEM

> سجل التغييرات الموثّقة. **آخر إدخال في الأعلى.**

---

## 2026-07-06 — Sprint-4: Preventive Hardening + Observability ✅

### 🎯 الهدف
Sprint-4 — تعزيز النظام ضد حوادث الـ startup (DEC-009 style) + إضافة observability للإنتاج.

### 📊 ملخص الإنجاز
- **11 commits** عبر 4 أيام
- **Backend:** 2 utilities (Batch + Retry) + 1 AdminController + 1 Middleware + 1 seeder refactor
- **Frontend:** 1 fix (useAuth infinite loop)
- **Tests:** 12 unit tests (5 Batch + 6 Retry + 1 middleware)
- **Documentation:** README + CHANGELOG updated
- **DEC-009 defense:** 4 layers active (config flag + DI check + endpoint 501 + class deleted)

### 📅 Day-by-Day

| Day | Focus | Commits |
|-----|-------|---------|
| 1 | Health endpoints + seeder feature flags | `8880454` `c5db35a` `34aa094` `14723bd` |
| 2 | Manual triggers + Batch + Retry + tests + integration | `83e8a45` `c496395` `f489ec6` |
| 3 | Structured logging + Request tracking + Sentry | `d373c9b` `108b16c` `db7d1ea` |
| 4 | Documentation + middleware tests | `884951a` |

### 🛡️ Defense-in-Depth (DEC-009 Prevention)

| Layer | Mechanism |
|-------|-----------|
| 1 | `SeedAlBurjScenario=false` default in appsettings |
| 2 | Program.cs doesn't register AlBurjHostedService |
| 3 | `POST /api/admin/seed/alburj` returns 501 |
| 4 | AlBurjSeederHostedService class deleted from codebase |

### 📡 Observability Stack

| Component | Production Value |
|-----------|------------------|
| Health endpoints | K8s/UptimeRobot probes |
| Structured JSON logs | Loki/Elasticsearch ingestion |
| X-Request-ID | Trace across requests |
| LogContext enrichers | Tenant/User filtering |
| Sentry (optional) | Real-time error tracking |

### 📝 الملفات الرئيسية الجديدة

| File | Purpose |
|------|---------|
| `src/backend/Host/Controllers/HealthController.cs` | `/live`, `/startup`, `/startup-deep` |
| `src/backend/Host/Controllers/AdminController.cs` | Manual seed triggers (Admin role) |
| `src/backend/Host/Utilities/BatchInsertHelper.cs` | 1000 records/batch extension method |
| `src/backend/Host/Utilities/RetryPolicy.cs` | 3 retries + exponential backoff |
| `src/backend/Host/Middleware/RequestTrackingMiddleware.cs` | X-Request-ID + LogContext |
| `src/frontend/lib/useAuth.ts` | Fixed React #185 infinite loop |

### 🎯 NEXT: Sprint-5

- Enable MartenDB event sourcing (DEC-017)
- DB rename: `erp-events` → `erp_events` (Neon)
- Complete MVP per FRD-aligned plan (`docs/MVP-COMPLETION-PLAN.md`)

---
- **DEC-009 defense:** 4 layers active (config flag + DI check + endpoint 501 + class deleted)

### 📅 Day-by-Day

| Day | Focus | Commits |
|-----|-------|---------|
| 1 | Health endpoints + seeder feature flags | `8880454` `c5db35a` `34aa094` `14723bd` |
| 2 | Manual triggers + Batch + Retry + tests + integration | `83e8a45` `c496395` `f489ec6` |
| 3 | Structured logging + Request tracking + Sentry | `d373c9b` `108b16c` `db7d1ea` |
| 4 | Documentation + middleware tests | `884951a` |

### 🛡️ Defense-in-Depth (DEC-009 Prevention)

| Layer | Mechanism |
|-------|-----------|
| 1 | `SeedAlBurjScenario=false` default in appsettings |
| 2 | Program.cs doesn't register AlBurjHostedService |
| 3 | `POST /api/admin/seed/alburj` returns 501 |
| 4 | AlBurjSeederHostedService class deleted from codebase |

### 📡 Observability Stack

| Component | Production Value |
|-----------|------------------|
| Health endpoints | K8s/UptimeRobot probes |
| Structured JSON logs | Loki/Elasticsearch ingestion |
| X-Request-ID | Trace across requests |
| LogContext enrichers | Tenant/User filtering |
| Sentry (optional) | Real-time error tracking |

### 📝 الملفات الرئيسية الجديدة

| File | Purpose |
|------|---------|
| `src/backend/Host/Controllers/HealthController.cs` | `/live`, `/startup`, `/startup-deep` |
| `src/backend/Host/Controllers/AdminController.cs` | Manual seed triggers (Admin role) |
| `src/backend/Host/Utilities/BatchInsertHelper.cs` | 1000 records/batch extension method |
| `src/backend/Host/Utilities/RetryPolicy.cs` | 3 retries + exponential backoff |
| `src/backend/Host/Middleware/RequestTrackingMiddleware.cs` | X-Request-ID + LogContext |
| `src/frontend/lib/useAuth.ts` | Fixed React #185 infinite loop |

### 🎯 NEXT: Sprint-5

- Enable MartenDB event sourcing (DEC-017)
- DB rename: `erp-events` → `erp_events` (Neon)
- Complete MVP per FRD-aligned plan (`docs/MVP-COMPLETION-PLAN.md`)

---