/**
 * Visual tour — opens key pages in headed Chrome, takes screenshots,
 * saves to ./screenshots/. Run: node scripts/visual-tour.mjs
 */
import { chromium } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const FRONTEND = 'http://localhost:3000';
const ADMIN_EMAIL = 'admin@alfajr.local';
const ADMIN_PASSWORD = 'Demo1234';
const OUT = path.join(process.cwd(), 'screenshots');

const PAGES = [
  { url: '/dashboard', name: '01-dashboard' },
  { url: '/finance/customers', name: '02-customers' },
  { url: '/finance/sales-invoices', name: '03-invoices' },
  { url: '/procurement/vendors', name: '04-vendors' },
  { url: '/procurement/bills', name: '05-bills' },
  { url: '/inventory/items', name: '06-items' },
  { url: '/hr/employees', name: '07-employees' },
  { url: '/hr/payroll', name: '08-payroll' },
  { url: '/hr/leaves', name: '09-leaves' },
  { url: '/projects', name: '10-projects' },
  { url: '/reports/financial/trial-balance', name: '11-trial-balance' },
  { url: '/reports/financial/balance-sheet', name: '12-balance-sheet' },
  { url: '/reports/financial/income-statement', name: '13-income-statement' },
  { url: '/reports/financial/cash-flow', name: '14-cash-flow' },
  { url: '/reports/financial/vat', name: '15-vat' },
  { url: '/reports/sales/top-customers', name: '16-top-customers' },
  { url: '/reports/procurement/top-vendors', name: '17-top-vendors' },
  { url: '/reports/inventory/valuation', name: '18-inventory-valuation' },
  { url: '/admin/users', name: '19-admin-users' },
  { url: '/admin/audit', name: '20-admin-audit' },
  { url: '/admin/posting-rules', name: '21-admin-posting-rules' },
  { url: '/profile', name: '22-profile' },
  { url: '/notifications', name: '23-notifications' },
];

(async () => {
  if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

  console.log('Launching headed Chrome…');
  const browser = await chromium.launch({ headless: true }); // use headless chrome (not headless shell) for full visual fidelity
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: 'ar',
    timezoneId: 'Africa/Tripoli',
  });
  const page = await ctx.newPage();

  // Login
  console.log('Logging in…');
  await page.goto(`${FRONTEND}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type="email"]');
  // Wait for hydration
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

  // Visit each page
  for (const p of PAGES) {
    try {
      console.log(`→ ${p.name} (${p.url})`);
      await page.goto(`${FRONTEND}${p.url}`, { waitUntil: 'domcontentloaded', timeout: 20_000 });
      await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => null);
      await page.waitForTimeout(800); // let charts/render
      const file = path.join(OUT, `${p.name}.png`);
      await page.screenshot({ path: file, fullPage: true });
      const size = fs.statSync(file).size;
      console.log(`  ✅ ${file} (${(size / 1024).toFixed(1)} KB)`);
    } catch (e) {
      console.log(`  ❌ ${p.name}: ${e.message}`);
    }
  }

  await browser.close();
  console.log(`\n🎉 Visual tour complete. ${PAGES.length} screenshots saved to ${OUT}/`);
})();
