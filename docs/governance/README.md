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

## 📊 Communication (Asynchronous)

- **Primary channel:** Hand-off files in `docs/governance/hand-offs/cycle-N.md`
- **Live board:** `docs/governance/board.md` (updated by both agents)
- **Summary:** `docs/governance/summary.md` (updated by Siti at end of each cycle)

## 🔁 Total Cycles: 20

| # | Title | Status |
|---|-------|--------|
| 0 | Protocol Setup + PR #152 merge | ✅ Done |
| 1 | 6.4 Documentation Sprint | 🟡 Active (waiting on Mavis Local) |
| 2 | 6.2 Tests Refactor | ⏳ Planned |
| 3 | 6.5 CI/Hardening | ⏳ Planned |
| 4 | Production Prep (Local Docker) | ⏳ Planned |
| 5 | Phase 7 Planning | ⏳ Planned |
| 6-10 | Phase 7 Implementation | ⏳ Backlog |
| 11-15 | Performance + Scaling | ⏳ Backlog |
| 16-20 | Monitoring + Polish | ⏳ Backlog |

---

*Last updated: 2026-07-27 18:42 UTC — Session ID clarification per Anas's feedback*
