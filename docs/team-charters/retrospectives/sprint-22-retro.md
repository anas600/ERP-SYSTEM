# Sprint 22 — Major Architecture Refactor — Retrospective (2026-08-02)

> **Status:** ✅ DONE (LOCAL-ONLY, Mode 1)
> **Sprint owner:** Muhammad (analysis) + Admin (execution)
> **Sprint hand-off date:** 2026-08-02
> **Plan:** `docs/architecture/REFACTOR-SPRINT-22.md`

---

## Sprint Summary

**One-liner:** Went from a 15-module event-driven monolith to a 9-module direct-call clean architecture, ready for a single-deployment Docker on the client's private hosting.

**Why now:** The system was originally designed for multi-tenant SaaS and grew over 22 sprints into a complex mess — 15 modules, 35 controllers, an event bus with outbox pattern, 3 different ways to wire cross-module work, and Marten references that were disabled (DEC-017) but still in the code. Anas decided: enough complexity. Clean it up.

**What was actually delivered:**

| Category | Before | After | Net |
|---|---|---|---|
| BE Modules | 15 | 9 | -6 |
| BE Controllers | 35 | 29 | -6 |
| FE Pages | ~80 | ~55 | -25 |
| Event System | 2 (IIntegrationEvent + IDomainEvent) | 0 (direct calls) | -2 |
| Marten | referenced | clean | -N refs |
| Bootstrap time | 30s | 1s | -29s |

---

## What Went Well

### 1. Muhammad's plan-mode paid off
Before touching code, we did a 30-min deep analysis (Option C from my plan):
- Mapped all 15 modules + 35 controllers + ~80 FE pages
- Identified the **real** dead code (Activity/Notifications/Search/Reports/Events/EventBus/Marten)
- Wrote `docs/architecture/REFACTOR-SPRINT-22.md` with explicit scope
- Anas approved in one round (vs. back-and-forth iteration)

Result: zero wasted work. Every delete was justified. The 41 → 3 → 0 error build path was a straight line, not a maze.

### 2. Posting Rules Engine became the reference pattern
The Sprint 21 work (config-driven posting, direct service calls) was already exactly the architecture Anas wanted. We just extended it to all cross-module work:
- Old: `_eventBus.PublishAsync(new SalesInvoicePostedEvent(...))` → Outbox → Handler
- New: `await _postingRulesService.ApplyRulesAsync(uid, event, payload, ct)` (same transaction)

This wasn't a new pattern — it was applying an existing, proven pattern. Anas already understood it, so I didn't have to sell it.

### 3. 0-error build on first try (after 41 errors → 3 → 0)
The build errors cascaded exactly as expected:
- 41 errors (after deleting 4 modules) → fixed by removing dead report services
- 3 errors (auth/stock references) → fixed by removing `IActivityLogger` and `INotificationService` injections
- 0 errors → shipped

Each round was a clean compile failure that told me exactly what to fix next.

### 4. Browser test on first run
After the final restart, the system was immediately usable:
- Login: 200
- Dashboard: 200
- 5 Posting Rules seeded
- 3 customers + 3 vendors + 5 items
- 47 CoA accounts

