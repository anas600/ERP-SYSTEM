# Sprint 1: Dashboard + Holding View

**Goal:** Top-level views for client demo
**Time:** 2 hours
**Owner:** Mavis Local + 2 Jimis (1 Frontend + 1 Backend in parallel)
**Refs:** [architecture.md](architecture.md) | [demo-roadmap.md](demo-roadmap.md) | [sprint-0.md](sprint-0.md)

## Block A (Backend Jimi — 1h)

- [ ] **T1**: `GET /api/dashboard/summary` endpoint
  - Returns: `{ companies: 3, users: 5, activities_today: 12, transactions: 3 }`
  - Reuse existing tables: companies, users, activity_log, ledger_entries
  - File: `src/backend/Modules/Dashboard/Endpoints/GetSummary.cs`

- [ ] **T2**: `GET /api/holdings/{slug}` endpoint
  - Returns: holding details + list of companies
  - Slug-based lookup (e.g., `mfa-holding`)
  - File: `src/backend/Modules/Holdings/Endpoints/GetBySlug.cs`

- [ ] **T3**: 1 unit test
  - `DashboardSummaryTests.cs` (~40 lines)

## Block B (Frontend Jimi — 1h)

- [ ] **T4**: `app/admin/dashboard/page.tsx` (new)
  - 4 KPI cards in a grid
  - Each shows: icon, label, value, change indicator
  - Use existing shadcn/ui Card component

- [ ] **T5**: `app/holding/page.tsx` (new)
  - Header: Holding name + logo placeholder
  - Sub-companies list (cards or table)
  - "Switch Active Company" dropdown in top-right
  - Uses CompanySwitcher from cycle 7

## Block C (Mavis Local — 30 min)

- [ ] **T6**: Verify both blocks
  - Backend: curl smoke test the 2 endpoints
  - Frontend: visual check on local dev server
  - Confirm data shows correctly

- [ ] **T7**: Open PR
  - Branch: `feature/sprint-1-dashboard-holding`
  - Squash merge per DEC-070

## Permissions
- ✅ Self-merge, --force-with-lease, skip Playwright
- ✅ Wide-permissions GITHUB_TOKEN
- ✅ Spawn 2 Jimis (1 FE + 1 BE) in parallel
- ❌ NO staging/prod/HF production

## Verification
```bash
# Backend
dotnet build && dotnet test --filter "DashboardSummary"
curl -H "Authorization: Bearer <token>" https://<api>/api/dashboard/summary

# Frontend
cd src/frontend && pnpm dev
# Open http://localhost:3000/admin/dashboard
# Verify: 4 KPI cards, holding view, company switcher
```

## Definition of Done
- [ ] Both endpoints return JSON (200)
- [ ] Dashboard shows 4 KPIs with real data
- [ ] Holding page shows 1 Holding + 3 companies
- [ ] Company switcher works (changes active company)
- [ ] All tests pass
- [ ] CI green on PR

## Next Sprint
Sprint 2 (Companies + Users) — 2h, FE + BE Jimis parallel

— سيتي (Cloud), 2026-07-28 04:05 UTC
