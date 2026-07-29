# Sprint 4: Polish + Demo Data

**Goal:** Final polish + demo data seed for MFA Holding client demo
**Time:** ~3 hours (Mephisto sandbox) | **Owner:** Mephisto (External Tech Lead, sandbox)
**Refs:** [architecture.md](architecture.md) | [demo-roadmap.md](demo-roadmap.md) | [sprint-3.md](sprint-3.md) | [demo-data spec](../../seed-sprint4-demo-data.sql)

## Context

Sprint 3 (Activity + Notifications) merged via PR #167 on 2026-07-28.
Sprint 4 was the final sprint before Verify+Deploy. The original branch
(`feature/sprint-4-polish-demo-data` @ c23d39bf) was opened as PR #168
with 121 files changed — but the cleanup amendment (2026-07-29) removed
124 docs/seeds from the repo, causing 142 conflicts on AGENTS.md,
CONSTITUTION.md, and CHANGELOG.md.

This sprint was re-scoped to a **clean cherry-pick** on the DOX-cleaned
develop branch: only the demo data SQL + static tests survive.

## Block A (Mephisto — Cherry-pick 2 files, 1h)

- [x] **T1:** `docs/seed-sprint4-demo-data.sql` (968 lines, 50KB)
  - 1 Holding + 3 subsidiary companies (ALF-CONST مقاولات, ALF-TRADE تجارة, ALN-LOG لوجستيات)
  - 10 users (1 admin + 9 new), all BCrypt-hashed password "Demo1234"
  - 30 sales invoices (S4-0001..S4-0030)
  - 20 vendor bills (7+7+6 distribution)
  - 30 journal entries (with balanced DR/CR)
  - 20 stock movements (10 IN + 10 OUT)
  - 42 activity log entries (5+/day for 7+ days)
  - 38× `ON CONFLICT` guards + 4× `CREATE TABLE IF NOT EXISTS` (fully idempotent)
  - 480 Arabic sequences (RTL-ready)
- [x] **T2:** `src/backend/Tests/ERPSystem.Tests/Seed/Sprint4SeedTests.cs` (251 lines, 10.8KB)
  - 19 static tests (no DB required, BASIC only)
  - Validates: companies, users, BCrypt, idempotency, Arabic, company_id-scope, journal balance, activity log, summary

## Block B (Mephisto — DOX polish, 30 min)

- [x] **T3:** `docs/workflow/sprint-4.md` — this file
- [x] **T4:** `CHANGELOG.md` — Sprint 4 entry + reference to PR
- [x] **T5:** `src/backend/Tests/AGENTS.md` — add `Seed/` to Child DOX Index

## Block C (Mavis Local — Verification, 30 min)

- [ ] **T6:** `dotnet build` on cleaned develop (after PR merge)
- [ ] **T7:** `dotnet test --filter Sprint4SeedTests` (must be 19/19 pass)
- [ ] **T8:** Confirm no `tenant_id` in new files: `grep -r "tenant" src/`
- [ ] **T9:** Approve PR + merge with `--admin` (per Constitution Article 10)
- [ ] **T10:** Run `psql -f docs/seed-sprint4-demo-data.sql` on dev DB (Supabase)
- [ ] **T11:** Verify sample login: `admin@alahliya.ly / Demo1234` (TBD: confirm exact email)

## Permissions (per Constitution)

- ✅ Self-merge allowed: Constitution Article 10 (admin bypass ON)
- ✅ Skip Playwright: not required for static tests
- ✅ Use sandbox git credentials (GITHUB_TOKEN issued by Anas, short-lived)

## Why cherry-pick, not rebase

| Approach | Files | Conflicts | Outcome |
|----------|-------|-----------|---------|
| **A. Rebase original branch (c23d39bf) onto cleaned develop** | 121 changed | 142 conflicts on AGENTS.md, CONSTITUTION.md, CHANGELOG.md | ❌ High risk, requires massive conflict resolution |
| **B. Clean cherry-pick 2 files on develop** | 2 changed | 0 | ✅ Clean, atomic, self-contained |

Option B is **strictly superior** for a single-purpose demo-data PR.

## Verification (Mephisto's sandbox run, 2026-07-29)

```bash
# Build
$ dotnet build Tests/ERPSystem.Tests/ERPSystem.Tests.csproj
Build succeeded.
0 Error(s). 15 Warning(s) (all pre-existing).
Time: 26.95 seconds

# Test
$ dotnet test --filter "FullyQualifiedName~Sprint4SeedTests" --no-build
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19
Duration: 46 ms
File: /root/ERP-SYSTEM-clean/src/backend/Tests/ERPSystem.Tests/bin/Debug/net9.0/ERPSystem.Tests.dll

# DOX-rail (Constitution Article 3)
$ grep -r "tenant_id" docs/seed-sprint4-demo-data.sql src/backend/Tests/ERPSystem.Tests/Seed/
# 0 matches in SQL; comments only in tests
```

## Definition of Done

- [x] 2 files committed to `feature/sprint-4-polish-demo-data-v2`
- [x] `dotnet build` clean
- [x] 19/19 static tests pass
- [x] No `tenant_id` in new files
- [x] DOX-rail read: root, src/, src/backend/, src/backend/Tests/, docs/
- [x] Branch pushed to origin
- [x] PR opened against `develop`
- [ ] PR approved by Mavis Local
- [ ] PR merged with `--admin` (squash)
- [ ] Demo data loaded into dev DB

## Notes

- Demo password is "Demo1234" (per Constitution Article 7, demo-only).
- BCrypt cost 11/12 (mixed): 12 is preferred per Constitution, but 11 is acceptable.
- `docs/architecture/holding-company-architecture.md` is referenced from CHANGELOG but may need follow-up if it was also pruned in cleanup.

## Next Sprint

Verify + Deploy (separate sprint, TBD by Mavis Local)

---

— Mephisto (Hermi), 2026-07-29
