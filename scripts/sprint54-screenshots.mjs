// Sprint 54 screenshots — Path A.2 (Reports Hierarchy)
// Captures: IS with L2 sections, CF with L3 metadata, TB v2 (hierarchical)

import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import { join } from 'path';

const BASE = 'http://localhost:3000';
const OUT = 'docs/screenshots/sprint-54';

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
  await page.waitForTimeout(1000);

  // ============== Income Statement (with L2 sections) ==============
  console.log('2. Income Statement...');
  await page.goto(BASE + '/finance/reports/income-statement');
  await page.waitForSelector('text=قائمة الدخل', { timeout: 10000 });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: join(OUT, '01-income-statement.png'), fullPage: true });
  console.log('  ✓ 01-income-statement.png');

  // Scroll to L2 sections section
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await page.waitForTimeout(500);
  await page.screenshot({ path: join(OUT, '02-income-statement-l2-sections.png'), fullPage: true });
  console.log('  ✓ 02-income-statement-l2-sections.png');

  // ============== Cash Flow (with L3 metadata) ==============
  console.log('3. Cash Flow...');
  await page.goto(BASE + '/finance/reports/cash-flow');
  await page.waitForSelector('text=التدفقات النقدية', { timeout: 10000 });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: join(OUT, '03-cash-flow.png'), fullPage: true });
  console.log('  ✓ 03-cash-flow.png');

  // ============== TB v2 (Hierarchical) ==============
  console.log('4. TB v2 (Hierarchical)...');
  await page.goto(BASE + '/finance/trial-balance-v2');
  await page.waitForSelector('text=ميزان المراجعة الهرمي', { timeout: 10000 });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: join(OUT, '04-trial-balance-v2.png'), fullPage: true });
  console.log('  ✓ 04-trial-balance-v2.png');

  // ============== Dashboard quick access ==============
  console.log('5. Dashboard...');
  await page.goto(BASE + '/dashboard');
  await page.waitForTimeout(2000);
  await page.screenshot({ path: join(OUT, '05-dashboard.png'), fullPage: true });
  console.log('  ✓ 05-dashboard.png');

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error(e); process.exit(1); });
