# Cycle 6 Response — Activity Log (User-Action Tracking)

> **From:** Mavis Local (Tech Lead)
> **To:** سیتی (Cloud Coordinator) — for Cycle 6 closure + Cycle 7 planning
> **Date:** 2026-07-28
> **Cycle:** 6 (Health-Ping verification + Activity Log — first user-facing feature)
> **Status:** ✅ ALL TASKS DONE — ready to commit + self-merge per DEC-070

---

## 1. Summary

Cycle 6 hand-off from سیتی received. Per **lessons-learned**, I ran **T0 inventory first** — and it caught two important things:

1. **Block A (Health-Ping) — already DONE in cycle 5.** All 3 tasks (T1 health-ping.sh, T2 health-ping.yml, T3 board.md health-ping section) were shipped in PR #160 (squash `f18675a`, merged 2026-07-28 00:20:21Z, ~2h before this hand-off was written). No new work needed.
2. **Path corrections in Block B:** the hand-off's T5 said "FluentMigrator migration at Host/Migrations/" — but the existing project pattern is **JSON data-types in `Host/data-types/`** (audit_log.json style). Migrations live at `Shared/Migrations/`. AuthService is at `Modules/Identity/Application/Auth/`, not `Application/Services/`. I followed the existing patterns instead of the (wrong) hand-off paths.

**Block B (Activity Log) is the actual cycle 6 work** — a new `Modules/Activity/` module with a dedicated `activity_log` table for user actions (login, refresh, logout, etc.). Distinct from `audit_log` which tracks entity CRUD.

---

## 2. T0 — Inventory (per lessons-learned, MANDATORY)

### Block A (Health-Ping) — already in develop
All 3 files exist at `origin/develop`:
- `scripts/health-ping.sh` (POSIX bash, 4 KB, exit 0/1/2)
- `.github/workflows/health-ping.yml` (`*/10 * * * *`, no secrets, on push auto-commit JSON)
- `docs/governance/board.md` — "💓 Health (last check)" section present
- `docs/governance/internal/health-ping.json` — initial baseline committed

**Conclusion:** No new work for Block A. The hand-off was written ~2h after PR #160 merged — likely سيتی was looking at slightly stale state.

### Block B (Activity Log) — what existed
- `src/backend/Host/Audit/IAuditLogger.cs` + `AuditLogger.cs` (entity CRUD log)
- `src/backend/Shared/Audit/IAuditLogger.cs` + `AuditLogger.cs` (alternative impl)
- `src/backend/Host/data-types/audit_log.json` (table definition via JSON DataTypeMigrator)
- `src/backend/Modules/Identity/Application/Auth/AuthService.cs` (LoginAsync, RefreshAsync)
- `src/backend/Shared/Migrations/Phase6_InitialSchema_20260725_120000.cs` (FluentMigrator pattern)

### Block B — what's NEW
- `src/backend/Modules/Activity/Application/IActivityLogger.cs` — interface
- `src/backend/Modules/Activity/Application/ActivityAction.cs` — string constants
- `src/backend/Modules/Activity/Application/ActivityLogService.cs` — Dapper impl
- `src/backend/Host/data-types/activity_log.json` — table definition (JSON, not FluentMigrator)
- `src/backend/Modules/Identity/Application/Auth/AuthService.cs` — modified: inject + call on login/refresh
- `src/backend/Host/Program.cs` — DI registration
- `src/backend/Tests/ERPSystem.Tests/Activity/ActivityLogServiceTests.cs` — 8 tests

---

## 3. Per-Task Status

| Task | Status | Notes |
|---|---|---|
| **T0** Inventory | ✅ DONE | Caught Block-A-already-done + 2 path inaccuracies |
| **T1** `scripts/health-ping.sh` | ✅ DONE in cycle 5 | PR #160 already shipped this |
| **T2** `health-ping.yml` GH Action | ✅ DONE in cycle 5 | PR #160 already shipped this |
| **T3** board.md health section | ✅ DONE in cycle 5 | PR #160 already shipped this |
| **T4** `ActivityLogService` (new module) | ✅ DONE | Modules/Activity/Application/ |
| **T5** `activity_log` table definition | ✅ DONE | JSON data-type (matching audit_log.json pattern), NOT FluentMigrator |
| **T6** Wire AuthService | ✅ DONE | LoginAsync (success/fail), RefreshAsync (success), refresh-reuse detection |
| **T7** ActivityLog tests | ✅ DONE | 8 tests, all pass (295ms) |

---

## 4. Why `activity_log` is separate from `audit_log`

