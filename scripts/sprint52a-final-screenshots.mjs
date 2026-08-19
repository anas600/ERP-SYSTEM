// Sprint 52a — Take final screenshots of all financial reports + new CoA tree.
import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

const URL = 'http://localhost:3000';
const OUT = 'docs/screenshots/sprint-52a';

async function main() {
  await mkdir(OUT, { recursive: true });
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  page.on('pageerror', (err) => console.log('PAGE ERROR:', err.message));

  // Login
  await page.goto(URL + '/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1500);
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL('**/dashboard', { timeout: 20000 });
  console.log('✓ Logged in');

  // Pages to screenshot
  const pages = [
    { name: 'dashboard', path: '/dashboard', wait: 2500 },
    { name: 'coa-tree', path: '/finance/accounts-tree', wait: 2500 },
    { name: 'accounts-list', path: '/finance/accounts', wait: 2000 },
    { name: 'trial-balance', path: '/finance/reports/trial-balance', wait: 2500 },
    { name: 'general-ledger', path: '/finance/reports/general-ledger', wait: 2500 },
    { name: 'balance-sheet', path: '/finance/reports/balance-sheet', wait: 2500 },
    { name: 'income-statement', path: '/finance/reports/income-statement', wait: 2500 },
    { name: 'cash-flow', path: '/finance/reports/cash-flow', wait: 2500 },
    { name: 'aging-summary', path: '/finance/reports/aging-summary', wait: 2500 },
    { name: 'aging-ar', path: '/finance/aging-ar', wait: 2500 },
  ];

  for (const p of pages) {
    try {
      await page.goto(URL + p.path, { waitUntil: 'domcontentloaded' });
      await page.waitForTimeout(p.wait);
      const file = OUT + '/page-' + p.name + '.png';
      await page.screenshot({ path: file, fullPage: true });
      console.log('✓ ' + p.name);
    } catch (e) {
      console.log('✗ ' + p.name + ': ' + e.message);
    }
  }

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error('FATAL:', e); process.exit(1); });
