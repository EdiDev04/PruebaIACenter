import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e/specs',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [['html', { outputFolder: 'playwright-report' }], ['list']],
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    trace: 'on-first-retry',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      cwd: '../cotizador-webapp',
      reuseExistingServer: true,
      timeout: 60_000,
    },
    {
      command: 'dotnet run --project src/Cotizador.API/Cotizador.API.csproj',
      url: 'http://localhost:5001/health',
      cwd: '../cotizador-backend',
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:3001/health',
      cwd: '../cotizador-core-mock',
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
});
