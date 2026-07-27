import { test, expect, request as apiRequest, type APIRequestContext } from '@playwright/test';

/**
 * E2E Smoke: verify every sidebar route is reachable + accessible.
 *
 * Phase 6.3 (Multi-Company) + Phase 6.2 (full sidebar menu).
 *
 * Strategy:
 *   - HTTP-level checks (no browser navigation) for raw reachability
 *   - Authenticated checks via /api/auth/login to ensure the route returns
 *     2xx/3xx with a valid Bearer token (Next.js renders, doesn't 401)
 *   - This is faster + more reliable than page.goto() in dev mode (avoids
 *     Next.js first-visit compile time per route)
 *
 * What this catches:
 *   - 404 regressions (e.g., next.config.js rewrites missing)
 *   - Auth-gated routes returning 401 unexpectedly
 *   - Routes that don't exist
 */

const ROUTES = [
  // لوحة التحكم
  '/dashboard',
  // المالية
  '/finance/accounts',
  '/finance/cost-centers',
  '/finance/journal-entries',
  '/finance/customers',
  '/finance/sales-invoices',
  '/finance/receipts',
  '/finance/aging-ar',
  // المخزون
  '/inventory/items',
  '/inventory/movements',
  '/inventory/reservations',
  '/inventory/stock-levels',
  // المشاريع
  '/projects',
  // المشتريات
  '/procurement/vendors',
  '/procurement/purchase-orders',
  '/procurement/goods-receipts',
  '/procurement/bills',
  // الموارد البشرية
  '/hr/employees',
  '/hr/attendance',
  '/hr/leaves',
  '/hr/payroll',
  // التقارير
  '/reports',
  '/reports/financial',
  '/reports/financial/trial-balance',
  '/reports/financial/balance-sheet',
  '/reports/financial/income-statement',
  '/reports/financial/vat',
  '/reports/sales',
  '/reports/sales/sales-by-customer',
  '/reports/inventory',
  '/reports/inventory/valuation',
  '/reports/projects',
  '/reports/projects/budget-vs-actual',
  // الإدارة
  '/admin/users',
  '/admin/companies',
  '/admin/item-categories',
  '/admin/posting-rules',
  '/admin/audit',
  '/notifications',
  '/admin/health',
];

const FRONTEND_URL = process.env.E2E_BASE_URL ?? 'http://localhost:3000';
const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5000';
const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@alfajr.local';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'Demo1234';
const HOLDING_ID = '00000000-0000-0000-0000-000000000001';

async function getAuthedContext(): Promise<APIRequestContext> {
  const ctx = await apiRequest.newContext({ baseURL: API_URL });
  const res = await ctx.post('/api/auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  if (res.status() !== 200) {
    throw new Error(`login failed: ${res.status()} ${await res.text()}`);
  }
  const body = await res.json();
  const token = body.accessToken;
  return apiRequest.newContext({
    baseURL: FRONTEND_URL,
    extraHTTPHeaders: {
      Authorization: `Bearer ${token}`,
      'X-Company-Id': HOLDING_ID,
    },
  });
}

test.describe('smoke — full sidebar reachability', () => {
  test.setTimeout(120_000);

  test('every sidebar route returns 2xx/3xx (authenticated)', async () => {
    const ctx = await getAuthedContext();
    const failures: { route: string; status: number }[] = [];

    for (const route of ROUTES) {
      const res = await ctx.get(route, { failOnStatusCode: false });
      // Accept 2xx and 3xx. 401/403/404 = broken. 5xx = server error.
      if (res.status() >= 400) {
        failures.push({ route, status: res.status() });
      }
    }

    await ctx.dispose();

    if (failures.length) {
      const lines = failures.map((f) => `  ${f.route}  ->  ${f.status}`).join('\n');
      throw new Error(`${failures.length} route(s) failed:\n${lines}`);
    }

    expect(ROUTES.length).toBeGreaterThanOrEqual(35);
  });

  test('routes are reachable without auth (login redirect OK)', async () => {
    const ctx = await apiRequest.newContext({ baseURL: FRONTEND_URL });
    let okCount = 0;
    let redirectedCount = 0;

    for (const route of ROUTES) {
      const res = await ctx.get(route, { failOnStatusCode: false, maxRedirects: 0 });
      if (res.status() === 200) okCount++;
      else if (res.status() >= 300 && res.status() < 400) redirectedCount++;
      else throw new Error(`${route} returned ${res.status()} without auth`);
    }

    await ctx.dispose();
    expect(okCount + redirectedCount).toBe(ROUTES.length);
  });
});
