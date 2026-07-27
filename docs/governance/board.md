# 📊 Live Communication Board

> **Last updated:** 2026-07-27 23:20 UTC (DEC-071 issued)
> **Updated by:** سيتي (Cloud)

## 🔄 Current Cycle: 2 / 20

| Field | Value |
|-------|-------|
| **Title** | Basic Tests Only (per DEC-071) |
| **Owner** | Mavis Local (Tech Lead) |
| **Status** | 🟡 ACTIVE — Simplified scope, awaiting Mavis Local start |
| **Hand-off** | docs/governance/hand-offs/cycle-2.md (v3, 128 lines) |
| **Cron** | `monitor-cycle-2-pr-merge` (active) |
| **DECs** | DEC-070 (admin) + DEC-071 (basic tests) |

## 📋 Cycle 2 v3 — SCOPE (Simplified per DEC-071)

### ✅ KEEP (only this)

- [ ] T8a: `HoldingBootstrap_Seeds_DefaultHolding_And_CoA` (integration, ~1h)
- [ ] T8b: `UserCompany_Limits_Access_To_Assigned_Companies` (unit, ~1h)
- [ ] T8c: `CompanySwitcher_Switches_Active_Company_In_Context` (unit, ~1h)

**Total: 3 test cases, 2-3 hours**

### ❌ REMOVED (per DEC-071)

- ❌ Refactor 31 C# test files (T1-T6)
- ❌ Update 10 Playwright e2e specs (T7)
- ❌ Exhaustive test coverage
- ❌ 3-Layer DB setup (T9-T14, per DEC-070)

## 🆕 DEC-071 — Effective NOW

| Setting | Value | Note |
|---------|-------|------|
| **Scope** | 3 new Phase 6 tests | ONLY these |
| **Risk tolerance** | ✅ High on develop | OK to break |
| **Don't touch** | HF Space, Supabase prod, main | Real concerns |
| **Continue cycle** | ✅ After tests pass | Don't pause |

## 🆕 DEC-070 — Active

| Setting | Value | Note |
|---------|-------|------|
| **Mavis Local authority** | Tech Lead (admin) | Full power on develop |
| **Playwright E2E** | Optional | Not required for merge |
| **Force-pushes** | ✅ Allowed | Use --force-with-lease |
| **Self-merge** | ✅ Allowed | Admin bypass |
| **Jimis** | Led by Mavis Local | تنفيذي + تحليلي |
| **Mavis (Cloud)** | Architectural Guardian | Strategy, governance, DECs |
| **Staging/Production** | 🟡 Frozen | Until Anas approves |

## 📈 Progress Tracking

- **Started:** 21:38 UTC (v1), 22:35 UTC (v2), 23:17 UTC (v3)
- **ETA:** 2-3 hours (just 3 tests)
- **Estimated completion:** 2026-07-28 01:17 - 02:17 UTC

## 🔄 Previous Cycles

### Cycle 1: 6.4 Documentation Sprint — DONE ✅
- PR #153 merged at 18:44 UTC (squash, SHA 47458bd3)
- 20 files, +1494 lines, -8 lines

---

*Updated by سيتي on each state change.*
