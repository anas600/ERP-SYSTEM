#!/bin/bash
# scripts/data-archive-t2.sh — DEC-052 P2: Tier 2 archive to R2 (cold storage)
#
# Moves records > 1 year (audit_log) or > 3 years (stock_movements) from hot DB to R2.
# Format: gzip-compressed JSONL, one line per record.
# Records in source DB are marked archived_at = NOW() (kept for reference, not deleted).
#
# Required env:
#   SUPABASE_URL or NEON_URL — PostgreSQL connection string
#   R2_ACCESS_KEY, R2_SECRET_KEY, R2_ENDPOINT, R2_BUCKET — R2 access
# Optional env:
#   DRY_RUN — 1=preview only, 0=archive (default 0)
#   TABLES — comma-separated list (default "audit_log,stock_movements")
#   AUDIT_LOG_AGE_DAYS — default 365 (1 year)
#   STOCK_MOVEMENTS_AGE_DAYS — default 1095 (3 years)
#   BATCH_SIZE — default 10000

set -euo pipefail

# Validate env
if [ -z "${SUPABASE_URL:-}${NEON_URL:-}" ]; then
  echo "ERROR: SUPABASE_URL or NEON_URL is required" >&2
  exit 1
fi
DB_URL="${SUPABASE_URL:-$NEON_URL}"

# R2 creds (optional for dry-run)
R2_READY=0
for var in R2_ACCESS_KEY R2_SECRET_KEY R2_ENDPOINT R2_BUCKET; do
  if [ -z "${!var:-}" ]; then
    R2_READY=0
    break
  fi
  R2_READY=1
done

DRY_RUN="${DRY_RUN:-0}"
TABLES="${TABLES:-audit_log,stock_movements}"
AUDIT_LOG_AGE_DAYS="${AUDIT_LOG_AGE_DAYS:-365}"
STOCK_MOVEMENTS_AGE_DAYS="${STOCK_MOVEMENTS_AGE_DAYS:-1095}"
BATCH_SIZE="${BATCH_SIZE:-10000}"
LOG_FILE="${LOG_FILE:-/tmp/data-archive-t2.log}"
TS=$(date -u +"%Y-%m-%d %H:%M:%S UTC")

log() {
  echo "[$TS] $*" | tee -a "$LOG_FILE"
}

# Install psql if missing
# Install psql if missing
if ! command -v psql >/dev/null 2>&1; then
  log "Installing postgresql-client..."
  if command -v apt-get >/dev/null 2>&1; then
    apt-get install -y postgresql-client >/dev/null 2>&1 || { log "ERROR: Failed to install postgresql-client (apt)"; exit 1; }
  elif command -v apk >/dev/null 2>&1; then
    apk add postgresql-client >/dev/null 2>&1 || { log "ERROR: Failed to install postgresql-client (apk)"; exit 1; }
  else
    log "ERROR: psql not installed and no package manager found"
    exit 1
  fi
fi

# Install boto3 if R2 ready
if [ "$R2_READY" = "1" ] && ! python3 -c "import boto3" 2>/dev/null; then
  log "Installing boto3..."
  pip3 install --quiet --break-system-packages boto3 || { log "ERROR: Failed to install boto3"; R2_READY=0; }
fi

log "============================================"
log "DEC-052 P2 — Tier 2 Archive"
log "Mode: $([ "$DRY_RUN" = "1" ] && echo "DRY_RUN" || echo "LIVE")"
log "Tables: $TABLES"
log "R2: $([ "$R2_READY" = "1" ] && echo "configured" || echo "NOT configured (local files only)")"
log "============================================"

# Process each table
IFS=',' read -ra TABLE_ARR <<< "$TABLES"
for table in "${TABLE_ARR[@]}"; do
  table=$(echo "$table" | xargs)  # trim whitespace
  log ""
  log "Processing: $table"

  # Determine age threshold
  case "$table" in
    audit_log) AGE_DAYS="$AUDIT_LOG_AGE_DAYS" ;;
    stock_movements) AGE_DAYS="$STOCK_MOVEMENTS_AGE_DAYS" ;;
    *) log "  SKIP: unknown table $table (add to script if needed)"; continue ;;
  esac

  # Count records to archive
  COUNT=$(psql "$DB_URL" --tuples-only --no-psqlrc -c "
    SELECT COUNT(*) FROM $table
    WHERE archived_at IS NULL
      AND created_at < NOW() - INTERVAL '$AGE_DAYS days'
  " 2>/dev/null | tr -d ' ' || echo "0")

  log "  Records to archive: $COUNT (age > $AGE_DAYS days)"

  if [ "$COUNT" -eq 0 ]; then
    log "  Nothing to archive"
    continue
  fi

  if [ "$DRY_RUN" = "1" ]; then
    log "  [DRY_RUN] Would archive $COUNT records"
    continue
  fi

  # Export to JSONL.gz
  EXPORT_FILE="/tmp/${table}_$(date -u +%Y%m%d_%H%M%S).jsonl.gz"
  log "  Exporting to: $EXPORT_FILE"

  psql "$DB_URL" --no-psqlrc -c "
    COPY (
      SELECT row_to_json(t)
      FROM $table t
      WHERE archived_at IS NULL
        AND created_at < NOW() - INTERVAL '$AGE_DAYS days'
      ORDER BY created_at
      LIMIT $BATCH_SIZE
    ) TO STDOUT
  " 2>/dev/null | gzip > "$EXPORT_FILE"

  SIZE=$(stat -c%s "$EXPORT_FILE")
  SHA=$(sha256sum "$EXPORT_FILE" | awk '{print $1}')
  log "  Size: $SIZE bytes, SHA: ${SHA:0:16}..."

  # Upload to R2 if configured
  if [ "$R2_READY" = "1" ]; then
    R2_KEY="archive/${table}/$(date -u +%Y)/$(basename $EXPORT_FILE)"
    log "  Uploading to R2: $R2_KEY"
    python3 << PYEOF
import os, boto3
s3 = boto3.client(
    's3',
    endpoint_url=os.environ['R2_ENDPOINT'],
    aws_access_key_id=os.environ['R2_ACCESS_KEY'],
    aws_secret_access_key=os.environ['R2_SECRET_KEY'],
)
s3.upload_file(
    "$EXPORT_FILE", os.environ['R2_BUCKET'], "$R2_KEY",
    ExtraArgs={'StorageClass': 'GLACIER', 'Metadata': {'sha256': '$SHA'}}
)
print("  [OK] Uploaded")
PYEOF

    # Record in metadata
    psql "$DB_URL" --no-psqlrc -c "
      INSERT INTO archive_metadata
        (table_name, period_start, period_end, record_count, size_bytes, sha256, storage_backend, storage_path)
      VALUES
        ('$table', NOW() - INTERVAL '$AGE_DAYS days', NOW(), $COUNT, $SIZE, '$SHA', 'r2', '$R2_KEY');
    " 2>/dev/null
    log "  [OK] Recorded in archive_metadata"

    # Mark records as archived
    UPDATED=$(psql "$DB_URL" --tuples-only --no-psqlrc -c "
      UPDATE $table
      SET archived_at = NOW()
      WHERE archived_at IS NULL
        AND created_at < NOW() - INTERVAL '$AGE_DAYS days';
    " 2>/dev/null | tr -d ' ' || echo "0")
    log "  Marked $UPDATED records as archived"
  else
    log "  [SKIP] R2 not configured, file kept at $EXPORT_FILE"
  fi
done

log ""
log "============================================"
log "Tier 2 archive complete"
log "============================================"
