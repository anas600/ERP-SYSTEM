# 🐳 infra/docker/AGENTS.md

> Docker Compose للتطوير + init scripts.

## شو فيه

```
docker/
├── docker-compose.dev.yml
└── init-scripts/
    └── 01-create-multiple-databases.sh
```

## Services

| Service | Image | Port | الغرض |
|---------|-------|------|--------|
| `postgres` | postgres:16-alpine | 5432 | OLTP + EventStore (قاعدتين) |
| `redis` | redis:7-alpine | 6379 | Cache + Session |
| `api` | (build from Dockerfile) | 5000 | الـ Backend API |
| `frontend` | node:20-alpine | 3000 | Next.js dev server |

## Conventions

- **Service naming**: `erp-<name>` للـ containers
- **Networks**: default network
- **Volumes** للـ data persistence + node_modules (تجنب re-install)
- **Init scripts** مرقّمة: `01-...`, `02-...` (ترتيب التنفيذ)
- **Healthchecks** على كل service يعتمد عليه آخر

## init-scripts/

- تُشغّل مرة واحدة عند إنشاء الـ volume لأول مرة
- `01-create-multiple-databases.sh`: ينشئ قواعد `erp_system` و `erp_events`
- يجب أن تكون `chmod +x`

## لما تشتغل هنا

- إضافة service: تأكد من الـ healthcheck
- تغيير connection strings: انتبه للفرق بين `localhost` (محلي) و service names (داخل Docker)
- **JWT secret** في الإنتاج: استخدم Docker secrets أو env من CI

## بعد التعديل

- `docker compose -f infra/docker/docker-compose.dev.yml config` نظيف
- الـ containers تبدأ بنجاح
- الـ migrations تشتغل تلقائياً

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md)
- [`../../src/backend/AGENTS.md`](../../src/backend/AGENTS.md)
