// Sprint 59 v2 — Projects redesign verification
// Usage: node verify_sprint59_projects.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const SCREEN_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\sprint-59-projects-screens';
fs.mkdirSync(SCREEN_DIR, { recursive: true });

async function login(page) {
  await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL(url => !url.toString().includes('/login'), { timeout: 15000 });
  // wait for localStorage to be set
  await page.waitForTimeout(500);
}

async function shot(page, name) {
  const file = path.join(SCREEN_DIR, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log(`✓ ${name}.png  (${(fs.statSync(file).size / 1024).toFixed(0)} KB)`);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Log console errors (skip hydration warnings — pre-existing in Input.tsx)
  page.on('pageerror', (err) => {
    if (err.message.includes('Hydration') || err.message.includes('htmlFor')) return;
    console.log(`[pageerror] ${err.message}`);
  });
  page.on('console', (msg) => {
    if (msg.type() !== 'error') return;
    const txt = msg.text();
    if (txt.includes('Hydration') || txt.includes('htmlFor') || txt.includes('did not match')) return;
    console.log(`[console.error] ${txt}`);
  });

  console.log('— Login');
  await login(page);
  console.log('  Logged in, current URL:', page.url());

  // 1) Projects list
  console.log('— /projects');
  await page.goto('http://localhost:3000/projects', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500); // wait for KPI loads
  await shot(page, 'projects-list');

  // 2) Projects new
  console.log('— /projects/new');
  await page.goto('http://localhost:3000/projects/new', { waitUntil: 'networkidle' });
  await page.waitForTimeout(800);
  await shot(page, 'projects-new');

  // 3) Projects [id] — find first project from the list
  console.log('— /projects/[id] (open first project)');
  await page.goto('http://localhost:3000/projects', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  const firstRow = await page.$('table tbody tr');
  if (firstRow) {
    const link = await firstRow.$('a[href*="/edit"]');
    if (link) {
      const href = await link.getAttribute('href');
      const id = href.split('/')[2]; // /projects/{id}/edit
      console.log('  Found project ID:', id);
      // Details page
      await page.goto(`http://localhost:3000/projects/${id}`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(1500);
      await shot(page, 'projects-details');
      // P&L tab
      await page.click('button:has-text("P&L")', { timeout: 5000 }).catch(() => {});
      await page.waitForTimeout(2500);
      await shot(page, 'projects-pnl');
      // Contract tab
      await page.click('button:has-text("العقد")', { timeout: 5000 }).catch(() => {});
      await page.waitForTimeout(1500);
      await shot(page, 'projects-contract');
      // Billings tab
      await page.click('button:has-text("المستخلصات")', { timeout: 5000 }).catch(() => {});
      await page.waitForTimeout(1500);
      await shot(page, 'projects-billings');
      // Edit page
      await page.goto(`http://localhost:3000/projects/${id}/edit`, { waitUntil: 'networkidle' });
      await page.waitForSelector('input[required]', { timeout: 15000 }).catch(() => {});
      await page.waitForTimeout(2500);
      await shot(page, 'projects-edit');
    } else {
      console.log('  No /edit link found in first row');
    }
  } else {
    console.log('  No project rows in list');
  }

  await browser.close();
  console.log('\n✓ Done. Screenshots in:', SCREEN_DIR);
})();
