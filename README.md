# 🏢 ERP-SYSTEM

نظام ERP متكامل (MVP) يتكون من **7 وحدات**: Identity + Companies + Finance + Projects + Inventory + Reports + Notifications.

> **الحالة الحالية:** Phase 2.5+ — Frontend integration مكتمل (8 صفحات + Auth)
> نشتغل على Multi-tenant Modular Monolith قابل للتحويل إلى Microservices لاحقاً.

> **Setup محلي بدون Docker:** راجع [`docs/SETUP-LOCAL.md`](docs/SETUP-LOCAL.md) (دليل مُبسَّط للتشغيل على Windows/macOS/Linux بدون `docker compose`).

---

## 🎯 الهدف

إطلاق نظام ERP قابل للاستخدام على VPS، مبني على Modular Monolith + Event-Driven Architecture.

---

## 🛠️ التقنيات

| الطبقة | التقنية | السبب |
|--------|---------|-------|
| Backend | ASP.NET Core 9 (C#) | Modular Monolith + Vertical Slices |
| Database (OLTP) | PostgreSQL 16 + Dapper | تحكم دقيق بالـ SQL وأداء عالٍ |
| Migrations | FluentMigrator | SQL-first، مناسب لـ Dapper |
| Event Store | MartenDB (Postgres-backed) | Event Sourcing بدون تعقيد Kafka |
| Cache/Queue | Redis 7 | Session / Cache / Pub-Sub |
| Auth | JWT (Access + Refresh) + BCrypt | معيار صناعي + Token Rotation |
| Frontend | Next.js 14 + TypeScript + shadcn/ui | SSR + DX ممتاز |
| API Docs | Swagger / OpenAPI 3 | تجربة مطوّر سلسة |
| Container | Docker + Compose | تشغيل موحّد عبر البيئات |
| CI/CD | GitHub Actions | build + test + docker image |

---

## 🏗️ البنية المعمارية

```
┌─────────────────────────────────────────────────┐
│              Next.js Frontend (3000)            │
└─────────────────────┬───────────────────────────┘
                      │ REST + JWT Bearer
┌─────────────────────▼───────────────────────────┐
│         ASP.NET Core Modular Monolith           │
│  ┌──────────┐ ┌──────────┐ ┌──────────────┐    │
│  │ Identity │ │ Finance  │ │   Projects   │    │
│  │  (Phase 0)│ │ (Phase 1)│ │   (Phase 2)  │    │
│  └──────────┘ └──────────┘ └──────────────┘    │
│  ┌──────────────────────────────────────────┐   │
│  │        Inventory Module (Phase 2)        │   │
│  └──────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────┐   │
│  │   Shared: TenantContext, Events, Logging │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────┬───────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
   ┌────────┐   ┌──────────┐   ┌─────────┐
   │ Postgres│  │  MartenDB │   │  Redis  │
   │ (OLTP)  │  │ (Events)  │   │ (Cache) │
   └────────┘   └──────────┘   └─────────┘
```

---

## 📁 هيكل المشروع

```
ERP-SYSTEM/
├── docs/
│   ├── AGENTS.md           # توثيق هذا المجلد
│   └── PLAN.md             # الخطة الكاملة 8-10 أسابيع
├── src/
│   ├── backend/
│   │   ├── Host/           # نقطة الدخول: Program.cs, Controllers, Swagger
│   │   ├── Modules/
│   │   │   ├── Identity/   # Users, Roles, Tenants, Auth flows
│   │   │   ├── Finance/    # (Phase 1)
│   │   │   ├── Projects/   # (Phase 2)
│   │   │   └── Inventory/  # (Phase 2)
│   │   ├── Shared/         # Infrastructure, MultiTenancy, Migrations, Events
│   │   ├── Tests/          # xUnit test projects
│   │   └── Dockerfile      # multi-stage build
│   └── frontend/           # Next.js 14
├── infra/
│   ├── docker/             # docker-compose.dev.yml + init-scripts
│   └── .github/workflows/  # CI
└── AGENTS.md               # ⭐ توثيق الجذر
```

---

## 🚀 البدء السريع (Phase 0)

### 1) المتطلبات

- Docker 24+ و Docker Compose
- .NET 9 SDK (للتطوير خارج Docker)
- Node 20+ (للتطوير خارج Docker)

### 2) تشغيل البنية التحتية

```bash
# من جذر المشروع
docker compose -f infra/docker/docker-compose.dev.yml up -d postgres redis
```

هذا يخلق:
- **Postgres** على `localhost:5432` بقاعدتي بيانات: `erp_system` (OLTP) و `erp_events` (Marten)
- **Redis** على `localhost:6379`

### 3) تشغيل الـ Backend

**عبر Docker (موصى به):**
```bash
docker compose -f infra/docker/docker-compose.dev.yml up -d --build api
# الـ migrations تشتغل تلقائياً عند أول تشغيل
# الـ API على http://localhost:5000
# Swagger على http://localhost:5000/swagger
```

**محلياً (خارج Docker):**
```bash
cd src/backend/Host
dotnet run
```

### 4) تجربة Auth

```bash
# 1) تسجيل أول مستخدم (يُنشئ tenant جديد تلقائياً)
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "tenantName": "Acme Corp",
    "email": "admin@acme.com",
    "password": "Strong1Pass",
    "fullName": "Acme Admin"
  }'

# 2) تسجيل دخول
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@acme.com", "password": "Strong1Pass" }'

# 3) معلومات المستخدم الحالي
curl http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer <access_token>"
```

### 5) Health Checks

```bash
# Liveness — عملية خفيفة
curl http://localhost:5000/health

# Readiness — يفحص Postgres + Redis
curl http://localhost:5000/health/ready
```

---

## 🧪 الاختبارات

```bash
cd src/backend/Tests/ERPSystem.Tests
dotnet test
```

15 اختبار يغطون:
- JWT Token generation & validation
- Refresh token rotation & reuse detection
- BCrypt password hashing
- FluentValidation على Register/Login/Refresh requests
- Multi-tenancy claim resolution

---

## 📊 الـ Modules

| Module | الوصف | الحالة |
|--------|-------|--------|
| **Identity** | Multi-tenant users, roles (RBAC), JWT auth, refresh tokens | ✅ Phase 0 (90%) |
| **Finance** | Chart of Accounts, Journal Entries, Invoices, Payments, Rules Engine | 📋 Phase 1 |
| **Projects** | Projects, Tasks, Budgets, Resources | 📋 Phase 2 |
| **Inventory** | Items, Warehouses, Stock Movements, Valuation | 📋 Phase 2 |
| **Shared** | MultiTenancy, Event Bus, Logging, Migrations | ✅ Phase 0 (60%) |

---

## 🔐 خيارات Auth

- **Access Token**: JWT قصير (60 دقيقة افتراضياً)، يحوي `tenant_id` + roles
- **Refresh Token**: عشوائي 256-bit، مُجزّأ (SHA-256) في DB، صالح 14 يوم
- **Token Rotation**: كل refresh يُلغي القديم ويُولّد جديد
- **Reuse Detection**: استخدام refresh token ملغى = هجوم → نُلغي جميع جلسات المستخدم
- **Multi-tenancy**: tenant_id من JWT، الـ `TenantContext` يضمن عزل البيانات

---

## 📜 الترخيص

Private — جميع الحقوق محفوظة © 2026

## 📞 التواصل

- GitHub: [@anas600](https://github.com/anas600)
- المشروع: [anas600/ERP-SYSTEM](https://github.com/anas600/ERP-SYSTEM)
