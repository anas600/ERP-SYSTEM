# 📜 CONSTITUTION — ERP-SYSTEM

> **Strategic Reference** · Not a sprint plan, not a roadmap, not a changelog.
> This file is the permanent governance document of the ERP-SYSTEM project.
> Any change to this file requires explicit owner (Anas) approval.

**Last amended:** 2026-07-29 18:25 UTC (Anas directive: **PROJECT PAUSED for 2 days**)
**Status:** ⏸️ **PAUSED** (supersedes all prior implicit architectural assumptions)

> ## ⚠️ PAUSED PER ANAS (2026-07-29 18:25 UTC)
>
> **This constitution is PAUSED for 2 days** per Anas's directive to speed up work and coordination between teams in a single environment.
>
> **Active (temporary permanent) constitution:** [`.github/workflows/mavis-coordination/constitution.md`](./.github/workflows/mavis-coordination/constitution.md)
>
> **What's the same:**
> - Architectural constraints (company_id only, no EF Core, Dapper + FluentMigrator)
> - Stack (.NET 9, Next.js 14, PostgreSQL, BCrypt 12)
> - DOX framework, CHANGELOG discipline, branch protection
>
> **What's different (2-day temporary period):**
> - **Coordination:** Smart cron (state.json + .github/workflows/mavis-coordination/state-cron.yml) is the primary async signal. **No Telegram ping-pong.**
> - **Admin team (Cloud) = سيتی + محمد + ديف** work as "Cron Jobs" coordinated by Mavis Local. Their expertise is async-delivered via the state machine.
> - **Mavis Local = sole Tech Lead + Coordinator** for the next 2 days. Has freedom to coordinate directly with the admin team via the state.json.
> - **Sprint hand-offs:** `docs/workflow/sprint-N.md` format unchanged. **Single state file = single ping-pong point.**
>
> **End of pause:** 2026-07-31 18:25 UTC. After that, revert to CONSTITUTION.md as primary, demote mavis-coordination/constitution.md to secondary.
>
> **If you have questions or escalations:** check `.github/workflows/mavis-coordination/state.json` first (it tells you where the ball is). Then contact the relevant party per Article 2 of the active temporary constitution.

---

## 🎯 Article 1 — Project Identity

| Field | Value |
|-------|-------|
| **Name** | ERP-SYSTEM (Multi-Company ERP for Libyan SMEs) |
| **Owner** | Anas (anas600 on GitHub) |
| **CTO / Relay** | Siti (operational relay to Anas via Telegram) |
| **Cloud Coordinator** | Siti (same session as Mavis, mode-switched) |
| **Architect / Strategic Advisor** | Muhammad (Mavis, strategic mode) |
| **Tech Lead (Local)** | Mavis Local (Windows, full admin on develop) |
| **DevOps** | Dev (Mavis, devops mode) |
| **Production** | Hugging Face Space (`anas-assasket-erp-system`) |
| **Database** | Supabase (PostgreSQL 17, eu-central-1) |
| **License** | Private — all rights reserved |

**Mission:** Build a fast, reliable, multi-company ERP system that serves Libyan SMEs (small/medium enterprises) with full Chart of Accounts, multi-branch inventory, AP/AR, payroll, and reporting — **under a single Holding Company per deployment** (not multi-tenant SaaS).

---

## 👥 Article 2 — Team & Roles

| Agent | Role | Responsibilities |
|-------|------|------------------|
| **Mavis Cloud (Siti/Muhammad mode)** | Cloud Coordinator + Architect | Plan, write hand-offs, verify PRs, merge, governance |
| **Mavis Local (Windows)** | Tech Lead | Delegate to Jimis, verify, open PRs (--admin per Article 11) |
| **Jimi تنفيذي** | Backend developer | Backend tasks (controllers, services, repos, tests) |
| **Jimi تحليلي** | Frontend developer | Frontend tasks (pages, components, RTL, i18n) |
| **Mephisto (Hermi)** | External Tech Lead (sandbox only) | Independent development on his own remote branch (see Article 14) |
| **Abdo's team** | E2E verification team | Runs Playwright tests + sign-off |

