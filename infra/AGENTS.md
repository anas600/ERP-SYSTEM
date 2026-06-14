# 🏗️ infra/AGENTS.md

> البنية التحتية: Docker + CI/CD.

## شو فيه

```
infra/
├── docker/
│   ├── docker-compose.dev.yml     # بيئة التطوير
│   └── init-scripts/              # سكربتات Postgres init
└── .github/
    └── workflows/
        └── ci.yml                 # GitHub Actions
```

## Conventions

### Docker

- **multi-stage builds** لتقليل حجم الـ images
- **non-root user** في الـ runtime stage
- **HEALTHCHECK** على كل service
- **.dockerignore** (يُضاف عند الحاجة)
- **Secrets** عبر env vars (لا تُحفظ في الـ compose file)

### CI/CD

- **GitHub Actions** فقط
- **PR checks** قبل الـ merge
- **Cache** للـ NuGet و npm
- **Postgres + Redis services** للـ integration tests
- **Docker build** بدون push (للتأكد من الـ Dockerfile)

## لما تشتغل هنا

- إضافة service جديد في `docker-compose.dev.yml`:
  1. حدد الـ image
  2. عرّف env vars
  3. عرّف healthcheck
  4. اربطه بالـ dependencies
- إضافة workflow جديد:
  1. حدد الـ trigger
  2. استخدم نفس الـ env values
  3. cache حيث أمكن

## بعد التعديل

- `docker compose config` يمر بدون errors
- الـ CI يمر على main branch
- Health checks تشتغل في الـ containers

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md)
- [`docker/AGENTS.md`](docker/AGENTS.md)
- [`../src/backend/AGENTS.md`](../src/backend/AGENTS.md)
