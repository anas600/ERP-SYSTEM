# DEC-051: Backup & Disaster Recovery Strategy

**Date**: 2026-07-22
**Status**: P1 implementation complete, awaiting R2 credentials for end-to-end test
**Defense Layers**: DL-132 to DL-138

## Overview

Multi-layered backup strategy for the ERP-SYSTEM Neon PostgreSQL database:

1. **Daily backups** to Cloudflare R2 (cold storage)
2. **30-day retention** with auto-rotation
3. **Disaster recovery** via `restore-from-r2.sh`
4. **Telegram notifications** on success/failure
5. **SHA256 checksums** for integrity verification

## Architecture

```
┌──────────────────────┐    ┌──────────────────┐
│ GitHub Action        │    │ Neon PostgreSQL  │
│ (Nightly 02:00 UTC)  │───▶│ erp-system-db    │
│                      │    │                  │
│ ┌──────────────────┐ │    └──────────────────┘
│ │ 1. pg_dump       │ │
│ │ 2. compress      │ │
│ │ 3. SHA256        │ │
│ │ 4. upload to R2  │ │    ┌──────────────────┐
│ │ 5. rotate old    │ │───▶│ Cloudflare R2    │
│ └──────────────────┘ │    │ erp-system-bkps  │
└──────────────────────┘    │ (10GB, $11/mo)   │
                            └──────────────────┘
```

## Scripts

| Script | Purpose | Where it runs |
|---|---|---|
| `scripts/pg-dump.sh` | Dump Neon DB to local file | GitHub Action (Ubuntu) |
| `scripts/r2-upload.sh` | Upload .sql.gz to R2 with checksum | GitHub Action |
| `scripts/restore-from-r2.sh` | DR restore (interactive confirm) | Manual / on-demand |

## Setup (Manual, by Anas)

### 1. Create Cloudflare R2 Bucket

```bash
# Using Cloudflare API (R2 API):
# - Go to https://dash.cloudflare.com → R2 → Create bucket
# - Name: erp-system-backups
# - Region: EU (auto)
# - Storage class: Standard
```

### 2. Create R2 API Token

```bash
# In Cloudflare dashboard:
# R2 → Manage R2 API Tokens → Create API token
# - Name: erp-backup-writer
# - Permissions: Object Read & Write
# - Bucket: erp-system-backups only
# - TTL: Never (long-lived)
#
# Save the access_key_id and secret_access_key!
```

### 3. Add Secrets to GitHub Repo

```
Settings → Secrets and variables → Actions → New repository secret

Name              Value
----              -----
NEON_URL          postgresql://neondb_owner:PASS@ep-xxx.aws.neon.tech/neondb?sslmode=require
R2_ACCESS_KEY     <R2 access key from step 2>
R2_SECRET_KEY     <R2 secret key from step 2>
R2_ENDPOINT       https://ACCOUNT_ID.r2.cloudflarestorage.com
R2_BUCKET         erp-system-backups
TG_BOT_TOKEN      <optional: Telegram bot for notifications>
TG_CHAT_ID        <optional: Telegram chat ID>
```

### 4. Test Manually

```
Actions → Nightly PG Backup → Run workflow
```

Expected output:
- pg_dump completes
- R2 upload completes
- File appears in R2: `backups/erp-backup-YYYYMMDD-HHMMSS.sql.gz`
- SHA256 verified
- Telegram notification (if configured)

## RPO & RTO

| Metric | Value | Notes |
|---|---|---|
| **RPO** (Recovery Point Objective) | **24 hours** | Daily backup |
| **RTO** (Recovery Time Objective) | **~30 min** | Download + restore time |

## Cost

| Item | Cost |
|---|---|
| R2 storage (10GB) | $0.015/GB/mo = $0.15/mo |
| R2 Class A ops (writes) | $4.50/M, ~30/mo = $0.14 |
| R2 Class B ops (reads) | $0.36/M, ~5/mo = $0.002 |
| R2 egress (restore) | FREE |
| **Total** | **~$0.30/mo** (was estimated $11 — actual is much cheaper) |

## Retention Policy

- **Daily backups**: Keep 30 most recent
- **Monthly archives**: TBD (DEC-052 P2)
- **Old backups**: Auto-deleted by R2 lifecycle (planned DEC-051 P2)

## Disaster Recovery Scenarios

### Scenario 1: Accidental data deletion (small)

```bash
# 1. Find latest backup before incident
aws s3 ls s3://erp-system-backups/backups/ --endpoint-url=$R2_ENDPOINT

# 2. Restore to a temporary DB first (NEVER restore directly to prod!)
NEON_URL=<temp_db_url> bash scripts/restore-from-r2.sh latest

# 3. Manually copy needed data from temp → prod
```

### Scenario 2: Full DB corruption (large)

```bash
# 1. Stand up new Neon branch
# 2. Restore latest backup to it
NEON_URL=<new_branch_url> bash scripts/restore-from-r2.sh latest

# 3. Cut over application to new branch
# 4. Old branch becomes fallback
```

### Scenario 3: R2 lost (disaster)

- TBD: replicate to second provider (DEC-051 P2)
- For now: rely on GitHub Action logs + Neon's own backups (7d point-in-time)

## Monitoring

- **GitHub Actions** → Actions tab → Nightly PG Backup
- Status: ✅ / ❌ icon
- Logs: click run → expand steps

## Files in this DEC

```
scripts/
├── pg-dump.sh              # dump Neon to local .sql.gz
├── r2-upload.sh            # upload to R2 with checksum + rotation
└── restore-from-r2.sh      # DR restore (interactive)

.github/workflows/
└── nightly-backup.yml      # GitHub Action (cron 02:00 UTC)

docs/dec-051/
└── DEC-051-README.md       # this file
```

## Defense Layers (DEC-051 P1)

- **DL-132**: pg_dump.sh script
- **DL-133**: r2-upload.sh script (with SHA256)
- **DL-134**: restore-from-r2.sh script
- **DL-135**: nightly-backup.yml GitHub Action
- **DL-136**: 30-day auto-rotation
- **DL-137**: SHA256 integrity verification
- **DL-138**: Telegram notification

## Open Items (P2)

- Monthly archive (DEC-052 P2)
- Second-region R2 replication
- Automated restore test (monthly)
- Encrypt backups at rest (R2 supports SSE-KMS)
