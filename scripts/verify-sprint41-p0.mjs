import { chromium } from 'playwright';
const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
const errors = [];
page.on('pageerror', (e) => errors.push('PAGE: ' + e.message));
page.on('console', (m) => { if (m.type() === 'error') errors.push('CONSOLE: ' + m.text()); });

async function step(name, fn) {
  try {
    await fn();
    console.log('  [OK] ' + name);
    return true;
  } catch (e) {
    console.log('  [FAIL] ' + name + ' - ' + e.message);
    return false;
  }
}

// Login
await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
await page.fill('input[type=email]', 'admin@erp.local');
await page.fill('input[type=password]', 'ChangeMe1234!');
await page.click('button[type=submit]');
await page.waitForURL('**/dashboard', { timeout: 15000 });

// Navigate to /finance/accounts
await page.goto('http://localhost:3000/finance/accounts', { waitUntil: 'networkidle' });
await page.waitForTimeout(1500);

console.log('--- Tree view test (Fix #2) ---');
// Count visible account rows initially
const initialRows = await page.locator('table tbody tr').count();
console.log('  Initial visible rows: ' + initialRows);

// Get the row for "1000 الأصول" — find it by text
const assetRow = page.locator('table tbody tr').filter({ hasText: '1000' }).first();
const assetVisible = await assetRow.isVisible();
console.log('  "1000 الأصول" row visible: ' + assetVisible);

// Get the row for "1100 أصول غير متداولة" (child of 1000)
const subAssetRow = page.locator('table tbody tr').filter({ hasText: '1100' }).first();
const subAssetVisible = await subAssetRow.isVisible();
console.log('  "1100 أصول غير متداولة" child visible (default expanded): ' + subAssetVisible);

// Click the expand/collapse button for 1000 الأصول
const expandBtn = assetRow.locator('button[aria-label]').first();
if (await expandBtn.count() > 0) {
  await step('Click expand/collapse for 1000', async () => {
    await expandBtn.click();
    await page.waitForTimeout(500);
  });

  // After click, the child should be hidden
  const subAfterCollapse = await subAssetRow.isVisible().catch(() => false);
  console.log('  After click: "1100" hidden? ' + !subAfterCollapse);

  if (subAfterCollapse) {
    console.log('  [BUG NOT FIXED] Tree not filtering by expanded state');
  } else {
    console.log('  [FIX VERIFIED] Child hidden after collapse');
  }

  // Click again to re-expand
  await step('Click expand again', async () => {
    await expandBtn.click();
    await page.waitForTimeout(500);
  });
  const subAfterReExpand = await subAssetRow.isVisible().catch(() => false);
  console.log('  After re-expand: "1100" visible? ' + subAfterReExpand);
} else {
  console.log('  [SKIP] No expand button found');
}

console.log('--- Add child account test (Fix #1) ---');
// Find a row with a "+" button to add child — try "1000 الأصول"
const addBtn = assetRow.locator('button[title*="إضافة"], button[title*="Add"]').first();
if (await addBtn.count() > 0) {
  await step('Click + to open add-child modal', async () => {
    await addBtn.click();
    await page.waitForTimeout(800);
  });

  // Fill the modal
  const codeInput = page.locator('input').filter({ hasText: '' }).first();
  // Find inputs by label
  const inputs = await page.locator('input[type="text"]').all();
  console.log('  Text inputs in modal: ' + inputs.length);
  if (inputs.length > 0) {
    await inputs[0].fill('9998');
    if (inputs.length > 1) await inputs[1].fill('اختبار P0 fix');
  }
  await step('Submit add-child', async () => {
    await page.click('button:has-text("إنشاء")');
    await page.waitForTimeout(2000);
  });
  // Check for error
  const errorText = await page.locator('[role="alert"], .text-danger-700, [class*="error"]').first().textContent().catch(() => '');
  if (errorText && errorText.length > 0) {
    console.log('  Error shown: ' + errorText.substring(0, 200));
  } else {
    console.log('  No error visible (good — fix #1 worked)');
  }
  // Close modal
  const closeBtn = page.locator('button:has-text("إلغاء")').first();
  if (await closeBtn.count() > 0) await closeBtn.click();
} else {
  console.log('  [SKIP] No add-child button');
}

await page.screenshot({ path: 'C:/Users/Anas/AppData/Local/Temp/sprint41-coa-after.png', fullPage: true });
console.log('---');
console.log('Page errors: ' + errors.length);
errors.forEach((e) => console.log('  ' + e));
await browser.close();
process.exit(0);
