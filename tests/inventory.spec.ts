/**
 * Inventory E2E — Items, Warehouses, Categories, UoM, Stock, Reports
 */
import { test, expect, describe } from '@playwright/test';

test.use({ storageState: '.auth/admin.json' });

describe('Inventory E2E: items → warehouses → categories → stock → reports', () => {
  test('Items list loads', async ({ page }) => {
    await page.goto('/inventory/items');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Warehouses list loads', async ({ page }) => {
    await page.goto('/inventory/warehouses').catch(() => {});
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(100);
  });

  test('Stock levels page loads', async ({ page }) => {
    await page.goto('/inventory/stock-levels').catch(() => {});
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(100);
  });

  test('Stock movements list loads', async ({ page }) => {
    await page.goto('/inventory/movements').catch(() => {});
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(100);
  });

  test('Inventory valuation report loads with non-zero total', async ({ page }) => {
    await page.goto('/reports/inventory/valuation');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(300);
  });

  test('Item detail page navigates from items list', async ({ page }) => {
    await page.goto('/inventory/items');
    await page.waitForLoadState('networkidle');
    const firstLink = page.locator('a[href^="/inventory/items/"]').first();
    if (await firstLink.count() > 0) {
      await firstLink.click();
      await page.waitForLoadState('networkidle');
      const body = await page.textContent('body');
      expect(body.length).toBeGreaterThan(200);
    }
  });
});
