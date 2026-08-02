# Sprint 24 Retrospective — outbox cleanup + Constitution Article 3 audit

**Sprint:** 24
**Branch:** `feature/sprint-21-posting-rules-engine` (Sprint 21+22+23+24 stacked)
**Date:** 2026-08-02
**Status:** ✅ LOCAL-ONLY done. Awaiting Anas "ادفع" → Mode 2 push.

---

## Goal (per DEC-082 + DEC-083 + carry-over from Sprint 22 retro)

1. **DEC-082:** Drop the outbox tables (`outbox_events` + `processed_events`) — the last piece of the "no event bus" cleanup that Sprint 22 left unfinished.
2. **DEC-083:** Run a code-level audit of Constitution Article 3 — verify every entity has `company_id`, every service that creates an entity reads it from `ICompanyContext` (never `Guid.Empty`), every repository INSERT includes `company_id`.
3. **Carry-over:** Document the `appsettings.Development.json` example template (new contributors can copy + edit).

---

## What we built

### DEC-082: Outbox cleanup
- **`Sprint24_DropOutboxAndProcessedEvents_20260802_120000`** migration:
  - `DROP TABLE IF EXISTS outbox_events CASCADE;`
  - `DROP TABLE IF EXISTS processed_events CASCADE;`
  - Idempotent. One-way (Down() throws NotSupportedException with a clear recovery path: re-add the JSON files from git history).
