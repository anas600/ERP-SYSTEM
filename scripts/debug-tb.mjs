// Quick check — dump TB page DOM
import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await ctx.newPage();
page.on('console', m => console.log(`[console.${m.type()}] ${m.text()}`));
page.on('pageerror', e => console.log(`[pageerror] ${e.message}`));

await page.goto('http://localhost:3000/login', { waitUntil: 'domcontentloaded' });
await page.fill('input[type=email]', 'admin@erp.local');
await page.fill('input[type=password]', 'ChangeMe1234!');
await page.click('button[type=submit]');
await page.waitForURL(/.*\/dashboard.*/, { timeout: 10000 });

await page.goto('http://localhost:3000/finance/trial-balance', { waitUntil: 'domcontentloaded' });
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);

// Scroll to bottom and take full page screenshot
await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
await page.waitForTimeout(500);

const html = await page.evaluate(() => document.body.innerHTML.length);
console.log(`Body HTML length: ${html}`);

const tables = await page.locator('table').count();
console.log(`Tables on page: ${tables}`);

const h3s = await page.locator('h3').allInnerTexts();
console.log(`H3 headers: ${h3s.join(' | ')}`);

await page.screenshot({ path: 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots\\sprint36-tb-scrolled.png', fullPage: true });
console.log('Scrolled screenshot saved');

await browser.close();
