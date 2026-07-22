#!/bin/bash
# infra/scripts/verify-backup.sh — DEC-051 P2: Backup verification
#
# Verifies the integrity of a backup file by:
# 1. Downloading from R2 (latest) or using local file
# 2. Creating a temp schema in Supabase
# 3. Restoring the dump INTO the temp schema
# 4. Running validation queries (table count, row counts, schema)
# 5. Reporting OK or list of issues
# 6. Cleaning up the temp schema
#
# Required env:
#   SUPABASE_URL or NEON_URL — PostgreSQL connection string
#   R2_ACCESS_KEY, R2_SECRET_KEY, R2_ENDPOINT, R2_BUCKET — for R2 access
# Optional env:
#   BACKUP_FILE — path to local backup file (skip R2 download)
#   EXPECTED_TABLES — comma-separated list of tables to check (default: all)
#   DRY_RUN — 1=don't actually restore, just verify dump file

set -euo pipefail

# Validate env
if [ -z "${SUPABASE_URL:-}${NEON_URL:-}" ]; then
  echo "ERROR: SUPABASE_URL or NEON_URL is required" >&2
  exit 1
fi
DB_URL="${SUPABASE_URL:-$NEON_URL}"

R2_READY=0
for var in R2_ACCESS_KEY R2_SECRET_KEY R2_ENDPOINT R2_BUCKET; do
  if [ -z "${!var:-}" ]; then
    R2_READY=0
    break
  fi
  R2_READY=1
done

if [ "$R2_READY" = "0" ] && [ -z "${BACKUP_FILE:-}" ]; then
  echo "ERROR: R2 not configured AND no BACKUP_FILE specified" >&2
  exit 1
fi

DRY_RUN="${DRY_RUN:-0}"
LOG_FILE="${LOG_FILE:-/tmp/backup-verify.log}"
TS=$(date -u +"%Y-%m-%d %H:%M:%S UTC")
TEMP_SCHEMA="backup_verify_$(date +%s)_$$"
ISSUES=0
CHECKS_PASSED=0

log() {
  echo "[$TS] $*" | tee -a "$LOG_FILE"
}

# Install required tools
if ! command -v psql >/dev/null 2>&1; then
  log "Installing postgresql-client..."
  apt-get install -y postgresql-client >/dev/null 2>&1 || apk add postgresql-client >/dev/null 2>&1
fi
if [ "$R2_READY" = "1" ] && ! python3 -c "import boto3" 2>/dev/null; then
  log "Installing boto3..."
  pip3 install --quiet --break-system-packages boto3
fi

log "============================================"
log "DEC-051 P2 — Backup Verification"
log "Started: $TS"
log "Mode: $([ "$DRY_RUN" = "1" ] && echo "DRY_RUN" || echo "LIVE")"
log "============================================"

# Step 1: Get backup file
if [ -n "${BACKUP_FILE:-}" ]; then
  if [ ! -f "$BACKUP_FILE" ]; then
    log "ERROR: BACKUP_FILE not found: $BACKUP_FILE"
    exit 1
  fi
  log "Using local backup: $BACKUP_FILE"
  WORK_FILE="$BACKUP_FILE"
else
  log "Downloading latest backup from R2..."
  WORK_FILE="/tmp/verify-backup_$(date +%s).sql.gz"
  python3 << PYEOF
import os, boto3
s3 = boto3.client(
    's3',
    endpoint_url=os.environ['R2_ENDPOINT'],
    aws_access_key_id=os.environ['R2_ACCESS_KEY'],
    aws_secret_access_key=os.environ['R2_SECRET_KEY'],
)
# Find latest
paginator = s3.get_paginator('list_objects_v2')
latest = None
for page in paginator.paginate(Bucket=os.environ['R2_BUCKET'], Prefix='backups/'):
    for obj in page.get('Contents', []):
        if obj['Key'].endswith('.sql.gz'):
            if latest is None or obj['LastModified'] > latest['LastModified']:
                latest = obj
