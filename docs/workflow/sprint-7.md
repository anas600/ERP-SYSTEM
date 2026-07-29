# 🚀 Sprint 7: Test Coverage Deepening + E2E Activation

**Date:** 2026-07-29 23:35 UTC
**Architect:** Mavis Local (self-planned; awaiting Admin Team / Anas's final scope approval)
**Implementer:** Mavis Local (Tech Lead) + Jimis (BE + FE if needed)
**Owner:** Anas (Project Owner)
**Duration:** ~2-3 hours
**Deliverable:** ONE PR (`feature/sprint-7-test-coverage` → develop) + state.json v1.7
**Goal:** Continue from Sprint 6 wrap-up. Test gap-fill the 4 untested CoA service methods + activate E2E test infrastructure.

---

## 🎯 Why this sprint

Sprint 6 wrap-up (PR #175) completed T3 (BE test gap-fill, 4 new tests) but BE Jimi flagged 4 more CoA service methods that still need tests:
- `ChartOfAccountsService.GetByIdAsync` — no tests
- `ChartOfAccountsService.GetByCodeAsync` — no tests
- `ChartOfAccountsService.CreateAsync` — no tests (happy + duplicate-code + missing-parent scenarios)
- `ChartOfAccountsService.DeleteAsync` — no tests (has-postings + has-children + happy scenarios)

Additionally:
- The 2 `RetentionTests` failures are now officially **Skipped** (per Anas's surgical intervention 2026-07-29 23:33 UTC). They should be re-enabled in a future sprint when the test DB is provisioned.
- The `FakeDbConnectionFactory` has a project-wide limitation with SQL `AS` aliases — could be addressed as a follow-up.

**Sprint 7 should focus on the CoA test gap** (the highest-value, clearest scope). Optional additions:
- BE Jimi-flagged scope: 4 CoA methods
- Plus: maybe 1 test for a related entity (e.g., `CompanyService.GetByIdAsync` if missing)

---

## 🏛️ Architectural Constraints (same as Sprint 6)

> These are non-negotiable. No weakening of DOX or WORKFLOW.md.

### 1. Constitution Compliance
- **Article 3:** Multi-Company, NO Multi-Tenant — `grep -r tenant_id src/` → 0
- **Article 8 Rule 5:** `company_id` Only — all queries filter on `company_id`
- **Article 8 Rule 6:** No EF Core — Dapper + FluentMigrator only
- **Article 8 Rule 10:** Document in AGENTS.md — update nearest AGENTS.md per DOX
- **Article 11:** One Test Per Endpoint — each new/modified endpoint has a smoke test

### 2. Stack Discipline
- **Backend:** C# / .NET 9 / Dapper / FluentMigrator / xUnit
- **Frontend:** TypeScript / Next.js 14 / Tailwind / shadcn/ui / Jest
- **DB:** PostgreSQL (local Docker for Mavis Local dev, Supabase for cloud)

### 3. Test Standards
- Use `FakeDbConnectionFactory` (existing pattern)
- Workaround for `AS` aliases: use projected column names in `AddRow` (per BE Jimi's pattern in `CompanyTreeTests`)
- xUnit `[Fact]` for single-case, `[Theory]` for parameterized

---

## 📋 Tasks (T0–T5)

### T0 — Inventory
- `git log origin/develop --oneline -10` (Sprint 6 work: PRs #173, #174, #175, #176)
- `git log src/backend/Modules/Finance/Accounts/` (CoA service history)
- Read `src/backend/Tests/ERPSystem.Tests/Finance/ChartOfAccountsServiceTests.cs` (the existing 2 tests from Sprint 6)
- Read `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs` (the test helper)

### T1 — BE Jimi scope (Test Coverage Deepening)
**Goal:** Add 1+ test per untested CoA service method.

| Method | Test scenarios | Priority |
|--------|---------------|----------|
| `GetByIdAsync` | happy path (returns account), not-found (returns null) | P1 |
| `GetByCodeAsync` | happy path, not-found, code is case-sensitive (or not, per service contract) | P1 |
| `CreateAsync` | happy path, duplicate code (returns error), missing parent (returns error) | P1 |
| `DeleteAsync` | happy path (no postings, no children), has-postings (returns error), has-children (returns error) | P1 |

Plus: `CompanyService.GetByIdAsync` (1 test) if it doesn't have one.

**DoD:**
- 4+ new tests added
- `dotnet test`: 437 → 441+ passed, 0 failed, 32 skipped (unchanged)
- `dotnet build`: 0 errors
- No regressions

### T2 — (Optional) Doc Jimi scope
- Update `src/backend/Modules/Finance/AGENTS.md` with the new test contracts
- Update `CHANGELOG.md` with Sprint 7 entry
- Maybe: a `docs/testing/coa-test-coverage.md` doc with the test matrix

### T3 — Verification (Mavis Local)
- `dotnet test` — all green (or only 32 pre-existing skips)
- `dotnet build` — 0 errors
- `grep -r tenant_id src/` — 0 matches
- `grep -r "password\s*=" src/` — 0 secrets
- Pre-commit hook (TruffleHog) — clean

### T4 — Open PR + self-merge
- Branch: `feature/sprint-7-test-coverage` (off `develop @ 20aa9b0`)
- Open PR with the standard PR body (Context + Files + T6 verify + DoD)
- Self-merge per DEC-070 (squash, --admin via API if worktree conflict)
- State.json update (v1.7): `active_sprint = "7"`, `ball_location = "mavis-local"`, recent_merges[0] = new PR

### T5 — Hand-off back to Admin Team
- Per INTER-TEAM-PROTOCOL Template 3
- Send via `mavis cron once` with `session_id = mvs_4a1f6064397f4440bac82e3f36602646`
- Include: merge commit + diff stat + T6 verify results + next-cycle ball state

---

## 📊 Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **New tests** | ≥ 4 (1 per CoA method) | `dotnet test` count |
| **Test failures** | 0 | `dotnet test` (only 32 pre-existing skips) |
| **Build errors** | 0 | `dotnet build` |
| **Regressions** | 0 | All previously-passing tests still pass |
| **Cycle duration** | ≤ 2.5h | Start → PR merged |

---

## 🚨 Risks

| Risk | Mitigation |
|------|------------|
| **Test gap-fill is mechanical** — might run over | Stick to 4 CoA methods; don't expand scope without Admin approval |
| **`FakeDbConnectionFactory` AS alias limitation** — might block some tests | Use the BE Jimi workaround pattern (projected column names) |
| **Sprint 7 hand-off is self-planned** — Admin might want different scope | Send hand-off back early; await Admin's response before doing more |

---

## 📌 Out of Scope (defer to Sprint 8+)

- **Re-enable the 2 skipped `RetentionTests`** — needs `erp_test_system` test DB provisioning
- **`FakeDbConnectionFactory` AS alias enhancement** — needs design decision
- **E2E test suite (Playwright on `feature/abdo-team`)** — separate workstream, not in Mavis Local's scope
- **Performance optimization** — separate sprint
- **Production deploy** — needs Anas approval, FROZEN per legacy CONSTITUTION Article 10
- **Frontend Sprint 7 work** — none planned; the FE is stable at 0 warnings

---

## 🏃 Coordination Protocol

### Mavis Local's role
- Self-plan this sprint (per the 2-day window rule: Mavis Local can decide directly)
- Spawn 1 BE Jimi (Test Coverage Deepening) — optional, can do it myself
- Verify (T3) + open PR + self-merge per DEC-070 (T4)
- Send hand-off back to Admin Team (T5)

### Admin Team's role
- Available as Cron Jobs via state.json
- Review PR within 15 min (if they choose to; Mavis Local can self-merge)
- Watch for `blocked` state

### Communication
- **Primary:** state.json updates
- **Secondary:** PR comments
- **Template 1 v2 (informational, per Anas 2026-07-29 23:02 UTC):** may apply if Admin Team decides
- **Tool to use for hand-off back:** `mavis cron once` with `session.mode=sessionId` + `session.session_id=mvs_4a1f6064397f4440bac82e3f36602646`

---

*Hand-off created: 2026-07-29 23:35 UTC by Mavis Local (self-planned, ball in mavis-local court per the 2-day window)*
*Reference: PR #175 (Sprint 6 Wrap-up), PR #176 (state v1.6), Anas's surgical intervention at 23:33 UTC*
*Last updated: 2026-07-29 23:35 UTC*
