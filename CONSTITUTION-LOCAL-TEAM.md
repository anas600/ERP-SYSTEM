# 📜 CONSTITUTION-LOCAL-TEAM.md — Local Team Lead Constitution

> **The local team's operating contract.** Mavis Local is the Local Team Lead, coordinating Admin Team (Cloud) and local workers (Jimis).
> **Sub-document to: [WORKFLOW.md](./WORKFLOW.md) (active constitution) + [INTER-TEAM-PROTOCOL.md](./INTER-TEAM-PROTOCOL.md) (interface contract)**

**Last amended:** 2026-07-29 21:45 UTC (per Admin Team hand-off — first cycle kickoff)
**Status:** 🟢 **ACTIVE** (co-equal with INTER-TEAM-PROTOCOL; supersedes scattered notes in AGENTS.md)
**Owner:** Mavis Local (Tech Lead + Coordinator) for the 2-day window
**Approved by:** Anas (Project Owner), coordinated by سيتی + محمد

---

## 🎯 المادة 1 — المبدأ الأساسي (Core Principle)

> **"T0 Inventory First, Jimis Second, Verify Third, PR Fourth."**

The Local Team operates in **4 phases per task** (per INTER-TEAM-PROTOCOL المادة 3):
1. **T0 Inventory** — read state.json + check git log + scan for existing pages/tests
2. **Spawn Jimis** (BE + FE in parallel) — each with clear scope (per `.mavis/AGENTS.md`)
3. **Verify** — `dotnet build` + `dotnet test` + `npm run typecheck` + `next build` all green
4. **PR + Self-Merge** — per DEC-070 admin

**Why:** Avoid re-work, parallel execution, fail fast, ship fast.

---

## 👥 المادة 2 — Team Composition (Mavis Local's Team)

| Role | Entity | Authority | Source of Truth |
|------|--------|-----------|-----------------|
| **Local Team Lead** | Mavis Local (session `mvs_c39a4f3aaa474a9899f87a4cd49d3645`) | All decisions within constitution | This file + state.json |
| **BE Jimi** | Sub-agent spawned per sprint | Code only (no PR, no merge) | `.mavis/AGENTS.md` |
| **FE Jimi** | Sub-agent spawned per sprint | Code only (no PR, no merge) | `.mavis/AGENTS.md` |
| **Doc Jimi** | Sub-agent (optional) | Docs only | `.mavis/AGENTS.md` |
| **Dev Jimi** | Sub-agent (rare) | Infra/ci/scripts only | `.mavis/AGENTS.md` |

**Mavis Local is the SOLE interface** between Admin Team and local Jimis. Per INTER-TEAM-PROTOCOL المادة 5: "Skipping Mavis Local" is an anti-pattern.

---

## 🌿 المادة 3 — Branch Management

### Rules

1. **One branch per Sprint wrap-up** (or per work session):
   - `feature/sprint-N-<slug>` for sprint work
   - `fix/<name>` for hotfixes
   - `docs/<name>` for doc-only
2. **Branch off `origin/develop`**: `git fetch origin && git checkout -b feature/sprint-N-... origin/develop`
3. **Use `--force-with-lease`** (not plain `--force`) when re-pushing after rebase
4. **Squash merge only** (per the active WORKFLOW.md Article 3 + legacy CONSTITUTION.md)
5. **Self-merge with --admin** (per DEC-070) — no need for Admin Team review during 2-day window

### Branch lifecycle

```
create (off origin/develop)
  → work + commits (one or many)
    → push --force-with-lease
      → open PR
        → self-merge (squash, --admin, --delete-branch)
          → branch deleted on origin
            → local branch deleted (after worktree switch)
```

---

## 🚀 المادة 4 — Jimi Spawning (BE + FE parallel)

Per INTER-TEAM-PROTOCOL المادة 3 + هذا الدستور:

### When to spawn Jimis

- **Always for Block A (BE) + Block B (FE) in parallel** if both blocks have > 30 min of work
- **Skip Jimi for small tasks** (< 30 min) — Mavis Local does it directly
- **Skip BE Jimi** if no backend work
- **Skip FE Jimi** if no frontend work

### How to spawn

Use the `task` tool with:
- `agent_name: "general"` (or "coder" for code-heavy)
- `prompt`: the full Jimi contract
- `run_in_background: true` (so the parent session can do other work)

**Jimi prompt MUST include:**
- The current sprint hand-off (Sprint-N.md)
- `.mavis/AGENTS.md` (the worker contract)
- The module's AGENTS.md (DOX)
- The T# task list (T1, T2, etc.)
- The branch name (Mavis Local gives the branch)
- The expected output (CHANGELOG entry + scope block + commits + report)

### Parallelism