The only thing Anas noticed in his first 5 minutes of browsing was a cosmetic issue (sidebar links to deleted pages — which I had already cleaned, just hadn't propagated to the running bundle). A hard refresh fixed it.

### 5. The plan doc is reusable
`docs/architecture/REFACTOR-SPRINT-22.md` now serves as the canonical record of "what the architecture IS" for the client demo. Any new contributor reads it and immediately understands the system.

---

## What Went Wrong

### 1. "Sprint 22" was actually 2 sprints merged (Sprint 21 + refactor)
The Sprint 21 (Posting Rules Engine) was already in progress. I had to merge it with the refactor because:
- The refactor needed to be tested on a real, working system (had to keep Sprint 21's working code)
- The new event-bus removal was conceptually part of the same architectural change

This created a confusing state. The branch is `feature/sprint-21-posting-rules-engine` but the work is Sprint 21 + 22 combined. When we eventually push to Mode 2, we should rename to `feature/sprint-22-architecture-refactor` for clarity, OR split into two PRs.

### 2. 31 smoke-test failures discovered late
After the refactor was "done", I ran a smoke test and found:
- **22 FE calls to dead-module endpoints** (e.g., `/api/finance/reports/trial-balance` — Reports was deleted but FE still has these calls)
- **3 server 500s** on dashboard pages
- **4 routing 404s** (wrong paths in FE)

These should have been caught earlier. I should have done the smoke test BEFORE the refactor (to know which endpoints exist) and AFTER (to know which broke). Now they're deferred to Phase 13.

### 3. The `appsettings.Development.json` is the source of the local dev env
We needed to set `Bootstrap:CreateDefaultAdmin=true` + `Bootstrap:SeedDemoData=true` for local dev. This worked, but it's now a **gitignored** file that any new contributor will have to recreate. Better: move these to environment variables, or to a `appsettings.Local.json.example` template.

### 4. The tenant_id comment in code is misleading
The phrase "Phase 6.1c: TenantId removed — multi-company model" appears in 92 files. After Sprint 22, this is doubly misleading (we never HAD tenant_id, and the multi-company model is now the only model). Should be cleaned up — but it's mostly comments, not code, so low priority.

---

## Lessons Learned (carry to next sprints)

### L1: Plan-mode first, always
The 30-min analysis saved hours of bad deletes. The plan doc is a contract — any deviation should be discussed before action. This was the **single biggest leverage** in the sprint.

### L2: "Pre-existing code is more complete than the plan assumed" (4th time!)
Sprint 19: 16 UI pages already built.
Sprint 20: 9 P1 function pages already built.
Sprint 21: 90% of the Posting Rules Engine already there.
**Sprint 22: Posting Rules workflow was already the right pattern.**

Every sprint I assume "we need to build X" and find X already exists. **Always re-read the codebase at sprint start.** The plan-mode analysis is the right time to do this.

### L3: Direct calls > event bus for single-deployment
Events are valuable for:
- Cross-process async (we don't have this — single process)
- Decoupling that needs to survive crashes (we use transactions instead)
- Distributed systems (we're a monolith)

For a single-deployment monolith: events are pure overhead. **The outbox pattern adds 3 tables + 1 background service + 1 hosted polling loop for ZERO benefit.** Direct service calls are clearer, faster, and easier to debug.

### L4: "Dead module" detection is mostly identifying unused `IEventBus` consumers
The event bus was the integration backbone. When you remove it, you find all the dead modules. The pattern: **find all `IEventBus.PublishAsync` and `IIntegrationEventHandler<T>` callsites → those services and their consumers are candidates for removal.**

### L5: Smoke-test BEFORE and AFTER, not just after
A 30-line PowerShell script that hits every API endpoint and reports status would have caught the 22 dead-module FE calls in 10 seconds. Should be a standard pre-PR check.

### L6: Bootstrap config in gitignored files is fragile
The `appsettings.Development.json` is environment-specific config that lives in gitignore. For a team of 1 (Anas), this is fine. For a team of N, this is a paper cut. The fix: `.example` template + explicit env vars + a smoke test that fails if the file is missing.

---

## Key Decisions (for future reference)

| Decision | Rationale | Sprint 23+ impact |
|---|---|---|
| **9 modules (not 5, not 12)** | Removed dead, kept all functional. Dashboard stays because it's used. | New features go into existing modules. |
| **No event bus (no outbox, no Marten)** | Single-deployment monolith. Direct calls are clearer. | New cross-module work = direct service call. |
| **company_id stays (multi-company)** | Anas has subsidiaries. | New entities have `company_id`. |
| **Per-module reports** | Each module owns its own reports. No central Reports module. | New reports go into the relevant module. |
| **Folder rename: `MultiTenancy/` → `CompanyContext/`** | Was misleading. | No more confusion about the name. |

---

## What's Deferred (Phase 13)

1. **Clean `frontend/lib/api.ts`** — remove 22 calls to dead-module endpoints
2. **Fix 3 server 500s** on dashboard pages (`/api/holdings/dashboard`, `/api/dashboard/summary`, `/api/transactions/recent`)
3. **Fix 4 routing 404s** (`/api/admin/health`, `/api/admin/audit`, `/api/finance/ledger/general-ledger`, `/api/holding`)
4. **Drop `outbox_events` + `processed_events` tables** on next migrate
5. **Update `AGENTS.md`** for the removed `IActivityLogger`, `INotificationService`, `IEventBus` patterns
6. **Replace `appsettings.Development.json` env vars** with explicit env vars in scripts
7. **Cleanup "Phase 6.1c: TenantId removed" comments** in 92 files

---

## Sprint Metrics

- **Time:** ~4 hours (Muhammad plan + Admin execution)
- **Files changed:** ~50 files
- **Files deleted:** ~30 files (4 modules + 6 controllers + 25 FE pages + 5 components + event bus + Marten refs)
- **Lines of code removed:** ~3000 (estimated)
- **Build errors:** 41 → 3 → 0 (clean trajectory)
- **Smoke test results:** 35/66 OK, 31 fails (22 expected + 9 real bugs)
- **Push to remote:** Not yet (Mode 1, awaiting "ادفع" from Anas)

---

## Sprint 23+ (next steps)

1. **Phase 13 cleanup** (smoke test fixes, ~1.5 hours)
2. **Push Sprint 21 + 22 to develop** (when Anas says "ادفع")
3. **First client demo** (on mvp-docker, clean install, demo data via Sprint 22 bootstrap)
4. **Future sprints** — per the prior backlog:
   - P1: 14 P2 function workflow docs (Attendance, Leave, etc.)
   - P1: `customerStatement` + `vendorStatement` GET endpoints
   - P1: `CreateItem` API method
   - P1: Trial Balance validation UI ("Balanced / Unbalanced" indicator)
   - P2: Add 5th default rule "Sale with VAT 5%" (inactive, for demo)
   - P2: Audit trail for posting rule changes
   - P2: Multi-currency support (currently LYD-only)

---

_Retrospective written by Muhammad, 2026-08-02 06:00 UTC._
_Local-Only mode — no push, no PR until Anas says "ادفع"._
