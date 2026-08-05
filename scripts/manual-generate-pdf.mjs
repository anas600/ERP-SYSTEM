// Generate PDF from the user manual HTML
import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';

const HTML_PATH = 'C:\\Users\\Anas\\.minimax-agent\\projects\\user-manual\\ERP-User-Manual.html';
const PDF_PATH = 'C:\\Users\\Anas\\.minimax-agent\\projects\\user-manual\\ERP-User-Manual.pdf';

(async () => {
  if (!fs.existsSync(HTML_PATH)) {
    console.error(`HTML not found: ${HTML_PATH}`);
    process.exit(1);
  }
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await context.newPage();
  // Load via file:// URL so relative ../user-manual-assets paths resolve
  const fileUrl = 'file:///' + HTML_PATH.replace(/\\/g, '/');
  console.log('Loading:', fileUrl);
  await page.goto(fileUrl, { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  await page.pdf({
    path: PDF_PATH,
    format: 'A4',
    printBackground: true,
    margin: { top: '20mm', right: '15mm', bottom: '20mm', left: '15mm' },
  });
  await browser.close();
  const stat = fs.statSync(PDF_PATH);
  console.log(`✓ PDF generated: ${PDF_PATH} (${(stat.size / 1024).toFixed(0)} KB)`);
})();
