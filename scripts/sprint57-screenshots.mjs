// Sprint 57 screenshots — Path C.2 (Executive Dashboard)
import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';
import { join } from 'path';

const BASE = 'http://localhost:3000';
const OUT = 'docs/screenshots/sprint-57';

async function main() {
  await mkdir(OUT, { recursive: true });
  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1400, height: 1000 } });
  const page = await context.newPage();

  console.log('1. Login...');
  await page.goto(BASE + '/login');
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL(/dashboard/, { timeout: 15000 });
  await page.waitForTimeout(1500);

  console.log('2. Executive Dashboard...');
  await page.goto(BASE + '/dashboard/executive');
  await page.waitForSelector('text=اللوحة التنفيذية', { timeout: 10000 });
  await page.waitForTimeout(3000); // wait for charts to render
  await page.screenshot({ path: join(OUT, '01-executive-dashboard.png'), fullPage: true });
  console.log('  ✓ 01-executive-dashboard.png');

  // Scroll to see AR/AP aging charts
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight - 800));
  await page.waitForTimeout(1000);
  await page.screenshot({ path: join(OUT, '02-aging-charts.png'), fullPage: false });
  console.log('  ✓ 02-aging-charts.png');

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error(e); process.exit(1); });
