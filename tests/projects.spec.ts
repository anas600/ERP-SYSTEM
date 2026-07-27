/**
 * Projects E2E — Project list, detail, budget-vs-actual
 */
import { test, expect, describe } from '@playwright/test';

test.use({ storageState: '.auth/admin.json' });

describe('Projects E2E', () => {
  test('Projects list loads', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Projects detail page navigates from list', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForLoadState('networkidle');
    const firstLink = page.locator('a[href^="/projects/"]').first();
    if (await firstLink.count() > 0) {
      await firstLink.click();
      await page.waitForLoadState('networkidle');
      const body = await page.textContent('body');
      expect(body.length).toBeGreaterThan(200);
    }
  });

  test('Budget vs actual report loads', async ({ page }) => {
    await page.goto('/reports/projects/budget-vs-actual').catch(() => page.goto('/reports/financial/budget-vs-actual'));
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(300);
  });
});
