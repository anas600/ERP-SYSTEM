# 🏗️ AGENTS.md — infra/

> **Infrastructure-as-Code.** Read root AGENTS.md first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

Infrastructure configuration: Docker Compose, deployment scripts, init scripts.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Dev (DevOps mode) |
| **Approval** | Anas (production) / Mavis Local (dev) |

## Local Contracts

- **All env-specific values via env vars.** NO hardcoded secrets.
- **Docker images pinned to specific versions** (no `latest`).
- **Idempotent init scripts** (use `IF NOT EXISTS`).

## Work Guidance

### Adding Docker Config
- Create in `infra/docker/docker-compose.<env>.yml`.
- Use named volumes for data persistence.
- Document in `infra/docker/AGENTS.md`.

## Verification

- [ ] `docker compose -f infra/docker/docker-compose.dev.yml config` — valid.
- [ ] No secrets in YAML files.
- [ ] All init scripts idempotent.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`infra/docker/`](./docker/) | Docker Compose + init scripts | Active |
| [`infra/scripts/`](./scripts/) | Deployment scripts | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
