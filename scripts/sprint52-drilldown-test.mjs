// Sprint 52 — Drill-down Test
// يتحقق من أن الضغط على صف في التقارير يوجه للـ General Ledger / Customer / Vendor

import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

const SCREENSHOTS_DIR = 'C:/Users/Anas/.minimax-agent/projects/ERP-Holding-sprint-21/docs/screenshots/sprint-52';

async function main() {
  await mkdir(SCREENSHOTS_DIR, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Login
  await page.goto('http://localhost:3000/login');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/\/dashboard/, { timeout: 10000 });
  console.log('✓ Logged in');

  let pass = 0, fail = 0;

  // ===== Test 1: Trial Balance → General Ledger =====
  console.log('\n1. Trial Balance → GL');
  await page.goto('http://localhost:3000/finance/trial-balance');
  await page.waitForLoadState('networkidle');
  const tbRow = page.locator('table tbody tr').first();
  const tbCode = await tbRow.locator('td').first().textContent();
  console.log(`   Clicking account ${tbCode}...`);
  await tbRow.click();
  await page.waitForTimeout(2500);
  if (page.url().includes('/general-ledger') && page.url().includes('accountId=')) {
    console.log('   ✅ SUCCESS → ' + page.url());
    pass++;
  } else {
    console.log('   ❌ FAILED → ' + page.url());
    fail++;
  }
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/drilldown-01-trial-balance-to-GL.png`, fullPage: true });

  // ===== Test 2: Balance Sheet → GL =====
  console.log('\n2. Balance Sheet → GL');
  await page.goto('http://localhost:3000/finance/reports/balance-sheet');
  await page.waitForLoadState('networkidle');
  const bsRow = page.locator('table tbody tr').first();
  const bsCode = await bsRow.locator('td').first().textContent();
  console.log(`   Clicking account ${bsCode} in Assets...`);
  await bsRow.click();
  await page.waitForTimeout(2500);
  if (page.url().includes('/general-ledger') && page.url().includes('accountId=')) {
    console.log('   ✅ SUCCESS → ' + page.url());
    pass++;
  } else {
    console.log('   ❌ FAILED → ' + page.url());
    fail++;
  }
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/drilldown-02-balance-sheet-to-GL.png`, fullPage: true });

  // ===== Test 3: Income Statement → GL =====
  console.log('\n3. Income Statement → GL');
  await page.goto('http://localhost:3000/finance/reports/income-statement');
  await page.waitForLoadState('networkidle');
  const isRow = page.locator('table tbody tr').first();
  const isCode = await isRow.locator('td').first().textContent();
  console.log(`   Clicking account ${isCode} in Revenue...`);
  await isRow.click();
  await page.waitForTimeout(2500);
  if (page.url().includes('/general-ledger') && page.url().includes('accountId=')) {
    console.log('   ✅ SUCCESS → ' + page.url());
    pass++;
  } else {
    console.log('   ❌ FAILED → ' + page.url());
    fail++;
  }
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/drilldown-03-income-statement-to-GL.png`, fullPage: true });

  // ===== Test 4: AR Aging → Customer =====
  console.log('\n4. AR Aging → Customer');
  await page.goto('http://localhost:3000/finance/aging-ar');
  await page.waitForLoadState('networkidle');
  const arRow = page.locator('table tbody tr').first();
  if (await arRow.count() > 0) {
    const arText = await arRow.textContent();
    console.log(`   Clicking customer: ${arText?.substring(0, 60)}...`);
    await arRow.click();
    await page.waitForTimeout(2500);
    if (page.url().includes('/finance/customers/')) {
      console.log('   ✅ SUCCESS → ' + page.url());
      pass++;
    } else {
      console.log('   ❌ FAILED → ' + page.url());
      fail++;
    }
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/drilldown-04-ar-to-customer.png`, fullPage: true });
  } else {
    console.log('   No AR customers');
  }

  // ===== Test 5: AP Aging → Vendor =====
  console.log('\n5. AP Aging → Vendor');
  await page.goto('http://localhost:3000/finance/reports/aging-summary');
  await page.waitForLoadState('networkidle');
  const apRow = page.locator('table tbody tr').first();
  if (await apRow.count() > 0) {
    const apText = await apRow.textContent();
    console.log(`   Clicking vendor: ${apText?.substring(0, 60)}...`);
    await apRow.click();
    await page.waitForTimeout(2500);
    if (page.url().includes('/procurement/vendors/')) {
      console.log('   ✅ SUCCESS → ' + page.url());
      pass++;
    } else {
      console.log('   ❌ FAILED → ' + page.url());
      fail++;
    }
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/drilldown-05-ap-to-vendor.png`, fullPage: true });
  } else {
    console.log('   No AP vendors');
  }

  await browser.close();
  console.log(`\n========= SUMMARY: ${pass} passed, ${fail} failed =========`);
}

main().catch((e) => { console.error('ERROR:', e); process.exit(1); });
