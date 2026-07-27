/**
 * HR E2E — Employees, Departments, Payroll, Leaves, Attendance
 */
import { test, expect, describe } from '@playwright/test';

test.use({ storageState: '.auth/admin.json' });

describe('HR E2E: employees → departments → payroll → leaves → attendance', () => {
  test('Employees list loads', async ({ page }) => {
    await page.goto('/hr/employees');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Departments list loads', async ({ page }) => {
    await page.goto('/hr/departments');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Payroll list loads', async ({ page }) => {
    await page.goto('/hr/payroll');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Leaves list loads', async ({ page }) => {
    await page.goto('/hr/leaves');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Attendance list loads', async ({ page }) => {
    await page.goto('/hr/attendance');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('Cost center performance report loads', async ({ page }) => {
    await page.goto('/reports/financial/cost-center-performance');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });
});
