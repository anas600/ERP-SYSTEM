// Sprint 31 Playwright smoke test — Muhammad's browser-based testing tool.
// Usage:  node scripts/playwright-smoke.mjs
// Output: C:\Users\Anas\AppData\Local\Temp\playwright-shots\sprint-NN-*.png + .json

import { chromium } from 'playwright';
import { mkdirSync, writeFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';

const OUT_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
const BASE_URL = process.env.BASE_URL || 'http://localhost:3000';
const EMAIL = process.env.LOGIN_EMAIL || 'admin@erp.local';
const PASSWORD = process.env.LOGIN_PASSWORD || 'ChangeMe1234!';
const SPRINT_TAG = process.env.SPRINT_TAG || 'sprint-31';

if (!existsSync(OUT_DIR)) mkdirSync(OUT_DIR, { recursive: true });

const results = [];
const log = (label, data) => {
  const ts = new Date().toISOString();
  console.log(`[${ts}] ${label}:`, JSON.stringify(data, null, 2));
  results.push({ ts, label, data });
};

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  const consoleErrors = [];
  const networkErrors = [];
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', e => consoleErrors.push(e.message));
  page.on('response', r => { if (r.status() >= 500) networkErrors.push({ url: r.url(), status: r.status() }); });

  // 1. Navigate to login
  await page.goto(BASE_URL, { waitUntil: 'networkidle', timeout: 15000 });
  await page.screenshot({ path: join(OUT_DIR, `${SPRINT_TAG}-01-login.png`), fullPage: true });
  log('navigate', { url: BASE_URL, finalUrl: page.url() });

  // 2. Login using data-testid
  if (page.url().includes('/login')) {
    await page.locator('[data-testid="email"]').fill(EMAIL);
    await page.locator('[data-testid="password"]').fill(PASSWORD);
    await Promise.all([
      page.waitForURL(url => !url.toString().includes('/login'), { timeout: 10000 }).catch(() => null),
      page.locator('button[type="submit"]').click()
    ]);
    log('login', { success: !page.url().includes('/login'), finalUrl: page.url() });
  } else {
    log('login', { success: true, reason: 'not on login page', finalUrl: page.url() });
  }
  await page.screenshot({ path: join(OUT_DIR, `${SPRINT_TAG}-02-after-login.png`), fullPage: true });

  // 3. Test pages (FE routes from app\(authenticated)\...)
  const pages = [
    { name: 'dashboard', path: '/dashboard' },
    { name: 'finance-accounts', path: '/finance/accounts' },
    { name: 'finance-cost-centers', path: '/finance/cost-centers' },
    { name: 'finance-sales-invoices', path: '/finance/sales-invoices' },
    { name: 'finance-receipts', path: '/finance/receipts' },
    { name: 'finance-journal-entries', path: '/finance/journal-entries' },
    { name: 'finance-customers', path: '/finance/customers' },
    { name: 'finance-aging-ar', path: '/finance/aging-ar' },
    { name: 'procurement-purchase-orders', path: '/procurement/purchase-orders' },
    { name: 'procurement-goods-receipts', path: '/procurement/goods-receipts' },
    { name: 'procurement-bills', path: '/procurement/bills' },
    { name: 'procurement-vendors', path: '/procurement/vendors' },
    { name: 'inventory-items', path: '/inventory/items' },
    { name: 'inventory-stock-levels', path: '/inventory/stock-levels' },
    { name: 'hr-employees', path: '/hr/employees' },
    { name: 'hr-departments', path: '/hr/departments' },
    { name: 'hr-attendance', path: '/hr/attendance' },
    { name: 'hr-leaves', path: '/hr/leaves' },
    { name: 'hr-payroll', path: '/hr/payroll' },
    { name: 'projects', path: '/projects' },
    { name: 'admin-posting-rules', path: '/admin/posting-rules' },
    { name: 'admin-users', path: '/admin/users' },
    { name: 'admin-item-categories', path: '/admin/item-categories' },
    { name: 'transactions', path: '/transactions' }
  ];
  const pageResults = [];
  for (const p of pages) {
    try {
      const r = await page.goto(`${BASE_URL}${p.path}`, { waitUntil: 'networkidle', timeout: 10000 });
      const status = r ? r.status() : 0;
      const hasError = await page.locator('text=/Error|خطأ|500|undefined/i').count();
      const hasData = await page.locator('table, .data-row, [data-testid*="row"]').count();
      const pageText = (await page.locator('body').innerText()).substring(0, 200);
      await page.screenshot({ path: join(OUT_DIR, `${SPRINT_TAG}-${p.name}.png`), fullPage: true });
      const result = { path: p.path, status, hasError: hasError > 0, hasData: hasData > 0, pageText };
      pageResults.push(result);
      log(`page:${p.name}`, result);
    } catch (e) {
      const result = { path: p.path, error: e.message };
      pageResults.push(result);
      log(`page:${p.name}`, result);
    }
  }

  // 4. Final report
  log('console-errors', { count: consoleErrors.length, sample: consoleErrors.slice(0, 10) });
  log('network-errors', { count: networkErrors.length, sample: networkErrors.slice(0, 10) });
  const summary = {
    totalPages: pageResults.length,
    ok200: pageResults.filter(r => r.status === 200).length,
    notFound404: pageResults.filter(r => r.status === 404).length,
    serverError500: pageResults.filter(r => r.status === 500).length,
    hasTextError: pageResults.filter(r => r.hasError).length,
    hasData: pageResults.filter(r => r.hasData).length
  };
  log('summary', summary);
  await browser.close();

  writeFileSync(join(OUT_DIR, `${SPRINT_TAG}-results.json`), JSON.stringify({ results, summary }, null, 2));
  console.log(`\n✅ Done. ${results.length} entries. Screenshots in ${OUT_DIR}`);
  console.log(`\n📊 Summary: 200=${summary.ok200}/24, 404=${summary.notFound404}, 500=${summary.serverError500}, hasError=${summary.hasTextError}, hasData=${summary.hasData}`);
}

main().catch(e => { console.error('FATAL', e); process.exit(1); });
