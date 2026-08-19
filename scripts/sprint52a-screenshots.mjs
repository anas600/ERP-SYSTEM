// Sprint 52a — Take screenshots of the new 4-level CoA tree page.
import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

const URL = 'http://localhost:3000';
const OUT = 'docs/screenshots/sprint-52a';

async function main() {
  await mkdir(OUT, { recursive: true });
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Listen for console errors
  page.on('console', (msg) => {
    if (msg.type() === 'error') console.log('CONSOLE ERROR:', msg.text());
  });
  page.on('pageerror', (err) => console.log('PAGE ERROR:', err.message));

  // Login
  await page.goto(URL + '/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(6000);
  console.log('URL after login:', page.url());

  const token = await page.evaluate(() => localStorage.getItem('accessToken'));
  console.log('Token in localStorage:', token ? token.substring(0, 50) + '...' : 'NONE');

  // Navigate to coa-tree
  await page.goto(URL + '/finance/accounts-tree', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);
  console.log('CoA tree URL:', page.url());
  await page.screenshot({ path: OUT + '/01-coa-tree.png', fullPage: true });
  console.log('✓ 01-coa-tree.png');

  // Expand all
  try {
    await page.click('button:has-text("فتح الكل")', { timeout: 2000 });
    await page.waitForTimeout(500);
    await page.screenshot({ path: OUT + '/02-coa-tree-expanded.png', fullPage: true });
    console.log('✓ 02-coa-tree-expanded.png');
  } catch (e) {
    console.log('Could not click expand-all button');
  }

  // Search test
  try {
    await page.fill('input[placeholder*="بحث"]', '1200');
    await page.waitForTimeout(500);
    await page.screenshot({ path: OUT + '/03-coa-tree-search.png', fullPage: true });
    console.log('✓ 03-coa-tree-search.png');
  } catch (e) {
    console.log('Could not search');
  }

  // Dashboard
  await page.goto(URL + '/dashboard', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: OUT + '/04-dashboard.png', fullPage: true });
  console.log('✓ 04-dashboard.png');

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error('FATAL:', e); process.exit(1); });
