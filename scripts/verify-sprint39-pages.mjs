// Sprint 39 comprehensive Playwright sweep
// Tests 30+ pages across all modules to find UI bugs (console errors, blank pages, broken layouts)
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const results = [];
let browser, page;
const consoleErrors = [];
const pageErrors = [];

const PAGES = [
  // Dashboard
  { url: '/dashboard', name: 'Dashboard' },
  // Finance
  { url: '/finance/accounts', name: 'CoA' },
  { url: '/finance/accounts/new', name: 'CoA New' },
  { url: '/finance/cost-centers', name: 'Cost Centers' },
  { url: '/finance/cost-centers/new', name: 'Cost Centers New' },
  { url: '/finance/journal-entries', name: 'Journal Entries' },
  { url: '/finance/journal-entries/new', name: 'Journal Entries New' },
  { url: '/finance/trial-balance', name: 'Trial Balance' },
  { url: '/finance/customers', name: 'Customers' },
  { url: '/finance/customers/new', name: 'Customer New' },
  { url: '/finance/sales-invoices', name: 'Sales Invoices' },
  { url: '/finance/sales-invoices/new', name: 'Sales Invoice New' },
  { url: '/finance/receipts', name: 'Receipts' },
  { url: '/finance/receipts/new', name: 'Receipt New' },
  { url: '/finance/aging-ar', name: 'Aging AR' },
  // Inventory
  { url: '/inventory/items', name: 'Items' },
  { url: '/inventory/items/new', name: 'Item New' },
  { url: '/inventory/movements', name: 'Movements' },
  { url: '/inventory/movements/new', name: 'Movement New' },
  { url: '/inventory/reservations', name: 'Reservations' },
  { url: '/inventory/stock-levels', name: 'Stock Levels' },
  // Procurement
  { url: '/procurement/vendors', name: 'Vendors' },
  { url: '/procurement/vendors/new', name: 'Vendor New' },
  { url: '/procurement/purchase-orders', name: 'POs' },
  { url: '/procurement/purchase-orders/new', name: 'PO New' },
  { url: '/procurement/goods-receipts', name: 'Goods Receipts' },
  { url: '/procurement/goods-receipts/new', name: 'GR New' },
  { url: '/procurement/bills', name: 'Bills' },
  { url: '/procurement/bills/new', name: 'Bill New' },
  // HR
  { url: '/hr/departments', name: 'HR Departments' },
  { url: '/hr/employees', name: 'HR Employees' },
  { url: '/hr/employees/new', name: 'Employee New' },
  { url: '/hr/attendance', name: 'HR Attendance' },
  { url: '/hr/leaves', name: 'HR Leaves' },
  { url: '/hr/leaves/new', name: 'Leave New' },
  { url: '/hr/payroll', name: 'HR Payroll' },
  // Projects
  { url: '/projects', name: 'Projects' },
  { url: '/projects/new', name: 'Project New' },
  { url: '/resources', name: 'Resources' },
  // Admin
  { url: '/admin/users', name: 'Admin Users' },
  { url: '/admin/users/new', name: 'Admin User New' },
  { url: '/admin/companies', name: 'Admin Companies' },
  { url: '/admin/audit', name: 'Admin Audit' },
  { url: '/admin/health', name: 'Admin Health' },
  { url: '/admin/item-categories', name: 'Admin Item Categories' },
  { url: '/admin/posting-rules', name: 'Admin Posting Rules' },
  // Other
  { url: '/profile', name: 'Profile' },
  { url: '/profile/change-password', name: 'Change Password' },
  { url: '/holding', name: 'Holding' },
  { url: '/transactions', name: 'Transactions' },
];

async function visit(target, opts = {}) {
  const url = `${BASE}${target.url}`;
  const result = { name: target.name, url: target.url, status: 0, ok: false, errors: [], renderMs: 0 };
  const start = Date.now();
  try {
    const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 });
    result.status = resp ? resp.status() : 0;
    // Wait a bit for client components to render
    await page.waitForLoadState('networkidle', { timeout: 8000 }).catch(() => {});
    await page.waitForTimeout(opts.settleMs || 1200);
    // Check the page is not blank
    const bodyText = await page.locator('body').innerText();
    if (bodyText.length < 50) {
      result.errors.push(`Page looks blank (${bodyText.length} chars)`);
    }
    result.renderMs = Date.now() - start;
    result.ok = result.status >= 200 && result.status < 400 && result.errors.length === 0;
  } catch (e) {
    result.errors.push(e.message);
  }
  return result;
}

(async () => {
  browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  page = await context.newPage();

  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      consoleErrors.push({ url: page.url(), text: msg.text() });
    }
  });
  page.on('pageerror', (e) => {
    pageErrors.push({ url: page.url(), text: e.message });
  });

  // 1) Login
  console.log('1) Login...');
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]', { timeout: 10000 });
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/, { timeout: 15000 });
  console.log('  ✓ Logged in');

  // 2) Visit all pages
  console.log(`\n2) Visiting ${PAGES.length} pages...`);
  for (const target of PAGES) {
    const r = await visit(target);
    results.push(r);
    const icon = r.ok ? '✓' : '✗';
    console.log(`  ${icon} ${r.name.padEnd(25)} ${r.status} ${r.renderMs}ms ${r.errors.length ? '— ' + r.errors[0] : ''}`);
  }

  // 3) Summary
  console.log('\n' + '='.repeat(50));
  const ok = results.filter(r => r.ok).length;
  const failed = results.length - ok;
  console.log(`Pages: ${ok}/${results.length} passed`);
  if (failed > 0) {
    console.log(`\nFailed pages:`);
    results.filter(r => !r.ok).forEach(r => console.log(`  - ${r.name} (${r.url}): ${r.errors.join('; ')}`));
  }
  if (pageErrors.length > 0) {
    console.log(`\nPage errors (${pageErrors.length}):`);
    pageErrors.slice(0, 20).forEach(e => console.log(`  - ${e.url}: ${e.text}`));
  }
  if (consoleErrors.length > 0) {
    console.log(`\nConsole errors (${consoleErrors.length}):`);
    // Group by message
    const grouped = {};
    consoleErrors.forEach(e => {
      grouped[e.text] = (grouped[e.text] || 0) + 1;
    });
    Object.entries(grouped).forEach(([text, n]) => console.log(`  - (${n}×) ${text.slice(0, 150)}`));
  }

  // Screenshots of failed pages
  if (failed > 0) {
    console.log('\nTaking screenshots of failed pages...');
    for (const r of results.filter(r => !r.ok)) {
      await page.goto(`${BASE}${r.url}`, { waitUntil: 'domcontentloaded' });
      await page.waitForTimeout(1500);
      const p = path.join(SHOTS_DIR, `sprint39-FAIL-${r.name}.png`);
      await page.screenshot({ path: p, fullPage: true });
      console.log(`  📸 ${path.basename(p)}`);
    }
  }

  await browser.close();

  if (failed > 0 || pageErrors.length > 0) {
    process.exit(1);
  } else {
    console.log('\n✓ All pages clean.');
  }
})().catch((e) => {
  console.error(`FATAL: ${e.message}`);
  process.exit(1);
});
