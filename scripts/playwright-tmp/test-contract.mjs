import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext();
const page = await context.newPage();

await page.goto('http://localhost:3000/login');
await page.waitForLoadState('networkidle');
await page.locator('input[type=email]').fill('admin@erp.local');
await page.locator('input[type=password]').fill('ChangeMe1234!');
await page.locator('button[type=submit]').click();
await page.waitForLoadState('networkidle');
await page.waitForTimeout(2000);

await page.goto('http://localhost:3000/projects/cc3e716f-3d88-4e6d-bc14-3a53e0703a62');
await page.waitForLoadState('networkidle');
await page.waitForTimeout(2000);

console.log('=== CONTRACT TAB ===');
await page.locator('button:has-text("العقد")').first().click();
await page.waitForTimeout(3000);
const ct = await page.locator('main').first().textContent();
console.log(ct.substring(0, 3500));

console.log('=== BILLINGS TAB ===');
await page.locator('button:has-text("المستخلصات")').first().click();
await page.waitForTimeout(3000);
const bt = await page.locator('main').first().textContent();
console.log(bt.substring(0, 3500));

await browser.close();
