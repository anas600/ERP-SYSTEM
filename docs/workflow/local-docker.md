# Local Docker Setup — Architecture & Usage

> **Audience:** Engineers running ERP-SYSTEM locally for demos, dev, or testing.
> **Status:** ✅ Stable (post PR #170 + PR #171).
> **Last updated:** 2026-07-29 by Mavis Local.

---

## What It Is

A **self-contained** Docker Compose stack that runs the entire ERP-SYSTEM locally — backend (.NET 9), frontend (Next.js 14), and PostgreSQL 15 — without any external services. Designed for **client demos** on Anas's personal machine (per **DEC-068 Decision 4**).

## What It Is NOT

- **Not production.** Use Supabase + Hugging Face Space for prod (per `docs/architecture/`).
- **Not for CI.** CI runs unit tests + builds, not full stack.
- **Not for team-wide dev.** Each engineer has their own; no shared state.

## When to Use It

| Scenario | Use local-docker? | Alternative |
|---|---|---|
| Client demo on your machine | ✅ **Yes** | — |
| Local dev with sample data | ✅ **Yes** | — |
| Local dev against Supabase (no demo) | ❌ No | `appsettings.Development.json` |
| Backend unit tests | ❌ No | `dotnet test` (in-memory) |
| E2E tests | ❌ No | Playwright (against dev Supabase) |
| Production deploy | ❌ No | Hugging Face Space |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ Docker Compose (local-docker/docker-compose.yml)               │
│                                                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────┐  │
│  │ erp-postgres-local│  │ erp-api-local    │  │erp-frontend-│  │
│  │  (postgres:15-    │◀─│  (dotnet:9.0     │  │   local     │  │
│  │   alpine)         │  │   + JSON data-   │  │ (node:20-   │  │
│  │                   │  │   type migrator) │  │  alpine)    │  │
│  │ Port: 5432        │  │ Port: 5000       │  │ Port: 3000  │  │
│  │ User: erp         │  │                   │  │             │  │
│  │ Pass: erp_local_* │  │                   │  │             │  │
│  └──────────────────┘  └──────────────────┘  └─────────────┘  │
│         ▲                          ▲                  ▲        │
│         │                          │                  │        │
│         └────── docker network ────┴──────────────────┘        │
│                (local-docker_default)                          │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Decisions

1. **PostgreSQL 15** (not 17): Matches what Supabase uses today; smaller image; faster local startup. The system is compatible with both.
2. **No healthcheck on API**: `wget` is not in `mcr.microsoft.com/dotnet/aspnet:9.0`. Use `curl` (multi-stage) or check via `docker ps` status. See `local-docker/README.md` for details.
3. **`Database__JsonMigrationEnabled: "true"`**: Per DEC-096, the system creates tables from JSON data-type files (`companies.json` → `companies` table). Without this flag, the API boots but every query returns `relation does not exist`.
4. **Migrations connection string must override `localhost`**: The default in `appsettings.json` is `Host=localhost;Port=5432` (for dev-without-Docker). In Docker, this is the container's own loopback, so the API can't reach postgres. We override via `ConnectionStrings__Migrations` env var.
5. **Frontend uses volume mount, not image build**: Faster iteration. `npm run dev` reloads on file changes.

## Quick Start

```bash
# 1. Clone (or use existing worktree)
git clone https://github.com/anas600/ERP-SYSTEM.git
cd ERP-SYSTEM

# 2. (One-time) Create .env from example
cd local-docker
cp .env.example .env

# 3. Start the stack
docker compose up -d --build
# Wait ~60-90s for API to finish auto-migration

# 4. Apply demo seed (3 companies, 10 users, 142 transactions)
docker cp ../docs/seed-sprint4-demo-data.sql erp-postgres-local:/tmp/seed.sql
docker exec -it erp-postgres-local psql -U erp -d erp_system -f /tmp/seed.sql
# No manual workarounds needed (post PR #171)

# 5. Open browser
# Frontend: http://localhost:3000
# Login: admin@alfajr.local / Demo1234
```

## Verification Checklist

- [ ] `docker ps` shows all 3 containers as "running" (or "healthy" for postgres)
- [ ] `curl http://localhost:5000/api/health/live` → 200 + `{"status":"alive"}`
- [ ] `curl http://localhost:5000/api/health/ready` → 200 + `{"status":"healthy"}`
- [ ] Browser at http://localhost:3000 loads the login page
- [ ] Login as `admin@alfajr.local` / `Demo1234` → dashboard shows 4 companies + 10 users
- [ ] `/activity` page shows ~42 entries
- [ ] `/admin/companies` shows 3 companies
- [ ] RTL Arabic layout works

## Troubleshooting

See `local-docker/README.md` "Troubleshooting" section for the full table. Common issues:

| Issue | Fix |
|---|---|
| Port 5432/5000/3000 in use | `netstat -ano | findstr :PORT` → kill the PID |
| Container fails | `docker compose logs api` |
| DB not initialized | `docker compose down -v && docker compose up` |
| Login returns 401 | Check that seed was applied (`SELECT count(*) FROM users;` should be 10) |
| Login returns 500 (BCrypt error) | Admin user might have wrong password hash — re-apply seed |
| `relation "companies" does not exist` | API missing `Database__JsonMigrationEnabled: "true"` (fixed in PR #170) |
| `Failed to connect to 127.0.0.1:5432` | API missing `ConnectionStrings__Migrations` (fixed in PR #170) |

## Reset Everything

```bash
# Nuclear option: destroy all data + rebuild
cd local-docker
docker compose down -v
docker compose up -d --build
# Re-apply seed
```

## Files

```
local-docker/
├── .env.example          # Template for .env (gitignored)
├── docker-compose.yml    # Stack definition (3 services)
├── README.md             # Quick start (updated in PR #171)
└── start.sh              # Bash wrapper for docker compose up

docs/seed-sprint4-demo-data.sql  # Demo data (3 companies + 10 users + 142 transactions)
docs/workflow/local-docker.md     # This file
```

## Why Not Just Use Supabase?

For most local dev, Supabase is fine (the system connects to it via `appsettings.Development.json`). The local Docker setup is for **client demos**:

1. **No internet dependency** — works in airplane mode
2. **Fast iteration** — `docker compose up` is faster than Supabase warmup
3. **Predictable state** — fresh DB on every reset
4. **No risk of demo data leaking** — completely isolated

## Maintenance

- **DB engine version:** Postgres 15. Bump to 16/17 when the Supabase prod version changes.
- **Backend image:** Built from `src/backend/Dockerfile`. Rebuild on backend changes.
- **Frontend:** Uses volume mount. Restarts on file change (no rebuild needed).
- **Seed file:** Updated in PR #171 to be self-healing. Re-runnable without manual workarounds.

---

_By Mavis Local, 2026-07-29_
