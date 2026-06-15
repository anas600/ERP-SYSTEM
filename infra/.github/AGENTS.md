# 🔄 infra/.github/AGENTS.md

> GitHub Actions workflows.

## شو فيه

```
.github/
└── workflows/
    └── ci.yml     # Backend + Frontend + Docker build
```

## Workflows

### ci.yml

- **Triggers**: push / PR على main و develop
- **Concurrency**: يلغي pipelines قديمة على نفس الـ PR
- **Jobs**:
  1. `backend` — restore → build → test مع Postgres + Redis services
  2. `frontend` — install → type-check → lint → build
  3. `docker` — يبني صورة الـ API (بدون push)

## Conventions

- **Pinned versions** للـ actions (`@v4`، `@v6`، إلخ)
- **Env vars** مشتركة في `env:` block
- **Cache** للـ NuGet و npm
- **Test results** تُرفع كـ artifacts
- **Secrets** من GitHub Secrets (لا تضعها في الـ YAML)

## لما تضيف workflow جديد

- حدّد الـ trigger بوضوح
- استخدم `actions/checkout@v4` و `actions/setup-*` المعتمدة
- اربطه من `infra/AGENTS.md`

## Secrets المستخدمة (في GitHub Repo Settings)

- (لا شيء حالياً في CI — كل الـ secrets في `appsettings.Development.json` للـ dev)
- للإنتاج لاحقاً: `JWT_SECRET`, `DB_PASSWORD`, `REDIS_URL`, `DOCKERHUB_TOKEN`

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md)
