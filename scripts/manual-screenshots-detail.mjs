// Take additional detail-page screenshots
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const MANUAL_DIR = 'C:\\Users\\Anas\\.minimax-agent\\projects\\user-manual-assets';
if (!fs.existsSync(MANUAL_DIR)) fs.mkdirSync(MANUAL_DIR, { recursive: true });

const BASE = 'http://localhost:3000';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();

  // Login
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/);
  console.log('✓ Logged in');

  // Customer detail (first customer)
  console.log('\nCustomer detail...');
  await page.goto(`${BASE}/finance/customers`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  // Click on the first customer's name link
  const firstCustomer = page.locator('table tbody tr a').first();
  if (await firstCustomer.count() > 0) {
    await firstCustomer.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-customer-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Customer statement
  console.log('\nCustomer statement...');
  const stmtLink = page.locator('a:has-text("كشف حساب")').first();
  if (await stmtLink.count() > 0) {
    await stmtLink.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-customer-statement.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Sales invoice detail
  console.log('\nSales invoice detail...');
  await page.goto(`${BASE}/finance/sales-invoices`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const firstInvoice = page.locator('table tbody tr a').first();
  if (await firstInvoice.count() > 0) {
    await firstInvoice.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-sales-invoice-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Sales invoice new (tax off + tax on)
  console.log('\nSales invoice new (tax on)...');
  await page.goto(`${BASE}/finance/sales-invoices/new`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  // Toggle tax on
  const toggle = page.locator('input[type=checkbox]').first();
  if (await toggle.count() > 0) {
    await toggle.click();
    await page.waitForTimeout(800);
    const filename = 'manual-sales-invoice-new-tax-on.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Journal entry detail
  console.log('\nJournal entry detail...');
  await page.goto(`${BASE}/finance/journal-entries`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const firstJE = page.locator('a:has(button:has-text("عرض"))').first();
  if (await firstJE.count() > 0) {
    await firstJE.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-journal-entry-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Vendor detail
  console.log('\nVendor detail...');
  await page.goto(`${BASE}/procurement/vendors`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const firstVendor = page.locator('table tbody tr a').first();
  if (await firstVendor.count() > 0) {
    await firstVendor.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-vendor-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Employee detail
  console.log('\nEmployee detail...');
  await page.goto(`${BASE}/hr/employees`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const firstEmployee = page.locator('table tbody tr a').first();
  if (await firstEmployee.count() > 0) {
    await firstEmployee.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-employee-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Project detail
  console.log('\nProject detail...');
  await page.goto(`${BASE}/projects`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const firstProject = page.locator('table tbody tr a, table tbody tr').first();
  if (await firstProject.count() > 0) {
    await firstProject.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-project-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Item detail
  console.log('\nItem detail...');
  await page.goto(`${BASE}/inventory/items`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const firstItem = page.locator('table tbody tr a').first();
  if (await firstItem.count() > 0) {
    await firstItem.click();
    await page.waitForTimeout(2000);
    const filename = 'manual-item-detail.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // User menu open
  console.log('\nUser menu open...');
  await page.goto(`${BASE}/dashboard`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1500);
  const userBtn = page.locator('header button:has(div.bg-gradient-to-br)').first();
  if (await userBtn.count() > 0) {
    await userBtn.click();
    await page.waitForTimeout(500);
    const filename = 'manual-user-menu.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  // Confirm dialog open
  console.log('\nConfirm dialog...');
  await page.goto(`${BASE}/finance/receipts`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(2000);
  const reverseBtn = page.locator('button[title="عكس"]').first();
  if (await reverseBtn.count() > 0) {
    await reverseBtn.click();
    await page.waitForTimeout(500);
    const filename = 'manual-confirm-dialog.png';
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    fs.copyFileSync(path.join(SHOTS_DIR, filename), path.join(MANUAL_DIR, filename));
    console.log(`  📸 ${filename}`);
  }

  await browser.close();
  console.log('\n✓ Detail screenshots done.');
})().catch((e) => {
  console.error(`FATAL: ${e.message}`);
  process.exit(1);
});
