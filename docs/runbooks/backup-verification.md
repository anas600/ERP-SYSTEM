# DEC-051 P2: Backup Verification Runbook

**Last Updated**: 2026-07-22
**Status**: P2 complete (DL-170-172)

## What This Does

`infra/scripts/verify-backup.sh` automatically validates nightly backups by:
1. **Downloading** the latest backup from R2 (or using a local file)
2. **Restoring** the dump into a temp schema in Supabase
3. **Validating** schema integrity (table count, key tables, columns)
4. **Reporting** OK or list of issues
5. **Cleaning up** the temp schema

## When to Use

| Scenario | Action |
|---|---|
| After nightly backup | Automatic (GitHub Action, Sunday 05:00 UTC) |
| Before major deploy | Run manually |
| After suspected DB issue | Run manually |
| Quarterly DR drill | Run manually + restore to test env |

## Quick Start (Manual)

```bash
# Default: download latest from R2 + verify
export SUPABASE_URL="postgresql://..."
export R2_ACCESS_KEY="..."
export R2_SECRET_KEY="..."
export R2_ENDPOINT="https://..."
export R2_BUCKET="erp-system-backups"
bash infra/scripts/verify-backup.sh
```

```bash
# Verify a local file (skip R2)
export SUPABASE_URL="..."
export BACKUP_FILE="/tmp/erp-backup-20260722-020000.sql.gz"
bash infra/scripts/verify-backup.sh
```

```bash
# DRY_RUN mode (only verify file integrity, no actual restore)
DRY_RUN=1 bash infra/scripts/verify-backup.sh
```

## Exit Codes

| Code | Meaning |
|---|---|
| 0 | ✅ All checks passed |
| 1 | ❌ One or more checks failed |
| 2 | ⚠️  Setup error (missing env, missing deps) |

## What Gets Checked

| # | Check | Pass Criteria |
|---|---|---|
| 1 | Table count | ≥ 30 tables |
| 2 | Key tables exist | tenants, users, roles, companies, accounts, journal_entries, items |
| 3 | Row counts | Sample tables have rows (informational) |
| 4 | Schema integrity | Key columns exist (users.email, tenants.code, etc.) |

## Output Example

```
[2026-07-22 05:00:00 UTC] DEC-051 P2 — Backup Verification
[2026-07-22 05:00:00 UTC] Mode: LIVE
[2026-07-22 05:00:00 UTC] ============================================
[2026-07-22 05:00:00 UTC] Latest: backups/erp-backup-20260722-020000.sql.gz
[2026-07-22 05:00:00 UTC] Downloaded to /tmp/verify-backup_xxx.sql.gz
[2026-07-22 05:00:00 UTC] Backup file size: 5242880 bytes
[2026-07-22 05:00:00 UTC] ✅ Backup file is valid gzip
[2026-07-22 05:00:00 UTC] Creating temp schema: backup_verify_1721619600_12345
[2026-07-22 05:00:00 UTC] Restoring backup into backup_verify_xxx
[2026-07-22 05:00:00 UTC] ✅ Restore completed
[2026-07-22 05:00:00 UTC] === Validation Queries ===
[2026-07-22 05:00:00 UTC] [1/4] Counting tables in backup_verify_xxx
[2026-07-22 05:00:00 UTC]   Tables in backup: 35
[2026-07-22 05:00:00 UTC]   ✅ PASS
[2026-07-22 05:00:00 UTC] [2/4] Checking key tables exist...
[2026-07-22 05:00:00 UTC]   ✅ tenants
[2026-07-22 05:00:00 UTC]   ✅ users
[2026-07-22 05:00:00 UTC]   ✅ roles
[2026-07-22 05:00:00 UTC]   ✅ companies
[2026-07-22 05:00:00 UTC]   ✅ accounts
[2026-07-22 05:00:00 UTC]   ✅ journal_entries
[2026-07-22 05:00:00 UTC]   ✅ items
[2026-07-22 05:00:00 UTC] [3/4] Row counts (sanity check)...
[2026-07-22 05:00:00 UTC]   tenants: 4 rows
[2026-07-22 05:00:00 UTC]   users: 5 rows
[2026-07-22 05:00:00 UTC]   companies: 6 rows
[2026-07-22 05:00:00 UTC]   items: 12 rows
[2026-07-22 05:00:00 UTC]   roles: 4 rows
[2026-07-22 05:00:00 UTC] [4/4] Schema integrity check...
[2026-07-22 05:00:00 UTC]   ✅ users.email
[2026-07-22 05:00:00 UTC]   ✅ tenants.code
[2026-07-22 05:00:00 UTC]   ✅ companies.code
[2026-07-22 05:00:00 UTC]   ✅ items.code
[2026-07-22 05:00:00 UTC] Cleaning up: DROP SCHEMA backup_verify_xxx CASCADE
[2026-07-22 05:00:00 UTC] ============================================
[2026-07-22 05:00:00 UTC] VERIFICATION SUMMARY
[2026-07-22 05:00:00 UTC] ============================================
[2026-07-22 05:00:00 UTC] Checks passed: 14
[2026-07-22 05:00:00 UTC] Issues found:  0
[2026-07-22 05:00:00 UTC] ============================================
[2026-07-22 05:00:00 UTC] ✅ VERIFICATION PASSED
```

