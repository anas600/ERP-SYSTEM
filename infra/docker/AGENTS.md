# 🐳 AGENTS.md — infra/docker/

> **Docker configs.** Read `/AGENTS.md` and `/infra/AGENTS.md` first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

Docker Compose files and database init scripts for local development.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Dev (DevOps mode) |
| **Approval** | Mavis Local + Anas |

## Local Contracts

- **PostgreSQL 17** (matches Supabase dev).
- **All credentials via env vars** (`.env.local`, not in repo).
- **Init scripts idempotent** (use `CREATE TABLE IF NOT EXISTS`).

## Work Guidance

### Starting Local Stack
```bash
cd infra/docker
docker compose -f docker-compose.dev.yml up -d
```

### Adding Init Script
1. Create in `init-scripts/<NNN>_<description>.sql`.
2. Mount to `/docker-entrypoint-initdb.d/` in compose file.
3. Test locally before committing.

## Verification

- [ ] `docker compose config` — valid.
- [ ] All init scripts re-runnable.
- [ ] No hardcoded passwords in YAML.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| `infra/docker/init-scripts/` | Database init SQL | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
