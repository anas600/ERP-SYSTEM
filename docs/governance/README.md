# 🏛️ Governance Protocol — ERP-SYSTEM

> **Established:** 2026-07-27 (Cycle 0)  
> **Authority:** Anas (Owner)  
> **Cycles:** 20 (Cycle 0 = setup, Cycles 1-20 = work)

## 🆔 Session Identification (CRITICAL — read first!)

There are **TWO Mavis agents** in this protocol. They are NOT the same.

| Identity | Siti (Cloud Coordinator) | Mavis Local (Windows Executor) |
|----------|--------------------------|--------------------------------|
| **Session ID** | `406067545768199` | `<varies per session — see response header>` |
| **Platform** | Cloud Sandbox (Web) | Windows (Anas's machine) |
| **Workspace** | `/workspace/.mavis/` | `C:\Users\Anas\.minimax-agent\projects\` |
| **Role** | Coordinator (CTO) | Executor (Team Lead) |
| **Authority** | develop branch (full push) | feature branches (PR to develop) |
| **Direction** | Anas → Siti → Hand-off | Hand-off → Mavis Local → Work → PR |

### ⚠️ Identity Confusion Prevention

**Problem:** Both agents are Mavis. If files don't distinguish, they may mis-identify themselves.

**Solution:** Every file in this governance includes explicit session ID + platform + workspace.

**Rule for Mavis Local:**
- Before executing ANY hand-off, **verify the From Session ID is `406067545768199`**
- If not, **stop and confirm with Anas**

**Rule for Siti:**
- Before merging ANY PR, **verify the PR author's branch + recent commits**
- Cross-check session ID via the response file

---

## 📜 Mission

Coordinate development between cloud (Siti) and local (Mavis Local) agents through a structured 20-cycle protocol. The **Single Source of Truth (SoT)** is the GitHub repository. All decisions, plans, and hand-offs are documented in the repo.

## 🏗️ Structure

```
docs/governance/
├── README.md                    # This file
├── hand-off-template.md         # Template for cycle Hand-offs (v1.1)
├── board.md                     # Live communication board
├── cycle-log.md                 # History of all 20 cycles
├── summary.md                   # Quick progress summary
├── agents/
│   ├── siti.md                  # Siti (Cloud Coordinator) — session 406067545768199
│   ├── mavis-local.md           # Mavis Local (Windows Executor) — your session ID
│   ├── muhammad.md              # Muhammad (Strategic Advisor) — internal mode
│   └── dev.md                   # Dev (DevOps) — internal mode
└── hand-offs/
    └── cycle-1.md               # 6.4 Documentation Sprint
```

## 🔄 The Cycle (One Iteration)

```
┌─────────────────────────────────────────────────────┐
│ Cycle N+1                                          │
├─────────────────────────────────────────────────────┤
│ 1. ANALYZE: Siti reviews previous cycle's hand-off │
│ 2. PLAN: Decide next work scope (cycle scope)      │
│ 3. HAND-OFF: Write hand-off-v(N+1) to develop       │
│ 4. EXECUTE: Mavis Local reads from develop         │
│ 5. WORK: Mavis Local implements + pushes + PR      │
│ 6. MERGE: Siti merges PR to develop                │
│ 7. INTERNAL: Siti + Muhammad + Dev analyze         │
│ 8. IMPROVE: Update hand-off template + protocol    │
└─────────────────────────────────────────────────────┘
```

## 📋 3-Layer Architecture (Governance)

| Layer | Owner | Branch | Purpose |
|-------|-------|--------|---------|
| **Dev** | Mavis Local | `feature/phase6-migrate-features` | Active development |
| **Staging** | Mavis Local → Siti | `develop` | Integration + verification |
| **Production** | (Deferred per DEC-068) | `main` | Local Docker ONLY |

## 🛡️ Boundaries

**Siti (Cloud Coordinator) can:**
- ✅ Push directly to `develop` (delegated by Anas)
- ✅ Create `governance/*` branches
- ✅ Merge PRs from Mavis Local (after verification)
- ✅ Read/write in `docs/governance/`
- ❌ Cannot merge to `main` (production deferred)
- ❌ Cannot delete branches

**Mavis Local (Windows Executor) can:**
- ✅ Work on their own branch (`feature/*`)
- ✅ Push + Open PR to `develop`
- ✅ Read from `docs/governance/`
- ❌ Cannot push to `develop` directly (must use PR)
- ❌ Cannot push to `main`

> **Note:** Per **DEC-070** (2026-07-27 22:33 UTC), Mavis Local has been
> promoted to **Tech Lead with full admin authority on `develop`**.
> Specifically: self-merge with `--admin` flag, `--force-with-lease`, skip
> Playwright E2E, lead Jimis (jimi تنفيذي + jimi تحليلي). The block-list
> (no staging/prod, no main) remains.

## 📊 Communication (Asynchronous)

- **Primary channel:** Hand-off files in `docs/governance/hand-offs/cycle-N.md`
- **Live board:** `docs/governance/board.md` (updated by both agents)
- **Summary:** `docs/governance/summary.md` (updated by Siti at end of each cycle)
- **Lessons learned:** `docs/governance/lessons-learned.md` (Mavis Local → Siti knowledge transfer, cycle 4)

## ⚠️ Documented Failure Modes (Cycle 4)

The following failure modes have been observed in cycles 0-3. Documented so
future cycles can recognize + avoid them. See [`lessons-learned.md`](./lessons-learned.md)
for full details.

### Network / Cloud Outage

- **Symptom:** No commits, no hand-off responses, no board updates from
  the analytical team (Siti) for >N hours.
- **Detection (current, cycle 1-3):** Human (Anas) happens to notice the
  team screen is offline. Slow, error-prone.
- **Detection (proposed, cycle 4+):** Smart cron with token-free health-ping.
  See [`docs/governance/hand-offs/presence-protocol.md`](./hand-offs/presence-protocol.md)
  for the proposed mechanism (DEC-072).
- **Workaround (current):** Continue Tier 1 work locally; document infra
  failures in hand-off; defer cloud-dependent work to dedicated sessions.
- **Hard limit:** No direct agent-to-agent messaging. All sync via docs + git.

### Hand-off Inaccuracy

- **Symptom:** Task scope in hand-off doesn't match actual state (e.g.,
  "add 2 new test cases" but they already exist; "fix workflow" but
  workflow doesn't apply to the branch).
- **Detection:** Mavis Local's T1 inventory always catches this before work
  begins.
- **Workaround:** Treat hand-off tasks as hypotheses, not commands. Verify
  against `git log origin/develop` first. Document the inventory result in
  the cycle response.

### Batch Tool Silent Failure

- **Symptom:** A script prints "✓" but the actual operation failed; data loss.
- **Detection:** `git status --short` after batch ops + `Select-String`
  verification of expected markers.
- **Workaround:** Never use PowerShell `-ireplace` for multi-line structural
  edits. Use the `edit` tool (Mavis's built-in) one file at a time.

## 📡 Cron Pattern (per cycle work)

Mavis Local uses `mavis cron self` for async monitoring. Convention:

- **Naming:** `check-pr-<N>-ci` for CI watching; `monitor-<X>-<Y>` for general.
- **Self-deleting on success:** Every cron deletes itself when its goal is
  achieved (merge complete, file detected, etc.). Never leave crons running
  indefinitely.
- **When to delete (not throttle):** When user says "wait for X" (X is a long
  wait, not CI). Throttling = still pinging, just less often. Deletion = the
  user explicitly said stop.
- **Tick content (gate discipline):** Wrap status in `<mavis-progress>...</mavis-progress>`
  on skip ticks. Full explanation only on state changes or human-action-needed.

## 🔁 Total Cycles: 20

| # | Title | Status |
|---|-------|--------|
| 0 | Protocol Setup + PR #152 merge | ✅ Done |
| 1 | 6.4 Documentation Sprint | ✅ Done (PR #153 merged) |
| 2 | 6.2 Tests Refactor | ✅ Done (PR #154 merged) |
| 3 | 6.5 CI/Hardening | ✅ Done (PR #155 merged) |
| 4 | Governance Improvement (Lessons Learned) | 🟡 Active |
| 5 | Production Prep (Local Docker) | ⏳ Planned |
| 6-10 | Phase 7 Implementation | ⏳ Backlog |
| 11-15 | Performance + Scaling | ⏳ Backlog |
| 16-20 | Monitoring + Polish | ⏳ Backlog |

---

*Last updated: 2026-07-28 00:25 UTC — Cycle 4 governance sprint (lessons learned, README failure modes, cycle log update).*
