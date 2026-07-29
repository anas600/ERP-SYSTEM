# ERP-SYSTEM — Local Docker Setup

**Single command**: `docker compose up -d --build`
**Self-contained**: No external dependencies, no Supabase, no HF Space.

> **For full architecture, see [docs/workflow/local-docker.md](../docs/workflow/local-docker.md).**
> This README is the **quick start**; the workflow doc is the **reference**.

---

## 🚀 Quick Start (5 steps)

### 1. Install Docker Desktop
https://www.docker.com/products/docker-desktop/

### 2. Clone + run
```bash
git clone https://github.com/anas600/ERP-SYSTEM.git
cd ERP-SYSTEM
git checkout develop
cd local-docker
cp .env.example .env
docker compose up -d --build
```

Wait ~60-90 seconds for the API to finish auto-migration. Check progress:
```bash
docker compose ps    # all should be "running" (postgres: "healthy")
docker compose logs api  # should show migration complete + DataTypeMigrator success
```

### 3. Apply demo seed
```bash
# Copy the demo seed into the postgres container
docker cp ../docs/seed-sprint4-demo-data.sql erp-postgres-local:/tmp/seed.sql

# Apply it (idempotent — safe to re-run)
docker exec -it erp-postgres-local psql -U erp -d erp_system -f /tmp/seed.sql

# Verify (should be: 4 / 10 / 19 / 17 / 4 / 42 / 30 / 20 / 30 / 20)
docker exec -it erp-postgres-local psql -U erp -d erp_system -c "SELECT 'companies' AS t, count(*) FROM companies UNION ALL SELECT 'users', count(*) FROM users UNION ALL SELECT 'user_companies', count(*) FROM user_companies UNION ALL SELECT 'user_roles', count(*) FROM user_roles UNION ALL SELECT 'roles', count(*) FROM roles UNION ALL SELECT 'activity_log', count(*) FROM activity_log UNION ALL SELECT 'sales_invoices', count(*) FROM sales_invoices UNION ALL SELECT 'vendor_bills', count(*) FROM vendor_bills UNION ALL SELECT 'journal_entries', count(*) FROM journal_entries UNION ALL SELECT 'stock_movements', count(*) FROM stock_movements;"
```

### 4. Open the app
| URL | Description |
|---|---|
| `http://localhost:3000` | **Frontend** (start here) |
| `http://localhost:5000/swagger` | API documentation |
| `http://localhost:5000/api/health/ready` | Backend health check |

### 5. Login
| Role | Email | Password |
|---|---|---|
| **Admin (full access)** | `admin@alfajr.local` | `Demo1234` |
| Admin (multi-company) | `mohamed@alfajr.local` | `Demo1234` |
| Accountant | `ahmed@alfajr.local` | `Demo1234` |
| Accountant | `fatima@alfajr.local` | `Demo1234` |
| Accountant (warehouse) | `ali@alfajr.local` | `Demo1234` |
| ProjectManager | `khaled@alfajr.local` | `Demo1234` |
| ProjectManager (procurement) | `naseer@alfajr.local` | `Demo1234` |
| ProjectManager (cross-company) | `omar@alfajr.local` | `Demo1234` |
| Viewer (secretary) | `sara@alfajr.local` | `Demo1234` |
| Viewer (financial controller) | `rida@alfajr.local` | `Demo1234` |

**All passwords are `Demo1234`** (BCrypt `$2a$11$` cost 11).

---

## 📋 What's included

| Service | Port | Image | Purpose |
|---|---|---|---|
| `postgres` | 5432 | `postgres:15-alpine` | Local database (user: `erp`, password: `erp_local_password`, db: `erp_system`) |
| `api` | 5000 | Built from `src/backend/Dockerfile` | Backend (.NET 9) — `http://+:5000` |
| `frontend` | 3000 | `node:20-alpine` | Frontend (Next.js 14 dev server) |

The `api` container:
- Runs FluentMigrator migrations on startup
- Runs DataTypeMigrator (creates tables from `data-types/*.json` per DEC-096)
- Exposes `/api/health/live` + `/api/health/ready` for monitoring
- Connects to `postgres` service via Docker networking (NOT `localhost`)

