# 🏗️ Architectural Constraints (Soft Rules for Demo)

> **Purpose:** Dynamic rules that guide the team without over-engineering.
> **Goal:** Demo in 10 hours, support future scaling.
> **Override priority:** Constitution > DEC-001+ > This file

---

## 🎯 The 10 Soft Rules

### 1. **One Branch Rule** — `develop` is the only source of truth
- All work merges to `develop` (squash, --admin)
- No long-lived feature branches
- Each cycle = 1 PR

### 2. **API-First Rule** — Backend before Frontend
- Define endpoint → implement → test → then frontend
- Frontend never invents API contracts
- Use OpenAPI/Swagger for documentation

### 3. **Idempotent Migrations** — Re-runnable always
- `IF EXISTS` checks in all migrations
- Use FluentMigrator's `.IfDatabase()`
- Never break reruns

### 4. **One Test Per New Endpoint** — Minimum coverage
- New endpoint = 1 happy-path test + 1 error-path test
- Use existing test patterns (xUnit, FluentAssertions)

### 5. **company_id Only** — No tenant_id (Constitution Article 3)
- All multi-tenancy via `company_id` and `user_companies`
- JWT carries `company_ids[]`
- Request: `X-Company-Id` header

### 6. **No EF Core** — Dapper + FluentMigrator (Constitution Article 2)
- SQL is the source of truth
- Migrations in `Migrations/` folder
- Queries via `Dapper.SqlMapper`

### 7. **Pre-Demo Data** — Real data, not mocks
- Use ERP-SYSTEM's existing data (Holding, companies, users)
- Seed via migration (idempotent)
- No `Mock` or `Fake` in production code

### 8. **No Secrets in Code/PRs** (Constitution Article 7)
- All secrets via env vars or secrets manager
- PRs with secrets = auto-reject
- Use `dotnet user-secrets` for local dev

### 9. **Frontend-First Errors** — UX > Internal details
- Show user-friendly messages in Arabic + English
- Log details server-side, return generic 4xx/5xx
- Toast notifications for action feedback

### 10. **Document in AGENTS.md** — When architecture changes
- Update the closest `AGENTS.md` file
- One paragraph: what + why + when
- Link to relevant DEC

---

## 🚫 The 5 Anti-Patterns (Don't Do)

1. **No Over-Engineering** — Don't build for hypothetical scale
2. **No Premature Optimization** — Profile first, optimize second
3. **No Speculative Features** — Only what the demo needs
4. **No Custom Solutions** — Use framework defaults (Next.js, ASP.NET)
5. **No Long Sync Tasks** — Anything > 100ms goes async/background

---

## 📐 Demo Environment Constraints

| Constraint | Value | Why |
|------------|-------|-----|
| **Hosting** | Local Docker / On-Prem | Client requirement |
| **Database** | PostgreSQL 17 | Supabase compatible, easy migration |
| **Auth** | JWT + cookies | Standard, no extra infra |
| **Frontend** | Next.js 14 + Tailwind | Fast, RTL-friendly |
| **Backend** | .NET 9 + Dapper | Per Constitution |
| **Migrations** | FluentMigrator | Version-controlled |
| **Tests** | xUnit + Playwright | Standard |
| **CI/CD** | GitHub Actions | Already set up |
| **Demo Data** | Seed via migration | Idempotent, no external deps |

---

## 🎬 Sprint Workflow (V2)

```
Siti (Cloud)                Mavis Local (Tech Lead)
     │                              │
     │  Plan + Architectural       │
     │  Constraints                 │
     │  (hand-off) ────────────────▶│
     │                              │
     │                              │  Delegate to 2 Jimis
     │                              │  (Frontend + Backend)
     │                              │  ──────────┐
     │                              │            │
     │                              │  ◀─────────┘
     │                              │  Verify + Integrate
     │                              │
     │  Review PR ◀────────────────│
     │  (CI green)                  │
     │                              │
     │  Merge + Close cycle         │
     │  ───────────────────────────▶│
     │                              │
     │                       [Next Sprint]
```

**Per sprint:**
- **0.5h** — Plan (Siti) + Hand-off + 2 Jimis spawned
- **1.5h** — Execution (parallel Jimis)
- **0.5h** — Verify + Merge + Close

**Total per sprint:** 2.5 hours

