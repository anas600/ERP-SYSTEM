// Sprint 58 verification script — Playwright smoke test of the new CoA + 2026 scenario
import { chromium } from 'playwright';
import { mkdir } from 'fs/promises';

const SCREENSHOT_DIR = 'C:/Users/Anas/AppData/Local/Temp/sprint58-screenshots';

await mkdir(SCREENSHOT_DIR, { recursive: true });

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

// Step 1: Login
console.log('Step 1: Login...');
await page.goto('http://localhost:3000/login');
await page.waitForSelector('[data-testid=email]', { timeout: 15000 });
await page.fill('[data-testid=email]', 'admin@erp.local');
await page.fill('[data-testid=password]', 'ChangeMe1234!');
await page.click('button[type=submit]');
// Wait for either URL change or toast
await page.waitForTimeout(5000);
console.log('  After submit, URL:', page.url());
await page.screenshot({ path: `${SCREENSHOT_DIR}/01-after-login.png` });

// If still on login, try navigating directly
if (page.url().includes('/login')) {
  console.log('  Still on login — navigating to /dashboard...');
  await page.goto('http://localhost:3000/dashboard');
  await page.waitForTimeout(3000);
}
console.log('  Final URL:', page.url());
await page.screenshot({ path: `${SCREENSHOT_DIR}/01-dashboard.png` });

// Step 2: Trial Balance
console.log('Step 2: Trial Balance...');
await page.goto('http://localhost:3000/finance/trial-balance');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/02-trial-balance.png` });

// Step 3: Income Statement
console.log('Step 3: Income Statement...');
await page.goto('http://localhost:3000/finance/reports/income-statement');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/03-income-statement.png` });

// Step 4: Balance Sheet
console.log('Step 4: Balance Sheet...');
await page.goto('http://localhost:3000/finance/reports/balance-sheet');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/04-balance-sheet.png` });

// Step 5: CoA page (the new 4-level chart)
console.log('Step 5: Chart of Accounts...');
await page.goto('http://localhost:3000/finance/accounts');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/05-coa.png` });

// Step 6: Projects
console.log('Step 6: Projects...');
await page.goto('http://localhost:3000/projects');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/06-projects.png` });

// Step 7: Executive Dashboard
console.log('Step 7: Executive Dashboard...');
await page.goto('http://localhost:3000/dashboard/executive');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/07-exec-dashboard.png` });

// Step 8: Journal Entries
console.log('Step 8: Journal Entries...');
await page.goto('http://localhost:3000/finance/journal-entries');
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${SCREENSHOT_DIR}/08-journal-entries.png` });

console.log('Done. Screenshots in', SCREENSHOT_DIR);
await browser.close();
