// Sprint 52 — V0 Audit + Screenshots
// يأخذ صور لكل صفحات التقارير في V0 (port 3000) لتحديد gaps قبل التحسين

import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

const SCREENSHOTS_DIR = 'C:/Users/Anas/.minimax-agent/projects/ERP-Holding-sprint-21/docs/screenshots/sprint-52';

async function main() {
  await mkdir(SCREENSHOTS_DIR, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Login
  console.log('1. Login...');
  await page.goto('http://localhost:3000/login');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/\/dashboard/, { timeout: 10000 });
  console.log('   ✓ Logged in, on dashboard');

  // Screenshot: Dashboard
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/01-dashboard.png`, fullPage: true });
  console.log('2. Dashboard screenshot saved');

  // Navigate to Trial Balance
  await page.goto('http://localhost:3000/finance/trial-balance');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/02-trial-balance.png`, fullPage: true });
  console.log('3. Trial Balance screenshot saved');

  // Try clicking on an account row (drill-down test)
  const accountRow = await page.locator('table tbody tr').first();
  if (await accountRow.count() > 0) {
    const accountCode = await accountRow.locator('td').first().textContent();
    console.log(`   Account row found: ${accountCode}`);
    console.log('   ⚠️  Click test (not yet implemented — would drill to General Ledger)');
  }

  // Balance Sheet
  await page.goto('http://localhost:3000/finance/reports/balance-sheet');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/03-balance-sheet.png`, fullPage: true });
  console.log('4. Balance Sheet screenshot saved');

  // Income Statement
  await page.goto('http://localhost:3000/finance/reports/income-statement');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/04-income-statement.png`, fullPage: true });
  console.log('5. Income Statement screenshot saved');

  // Cash Flow
  await page.goto('http://localhost:3000/finance/reports/cash-flow');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/05-cash-flow.png`, fullPage: true });
  console.log('6. Cash Flow screenshot saved');

  // AR Aging
  await page.goto('http://localhost:3000/finance/aging-ar');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/06-ar-aging.png`, fullPage: true });
  console.log('7. AR Aging screenshot saved');

  // Aging Summary (Sprint 49 page)
  await page.goto('http://localhost:3000/finance/reports/aging-summary');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/07-aging-summary.png`, fullPage: true });
  console.log('8. Aging Summary screenshot saved');

  // General Ledger (with account picker)
  await page.goto('http://localhost:3000/finance/reports/general-ledger');
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/08-general-ledger.png`, fullPage: true });
  console.log('9. General Ledger screenshot saved');

  // Test AR customer detail (drill-down test)
  console.log('10. Testing AR customer drill-down...');
  await page.goto('http://localhost:3000/finance/aging-ar');
  await page.waitForLoadState('networkidle');
  const arRow = await page.locator('table tbody tr').first();
  if (await arRow.count() > 0) {
    const arCode = await arRow.locator('td').first().textContent();
    console.log(`   AR customer found: ${arCode}`);
    console.log('   ⚠️  Click test (not yet implemented — would drill to customer statement)');
  }

  // Test AP drill-down
  console.log('11. Testing AP vendor drill-down...');
  await page.goto('http://localhost:3000/finance/reports/aging-summary');
  await page.waitForLoadState('networkidle');
  const apRow = await page.locator('table tbody tr').first();
  if (await apRow.count() > 0) {
    const apCode = await apRow.locator('td').first().textContent();
    console.log(`   AP vendor found: ${apCode}`);
    console.log('   ⚠️  Click test (not yet implemented — would drill to vendor statement)');
  }

  // Test sidebar groups
  console.log('12. Checking sidebar groups...');
  const sidebarGroups = await page.locator('nav p').allTextContents();
  console.log('   Sidebar groups:', sidebarGroups.slice(0, 10).join(' | '));

  await browser.close();
  console.log('\n✅ Done! Screenshots saved to:', SCREENSHOTS_DIR);
}

main().catch((e) => { console.error('ERROR:', e); process.exit(1); });
