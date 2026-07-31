#!/bin/bash
# scripts/pg-dump.sh — DEC-051: Dump Neon PostgreSQL to local file
#
# Usage: NEON_URL=postgresql://... bash scripts/pg-dump.sh
# Output: /tmp/erp-backup-YYYYMMDD-HHMMSS.sql.gz (and .sha256)
#
# Required env:
#   NEON_URL    — full PostgreSQL connection string
# Optional env:
#   BACKUP_DIR  — output directory (default /tmp)
#   KEEP_LOCAL  — keep local copy after upload (default 1, set 0 to delete)

set -euo pipefail

# Validate env
if [ -z "${NEON_URL:-}" ]; then
  echo "ERROR: NEON_URL is required" >&2
  exit 1
fi

# Setup
BACKUP_DIR="${BACKUP_DIR:-/tmp}"
KEEP_LOCAL="${KEEP_LOCAL:-1}"
TIMESTAMP=$(date -u +"%Y%m%d-%H%M%S")
BACKUP_FILE="$BACKUP_DIR/erp-backup-$TIMESTAMP.sql.gz"
CHECKSUM_FILE="$BACKUP_FILE.sha256"
LOG_FILE="$BACKUP_DIR/erp-backup-$TIMESTAMP.log"

mkdir -p "$BACKUP_DIR"

log() {
  echo "[$(date -u +%H:%M:%S)] $*" | tee -a "$LOG_FILE"
}

log "=== DEC-051 pg_dump started ==="
log "Backup file: $BACKUP_FILE"
log "NEON_URL host: $(echo "$NEON_URL" | sed -E 's|.*@([^/]+)/.*|\1|')"

# Check pg_dump available
if ! command -v pg_dump >/dev/null 2>&1; then
  log "ERROR: pg_dump not found. Install postgresql-client:"
  log "  apt-get install -y postgresql-client"
  log "  apk add postgresql-client"
  log "  brew install libpq"
  exit 1
fi

# Run pg_dump (compressed, no owner, no privileges, custom-ish for restore)
log "Running pg_dump..."
if pg_dump "$NEON_URL" \
  --no-owner \
  --no-privileges \
  --clean \
  --if-exists \
  --quote-all-identifiers \
  --compress=9 \
  --file="$BACKUP_FILE" 2>>"$LOG_FILE"; then
  log "pg_dump OK"
else
  log "ERROR: pg_dump failed (see $LOG_FILE)"
  exit 1
fi

# Verify file exists and has content
if [ ! -s "$BACKUP_FILE" ]; then
  log "ERROR: backup file is empty or missing"
  exit 1
fi

# Generate SHA256 checksum
log "Generating checksum..."
sha256sum "$BACKUP_FILE" | awk '{print $1}' > "$CHECKSUM_FILE"
SIZE=$(stat -c%s "$BACKUP_FILE" 2>/dev/null || stat -f%z "$BACKUP_FILE")
log "Size: $SIZE bytes"
log "Checksum: $(cat "$CHECKSUM_FILE")"

# Cleanup if not keeping
if [ "$KEEP_LOCAL" = "0" ]; then
  log "KEEP_LOCAL=0, will be cleaned after upload"
fi

log "=== DEC-051 pg_dump complete ==="
echo "BACKUP_FILE=$BACKUP_FILE"
echo "CHECKSUM=$CHECKSUM_FILE"
echo "SIZE=$SIZE"
