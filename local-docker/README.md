# ERP-SYSTEM — Local Docker Setup

**Single command**: `docker compose up`
**Self-contained**: No external dependencies, no Supabase, no HF Space.

## 🚀 Quick Start (3 steps)

### 1. Install Docker Desktop
https://www.docker.com/products/docker-desktop/

### 2. Clone + run
```bash
git clone https://github.com/anas600/ERP-SYSTEM.git
cd ERP-SYSTEM/local-docker
docker compose up
```

### 3. Open the app
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Health check**: http://localhost:5000/api/health/ready
- **Swagger**: http://localhost:5000/swagger
- **PostgreSQL**: localhost:5432 (user: `erp`, password: `erp_local_password`, db: `erp_system`)

## 📋 What's included

| Service | Port | Image | Purpose |
|---|---|---|---|
| `postgres` | 5432 | postgres:15-alpine | Local database |
| `api` | 5000 | Built from repo Dockerfile | Backend (.NET 9) |
| `frontend` | 3000 | node:20-alpine | Frontend (Next.js 14) |

## 🛑 Stop / Reset

```bash
# Stop and keep data
docker compose stop

# Stop and remove everything (clean slate)
docker compose down -v

# Rebuild from scratch
docker compose up --build
```

## 🔌 Integrate with your IT system

The ERP-SYSTEM API is at `http://localhost:5000` with OpenAPI/Swagger.

**Test endpoints**:
- `GET /api/health/live` — liveness
- `GET /api/health/ready` — DB health
- `GET /api/health/full` — full diagnostic
- `POST /api/auth/login` — login
- `GET /api/users` — list users
- `GET /api/companies` — list companies
- `GET /api/audit` — audit log
- `GET /api/reports/*` — reports

**Database direct access**:
```bash
docker exec -it erp-postgres-local psql -U erp -d erp_system
```

## 🆘 Troubleshooting

| Issue | Fix |
|---|---|
| Port 5432/5000/3000 in use | Stop local services or change ports in docker-compose.yml |
| Container fails | `docker compose logs api` |
| DB not initialized | `docker compose down -v && docker compose up` |

## 🎯 Per DEC-068

This is the **Local Docker Setup** (Decision 4). NOT production.
For Anas's personal computer only.

---

*By Mavis (Coordinator) | 2026-07-25 05:18 UTC*
