# 📊 Live Communication Board

> **Last updated:** 2026-07-27 22:40 UTC (DEC-070 issued)
> **Updated by:** سيتي (Cloud)

## 🔄 Current Cycle: 2 / 20

| Field | Value |
|-------|-------|
| **Title** | 6.2 Tests Refactor ONLY (per DEC-070) |
| **Owner** | Mavis Local (Tech Lead, full admin) |
| **Status** | 🟡 ACTIVE — Hand-off v2 pushed, awaiting Mavis Local start |
| **Started** | 2026-07-27 21:38 UTC (v1), 22:35 UTC (v2 per DEC-070) |
| **Hand-off** | docs/governance/hand-offs/cycle-2.md (v2, 154 lines) |
| **Cron** | `monitor-cycle-2-pr-merge` (task_id 423253905956983, every 3 min) |
| **PR** | ⏳ Awaiting Mavis Local to open |
| **DEC** | DEC-070 (Local Team Empowerment) |

## 📋 Cycle 2 Tasks (v2 - per DEC-070)

### Block A (Mavis Local, FULL POWER) — 6.2 Tests Refactor

- [ ] T1: Search for tenant_id patterns across 31 C# test files
- [ ] T2: Rename tenant_id → company_id
- [ ] T3: Update test signatures
- [ ] T4: Update test fixtures (FakeDbConnectionFactory, etc.)
- [ ] T5: Update assertion expectations
- [ ] T6: Run dotnet test → all green
- [ ] T7: Update 10 Playwright e2e specs (OPTIONAL, not CI gated)
- [ ] T8: Add 3 new test cases (HoldingBootstrap, UserCompany, CompanySwitcher)

### 🟡 Block B (3-Layer DB) — REMOVED per DEC-070

> ❌ NO staging, NO production, NO STAGING_* secrets
> Until Anas gives explicit approval

## 🆕 DEC-070 — Effective NOW

| Setting | Value | Note |
|---------|-------|------|
| **Mavis Local authority** | Tech Lead (admin) | Full power on develop |
| **Playwright E2E** | Optional | Not required for merge |
| **Force-pushes** | ✅ Allowed | Use --force-with-lease |
| **Self-merge** | ✅ Allowed | Admin bypass |
| **Jimis leadership** | Mavis Local leads | Jimi تنفيذي + Jimi تحليلي |
| **Mavis (Cloud) role** | Architectural Guardian | Strategy, governance, DECs |
| **Staging layer** | 🟡 Frozen | Until Anas approves |
| **Production layer** | 🟡 Frozen | Until Anas approves |

## 📈 Progress Tracking

- **Started:** 21:38 UTC (v1), 22:35 UTC (v2)
- **ETA:** 2-3 hours (just Block A now)
- **Estimated completion:** 2026-07-28 00:30 - 01:30 UTC

## 🔄 Previous Cycles

### Cycle 1: 6.4 Documentation Sprint — DONE ✅
- PR #153 merged at 18:44 UTC (squash, SHA 47458bd3)
- 20 files, +1494 lines, -8 lines
- 6/6 CI checks PASS

---

*Updated by سيتي on each state change.*
