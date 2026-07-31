# 🛠️ AGENTS.md — src/

> **Source code root.** Read root AGENTS.md first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

All application source code: backend (.NET) and frontend (Next.js).

## Ownership

| Subtree | Owner | Authority |
|---------|-------|-----------|
| `src/backend/` | Jimi تنفيذي (Backend) | Backend code, tests, migrations |
| `src/frontend/` | Jimi تحليلي (Frontend) | Frontend pages, components, RTL/i18n |

Tech Lead (Mavis Local) coordinates both. Cloud (Siti) verifies and merges.

## Local Contracts

- **API-First:** Backend before Frontend. One test per endpoint.
- **NO `tenant_id`** anywhere. Use `company_id`.
- **NO EF Core.** Dapper + FluentMigrator only (backend).
- **NO secrets** in code. Use env vars.
- **Arabic + RTL** for all user-facing content (frontend).
- **Idempotent migrations** (backend).

## Work Guidance

### Backend
- `cd src/backend && dotnet build && dotnet test`
- Run on port 5001: `dotnet run --project Host`
- New module → create in `src/backend/Modules/<ModuleName>/` with `AGENTS.md`.

### Frontend
- `cd src/frontend && npm install && npm run dev`
- Run on port 3000.
- New page → create in `src/frontend/app/(authenticated)/<route>/` with `loading.tsx` and `error.tsx`.
- New component → create in `src/frontend/components/`.

## Verification

- [ ] Backend: `dotnet build` + `dotnet test` pass.
- [ ] Frontend: `npm run typecheck` + `npm run build` pass.
- [ ] No `tenant_id` references: `grep -r "tenant_id" src/`.
- [ ] No secrets in code: `grep -rE "(password|api_key)\s*=" src/`.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`src/backend/`](./backend/) | .NET 9 backend (13 modules + shared + tests) | Active |
| [`src/frontend/`](./frontend/) | Next.js 14 frontend (App Router) | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
