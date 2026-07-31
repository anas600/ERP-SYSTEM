# ERP-SYSTEM — MVP Docker (Layer 2 of 3-Layer Model)

**Sprint 13** — per **Anas's 2026-07-31 21:51 UTC directive**.

> Layer 2 of the 3-layer model: **Staging/Containerized MVP** — Docker container, clean schema, no test data, browsable on `localhost:3000`. Mimics the initial MVP deliverable for the client.

The 3 layers are:

| Layer | Purpose | Tool |
|---|---|---|
| **1. Development** | Local backend on host, with test data, fast iteration | `local-docker/` (with seed) + direct host runs |
| **2. Staging / Containerized MVP** | Clean schema in Docker, browsable, no test data | **`mvp-docker/` (this dir)** ← you are here |
| **3. Production** | FROZEN (out of scope, per Anas 2026-07-31 21:51 UTC) | (none) |

---

## 🚀 Quick Start (3 steps)

### 1. Copy env template
```bash
cd mvp-docker
cp .env.example .env
# Edit .env — at minimum, set JWT_SECRET to a random 64-char string:
#   openssl rand -base64 64
```

### 2. Build + run
```bash
docker compose up -d --build
```

Wait ~60-90 seconds for the API to finish auto-migration. The bootstrap service creates a default Holding + admin user on first run.

### 3. Run smoke test
```powershell
# Windows PowerShell
./smoke-test.ps1
```

Expected output: **All checks passed. MVP is ready to browse.**

Then open: **http://localhost:3000**

| URL | Description |
|---|---|
| `http://localhost:3000` | **Frontend** (start here) |
| `http://localhost:5000/swagger` | API documentation |
| `http://localhost:5000/api/health/ready` | Backend health check |

**Default login** (created by `DefaultHoldingBootstrapHostedService` on first run):
- Email: `admin@erp.local`
- Password: `Admin1234!`  *(change this immediately in any non-demo deployment)*

---

## 🆚 Differences from `local-docker/`

| | `local-docker/` (Layer 1) | `mvp-docker/` (Layer 2) |
|---|---|---|
| **Purpose** | Dev with test data | Clean client-deliverable MVP |
| **Seed data** | `docs/seed-sprint4-demo-data.sql` (10 users, 100+ txns) | None — bootstrap admin only |
| **Frontend** | Dev server (`npm run dev` via volume mount) | Production build (`npm run build` + standalone) |
| **Postgres data volume** | `postgres_data` | `mvp_postgres_data` (separate — won't collide) |
| **Container names** | `erp-postgres-local`, `erp-api-local`, `erp-frontend-local` | `erp-mvp-postgres`, `erp-mvp-api`, `erp-mvp-frontend` |
| **Ports** | 5432 / 5000 / 3000 (same — but separate DB data) | 5432 / 5000 / 3000 (same — but separate DB data) |
| **ASPNETCORE_ENVIRONMENT** | Development | Production |
| **JWT secret** | Hardcoded dev secret (OK for local) | Read from env var (you must set it) |

> **Run BOTH at the same time?** Yes — they use different container names, different Postgres data volumes, and different database names (`erp_system` for both but different volumes). They do NOT collide.

---

## 🛑 Stop / Reset

```bash
# Stop (keep data)
docker compose stop

# Stop + remove everything (clean slate — also drops the DB volume)
docker compose down -v
```

---

## 🔁 Workflow (per Anas 2026-07-31 21:51 UTC directive)

1. **Local Team** (sprint work) → develops in Layer 1 (direct host + test data) — fast iteration
2. **Sprint done in Layer 1** → Local Team merges to `develop` via PR
3. **Admin Team** (Mavis) → takes over:
   1. Pulls new commit
   2. `cd mvp-docker && docker compose up -d --build` (rebuilds the image)
   3. `./smoke-test.ps1` (verifies clean MVP runs)
   4. **Notifies Anas** to browse
4. **Anas** browses the system, decides: continue development, or hand to client

> **Strategic Advisor محمد (Mavis)** decides when to transition between layers.

---

## 🆘 Troubleshooting

| Issue | Fix |
|---|---|
| `relation "companies" does not exist` | API missing `Database__JsonMigrationEnabled: "true"`. Check `docker compose logs api`. |
| Port 5432/5000/3000 in use | Either stop the other container, or change ports in `docker-compose.yml`. |
| Admin user not created (login fails 401) | First run takes ~30-60s for bootstrap. Wait + retry. Check `docker compose logs api | grep -i bootstrap`. |
| `smoke-test.ps1` says "DB not clean" | Someone ran the local-docker seed against this volume. Run `docker compose down -v` to wipe. |
| Frontend 502 Bad Gateway | API not ready yet. Wait 30s and reload. |
| `JWT_SECRET` warning in logs | Set it in `.env`. Default placeholder works for dev but is insecure. |

---

## 📚 Related Docs

- `docs/architecture/holding-company-architecture.md` — System architecture
- `docs/workflow/local-docker.md` — Layer 1 (dev) reference
- `local-docker/README.md` — Layer 1 quick start
- `AGENTS.md` (root) — 3-Layer Model section (added in Sprint 13)

---

*Added 2026-07-31 by Mavis (Admin Team) — Sprint 13.*
