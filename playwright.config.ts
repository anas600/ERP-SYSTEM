import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config — Multi-Company aware
 * Run modes:
 *   - headless (default, CI-friendly):     npx playwright test
 *   - headed (visual debug):               npx playwright test --headed
 *   - UI mode (interactive):               npx playwright test --ui
 *   - specific spec:                       npx playwright test smoke.spec.ts
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1,
  workers: process.env.CI ? 2 : undefined,
  reporter: [
    ['list'],
    ['html', { open: 'never', outputFolder: 'playwright-report' }],
  ],
  // Run auth.setup.ts before all tests, reuse storage state
  globalSetup: require.resolve('./tests/global-setup.ts'),
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    apiBaseURL: process.env.API_BASE_URL || 'http://localhost:5000',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10_000,
    navigationTimeout: 30_000,
    locale: 'ar',
    timezoneId: 'Africa/Tripoli',
  },
  expect: { timeout: 5_000 },
  timeout: 30_000,
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1440, height: 900 },
      },
    },
  ],
  // Fail the build if test.summary has unexpected
  metadata: {
    product: 'ERP-SYSTEM',
    feature: 'Phase 6 Multi-Company',
    testEnvironment: process.env.NODE_ENV || 'local',
  },
});
