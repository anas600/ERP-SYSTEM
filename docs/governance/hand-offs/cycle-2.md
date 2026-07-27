# 📦 Hand-Off v3 — Cycle 2: BASIC TESTS ONLY (per DEC-071)

> **From:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
> **To:** Mavis Local (Tech Lead) — your session, Windows  
> **Cycle:** 2 / 20 — **ACTIVE ✅ (v3 simplified per DEC-071)**  
> **Authority:** DEC-070 (admin) + DEC-071 (basic tests only)

---

## 🆕 What's New in v3 (per DEC-071)

**Major simplification:**

1. ❌ **REMOVED**: Refactor 31 C# test files (T1-T6)
2. ❌ **REMOVED**: Update 10 Playwright e2e specs (T7)
3. ❌ **REMOVED**: Exhaustive testing
4. ✅ **KEEP**: Add 3 new Phase 6 test cases (T8)
5. ✅ **Risk tolerance on develop**: Breaking develop is OK (HF production is the real concern)

**Why simpler:**
- The "new product" = Phase 6 (Multi-Company)
- The "basic tests" = tests that verify Phase 6 works
- The 3 new tests ARE the basic tests
- Don't waste time on exhaustive coverage now

---

## 🎯 Cycle 2 v3 Scope: ONLY 3 NEW TEST CASES

### The Work (T8 only)

Add 3 new test cases for Phase 6:

1. **`HoldingBootstrap_Seeds_DefaultHolding_And_CoA`**
   - Type: Integration test
   - Verifies: When app starts, a default Holding is created + CoA (Chart of Accounts) is seeded
   - File: `src/backend/Tests/ERPSystem.Tests/Companies/HoldingBootstrapTests.cs` (new)
   - Estimated: 1 hour

2. **`UserCompany_Limits_Access_To_Assigned_Companies`**
   - Type: Unit test
   - Verifies: User can only access companies they're assigned to (via user_companies table)
   - File: `src/backend/Tests/ERPSystem.Tests/Auth/UserCompanyAccessTests.cs` (new)
   - Estimated: 1 hour

3. **`CompanySwitcher_Switches_Active_Company_In_Context`**
   - Type: Unit test
   - Verifies: When user switches active company, CompanyContext updates
   - File: `src/backend/Tests/ERPSystem.Tests/Auth/CompanySwitcherTests.cs` (new)
   - Estimated: 1 hour

**Total estimated time:** 2-3 hours

---

## 🛡️ Permissions (from DEC-070)

You have full Tech Lead authority:
- ✅ Self-merge (admin bypass)
- ✅ `--force-with-lease`
- ✅ Skip Playwright (not required)
- ✅ Lead Jimis (تنفيذي + تحليلي)

**Plus new freedom (from DEC-071):**
- ✅ Risk tolerance on develop
- ✅ Don't worry about breaking develop
- ❌ DO NOT touch HF Space production
- ❌ DO NOT touch Supabase prod
- ❌ DO NOT touch main branch

---

## 🔧 Verification (minimal)

```bash
# 1. Build (REQUIRED)
npx tsc --noEmit
dotnet build Host/ERP-SYSTEM.csproj

# 2. The 3 new tests (REQUIRED)
dotnet test --filter "HoldingBootstrap_Seeds_DefaultHolding_And_CoA"
dotnet test --filter "UserCompany_Limits_Access_To_Assigned_Companies"
dotnet test --filter "CompanySwitcher_Switches_Active_Company_In_Context"

# 3. Quick smoke (optional)
dotnet test --filter "FullyQualifiedName~Auth"
```

**That's it.** No need to refactor 31 files. No Playwright. Just verify the 3 new tests work.

---

## 🚨 Risk Notes

- **R1**: If a test fails, fix it (don't disable, don't skip)
- **R2**: If develop breaks, that's OK — fix and move on
- **R3**: If you need to delete old tests, ask me first (audit trail)
- **R4**: Don't touch HF Space — that's the real production concern

---

## 📡 Async Protocol (Reminder)

- Cron `monitor-cycle-2-pr-merge` is ACTIVE (every 3 min)
- Silent on no-change
- Notify on state-change
- Self-delete on merge
- I'll merge your PR when CI green + you say "ready"

---

## 🚀 When Ready to Start

1. Read this hand-off (you just did)
2. Optionally create a feature branch: `git checkout -b feature/cycle-2-basic-tests`
3. Add the 3 new test files
4. Run `dotnet test --filter "..."` for each
5. Open PR to develop
6. Say "ready for merge"
7. I (سيتي) merge

**You have full authority. Go. 2-3 hours max. 🎯**

---

**Signed:** سيتي (Cloud Coordinator)  
**Authority:** DEC-070 + DEC-071  
**Date:** 2026-07-27 23:18 UTC
