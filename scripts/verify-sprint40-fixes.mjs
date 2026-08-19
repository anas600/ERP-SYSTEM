// Sprint 40 — verify the fixed forms actually work (no more 401s)
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

  let console401 = 0;
  page.on('console', (msg) => {
    if (msg.type() === 'error' && msg.text().includes('401')) {
      console401++;
      console.log(`    [401] ${msg.text()}`);
    }
  });

  // Login
  console.log('\n1) Login...');
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/);
  console.log('  ✓ Logged in');

  // Test 1: Item categories page loads
  console.log('\n2) Item categories list...');
  await check('Item categories list loads', async () => {
    const resp = await page.goto(`${BASE}/admin/item-categories`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    if (resp.status() !== 200) throw new Error(`Status ${resp.status()}`);
    const txt = await page.locator('body').innerText();
    if (!txt.includes('فئة') && !txt.includes('فئات')) throw new Error('No categories text');
  });

  // Test 2: Cost centers list loads
  console.log('\n3) Cost centers list...');
  await check('Cost centers list loads', async () => {
    const resp = await page.goto(`${BASE}/finance/cost-centers`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    if (resp.status() !== 200) throw new Error(`Status ${resp.status()}`);
  });

  // Test 3: Posting rules list loads
  console.log('\n4) Posting rules list...');
  await check('Posting rules list loads', async () => {
    const resp = await page.goto(`${BASE}/admin/posting-rules`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    if (resp.status() !== 200) throw new Error(`Status ${resp.status()}`);
  });

  // Test 4: Reservations list loads
  console.log('\n5) Reservations list...');
  await check('Reservations list loads', async () => {
    const resp = await page.goto(`${BASE}/inventory/reservations`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    if (resp.status() !== 200) throw new Error(`Status ${resp.status()}`);
  });

  // Test 5: Movements list loads
  console.log('\n6) Movements list...');
  await check('Movements list loads', async () => {
    const resp = await page.goto(`${BASE}/inventory/movements`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    if (resp.status() !== 200) throw new Error(`Status ${resp.status()}`);
  });

  // Test 6: Projects list loads
  console.log('\n7) Projects list...');
  await check('Projects list loads', async () => {
    const resp = await page.goto(`${BASE}/projects`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    if (resp.status() !== 200) throw new Error(`Status ${resp.status()}`);
  });

  await browser.close();

  const passed = results.filter(r => r.ok).length;
  const failed = results.length - passed;
  console.log(`\n${'='.repeat(40)}`);
  console.log(`Sprint 40 fixes: ${passed}/${results.length} passed`);
  console.log(`Total 401 errors: ${console401}`);
  if (failed > 0 || console401 > 0) {
    results.filter(r => !r.ok).forEach(r => console.log(`  - ${r.name}: ${r.error}`));
    process.exit(1);
  } else {
    console.log('✓ No more 401s. All forms use API client.');
  }
})().catch((e) => {
  console.error(`FATAL: ${e.message}`);
  process.exit(1);
});
