#!/bin/bash
# scripts/data-retention-cleanup.sh — DEC-052 P1: Tier 1 hot cleanup
#
# Runs nightly at 03:00 UTC (after backup at 02:00).
# Idempotent: can run multiple times safely.
#
# Required env:
#   NEON_URL — PostgreSQL connection string
# Optional env:
#   DRY_RUN — if "1", only count rows, don't delete (default 0)
#   LOG_FILE — log file path (default /tmp/retention-cleanup.log)

set -euo pipefail

if [ -z "${NEON_URL:-}" ]; then
  echo "ERROR: NEON_URL is required" >&2
  exit 1
fi

DRY_RUN="${DRY_RUN:-0}"
LOG_FILE="${LOG_FILE:-/tmp/retention-cleanup.log}"
TS=$(date -u +"%Y-%m-%d %H:%M:%S UTC")

log() {
  echo "[$TS] $*" | tee -a "$LOG_FILE"
}

run_sql() {
  local label="$1"
  local sql="$2"
  if [ "$DRY_RUN" = "1" ]; then
    log "[DRY_RUN] $label — would execute:"
    log "  $sql"
    return 0
  fi
  log "Running: $label"
  local rows
  if rows=$(psql "$NEON_URL" --tuples-only --no-psqlrc -c "$sql" 2>>"$LOG_FILE"); then
    log "  → $rows rows affected"
    echo "$rows"
  else
    log "  ✗ ERROR (see $LOG_FILE)"
    echo "0"
  fi
}

# Install psql if missing
if ! command -v psql >/dev/null 2>&1; then
  log "Installing postgresql-client..."
  apt-get install -y postgresql-client >/dev/null 2>&1 || apk add postgresql-client >/dev/null 2>&1
fi

log "============================================"
log "DEC-052 P1 — Tier 1 Hot Cleanup"
log "Started: $TS"
log "Mode: $([ "$DRY_RUN" = "1" ] && echo "DRY_RUN" || echo "LIVE")"
log "============================================"

TOTAL_DELETED=0

# 1. Refresh tokens: delete revoked > 30 days ago OR expired > 30 days ago
log ""
log "1. refresh_tokens (>30d old, expired/revoked)"
DELETED=$(run_sql "refresh_tokens cleanup" "
DELETE FROM refresh_tokens
WHERE (revoked_at IS NOT NULL AND revoked_at < NOW() - INTERVAL '30 days')
   OR (expires_at < NOW() - INTERVAL '30 days');
")
TOTAL_DELETED=$((TOTAL_DELETED + DELETED))

# 2. Password reset tokens: delete used OR expired > 24h
log ""
log "2. password_reset_tokens (used or expired > 24h)"
DELETED=$(run_sql "password_reset_tokens cleanup" "
DELETE FROM password_reset_tokens
WHERE used_at IS NOT NULL
   OR expires_at < NOW() - INTERVAL '24 hours';
")
TOTAL_DELETED=$((TOTAL_DELETED + DELETED))

# 3. Outbox events: delete processed > 30 days
log ""
log "3. outbox_events (processed > 30d)"
DELETED=$(run_sql "outbox_events cleanup" "
DELETE FROM outbox_events
WHERE processed_at IS NOT NULL
  AND processed_at < NOW() - INTERVAL '30 days';
")
TOTAL_DELETED=$((TOTAL_DELETED + DELETED))

# 4. Processed events: delete > 30 days
log ""
log "4. processed_events (> 30d)"
DELETED=$(run_sql "processed_events cleanup" "
DELETE FROM processed_events
WHERE processed_at < NOW() - INTERVAL '30 days';
")
TOTAL_DELETED=$((TOTAL_DELETED + DELETED))

# 5. Notifications: delete read > 90 days
log ""
log "5. notifications (read > 90d)"
DELETED=$(run_sql "notifications cleanup" "
DELETE FROM notifications
WHERE read_at IS NOT NULL
  AND read_at < NOW() - INTERVAL '90 days';
")
TOTAL_DELETED=$((TOTAL_DELETED + DELETED))

# Summary
log ""
log "============================================"
log "Total rows deleted: $TOTAL_DELETED"
log "============================================"

# Alert if high delete count (possible runaway)
if [ "$TOTAL_DELETED" -gt 10000 ] && [ "$DRY_RUN" = "0" ]; then
  log "⚠️ WARNING: Deleted $TOTAL_DELETED rows — high count, possible issue"
  # Send Telegram alert if configured
  if [ -n "${TG_BOT_TOKEN:-}" ] && [ -n "${TG_CHAT_ID:-}" ]; then
    curl -s -X POST "https://api.telegram.org/bot${TG_BOT_TOKEN}/sendMessage" \
      -d "chat_id=${TG_CHAT_ID}&text=⚠️ Retention cleanup deleted $TOTAL_DELETED rows (high count)" \
      > /dev/null || true
  fi
fi

log "Done."