- **JSON files removed:** `outbox_events.json` + `processed_events.json` (DataTypeMigrator won't recreate the tables on next startup).
- **Code refs cleaned:**
  - `Program.cs` — replaced the 3 stale comments (OutboxEventPublisher, IOutboxRepository, OutboxProcessorHostedService) with a single Sprint 24 + Sprint 22 breadcrumb.
  - `RetentionTests.cs` — removed `outbox_events` + `processed_events` from the retention-period dict (Sprint 24 comment explains why).

### DEC-083: Constitution Article 3 audit (code-level)

**Found 2 more `tenant_id`-style violations** (sequence tables missing `company_id`):

| Table | Before | After |
|---|---|---|
| `procurement_document_sequences` | `PK (prefix)` only — no company_id | `PK (company_id, prefix)` + `company_id NOT NULL` |
| `hr_document_sequences` | `PK (prefix)` only — no company_id | `PK (company_id, prefix)` + `company_id NOT NULL` |
| `ar_document_sequences` | Already had `company_id` (added in earlier work) | (unchanged) |

**Why this matters:** In a single-deployment-with-N-subsidiaries world (the 3-Layer Model + Article 3), two companies sharing the same `prefix` (e.g. both want `PO-2026-0001`) would collide on the same sequence number. Adding `company_id` to the PK scopes each company's counter independently.

**Fix:**
- **`Sprint24_DocumentSequencesAddCompanyId_20260802_121000`** migration:
  - `ALTER TABLE ... ADD COLUMN IF NOT EXISTS company_id UUID;` (nullable first for safe migration)
  - Backfill: `UPDATE ... SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;` (only safe default in a non-multi-company legacy DB)
  - Enforce NOT NULL + drop old single-column PK + add new composite PK, all wrapped in DO blocks for idempotency
- **3 sequence repositories** updated:
  - `DocumentSequenceRepository` (Procurement) — now takes `ICompanyContext` in ctor, uses it in the UPSERT and SELECT
  - `PaymentSequenceRepository` (Payments) — same pattern (uses the same `procurement_document_sequences` table)
  - `HRDocumentSequenceRepository` (HR) — same pattern

### Sprint 24 docs
- **`appsettings.Development.json.example`** — template for the gitignored `appsettings.Development.json`. Every key (ConnectionStrings, Marten, JwtSettings, Deployment, Bootstrap) has a comment explaining the safe local-dev default.
- **`docs/team-charters/retrospectives/sprint-24-retro.md`** — this file.
- **`CHANGELOG.md`** — Sprint 24 entry at the top with Removed / Fixed / Added / Carry-over / Verified sections.

---

## Stats

- 9 files changed: 2 new migrations + 4 source files (Program.cs, 3 sequence repos) + 1 test + 2 deleted JSONs + 1 new template
- Build: 0 errors, 0 warnings
- No `tenant_id` regressions
- 2 new migrations are idempotent (DO blocks + IF EXISTS guards)
- 5 lessons (below)

---

## Decisions (DEC-082, DEC-083, + 4 new)

- **DEC-082 (carried from Sprint 22 retro):** `outbox_events` + `processed_events` tables dropped in a single migration. One-way (Down() not supported) — the event bus is gone for good.
- **DEC-083 (new):** Every document sequence table must include `company_id` in the PK. The 3 sequence repositories all take `ICompanyContext` in the constructor.
- **DEC-084 (new):** `appsettings.Development.json.example` is the template. New contributors copy → edit → gitignore keeps it private. (No more "what do I put in this file?" questions.)
- **DEC-085 (new):** Constitution Article 3 audit is now a recurring task in the pre-push checklist (in `AGENTS.md`). The audit looks for: (1) entities without `CompanyId`, (2) `CompanyId = Guid.Empty` boilerplate, (3) `CREATE TABLE` statements without `company_id`, (4) runtime `INSERT` statements without `company_id`, (5) PK definitions on shared-resource tables that don't include `company_id`.
- **DEC-086 (new):** "PRE-EXISTING CODE IS MORE COMPLETE (AND MORE BROKEN) THAN YOU THINK" — 6 sprints in a row (S19-24) have surfaced at least one pre-existing bug that the plan assumed didn't exist. The Article 3 audit (DEC-085) is the formalization of this lesson.

---

## Lessons (L1-L7, building on L1-L11 from prior sprints)

- **L1: "Pre-existing code is more complete (and more broken) than the plan assumed" (6th time in a row).** Sprint 24 plan said "drop outbox JSON files" — we found 3 sequence tables also missing `company_id`. The audit always finds something.
- **L2: "Audit + migration + code update + test = atomic."** Sprint 23 had a similar pattern (Sprint 23.2 was: drop `activity_log` from DashboardSummary + update test). Sprint 24 has the same pattern at a larger scale (4 files: 2 migrations + 3 repos). When you touch a schema, the test + the code must move together, or the CI will catch you.
- **L3: "Idempotency is not optional in migrations."** The two Sprint 24 migrations use `ADD COLUMN IF NOT EXISTS` + DO blocks + backfill with WHERE company_id IS NULL. The DO block lets us re-run on a fresh DB (where the new PK is already in place from the JSON migrator) without erroring out. **Future migration template** — copy this structure.
- **L4: "One-way migrations should be explicit about the recovery path."** Both Sprint 24 migrations throw `NotSupportedException` from `Down()` with a clear "to revert, do X" message in the exception text. This is much better than a silent no-op.
- **L5: "Sequence tables are the canary."** They look trivial (3 columns, 1 INSERT) but they're a shared resource across multiple modules (Procurement, Payments, HR) and a single-company world can hide the multi-company bug. **Lesson:** when reviewing any shared-resource table, the PK is the first place to look for Article 3 violations.
- **L6: "appsettings.Development.json.example is a documentation surface, not just a template."** Putting the safe defaults in a version-controlled file (with comments explaining what each value does) means the next contributor can fork-and-edit instead of asking "what should this be?" The _comment_* keys are JSON-style docstrings; they don't affect runtime but they're searchable.
- **L7: "Constitution Article 3 audit should be pre-push, not post-merge."** Sprint 23 caught a 3-bug wave (entity without CompanyId, 2 services with `Guid.Empty`) at the audit table. Sprint 24 found 2 more (sequence tables). Every sprint since 19 has surfaced at least one Article 3 violation. **Action:** add DEC-085 to the pre-push checklist in `AGENTS.md`.

---

## Carry-over (Sprint 25+, still open from Sprints 19-23)

| Priority | Item | From | Notes |
|---|---|---|---|
| P1 | 14 P2 function workflow docs | Sprint 19 | Attendance, Leave, Department, Cost Center, Posting Rules, Stock Movement, Warehouse, Item Category, UoM, User/Role, Audit Log, Holding/Company, Notification, Activity Feed |
| P1 | `customerStatement` + `vendorStatement` GET endpoints | Sprint 15 | AR aging + AP aging statements |
| P1 | `CreateItem` API method | Sprint 16 | P0 polish carry-over |
| P1 | Trial Balance validation UI | Sprint 21 | "Balanced / Unbalanced" indicator on the GL report page |
| P1 | Posting Rules integration unit tests | Sprint 23.1 | New — Stock→Posting direct call needs coverage. Carry-over to Sprint 25. |
| P2 | 5th default posting rule "Sale with VAT 5%" (inactive, for demo) | Sprint 21 | Libya default = no tax, but a tax-on-tax rule is useful to show the engine works for accountants who want tax. |
| P2 | Audit trail for posting rule changes | Sprint 21 | Every Create/Update/Delete on `posting_rules` should write to `audit_log`. |
| P2 | Multi-currency support (currently LYD-only) | Sprint 21 | `currency_code` column exists; FX rates + conversion at posting time needed. |
| P2 | `mvp-docker/.env` to `.gitignore` | Sprint 14 | Already gitignored in practice (it's in `.gitignore` already — verify). |
| P2 | Move outbox + processed_events JSON deletion out of CI | Sprint 24 | Sprint 24 just dropped them; verify the CI build doesn't reference them anywhere. |

---

## Status & next steps

- ✅ Sprint 21 done (LOCAL) — `504b50e`
- ✅ Sprint 22 done (LOCAL) — `76e857d`
- ✅ Sprint 23 done (LOCAL) — `a6998c6` + `999f913` + `597dfd1`
- ✅ Sprint 24 done (LOCAL) — `5c54632` + `6b2d099` + (Sprint 24 commits to be added)
- ⏳ Awaiting Anas "ادفع" → Mode 2 push (relax → PR → CI 6/6 → squash-merge --admin → tag → restore)
- 📋 **Sprint 25 plan (post-push):**
  - P1 items above (in order of demo impact: Trial Balance UI → Statement endpoints → Posting Rules unit tests)
  - P2: 5th VAT rule + audit trail
  - Carry-over tracking: any new Article 3 violations found by the pre-push audit

---

_Last updated: 2026-08-02 by Mavis (Muhammad mode)_
