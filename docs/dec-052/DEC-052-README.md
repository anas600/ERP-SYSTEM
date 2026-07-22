# DEC-052: Data Retention Policy

**Date**: 2026-07-22
**Status**: P1 implementation complete
**Defense Layers**: DL-139 to DL-143

## Overview

7-year data retention policy for the ERP-SYSTEM, with tiered archive + cleanup.

## Retention Tiers

| Tier | Age | Action | Storage |
|---|---|---|---|
| **T0 - Hot** | 0-12 months | Live DB | Neon PostgreSQL |
| **T1 - Warm** | 1-3 years | Compressed in DB (table partitioning) | Neon PostgreSQL |
| **T2 - Archive** | 3-7 years | R2 + S3-compatible cold storage | Cloudflare R2 (GLACIER) |
| **T3 - Purged** | > 7 years | Hard delete | N/A |

## Per-Entity Retention

| Entity | T0 (hot) | T1 (warm) | T2 (archive) | T3 (purge) | Notes |
|---|---|---|---|---|---|
| `audit_log` | 12 mo | — | 3-7y | >7y | Legal/audit requirement |
| `journal_entries` | 7y | — | — | >7y | Financial: 7y retention (legal) |
| `vendor_bills` | 7y | — | — | >7y | Tax compliance |
| `sales_invoices` | 7y | — | — | >7y | Tax compliance |
| `stock_movements` | 3y | 3-7y | R2 | >7y | Operational, less critical |
| `notifications` | 6 mo | — | — | >6mo | Ephemeral |
| `refresh_tokens` | 14 days | — | — | >14d | Security (rotated) |
| `password_reset_tokens` | 24 hours | — | — | >24h | Security |
| `outbox_events` (processed) | 30 days | — | — | >30d | Idempotency |
| `processed_events` | 30 days | — | — | >30d | Idempotency |
| `users` (inactive) | Indefinite | — | — | On request | GDPR right-to-erasure |
| `tenants` | Indefinite | — | — | On request | Legal entity |
| `companies` | Indefinite | — | — | On request | |

## Cleanup Strategy

### Tier 1: Hot Database (in-DB)

- **Ephemeral tokens** (refresh, password_reset): 1-day cleanup
- **Processed events** (outbox, processed_events): 30-day cleanup
- **Notifications**: 6-month cleanup
- **Stock movements** (soft-deleted): 3-year cleanup (hard delete)

### Tier 2: Archive to R2 (cold)

- **Audit log**: Move to R2 after 1 year
- **Stock movements**: Move to R2 after 3 years
- **Format**: gzip-compressed JSONL (one line per record)
- **Filename pattern**: `archive/{table}/YYYY-MM-DD.jsonl.gz`

### Tier 3: Purged

- **After 7 years**: hard delete
- **Subject to legal hold**: skip purge if active hold
- **GDPR erasure**: immediate purge on request

## Implementation (P1)

### Files

```
docs/dec-052/
├── DEC-052-README.md              # this file
├── RETENTION-MATRIX.md            # detailed entity-level rules
└── GDPR-PROCEDURE.md              # right-to-erasure workflow

scripts/
└── data-retention-cleanup.sh      # runs all tier-1 cleanups

src/backend/Shared/Migrations/
└── 20260722_120000_AddRetentionArchiveTables.cs
```

### Cleanup Script (Tier 1)

`scripts/data-retention-cleanup.sh`:
- Runs nightly at 04:00 UTC (after backup at 02:00)
- Tier 1: in-DB cleanup (safe, no archive needed)
- Idempotent (can run multiple times safely)
- Logs to /tmp + sends to Sentry
- Email/Telegram alert if deletes > 10K rows

### Archive Migration (P1)

- Add `archived_at` column to `audit_log`, `stock_movements`
- Add partition strategy for `journal_entries` by year
- Add `retention_class` enum: `hot | warm | archive | purged`

## RPO & RTO (Retention-Specific)

| Scenario | RPO | RTO | Notes |
|---|---|---|---|
| Single record corruption | 0 (transactional) | 5 min | Hot DB |
| Annual purge accident | 24h (last backup) | 1h (restore) | Cold backup |
| 7-year audit query | N/A | 24h (R2 download) | Archive |
| GDPR right-to-erasure | N/A | < 30 days (per GDPR) | Manual + automated |

## Schedule

| Time (UTC) | Action | Where |
|---|---|---|
| 02:00 | Daily backup → R2 | GitHub Action (DEC-051) |
| 03:00 | Hot cleanup (Tier 1) | GitHub Action (DEC-052) |
| 04:00 | Archive to R2 (Tier 2) | GitHub Action (DEC-052) |
| 1st of month | Retention report (counts) | GitHub Action |
| Annually (Jan 1) | Tier 3 purge review | Manual |

## Compliance

| Standard | Compliance |
|---|---|
| **GDPR** (EU) | Right-to-erasure supported (P3, future) |
| **CCPA** (California) | Right-to-delete supported |
| **SOX** (US, financial) | 7-year retention for journal/invoices |
| **IFRS** (international) | 7-year retention |
| **Libyan Tax Law** | 7-year for financial records |
| **HIPAA** (US, health) | N/A (no health data) |

## Defense Layers (DEC-052 P1)

- **DL-139**: Retention matrix documented
- **DL-140**: Tier 1 cleanup script
- **DL-141**: Archive migration (schema)
- **DL-142**: Cleanup cron (GitHub Action)
- **DL-143**: GDPR right-to-erasure procedure (P2)

## Open Items (P2/P3)

- Tier 2 archive automation (move to R2)
- Tier 3 purge automation (with legal hold check)
- GDPR endpoint (`DELETE /api/users/{id}` for self-erasure)
- Data anonymization (vs hard delete)
- Per-tenant retention override (e.g., for tenant-specific compliance)