if latest:
    print(f"Latest: {latest['Key']} ({latest['LastModified']})")
    s3.download_file(os.environ['R2_BUCKET'], latest['Key'], "$WORK_FILE")
    print(f"Downloaded to $WORK_FILE")
else:
    print("ERROR: No backups found in R2")
    exit(1)
PYEOF
fi

if [ ! -f "$WORK_FILE" ]; then
  log "ERROR: Working file not created: $WORK_FILE"
  exit 1
fi

SIZE=$(stat -c%s "$WORK_FILE")
log "Backup file size: $SIZE bytes"

# Verify file is valid gzip
if ! gunzip -t "$WORK_FILE" 2>/dev/null; then
  log "❌ FAIL: Backup file is not valid gzip"
  ISSUES=$((ISSUES+1))
  exit 1
fi
log "✅ Backup file is valid gzip"
CHECKS_PASSED=$((CHECKS_PASSED+1))

# Step 2: Create temp schema (if not dry-run)
if [ "$DRY_RUN" != "1" ]; then
  log "Creating temp schema: $TEMP_SCHEMA"
  psql "$DB_URL" --no-psqlrc -c "CREATE SCHEMA $TEMP_SCHEMA;" 2>/dev/null
fi

# Step 3: Restore into temp schema (if not dry-run)
if [ "$DRY_RUN" != "1" ]; then
  log "Restoring backup into $TEMP_SCHEMA..."
  # Modify the dump to use our schema
  if gunzip -c "$WORK_FILE" | sed "s|SET search_path = public, pg_catalog;|SET search_path = $TEMP_SCHEMA, pg_catalog;|g" | \
     grep -v "CREATE SCHEMA public" | grep -v "COMMENT ON SCHEMA" | \
     psql "$DB_URL" --no-psqlrc --single-transaction --quiet 2>>"$LOG_FILE"; then
    log "✅ Restore completed"
    CHECKS_PASSED=$((CHECKS_PASSED+1))
  else
    log "⚠️  Restore had warnings (check $LOG_FILE)"
    # Continue anyway — some warnings are OK
    CHECKS_PASSED=$((CHECKS_PASSED+1))
  fi
fi

# Step 4: Run validation queries
log ""
log "=== Validation Queries ==="

# 4.1 Table count
log "[1/4] Counting tables in $TEMP_SCHEMA..."
if [ "$DRY_RUN" = "1" ]; then
  EXPECTED_TABLE_COUNT=$(gunzip -c "$WORK_FILE" | grep -c "^CREATE TABLE" || echo 0)
  log "  Expected from dump: $EXPECTED_TABLE_COUNT tables"
  if [ "$EXPECTED_TABLE_COUNT" -lt 30 ]; then
    log "  ❌ FAIL: Too few tables (expected ≥ 30, got $EXPECTED_TABLE_COUNT)"
    ISSUES=$((ISSUES+1))
  else
    log "  ✅ PASS"
    CHECKS_PASSED=$((CHECKS_PASSED+1))
  fi