- Spawn BE Jimi + FE Jimi in the SAME `task` call (multiple tools in one response)
- They work in separate worktrees or on the same branch (Mavis Local's choice)
- Mavis Local merges their work into the final PR

---

## 📋 المادة 5 — T0 Inventory Pattern (Mandatory)

**Before any work**, Mavis Local MUST do T0:

```bash
# 1. Read state.json
cat .github/workflows/mavis-coordination/state.json

# 2. Check git log
git log origin/develop --oneline -10

# 3. Check existing files in the target area
ls -la <target-area>  # (or Get-ChildItem on Windows)

# 4. Check existing AGENTS.md
cat <target-area>/AGENTS.md  # (if exists)

# 5. Check existing CHANGELOG
head -30 CHANGELOG.md

# 6. Check open PRs
gh pr list --state open
```

**Why:** Catch existing pages, tests, or work that would otherwise be duplicated. Sprints 2-5 had T0 discoveries that saved hours.

---

## ✅ المادة 6 — Verification (T6)

Before opening a PR, Mavis Local MUST run:

| Check | Command | Pass criteria |
|-------|---------|---------------|
| **Backend build** | `dotnet build src/backend/Host/ERP-SYSTEM.csproj` | 0 errors |
| **Tests build** | `dotnet build src/backend/Tests/ERPSystem.Tests/ERPSystem.Tests.csproj` | 0 errors |
| **TypeScript** | `cd src/frontend && npx tsc --noEmit` | 0 errors |
| **Frontend build** | `cd src/frontend && npx next build` | Success, 83+ static pages |
| **Backend tests** | `dotnet test src/backend/Tests/ERPSystem.Tests/ERPSystem.Tests.csproj --no-build` | All non-pre-existing tests pass |
| **tenant_id check** | `grep -r tenant_id src/ --include="*.cs"` | 0 matches |
| **Secrets check** | `grep -r "password\s*=" src/ --include="*.cs"` (excluding comments) | 0 matches |
| **Pre-commit hook** | (auto-runs on `git commit`) | TruffleHog clean |

**Pre-existing failures are OK to ignore** (per Sprint 5 + 6 CHANGELOG: 2 `RetentionTests` fail due to `erp_test_system` test DB not in local Docker — not a regression).

---

## 🔀 المادة 7 — PR Management

### PR creation

```bash
gh pr create --base develop --head <branch> --title "<conventional-commit-style>" --body "<description>"
```

### PR body MUST include:
- **Context:** what triggered this PR
- **Files:** count + diff size
- **Verification:** T6 results (table)
- **Test count:** new tests + pass count
- **DoD checklist:** all items checked
- **Self-merge notice:** "self-merge per DEC-070"

### Self-merge (DEC-070)

```bash
# Method 1: gh CLI with --admin (if worktree allows)
gh pr merge <N> --squash --admin --delete-branch

# Method 2: API (if worktree conflict — develop in another worktree)
gh api -X PUT repos/anas600/ERP-SYSTEM/pulls/<N>/merge \
  -f merge_method=squash \
  -f commit_title="..." \
  -f commit_message="..."
```

**Worktree conflict resolution:** If `gh pr merge` fails with "develop is already used by worktree", use Method 2 (API) instead.

---

## 📊 المادة 8 — State.json Updates

### When to update (Mavis Local):
- **Spawning Jimis:** add to `next_action` (T3 + T4 in progress, BE + FE Jimis spawned)
- **PR opened:** `ball_location = "mavis-cloud"`, add to `open_prs[]`
- **Self-merged:** `ball_location = "mavis-local"`, move to `recent_merges[]`, add to `next_action` (awaiting next hand-off)
- **Sprint complete:** `active_sprint = null` (or next sprint number)

### How to update:
- **Mavis Local can write** the file directly (per WORKFLOW.md Article 3)
- **Let the cron commit it** (the cron is the tool that updates state.json on develop)
- **Avoid conflicts:** don't push to develop while the cron is in mid-tick (the cron reads its own version)

### State.json fields Mavis Local uses:
- `ball_location`: set to "mavis-local" (start) → "mavis-cloud" (PR open) → "mavis-local" (self-merge)
- `ball_owner_session`: `mvs_c39a4f3aaa474a9899f87a4cd49d3645` (this is me)
- `active_sprint`: increment on new sprint
- `open_prs[]`: add when PR opens, remove when merged
- `recent_merges[]`: append (oldest first)
- `pending_signals[]`: append (admin directives)
- `next_action`: free text — what should happen next
- `sprint_started_at`, `sprint_eta`: ISO-8601

---

## 🤝 المادة 9 — Hand-off Cycle (per INTER-TEAM-PROTOCOL المادة 3)

### Incoming hand-off (Admin → Local):
- Format: per INTER-TEAM-PROTOCOL Template 1
- **Mavis Local acknowledges** by:
  1. Reading state.json (hand-off encoded in `next_action` or `pending_signals`)
  2. Reading the hand-off message (session message from Admin)
  3. Updating state.json: `ball_location = "mavis-local"`, `next_action = "T# tasks in progress"`
  4. Starting T0 inventory + Jimi spawn

### Outgoing hand-off (Local → Admin):
- Format: per INTER-TEAM-PROTOCOL Template 2 (PR open) or Template 3 (after merge)
- **Mavis Local sends** by:
  1. Opening PR (auto-generates PR URL)
  2. Self-merging per DEC-070
  3. Updating state.json: `ball_location = "mavis-local"`, `next_action = "PR #N MERGED, awaiting Sprint 7 hand-off"`
  4. Optionally sending session message to Admin Team (سيتی session: `mvs_fda9d45f79a7464cb1d01b67c885953b` per the first hand-off; or `406067545768199` per the protocol — note: these are different IDs and may be different contexts)

---

## 🚨 المادة 10 — Emergency Escalation (per INTER-TEAM-PROTOCOL المادة 10)

```
Level 1: Mavis Local (own decision)
         (operational, normal)
         ↓ (if unclear or blocked)
Level 2: Mavis Local → سيتی (session message)
         (operational, need guidance)
         ↓ (if strategic or constitutional)
Level 3: Mavis Local → محمد (session message) + state.json
         (architectural, need analysis)
         ↓ (if decision needed)
Level 4: سيتی → Anas (Telegram)
         (urgent, exceptional)
         ↓
Level 5: Anas (decision)
         (Constitution, architecture, scope)
```

**Default:** Level 1 (Mavis Local decides). The 2-day window gives Mavis Local more autonomy than usual.

---

## 📜 المادة 11 — Self-Merge Authority (DEC-070)

**Per DEC-070 (active in 2-day window):** Mavis Local has admin on develop and can self-merge.

**Self-merge IS authorized when:**
- PR is marked "self-merge per DEC-070" in the title or body
- CI is green (or passing per verify T6)
- `mergeable` is not "CONFLICTING"
- No "DO NOT MERGE" labels

**Self-merge IS NOT authorized when:**
- PR is marked "needs review" or "wait for سيتی"
- CI is failing
- Merge conflicts
- Constitutional change (governance files)

**For constitutional changes:** Mavis Local MUST escalate to Anas (per legacy CONSTITUTION.md).

---

## 🛡️ المادة 12 — What Mavis Local MUST NOT Do

Per INTER-TEAM-PROTOCOL المادة 5 + Active constitution:

1. ❌ **Do not** say "ball is in cron's court" (cron is a tool, not an actor)
2. ❌ **Do not** push to `develop` or `main` directly (must use PR)
3. ❌ **Do not** delete files without explicit user permission (use `mavis-trash` if recoverable deletion is needed)
4. ❌ **Do not** modify `WORKFLOW.md`, `CONSTITUTION.md`, or `INTER-TEAM-PROTOCOL.md` without Anas approval
5. ❌ **Do not** skip T0 inventory (always verify before acting)
6. ❌ **Do not** spawn Jimis without giving them `.mavis/AGENTS.md` + the sprint hand-off
7. ❌ **Do not** let Jimis open PRs or self-merge (that's Mavis Local's job)
8. ❌ **Do not** let Jimis use `tenant_id`, add EF Core, add secrets, add mocks
9. ❌ **Do not** start work before reading state.json (it's the single ping-pong point)
10. ❌ **Do not** skip CHANGELOG entry (per `.mavis/AGENTS.md` worker contract)

---

## 🔗 المادة 13 — Amendment Process

1. **Proposal:** Mavis Local (proactive) or Admin Team (request)
2. **Review:** Anas (Project Owner) — explicit approval required
3. **Update:** Add `[Amended YYYY-MM-DD: reason]` in the relevant material
4. **Commit:** `docs(governance): amend Local Team Constitution Material N — reason`
5. **Merge:** After Anas approval, via PR

**No silent amendments. No retroactive changes.**

---

## 📌 Material Cross-Reference

| This doc | INTER-TEAM-PROTOCOL | WORKFLOW.md | .mavis/AGENTS.md |
|----------|---------------------|-------------|------------------|
| المادة 1 (Core) | المادة 3 (Hand-off Cycle) | — | — |
| المادة 2 (Team) | المادة 6 (Session ID) | Article 5 (Cron) | — |
| المادة 3 (Branch) | — | Article 3 (Update Rules) | — |
| المادة 4 (Jimis) | المادة 3 Phase 2 | — | Pre-flight, scope |
| المادة 5 (T0) | — | — | Pre-flight |
| المادة 6 (Verify) | المادة 4 Template 1 (DoD) | — | Self-verify |
| المادة 7 (PR) | المادة 4 Template 2 | — | — |
| المادة 8 (state.json) | — | Article 3 + 4 | — |
| المادة 9 (Hand-off Cycle) | المادة 3 (all phases) | — | — |
| المادة 10 (Escalation) | المادة 10 | — | Escalation rules |
| المادة 11 (Self-Merge) | المادة 7 (Authority) | — | — |
| المادة 12 (What NOT to Do) | المادة 5 (Anti-Patterns) | — | 10 rules |
| المادة 13 (Amendment) | المادة 11 | Article 7 | — |

---

_Last amended: 2026-07-29 21:45 UTC by Mavis Local, per Admin Team (سيتی + محمد) hand-off (first cycle kickoff)_
