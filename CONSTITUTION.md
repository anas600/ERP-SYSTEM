# 📜 CONSTITUTION — ERP-SYSTEM

> **Strategic Reference** · Not a sprint plan, not a roadmap, not a changelog.
> This file is the permanent governance document of the ERP-SYSTEM project.
> Any change to this file requires explicit owner (Anas) approval.

**Last amended:** 2026-07-25 (Phase 6 — Multi-Company Refactoring kickoff)
**Status:** Active (supersedes all prior implicit architectural assumptions)

---

## 🎯 Article 1 — Project Identity

| Field | Value |
|-------|-------|
| **Name** | ERP-SYSTEM (Multi-Company ERP for Libyan SMEs) |
| **Owner** | Anas (anas600 on GitHub) |
| **CTO / Relay** | Siti (operational relay to Anas via Telegram) |
| **Production** | Hugging Face Space (`anas-assaket-erp-system`) |
| **Database** | Supabase (PostgreSQL 15, eu-central-1) |
| **License** | Private — all rights reserved |

**Mission:** Build a fast, reliable, multi-company ERP system that serves Libyan SMEs (small/medium enterprises) with full Chart of Accounts, multi-branch inventory, AP/AR, payroll, and reporting — **under a single Holding Company per deployment** (not multi-tenant SaaS).

---

## 👥 Article 2 — Team & Roles

| Agent | Role | Responsibilities |
|-------|------|-----------------|
| **Mavis** (orchestrator) | Tech Lead, Orchestrator, Code Reviewer, Final Approver | High-level analysis, planning, architecture decisions, code review, final approval, memory management, hand-off reports |
| **Jamie Executive** | Implementer (producer) | Backend/frontend code, migrations, infra, CLI scripting, **executes approved plans only** |
| **Jamie التحليلي** | Analyst & Verifier (verifier) | Code review, conflict resolution, hotfixes, performance/perf analysis, atomicity proofs, **never ships code without orchestrator approval** |

**Delegation rule:** Mavis plans and reviews. Jamies execute. No code is written by Mavis directly unless the task is trivial (single-line fix) or time-critical (production hotfix with no team available).

---

## 🏗️ Article 3 — Architecture (Multi-Company, NOT Multi-Tenant)

> **Approved:** 2026-07-25 by Anas + Siti. This **supersedes** all prior multi-tenant assumptions in DEC-019, DEC-091, DEC-105, etc.

### 3.1 The model

- **One Holding Company** per deployment (a single legal parent entity).
- **Many Subsidiary Companies** (branches, divisions, legal entities) under the Holding.
- **Users** are scoped to **one or more Companies** via a `user_companies` join table (no global "tenant" concept).
- **All data** is partitioned by `company_id` (NOT `tenant_id`).

### 3.2 Schema principles

| Concept | Decision |
|---------|----------|
| **Outer isolation** | NONE (no `tenant_id` column anywhere) |
| **Inner isolation** | `company_id` on every business table (FK → `companies.id`) |
| **Holding** | First row in `companies` with `is_group = true`, `parent_company_id = NULL` |
| **Subsidiaries** | Rows in `companies` with `is_group = false`, `parent_company_id = holding.id` |
| **Cross-company data** | Allowed for shared lookups (e.g., customers, items) via `is_shared` flag |
| **Per-company data** | Filtered by `company_id` (CoA, journals, vendors, employees, payroll) |

### 3.3 Auth & session

- **Register** = create the first user under the default Holding Company (no tenant creation wizard).
- **Login** = returns JWT with `user_id` + `default_company_id` + `company_ids[]` (list of accessible companies).
- **No TenantMiddleware**. The frontend's company switcher is a `company_id` picker.
- **Authorization** = `[CompanyAuthorize(companyId)]` attribute (replaces `[TenantAuthorize]`).

### 3.4 Migration strategy

