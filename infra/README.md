# Infrastructure Scripts

This directory contains infrastructure automation scripts.

## Backup Pipeline (DEC-051)

| Script | Purpose |
|---|---|
| `scripts/pg-dump.sh` | Dump PostgreSQL to local .sql.gz |
| `scripts/r2-upload.sh` | Upload to Cloudflare R2 with SHA256 + 30-day rotation |
| `scripts/restore-from-r2.sh` | DR restore (interactive) |
| **`scripts/verify-backup.sh`** | **Weekly backup verification (DEC-051 P2)** |

## Backup Verification (DEC-051 P2)

`scripts/verify-backup.sh` validates that nightly backups are restorable and complete.

**Usage**: See [docs/runbooks/backup-verification.md](../docs/runbooks/backup-verification.md)

**What it does**:
1. Downloads latest backup from R2 (or accepts local file)
2. Restores into a temp schema in Supabase
3. Validates table count, key tables, row counts, schema
4. Reports OK or list of issues
5. Cleans up the temp schema

**Schedule**: Weekly GitHub Action (Sunday 05:00 UTC).

## Docker

This directory also contains Docker configurations for the application.
