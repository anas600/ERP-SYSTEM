#!/usr/bin/env bash
# =============================================================================
# health-ping.sh — Token-free Cloud↔Local heartbeat (Phase 6 / Cycle 5)
# =============================================================================
#
# Purpose:  Detect when سيتی (Cloud Coordinator) is unresponsive, without
#           burning tokens. The "network/cloud outage" failure mode was
#           documented in cycle 1 (DEC-072) and cycle 4 (lessons-learned.md).
#
# What it does:
#   1. Token-free health check (git fetch + a small count check + remote API)
#   2. Writes status to docs/governance/internal/health-ping.json
#   3. Compares last_check against now; marks stuck if > 30 min
#
# What it does NOT do:
#   - No LLM calls (no token spend)
#   - No auto-merge / no file modification outside the health-ping file
#   - No network calls except curl (no auth headers, no API keys)
#
# Schedule: every 10 min in cloud session
# Output:   docs/governance/internal/health-ping.json
# Status:   alive | idle | stuck | unreachable
#
# Usage:    bash scripts/health-ping.sh
#           (also called by .github/workflows/health-ping.yml)
#
# Cross-platform: works on Linux, macOS, and Windows (Git Bash)
# =============================================================================

set -euo pipefail

# Resolve repo root (script can be called from anywhere)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
HEALTH_FILE="$REPO_ROOT/docs/governance/internal/health-ping.json"

# Ensure the internal/ directory exists
mkdir -p "$(dirname "$HEALTH_FILE")"

# Get current timestamp in ISO 8601 (UTC)
NOW_ISO=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
NOW_EPOCH=$(date -u +%s)

# Health check 1: git fetch + count (no auth, no secrets)
#  - If git fetch fails, we're either offline or the remote is unreachable.
#  - If the remote HEAD moved recently, the cloud is alive.
#  - This is the primary signal: "did the cloud push anything in the last 10 min?"
LAST_REMOTE_COMMIT_EPOCH=0
if git fetch --dry-run origin develop >/dev/null 2>&1; then
    LAST_REMOTE_COMMIT_EPOCH=$(git log origin/develop -1 --format=%ct 2>/dev/null || echo 0)
fi

# Health check 2: GitHub API (optional, token-free)
#  - Just check if the repo is reachable via the public API.
#  - If we get a 200, the network is up.
GITHUB_API_STATUS="unreachable"
if command -v curl >/dev/null 2>&1; then
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "User-Agent: Mavis-Local-Health-Ping" \
        "https://api.github.com/repos/anas600/ERP-SYSTEM" 2>/dev/null || echo "000")
    case "$HTTP_CODE" in
        200|301|304) GITHUB_API_STATUS="reachable" ;;
        403|429)     GITHUB_API_STATUS="rate_limited" ;;
        *)           GITHUB_API_STATUS="unreachable" ;;
    esac
fi

# Compute status
# - "alive"   = remote moved in the last 10 min
# - "idle"    = remote is reachable, no recent activity
# - "stuck"   = no remote activity for > 30 min
# - "unreachable" = git fetch failed
STATUS="unreachable"
if [ "$LAST_REMOTE_COMMIT_EPOCH" -gt 0 ]; then
    AGE=$(( NOW_EPOCH - LAST_REMOTE_COMMIT_EPOCH ))
    if [ "$AGE" -lt 600 ]; then
        STATUS="alive"
    elif [ "$AGE" -lt 1800 ]; then
        STATUS="idle"
    else
        STATUS="stuck"
    fi
fi

# Write the JSON file (atomic via temp file + mv)
TEMP_FILE=$(mktemp 2>/dev/null || echo "$HEALTH_FILE.tmp")
cat > "$TEMP_FILE" <<EOF
{
  "last_check": "$NOW_ISO",
  "last_check_epoch": $NOW_EPOCH,
  "status": "$STATUS",
  "github_api": "$GITHUB_API_STATUS",
  "last_remote_commit_epoch": $LAST_REMOTE_COMMIT_EPOCH,
  "stale_threshold_seconds": 1800
}
EOF
mv "$TEMP_FILE" "$HEALTH_FILE"

# Output a one-line summary (for cron logs)
echo "[health-ping] $NOW_ISO status=$STATUS github=$GITHUB_API_STATUS last_commit=$LAST_REMOTE_COMMIT_EPOCH"

# Exit code: 0 = alive/idle, 1 = stuck, 2 = unreachable
case "$STATUS" in
    alive|idle)  exit 0 ;;
    stuck)       exit 1 ;;
    unreachable) exit 2 ;;
esac
