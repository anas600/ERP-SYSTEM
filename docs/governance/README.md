# 🏛️ Governance Protocol — ERP-SYSTEM

> **Established:** 2026-07-27 (Cycle 0)  
> **Authority:** Anas (Owner)  
> **Coordinator:** Siti (Cloud CTO)  
> **Executor:** Mavis Local Team  
> **Total Cycles:** 20

## 📜 Mission

Coordinate development between cloud (Siti) and local (Mavis) agents through a structured 20-cycle protocol. The **Single Source of Truth (SoT)** is the GitHub repository. All decisions, plans, and hand-offs are documented in the repo.

## 🏗️ Structure

```
docs/governance/
├── README.md                    # This file
├── hand-off-template.md         # Template for cycle Hand-offs
├── board.md                     # Live communication board
├── cycle-log.md                 # History of all 20 cycles
├── summary.md                   # Quick progress snapshot
├── agents/
│   ├── siti.md                  # Siti (Coordinator) role
│   ├── mavis-local.md           # Mavis Local (Executor) role
│   ├── muhammad.md              # Muhammad (Strategic Advisor) role
│   └── dev.md                   # Dev (DevOps) role
└── internal/                    # 🔒 NOT shared (Siti private)
    ├── analysis/                # Per-cycle analysis
    └── improvements/            # Improvement log
```

## 🔄 The Cycle (One Iteration)

```
┌─────────────────────────────────────────────────────┐
│ Cycle N+1                                          │
├─────────────────────────────────────────────────────┤
│ 1. ANALYZE: Siti reviews previous cycle's hand-off  │
│ 2. PLAN: Decide next work scope (cycle scope)       │
│ 3. HAND-OFF: Write hand-off-v(N+1) to develop       │
│ 4. EXECUTE: Local Mavis reads from develop          │
│ 5. WORK: Local Mavis implements + pushes + PR       │
│ 6. MERGE: Siti merges PR to develop                 │
│ 7. INTERNAL: Siti + Muhammad + Dev analyze          │
│ 8. IMPROVE: Update hand-off template + protocol     │
└─────────────────────────────────────────────────────┘
```

## 📋 3-Layer Architecture (Governance)

| Layer | Owner | Branch | Purpose |
|-------|-------|--------|---------|
| **Dev** | Mavis Local | `feature/phase6-migrate-features` | Active development |
| **Staging** | Mavis Local | `develop` | Integration + verification |
| **Production** | (Deferred per DEC-068) | `main` | Local Docker ONLY |

## 🛡️ Boundaries

**Siti (Coordinator) can:**
- ✅ Push directly to `develop` (delegated by Anas)
- ✅ Create `governance/*` branches
- ✅ Merge PRs from local Mavis (after verification)
- ✅ Read/write in `docs/governance/`
- ❌ Cannot merge to `main` (production deferred)
- ❌ Cannot delete branches

**Mavis Local (Executor) can:**
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

This protocol runs for **20 cycles** to complete:
- **Cycle 1-3:** Phase 6 completion (6.4 Docs, 6.2 Tests, 6.5 CI)
- **Cycle 4-5:** Production prep (Local Docker end-to-end)
- **Cycle 6-10:** Phase 7 (new features)
- **Cycle 11-20:** Long-term improvements (monitoring, scaling, etc.)

> **Current cycle: 1/20** (Documentation Sprint 6.4)

---

*Last updated: 2026-07-27 17:50 UTC — Cycle 0 complete, Cycle 1 starting*
