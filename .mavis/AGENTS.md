# 🤖 .mavis/AGENTS.md — Worker (Jimi) Instructions

> **The contract for every local worker (Jimi) Mavis Local spawns.**
> Read this file fully before starting any work. Follow it strictly.

**Owner:** Mavis Local (Tech Lead + Coordinator)
**Last updated:** 2026-07-29 19:15 UTC (per Anas mandate)
**Status:** 🟢 **ACTIVE** (supersedes the "TO CREATE" status in the root `AGENTS.md` child DOX index)

---

## 🎯 Purpose

This file tells every Jimi spawned by Mavis Local **how to work**, **what to document**, and **what NOT to do**. It exists so that:

1. **No work is undocumented.** Every code change lands with a paper trail.
2. **Scope is explicit.** Each Jimi knows exactly what they own and what they don't.
3. **Verification is fast.** Mavis Local can review the Jimi output in minutes, not hours.
4. **DOX is enforced.** The nearest AGENTS.md is always updated when code under it changes.

---

## 👥 Who is a Jimi?

A **Jimi** is a sub-agent (Mavis spawned in a separate session) that executes a single slice of work. They can be:

- **BE Jimi** — Backend (C# / .NET 9 / Dapper / FluentMigrator / xUnit)
- **FE Jimi** — Frontend (TypeScript / Next.js 14 / Tailwind / shadcn/ui / Jest)
- **Dev Jimi** — Infrastructure / CI / scripts
- **Doc Jimi** — Documentation-only changes

Mavis Local spawns 2 Jimis in parallel (typically BE + FE) for each sprint, per the Lead Cycle in [WORKFLOW.md § Leadership Cycles](../WORKFLOW.md#-leadership-cycles-mavis-locals-role-as-coordinator).

---

## 📋 Pre-flight (mandatory)

Before you write a single line of code, **in order**:

### 1. Read the workflow file (root)
```bash
cat WORKFLOW.md
```
Understand the 8 articles, especially:
- Article 2 (ball locations — you don't move the ball, Mavis Local does)
- Article 3 (update rules — you don't write to state.json)
- Article 5 (cron behavior — the cron is a tool, not an actor)

### 2. Read the root AGENTS.md (DOX rail)
```bash
cat AGENTS.md
```
Walk to **every** AGENTS.md in the path between the root and the files you expect to touch. The closest AGENTS.md is your local contract.

### 3. Read your module's AGENTS.md (if exists)
Example: if you're a BE Jimi working on `src/backend/Modules/Activity/`, read `src/backend/Modules/Activity/AGENTS.md` if it exists. If it doesn't, you may **create it** (per DOX).

### 4. Read the sprint hand-off
```bash
cat docs/workflow/sprint-N.md
```
Find the T# (task number) that Mavis Local assigned you. Quote it back in your work log.

### 5. Read the architecture doc
```bash
cat docs/workflow/architecture.md
```
Note the 10 soft rules. These are non-negotiable.

### 6. Read the latest CHANGELOG entry
```bash
head -50 CHANGELOG.md
```
See what's already done. Avoid re-work.

---

## 🛠️ During work (the rules)

### Rule 1: One scope, one PR slice
- You own **one** T# task (or a small group of related T#s).
- You do NOT work on tasks outside your scope.
- If you discover a related task that needs doing, **report it back to Mavis Local** — don't silently absorb it.

### Rule 2: Document your scope (the old-constitution custom, preserved)
**Before you start coding**, in the **nearest applicable AGENTS.md** (or your task notes), write:

```markdown
## Jimi Scope — <date>

**Jimi type:** BE | FE | Dev | Doc
**Sprint / Cycle:** Sprint N / Cycle N
**T# tasks:** T1, T2 (etc.)
**Branch:** feature/sprint-N-<slug>
**Files I will touch:**
- path/to/file1.cs (new)
- path/to/file2.tsx (modify — explain)
**Files I will NOT touch:**
- path/to/other.cs (out of scope)
**Tests I will add:**
- 1 test per endpoint (per Article 11)
**Constitution articles I must respect:**
- Article 3 (company_id only)
- Article 8 Rule 6 (no EF Core)
- (etc.)
```

This block is your **scope declaration**. It goes either:
- In the nearest module-level `AGENTS.md` (preferred for module-specific work)
- In your work log / response back to Mavis Local (for cross-cutting work)

### Rule 3: CHANGELOG entry (mandatory)
Before you finish, add a `### Added` / `### Changed` / `### Fixed` block under the current sprint section in [`CHANGELOG.md`](../CHANGELOG.md). One Jimi, one entry. Keep it factual and short.

Example:
```markdown
## Sprint 6 — CoA Polish (2026-07-29)
### Added (BE Jimi)
- `GET /api/accounts/tree` — hierarchical CoA tree with parent_account_id
- New service: `src/backend/Modules/Finance/Application/Services/ChartOfAccountsService.cs`
- 3 new tests: `ChartOfAccountsServiceTests.cs`
```

### Rule 4: Code standards (already in AGENTS.md, but emphasized)
- **Backend:** Dapper only. NO EF Core. FluentMigrator. One test per endpoint (xUnit).
- **Frontend:** TypeScript strict. Tailwind + shadcn/ui. No new framework.
- **Migrations:** Idempotent (`CREATE TABLE IF NOT EXISTS`, `DO $$ ... IF EXISTS ... $$`).
- **Batch inserts:** Postgres `unnest()` for ≥ 10 rows. No N+1.
- **Atomicity:** Multi-insert in single transaction.
- **API-First:** Backend before Frontend. One test per endpoint.
- **No `tenant_id`.** Ever. `company_id` only. (Constitution Article 3.)
- **No secrets** in code, chat, or PRs.
- **Bilingual errors** in frontend (AR + EN).

### Rule 5: Conventional Commits
Format: `<type>(<scope>): <subject>`
- `feat(be):` / `feat(fe):` / `feat(dev):` / `feat(docs):`
- `fix(be):` / `fix(fe):` / ...
- `chore:`, `refactor:`, `test:`, `docs:`, `ci:`

Subject ≤ 72 chars, imperative mood, no trailing period.

### Rule 6: DOX pass before you finish
Before you report "done" to Mavis Local:
- Did you touch files that the nearest AGENTS.md covers? Update it.
- Did you create a new durable boundary (module, route, workflow)? Create its AGENTS.md.
- Did you add a CHANGELOG entry? Yes.
- Does the new module have a placeholder for `AGENTS.md`? Write one (even 5 lines).

### Rule 7: Self-verify before reporting
- BE Jimi: `dotnet build` (0 errors) + `dotnet test` (all green) + verify your new test passes.
- FE Jimi: `npm run typecheck` (0 errors) + `npm run build` (success).
- Dev Jimi: workflow runs locally or syntax-validates.

If something fails, **fix it yourself first.** Only escalate to Mavis Local if it's a deeper issue (architecture, missing dependency, unclear requirement).

### Rule 8: One branch, one commit (or small logical commits)
- Branch: `feature/sprint-N-<slug>` (Mavis Local gives you the branch name)
- Commit your slice with Conventional Commits format
- Push to your own fork if Mavis Local instructs; otherwise push to the team fork

### Rule 9: Report back to Mavis Local
When done, send Mavis Local a **summary** with:
- Branch name + commit hash
- Files added/modified (count)
- Lines added/removed
- Tests added (count) + pass status
- Build + typecheck status
- Any out-of-scope discoveries (for Mavis Local to decide)
- Your CHANGELOG entry (copy-paste)

---

## 🚫 What you MUST NOT do

1. ❌ **Do not** move the ball in `state.json` — that's Mavis Local's job
2. ❌ **Do not** open a PR — Mavis Local opens it
3. ❌ **Do not** self-merge — Mavis Local self-merges per DEC-070
4. ❌ **Do not** push to `develop` or `main` directly
5. ❌ **Do not** use `tenant_id` (ever, anywhere)
6. ❌ **Do not** add EF Core, secrets, mocks, or untracked files
7. ❌ **Do not** create files outside your scope
8. ❌ **Do not** modify `CONSTITUTION.md` or `WORKFLOW.md` (governance files are Mavis Local's job, requires Anas approval)
9. ❌ **Do not** delete files (use `.mavis-trash` if absolutely needed, prefer to keep history)
10. ❌ **Do not** start work without reading this file + the sprint hand-off + the relevant AGENTS.md

---

## ✅ Quick checklist before you report "done"

```
[ ] I read WORKFLOW.md, AGENTS.md, my module's AGENTS.md, the sprint hand-off, and architecture.md
[ ] I declared my scope in the nearest AGENTS.md (Scope block)
[ ] I added a CHANGELOG entry under the current sprint
[ ] I added at least 1 test per new endpoint (BE) or per new component (FE)
[ ] Build / typecheck / test all pass
[ ] I committed with Conventional Commits format
[ ] I reported back to Mavis Local with the summary
[ ] No `tenant_id`, no secrets, no EF Core, no untracked files
```

---

## 🔄 When you finish

You are **done** when:
1. Mavis Local acknowledges your summary
2. Mavis Local opens the PR (or assigns you to push)
3. The CHANGELOG entry is committed
4. The scope block in the nearest AGENTS.md is committed

You are **not done** when:
- The code compiles but you haven't added tests
- The tests pass but you haven't updated CHANGELOG
- The CHANGELOG is updated but you haven't declared your scope
- You've pushed but haven't told Mavis Local

---

## 📞 When to escalate to Mavis Local

- **Architecture conflict** — the sprint hand-off seems to violate a Constitution article
- **Missing requirement** — the T# task is unclear or contradictory
- **Out-of-scope discovery** — you found a related bug/feature that needs a decision
- **Blocked on a dependency** — you need another Jimi to finish first
- **CI failure you can't fix** — token issue, infra problem, etc.

Don't silently re-scope. Don't silently absorb related work. **Report and wait.**

---

_Last updated: 2026-07-29 19:15 UTC by Mavis Local, per Anas mandate (worker instructions for local team)_
