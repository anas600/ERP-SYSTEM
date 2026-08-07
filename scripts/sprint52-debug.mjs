// Debug — what HTML is being served?
import { chromium } from 'playwright';

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  // Login
  await page.goto('http://localhost:3000/login');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/\/dashboard/);

  // Trial Balance
  await page.goto('http://localhost:3000/finance/trial-balance');
  await page.waitForLoadState('networkidle');

  // Inspect first row
  const row = page.locator('table tbody tr').first();
  const html = await row.evaluate(el => el.outerHTML);
  console.log('First row HTML:');
  console.log(html.substring(0, 500));
  console.log('...');

  // Check for onClick attribute (in React, the click handler is in JS, not HTML)
  const hasOnClick = await row.evaluate(el => {
    // React 14+ uses internal __reactProps
    const keys = Object.keys(el);
    return keys.filter(k => k.startsWith('__reactProps') || k.startsWith('__reactEventHandlers'));
  });
  console.log('\nReact handlers:', hasOnClick);

  // Try clicking directly on the row
  console.log('\nClicking row...');
  await row.click();
  await page.waitForTimeout(2000);
  console.log('URL after click:', page.url());

  await browser.close();
}

main().catch((e) => { console.error('ERROR:', e); process.exit(1); });
