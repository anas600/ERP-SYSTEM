# Sprint 0: Setup + Demo Data

**Goal:** Environment ready, demo data seeded, Mavis Local + 2 Jimis operational
**Time:** 0.5 hours (30 min)
**Owner:** Mavis Local (lead) + 1 Jimi (Backend)
**Refs:** [architecture.md](architecture.md) | [demo-roadmap.md](demo-roadmap.md)

## Tasks (3)

- **T1 (15 min)**: Verify environment
  - HF Space URL responds (`GET /api/health/live` returns 200)
  - Local Docker is up (if applicable)
  - GITHUB_TOKEN has write access
  - Wide-permissions token (per cycle 8 fix)

- **T2 (10 min)**: Seed demo data via migration
  - 1 Holding: "MFA Holding" (slug: `mfa-holding`)
  - 3 Companies: "MFA Tech", "MFA Trade", "MFA Services"
  - 5 Users: `demo@mfaholding.local` + 4 with varied roles
  - 1 demo transaction per company (idempotent seed)

- **T3 (5 min)**: Test login + JWT + X-Company-Id
  - Login as `demo@mfaholding.local`
  - Verify JWT contains `company_ids: [3 ids]`
  - Switch active company via header
  - Verify `GET /api/me/companies` returns 3

## Permissions
- ✅ Self-merge, --force-with-lease, skip Playwright
- ✅ Wide-permissions GITHUB_TOKEN (per cycle 8)
- ✅ Spawn 2 Jimis (1 Backend, Frontend in Sprint 1+)
- ❌ NO staging/prod/HF production app

## Verification
```bash
# Build + tests
dotnet build Host/ERP-SYSTEM.csproj
dotnet test --filter "DemoDataSeed"

# Smoke test
curl -X POST https://<api>/api/auth/login \
  -d '{"email":"demo@mfaholding.local","password":"..."}'
# → 200 with JWT + 3 company_ids

# X-Company-Id
curl -H "Authorization: Bearer <token>" \
  -H "X-Company-Id: <company_id>" \
  https://<api>/api/me/companies
# → [{id, name, role}, ...]
```

## Definition of Done
- [ ] HF Space URL responds to health check
- [ ] Demo user can log in
- [ ] Demo user has 3 companies
- [ ] 1 transaction per company exists
- [ ] All migrations run idempotently

## Next Sprint Preview
Sprint 1 (Dashboard + Holding) starts immediately after Sprint 0 closes.
Plan: 1 Frontend Jimi + 1 Backend Jimi in parallel.

---

— سيتي (Cloud), 2026-07-28 03:45 UTC
