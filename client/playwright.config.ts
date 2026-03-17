import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://127.0.0.1:4173';
const useExistingServer = process.env.PLAYWRIGHT_USE_EXISTING_SERVER === 'true';

export default defineConfig({
  testDir: './e2e/tests',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      grepInvert: /@mobile/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'mobile-smoke',
      grep: /@mobile/,
      use: { ...devices['Pixel 5'] },
    },
  ],
  webServer: useExistingServer
    ? undefined
    : {
        command: 'npm run dev -- --host 127.0.0.1 --port 4173',
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        env: {
          ...process.env,
          VITE_REACT_APP_SERVER_URL: process.env.VITE_REACT_APP_SERVER_URL ?? 'http://localhost:8080',
          VITE_REACT_APP_PAYMENT_PROVIDER: process.env.VITE_REACT_APP_PAYMENT_PROVIDER ?? 'mock',
          VITE_REACT_APP_ENABLE_MOCK_PAYMENT_UI: process.env.VITE_REACT_APP_ENABLE_MOCK_PAYMENT_UI ?? 'true',
        },
      },
});
