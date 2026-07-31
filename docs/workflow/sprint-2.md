# Sprint 2: Companies + Users

**Goal:** Multi-company management + user management for client demo
**Time:** 2 hours | **Owner:** Mavis Local + 2 Jimis (FE+BE parallel)
**Refs:** [architecture.md](architecture.md) | [demo-roadmap.md](demo-roadmap.md) | [sprint-1.md](sprint-1.md)

## Block A (Backend Jimi — 1h)

- [ ] **T1**: `GET /api/companies` — list with pagination (`?page=1&pageSize=20`)
- [ ] **T2**: `GET /api/companies/{id}` — details
- [ ] **T3**: `POST /api/companies` — create (idempotent on name + holding_id)
- [ ] **T4**: `GET /api/users` — list with optional `?company_id=` filter
- [ ] **T5**: `GET /api/users/{id}/companies` — assigned companies
- [ ] **T6**: 1-2 unit tests (CompaniesListTests, UserCompanyAccessTests)

## Block B (Frontend Jimi — 1h)

- [ ] **T7**: `app/(authenticated)/admin/companies/page.tsx` — table view (paginated)
- [ ] **T8**: `app/(authenticated)/admin/companies/[id]/page.tsx` — details + edit
- [ ] **T9**: `app/(authenticated)/admin/users/page.tsx` — table view (with company filter)
- [ ] **T10**: `app/(authenticated)/admin/users/[id]/page.tsx` — details + assigned companies

## Block C (Mavis Local — 30 min)

- [ ] **T11**: Verify both blocks (Backend smoke + Frontend visual)
- [ ] **T12**: Open PR (`feature/sprint-2-companies-users`, squash merge per DEC-070)

## Permissions
- ✅ Self-merge, --force-with-lease, skip Playwright
- ✅ Wide-permissions GITHUB_TOKEN
- ✅ Spawn 2 Jimis (FE+BE) in parallel

## Verification
```bash
dotnet build && dotnet test --filter "CompaniesList|UserCompanyAccess"
# T1: GET /api/companies → JSON array (3 companies from demo)
# T4: GET /api/users?company_id=1 → filtered list
# T7-T10: Frontend pages load, show data, allow interaction
```

## Definition of Done
- [ ] Companies list shows 3 demo companies
- [ ] User details show assigned companies
- [ ] Can navigate between companies and users
- [ ] All tests pass
- [ ] CI green on PR

## Next Sprint
Sprint 3 (Activity + Notifications, 1.5h)

— سيتي (Cloud), 2026-07-28 21:15 UTC
