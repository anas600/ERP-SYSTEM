// Sprint 36 smoke test — verify the 3 new pages render correctly
// Usage: node scripts/verify-sprint36.mjs
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const results = [];
let browser, context, page;

async function shot(name) {
  const p = path.join(SHOTS_DIR, `sprint36-${name}.png`);
  await page.screenshot({ path: p, fullPage: true });
  console.log(`  📸 ${name}.png`);
}

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
  browser = await chromium.launch({ headless: true });
  context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  page = await context.newPage();
  page.on('pageerror', (e) => console.log(`  [pageerror] ${e.message}`));

  // 1) Login
  console.log('\n1) Login as admin...');
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]', { timeout: 10000 });
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/, { timeout: 10000 });
  console.log('  ✓ Logged in');

  // 2) Trial Balance page
  console.log('\n2) /finance/trial-balance...');
  await page.goto(`${BASE}/finance/trial-balance`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 15000 });
  await check('TB: balanced bar visible', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('ميزان متوازن') && !txt.includes('ميزان غير متوازن')) {
      throw new Error('Balanced bar not visible');
    }
  });
  await check('TB: 30 accounts shown', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('30')) throw new Error('No "30" count visible');
  });
  await shot('trial-balance');

  // 3) Customer list → click "كشف حساب"
  console.log('\n3) Customer list → statement link...');
  await page.goto(`${BASE}/finance/customers`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await check('Customer list: has كشف حساب link', async () => {
    const link = page.locator('a:has-text("كشف حساب")').first();
    if (!(await link.isVisible())) throw new Error('No كشف حساب link found');
  });
  await shot('customers-list-with-link');

  // 4) Click first customer statement link
  console.log('\n4) Open customer statement...');
  await page.click('a:has-text("كشف حساب")');
  await page.waitForLoadState('networkidle', { timeout: 15000 });
  // Wait for actual content (not skeleton) — look for "الرصيد الافتتاحي" text
  try {
    await page.waitForSelector('text=الرصيد الافتتاحي', { timeout: 10000 });
  } catch (e) {
    console.log('  [warn] الرصيد الافتتاحي not found within 10s');
  }
  await check('Customer Stmt: 4 summary cards', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('الرصيد الافتتاحي')) throw new Error('No opening balance card');
    if (!txt.includes('إجمالي الفواتير')) throw new Error('No total invoiced card');
    if (!txt.includes('إجمالي المقبوضات')) throw new Error('No total received card');
    if (!txt.includes('الرصيد الختامي')) throw new Error('No closing balance card');
  });
  await shot('customer-statement');

  // 5) Vendor list → statement
  console.log('\n5) Vendor list → statement...');
  await page.goto(`${BASE}/procurement/vendors`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await check('Vendor list: has كشف حساب link', async () => {
    const link = page.locator('a:has-text("كشف حساب")').first();
    if (!(await link.isVisible())) throw new Error('No كشف حساب link found');
  });
  await shot('vendors-list-with-link');

  await page.click('a:has-text("كشف حساب")');
  await page.waitForLoadState('networkidle', { timeout: 15000 });
  try {
    await page.waitForSelector('text=الرصيد الافتتاحي', { timeout: 10000 });
  } catch (e) {
    console.log('  [warn] الرصيد الافتتاحي not found within 10s');
  }
  await check('Vendor Stmt: 4 summary cards', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('الرصيد الافتتاحي')) throw new Error('No opening balance card');
    if (!txt.includes('إجمالي الفواتير')) throw new Error('No total billed card');
    if (!txt.includes('إجمالي المدفوعات')) throw new Error('No total paid card');
    if (!txt.includes('الرصيد الختامي')) throw new Error('No closing balance card');
  });
  await shot('vendor-statement');

  await browser.close();

  const passed = results.filter(r => r.ok).length;
  const failed = results.length - passed;
  console.log(`\n${'='.repeat(40)}`);
  console.log(`Sprint 36 smoke: ${passed}/${results.length} passed`);
  if (failed > 0) {
    console.log(`\nFailed:`);
    results.filter(r => !r.ok).forEach(r => console.log(`  - ${r.name}: ${r.error}`));
    process.exit(1);
  } else {
    console.log('✓ All checks passed');
  }
})().catch((e) => {
  console.error(`FATAL: ${e.message}`);
  process.exit(1);
});
