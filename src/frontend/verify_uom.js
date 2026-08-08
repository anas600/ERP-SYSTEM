// Quick screenshot of the new inventory items/new form
const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
  const page = await ctx.newPage();

  page.on('pageerror', (err) => {
    if (!err.message.includes('Hydration') && !err.message.includes('htmlFor')) {
      console.log(`[pageerror] ${err.message}`);
    }
  });
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      const txt = msg.text();
      if (txt.includes('Hydration') || txt.includes('htmlFor') || txt.includes('did not match')) return;
      console.log(`[console.error] ${txt}`);
    }
  });

  console.log('— Login');
  await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
  await page.fill('input[type="email"]', 'admin@erp.local');
  await page.fill('input[type="password"]', 'ChangeMe1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.toString().includes('/login'), { timeout: 15000 });
  await page.waitForTimeout(500);

  console.log('— /inventory/items/new');
  await page.goto('http://localhost:3000/inventory/items/new', { waitUntil: 'networkidle' });
  // Wait for the UoM select to populate
  await page.waitForFunction(() => {
    const selects = Array.from(document.querySelectorAll('select'));
    const uomSelect = selects.find((s) => s.options.length > 5);
    return !!uomSelect;
  }, { timeout: 15000 }).catch(() => console.log('  (UoM select did not load in time)'));

  await page.waitForTimeout(1500);

  // Count UoM options
  const uomCount = await page.evaluate(() => {
    const selects = Array.from(document.querySelectorAll('select'));
    for (const s of selects) {
      const labelEl = s.closest('div')?.querySelector('label');
      if (labelEl && labelEl.textContent && labelEl.textContent.includes('وحدة القياس')) {
        return s.options.length;
      }
    }
    return 0;
  });
  console.log(`  UoM options count: ${uomCount}`);

  await page.screenshot({
    path: 'C:\\Users\\Anas\\AppData\\Local\\Temp\\uom-form.png',
    fullPage: true,
  });
  console.log('✓ Screenshot: uom-form.png');

  // Also screenshot the UoM dropdown open
  const uomSelect = await page.evaluate(() => {
    const selects = Array.from(document.querySelectorAll('select'));
    for (const s of selects) {
      const labelEl = s.closest('div')?.querySelector('label');
      if (labelEl && labelEl.textContent && labelEl.textContent.includes('وحدة القياس')) {
        return true;
      }
    }
    return false;
  });
  if (uomSelect) {
    // Click the uom select to open it
    const clicked = await page.evaluate(() => {
      const selects = Array.from(document.querySelectorAll('select'));
      for (const s of selects) {
        const labelEl = s.closest('div')?.querySelector('label');
        if (labelEl && labelEl.textContent && labelEl.textContent.includes('وحدة القياس')) {
          s.focus();
          s.click();
          return true;
        }
      }
      return false;
    });
    if (clicked) {
      await page.waitForTimeout(500);
      await page.screenshot({
        path: 'C:\\Users\\Anas\\AppData\\Local\\Temp\\uom-form-open.png',
        fullPage: true,
      });
      console.log('✓ Screenshot: uom-form-open.png');
    }
  }

  await browser.close();
  console.log('\n✓ Done.');
})();
