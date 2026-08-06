import { chromium } from 'playwright';
const tests = [];
const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
const errors = [];
page.on('pageerror', (e) => errors.push('PAGE: ' + e.message));
page.on('console', (m) => { if (m.type() === 'error') errors.push('CONSOLE: ' + m.text()); });

async function go(p, label) {
  const startErrs = errors.length;
  try {
    await page.goto('http://localhost:3000' + p, { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(800);
    const status = page.url();
    const body = await page.textContent('body').catch(() => '');
    const blank = !body || body.length < 50;
    const errs = errors.length - startErrs;
    tests.push({ label, status, blank, errs });
  } catch (e) {
    tests.push({ label, error: e.message });
  }
}

await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
await page.fill('input[type=email]', 'admin@erp.local');
await page.fill('input[type=password]', 'ChangeMe1234!');
await page.click('button[type=submit]');
await page.waitForURL('**/dashboard', { timeout: 15000 });
console.log('LOGIN: OK -> ' + page.url());

const pages = [
  ['/dashboard', 'Dashboard'],
  ['/finance/journal-entries', 'Journal List'],
  ['/finance/journal-entries/new', 'Journal New'],
  ['/finance/receipts', 'Receipts'],
  ['/finance/sales-invoices/new', 'Sales Invoice New'],
  ['/admin/users/new', 'Admin Users New'],
  ['/admin/posting-rules', 'Posting Rules'],
  ['/admin/posting-rules/new', 'Posting Rules New'],
  ['/admin/item-categories', 'Categories List'],
  ['/admin/item-categories/new', 'Categories New'],
  ['/finance/accounts/new', 'Accounts New'],
  ['/finance/cost-centers', 'Cost Centers'],
  ['/finance/cost-centers/new', 'Cost Centers New'],
  ['/inventory/items/new', 'Items New'],
  ['/inventory/movements', 'Movements'],
  ['/inventory/reservations', 'Reservations'],
  ['/inventory/reservations/new', 'Reservations New'],
  ['/procurement/goods-receipts/new', 'GR New'],
  ['/projects/new', 'Projects New'],
];
for (const [p, l] of pages) await go(p, l);

const blanks = tests.filter((t) => t.blank);
const withErr = tests.filter((t) => t.errs > 0);
console.log(`Pages: ${tests.length} | blank: ${blanks.length} | console errors: ${withErr.length}`);
for (const t of tests) {
  if (t.error) console.log('  [ERR] ' + t.label + ' -> ' + t.error);
  else console.log('  [' + (t.blank ? 'BLANK' : 'OK   ') + '] ' + (t.errs ? '(' + t.errs + ' errs) ' : '') + t.label);
}
await browser.close();
process.exit(0);
