#!/bin/bash
# scripts/retention-report.sh — DEC-052 P2: Monthly retention report
#
# Generates a CSV report of:
# - Row counts per table (per tier)
# - Records marked for archive (archived_at IS NULL but old)
# - Records already archived (archived_at IS NOT NULL)
# - Oldest record per table
# - Total size per table
#
# Output: /tmp/retention-report-YYYY-MM.csv
# Email: sent to admin (if SMTP configured)
# Upload: uploaded as GitHub Actions artifact

set -euo pipefail

if [ -z "${SUPABASE_URL:-}${NEON_URL:-}" ]; then
  echo "ERROR: SUPABASE_URL or NEON_URL is required" >&2
  exit 1
fi
DB_URL="${SUPABASE_URL:-$NEON_URL}"

REPORT_DATE=$(date -u +%Y-%m)
REPORT_FILE="/tmp/retention-report-${REPORT_DATE}.csv"
LOG_FILE="/tmp/retention-report.log"

# Tables to report on
TABLES=(
  "audit_log"
  "stock_movements"
  "notifications"
  "outbox_events"
  "processed_events"
  "refresh_tokens"
  "password_reset_tokens"
  "journal_entries"
  "vendor_bills"
  "sales_invoices"
)

log() {
  echo "[$(date -u +%H:%M:%S)] $*" | tee -a "$LOG_FILE"
}

log "=== DEC-052 P2 Retention Report: $REPORT_DATE ==="

# CSV header
echo "table,total_rows,oldest,archived,tier0_hot,tier1_warm,tier2_archive,size_mb,notes" > "$REPORT_FILE"

for table in "${TABLES[@]}"; do
  log "Processing: $table"
  psql "$DB_URL" --tuples-only --no-psqlrc -c "
    SELECT
      '$table',
      COUNT(*)::text,
      COALESCE(MIN(created_at)::text, ''),
      COUNT(archived_at)::text,
      COUNT(CASE WHEN created_at > NOW() - INTERVAL '1 year' THEN 1 END)::text,
      COUNT(CASE WHEN created_at BETWEEN NOW() - INTERVAL '3 years' AND NOW() - INTERVAL '1 year' THEN 1 END)::text,
      COUNT(CASE WHEN created_at < NOW() - INTERVAL '3 years' THEN 1 END)::text,
      ROUND(pg_total_relation_size('public.$table') / 1024.0 / 1024.0, 2)::text,
      ''
    FROM $table
  " 2>/dev/null | sed 's/^ *//;s/ *$//;s/ /,/g' >> "$REPORT_FILE" || echo "$table,ERROR,,,,,,," >> "$REPORT_FILE"
done

log "Report: $REPORT_FILE"
log "Size: $(stat -c%s "$REPORT_FILE") bytes"
log "Done."

# Output preview
echo ""
echo "=== Preview ==="
head -10 "$REPORT_FILE"