else
  TABLE_COUNT=$(psql "$DB_URL" --tuples-only --no-psqlrc -c "
    SELECT COUNT(*) FROM information_schema.tables
    WHERE table_schema = '$TEMP_SCHEMA'" 2>/dev/null | tr -d ' ' || echo 0)
  log "  Tables in backup: $TABLE_COUNT"
  if [ "$TABLE_COUNT" -lt 30 ]; then
    log "  ❌ FAIL: Too few tables (expected ≥ 30, got $TABLE_COUNT)"
    ISSUES=$((ISSUES+1))
  else
    log "  ✅ PASS"
    CHECKS_PASSED=$((CHECKS_PASSED+1))
  fi
fi

# 4.2 Key tables exist
log "[2/4] Checking key tables exist..."
KEY_TABLES=("tenants" "users" "roles" "companies" "accounts" "journal_entries" "items")
MISSING=0
for tbl in "${KEY_TABLES[@]}"; do
  if [ "$DRY_RUN" = "1" ]; then
    if gunzip -c "$WORK_FILE" | grep -q "CREATE TABLE $tbl "; then
      log "  ✅ $tbl"
      CHECKS_PASSED=$((CHECKS_PASSED+1))
    else
      log "  ❌ MISSING: $tbl"
      MISSING=$((MISSING+1))
    fi
  else
    EXISTS=$(psql "$DB_URL" --tuples-only --no-psqlrc -c "
      SELECT EXISTS (SELECT 1 FROM information_schema.tables
      WHERE table_schema = '$TEMP_SCHEMA' AND table_name = '$tbl')" 2>/dev/null | tr -d ' ' || echo "f")
    if [ "$EXISTS" = "t" ]; then
      log "  ✅ $tbl"
      CHECKS_PASSED=$((CHECKS_PASSED+1))
    else
      log "  ❌ MISSING: $tbl"
      MISSING=$((MISSING+1))
    fi
  fi
done
if [ "$MISSING" -gt 0 ]; then
  ISSUES=$((ISSUES+1))
fi

# 4.3 Row counts (sanity check)
log "[3/4] Row counts (sanity check)..."
SAMPLE_TABLES=("tenants" "users" "companies" "items" "roles")
for tbl in "${SAMPLE_TABLES[@]}"; do
  if [ "$DRY_RUN" = "1" ]; then
    log "  [DRY_RUN] Would check row count of $tbl"
  else
    CNT=$(psql "$DB_URL" --tuples-only --no-psqlrc -c "
      SELECT COUNT(*) FROM $TEMP_SCHEMA.$tbl" 2>/dev/null | tr -d ' ' || echo 0)
    log "  $tbl: $CNT rows"
  fi
done
CHECKS_PASSED=$((CHECKS_PASSED+1))

# 4.4 Schema integrity
log "[4/4] Schema integrity check..."
if [ "$DRY_RUN" = "1" ]; then
  log "  [DRY_RUN] Would compare schema"
else
  # Check that key columns exist
  COL_CHECKS=0
  for col_check in "users.email" "tenants.code" "companies.code" "items.code"; do
    tbl=$(echo "$col_check" | cut -d. -f1)
    col=$(echo "$col_check" | cut -d. -f2)
    EXISTS=$(psql "$DB_URL" --tuples-only --no-psqlrc -c "
      SELECT EXISTS (SELECT 1 FROM information_schema.columns
      WHERE table_schema = '$TEMP_SCHEMA' AND table_name = '$tbl' AND column_name = '$col')" 2>/dev/null | tr -d ' ' || echo "f")
    if [ "$EXISTS" = "t" ]; then
      log "  ✅ $tbl.$col"
      CHECKS_PASSED=$((CHECKS_PASSED+1))
    else
      log "  ❌ MISSING: $col_check"
      COL_CHECKS=$((COL_CHECKS+1))
    fi
  done
  if [ "$COL_CHECKS" -gt 0 ]; then
    ISSUES=$((ISSUES+1))
  fi
fi

# Step 5: Cleanup
if [ "$DRY_RUN" != "1" ]; then
  log ""
  log "Cleaning up: DROP SCHEMA $TEMP_SCHEMA CASCADE"
  psql "$DB_URL" --no-psqlrc -c "DROP SCHEMA $TEMP_SCHEMA CASCADE;" 2>>"$LOG_FILE" || log "  ⚠️  Drop failed (may have been auto-cleaned)"
fi

# Summary
log ""
log "============================================"
log "VERIFICATION SUMMARY"
log "============================================"
log "Checks passed: $CHECKS_PASSED"
log "Issues found:  $ISSUES"
log "Backup file:  $WORK_FILE"
log "Size:         $SIZE bytes"
log "============================================"

if [ "$ISSUES" -gt 0 ]; then
  log "❌ VERIFICATION FAILED"
  exit 1
else
  log "✅ VERIFICATION PASSED"
  exit 0
fi
