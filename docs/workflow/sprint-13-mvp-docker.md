# Sprint 13 Hand-off — Containerized MVP (Layer 2 of 3-Layer Model)

**Author:** Mavis Coordinator (v1.8 governance, محمد/سيتی mode)
**Audience:** Mavis Local + Admin Team
**Branch target:** `feature/sprint-13-mvp-container`
**Mode:** LOCAL-ONLY (no push, no PR until Anas says "ادفع")

---

## 🎯 Sprint 13 Goal

Per **Anas's 2026-07-31 21:51 UTC directive**: implement **Layer 2 of the 3-Layer Model** — a clean, containerized MVP that mimics the client deliverable. No test data, browsable, ready to hand to the client.

**3-Layer Model (Anas's directive):**
- **Layer 1: Development** — local backend on host, with test data, fast iteration. **Active** (unchanged, uses `local-docker/`).
- **Layer 2: Staging / Containerized MVP** — clean schema in Docker, no test data, browsable. **Active (NEW in Sprint 13).**
- **Layer 3: Production** — FROZEN ("لا اهتم بيها الان").

---

## 📦 What Shipped

### P0a — `mvp-docker/` (NEW directory)

- **`mvp-docker/docker-compose.yml`** — separate from `local-docker/`. Clean schema (no seed), `ASPNETCORE_ENVIRONMENT=Production`, distinct container names (`erp-mvp-*`), distinct Postgres data volume (`mvp_postgres_data`). **Coexists with `local-docker/`** — different volumes, no collision.
- **`mvp-docker/.env.example`** — template for `JWT_SECRET` and DB creds. The real `.env` is gitignored.
- **`mvp-docker/.gitignore`** — excludes the real `.env`.
- **`mvp-docker/README.md`** — quick start + comparison with `local-docker/` + troubleshooting + the Layer 1→2→3 workflow per Anas's directive.

### P0b — Production frontend Dockerfile

- **`src/frontend/Dockerfile`** (NEW) — multi-stage build (`deps` → `build` → `runner`), Next.js standalone output, non-root user (`nextjs:1001`), `NODE_ENV=production`. **Layer 1's frontend (dev server via volume mount in `local-docker/`) is unchanged.** This is the production-mode image for Layer 2.

### P0c — Smoke test script

- **`mvp-docker/smoke-test.ps1`** — PowerShell script that verifies the MVP container end-to-end:
  1. Waits for API `/api/health/live` (up to 90s)
  2. Health endpoints (`/api/health/live`, `/api/health/ready`)
  3. Login as bootstrap admin (`admin@erp.local / Admin1234!`) — retries once after 5s for first-run bootstrap
  4. Frontend serves HTML
  5. **Database is clean** (`companies` count = 0)
  6. Swagger reachable
  - Exits 0 on success, 1 on any failure.

### P1 — AGENTS.md updates (3-Layer Model documented)

- **`AGENTS.md`** (UPDATED) — added "3-Layer Model" section. Old "Environment Layers" section kept as "Legacy" reference.

### Side work (per Anas 2026-07-31 21:51 UTC)

- **3 crons deleted:** `monitor-sprint10-jimis-local-only`, `monitor-sprint11-fe-be-parallel`, `sprint-10-11-pushed-2h-check` (all obsolete after their sprints merged).
- **GitHub PAT rotated** — old token replaced in `.git/config` remote URL with the new token from `.mavis/gh-token-key.,md.txt`. Verified via `git ls-remote origin HEAD` (returns SHA `afa43c7...`).

---

## 🔁 Workflow (per Anas 2026-07-31 21:51 UTC)

1. **Local Team** develops in **Layer 1** (direct host + test data) — fast iteration
2. **Sprint done in Layer 1** → Local Team merges to `develop` via PR
3. **Admin Team** (Mavis) takes over:
   1. Pull new commit
   2. `cd mvp-docker && cp .env.example .env && docker compose up -d --build`
   3. `./smoke-test.ps1` (verifies clean MVP runs)
   4. **Notify Anas** to browse the system
4. **Anas** browses the system, decides: continue development, or hand to client
5. **Strategic Advisor محمد (Mavis)** decides when to transition Layer 1 → Layer 2

---

## 🧪 Verification (T2 for Sprint 13)

Before opening the PR (when Anas says "ادفع"):

```bash
cd C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-13
git status  # should be clean after commit
py -c "import yaml; yaml.safe_load(open('mvp-docker/docker-compose.yml', encoding='utf-8'))"  # YAML valid
git grep -l "tenant_id" src/  # should return 0 hits
```

Then on **Anas's local machine** (the only place with Docker + the actual `mvp-docker` runtime):
```bash
cd mvp-docker
cp .env.example .env
# Edit .env: set JWT_SECRET
docker compose up -d --build
./smoke-test.ps1
# Open http://localhost:3000, login as admin@erp.local / Admin1234!
```

> **Note:** the smoke test requires Docker + the local machine. It cannot be run in CI (no Docker socket). The CI equivalent (Testcontainers) is a future Sprint.

---

## 🎭 Worker Allocation

- **1 worker, mixed (BE + DevOps + Docs)** — recommended. The work is tightly coupled (frontend Dockerfile affects the compose file affects the smoke test affects the docs). This is a low-complexity sprint.
- **Or split into 2:**
  - Jimi 1: `mvp-docker/` files (compose, env, smoke test, README)
  - Jimi 2: `src/frontend/Dockerfile` + AGENTS.md update
  - But the split is artificial — they share concepts (port mapping, container names, env var names).

**Decision: Admin Team (me, here) did it directly** — Sprint 12's Jimi failure pattern (silent failure with "succeeded" status) showed that small focused infra work is faster without spawning.

---

## ⚠️ Hard constraints

- **NO push to origin.** Local commits only.
- **NO `tenant_id`** in any file (Article 3).
- **NO EF Core.** Dapper only.
- **NO secrets in committed code** (the real `.env` is gitignored; only `.example` is committed).
- **No changes to `local-docker/`** — that setup is preserved as Layer 1.
- **DOX pass:** AGENTS.md updated; CHANGELOG entry added. No new module AGENTS.md needed (mvp-docker/ is too new for that — it'll get one if/when modules become durable boundaries).

---

## 🏁 Sprint 13 Definition of Done

- [x] `mvp-docker/docker-compose.yml` exists and is valid YAML
- [x] `mvp-docker/.env.example` is committed; the real `.env` is gitignored
- [x] `mvp-docker/.gitignore` excludes the real `.env`
- [x] `mvp-docker/README.md` documents the Layer 1 vs Layer 2 split
- [x] `mvp-docker/smoke-test.ps1` checks all 6 acceptance criteria
- [x] `src/frontend/Dockerfile` is a multi-stage production build
- [x] AGENTS.md has a "3-Layer Model" section with the workflow
- [x] CHANGELOG.md has the Sprint 13 entry
- [x] `git grep -l "tenant_id" src/` returns 0 hits
- [x] No secrets in committed code
- [x] 3 obsolete crons deleted (per Anas directive)
- [x] **LOCAL-ONLY** — no push, no PR. Awaiting Anas "ادفع".

---

## 🔮 Sprint 14 Candidates (from Sprint 13 retro)

### P0 — Testcontainers for CI smoke tests
- Replace the manual smoke-test.ps1 with a CI-runnable version using Testcontainers.
- This would let CI run smoke tests on every PR (currently CI only runs unit tests).

### P1 — Auto-trigger: when Local Team pushes a new commit, Admin Team auto-builds MVP
- A cron that watches `develop` for new commits and rebuilds `mvp-docker/` automatically.
- Replaces the manual "Admin Team pulls + builds + notifies Anas" step.

### P1 — Add an MVP demo data bootstrap
- A minimal seed for the MVP (1 admin user, 1 holding, 1 default company) — currently the bootstrap service handles the admin, but no other data.
- Useful for client demos so the UI isn't empty.

### P2 — Wire up the smoke test to the GitHub Actions workflow
- On every PR to develop, run the smoke test against a Testcontainer.
- Provides a "MVP verification" required check alongside Backend Tests, Frontend Build, etc.

**Stop. Wait for Anas. Do not push. Do not open PR.**
