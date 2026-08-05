// Quick check — check the body for table content
import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await ctx.newPage();

await page.goto('http://localhost:3000/login', { waitUntil: 'domcontentloaded' });
await page.fill('input[type=email]', 'admin@erp.local');
await page.fill('input[type=password]', 'ChangeMe1234!');
await page.click('button[type=submit]');
await page.waitForURL(/.*\/dashboard.*/, { timeout: 10000 });

await page.goto('http://localhost:3000/finance/trial-balance', { waitUntil: 'domcontentloaded' });
await page.waitForLoadState('networkidle', { timeout: 15000 });
await page.waitForTimeout(2000);

const bodyText = await page.locator('body').innerText();
console.log('--- BODY TEXT ---');
console.log(bodyText.substring(0, 3000));

await browser.close();
