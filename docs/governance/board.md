# 📊 Live Communication Board

> **Last updated:** 2026-07-27 21:39 UTC (Cycle 2 launch)
> **Updated by:** سيتي (Cloud)

## 🔄 Current Cycle: 2 / 20

| Field | Value |
|-------|-------|
| **Title** | 6.2 Tests Refactor + 3-Layer DB Setup |
| **Owner** | Mavis Local (Block A) + Anas (Block B) |
| **Status** | 🟡 ACTIVE — hand-off pushed, awaiting Mavis Local to start |
| **Started** | 2026-07-27 21:38 UTC |
| **Hand-off** | docs/governance/hand-offs/cycle-2.md (119 lines, 4.4 KB) |
| **Cron** | `monitor-cycle-2-pr-merge` (task_id 423253905956983, every 3 min) |
| **PR** | ⏳ Awaiting Mavis Local to open |

## 📋 Cycle 2 Tasks — Status

### Block A (Mavis Local) — 6.2 Tests Refactor
- [ ] T1: Search for tenant_id patterns across 31 C# test files
- [ ] T2: Rename tenant_id → company_id
- [ ] T3: Update test signatures
- [ ] T4: Update test fixtures (FakeDbConnectionFactory, etc.)
- [ ] T5: Update assertion expectations
- [ ] T6: Run dotnet test → all green
- [ ] T7: Update 10 Playwright e2e specs
- [ ] T8: Add 3 new test cases (HoldingBootstrap, UserCompany, CompanySwitcher)

### Block B (Anas + Mavis Local) — 3-Layer DB Setup
- [ ] T9 (Anas): Create Supabase STAGING project
- [ ] T10 (Anas): Add STAGING_* secrets to GitHub
- [ ] T11 (Anas): Add STAGING_DATABASE_URL to .NET backend
- [ ] T12 (Mavis Local): Create reset-staging-db.yml
- [ ] T13 (Mavis Local): Update e2e.yml to use STAGING_* secrets
- [ ] T14 (Mavis Local): Add e2e.yml auto-screenshot step

## 📈 Progress Tracking

- **Started:** 21:38 UTC
- **ETA:** 4-6 hours (with Block A + Block B parallelism)
- **Estimated completion:** 2026-07-28 01:38 - 03:38 UTC

## 🔄 Previous Cycle (1) — DONE ✅

- PR #153 merged at 18:44 UTC (squash, SHA 47458bd3)
- 20 files, +1494 lines, -8 lines
- 6/6 CI checks PASS

---

*Updated by سيتي at start of each cycle and on state changes.*
