# 🛠️ Sprint 10: Holding Refactor Phase 2 + 3 (LOCAL-ONLY per Anas 06:47 UTC)

> **Date:** 2026-07-31
> **Architect:** Mavis (محمد mode)
> **Owner:** Anas (Project Owner) — pivot to **LOCAL-ONLY** dev (no PR until the end)
> **Status:** 🟡 HAND-OFF ready
> **Source:** Per Anas mandate 2026-07-31 06:47 UTC (stop auto-merge, work locally only)

---

## 🎯 Goal

Continue the **Holding Company Refactor Proposal** (Sprint 8 T4) — **Phase 2 + 3** — but **locally only** per Anas's new strategy. No push to cloud, no PR to develop. The PR (with all 3 phases' work) opens at the END when the local state is stable.

**Deliverable:** Local commits on `feature/sprint-10-refactor-multi-tenancy-rename`. Final PR (with all phases) when Anas says.

---

## 📋 Tasks (T0–T4)

### T0 — Inventory (Coordinator)

- ✅ Sprint 8 T4 proposal exists: `docs/architecture/holding-company-refactor-proposal.md`
- ✅ Sprint 9 T1 (Phase 1 docs) completed in `feature/sprint-9-demo` (open in PR #182, not merged)
- Phase 2 (rename) + Phase 3 (scoped DI) — **NOT YET STARTED**
- `Shared/MultiTenancy/` folder still has the misleading name (28 files reference it)
- `CompanyContext` still uses `AsyncLocal` (static state fragility)

---

### T1 — Phase 2: Rename `Shared/MultiTenancy/` → `Shared/CompanyContext/`

**Goal:** Eliminate the misleading folder name + namespace.

| # | Action | Files | Effort |
|---|--------|-------|--------|
| 2.1 | Move 3 files | `CompanyContext.cs`, `ICompanyContext.cs`, `CompanyContextMiddleware.cs` | 0.5h |
| 2.2 | Update `namespace ERPSystem.Shared.MultiTenancy;` → `namespace ERPSystem.Shared.CompanyContext;` | 3 files | 0.5h |
| 2.3 | Find/replace `using ERPSystem.Shared.MultiTenancy;` → `using ERPSystem.Shared.CompanyContext;` | 28 referencing files | 2h |
| 2.4 | Verify: `dotnet build` (0 errors) + `dotnet test` (436 pass) + `npm run type-check` (0 errors) | — | 1h |
| 2.5 | Commit as `refactor(be): rename Shared/MultiTenancy → Shared/CompanyContext (align with Article 3)` | — | 0.5h |

**Risk:** Low — pure rename, no behavior change. CI catches any missed reference.

**Out of scope:** No code changes to the implementation itself (just the namespace).

---

### T2 — Phase 3: Replace `AsyncLocal` with scoped DI

**Goal:** Make `CompanyContext` properly scoped to the request via DI instead of static state.

**Migration path:**

1. Add `IHttpContextAccessor` to DI (already in ASP.NET Core).
2. Make `ICompanyContext` a **scoped** service.
3. Middleware writes to `HttpContext.Items` instead of `AsyncLocal`.
4. Service reads from `_http.HttpContext?.Items`.
5. Remove `Set`/`Clear` from interface (breaking change, but internal — only middleware + 1-2 tests use them).
6. Update 28 referencing files (mostly constructor injection changes).
7. Update tests to use a mock `IHttpContextAccessor` instead of `CompanyContext.Clear()`.

| # | Action | Files | Effort |
|---|--------|-------|--------|
| 3.1 | Add `IHttpContextAccessor` registration to `Program.cs` | 1 file | 0.5h |
| 3.2 | Change `CompanyContext` to inject `IHttpContextAccessor` | 1 file | 1h |
| 3.3 | Change `CompanyContext` to read from `HttpContext.Items` | 1 file | 1h |
| 3.4 | Change `CompanyContextMiddleware` to write to `HttpContext.Items` | 1 file | 1h |
| 3.5 | Update DI registration: `services.AddScoped<ICompanyContext, CompanyContext>();` | 1 file | 0.5h |
| 3.6 | Update 28 referencing files (constructor changes) | 28 files | 4h |
| 3.7 | Update tests (`CompanyContextTests.cs` + `DashboardSummaryTests` + others) | 4-5 files | 2h |
| 3.8 | Verify: build + test (436 pass) + typecheck | — | 1h |
| 3.9 | Commit as `refactor(be): replace AsyncLocal with scoped DI in CompanyContext (Phase 3)` | — | 0.5h |

**Risk:** Medium — changes a core contract. Requires:
- Coordination with all 28 referencing files
- Test rewrite for any test that calls `companyContext.Set(...)` directly
- Possibly a feature flag for staged rollout

**Benefit:** Removes the static state fragility, plays nice with `Task.WhenAll`, easier to test, works with `BackgroundService` (uses `IServiceScopeFactory`).

---

### T3 — Holding Company Docs Section 6 Fix (Follow-up from Sprint 9 Jimi 1)

**Context:** Jimi 1 in Sprint 9 flagged that **Section 6** of the architecture doc still has the legacy `companies` SQL with `holding_id` (FK → `holdings`). Same fix pattern as T1.

| # | Action | Effort |
|---|--------|--------|
| 3a.1 | Update `docs/architecture/holding-company-architecture.md` Section 6 (Multi-Company) — remove `holding_id` FK, use self-ref model | 1h |
| 3a.2 | Commit as `docs(be): Sprint 10 — fix Section 6 of architecture doc (self-ref model)` | 0.25h |

---

### T4 — Local Verification (Coordinator role)

After all 3 Jimis finish:
```bash
# In the worktree
cd C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-10
git log --oneline -10              # verify 3+ commits
dotnet build                        # 0 errors
dotnet test                         # 436 pass + 0 fail new
npm run type-check                  # 0 errors
npm run build                       # success
grep -r "MultiTenancy" src/         # 0 matches (after rename)
grep -r "tenant_id" src/            # 0 matches
```

**No push. No PR. Per Anas's new strategy — local-only until the end.**

---

## 📊 Sprint 10 — Success Criteria

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **New tests** | ≥ 0 (mostly refactor, no new features) | `dotnet test` count unchanged |
| **Test failures** | 0 new | `dotnet test` |
| **Build errors** | 0 | `dotnet build` |
| **Regressions** | 0 | All 436 existing tests still pass |
| **AsyncLocal removed** | 1 occurrence → 0 | `grep -r "AsyncLocal" src/backend/Shared/CompanyContext/` |
| **MultiTenancy namespace** | 0 occurrences | `grep -r "MultiTenancy" src/` |
| **Cycle duration** | ≤ 1 day (per R7) | Start → last commit |

---

## 🚦 Status Check (Anas's new directive)

- ❌ **Auto-merge cron DISABLED** (sprint-pr-review-v1.8 — `28e88987-...`)
- ✅ **Local-only** for this sprint
- ❌ **No push to cloud**
- ❌ **No PR to develop**
- ✅ **Push + PR at the end** (per Anas: "اخيرا يرفع بي ار ع الجت هوب")
- ✅ **PR #182 (Sprint 9 demo) OPEN, MERGEABLE** — kept for the end
- ✅ **3 Jimis max parallel** (per Anas)

---

## 🛠️ Architecture Plan (Holding Refactor Phase 2 + 3)

Per Sprint 8 T4 proposal:

**Phase 2 — Rename** (1 day, low risk):
- Move `Shared/MultiTenancy/` → `Shared/CompanyContext/`
- Update 28 referencing files
- Pure rename, no behavior change

**Phase 3 — Scoped DI** (1 day, medium risk):
- Replace `AsyncLocal` with `IHttpContextAccessor` + `HttpContext.Items`
- Update 28 referencing files
- Update tests
- Add `AddScoped` DI registration

**Section 6 fix** (1.25h):
- Continue the docs cleanup from Sprint 9 T1

---

## 🔗 Reference Files

- `docs/architecture/holding-company-refactor-proposal.md` (Sprint 8 T4)
- `docs/architecture/holding-company-architecture.md` (current, needs Section 6 fix)
- `src/backend/Shared/MultiTenancy/CompanyContext.cs` (the AsyncLocal code to refactor)
- `docs/workflow/sprint-9-demo.md` (Sprint 9 hand-off, for pattern reference)
- `docs/personas/local-team.md` (Mavis Local's role)

---

## 🚀 How to spawn the Jimis (LOCAL-ONLY)

3 Jimis, parallel (per Anas "max 3 parallel"):

**Jimi 1 (Phase 2) — Rename:**
- 32 files (3 moves + 28 namespace updates + 1 commit)
- ~4 hours
- Read all 28 files that reference `MultiTenancy`

**Jimi 2 (Phase 3) — Scoped DI:**
- 8 files (CompanyContext rewrite + middleware + DI + tests)
- ~8 hours (medium risk)
- Read .mavis/AGENTS.md + the test patterns

**Jimi 3 (Section 6 docs) — Architecture fix:**
- 1 file (architecture doc)
- ~1.25 hours
- Quick docs work

---

**Approval chain:**
- ✅ Anas (Owner) approved local-only dev at 2026-07-31 06:47 UTC
- ✅ Mavis (Coordinator) drafted the hand-off
- ⏸️ Local Team v1.8 — your turn (spawn 3 Jimis in parallel, local-only)

🛠️ Go.
