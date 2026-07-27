/**
 * Remaining screenshots — admin pages (with proper login)
 */
import { chromium } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const FRONTEND = 'http://localhost:3000';
const ADMIN_EMAIL = 'admin@alfajr.local';
const ADMIN_PASSWORD = 'Demo1234';
const OUT = path.join(process.cwd(), 'screenshots');
const PAGES = [
  { url: '/admin/audit', name: '20-admin-audit' },
  { url: '/admin/posting-rules', name: '21-admin-posting-rules' },
  { url: '/admin/item-categories', name: '22-item-categories' },
  { url: '/profile', name: '23-profile' },
  { url: '/profile/change-password', name: '24-change-password' },
  { url: '/notifications', name: '25-notifications' },
  { url: '/admin/companies', name: '26-admin-companies' },
  { url: '/admin/users/new', name: '27-new-user' },
  { url: '/finance/customers/new', name: '28-new-customer' },
];

(async () => {
  if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
  console.log('Launching headed Chrome…');
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'ar', timezoneId: 'Africa/Tripoli' });
  const page = await ctx.newPage();

  // Login via UI (with hydration wait)
  await page.goto(`${FRONTEND}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type="email"]');
  for (let i = 0; i < 20; i++) {
    const hydrated = await page.evaluate(() => {
      const btn = document.querySelector('button[type="submit"]');
      if (!btn) return false;
      const keys = Object.keys(btn);
      return keys.some(k => k.startsWith('__reactProps$') || k.startsWith('__reactEventHandlers$'));
    });
    if (hydrated) break;
    await page.waitForTimeout(250);
  }
  await page.fill('input[type="email"]', ADMIN_EMAIL);
  await page.fill('input[type="password"]', ADMIN_PASSWORD);
  await Promise.all([
    page.waitForResponse(r => r.url().includes('/api/auth/login') && r.status() === 200, { timeout: 10_000 }).catch(() => null),
    page.click('button[type="submit"]'),
  ]);
  await page.waitForTimeout(1500);
  console.log(`After login URL: ${page.url()}`);

  for (const p of PAGES) {
    try {
      console.log(`→ ${p.name} (${p.url})`);
      await page.goto(`${FRONTEND}${p.url}`, { waitUntil: 'domcontentloaded', timeout: 20_000 });
      await page.waitForLoadState('networkidle', { timeout: 8_000 }).catch(() => null);
      await page.waitForTimeout(800);
      const file = path.join(OUT, `${p.name}.png`);
      await page.screenshot({ path: file, fullPage: true });
      console.log(`  ✅ ${file} (${(fs.statSync(file).size / 1024).toFixed(1)} KB)`);
    } catch (e) {
      console.log(`  ❌ ${p.name}: ${e.message.substring(0, 100)}`);
    }
  }

  await browser.close();
  console.log(`\n🎉 Done.`);
})();
