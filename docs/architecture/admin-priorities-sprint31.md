# Mode 1 Admin Priorities — Sprint 31 (2026-08-04)

> **Author:** Muhammad (Mavis mode), Architect/Strategic Advisor
> **Audience:** Admin Team (Mavis Local) + Anas
> **Mode:** Mode 1 (host local dev, no push, no PR)
> **Goal:** Help Anas decide which Sprint 31 work delivers the most user value

---

## Priority Matrix (Muhammad's recommendation)

| # | Priority | Item | Why | Estimated | Sprint 31? |
|---|---|---|---|---|---|
| 1 | **P0** | Fix Projects module (4 missing tables) | 2 endpoints 500 — users see broken UI | 1-2 hours | ✅ YES |
| 2 | **P0** | Add `ProjectService` tests (10+ cases) | Audit per L21 (test refactor pass) | 2-3 hours | ✅ YES |
| 3 | **P0** | Posting Rules benchmark vs engine comparison | Discover posting bugs (L39) | 4-6 hours | ✅ YES |
| 4 | **P1** | Manual JEs (depreciation + accruals + year-end) | Core accounting workflow | 6-8 hours | 🔶 Maybe |
| 5 | **P1** | `DepartmentResponse.managerName` enrichment | UI shows "no name" today | 30 min | ✅ YES |
| 6 | **P1** | `customerStatement` + `vendorStatement` GET endpoints | Users asked for this | 4-6 hours | ✅ YES |
| 7 | **P1** | 5th default rule "Sale with VAT 5%" (inactive) | For demo + future VAT adoption | 1 hour | ✅ YES |
| 8 | **P1** | Audit 4 still-pending Article 3 modules | Continue the audit pattern (L25) | 4-6 hours | ✅ YES |
| 9 | **P2** | Trial Balance validation UI ("Balanced / Unbalanced") | Admin needs confidence | 2-3 hours | 🔶 Maybe |
| 10 | **P2** | Year-scenario Phase 2 (payroll + stock + project costs) | More data to test | 8-10 hours | ❌ Sprint 32+ |
| 11 | **P2** | Pre-push script: scan for `?` in user-visible columns | Would have caught Sprint 25/26 bugs | 1-2 hours | 🔶 Maybe |
| 12 | **P2** | Build-time test that enforces DEC-085 | Prevents Article 3 violations | 2-3 hours | 🔶 Maybe |
| 13 | **P2** | Add Playwright e2e tests for top 5 user flows | Per Anas's recurring request | 6-8 hours | ❌ Sprint 32+ |
| 14 | **P3** | Multi-currency support (currently LYD-only) | Libya-only, future-proof | 8-12 hours | ❌ Sprint 33+ |
| 15 | **P3** | Audit trail for posting rule changes | Compliance | 4-6 hours | ❌ Sprint 33+ |

---

## Sprint 31 PROPOSED Scope (1.5-2h per item, total ~6-8h)

### ✅ MUST DO (P0)
1. **Fix Projects module tables** — Add 4 `data-types/*.json` files (resources, tasks, project_assignments, project_budgets) + add DataType registrars → 2 endpoints stop 500ing
2. **Add `ProjectService` tests** — L21 refactor pass (since we just changed constructors). 10+ cases covering: Create, GetById, List, Update, Delete, Task assignment, Resource assignment
3. **Posting Rules benchmark vs engine** — Run the PostingRulesService on the 12 year invoices + 12 year bills + 24 receipts + 24 payments. Compare engine JEs vs benchmark JEs. Any discrepancy = bug to fix.

### ✅ SHOULD DO (P1, ~2h)
4. **`DepartmentResponse.managerName` + `managerCode`** — Same pattern as DEC-104 (L40). 30 min.
5. **5th default rule "Sale with VAT 5%" (inactive)** — Add to posting_rules seeder. 1 hour. For demo + future VAT adoption.
6. **Audit `Payments` module** (one of the 5 pending) — L25 carry-over. 2-3 hours. Apply DEC-085 checklist.

### 🔶 OPTIONAL (P1, only if time)
7. **`customerStatement` + `vendorStatement` GET endpoints** — Add 2 new endpoints + tests. 4-6 hours. Users asked for this.