- **Clean slate allowed.** The old `tenant_id` schema can be dropped entirely.
- A new **Initial Schema** migration creates all tables without `tenant_id`.
- All FluentMigrator migrations are reset (versioninfo table dropped) before the Initial Schema runs.
- JSON schema files in `Host/data-types/*.json` are regenerated to match the new schema.
- Old seed data (AlFajr, AlBurj, Realistic) is regenerated without `tenant_id`.

### 3.5 What we DROP from the old model

- ❌ `tenant_id` column on every table
- ❌ `Tenant` entity + `tenants` table
- ❌ `ITenantContext` / `TenantContext` / `TenantMiddleware`
- ❌ `[TenantAuthorize]` attribute
- ❌ `OnTenantCreatedAsync` bootstrap (replaced by `OnCompanyCreatedAsync` or `SeedDefaultHoldingAsync`)
- ❌ Multi-tenant login queries (`WHERE tenant_id = @TenantId`)
- ❌ Subdomain-based tenant routing
- ❌ Atomic register flow for tenant creation (replaced by simple user creation under existing Holding)

### 3.6 What we KEEP

- ✅ Multi-Company (already partially implemented in Phase 5.A via `companies` table)
- ✅ Chart of Accounts hierarchy (parent_account_id)
- ✅ Atomic transactions for multi-insert flows (general pattern, not tenant-specific)
- ✅ JWT + Refresh Token auth
- ✅ RBAC via `roles` + `user_roles` + `user_companies`
- ✅ All 7 modules (Identity, Companies, Finance, Projects, Inventory, Reports, Notifications, AccountsReceivable, Payments, Procurement, HR, Payroll)
- ✅ Playwright E2E suite (modified to remove tenant assertions)

---

## 🌿 Article 4 — Branch & Environment Discipline

| Branch | Role | Rules |
|--------|------|-------|
| `main` | **Production only.** Stable, deployed to HF Space. | `enforce_admins: true` · 1 review · `Build and Deploy to HF` required check. **PRs from develop only. No direct push except admin bypass for solo owner (Anas).** |
| `develop` | **Integration.** All work happens here. | `enforce_admins: false` · 1 review · `CI + Deploy` + `Playwright E2E` required. Direct push allowed for solo owner. **Locked against re-deploys to HF** (no automatic trigger to main from develop pushes). |
| `feature/*`, `fix/*`, `hotfix/*`, `docs/*` | Working branches | None (free) |

**Re-deploy policy:** Every push to `main` triggers `build-and-deploy-hf.yml` → ~3-5 min build + cold start (consumes compute hours). **Avoid redundant main pushes.** Consolidate fixes into single PRs.

**Develop auto-deploy:** Develop pushes do **NOT** auto-deploy to HF. E2E runs in CI only. Manual deploy via `workflow_dispatch` if needed.

---

## 🔄 Article 5 — Workflow Discipline

### 5.1 Pre-PR checklist (mandatory)

1. Run `dotnet build` (backend) + `tsc --noEmit` (frontend) — 0 errors, 0 new warnings.
2. Run unit tests — 100% pass.
3. Run `npm run e2e` (Playwright) on develop — 0 failures.
4. Update relevant `AGENTS.md` files (root + module).
5. Update `docs/CHANGELOG.md` with sprint entry.
6. Update memory if new pattern/gotcha learned.

### 5.2 PR flow

```
feature/* → develop (squash merge, CI must pass)
develop  → main    (squash merge, admin only, Build and Deploy to HF must pass)
```

- **Squash merge** for clean linear history.
- **Conventional Commits** prefixes: `feat:`, `fix:`, `perf:`, `chore:`, `docs:`, `refactor:`.
- **Commit body** must include `Refs: <DEC-NNN>` or `Refs: <PR-NNN>` for traceability.

### 5.3 Conflict resolution

- Develop diverges from main → create `fix/sprintN-merge-conflicts` from `origin/develop` (NOT local develop).
- `git merge origin/main`, `git checkout --ours <conflicted>`, commit, push, open new PR, admin merge.
- See memory entry: "Solo-owner PR conflict resolution" (2026-07-25).

