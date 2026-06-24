# 🔄 .github/AGENTS.md

> GitHub Actions workflows + Repository automation.

## شو فيه

```
.github/
├── workflows/
│   └── ci.yml     # Backend + Frontend + Docker build
└── AGENTS.md      # هذا الملف
```

> **ملاحظة:** الـ workflows انتقلت من `infra/.github/workflows/` إلى `.github/workflows/` ليقرأها GitHub Actions بشكل صحيح.

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

## PR Rules

- كل push على `main` أو `develop` → CI يـ runs
- كل PR → CI required check قبل الـ merge
- الـ branch protection لازم يكون: "Require status checks to pass before merging"
