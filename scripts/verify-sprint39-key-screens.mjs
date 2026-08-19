// Sprint 39 — Visual screenshot of key pages
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const PAGES = [
  { url: '/dashboard', name: 'dashboard' },
  { url: '/finance/customers', name: 'customers' },
  { url: '/finance/accounts', name: 'accounts' },
  { url: '/finance/trial-balance', name: 'trial-balance' },
  { url: '/finance/journal-entries', name: 'journal-entries' },
  { url: '/finance/receipts', name: 'receipts' },
  { url: '/procurement/vendors', name: 'vendors' },
  { url: '/procurement/purchase-orders', name: 'purchase-orders' },
  { url: '/inventory/items', name: 'items' },
  { url: '/hr/employees', name: 'employees' },
  { url: '/projects', name: 'projects' },
  { url: '/admin/companies', name: 'companies' },
  { url: '/finance/sales-invoices/new', name: 'sales-invoice-new' },
];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();

  // Login
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/, { timeout: 15000 });
  console.log('  ✓ Logged in');

  for (const p of PAGES) {
    try {
      await page.goto(`${BASE}${p.url}`, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
      await page.waitForTimeout(1500);
      await page.screenshot({ path: path.join(SHOTS_DIR, `sprint39-page-${p.name}.png`), fullPage: true });
      console.log(`  📸 ${p.name}`);
    } catch (e) {
      console.log(`  ✗ ${p.name}: ${e.message}`);
    }
  }
  await browser.close();
})();
