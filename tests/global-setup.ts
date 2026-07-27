/**
 * Global setup — runs once before all tests.
 * Verifies backend + frontend are reachable; logs in admin and writes
 * the storage state to .auth/admin.json so individual specs don't
 * need to re-login.
 */
import { chromium, FullConfig, request } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const API = process.env.API_BASE_URL || 'http://localhost:5000';
const FRONTEND = process.env.BASE_URL || 'http://localhost:3000';
const ADMIN_EMAIL = 'admin@alfajr.local';
const ADMIN_PASSWORD = 'Demo1234';
const HOLDING_ID = '00000000-0000-0000-0000-000000000001';
const AUTH_DIR = path.join(__dirname, '..', '.auth');

export default async function globalSetup(config: FullConfig) {
  console.log(`\n[setup] Verifying backend at ${API}...`);
  const apiCtx = await request.newContext({ baseURL: API });
  // Health check via /api/auth/login
  const loginRes = await apiCtx.post('/api/auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  if (!loginRes.ok()) {
    throw new Error(`Backend unreachable or login failed: ${loginRes.status()} ${await loginRes.text()}`);
  }
  const loginJson = await loginRes.json();
  if (!loginJson.accessToken) {
    throw new Error('Login did not return accessToken');
  }
  console.log(`[setup] ✅ Login OK, companyId default = ${HOLDING_ID}`);

  // Frontend reachability
  console.log(`[setup] Verifying frontend at ${FRONTEND}...`);
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const nav = await page.goto(FRONTEND, { waitUntil: 'domcontentloaded' });
  if (!nav || !nav.ok()) {
    await browser.close();
    throw new Error(`Frontend unreachable: ${nav?.status() ?? 'no response'}`);
  }
  console.log(`[setup] ✅ Frontend OK (${nav.status()})`);

  // Save auth storage state for reuse across specs
  if (!fs.existsSync(AUTH_DIR)) fs.mkdirSync(AUTH_DIR, { recursive: true });
  // Login in browser context
  try {
    await page.goto(`${FRONTEND}/login`, { waitUntil: 'domcontentloaded', timeout: 15_000 });
  } catch (e: any) {
    await browser.close();
    throw new Error(`Failed to load login page: ${e.message}`);
  }
  // Wait for the form to be ready
  try {
    await page.waitForSelector('input[type="email"]', { timeout: 10_000 });
  } catch (e: any) {
    await browser.close();
    throw new Error(`Login form not found: ${e.message}`);
  }
  // Wait for React hydration to complete (Next.js dev mode is slow).
  // We detect this by waiting until a click on the button triggers the JS handler
  // instead of the form's default GET submission. Poll for hydration.
  console.log(`[setup] Waiting for React hydration...`);
  for (let i = 0; i < 20; i++) {
    const hydrated = await page.evaluate(() => {
      const btn = document.querySelector('button[type="submit"]');
      if (!btn) return false;
      // React attaches event listeners via a special property
      const keys = Object.keys(btn);
      return keys.some(k => k.startsWith('__reactProps$') || k.startsWith('__reactEventHandlers$'));
    });
    if (hydrated) {
      console.log(`[setup] ✅ React hydrated after ${i * 250}ms`);
      break;
    }
    await page.waitForTimeout(250);
  }
  await page.fill('input[type="email"]', ADMIN_EMAIL);
  await page.fill('input[type="password"]', ADMIN_PASSWORD);
  // Click submit and wait for the API call to complete
  const [response] = await Promise.all([
    page.waitForResponse(
      (r) => r.url().includes('/api/auth/login') && r.request().method() === 'POST',
      { timeout: 10_000 }
    ).catch(() => null),
    page.click('button[type="submit"]'),
  ]);
  if (response) {
    console.log(`[setup] Login API: ${response.status()}`);
    if (response.status() !== 200) {
      const body = await response.text().catch(() => '');
      await browser.close();
      throw new Error(`Login API failed: ${response.status()} ${body}`);
    }
  }
  // Give the React app 2s to write to localStorage (the authApi.login() does it
  // synchronously after the API returns, but useState updates are async).
  await page.waitForTimeout(2000);
  // Verify localStorage was populated
  const lsCheck = await page.evaluate(() => ({
    token: !!localStorage.getItem('accessToken'),
    user: !!localStorage.getItem('user'),
    company: localStorage.getItem('currentCompanyId'),
  })).catch(() => ({ token: false, user: false, company: null }));
  console.log(`[setup] localStorage: token=${lsCheck.token}, user=${lsCheck.user}, company=${lsCheck.company}`);

  if (!lsCheck.token) {
    // Force navigate to dashboard so useAuth loads the token
    console.log(`[setup] ⚠️ No token in localStorage, navigating to dashboard manually`);
    try {
      await page.goto(`${FRONTEND}/dashboard`, { waitUntil: 'domcontentloaded', timeout: 15_000 });
      await page.waitForTimeout(1500);
    } catch (e: any) {
      await browser.close();
      throw new Error(`Dashboard nav failed: ${e.message}`);
    }
  }
  console.log(`[setup] After login URL: ${page.url()}`);

  const storagePath = path.join(AUTH_DIR, 'admin.json');
  const state = await page.context().storageState();
  fs.writeFileSync(storagePath, JSON.stringify(state, null, 2));
  console.log(`[setup] ✅ Storage state written to ${storagePath}`);

  await browser.close();
  await apiCtx.dispose();
}
