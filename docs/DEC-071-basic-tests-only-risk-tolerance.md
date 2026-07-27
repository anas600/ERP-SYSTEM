# DEC-071: Basic Tests Only + Risk Tolerance on Develop

> **Status:** ✅ ACTIVE (per Anas's directive)  
> **Date:** 2026-07-27 23:17 UTC (Europe/Berlin)  
> **Authority:** Anas (Project Owner) — Administrative Decision  
> **Supersedes:** Partial override of cycle-2 hand-off v2 (Block A scope)

---

## 📋 Summary

Anas has issued a direct administrative decision to scope down Cycle 2 testing:

1. **Don't worry about breaking develop** — The real error is in HF production, not develop
2. **Run only basic tests** — Tests that match the new Phase 6 product
3. **Continue cycle after** — Don't stop, keep momentum

---

## 🎯 Decision Details

### Decision 1: Risk Tolerance on Develop Branch

**Rationale:** The production issue is in HF Space environment, NOT in the develop branch code. Breaking develop is acceptable; broken production is the actual problem.

**Scope:**
- ✅ Mavis Local can take aggressive changes
- ✅ If develop breaks, it's acceptable
- ❌ DO NOT touch HF Space production
- ❌ DO NOT touch Supabase prod
- ❌ DO NOT touch main branch

**Real priority:** Fix HF production, not protect develop.

### Decision 2: Basic Tests Only (Phase 6 Specific)

**Rationale:** The "new product" is Phase 6 (Multi-Company Refactor). The basic tests are the ones that verify Phase 6 works.

**Scope:**
- ✅ Test only the 3 new Phase 6 test cases:
  1. `HoldingBootstrap_Seeds_DefaultHolding_And_CoA` (integration)
  2. `UserCompany_Limits_Access_To_Assigned_Companies` (unit)
  3. `CompanySwitcher_Switches_Active_Company_In_Context` (unit)
- ❌ SKIP refactor of all 31 C# test files
- ❌ SKIP 10 Playwright e2e specs
- ❌ SKIP exhaustive testing

**Verification:** Just dotnet test the 3 new test cases, ensure they pass.

### Decision 3: Continue Cycle After Tests

**Rationale:** Don't pause. After basic tests pass, continue with the cycle work.

**Scope:**
- ✅ After 3 tests pass → move to next phase of cycle
- ✅ Don't wait for "perfect" test coverage
- ✅ Ship and iterate

---

## 🔄 Updates to Cycle 2 Hand-off

The cycle-2 hand-off needs to be UPDATED to v3:

| v1 (original) | v2 (DEC-070) | **v3 (DEC-071, current)** |
|---------------|--------------|---------------------------|
| T1-T6: Refactor 31 files | T1-T6: Refactor 31 files | ❌ **REMOVED** |
| T7: Update 10 Playwright | T7: Optional | ❌ **REMOVED** |
| T8: Add 3 new tests | T8: Add 3 new tests | ✅ **KEEP (this IS the work)** |

**v3 scope = ONLY T8 (3 new test cases).** That's it.

---

## ⚖️ Constitution Compliance

- ✅ **Article 3** (company_id): 3 new tests verify this
- ✅ **Article 4** (Branch discipline): --force-with-lease still required
- ✅ **Article 7** (NO SECRETS): No new secrets introduced
- ❌ Articles 5, 6 partially relaxed (basic tests only)

---

## 📅 Effective Period

**Start:** 2026-07-27 23:17 UTC (immediately)
**End:** Until cycle 2 closes OR Anas issues another DEC

---

**Signed:** Anas (via Telegram)  
**Witnessed by:** محمد (Strategic Advisor, session 406067545768199)  
**Documented by:** سيتي (Cloud Coordinator, session 406067545768199)
