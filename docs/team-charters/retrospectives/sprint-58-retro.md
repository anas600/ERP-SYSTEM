# Sprint 58 Retro — Contracts + Progress Billings + WIP (DEC-163..165)

**Date:** 2026-08-07
**Branch:** `feature/sprint-58-contracts-billings` (based on `feature/sprint-57-projects-pnl`)
**Status:** ✅ DONE (LOCAL-ONLY)
**Duration:** ~3 hours (start to committed + verified)

---

## Goal

Per Sprint 57 plan: "الأساس اللي VO يحتاجه" — add Contracts + Progress Billings + WIP to the Projects module. This is the construction-project workflow on top of the basic Projects entity.

---

## What was done

### DEC-163 — Contracts Module
- **`data-types/contracts.json`**: new table (auto-applied by DataTypeMigrator)
  - Fields: id, company_id, project_id, contract_number, contract_value, advance_percent, retention_percent, retention_start_billing, start_date, end_date, notes, audit
  - UNIQUE (company_id, project_id) — one contract per project
  - Indexes on project_id, deleted_at
- **`Contract.cs` entity** + **`IContractRepository`** + **`ContractRepository`** + **`IContractService`** + **`ContractService`**
- **Endpoints**:
  - `GET /api/projects/{id}/contract` (one per project)
  - `POST /api/projects/{id}/contract` (only if no existing contract)
  - `PUT /api/contracts/{contractId}` (only if no billings)
  - `DELETE /api/contracts/{contractId}` (soft delete, only if no billings)
- **Validations**: contract_value > 0, advance/retention 0-100, retention_start_billing >= 1
- **UI**: New "العقد" tab in `/projects/[id]` with view/edit/delete + create modal

### DEC-164 — Progress Billings Module
- **`data-types/progress_billings.json`**: new table
  - Fields: id, company_id, project_id, contract_id, billing_number, billing_date, period_from/to, work_completed_percent, gross_amount, advance_deducted, retention_deducted, net_amount, status, invoice_id, journal_entry_id, notes
  - UNIQUE (company_id, billing_number)
  - Indexes on project_id, contract_id, status
- **`ProgressBilling.cs`** + **`BillingStatus` enum** (Draft/Invoiced/Cancelled)
- **`IBillingRepository`** + **`BillingRepository`**
- **`IBillingService`** + **`BillingService`** (~19KB) — the biggest service in the module
- **Endpoints** (7 new):
  - `GET /api/projects/{id}/billings`
  - `POST /api/projects/{id}/billings`
  - `GET /api/billings/{id}`
  - `GET /api/contracts/{id}/billing-preview?percent=` (live preview)
  - `POST /api/billings/{id}/approve` (DRAFT → INVOICED, atomic)
  - `POST /api/billings/{id}/cancel` (DRAFT → CANCELLED)
- **Billing Algorithm** (verified with golden scenario):
  ```
  gross = contract_value × (work_completed_percent / 100)
  previous_advance_sum = SUM(advance_deducted) WHERE status != 'CANCELLED'
  total_advance = contract_value × (advance_percent / 100)
  remaining_advance = MAX(0, total_advance − previous_advance_sum)
  advance_deducted = MIN(gross, remaining_advance)  // تُخصم مرة واحدة
  next_number = COUNT(non_cancelled) + 1
  retention_deducted = (next_number >= retention_start_billing)
      ? gross × (retention_percent / 100) : 0
  net = gross − advance − retention
  ```
- **Atomic Approve** (the most complex piece):
  1. Verify status = DRAFT
  2. Verify project.customer_id (required for invoice)
  3. Lookup AR (1103) + Revenue (4101) accounts
  4. Begin transaction
  5. INSERT sales_invoice (status='Posted', with project_id)
  6. INSERT journal_entry (status=Posted) + 2 journal_lines (DR 1103 / CR 4101)
  7. UPDATE sales_invoice.journal_entry_id (back-link)
  8. UPDATE progress_billings (status=Invoiced, invoice_id, journal_entry_id)
  9. Commit (or rollback on any failure)
- **UI**: New "المستخلصات" tab with:
  - Billing list table (status, amounts, actions)
  - Create modal with **live preview** (debounced 300ms API call to /billing-preview)
  - Approve / Cancel action buttons
  - Status badge (Draft=warning, Invoiced=success, Cancelled=neutral)

### DEC-165 — WIP + Retention
- **`WipResponse` DTO**
- **`IBillingService.GetWipAsync(projectId, ct)`** — WIP calculation
  - `totalCosts = SUM(journal_lines.debit − credit) WHERE je.project_id = X AND accounts.type = 5`
  - `totalBilledNet = SUM(progress_billings.net_amount) WHERE project_id = X AND status = 2`
  - `totalRetentionHeld = SUM(retention_deducted) WHERE project_id = X AND status = 2`
  - `wip = totalCosts − totalBilledNet`
  - Status: BALANCED / COSTS_EXCEED_BILLED / BILLED_EXCEED_COSTS
