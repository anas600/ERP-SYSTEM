# Sprint 18 — Retrospective

**Date:** 2026-08-01
**Sprint goal:** Per Anas 2026-08-01 08:11 UTC — apply Muhammad's governance audit. Remove old docs that break the Two-Mode Workflow, restore the `ACTIVE` status of the constitution, and consolidate workflow documentation. **No code changes** — pure governance cleanup.

**Result:** ✅ DONE locally on `feature/sprint-18-governance-cleanup` (off `ad91825`). Mode 2 push planned per the new Two-Mode Workflow.

---

## What worked

### 1. "First task, then continue" pattern
Anas gave a clear directive: do task 0 (document Muhammad's analysis) first, then continue with the 11 tasks. This gave a clear starting point and a way to reference the analysis throughout the sprint. The `docs/notes/muhammad-sprint-18-analysis.md` file is now Anas's persistent reference for the strategy.

### 2. Mechanical rg checks (Task 6)
The 5 rg checks (`tenant_id`, `WORKFLOW.md`, `mavis-coordination`, `Cloud Coordinator`, `PAUSED`) confirmed the cleanup was complete. All stale references were in expected places (CHANGELOG.md historical entries, negation contexts, or my new analysis file). Zero false positives in active docs.

### 3. mavis-trash for safe deletion
Used `mavis-trash` instead of `Remove-Item` for WORKFLOW.md and the mavis-coordination directory. The PowerShell safety guard blocks `Remove-Item` for sensitive paths, but `mavis-trash` is the right tool (recoverable, audit trail). No accidental loss.

### 4. The "no-op" is a valid outcome
Task 5 (Update docs/personas/) was a no-op because the directory doesn't exist on develop. The pre-flight check (Sprint 14 lesson: verify before acting) saved time. Documented the no-op in the retro for future reference.

---

## What was hard

### 1. CONSTITUTION.md header rewrite
The original header was a "PAUSED" notice with a "2-day pause" narrative. I had to:
- Remove the entire PAUSED blockquote
- Replace with an ACTIVE status + Two-Mode Workflow reference
- Add the new "active governance" model section
- Cross-reference Article 10 (the actual canonical governance doc)

The risk: removing the PAUSED block but leaving stale references. I verified with `rg "PAUSED"` after the edit (only historical contexts remained).

### 2. AGENTS.md "active governance" section
The old section referenced `WORKFLOW.md` (now deleted) and `state.json` (directory now deleted). I had to:
- Replace the entire "📜 ACTIVE GOVERNANCE" blockquote
- Add a new section that points to the canonical sources (CONSTITUTION.md, AGENTS.md for Two-Mode Workflow, branch architecture from Sprint 17)
- Keep the reference to `.mavis/AGENTS.md` (worker instructions — still active)

### 3. AGENTS.md "Last updated" header needs careful wording
The new "Last updated" line documents the changes made in Sprint 18. The line is:
```
2026-08-01 (Sprint 18: governance cleanup — removed WORKFLOW.md + state.json references, restored ACTIVE constitution, codified Two-Mode Workflow per Sprint 17)
```
This contains the words "WORKFLOW.md" and "state.json" but they're historical (describing what was removed), not active references. The rg check for "WORKFLOW.md" still found this line, but it's the correct historical mention.

---

## Numbers

| Metric | Value |
|--------|-------|
| Files changed | 4 (CONSTITUTION.md, AGENTS.md, CHANGELOG.md, docs/notes/muhammad-sprint-18-analysis.md) |
| Files deleted | 6+ (WORKFLOW.md + entire .github/workflows/mavis-coordination/ directory) |
| Lines added | ~280 (CHANGELOG + analysis + retro) |
| Lines removed | ~50 (PAUSED header in CONSTITUTION, AGENTS section) |
| rg checks | 5 (all passed) |
| No-op tasks | 1 (docs/personas/ doesn't exist) |
| Total sprints on develop | 5 (Sprint 14 + 15 + 16 + 17 + 18) |

---

## Architecture after Sprint 18

```
[Active governance — single source of truth]
CONSTITUTION.md (✅ ACTIVE)
    │
    ├─ Article 1-9: Project identity, roles, architecture, branches, workflow
    ├─ Article 10: Two-Mode Workflow (Mode 1: Development, Mode 2: Release)
    ├─ Article 11-15: Test strategy, communication, etc. (updated for Sprint 17+18)
    └─ Last amended: 2026-08-01 (Sprint 18)

AGENTS.md (developer guide)
    │
    ├─ Child DOX Index (per directory)
    ├─ DOX framework + read-before-edit
    ├─ Work Guidance (Two-Mode Workflow, Commands, Sprint Model)
    └─ References CONSTITUTION.md for governance details

[Retired/Obsolete]
✗ WORKFLOW.md (deleted)
✗ .github/workflows/mavis-coordination/ (deleted)
✗ docs/personas/siti.md (was never on develop)
✗ docs/personas/dev.md (was never on develop)
```

---

## Carry-over actions for Sprint 19+

| Priority | Action |
|----------|--------|
| P1 | Testcontainers in CI → smoke test runs on every PR (not just after merge) |
| P1 | Update smoke test to wait for "bootstrap admin exists" before login check |
| P2 | Wire watcher into Local Team's pre-push hook |
| P2 | AGENTS.md: clarify "Mode 1 = single worktree, Jimis commit to same branch" (already documented in Sprint 17 but could be more explicit) |
| P3 | Self-cleanup cron: prune mvp-docker images older than N days |

---

## Lessons (compounded from Sprint 14+15+16+17+18)

### L1: Governance-only sprints are valuable
Sprint 18 changed 0 lines of code, 0 lines of business logic, 0 schema. **But the system is now cleaner.** The 6 stale references to obsolete patterns (WORKFLOW.md, state.json, Cloud Coordinator) are gone. The CONSTITUTION.md says ACTIVE, not PAUSED. Future contributors won't be confused.

### L2: Pre-flight checks save time
Task 5 (Update personas) was identified as a no-op before doing any work. The pre-flight rg check took 5 seconds and saved 30 minutes of "update files that don't exist" work.

### L3: rg checks are the right tool for "is it really clean?"
A 5-line bash command (rg "WORKFLOW.md" / rg "mavis-coordination" / etc.) is faster and more reliable than manual reading. The checks are deterministic — no human error.

### L4: mavis-trash is the right tool for sensitive deletes
When the safety guard blocks `Remove-Item`, the right answer is `mavis-trash` (recoverable, audit trail). Sprint 12 retro documented this pattern; Sprint 18 confirms it works at the directory level.

### L5: "First task, then continue" is a good sprint pattern
Anas's "task 0 first, then 11 more" is a clear pattern for sequential work:
- Task 0 is small and verifiable (write a file)
- Then 11 tasks can be done in order
- The cron (set up later) handles Mode 2
- The user can interrupt at any task if needed

### L6: Delete obsolete infrastructure, don't archive it
The instinct might be to archive `.github/workflows/mavis-coordination/` as a historical record. **But for a 2-day pause that ended 6 days ago, the archive is noise.** The cron pattern in Sprint 15+16+17 is the active model. Archiving the old one would just create a "what's this?" moment for future contributors. **Delete > Archive for short-lived patterns.**

---

_Last updated: 2026-08-01 by Mavis (Sprint 18 mode). DOX applied._
