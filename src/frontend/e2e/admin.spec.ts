import { test, expect, type Page } from '@playwright/test';

/**
 * E2E Admin: regression test for the 4 admin pages that were 404'ing before
 * next.config.js added /api/* rewrites (commit 70a5b58).
 *
 * Affected routes that previously returned 404 from /api/* on port 3000:
 *   - /admin/audit           (calls /api/inventory/notifications + /api/finance/posting-rules)
 *   - /admin/posting-rules   (calls /api/finance/posting-rules)
 *   - /admin/item-categories (calls /api/inventory/categories)
 *   - /admin/notifications   (calls /api/inventory/notifications)
 *
 * The fix: next.config.js gained `async rewrites()` that proxies /api/* to the
 * backend on port 5000. If rewrites are missing or broken, these pages render
 * the "فشل التحميل" error banner — which is what we assert is NOT shown.
 */

const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@alfajr.local';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'Demo1234';

const ADMIN_ROUTES = [
  '/admin/users',
  '/admin/audit',
  '/admin/posting-rules',
  '/admin/item-categories',
  '/admin/companies',
  '/admin/health',
  '/admin/notifications',
];

async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/login');
  await page.waitForSelector('input[type="email"]', { timeout: 15_000 });
  await page.fill('input[type="email"]', ADMIN_EMAIL);
  await page.fill('input[type="password"]', ADMIN_PASSWORD);
  await Promise.all([
    page.waitForURL(/\/(dashboard|admin|hr|inventory|finance|procurement|projects|reports)/, { timeout: 20_000 }),
    page.click('button[type="submit"]'),
  ]);
  await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {});
}

test.describe('admin — pages survive the /api/* rewrite fix', () => {
  test.setTimeout(120_000);

  for (const route of ADMIN_ROUTES) {
    test(`${route} renders without /api 404s`, async ({ page }) => {
      await loginAsAdmin(page);

      // Track 4xx/5xx on /api/* calls
      const apiErrors: string[] = [];
      page.on('response', (r) => {
        if (r.url().includes('/api/') && r.status() >= 400) {
          apiErrors.push(`${r.status()} ${r.url()}`);
        }
      });

      const response = await page.goto(route, { waitUntil: 'domcontentloaded', timeout: 20_000 });
      expect(response, `no response for ${route}`).not.toBeNull();
      expect(response!.status(), `${route} returned ${response!.status()}`).toBeLessThan(400);

      // Give the page time to fetch /api/* and render
      await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});
      await page.waitForTimeout(2_000);

      // The "فشل التحميل" banner is the visual signal of an /api/* 404
      const failBanner = page.getByText('فشل التحميل');
      await expect(failBanner, `${route} shows "فشل التحميل" — /api/* rewrite is broken`).toHaveCount(0);

      // No raw 404s on /api/* (the rewrites proxy to backend; 401 is OK, 404 is NOT)
      const realErrors = apiErrors.filter((e) => !e.startsWith('401 '));
      expect(
        realErrors,
        `${route} got raw 4xx on /api/*:\n${realErrors.join('\n')}`
      ).toEqual([]);
    });
  }

  test('admin sidebar group shows all 7 items', async ({ page }) => {
    await loginAsAdmin(page);
    // Open the sidebar (it should be visible at desktop width by default in tests)
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Group label
    await expect(sidebar.getByText('الإدارة')).toBeVisible();

    // Each item from NAV_GROUPS 'الإدارة' (commit c77921b)
    const adminItems = [
      'المستخدمين',
      'الشركات',
      'فئات الأصناف',
      'قواعد الترحيل',
      'سجل التدقيق',
      'إشعاراتي',
      'صحة النظام',
    ];
    for (const label of adminItems) {
      await expect(sidebar.getByText(label), `missing sidebar item: ${label}`).toBeVisible();
    }
  });
});
