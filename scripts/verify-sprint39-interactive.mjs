// Sprint 39 — Interactive flow tests
// Tests: click on a journal entry to view detail, navigate to a customer, etc.
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const results = [];

async function check(name, fn) {
  try {
    await fn();
    results.push({ name, ok: true });
    console.log(`  ✓ ${name}`);
  } catch (e) {
    results.push({ name, ok: false, error: e.message });
    console.log(`  ✗ ${name}: ${e.message}`);
  }
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();

  page.on('pageerror', (e) => console.log(`  [pageerror] ${e.message}`));

  // 1) Login
  console.log('1) Login...');
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/);
  console.log('  ✓ Logged in');

  // 2) Open a journal entry
  console.log('\n2) Click first journal entry...');
  await page.goto(`${BASE}/finance/journal-entries`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  await check('Journal entries list loaded', async () => {
    const cards = await page.locator('h3.font-bold').count();
    if (cards < 1) throw new Error('No journal entries shown');
  });
  await check('Click first journal entry navigates to detail', async () => {
    // Click the first "عرض" (view) button
    const viewBtn = page.locator('a:has(button:has-text("عرض"))').first();
    await viewBtn.click();
    await page.waitForURL(/.*\/finance\/journal-entries\/[a-f0-9-]+/, { timeout: 10000 });
  });
  await page.waitForTimeout(1500);
  await check('Journal entry detail shows lines', async () => {
    const lines = await page.locator('table tbody tr').count();
    if (lines < 1) throw new Error('No lines shown in journal entry detail');
  });
  await page.screenshot({ path: path.join(SHOTS_DIR, 'sprint39-je-detail.png'), fullPage: true });
  console.log('  📸 sprint39-je-detail.png');

  // 3) Open a customer
  console.log('\n3) Customer statement flow...');
  await page.goto(`${BASE}/finance/customers`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  await check('Customers loaded', async () => {
    const rows = await page.locator('table tbody tr').count();
    if (rows < 1) throw new Error('No customers shown');
  });
  await check('Click "كشف حساب" navigates to statement', async () => {
    const stmtLink = page.locator('a:has-text("كشف حساب")').first();
    await stmtLink.click();
    await page.waitForURL(/.*\/statement/, { timeout: 10000 });
  });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(SHOTS_DIR, 'sprint39-customer-statement.png'), fullPage: true });
  console.log('  📸 sprint39-customer-statement.png');

  // 4) User menu
  console.log('\n4) User menu interaction...');
  await page.goto(`${BASE}/dashboard`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1500);
  await check('User menu opens', async () => {
    const userBtn = page.locator('header button:has(div.bg-gradient-to-br)').first();
    await userBtn.click();
    await page.waitForTimeout(500);
    // Menu should show "تسجيل الخروج" link
    const logout = await page.locator('button:has-text("تسجيل الخروج")').count();
    if (logout < 1) throw new Error('Logout button not visible in menu');
  });
  await page.screenshot({ path: path.join(SHOTS_DIR, 'sprint39-user-menu.png'), fullPage: true });
  console.log('  📸 sprint39-user-menu.png');

  // 5) Receipts ConfirmDialog
  console.log('\n5) Receipts ConfirmDialog...');
  await page.goto(`${BASE}/finance/receipts`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  await check('Receipts table loaded', async () => {
    const rows = await page.locator('table tbody tr').count();
    if (rows < 1) throw new Error('No receipts shown');
  });
  await check('ConfirmDialog opens on reverse click', async () => {
    // Find a posted receipt (has reverse button)
    const reverseBtn = page.locator('button[title="عكس"]').first();
    if ((await reverseBtn.count()) < 1) throw new Error('No reverse button found');
    await reverseBtn.click();
    await page.waitForTimeout(500);
    const dialog = await page.locator('div[role="dialog"]').count();
    if (dialog < 1) throw new Error('ConfirmDialog did not open');
  });
  await page.screenshot({ path: path.join(SHOTS_DIR, 'sprint39-confirm-dialog.png'), fullPage: true });
  console.log('  📸 sprint39-confirm-dialog.png');

  await browser.close();

  const passed = results.filter(r => r.ok).length;
  const failed = results.length - passed;
  console.log(`\n${'='.repeat(40)}`);
  console.log(`Sprint 39 interactive: ${passed}/${results.length} passed`);
  if (failed > 0) {
    results.filter(r => !r.ok).forEach(r => console.log(`  - ${r.name}: ${r.error}`));
    process.exit(1);
  } else {
    console.log('✓ All interactive flows passed');
  }
})().catch((e) => {
  console.error(`FATAL: ${e.message}`);
  process.exit(1);
});
