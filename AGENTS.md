# 🤖 AGENTS.md — ERP-SYSTEM (Root)

> **Per-directory technical context.** This file describes patterns and constraints for the entire repository. Per-module files exist in subdirectories.

**Last updated:** 2026-07-29 (Cleanup — hallucination reset)

---

## 🎯 Quick Reference

| Field | Value |
|-------|-------|
| **Constitution** | [`/CONSTITUTION.md`](./CONSTITUTION.md) (READ FIRST) |
| **Roadmap** | [`/docs/workflow/demo-roadmap.md`](./docs/workflow/demo-roadmap.md) |
| **Architecture** | [`/docs/architecture/holding-company-architecture.md`](./docs/architecture/holding-company-architecture.md) |
| **Changelog** | [`/docs/CHANGELOG.md`](./docs/CHANGELOG.md) |
| **Production** | `https://anas-assasket-erp-system.hf.space/` (canonical lowercase) |
| **Database** | Supabase (PostgreSQL 17, eu-central-1) |

---

## 🏛️ Architecture in 5 Lines

1. **Multi-Company**, NOT multi-tenant. `company_id` everywhere, NO `tenant_id`.
2. **One Holding + Many Companies** (1:N). No `Tenant` entity.
3. **JWT** carries `company_ids[]` + `X-Company-Id` header for current company.
4. **Dapper + FluentMigrator** (NO EF Core). Idempotent migrations.
5. **Clean Architecture**: Domain → Application → Infrastructure → Host.

---

## 🛠️ Stack

| Layer | Technology |
|-------|------------|
| **Backend** | C# / .NET 9 / ASP.NET Core / Dapper / FluentMigrator |
| **Frontend** | TypeScript / Next.js 14 (App Router) / Tailwind / shadcn/ui |
| **Database** | PostgreSQL 17 (Supabase) |
| **Auth** | JWT (HS256) + BCrypt |
| **CI/CD** | GitHub Actions (6 required checks) |
| **Hosting** | Hugging Face Space (Docker) |
| **Migrations** | FluentMigrator + raw SQL for hotfixes |

---

## 📁 Repository Structure

```
/
├── CONSTITUTION.md          ← READ FIRST
├── AGENTS.md                ← This file
├── CHANGELOG.md             ← Per-sprint changes
├── README.md
├── Dockerfile
├── package.json
├── xunit.runner.json
├── docs/
│   ├── AGENTS.md            ← docs-specific context
│   ├── CHANGELOG.md
│   ├── workflow/            ← Roadmap + sprint files
│   │   ├── demo-roadmap.md
│   │   ├── architecture.md
│   │   └── sprint-N.md
│   └── architecture/
│       └── holding-company-architecture.md
├── src/
│   ├── backend/             ← .NET 9 API
│   │   ├── Host/            ← Entry point
│   │   ├── Modules/         ← Business modules
│   │   ├── Shared/          ← Cross-cutting
│   │   └── Tests/           ← xUnit
│   └── frontend/            ← Next.js 14
├── .github/                 ← CI workflows
├── scripts/                 ← Build/utility scripts
├── infra/                   ← IaC
├── local-docker/            ← Local dev environment
├── .githooks/               ← Git hooks
└── .mavis/                  ← Mavis orchestration
```

---

## 🚦 Development Workflow

```
1. Anas / Cloud writes hand-off → docs/workflow/sprint-N.md
2. Mavis Local pulls develop, spawns Jimis (BE+FE parallel)
3. Jimis execute, Mavis Local verifies
4. Mavis Local opens PR (--admin merge)
5. Cloud auto-merges when CI green
6. Develop updated → next sprint
```

**Sprint duration:** 1.5-2 hours.

---

## 🚫 Forbidden Patterns

| ❌ NEVER | ✅ USE |
|----------|--------|
| `tenant_id` | `company_id` |
| `Tenant` entity | `Company` entity |
| `TenantContext` | `CompanyContext` |
| `TenantMiddleware` | `CompanyMiddleware` |
| `[TenantAuthorize]` | `[CompanyAuthorize]` |
| `EF Core` | `Dapper + FluentMigrator` |
| `user_tenants` table | `user_companies` table |
| `X-Tenant-Id` header | `X-Company-Id` header |
| Multi-tenant SaaS | Multi-Company |
| Hardcoded passwords/secrets | Env vars + secret manager |
| Direct commit to `main` | PR via `develop` |
| Force-push without `--force-with-lease` | `--force-with-lease` only |

---

## 🔧 Commands

```bash
# Backend
cd src/backend
dotnet build                    # Build
dotnet test                     # Tests
dotnet run --project Host       # Run API on :5001

# Frontend
cd src/frontend
npm install
npm run dev                     # Run on :3000
npm run build                   # Production build
npm run typecheck               # tsc --noEmit

# Git
git fetch origin
git pull --rebase origin develop
git push --force-with-lease origin feature/<name>
gh pr create --base develop
gh pr merge <num> --squash --admin   # Mavis Local only

# Local Docker
cd local-docker
docker compose up -d
```

---

## 🧪 Testing

| Tier | Required for merge? |
|------|---------------------|
| **Unit (xUnit + Jest)** | ✅ One per endpoint |
| **Integration** | ⚠️ Optional |
| **E2E (Playwright)** | ❌ NOT required (per Constitution Article 11) |

---

## 📞 Contact Points

- **Constitution changes:** Anas (owner) approval required.
- **Architecture questions:** Reference `/docs/architecture/`.
- **Roadmap questions:** Reference `/docs/workflow/demo-roadmap.md`.
- **Daily state:** Reference `/docs/CHANGELOG.md`.

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode), approved by Anas_