---

## 🤝 Article 6 — Delegation Protocol

### 6.1 When Mavis delegates

- **Analysis task** → Jamie التحليلي: "Analyze X and produce findings, no code changes."
- **Implementation task** → Jamie Executive: "Implement approved plan Y, deliver PR on develop."
- **Verification task** → Jamie التحليلي: "Verify Z matches spec, report any deltas."

### 6.2 Delegation format

Every delegation includes: (a) scope, (b) constraints, (c) success criteria, (d) time-box, (e) hand-off expectations.

### 6.3 Escalation

- **Ambiguity** → Mavis resolves, no Jamies guess.
- **Cross-cutting change** → Mavis drafts the plan, both Jamies review.
- **Production incident** → Mavis + Jamie Executive fix; Jamie التحليلي reviews post-mortem.

---

## 🧪 Article 7 — Quality & Decision Standards

| Decision | Standard |
|----------|----------|
| **Atomicity** | Any service method that inserts > 1 row MUST use a single transaction with `(conn, tx, ct)` repo overloads. |
| **Performance** | Batch INSERTs via Postgres `unnest()` for ≥ 10 rows. No N+1 queries. |
| **Resiliency** | All Npgsql connection strings get Resiliency baseline (CommandTimeout=60s, KeepAlive=30s, Pool=1-20). |
| **Migrations** | Idempotent (`CREATE TABLE IF NOT EXISTS`, `DO $$ ... IF EXISTS ... $$` guards). All migrations defensive against missing tables. |
| **JSON schemas** | Source of truth for additive schema. `JsonMigrationEnabled=true` in production. No `--` comments (JSON spec doesn't allow them). |
| **Cold start** | Backend must respond in < 60s on first request after deploy. Anything > 60s gets the HF Caddy 504. |
| **E2E** | Every PR must include/update Playwright tests for affected flows. |

---

## 📚 Article 8 — Memory & Documentation

- **Agent memory** (Mavis's `MEMORY.md`): project patterns, gotchas, workflows. Append-only on `append`. Edit only to correct/remove.
- **User memory** (`user.md`): cross-project preferences (Anas's workflow style, Telegram routing, etc.).
- **`AGENTS.md` files**: per-directory technical context. Updated in same scope as code change.
- **`docs/CHANGELOG.md`**: per-sprint entries at the top (newest first). Updated in same scope as code change.
- **`docs/PLAN.md`**: per-phase high-level plan. Updated at phase boundaries only.
- **`docs/dec-NNN/`**: decision records for major architectural choices. Created at decision points.

---

## ✏️ Article 9 — Constitution Amendment Process

1. **Proposal** by Mavis or Anas, with rationale and impact analysis.
2. **Review** by Anas (owner) — explicit approval required.
3. **Update** the file with `[Amended YYYY-MM-DD: <reason>]` in the relevant article.
4. **Commit** on develop with `docs(constitution): amend Article N — <reason>`.
5. **Merge to main** after Phase completion (not after every amendment).

**No silent amendments. No retroactive changes. This file is append-only on history.**

---

## 📞 Article 10 — Communication Protocol

- **Anas ↔ Mavis**: direct, in-session messages.
- **Mavis ↔ Jamies**: via `task` tool (sub-agents). Each task has clear deliverable.
- **Anas ↔ Siti**: via Telegram (Siti relays to Mavis).
- **Hand-Off report** at the end of every Mavis response includes: (a) current system state, (b) changes made this turn, (c) decisions required from Anas/Siti, (d) next direct step.

---

**This Constitution supersedes all prior architectural assumptions and sprint-level decisions. Any code, document, or plan that contradicts this Constitution is to be updated to align.**

_Last amended: 2026-07-25 by Mavis, approved by Anas + Siti — Phase 6 Multi-Company Refactoring kickoff._