## What to Do if It Fails

### Symptom: "Too few tables" or "MISSING: <table>"

**Root cause**: Backup is incomplete or corrupted.

**Action**:
1. Check the backup log (`/tmp/retention-cleanup.log` or nightly-backup workflow logs)
2. Try the previous backup:
   ```bash
   # List all backups
   aws s3 ls s3://$R2_BUCKET/backups/ --endpoint-url=$R2_ENDPOINT
   # Try the previous one
   BACKUP_FILE=/path/to/previous.sql.gz bash verify-backup.sh
   ```
3. If all backups fail, trigger fresh backup manually:
   ```bash
   bash scripts/pg-dump.sh
   ```

### Symptom: "Restore had warnings"

**Root cause**: DDL differences (e.g., missing GRANT statements).

**Action**:
1. Check the log: `tail -50 /tmp/backup-verify.log`
2. If warnings are about `permission denied for table`, run:
   ```sql
   GRANT ALL ON ALL TABLES IN SCHEMA backup_verify_xxx TO current_user;
   ```
3. Re-run verification

### Symptom: Connection timeout

**Root cause**: Network issues or Supabase maintenance.

**Action**:
1. Check Supabase status: https://status.supabase.com
2. Retry in 5 minutes
3. If persistent, check network: `ping aws-0-eu-central-1.pooler.supabase.com`

## Performance

- Download from R2: ~30s for 5MB backup
- Restore: ~1-2 min for 5MB
- Validation queries: ~5s
- Cleanup: ~5s
- **Total**: ~3 min per run

## Cost

- R2 download: FREE (no egress charge)
- Supabase temp schema: FREE (no extra compute)
- GitHub Action: 3 min × weekly = ~12 min/month (free tier = 2000 min)

## GitHub Action

`.github/workflows/backup-verify.yml`:
- Schedule: Sunday 05:00 UTC
- Manual trigger with `dry_run` input
- 90-day log retention
- Telegram notification on success/failure

## Required GitHub Secrets

| Secret | Purpose |
|---|---|
| `SUPABASE_URL` | PostgreSQL connection |
| `R2_ACCESS_KEY` | R2 download |
| `R2_SECRET_KEY` | R2 download |
| `R2_ENDPOINT` | R2 endpoint URL |
| `R2_BUCKET` | Bucket name |
| `TG_BOT_TOKEN` | (optional) Telegram notify |
| `TG_CHAT_ID` | (optional) Telegram chat |

## Defense Layers (DEC-051 P2)

- **DL-170**: Verify script
- **DL-171**: Weekly GitHub Action
- **DL-172**: Verification runbook

## Related Documents

- `docs/dec-051/DEC-051-README.md` — Backup + DR overview
- `scripts/pg-dump.sh` — Backup creation
- `scripts/r2-upload.sh` — R2 upload
- `scripts/restore-from-r2.sh` — Full restore (not just verify)
