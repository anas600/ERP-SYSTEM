import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E config (DEC-094, 2026-07-24)
 *
 * Strategy:
 * - Local dev: runs against `npm run dev` (frontend on :3000, backend on :5000 via .env.local)
 * - CI: builds backend, starts it, runs frontend dev mode, then E2E
 * - Single browser (chromium) for speed + low memory (6GB dev machine + free HF CI)
 * - Parallel: 1 worker locally to avoid memory spikes; CI can run 2
 */
const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:3000';
const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5000';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : 1,
  reporter: process.env.CI
    ? [['github'], ['list']]
    : [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: BASE_URL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10_000,
    navigationTimeout: 30_000,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: process.env.CI
    ? [
        // CI: assumes backend + frontend are already running (started by the workflow)
      ]
    : [
        // Local dev: start backend + frontend if not already running
        {
          command: 'cd ../backend && dotnet run --project Host/ERP-SYSTEM.csproj --no-launch-profile',
          url: API_URL + '/api/health/ready',
          reuseExistingServer: true,
          timeout: 120_000,
          env: {
            ASPNETCORE_ENVIRONMENT: 'Development',
            ASPNETCORE_URLS: API_URL.replace('http://', ''),
          },
        },
      ],
});
