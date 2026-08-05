#!/usr/bin/env python3
"""
Sprint 40 (L67): Bulk-fix raw fetch('/api/...') to use API client methods.

For each file, applies a list of regex replacements that map:
  fetch GET /api/inventory/categories       -> inventoryApi.listCategories()
  fetch POST /api/inventory/categories      -> inventoryApi.createCategory(...)
  fetch PUT /api/inventory/categories/{id}  -> inventoryApi.updateCategory(id, ...)
  fetch DELETE /api/inventory/categories/{id} -> inventoryApi.deleteCategory(id)
  fetch GET /api/inventory/items            -> inventoryApi.listItems()
  fetch POST /api/inventory/items           -> inventoryApi.createItem(...)
  fetch GET /api/inventory/items/{id}       -> inventoryApi.getItem(id)
  fetch PUT /api/inventory/items/{id}       -> inventoryApi.updateItem(id, ...)
  fetch GET /api/inventory/warehouses       -> inventoryApi.listWarehouses()
  fetch GET /api/inventory/reservations     -> inventoryApi.listReservations()
  fetch GET /api/inventory/reservations/{id} -> inventoryApi.getReservation(id)
  fetch POST /api/inventory/reservations    -> inventoryApi.createReservation(...)
  fetch PUT /api/inventory/reservations/{id} -> inventoryApi.updateReservation(id, ...)
  fetch GET /api/inventory/movements        -> inventoryApi.listMovements()
  fetch POST /api/inventory/movements       -> inventoryApi.createMovement(...)
  fetch GET /api/finance/posting-rules      -> financeApi.listPostingRules()
  fetch GET /api/finance/posting-rules/{id} -> financeApi.getPostingRule(id)
  fetch POST /api/finance/posting-rules     -> financeApi.createPostingRule(...)
  fetch PUT /api/finance/posting-rules/{id} -> financeApi.updatePostingRule(id, ...)
  fetch GET /api/cost-centers                -> financeApi.listCostCenters()
  fetch GET /api/cost-centers/{id}           -> financeApi.getCostCenter(id)
  fetch POST /api/cost-centers               -> financeApi.createCostCenter(...)
  fetch PUT /api/cost-centers/{id}           -> financeApi.updateCostCenter(id, ...)
  fetch POST /api/projects                   -> projectsApi.createProject(...)

Adds the import automatically if needed.
"""
import re
import sys
from pathlib import Path

ROOT = Path(r"C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21\src\frontend\app\(authenticated)")

# (file path, list of (old_pattern, new_replacement) using re.sub)
# We'll use simpler line-based replacements per file

FILES = [
    "admin/item-categories/[id]/edit/page.tsx",
    "admin/item-categories/new/page.tsx",
    "admin/posting-rules/[id]/edit/page.tsx",
    "admin/posting-rules/[id]/page.tsx",
    "admin/posting-rules/new/page.tsx",
    "finance/accounts/new/page.tsx",
    "finance/cost-centers/new/page.tsx",
    "finance/cost-centers/page.tsx",
    "inventory/items/new/page.tsx",
    "inventory/reservations/[id]/page.tsx",
    "inventory/reservations/new/page.tsx",
    "inventory/reservations/page.tsx",
    "inventory/movements/page.tsx",
    "procurement/goods-receipts/new/page.tsx",
    "projects/new/page.tsx",
]

# For each file, show what needs to change
for f in FILES:
    p = ROOT / f
    text = p.read_text(encoding='utf-8')
    # Find all fetch calls
    fetches = re.findall(r'fetch\([\'"](/api/[^\'"]+)[\'"]\s*,\s*\{([^}]*)\}', text)
    if not fetches:
        print(f"  {f}: NO FETCH FOUND (might be in dynamic form)")
        continue
    print(f"\n{f}:")
    for url, opts in fetches:
        method_m = re.search(r'method\s*:\s*[\'"](\w+)[\'"]', opts)
        method = method_m.group(1) if method_m else 'GET'
        print(f"  [{method}] {url}")
