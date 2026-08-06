// Take fresh screenshots of key pages for the user manual
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const BASE = 'http://localhost:3000';
const PAGES = [
  { url: '/finance/sales-invoices/new', name: 'sales-invoice-new-after-fix' },
  { url: '/finance/journal-entries', name: 'journal-entries-after-fix' },
  { url: '/admin/posting-rules', name: 'posting-rules-after-fix' },
  { url: '/inventory/reservations', name: 'reservations-after-fix' },
  { url: '/finance/cost-centers', name: 'cost-centers-after-fix' },
];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();

  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/);

  for (const p of PAGES) {
    await page.goto(`${BASE}${p.url}`, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
    await page.waitForTimeout(1500);
    const filename = `sprint40-${p.name}.png`;
    await page.screenshot({ path: path.join(SHOTS_DIR, filename), fullPage: true });
    console.log(`  📸 ${filename}`);
  }
  await browser.close();
})();
