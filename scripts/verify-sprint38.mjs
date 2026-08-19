// Sprint 38 smoke test — L19 fixes + 4 new manual JE templates
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const results = [];
let browser, page;

async function shot(name) {
  const p = path.join(SHOTS_DIR, `sprint38-${name}.png`);
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

  // 2) Trial Balance should be L19-filtered (now 35 accounts instead of 30)
  console.log('\n2) /finance/trial-balance (L19 filtered)...');
  await page.goto(`${BASE}/finance/trial-balance`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 15000 });
  await page.waitForTimeout(2000);
  await check('TB: balanced bar visible', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('ميزان متوازن') && !txt.includes('ميزان غير متوازن')) {
      throw new Error('Balanced bar not visible');
    }
  });
  await check('TB: 35 accounts (was 30 before L19 fix)', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('35')) throw new Error('No "35" count visible');
  });

  // 3) JE list page should work
  console.log('\n3) /finance/journal-entries...');
  await page.goto(`${BASE}/finance/journal-entries`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await check('JE: list loaded', async () => {
    const txt = await page.locator('body').innerText();
    if (!txt.includes('قيد') && !txt.includes('القيود')) {
      throw new Error('No JEs text visible');
    }
  });

  // 4) Open new JE page and check 12 templates
  console.log('\n4) /finance/journal-entries/new (12 templates)...');
  await page.goto(`${BASE}/finance/journal-entries/new`, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.waitForTimeout(2000);
  const expectedTemplates = [
    'تسوية يدوية',         // Sprint 34
    'إهلاك أصول',          // Sprint 34
    'مصروف مستحق',         // Sprint 34
    'مصروف مسبق',          // Sprint 34
    'رواتب',                // Sprint 37
    'سلفة موظف',            // Sprint 37
    'ديون معدومة',          // Sprint 37
    'تسوية مخزون',          // Sprint 37
    'دفع ضريبة',            // Sprint 38 (NEW)
    'فروق عملة',            // Sprint 38 (NEW)
    'سحب رأس مال',          // Sprint 38 (NEW)
  ];
  for (const t of expectedTemplates) {
    await check(`JE: template "${t}" available`, async () => {
      const select = page.locator('select').first();
      const options = await select.locator('option').allTextContents();
      if (!options.some((o) => o.includes(t))) {
        throw new Error(`Not found in: ${options.join(' | ')}`);
      }
    });
  }

  // 5) Apply each Sprint 38 template
  console.log('\n5) Apply Sprint 38 templates...');
  for (const tpl of [
    { id: 'tax-payment', label: 'دفع ضريبة', expectedAcct: '4300' },
    { id: 'fx-gain', label: 'فروق عملة', expectedAcct: '1230' },
    { id: 'fx-loss', label: 'فروق عملة', expectedAcct: '4110' },
    { id: 'capital-withdrawal', label: 'سحب رأس مال', expectedAcct: '3100' },
  ]) {
    await check(`Apply template: ${tpl.label} (${tpl.id})`, async () => {
      const select = page.locator('select').first();
      const optValue = await select.evaluate((el, label) => {
        const opts = Array.from(el.options);
        const found = opts.find((o) => o.text.startsWith(label));
        return found ? found.value : null;
      }, tpl.label);
      if (!optValue) throw new Error(`Option with label "${tpl.label}" not found`);
      await select.selectOption(optValue);
      await page.click('button:has-text("تطبيق القالب")');
      await page.waitForTimeout(500);
      const allSelects = await page.locator('select').all();
      let found = false;
      for (const sel of allSelects.slice(1)) {
        const opts = await sel.locator('option').allTextContents();
        if (opts.some((o) => o.includes(tpl.expectedAcct))) {
          found = true;
          break;
        }
      }
      if (!found) throw new Error(`Expected account ${tpl.expectedAcct} not found in lines`);
    });
  }

  await shot('je-all-templates');
  await browser.close();

  const passed = results.filter(r => r.ok).length;
  const failed = results.length - passed;
  console.log(`\n${'='.repeat(40)}`);
  console.log(`Sprint 38 smoke: ${passed}/${results.length} passed`);
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
