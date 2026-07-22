#!/bin/bash
# scripts/retention-stats.sh — DEC-052 P1: Show row counts for retention planning
#
# Useful for identifying which tables need cleanup and for compliance reporting.
# Run before/after cleanup to see the impact.

set -euo pipefail

if [ -z "${NEON_URL:-}" ]; then
  echo "ERROR: NEON_URL is required" >&2
  exit 1
fi

if ! command -v psql >/dev/null 2>&1; then
  echo "ERROR: psql not installed. Run: apt-get install -y postgresql-client" >&2
  exit 1
fi

# Tables to inspect (focus on high-volume + retention-sensitive)
TABLES=(
  "refresh_tokens"
  "password_reset_tokens"
  "outbox_events"
  "processed_events"
  "notifications"
  "audit_log"
  "stock_movements"
  "journal_entries"
  "vendor_bills"
  "sales_invoices"
)

echo "============================================"
echo "DEC-052: Retention Statistics"
echo "Time: $(date -u +'%Y-%m-%d %H:%M:%S UTC')"
echo "============================================"
printf "%-25s | %12s | %10s | %s\n" "Table" "Total" "Oldest" "Notes"
echo "-------------------------------+--------------+------------+----------------"

for t in "${TABLES[@]}"; do
  RESULT=$(psql "$NEON_URL" --tuples-only --no-psqlrc -c "
    SELECT
      COUNT(*)::text,
      MIN(created_at)::text
    FROM $t
  " 2>/dev/null) || { echo "  $t: (table missing)"; continue; }
  COUNT=$(echo "$RESULT" | tr -d ' ' | head -1)
  OLDEST=$(echo "$RESULT" | tail -1)
  printf "%-25s | %12s | %-10s |\n" "$t" "$COUNT" "${OLDEST:0:10}"

  # Add retention note
  case $t in
    refresh_tokens) echo "  └─ retention: 30d after revoke/expire";;
    password_reset_tokens) echo "  └─ retention: 24h after use/expire";;
    outbox_events) echo "  └─ retention: 30d after processed";;
    processed_events) echo "  └─ retention: 30d (idempotency)";;
    notifications) echo "  └─ retention: 90d after read";;
    audit_log) echo "  └─ retention: 7y (IFRS) — NOT auto-deleted";;
    stock_movements) echo "  └─ retention: 3y hot, then archive";;
    journal_entries) echo "  └─ retention: 7y (IFRS) — NEVER delete";;
    vendor_bills) echo "  └─ retention: 7y (tax) — NEVER delete";;
    sales_invoices) echo "  └─ retention: 7y (tax) — NEVER delete";;
  esac
done

echo ""
echo "============================================"
echo "Next: run scripts/data-retention-cleanup.sh"
echo "============================================"
