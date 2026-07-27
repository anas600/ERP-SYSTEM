/**
 * Admin E2E — Users, Roles, Companies, Notifications, Profile
 */
import { test, expect, describe } from '@playwright/test';

test.use({ storageState: '.auth/admin.json' });

describe('Admin E2E: users → roles → companies → profile → notifications', () => {
  test('Admin users page loads', async ({ page }) => {
    await page.goto('/admin/users');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body).toMatch(/مستخدم|admin/i);
    expect(body.length).toBeGreaterThan(300);
  });

  test('New user page loads with form', async ({ page }) => {
    await page.goto('/admin/users/new');
    await page.waitForLoadState('networkidle');
    // Should have email + password fields
    const emailInput = page.locator('input[type="email"]');
    const pwdInput = page.locator('input[type="password"]');
    expect(await emailInput.count()).toBeGreaterThan(0);
    expect(await pwdInput.count()).toBeGreaterThan(0);
  });

  test('User detail page (navigate to first user)', async ({ page }) => {
    await page.goto('/admin/users');
    await page.waitForLoadState('networkidle');
    // Click on first user link/card if present
    const firstUserLink = page.locator('a[href^="/admin/users/"]').first();
    if (await firstUserLink.count() > 0) {
      await firstUserLink.click();
      await page.waitForLoadState('networkidle');
      const body = await page.textContent('body');
      expect(body.length).toBeGreaterThan(200);
    }
  });

  test('Profile page loads', async ({ page }) => {
    await page.goto('/profile');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body).toMatch(/profile|ملف|حساب/i);
  });

  test('Change password page loads', async ({ page }) => {
    await page.goto('/profile/change-password');
    await page.waitForLoadState('networkidle');
    const pwdInputs = page.locator('input[type="password"]');
    expect(await pwdInputs.count()).toBeGreaterThanOrEqual(2);
  });

  test('Notifications page loads', async ({ page }) => {
    await page.goto('/notifications');
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(100);
  });

  test('Companies admin page loads', async ({ page }) => {
    await page.goto('/admin/companies').catch(() => page.goto('/companies'));
    await page.waitForLoadState('networkidle');
    const body = await page.textContent('body');
    expect(body.length).toBeGreaterThan(200);
  });

  test('UserMenu in topbar opens dropdown with logout', async ({ page }) => {
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');
    // Look for user menu trigger (avatar, name, etc.)
    const trigger = page.locator('[data-testid="user-menu"], [aria-label*="user" i], [aria-label*="مستخدم"]').first();
    if (await trigger.count() > 0) {
      await trigger.click();
      await page.waitForTimeout(300);
      // Should have logout option
      const body = await page.textContent('body');
      expect(body).toMatch(/تسجيل الخروج|خروج|logout/i);
    }
  });
});
