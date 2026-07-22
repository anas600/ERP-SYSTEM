# DEC-052 P3: Soft Delete Pattern

**Date**: 2026-07-22
**Status**: P3 complete (4 commits in 1 PR)
**Defense Layers**: DL-173 to DL-176

## What P3 Adds

DEC-052 P3 implements a soft delete pattern for financial tables:
- Records are marked deleted, not physically removed
- Easy to recover (restore endpoint)
- Audit trail (who deleted, when)
- Tenant isolation

## Tables Covered (4)

- `sales_invoices` (financial)
- `payments` (financial)
- `journal_entries` (financial)
- `users` (admin)

Other tables (`vendors`, `customers`) already have `deleted_at` from DEC-082.

## Schema Changes

Each table gets 3 new columns:

| Column | Type | Default | Purpose |
|---|---|---|---|
| `is_deleted` | BOOLEAN | FALSE | Fast filter (indexed) |
| `deleted_at` | TIMESTAMPTZ | NULL | When deleted |
| `deleted_by` | UUID | NULL | Who deleted |

Plus index `ix_{table}_is_deleted` on `(is_deleted)` for fast filtering.

## API Endpoints

All under `/api/soft-delete/`:

| Method | Path | Purpose |
|---|---|---|
| `DELETE` | `/{table}/{id}` | Soft delete (set is_deleted=true) |
| `POST` | `/{table}/{id}/restore` | Restore (clear is_deleted) |
| `GET` | `/{table}/deleted` | List deleted records (admin) |

**Whitelist**: `sales_invoices`, `payments`, `journal_entries`, `users` only.

**Auth**: `WriteMasterData` policy (Admin only).

**Tenant isolation**: All queries filter by `tenant_id`.

## Why a Generic Controller?

Centralized soft-delete logic = single audit trail, consistent behavior.

Instead of adding DELETE methods to 4 controllers, one generic endpoint handles all 4 tables.

## Trade-offs

| Aspect | Pros | Cons |
|---|---|---|
| Soft delete | Recoverable, auditable | Tables grow over time |
| Generic controller | DRY, single audit | Less type safety |
| Whitelist | Security | Manual list maintenance |

## Files (4)

### Migration
- `src/backend/Shared/Migrations/20260722_150000_AddSoftDeleteColumns.cs` (NEW, 66 lines)
- Idempotent (DO $$ ... END $$ per DEC-052 P2.6 lesson)
- Creates index per table

### Controller
- `src/backend/Host/Controllers/SoftDeleteController.cs` (NEW, 114 lines)
- DELETE / POST / GET endpoints
- Whitelist of 4 tables
- Tenant + audit fields

### Tests
- `src/backend/Tests/ERPSystem.Tests/SoftDelete/SoftDeleteTests.cs` (NEW, 95 lines)
- 8 test cases
- Whitelist + SQL injection + tenant isolation

### Docs (this file)
- `docs/dec-052/DEC-052-P3-README.md`

## Usage Examples

```bash
# Soft delete an invoice
curl -X DELETE https://app/api/soft-delete/sales_invoices/12345 \
  -H "Authorization: Bearer $TOKEN"
# → 200 OK, { id, table, deleted_at, deleted_by }

# Restore it
curl -X POST https://app/api/soft-delete/sales_invoices/12345/restore \
  -H "Authorization: Bearer $TOKEN"
# → 200 OK, { id, table, restored_at }

# List all deleted invoices
curl https://app/api/soft-delete/sales_invoices/deleted \
  -H "Authorization: Bearer $TOKEN"
# → [{ id, deleted_at, deleted_by }, ...]
```

## Application Code

The existing services still work (repos don't change). But to **filter out deleted records** in normal queries, services need to add `WHERE is_deleted = FALSE`:

```csharp
// Before
var list = await _invoices.ListAsync(tenantId, ...);

// After (if you want to filter out deleted)
var list = await _invoices.ListAsync(tenantId, ...);
// Add filter in repo or service
```

For now, the soft-delete is **additive**: existing queries still return all records. To filter, add `is_deleted = FALSE` to the SQL.

## Open Items (P4/P5)

- **P4**: Update existing services to filter `is_deleted = FALSE` by default
- **P5**: Add `Show deleted` toggle to admin frontend pages
- **P5**: Hard delete (admin only, with audit) — separate endpoint
- **P5**: GDPR endpoint (right-to-be-forgotten) — uses hard delete + audit

## Defense Layers

- **DL-173**: Soft delete migration
- **DL-174**: Generic SoftDeleteController
- **DL-175**: Soft delete tests
- **DL-176**: P3 documentation

## Total DEC-052 Defense Layers: DL-139 to DL-176 (38 layers across P1, P2, P3)
