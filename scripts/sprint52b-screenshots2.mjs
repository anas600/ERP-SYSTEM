// Sprint 52b — Take screenshots with wider date range
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
  if (!hasToken) {
    console.log('Login failed - aborting');
    await browser.close();
    return;
  }

  // Get a vendor with bills (the second or third one usually has more)
  const apResp = await page.evaluate(async () => {
    const token = localStorage.getItem('accessToken');
    const r = await fetch('http://localhost:5001/api/procurement/ap-aging?asOf=2026-08-07', {
      headers: { Authorization: 'Bearer ' + token, 'X-Company-Id': '00000000-0000-0000-0000-000000000001' }
    });
    return await r.json();
  });
  // Use the SECOND vendor (شركة الإمداد الذهبي — has 43,050)
  const vendor = apResp.vendors?.[1];
  console.log('Vendor:', vendor?.vendorName);

  if (vendor) {
    // Vendor statement — set wide date range first (2025)
    await page.goto(URL + '/procurement/vendors/' + vendor.vendorId + '/statement', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);
    // Clear and set wide range
    const fromInput = page.locator('input[type="date"]').first();
    const toInput = page.locator('input[type="date"]').nth(1);
    await fromInput.fill('2025-01-01');
    await toInput.fill('2026-08-07');
    await page.click('button:has-text("تطبيق")');
    await page.waitForTimeout(3000);
    await page.screenshot({ path: OUT + '/08-vendor-statement-2025.png', fullPage: true });
    console.log('✓ Vendor statement 2025 (default tab)');

    // Bills tab
    const billsTab = page.locator('button:has-text("الفواتير")').first();
    await billsTab.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: OUT + '/09-vendor-bills-2025.png', fullPage: true });
    console.log('✓ Vendor bills tab 2025');

    // Payments tab
    const payTab = page.locator('button:has-text("المدفوعات")').first();
    await payTab.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: OUT + '/10-vendor-payments-2025.png', fullPage: true });
    console.log('✓ Vendor payments tab 2025');
  }

  // Customer statement with wide range
  const arResp = await page.evaluate(async () => {
    const token = localStorage.getItem('accessToken');
    const r = await fetch('http://localhost:5001/api/ar/aging?asOfDate=2026-08-07', {
      headers: { Authorization: 'Bearer ' + token, 'X-Company-Id': '00000000-0000-0000-0000-000000000001' }
    });
    return await r.json();
  });
  const customer = arResp.rows?.[0];
  if (customer) {
    await page.goto(URL + '/finance/customers/' + customer.customerId + '/statement', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);
    const fromInput = page.locator('input[type="date"]').first();
    const toInput = page.locator('input[type="date"]').nth(1);
    await fromInput.fill('2025-01-01');
    await toInput.fill('2026-08-07');
    await page.click('button:has-text("تطبيق")');
    await page.waitForTimeout(3000);
    await page.screenshot({ path: OUT + '/11-customer-statement-2025.png', fullPage: true });
    console.log('✓ Customer statement 2025 (default tab)');

    // Invoices tab
    const invTab = page.locator('button:has-text("الفواتير")').first();
    await invTab.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: OUT + '/12-customer-invoices-2025.png', fullPage: true });
    console.log('✓ Customer invoices tab 2025');

    // Receipts tab
    const recTab = page.locator('button:has-text("المقبوضات")').first();
    await recTab.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: OUT + '/13-customer-receipts-2025.png', fullPage: true });
    console.log('✓ Customer receipts tab 2025');
  }

  await browser.close();
  console.log('Done.');
}

main().catch((e) => { console.error('FATAL:', e); process.exit(1); });
