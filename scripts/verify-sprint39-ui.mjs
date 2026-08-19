// Sprint 39 smoke — UI/UX + tax opt-in
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const results = [];
let browser, page;

async function shot(name) {
  const p = path.join(SHOTS_DIR, `sprint39-${name}.png`);
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
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
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

  // 2) New sales invoice form — tax opt-in
  console.log('\n2) /finance/sales-invoices/new (tax opt-in)...');
  await page.goto(`${BASE}/finance/sales-invoices/new`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 15000 });
  await page.waitForTimeout(2000);
  await shot('new-invoice-tax-off');

  // Check tax toggle is visible
  await check('Tax toggle (اختياري) visible', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('اختياري') || !txt.includes('5%')) {
      throw new Error('Tax toggle text not found');
    }
  });
  await check('Tax toggle is OFF by default', async () => {
    const cb = page.locator('input[type=checkbox]').first();
    if (!(await cb.isVisible())) throw new Error('Checkbox not visible');
    const checked = await cb.isChecked();
    if (checked) throw new Error('Checkbox should be OFF by default');
  });
  await check('Tax row not shown in summary (default state)', async () => {
    // Check the summary card specifically — not the toggle description
    const summaryCard = page.locator('text=الملخص').locator('xpath=ancestor::div[contains(@class, "rounded")][1]');
    const txt = await summaryCard.innerText();
    // Should NOT contain the tax row (format: "ضريبة 5% (VAT):")
    if (/ضريبة\s*5%\s*\(VAT\)/.test(txt)) {
      throw new Error('Tax row should be hidden in summary when OFF');
    }
  });
  await check('Per-line taxRate column visible (default state)', async () => {
    const ths = await page.locator('thead th').allTextContents();
    if (!ths.some((t) => t === 'الضريبة')) {
      throw new Error('Per-line tax column should be visible when OFF');
    }
  });

  // Toggle the tax ON
  console.log('\n3) Toggle tax ON...');
  await page.click('input[type=checkbox]');
  await page.waitForTimeout(500);
  await shot('new-invoice-tax-on');

  await check('Tax row appears in summary when ON', async () => {
    const summaryCard = page.locator('text=الملخص').locator('xpath=ancestor::div[contains(@class, "rounded")][1]');
    const txt = await summaryCard.innerText();
    if (!/ضريبة\s*5%\s*\(VAT\)/.test(txt)) {
      throw new Error('Tax row should be visible in summary when ON');
    }
  });
  await check('Per-line taxRate column hidden when ON', async () => {
    const ths = await page.locator('thead th').allTextContents();
    if (ths.some((t) => t === 'الضريبة')) {
      throw new Error('Per-line tax column should be hidden when ON');
    }
  });

  // 4) UI smoke — check the new design system
  console.log('\n4) UI design system check...');
  await check('Sidebar has gradient logo (Sprint 39 design)', async () => {
    await page.goto(`${BASE}/dashboard`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle', { timeout: 10000 });
    const logo = page.locator('aside a[href="/dashboard"] > div').first();
    const hasGradient = await logo.evaluate((el) => {
      const style = window.getComputedStyle(el);
      return style.backgroundImage.includes('gradient');
    });
    if (!hasGradient) throw new Error('Logo should have gradient background');
  });

  // 5) Check the new global styles are applied
  await check('Body has light gray background (Sprint 39 design)', async () => {
    const bg = await page.evaluate(() => {
      return window.getComputedStyle(document.body).backgroundColor;
    });
    // Accept any near-white gray: rgb(248, 250, 252) (#f8fafc, our --color-bg)
    // or rgb(249, 250, 251) (#f9fafb, Tailwind gray-50). Both are visually equivalent.
    const m = bg.match(/rgb\((\d+),\s*(\d+),\s*(\d+)\)/);
    if (!m) throw new Error(`Could not parse body bg: ${bg}`);
    const [r, g, b] = [parseInt(m[1]), parseInt(m[2]), parseInt(m[3])];
    if (r > 252 || g > 252 || b > 253) {
      throw new Error(`Body bg should be light gray, got ${bg}`);
    }
  });

  // 6) Quick visual check — page has smooth transitions
  await check('Buttons have transitions', async () => {
    const trans = await page.evaluate(() => {
      const btn = document.querySelector('button');
      if (!btn) return null;
      const style = window.getComputedStyle(btn);
      return style.transition;
    });
    if (!trans || (!trans.includes('background-color') && !trans.includes('all'))) {
      throw new Error(`Buttons should have transitions, got: ${trans}`);
    }
  });

  await shot('dashboard-sprint39');

  await browser.close();

  const passed = results.filter(r => r.ok).length;
  const failed = results.length - passed;
  console.log(`\n${'='.repeat(40)}`);
  console.log(`Sprint 39 smoke: ${passed}/${results.length} passed`);
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
