# ERP-SYSTEM — Local Docker Setup

**Single command**: `docker compose up`

**Self-contained**: No external dependencies, no Supabase, no HF Space.

## 🚀 Quick Start (3 steps)

### 1. Install Docker Desktop

Download from https://www.docker.com/products/docker-desktop/

- Windows / macOS / Linux
- Free for personal use

### 2. Clone the repo + run

```bash
git clone https://github.com/anas600/ERP-SYSTEM.git
cd ERP-SYSTEM/local-docker
docker compose up
```

### 3. Open the app

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Health check**: http://localhost:5000/api/health/ready
- **PostgreSQL**: localhost:5432 (user: erp, password: erp_local_password, db: erp_system)

## 📋 What's included

| Service | Port | Image | Purpose |
|---|---|---|---|
| `postgres` | 5432 | postgres:15-alpine | Local database |
| `api` | 5000 | Built from repo Dockerfile | Backend (.NET 9) |
| `frontend` | 3000 | node:20-alpine | Frontend (Next.js 14) |

## 🛑 Stop

```bash
# Stop and keep data
docker compose stop

# Stop and remove everything (clean slate)
docker compose down -v
```

## 🔄 Reset

If something breaks:

```bash
# Stop + remove volumes (wipe DB)
docker compose down -v

# Rebuild from scratch
docker compose up --build
```

## 🔧 Customization

Copy `.env.example` to `.env` and adjust:

```bash
cp .env.example .env
# Edit .env
docker compose up
```

## 🔌 Integrate with your IT system

The ERP-SYSTEM API is at `http://localhost:5000` with OpenAPI/Swagger at `http://localhost:5000/swagger`.

**Test endpoints**:
- `GET /api/health/live` — liveness
- `GET /api/health/ready` — DB health
- `GET /api/health/full` — full diagnostic
- `POST /api/auth/login` — login (default user: `admin@erp.local` / `admin123`)
- `GET /api/users` — list users
- `GET /api/companies` — list companies
- `GET /api/audit` — audit log
- `GET /api/reports/*` — reports

**Database direct access** (psql):
```bash
docker exec -it erp-postgres-local psql -U erp -d erp_system
```

## 📁 Project structure (post-`docker compose up`)

```
ERP-SYSTEM/
├── src/
│   ├── backend/        # .NET 9 source
│   └── frontend/       # Next.js 14 source
├── infra/
│   └── docker/         # HF Space setup
├── local-docker/       # ← YOU ARE HERE (this folder)
│   ├── docker-compose.yml
│   ├── .env.example
│   └── README.md
└── Dockerfile          # Backend image (used by docker-compose)
```

## 🆘 Troubleshooting

| Issue | Fix |
|---|---|
| Port 5432 already in use | Stop local Postgres or change port in docker-compose.yml |
| Port 5000 already in use | Change `5000:5000` to `5001:5000` |
| Port 3000 already in use | Change `3000:3000` to `3001:3000` |
| Container fails to start | Check `docker compose logs api` |
| Database not initialized | Run `docker compose down -v` then `docker compose up` |

## 🎯 Production vs Local

- **Local (this setup)**: For Anas's personal computer, no external services
- **Production**: NOT included (per DEC-068). Use a separate deployment plan.

## 📞 Support

- Issues? → `docker compose logs` + check the docs/
- Want to contribute? → `git push` to develop branch
- Questions? → Mavis (Telegram: @MavisAnasCo_bot)

---

*Created per DEC-068 Decision 4: Local Docker ONLY for Anas's personal computer*
*By: Mavis (Coordinator) | 2026-07-25 05:15 UTC*
