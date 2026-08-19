// Sprint 56 screenshots — Path C.1 (Top Customers + Top Items)
import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import { join } from 'path';

const BASE = 'http://localhost:3000';
const OUT = 'docs/screenshots/sprint-56';

async function main() {
  await mkdir(OUT, { recursive: true });
  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1400, height: 900 } });
  const page = await context.newPage();

  console.log('1. Login...');
  await page.goto(BASE + '/login');
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL(/dashboard/, { timeout: 15000 });
  await page.waitForTimeout(1500);

  console.log('2. Top Customers (Tab 1)...');
  await page.goto(BASE + '/finance/reports/top-customers');
  await page.waitForSelector('text=أكبر العملاء', { timeout: 10000 });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: join(OUT, '01-top-customers.png'), fullPage: true });
  console.log('  ✓ 01-top-customers.png');

  console.log('3. Top Items (Tab 2)...');
  await page.click('text=أكبر الأصناف');
  await page.waitForTimeout(1500);
  await page.screenshot({ path: join(OUT, '02-top-items.png'), fullPage: true });
  console.log('  ✓ 02-top-items.png');

  console.log('4. Aging summary (for comparison)...');
  await page.goto(BASE + '/finance/reports/aging-summary');
  await page.waitForTimeout(2000);
  await page.screenshot({ path: join(OUT, '03-aging-summary.png'), fullPage: true });
  console.log('  ✓ 03-aging-summary.png');

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error(e); process.exit(1); });