The `frontend` container:
- Uses volume mount (`../src/frontend:/app`) for hot-reload
- Calls backend at `http://localhost:5000` (from host) or `http://api:5000` (from inside network)

---

## 🛑 Stop / Reset

```bash
# Stop and keep data
docker compose stop

# Stop and remove everything (clean slate)
docker compose down -v

# Rebuild from scratch (e.g., after Dockerfile change)
docker compose up -d --build

# Just the API container (rebuilds if needed)
docker compose up -d --build api
```

---

## 🆘 Troubleshooting

| Issue | Fix |
|---|---|
| Port 5432/5000/3000 in use | `netstat -ano | findstr :PORT` → kill the PID. Or change ports in `docker-compose.yml`. |
| Container fails to start | `docker compose logs api` (or `frontend`, `postgres`) |
| API shows "unhealthy" but works | Cosmetic — healthcheck uses `wget` which isn't in the ASP.NET image. Use `docker ps` to check actual status. See [docs/workflow/local-docker.md#why-not-just-use-supabase] for details. |
| DB not initialized (empty tables) | `docker compose down -v && docker compose up` (nuclear), then re-apply seed. |
| `relation "companies" does not exist` | API missing `Database__JsonMigrationEnabled: "true"`. Fixed in PR #170. |
| `Failed to connect to 127.0.0.1:5432` | API missing `ConnectionStrings__Migrations`. Fixed in PR #170. |
| Login returns 401 | Check that seed was applied: `SELECT count(*) FROM users;` should be ≥ 10. Also check `SELECT count(*) FROM user_companies WHERE user_id = (SELECT id FROM users WHERE email='admin@alfajr.local');` should be 4. |
| Login returns 500 (BCrypt error) | Admin user has wrong password hash. Re-apply the seed (it includes the admin user with proper hash). |
| `package.json not found` in frontend | Volume mount wrong. The frontend volume should be `../src/frontend:/app` (NOT `./src/frontend`). Fixed in PR #170. |
| `relation "users" does not exist` after seed | Migrations didn't run. Check `docker compose logs api | grep -i migration`. |
| `role "postgres" does not exist` | Old config from a previous attempt. Make sure `ConnectionStrings__Migrations` is set to `Host=postgres` (not `localhost`). |
| Need to inspect the database | `docker exec -it erp-postgres-local psql -U erp -d erp_system` |

---

## 🔌 API Endpoints (Quick Test)

```bash
# Health
curl http://localhost:5000/api/health/live
curl http://localhost:5000/api/health/ready

# Login (returns JWT)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@alfajr.local","password":"Demo1234"}'

# Use the token
TOKEN="eyJhbGc..."  # from login response
curl http://localhost:5000/api/companies -H "Authorization: Bearer $TOKEN"
curl http://localhost:5000/api/activity/recent?limit=20 -H "Authorization: Bearer $TOKEN"
```

**Database direct access:**
```bash
docker exec -it erp-postgres-local psql -U erp -d erp_system

# Useful queries
\dt                          # list tables
SELECT * FROM companies;
SELECT email, is_active FROM users;
SELECT count(*) FROM activity_log;
```

---

## 🎯 Per DEC-068

This is the **Local Docker Setup** (Decision 4 of DEC-068). NOT production. For Anas's personal computer (and any engineer's local dev/demo).

For production: Supabase + Hugging Face Space (see `docs/architecture/`).

---

## 📚 Related Docs

- [docs/workflow/local-docker.md](../docs/workflow/local-docker.md) — Architecture & design decisions
- [docs/workflow/local-docker-fixes-report.md](../docs/workflow/local-docker-fixes-report.md) — PR #170 technical report
- [docs/seed-sprint4-demo-data.sql](../docs/seed-sprint4-demo-data.sql) — Demo data definition

---

*Updated 2026-07-29 by Mavis Local (PR #171 — local-docker setup improvements)*
