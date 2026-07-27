/**
 * Procurement E2E — Vendors, Bills, POs, GRs, Reports
 */
import { test, expect, describe } from '@playwright/test';

test.use({ storageState: '.auth/admin.json' });

describe('Procurement E2E: vendors → bills → POs → GRs → reports', () => {
  test('Vendors list page loads', async ({ page }) => {
    await page.goto('/procurement/vendors');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body).toMatch(/مورد|vendor/i);
    expect(body.length).toBeGreaterThan(200);
  });

  test('Vendor bills list loads', async ({ page }) => {
    await page.goto('/procurement/bills');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('POs list loads', async ({ page }) => {
    await page.goto('/procurement/purchase-orders');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('GRs list loads', async ({ page }) => {
    await page.goto('/procurement/goods-receipts');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Top vendors report loads', async ({ page }) => {
    await page.goto('/reports/procurement/top-vendors');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(300);
  });

  test('Purchases by vendor report loads', async ({ page }) => {
    await page.goto('/reports/procurement/purchases-by-vendor');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(300);
  });

  test('AP Aging report loads', async ({ page }) => {
    await page.goto('/reports/financial/ap-aging');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });
});
