# DEC-052 P2: Tier 1 Warm Storage + Tier 2 Archive + Reporting

**Date**: 2026-07-22
**Status**: P2 complete (5 commits in 1 PR)
**Defense Layers**: DL-148 to DL-155

> ⚠️ **Historical document** — pre-Phase 6 multi-tenant model. Some references to the obsolete `tenant`/`subdomain`/`ITenantContext` model are preserved for context. See `CONSTITUTION.md` Article 3 for the current Multi-Company model.

## What P2 Adds

DEC-052 P2 builds on P1 (which was just cleanup scripts + indexes) with:

| Feature | What | Why |
|---|---|---|
| **T1 Warm** | audit_log partitioned by year | Fast cleanup (DROP PARTITION), tiered storage |
| **T2 Archive** | JSONL.gz export to R2 with metadata tracking | Move cold data out of hot DB |
| **T2 Metadata** | `archive_metadata` table | Audit trail of what was archived where |
| **Monthly Report** | CSV auto-generated on 1st of month | Compliance reporting |
| **archived_at columns** | Mark records as archived (not deleted) | Audit + DR capability |

## Architecture (P2)

```
┌─────────────────────────────────────────────────────┐
│ Supabase PostgreSQL (Hot)                           │
│                                                     │
│ audit_log (partitioned by year)                     │
│   ├── audit_log_y2024                                │
│   ├── audit_log_y2025                                │
│   ├── audit_log_y2026                                │
│   └── audit_log_default (fallback)                   │
│                                                     │
│ stock_movements (with archived_at)                  │
│   ├── Hot (< 3 years)                                │
│   └── Flagged (archived_at IS NOT NULL)              │
└─────────────────────────────────────────────────────┘
                        ↓ Tier 2 archive
┌─────────────────────────────────────────────────────┐
│ Cloudflare R2 (Cold)                                │
│                                                     │
│ archive/                                            │
│   ├── audit_log/                                     │
│   │   └── 2025/audit_log_20250722_030000.jsonl.gz     │
│   ├── stock_movements/                               │
│   │   └── 2024/stock_movements_20240722_030000...    │
│   └── _metadata.json (SHA256 + record counts)        │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ archive_metadata table (PostgreSQL)                  │
│   - id, table_name, period_start/end                 │
│   - record_count, size_bytes, sha256                 │
│   - storage_path, archived_at, archived_by           │
└─────────────────────────────────────────────────────┘
```

## Files in P2 (5 commits)

### Commit 1: T1 Warm Storage (Migration 018)
- `src/backend/Shared/Migrations/20260722_130000_AddRetentionTier1Warm.cs`
- Converts `audit_log` to partitioned table (RANGE by `created_at`)
- Pre-creates 3 yearly partitions (current year + 2 ahead)
- Adds `archived_at` column to `stock_movements` and `audit_log`
- Indexes for `archived_at` for fast queries

### Commit 2: T2 Archive (Migration 019 + Script)
- `src/backend/Shared/Migrations/20260722_140000_AddArchiveMetadata.cs`
- New `archive_metadata` table for tracking archived batches
- `scripts/data-archive-t2.sh` — T2 archive script
  - Exports to JSONL.gz
  - Uploads to R2 (GLACIER storage class)
  - Marks records with `archived_at`
  - Records in `archive_metadata`

### Commit 3: Monthly Report (Script + Workflow)
- `scripts/retention-report.sh` — CSV report generator
- `.github/workflows/retention-monthly.yml` — 1st of month at 05:00 UTC
- Output: `/tmp/retention-report-YYYY-MM.csv`
- Counts: total, oldest, archived, tier breakdown, size

### Commit 4: Tests
- `src/backend/Tests/ERPSystem.Tests/Retention/RetentionTests.cs`
- Tests:
  - Partitioned table accepts inserts
  - archive_metadata insert/query
  - Retention periods are valid
  - Archive thresholds match spec
- Category: `Retention`

### Commit 5: Docs (this file)
- `docs/dec-052/DEC-052-P2-README.md`
- Architecture, files, schedule, DLs

## Schedule (Combined with P1)

| Time (UTC) | Action | Source |
|---|---|---|
| 02:00 | Daily backup → R2 | DEC-051 (PR #110) |
| 03:00 | Tier 1 cleanup | DEC-052 P1 (PR #111) |
| **04:00** | **Tier 2 archive** (move old data → R2) | **DEC-052 P2 (this PR)** |
| **05:00 1st/month** | **Monthly retention report** | **DEC-052 P2 (this PR)** |

## Retention Thresholds

| Table | Tier 0 (Hot) | Tier 1 (Warm) | Tier 2 (Archive) | Tier 3 (Purge) |
|---|---|---|---|---|
| `audit_log` | 12 mo | 1-7y (partitioned) | R2 (after 1y) | Never (legal) |
| `stock_movements` | 3y | — | R2 (after 3y) | Never |
| `journal_entries` | 7y (live) | — | — | Never (IFRS) |
| `notifications` | 90 days | — | — | After 90d |
| `outbox/processed` | 30 days | — | — | After 30d |

## Archive Format

**Path**: `archive/{table}/YYYY/{filename}.jsonl.gz`
**Example**: `archive/audit_log/2025/audit_log_20250722_030000.jsonl.gz`
**Compression**: gzip level 9
**Format**: JSONL (one JSON object per line)
**Size**: ~10K records → ~5-10 MB compressed

## Metadata Tracking

```sql
SELECT * FROM archive_metadata
WHERE table_name = 'audit_log'
ORDER BY archived_at DESC
LIMIT 5;
```

Returns:
- `period_start`, `period_end` — date range archived
- `record_count` — number of records
- `size_bytes` — compressed size
- `sha256` — integrity hash
- `storage_path` — R2 key
- `archived_at`, `archived_by` — when + who

## Disaster Recovery

To restore archived data:
1. Download from R2: `s3 cp s3://erp-system-archive/archive/audit_log/2025/...`
2. Decompress: `gunzip file.jsonl.gz`
3. Parse JSONL: `jq -c '.id' file.jsonl`
4. Insert back to DB (or query in place)

## Defense Layers (P2)

- **DL-148**: T1 partitioning migration
- **DL-149**: archive_metadata table
- **DL-150**: T2 archive script
- **DL-151**: Monthly retention report script
- **DL-152**: Monthly retention report cron
- **DL-153**: Retention tests
- **DL-154**: archived_at indexes
- **DL-155**: P2 documentation

## Total DEC-052 Defense Layers: DL-139 to DL-155 (17 layers)

## Open Items (P3)

- T3 purge automation (with legal hold check)
- GDPR endpoint (DELETE /api/users/{id})
- Data anonymization (vs hard delete)
- Per-tenant retention override
- Tier 1 warm auto-partitioning cron (currently manual yearly)

## Migration Notes

For existing prod data:
1. Apply migration 018 → audit_log becomes partitioned (data preserved)
2. Apply migration 019 → archive_metadata table created
3. Run T2 archive for first time:
   - Will export ~1 year of audit_log (if exists)
   - May take several minutes for large datasets
4. Monthly report will start appearing from 1st of next month
