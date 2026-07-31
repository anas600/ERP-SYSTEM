#!/bin/bash
# DEC-053: Apply role-based policies to 13 controllers.
# Replaces `[Authorize]` with the appropriate `[Authorize(Policy="...")]`.

set -e
cd "$(dirname "$0")/.."

# Controller -> Policy mapping (which policy class-level)
declare -A POLICIES
POLICIES["src/backend/Host/Controllers/AccountsController.cs"]="WriteFinance"
POLICIES["src/backend/Host/Controllers/JournalEntriesController.cs"]="WriteFinance"
POLICIES["src/backend/Host/Controllers/LedgerController.cs"]="ReadAccess"
POLICIES["src/backend/Host/Controllers/ItemsController.cs"]="WriteStock"
POLICIES["src/backend/Host/Controllers/StockLevelsController.cs"]="ReadAccess"
POLICIES["src/backend/Host/Controllers/StockMovementsController.cs"]="WriteStock"
POLICIES["src/backend/Host/Controllers/ProjectsController.cs"]="WriteProjects"
POLICIES["src/backend/Host/Controllers/TasksController.cs"]="WriteProjects"
POLICIES["src/backend/Host/Controllers/CompaniesController.cs"]="WriteMasterData"
POLICIES["src/backend/Host/Controllers/ReportsController.cs"]="ReadAccess"
POLICIES["src/backend/Host/Controllers/PostingRulesController.cs"]="WriteMasterData"
POLICIES["src/backend/Host/Controllers/ResourcesController.cs"]="WriteProjects"
POLICIES["src/backend/Host/Controllers/NotificationsController.cs"]="ReadAccess"

UPDATED=0
for file in "${!POLICIES[@]}"; do
  policy="${POLICIES[$file]}"
  if [ ! -f "$file" ]; then
    echo "  ✗ MISSING: $file"
    continue
  fi
  # Check if [Authorize] (no args) is present
  if ! grep -q "^\[Authorize\]$" "$file"; then
    echo "  SKIP: $file (no plain [Authorize])"
    continue
  fi
  # Replace [Authorize] with [Authorize(Policy = "...")]
  sed -i "s|^\[Authorize\]$|[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.${policy})]|" "$file"
  echo "  ✓ $file -> $policy"
  UPDATED=$((UPDATED+1))
done

echo "---"
echo "Total updated: $UPDATED"