### ❌ DEFER to Sprint 32+
- Year-scenario Phase 2 (8-10h)
- Playwright e2e tests (6-8h)
- Manual JEs (6-8h)
- Multi-currency (8-12h)

---

## Sprint 31 EXECUTION PLAN (Muhammad's recommended split)

### Phase 1: Auto-fix (Muhammad, ~30 min)
- **3.1**: Add 4 `data-types/*.json` files (resources, tasks, project_assignments, project_budgets)
- **3.2**: Add `managerName` + `managerCode` to `DepartmentResponse` + `DepartmentService.BuildEmployeeMapAsync`
- **3.3**: Add 5th rule "Sale with VAT 5%" to posting_rules seeder (DEC-088-style, inactive by default)
- **3.4**: Test all 3 previously-broken endpoints → confirm 200
- **3.5**: Add 10 `ProjectService` tests (L21)
- **3.6**: Run audit on `Payments` module (DEC-085 checklist)

### Phase 2: Verify (Anas, ~1h)
- **3.7**: Test the system via Chrome at http://localhost:3000
  - Login as admin
  - Try Projects page (should now work)
  - Try HR > Departments (should now show manager name)
  - Verify posting rules page (should show 5 rules now)
- **3.8**: Report any issues found → add to Sprint 32 backlog

### Phase 3: Commit (Admin, ~10 min)
- **3.9**: `git add -A` + `git commit -F <msg>` with DEC-107..111
- **3.10**: Update CHANGELOG + AGENTS.md + sprint-31-retro.md
- **3.11**: Send Telegram confirmation

### Phase 4: Benchmark vs Engine (Optional, ~4-6h)
- **3.12**: Implement a comparison test: run PostingRulesService on the 12 year invoices + 12 year bills + 24 receipts + 24 payments. Capture engine JEs.
- **3.13**: Compare engine JEs to benchmark JEs. Any discrepancy = bug.
- **3.14**: Fix bugs found (if any).
- **3.15**: Add the comparison test to the test suite so it runs in CI.

---

## Anass's Role (Per His Request)

> "المهمه الاخير في كل الاسبريت تقوم بالتخطيط له وينفده الادمن وتتحقق انت"

Per Anas's directive:
1. **Muhammad plans the sprint** (this document)
2. **Admin implements** (Mavis Local spawns Jimis for parallel work)
3. **Muhammad verifies** (T6: build + test + typecheck)

If the user wants Muhammad to also IMPLEMENT, then:
- Muhammad (Mavis mode) does the architectural work (DECs, audits, code review)
- Admin (Mavis Local) does the routine work (build, test, merge)

---

## Browser/MCP Tool — Clarification Needed

Anas asked Muhammad to use a "Blueprint MCP" / "Playwright MCP" tool to enter Chrome and test the system himself. **I do NOT have a browser automation tool in my current toolset.** My available tools are:
- `web_fetch` (HTTP only, no JS)
- `web_search`
- `mavis` (CLI management)
- No browser, no Playwright, no MCP

To get browser access, we need to:
1. Install a Playwright MCP server in the Mavis runtime
2. Configure the tool
3. Restart the session

**OR** we use a different approach:
- Muhammad writes curl-based smoke tests (T6: build + test + typecheck) + JSON response validation
- Anas tests the UI manually in Chrome
- Admin captures feedback from Anas

**Decision needed from Anas:**
- Option A: Install Playwright MCP (1-2 hours setup, then I can browse)
- Option B: Use curl-based smoke tests only (faster, less coverage)
- Option C: Admin tests in Chrome, reports back (current state)

---

## Sprint 31 OPEN QUESTIONS

1. **P0: Projects module fix** — Should we add the 4 tables + seeder in Sprint 31, or defer?
2. **P1: Manual JEs** — In Sprint 31 or Sprint 32?
3. **P1: customer/vendor statements** — In Sprint 31 or Sprint 32?
4. **Browser MCP** — Install Playwright MCP, or use curl-based?
5. **Sprint duration** — 1.5-2h per item (default) or 4-6h (for big demo work)?

---

_Last updated: 2026-08-04 by Muhammad (Mavis mode) for Sprint 31 planning_
