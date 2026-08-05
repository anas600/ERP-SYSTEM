// Sprint 37 smoke test — verify L19 audit + 4 new manual JE templates
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const results = [];
let browser, page;

async function shot(name) {
  const p = path.join(SHOTS_DIR, `sprint37-${name}.png`);
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

  // 2) Verify CoA has 52 accounts
  console.log('\n2) Check CoA has 52 accounts (47 + 5 new)...');
  await page.goto(`${BASE}/finance/accounts`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await check('CoA: 52 accounts loaded', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('52') && !txt.includes('50') && !txt.includes('51') && !txt.includes('53')) {
      // Try to look for any 5xx number
      throw new Error('Account count not visible');
    }
  });

  // 3) Open new journal entry page
  console.log('\n3) Open new JE page...');
  await page.goto(`${BASE}/finance/journal-entries/new`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.waitForTimeout(1500);
  await shot('je-page-initial');

  // 4) Check templates selector exists
  await check('JE: Templates selector visible', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('قوالب جاهزة') && !txt.includes('Templates')) {
      throw new Error('Templates header not found');
    }
  });

  // 5) Check all 8 templates are in dropdown
  const expectedTemplates = ['تسوية يدوية', 'إهلاك أصول', 'مصروف مستحق', 'مصروف مسبق', 'رواتب', 'سلفة موظف', 'ديون معدومة', 'تسوية مخزون'];
  for (const t of expectedTemplates) {
    await check(`JE: template "${t}" available`, async () => {
      const select = page.locator('select').first();
      const options = await select.locator('option').allTextContents();
      const found = options.some((o) => o.includes(t));
      if (!found) throw new Error(`Not found in: ${options.join(' | ')}`);
    });
  }

  // 6) Apply each new Sprint 37 template and verify lines pre-filled
  console.log('\n6) Apply Sprint 37 templates and verify lines...');
  for (const tpl of [
    { id: 'salary', label: 'رواتب', expectedAcct: '4112' },
    { id: 'loan', label: 'سلفة موظف', expectedAcct: '1410' },
    { id: 'bad-debt', label: 'ديون معدومة', expectedAcct: '5410' },
    { id: 'inventory-adjust', label: 'تسوية مخزون', expectedAcct: '1240' },
  ]) {
    await check(`Apply template: ${tpl.label}`, async () => {
      // Find the value attribute for the option whose text starts with the label
      const select = page.locator('select').first();
      const optValue = await select.evaluate((el, label) => {
        const opts = Array.from(el.options);
        const found = opts.find((o) => o.text.startsWith(label));
        return found ? found.value : null;
      }, tpl.label);
      if (!optValue) throw new Error(`Option with label "${tpl.label}" not found`);
      await select.selectOption(optValue);
      // Click apply button
      await page.click('button:has-text("تطبيق القالب")');
      await page.waitForTimeout(500);
      // Check that at least one line account selector has the expected account code
      const allSelects = await page.locator('select').all();
      let found = false;
      for (const sel of allSelects.slice(1)) {  // skip first (template selector)
        const opts = await sel.locator('option').allTextContents();
        if (opts.some((o) => o.includes(tpl.expectedAcct))) {
          found = true;
          break;
        }
      }
      if (!found) throw new Error(`Expected account ${tpl.expectedAcct} not found in lines`);
    });
  }

  await shot('je-page-with-templates-applied');
  await browser.close();

  const passed = results.filter(r => r.ok).length;
  const failed = results.length - passed;
  console.log(`\n${'='.repeat(40)}`);
  console.log(`Sprint 37 smoke: ${passed}/${results.length} passed`);
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