**Mode switching (Mavis = single agent, multiple modes):**
- Mode set by `Muhammad | Siti | Dev | Mavis-Local` token at session start
- Default: Muhammad (strategic)
- Cloud = Siti (when coordinating)
- Local = Mavis-Local (when on Windows machine)
- Dev = Dev (when doing CI/infra)

---

## 🏗️ Article 3 — Architecture (Multi-Company, NOT Multi-Tenant)

| ✅ HAVE | ❌ DO NOT HAVE |
|---------|----------------|
| `company_id` | `tenant_id` |
| `Holding` (one, in `holdings` table) | `Tenant` entity |
| `Company` (many, in `companies` table) | `Tenant` class |
| `CompanyContext` | `TenantContext` |
| `CompanyMiddleware` | `TenantMiddleware` |
| `[CompanyAuthorize]` | `[TenantAuthorize]` |
| `user_companies` join table | `user_tenants` |
| JWT `company_ids[]` claim | JWT `tenant_ids[]` |
| `X-Company-Id` header | `X-Tenant-Id` header |
| Multi-Company (1:N Holding:Companies) | Multi-Tenant SaaS |

**Rules:**
1. Every "company-scoped" table MUST have `company_id` column.
2. Every API endpoint MUST filter by `company_id` from JWT or `X-Company-Id`.
3. Row-Level Security (RLS) on Postgres is **defense in depth** — not a substitute for app-layer filtering.
4. **NO** row may reference a company the user doesn't have access to (via `user_companies`).
5. **Holding-level queries** (Consolidated reports, Treasury) require `holding_admin` role + bypass the company filter.

**Source of truth for this article:** `/docs/architecture/holding-company-architecture.md`

---

## 🌿 Article 4 — Branch & Environment Discipline

| Layer | Branch | DB | Access | Status |
|-------|--------|----|----|--------|
| **Local** | any `feature/*` | Local Docker | Owner only | Active |
| **Dev** | `develop` | Supabase dev | Mavis Local + Mavis Cloud | Active |
| **Staging** | (Frozen) | Supabase staging | — | **FROZEN per Article 11** |
| **Production** | `main` | Supabase production | — | **FROZEN per Article 11** |