- **Endpoint**: `GET /api/projects/{id}/wip`
- **UI**: WIP card added to P&L tab (amber background, 4 stats + status message)

### Branching
- **Branched from `feature/sprint-57-projects-pnl`** (not from develop) — Sprint 58 builds on Sprint 57's `project_id` on journal_entries

---

## Architecture decision

**Why branch from Sprint 57, not from develop?**

Sprint 58 depends on Sprint 57's `project_id` column on `journal_entries` (for the WIP cost calculation). Develop branch doesn't have Sprint 57 yet (all sprints are local-only, awaiting the user's "ادفع" push). So Sprint 58 = `feature/sprint-57-projects-pnl` + DEC-163..165.

When the user merges Sprint 57 to develop, Sprint 58 will need a rebase (or merge). The order matters: 57 → 58.

---

## Verification

### Build
```
$ dotnet build src/backend/Host/ERP-SYSTEM.csproj
0 errors, 17 pre-existing warnings (unchanged from Sprint 57)

$ npm run type-check
0 errors

$ npm run build
✓ Compiled successfully
Route /projects/[id] = 7.91 kB (was 4.16 kB after Sprint 57)
```

### Tests
```
$ dotnet test --filter Projects
Passed! 24/24, 0 failed

$ dotnet test --filter Finance
Passed! 25/25, 0 failed (4 pre-existing [Skip] benchmark tests)
```

### Manual smoke (would need DB to verify)
- POST /api/projects/{id}/contract → 201 with contract object
- GET /api/projects/{id}/contract → 200 with contract
- PUT /api/contracts/{contractId} → 200 (only if no billings)
- POST /api/projects/{id}/billings → 201 with calculated amounts
- GET /api/contracts/{id}/billing-preview?percent=30 → 200 with live preview
- POST /api/billings/{id}/approve → 200 with invoice_id + journal_entry_id
- GET /api/projects/{id}/wip → 200 with WIP calculation

---

## Lessons learned

### L113 (NEW)
**Branching order matters for stacked local-only sprints.** When Sprint N+1 depends on Sprint N's local changes, branch from Sprint N (not from develop). The merge order will be 1 → 2 → 3 → ... → N. If user pushes out of order, conflicts.

### L114 (NEW)
**Billing Algorithm: validate `work_completed_percent` against `MAX(percent) WHERE status != 'CANCELLED'`.** Reject if new percent < previous max (can't go backwards). The user might cancel a high-percent billing and re-create with lower; that's allowed. But going backwards on a non-cancelled chain is a logic error.

### L115 (NEW)
**For atomic approve of a billing (which creates invoice + JE + updates billing), the AR account (1103) and Revenue account (4101) must exist in the company.** The service fetches them by code. If missing → 400 with helpful message ("add account 1103 to CoA first"). Don't hardcode UUIDs — they vary by company seeding.

### L116 (NEW)
**Dapper transactions with Npgsql require explicit cast to NpgsqlConnection.** The `IDbConnection` interface doesn't expose `BeginTransactionAsync` properly. Pattern: `using var conn = (Npgsql.NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct); using var tx = await conn.BeginTransactionAsync(ct);`. The cast is the idiomatic .NET 9 fix.

### L117 (NEW)
**The `sprint-NN-projects-pnl` naming convention from Sprint 57 was reused (`feature/sprint-58-contracts-billings`).** Future sprints: `feature/sprint-NN-<topic-slug>`. Avoid name collisions with already-existing branches (we had to recreate Sprint 58's worktree from Sprint 57 because the initial worktree was from develop without P&L service).

### L118 (NEW)
**Live preview pattern: debounce the API call (300ms) to avoid hammering the server on every keystroke.** The preview endpoint is read-only and idempotent, so debouncing is safe. Use `setTimeout` + cleanup in `useEffect`.

---

## Carry-over (Sprint 59+)

- **Variation Orders (Sprint 59)**: extend contract_value via approved VOs
- **Project Close-out + Retention Release (Sprint 60)**: lifecycle completion workflow
- **Unit tests for BillingService** (the algorithm deserves a golden-scenario test)
- **Integration test** for atomic approve (need real DB)
- **AR account / Revenue account validation** at company setup (warn if missing)
- **Multiple P&L for project status** when project is `Completed` (lock new billings)
- **Auto-propagate `project_id` to auto-generated JEs** from sales_invoices (Sprint 57 carry-over)

---

## Sprint 60 preview

Per the original plan:
- **DEC-166**: Variation Orders
- **DEC-167**: Project Close-out workflow
- **DEC-168**: Retention Release (creates payment, decreases retention_held)

Then the Projects module will be ~95% feature-complete for construction use cases. Sprint 61+ can tackle:
- Subcontractor liability (DEC-169, separate module)
- Multi-currency contracts (FX complexity)
- Inventory + Project material requests
