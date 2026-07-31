# Playwright E2E Tests (DEC-094)

End-to-end tests for critical user flows. Runs against a real backend + frontend stack.

## Quick start (local dev)

```bash
# 1. Make sure backend is running (with Supabase config in appsettings.Development.json)
cd src/backend/Host
ASPNETCORE_ENVIRONMENT=Development dotnet run

# 2. In a new terminal — install Playwright + browsers (one-time)
cd src/frontend
npm install
npm run e2e:install   # downloads ~200MB chromium

# 3. Run E2E (frontend dev server auto-started by Playwright if not already running)
npm run e2e
```

## Config

| Env var | Default | Purpose |
|---------|---------|---------|
| `E2E_BASE_URL` | `http://localhost:3000` | Frontend URL |
| `E2E_API_URL` | `http://localhost:5000` | Backend URL |
| `CI` | unset | When set: 2 workers, 2 retries, github reporter |

## Test categories

| Test | Verifies |
|------|----------|
| `register.happy` | Full register flow → JWT cookie, user.defaultCompanyId + user.companies populated, holdingCompanyId returned |
| `register.duplicate` | Same email twice → conflict, original user intact (no orphan user) |
| `login.happy` | Register then login → JWT cookie set, /api/auth/me echoes the same defaultCompanyId |
| `atomicity` | **DEC-091 proof:** abort 5 register requests mid-process → ZERO orphan users (login with aborted email should fail). Phase 6.3: the failure mode is now "no orphan user" (tenants are no longer created at register time). |

## CI integration

The `.github/workflows/e2e.yml` workflow runs these on every push to `develop`:
1. Build backend (`dotnet build`)
2. Start backend with `ASPNETCORE_ENVIRONMENT=Development` + Supabase config
3. Install Playwright + chromium
4. Run `npm run e2e`
5. Upload `playwright-report/` and `test-results/` artifacts on failure

A failed E2E blocks merging to `main` (required status check).

## Why these tests?

<<<<<<< HEAD
- **Atomicity test (DEC-091)**: pre-Phase 6, the 15 orphan tenants found on HF Space in July 2026 were caused by HF proxy timeouts dropping the connection mid-register. The fix made `RegisterAsync` atomic (single conn + tx). Post-Phase 6, the same atomicity property protects against orphan users (no user without user_companies link). This test proves the fix works by simulating a mid-process abort.
- **Duplicate test**: Confirms the second register doesn't leak a new partial state if the email is taken.
=======
- **Atomicity test (DEC-091)**: The 15 orphan rows found on HF Space in July 2026 (tenants at the time; users now in the multi-company model) were caused by HF proxy timeouts dropping the connection mid-register. The fix made `RegisterAsync` atomic (single conn + tx). This test proves the fix works by simulating a mid-process abort.
- **Duplicate test**: Confirms the second register doesn't leak a new user if the email is taken.
>>>>>>> 6062ee7 (refactor(phase6-3): Frontend — Multi-Company model (CompanySwitcher + X-Company-Id))
- **Login happy**: Smoke test for the JWT issuance + refresh token rotation.

## Adding new tests

Use `request.newContext({ baseURL: API_URL })` for API-only tests (faster), or `page.goto('/...')` for full UI tests. Tag with `@smoke` or `@regression` if you want to split runs.
