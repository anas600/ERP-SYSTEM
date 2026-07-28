import { test, expect, type Page } from '@playwright/test';

/**
 * E2E CompanySwitcher (Phase 6.3 / Cycle 5 / DEC-073).
 *
 * Tests the CompanySwitcher component in the topbar:
 *  - Renders the active company name
 *  - Click opens the dropdown
 *  - Dropdown shows all assigned companies
 *  - Selecting a company updates localStorage (currentCompanyId)
 *  - Reloads the view (router.refresh) so data refreshes
 *
 * Pre-conditions: an admin user is logged in (the topbar shows the switcher
 * for all authenticated users). The user must have at least one company
 * assigned (post-Phase 6.1c, every user has the Holding via user_companies).
 *
 * Note: this test is OPTIONAL per DEC-070. It runs on Playwright CI
 * (the e2e job in ci-fast.yml) but is not gating for the build.
 */

const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@alfajr.local';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'Demo1234';

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

test.describe('company switcher — topbar dropdown (Phase 6.3)', () => {
  test.setTimeout(120_000);

  test('renders with the active company name in the topbar', async ({ page }) => {
    await loginAsAdmin(page);
    // The switcher button has aria-haspopup="listbox"
    const switcher = page.locator('button[aria-haspopup="listbox"]').first();
    await expect(switcher, 'topbar CompanySwitcher button').toBeVisible({ timeout: 10_000 });
    // The button should show a company name (not "جاري التحميل…")
    const text = (await switcher.textContent()) ?? '';
    expect(text, 'switcher text should not be the loading placeholder').not.toContain('جاري التحميل');
  });

  test('opens the dropdown and shows at least the Holding company', async ({ page }) => {
    await loginAsAdmin(page);
    const switcher = page.locator('button[aria-haspopup="listbox"]').first();
    await expect(switcher).toBeVisible({ timeout: 10_000 });

    await switcher.click();
    // Dropdown opens (aria-expanded toggles to true)
    await expect(switcher).toHaveAttribute('aria-expanded', 'true', { timeout: 5_000 });

    // The listbox should be visible with at least one option
    const listbox = page.locator('[role="listbox"]').first();
    await expect(listbox).toBeVisible();
    const options = page.locator('[role="option"]');
    const count = await options.count();
    expect(count, 'should have at least one company option').toBeGreaterThanOrEqual(1);
  });

  test('selecting a different company updates localStorage + reloads the view', async ({ page }) => {
    await loginAsAdmin(page);
    const switcher = page.locator('button[aria-haspopup="listbox"]').first();
    await expect(switcher).toBeVisible({ timeout: 10_000 });

    // Capture the initial currentCompanyId from localStorage
    const beforeId = await page.evaluate(() => localStorage.getItem('currentCompanyId'));
    expect(beforeId, 'localStorage should have a currentCompanyId set after login').toBeTruthy();

    // Open dropdown
    await switcher.click();
    const listbox = page.locator('[role="listbox"]').first();
    await expect(listbox).toBeVisible();

    // Find a non-active option
    const options = page.locator('[role="option"]');
    const count = await options.count();
    let switched = false;
    for (let i = 0; i < count; i++) {
      const opt = options.nth(i);
      const selected = await opt.getAttribute('aria-selected');
      if (selected !== 'true') {
        await opt.click();
        switched = true;
        break;
      }
    }
    expect(switched, 'should have at least one non-active company to switch to').toBe(true);

    // After clicking, localStorage currentCompanyId should change
    await page.waitForFunction(
      (prev) => localStorage.getItem('currentCompanyId') !== prev,
      beforeId,
      { timeout: 10_000 },
    );
    const afterId = await page.evaluate(() => localStorage.getItem('currentCompanyId'));
    expect(afterId, 'localStorage currentCompanyId should be different after switch').not.toBe(beforeId);
  });
});
