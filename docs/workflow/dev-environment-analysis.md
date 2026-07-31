# 🔧 Dev Environment Analysis (Per Anas 2026-07-31 05:42 UTC)

> **Author:** Mavis (ديف mode) — DevOps
> **Per:** Anas mandate — analyze local dev environment BEFORE planning the demo sprint.

---

## ✅ What's Available

| Tool | Version | Path | Status |
|------|---------|------|--------|
| **dotnet SDK** | 10.0.101 + 9.0.308 + 7.0.410 | `C:\Program Files\dotnet\` | ✅ Ready (multi-SDK) |
| **node** | v24.12.0 | `C:\Program Files\nodejs\` | ✅ Ready (over-spec for Next.js 14) |
| **npm** | 11.8.0 | (bundled with node) | ✅ Ready |
| **git** | 2.52.0.windows.1 | PATH | ✅ Ready |
| **GitHub CLI (gh)** | 2.93.0 | `C:\Program Files\GitHub CLI\` | ✅ Ready (in PR workflow) |
| **Docker Engine** | 29.6.2 | `C:\Program Files\Docker\` | ✅ Ready (compose v5.3.1) |
| **PostgreSQL 17** | 17.10 (full) | `C:\Program Files\PostgreSQL\17\bin\` | ✅ Ready (psql + all tools) |
| **curl** | (Windows default) | `C:\Windows\system32\` | ✅ Ready |

## ❌ What's NOT Available (out of PATH or missing)

| Tool | Status | Workaround |
|------|--------|------------|
| **psql** | NOT in default PATH | Use full path: `C:\Program Files\PostgreSQL\17\bin\psql.exe` |
| **docker** | NOT in default PATH | Use full path: `C:\Program Files\Docker\Docker\resources\bin\docker.exe` |
| **gh** | NOT in default PATH | Use full path: `C:\Program Files\GitHub CLI\gh.exe` |
| **supabase CLI** | NOT installed | Skip — use direct Postgres or HF Space (per existing setup) |
| **pnpm** | NOT installed | Use `npm` instead (slower but works) |
| **yarn** | NOT installed | Use `npm` instead |
| **Make** | NOT installed | Use PowerShell scripts or `docker compose` directly |
| **jq** | NOT installed | Use `ConvertFrom-Json` in PowerShell |
| **TruffleHog** | NOT installed | Pre-commit hook skips it (per existing build) |

---

## 🎯 Implications for the Demo Sprint

### ✅ Can do locally (right now)
- `dotnet build` + `dotnet test` (full test suite, ~7s, 436 pass baseline)
- `npm install` + `npm run typecheck` + `npm run build` (frontend)
- `git push` + `gh pr create` (full GitHub workflow)
- `docker compose up` (full local stack — postgres + api + frontend)
- `dotnet ef` is NOT applicable (we use FluentMigrator, no EF Core per Article 8 Rule 6)

### ⚠️ Need to be careful with
- **PATH issues** — PowerShell sessions don't auto-load docker/gh. Need to:
  - Use full paths in scripts, OR
  - Add to `~/.bashrc` / `$PROFILE` for permanent fix
  - Document in `local-docker/start.sh` (already done)
- **Docker Desktop** must be running (Linux engine) before `docker compose up`. Check: `docker info`
- **Local Postgres port 5432** must be free for `docker compose up postgres` to work
- **psql calls** need full path (or wrap in `local-docker/start.sh`)

### 🚀 Recommended Setup for the Demo Sprint
```bash
# 1. Add to PowerShell profile for permanent PATH
$env:Path += ";C:\Program Files\Docker\Docker\resources\bin;C:\Program Files\GitHub CLI;C:\Program Files\PostgreSQL\17\bin"

# 2. Verify
docker --version
gh --version
psql --version

# 3. Use local-docker
cd local-docker
cp .env.example .env
docker compose up -d --build

# 4. Wait for healthy
docker compose ps

# 5. Apply demo seed
docker exec -i erp-postgres-local psql -U erp -d erp_system < ../docs/seed-sprint4-demo-data.sql

# 6. Open: http://localhost:3000 (login: admin@alfajr.local / Demo1234)
```

---

## 🐳 Docker Image Strategy (per Anas "lightweight CI")

**Current setup (heavy):**
- `mcr.microsoft.com/dotnet/sdk:9.0` for build (1.5GB)
- `mcr.microsoft.com/dotnet/aspnet:9.0` for runtime (250MB)
- `postgres:15-alpine` for DB (80MB)
- `node:20-alpine` for FE (180MB)

**Recommended for demo (lighter):**
- `mcr.microsoft.com/dotnet/sdk:9.0-alpine` (1.2GB) — saves 300MB
- `mcr.microsoft.com/dotnet/aspnet:9.0-alpine` (180MB) — saves 70MB
- `postgres:17-alpine` (80MB) — same
- `node:20-alpine` (180MB) — same

**CI alternative (lightweight, per Anas):**
- Skip GitHub Actions (heavy, slow)
- Use a `Makefile` or `scripts/ci.sh` for local CI (run before push)
- `pre-commit` hook (already exists) for TruffleHog-like scans
- `dotnet test --filter` for fast feedback (~30s for affected tests)

---

## 📊 Resource Estimate for the Demo

| Component | Docker Image | Memory | Disk |
|-----------|--------------|--------|------|
| **postgres** | postgres:17-alpine | 256MB | 200MB (with demo seed) |
| **api** | dotnet/aspnet:9.0-alpine | 512MB | 250MB |
| **frontend** | node:20-alpine (dev) | 512MB | 500MB (with node_modules) |
| **Total** | — | **1.3GB** | **~1GB** |

This is feasible on a typical dev machine (16GB RAM, 50GB free disk).

---

## 🚦 Recommendations for the Demo Sprint

1. **Pre-flight check** (Dev script):
   - Verify Docker is running
   - Verify psql can connect to the local DB
   - Verify `gh` is authenticated
   - Verify `dotnet test` baseline (436 pass)
2. **Worktree pattern** — every Local session gets its own worktree off develop
3. **Test in Docker early** — don't wait until the end to test the full stack
4. **Document env setup** in `local-docker/README.md` (already there, may need updates)
5. **Consider a `Makefile`** for common operations (`make up`, `make test`, `make down`)

---

_Author: Mavis (ديف mode) — DevOps_
_Date: 2026-07-31_
_Status: 🟢 READY for demo sprint planning_
