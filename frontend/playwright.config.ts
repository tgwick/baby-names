import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    // Setup project - runs first to create authentication state
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },
    // Tests that don't need authentication
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      testIgnore: /.*\.setup\.ts/,
      dependencies: ['setup'],
    },
    // Tests that need authentication
    {
      name: 'chromium-authenticated',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'e2e/.auth/user.json',
      },
      testMatch: /.*\.authenticated\.ts/,
      dependencies: ['setup'],
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../backend/NameMatch.Api',
      url: 'http://localhost:5001/api/health',
      reuseExistingServer: true, // Reuse if already running (Docker or manual)
      timeout: 120 * 1000,
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: true, // Reuse if already running (Docker or manual)
      timeout: 120 * 1000,
    },
  ],
})
