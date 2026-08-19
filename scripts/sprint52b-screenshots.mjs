// Sprint 52b — Screenshots for the 3 fixes
import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

const URL = 'http://localhost:3000';
const OUT = 'docs/screenshots/sprint-52b';

async function main() {
  await mkdir(OUT, { recursive: true });
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  page.on('pageerror', (err) => console.log('PAGE ERROR:', err.message));

  // Login
  await page.goto(URL + '/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(8000);

  const hasToken = await page.evaluate(() => !!localStorage.getItem('accessToken'));
  console.log('Has token:', hasToken);
  if (!hasToken) {
    console.log('Login failed - aborting');
    await browser.close();
    return;
  }

  // Get account 1210 ID
  const accId = '7f64fb76-8eff-4916-a412-d0506bda70cf'; // from earlier psql

  // 1. GL page with account 1210 cash (via URL)
  await page.goto(URL + '/finance/reports/general-ledger?accountId=' + accId, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);
  await page.screenshot({ path: OUT + '/01-gl-cash.png', fullPage: true });
  console.log('✓ GL page with cash account');

  // 2. Aging summary
  await page.goto(URL + '/finance/reports/aging-summary', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);
  await page.screenshot({ path: OUT + '/02-aging-summary.png', fullPage: true });
  console.log('✓ Aging summary');

  // 3. Find a vendor and go to its statement — use direct URL with the first vendor ID
  // First, get the first vendor ID
  const apResp = await page.evaluate(async () => {
    const token = localStorage.getItem('accessToken');
    const r = await fetch('http://localhost:5001/api/procurement/ap-aging?asOf=2026-08-07', {
      headers: { Authorization: 'Bearer ' + token, 'X-Company-Id': '00000000-0000-0000-0000-000000000001' }
    });
    return await r.json();
  });
  const firstVendor = apResp.vendors?.[0];
  if (firstVendor) {
    await page.goto(URL + '/procurement/vendors/' + firstVendor.vendorId + '/statement', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(4000);
    await page.screenshot({ path: OUT + '/03-vendor-statement.png', fullPage: true });
    console.log('✓ Vendor statement (default tab)');

    // 4. Click bills tab
    const billsTab = page.locator('button:has-text("الفواتير")').first();
    if (await billsTab.count() > 0) {
      await billsTab.click();
      await page.waitForTimeout(1500);
      await page.screenshot({ path: OUT + '/04-vendor-bills.png', fullPage: true });
      console.log('✓ Vendor bills tab');
    }

    // 5. Click payments tab
    const payTab = page.locator('button:has-text("المدفوعات")').first();
    if (await payTab.count() > 0) {
      await payTab.click();
      await page.waitForTimeout(1500);
      await page.screenshot({ path: OUT + '/05-vendor-payments.png', fullPage: true });
      console.log('✓ Vendor payments tab');
    }
  }

  // 6. Customer statement - get first customer
  const arResp = await page.evaluate(async () => {
    const token = localStorage.getItem('accessToken');
    const r = await fetch('http://localhost:5001/api/ar/aging?asOfDate=2026-08-07', {
      headers: { Authorization: 'Bearer ' + token, 'X-Company-Id': '00000000-0000-0000-0000-000000000001' }
    });
    return await r.json();
  });
  const firstCustomer = arResp.rows?.[0];
  if (firstCustomer) {
    await page.goto(URL + '/finance/customers/' + firstCustomer.customerId + '/statement', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(4000);
    await page.screenshot({ path: OUT + '/06-customer-statement.png', fullPage: true });
    console.log('✓ Customer statement');

    // 7. Click invoices tab
    const invTab = page.locator('button:has-text("الفواتير")').first();
    if (await invTab.count() > 0) {
      await invTab.click();
      await page.waitForTimeout(1500);
      await page.screenshot({ path: OUT + '/07-customer-invoices.png', fullPage: true });
      console.log('✓ Customer invoices tab');
    }
  }

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error('FATAL:', e); process.exit(1); });