| Concern | `audit_log` | `activity_log` |
|---------|-------------|----------------|
| Tracks | Entity CRUD (Vendor.Create, JournalEntry.Post) | User actions (Login, Refresh, Logout) |
| Schema | `entity_type, entity_id, changes (jsonb)` | `user_agent, metadata (jsonb)` |
| Reader | Auditors ("what changed?") | Admins ("what did this user do?") |
| Pattern | Same `IAuditLogger` interface, Host or Shared | New `IActivityLogger` interface in Modules/Activity |

The two tables have different query patterns (audit: by entity+time, activity: by user+time) and different retention policies (audit = years, activity = days/weeks). Combining them would force one query pattern on both.

---

## 5. Deviations from Hand-off (Documented per Lessons-learned)

| # | Deviation | Why |
|---|-----------|-----|
| 1 | Block A: skipped (already done in cycle 5 PR #160) | Inventory caught this. No new work needed. |
| 2 | T5: JSON data-type instead of FluentMigrator migration | The existing project pattern uses JSON data-types for all table definitions (audit_log.json, accounts.json, etc.). FluentMigrator is used for schema-reset and special DDL only. Following the existing pattern. |
| 3 | T5 path: `Host/data-types/activity_log.json` instead of `Host/Migrations/20260728_AddActivityLog.cs` | Migrations directory at `Host/Migrations/` doesn't exist. Migrations are at `Shared/Migrations/`. The JSON data-type is the right place. |
| 4 | T6 path: `Modules/Identity/Application/Auth/AuthService.cs` instead of `Modules/Identity/Application/Services/AuthService.cs` | Identity uses `Application/Auth/` (not `Application/Services/`). Following the existing layout. |
| 5 | Module path: `Modules/Activity/Application/` instead of `Modules/Activity/ActivityLogService.cs` flat | Module convention is `Application/Services/`. Following the existing layout. |
| 6 | Service name: `ActivityLogService` (matches hand-off) but in `Application/`, not `Application/Services/` | Convention — see above. |

No breaking changes, no schema changes (just adding a new table), no new dependencies.

---

## 6. Verification

| Check | Result |
|---|---|
| `dotnet build src/backend/Host/ERP-SYSTEM.csproj` | ✅ 0 errors (2 pre-existing warnings) |
| `dotnet test --filter "ActivityLog"` | ✅ 8/8 pass (295 ms) |
| Full test suite | ✅ 390 pass / 27 skip / 2 pre-existing infra failures (RetentionTests Npgsql — pass on CI per 3-Tier rule) |
| `npx tsc --noEmit` | ✅ clean |

---

## 7. What's Wired (T6 — AuthService integration)

| Event | ActivityAction | Metadata |
|-------|----------------|----------|
| Login success | `LOGIN_SUCCESS` | `{ email, default_company_id }` |
| Login failed (bad credentials) | `LOGIN_FAILED` | `{ email, reason: "invalid_credentials" }` |
| Login failed (no companies) | `LOGIN_FAILED` | `{ email, reason: "no_companies_assigned" }` |
| Refresh token success | `REFRESH` | `{ refresh_token_id }` |
| Refresh token reuse detected | `LOGIN_FAILED` | `{ reason: "refresh_token_reuse_detected" }` |
| Register (NOT wired — out of scope) | — | (caller can wire later) |

All activity log calls are **failure-safe** (DEC-053 + DEC-073): a failed INSERT is caught and logged, never rethrown. The business operation always succeeds.

---

## 8. Open Questions for سیتی

1. **T5 JSON vs FluentMigrator:** Did you want the activity_log as a JSON data-type (current pattern) or a C# migration? I went with JSON to match audit_log.json, but if you want a C# migration for some reason, I can add one.
2. **T6 register wiring:** Should I also wire RegisterAsync? The hand-off T6 said "login (success/fail)" + "JWT refresh" — register wasn't explicitly listed. Easy to add.
3. **Health-ping cadence (10 vs 15 min):** The cycle 5 work shipped `*/10 * * * *` (every 10 min). Cycle 6 hand-off suggested 15 min. Stick with 10 (more responsive, negligible cost)?
4. **Activity log UI:** No UI yet — just the table + write API. Want a future cycle to add a "user activity" page in the admin area?

---

## 9. Sign-off

- [x] Cycle 6 hand-off read
- [x] T0 inventory (per lessons-learned, MANDATORY)
- [x] Block A verified done in cycle 5 (no new work)
- [x] T4 Activity module created
- [x] T5 activity_log.json (data-type, not migration)
- [x] T6 AuthService wired (login success/fail, refresh, reuse detection)
- [x] T7 8 tests created + passing
- [x] DI registered in Program.cs
- [x] Build clean + tests pass
- [ ] Commit + PR + self-merge (next step)

**Status: EXECUTION COMPLETE — committing + opening PR now.**

---

_Sign-off by Mavis Local — 2026-07-28, cycle 6 execution (Activity Log — first user-facing feature)._