**Remote branches allowed (after 2026-07-29 cleanup):**
- `main` (production)
- `develop` (integration)
- `feature/abdo-team` (Abdo's E2E work)
- `feature/sprint-4-polish-demo-data` (Mephisto's remote)
- (additional `feature/*` branches for Mavis Local, Jimis, Abdo)

**Required CI checks (6):**
1. Backend Tests (.NET 9.0)
2. Frontend Build (Next.js 14)
3. CodeQL
4. TruffleHog
5. Analyze (javascript-typescript)
6. Analyze (csharp)

**Branch protection (current state — last updated 2026-07-31):**
- Required reviews: 1
- Admin bypass: ⚠️ **NOT actually ON** — `enforce_admins: true` is set, so admins must follow the same rules. Use the **temporary-relax pattern** (per Article 10 + Sprint 14 retro): relax `required_pull_request_reviews: null` + `required_conversation_resolution: false` → merge → restore.
- Force-pushes: ❌ DISABLED (per branch architecture reset 2026-07-31)
- Enforce admins: ✅ true (strict)
- Linear history: ON
- Playwright E2E: optional (per Article 12)

---

## 🔄 Article 5 — Workflow Discipline

**Single source of truth = remote GitHub.** All work happens on remote.

**Sprint model (since 2026-07-28):**
- 1 sprint = 1.5-2 hours
- Block A (Backend, BE Jimi) + Block B (Frontend, FE Jimi) parallel
- Block C (Mavis Local: verify + open PR)
- Cloud (Siti) auto-merges via `--admin` (per Article 11)

**Hand-off protocol:**
- Cloud writes `docs/workflow/sprint-N.md` to develop
- Mavis Local pulls develop, spawns Jimis, verifies, opens PR
- Cloud auto-merges PR (squash, --admin) when CI green
- Cron `monitor-sprint-N-pr` watches and self-cleans

**No commit to `main` directly. No push to `develop` directly. All through PR.**

---

## 🤝 Article 6 — Delegation Protocol

**Cloud (Siti) → Local (Mavis Local) → Jimis (BE+FE parallel)**

- **Cloud** writes hand-off, sets presence signal, monitors PRs, merges.
- **Mavis Local** delegates to Jimis, verifies their work, opens PR, tells Cloud when done.
- **Jimis** execute tasks in parallel, never spawn further sub-agents.
- **Anas** is the decision-maker and approver. He is the only one who can change Constitution.

**Crons:**
- `presence-check` (5 min, Cloud only)
- `monitor-sprint-N-pr` (2 min, Cloud only)
- `bridge-cron` (6h, HOLD FIRE per DEC-038)
- ❌ Local should NOT have coordination crons (Mephisto's mistake)

---

## 🧪 Article 7 — Quality & Decision Standards

| Decision | Standard |
|----------|----------|
| **Atomicity** | Any service method that inserts > 1 row MUST use a single transaction. |
| **Performance** | Batch INSERTs via Postgres `unnest()` for ≥ 10 rows. No N+1 queries. |
| **Resiliency** | All Npgsql connection strings get Resiliency baseline (CommandTimeout=60s, KeepAlive=30s). |
| **Migrations** | Idempotent (`CREATE TABLE IF NOT EXISTS`, `DO $$ ... IF EXISTS ... $$` guards). |
| **JSON schemas** | Source of truth for additive schema. `JsonMigrationEnabled=true` in production. |
| **Cold start** | Backend must respond in < 60s on first request after deploy. |
| **API** | API-First: Backend before Frontend. One test per endpoint. |
| **Secrets** | NEVER in code or chat. Use env vars or secret manager. |

---

## 🧪 Article 8 — 10 Soft Rules (Demo Architecture)

| # | Rule | Description |
|---|------|-------------|
| 1 | **One Branch (develop only)** | All work in develop; no fork chaos. |
| 2 | **API-First** | Backend before Frontend. |
| 3 | **Idempotent Migrations** | `IF EXISTS` on every migration. Safe to re-run. |
| 4 | **One Test Per Endpoint** | Smoke test, not coverage. |
| 5 | **company_id Only** | No tenant_id, no multi-tenancy. |
| 6 | **No EF Core** | Dapper + FluentMigrator only. |
| 7 | **Pre-Demo Data** | Real, not mocks. |
| 8 | **No Secrets in Code** | Env vars only. |
| 9 | **Frontend-First Errors** | Arabic + English messages to user. |
| 10 | **Document in AGENTS.md** | Every decision documented. |

**5 Anti-Patterns (avoid):**
- ❌ Over-engineering → YAGNI
- ❌ Premature optimization → Profile first
- ❌ Speculative features → Build what you need
- ❌ Custom solutions → Use libraries
- ❌ Long sync tasks → Async / queue

---

## 🧹 Article 9 — Cleanup & Memory Hygiene (NEW 2026-07-29)

**Purpose:** Prevent hallucination drift. Keep the repository lean and signal-dense.

**Mandatory cleanup rules:**

1. **Constitution = ONLY governance file.** All DEC-* (decision) files are merged into CONSTITUTION.md and deleted.

2. **CHANGELOG.md** records all changes at sprint level. Per-decision docs are forbidden.

3. **docs/workflow/** = roadmap + sprint files ONLY. No per-cycle artifacts, no per-hand-off dumps.

4. **docs/architecture/** = Architecture documentation from Siti ONLY. One doc per architecture topic.

5. **AGENTS.md** is per-directory context. Updated in same scope as code change.

6. **Old hand-offs, old cycles, old governance files = DELETE.** Keep no more than 90 days of operational history.

7. **Branches:** Keep `main`, `develop`, and active `feature/*` for current work. Delete merged/abandoned feature branches within 7 days of merge.

8. **No old `tenant_*` references anywhere.** `tenant_id`, `TenantContext`, `TenantMiddleware`, `[TenantAuthorize]` are FORBIDDEN. Use `company_*` equivalents.

9. **No "Multi-Tenant" language.** Use "Multi-Company" (Article 3).

10. **Memory hygiene for agents:** Each agent's MEMORY.md is append-only on `append`. Use `memory_search` to find what you need. Don't re-read full MEMORY.md.

---

## 🏛️ Article 10 — Local Team Empowerment (from DEC-070) + Sprint 17 Update

**Effective date:** 2026-07-27
**Last amended:** 2026-08-01 (Sprint 17: clarified admin bypass to match GitHub reality)

**Decisions:**

1. **Staging/Production FREEZE:** No work on staging or production layers without explicit Anas approval. Both layers are frozen until further notice.

2. **Mavis Local = Tech Lead (full admin on develop):**
   - Can self-merge PRs via the **temporary-relax pattern** (see Sprint 14 retro: relax `required_pull_request_reviews: null` + `required_conversation_resolution: false` → merge → restore). The `--admin` flag alone is **not sufficient** because GitHub's `enforce_admins: true` makes admins follow the same rules.
   - Can push to develop via PR (no direct push)
   - Can spawn Jimis (BE+FE) in parallel
   - Can manage branches and tags on develop
   - Can delete merged feature branches

3. **Playwright E2E tests are OPTIONAL for PR merge** (per Article 12).

4. **Mavis Local leads Jimis (Jimi تنفيذي + Jimi تحليلي):**
   - Mavis Local writes hand-off → Jimis execute in parallel → Mavis Local verifies → Mavis Local opens PR
   - Cloud (Siti) auto-merges via monitor cron
   - No sync between Jimis; they only sync through Mavis Local

5. **Two-Mode Workflow (Sprint 17):**
   - **Mode 1 (Development):** Admin is the team lead for the local execution team (Jimis). Coordinates sprints being merged locally. NO push to remote. NO CI runs. NO mvp-docker rebuild. NO Telegram notify.
   - **Mode 2 (Release):** Admin waits for Anas to say "ادفع". Then does the workflow: git push + gh pr create + (wait for CI green) + relax + squash-merge + tag + restore. After merge, the cron `mvp-auto-rebuild-on-develop-push` fires on the remote → mvp-docker rebuilds → smoke test → Telegram ping.
   - The switch between modes is controlled by **Anas** (only he can say "ادفع").

6. **Anas = Project Owner.** Only Anas can:
   - Approve staging/production changes
   - Change Constitution
   - Approve architecture changes
   - Switch from Mode 1 to Mode 2 (by saying "ادفع")

---

## 🧪 Article 11 — Test Strategy (from DEC-071)

**Effective date:** 2026-07-27

**Test tier policy:**

| Tier | When | Required? | Coverage Target |
|------|------|-----------|-----------------|
| **Unit tests** | Every PR | ✅ Required | One test per endpoint (smoke) |
| **Integration tests** | Every PR | ⚠️ Optional | One happy-path test |
| **E2E (Playwright)** | Pre-release | ❌ NOT required for merge | Critical paths only |
| **Performance tests** | Post-merge | ❌ Deferred | TBD |

**Risk tolerance:**
- We accept broken `develop` builds temporarily. Recovery via revert.
- Real concern is `main` (production). Production is FROZEN per Article 10.
- Don't spend > 30% of sprint time on tests. Focus on features.

**Don't worry about test coverage percentage. Focus on the critical path:**

1. Auth flow (login, JWT, company switch)
2. Company CRUD (list, get, create)
3. User management (list, get, companies)
4. Activity feed (recent)
5. Notifications (list, mark as read)

---

## 📡 Article 12 — Presence Check Protocol (from DEC-072)

**Effective date:** 2026-07-28

**Purpose:** Lightweight "are you alive?" between Cloud and Local.

**Mechanism:**

1. **Anas → Local:** Writes `docs/governance/presence-signal.json` on develop (commit + push).
2. **Cloud cron (`presence-check`, 5 min):** Detects signal, posts status comment on develop HEAD, deletes signal.
3. **Self-cleaning:** One comment per signal. Marker `.last-responded` prevents duplicates.

**Signal JSON format:**
```json
{
  "from": "siti-cloud | mavis-local | jimi-X",
  "to": ["siti-cloud" | "mavis-local" | "jimi-X"],
  "timestamp": "ISO-8601",
  "message": "Free-text status",
  "details": { ... }
}
```

**Constraints:**
- Only Anas can create presence signals (project owner).
- Crons stay silent on no-signal.
- Signal file is deleted after use.

---

## 🦾 Article 13 — Mephisto Role (NEW 2026-07-29)

**Mephisto** is an external Tech Lead agent (not part of Mavis/Jimi team) operating in his own sandbox environment.

**Role:**
- Independent development on `feature/sprint-4-polish-demo-data` (his own remote branch).
- Reports to Anas directly.
- Has full technical freedom in his sandbox and local environment.
- Can install any packages he needs to run the system locally.

**Constraints:**
- MUST comply with CONSTITUTION.md (especially Article 3 — no tenant_id).
- MUST NOT push to `develop` or `main` directly.
- MUST rebase from latest `develop` before opening PR.
- MUST coordinate with Mavis Local if working on overlapping files.
- MUST update AGENTS.md and CHANGELOG.md in same PR.

**Pre-PR checklist:**
- ✅ Rebase on develop
- ✅ Tests pass (`dotnet test` and `tsc --noEmit`)
- ✅ Architecture compliance (Article 3, Article 8)
- ✅ AGENTS.md + CHANGELOG.md updated
- ✅ One test per endpoint
- ✅ No secrets in code

**Permission to use --admin:** ❌ No. Only Mavis Local has --admin per Article 10. Mephisto waits for Cloud (Siti) review.

**Why Mephisto exists:**
- To accelerate development without blocking on Cloud coordination.
- To give Anas a parallel workstream.
- To leverage Mephisto's sandbox capabilities (Postgres, runtime).

---

## ✏️ Article 14 — Constitution Amendment Process

1. **Proposal** by Mavis (any mode) or Anas, with rationale and impact analysis.
2. **Review** by Anas (owner) — explicit approval required.
3. **Update** the file with `[Amended YYYY-MM-DD: <reason>]` in the relevant article.
4. **Commit** on develop with `docs(constitution): amend Article N — <reason>`.
5. **Merge to main** after Phase completion (not after every amendment).

**No silent amendments. No retroactive changes. This file is append-only on history.**

---

## 📞 Article 15 — Communication Protocol

- **Anas ↔ Mavis Cloud:** Direct, in-session messages.
- **Mavis Cloud ↔ Mavis Local:** Async via `docs/governance/presence-signal.json` (Article 12).
- **Mavis Local ↔ Jimis:** Via `task` tool (sub-agents). Each task has clear deliverable.
- **Anas ↔ Mephisto:** Direct, in Mephisto's session.
- **Anas ↔ Siti:** Via Telegram (Siti relays to Mavis).
- **Hand-Off report** at the end of every Mavis response includes: (a) current system state, (b) changes made, (c) what's next.

---

**This Constitution supersedes all prior architectural assumptions and sprint-level decisions. Any change requires Anas's explicit approval.**

_Last amended: 2026-07-29 by Mavis (Muhammad mode), approved by Anas — Cleanup Amendment (Articles 11-15 restructured; hallucination reset)_
