import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright config. Specs live in the repo-level tests/ folder.
 *
 * Local mode (default): starts BOTH servers it needs — the .NET API (port 5000)
 * and the Vite dev server (port 5173) — and reuses them if already running.
 *
 * Live mode: set PLAYWRIGHT_BASE_URL to a deployed URL to run against the live
 * deployment instead; no local servers are started.
 */
// Treat an empty string the same as unset (|| not ??), so `PLAYWRIGHT_BASE_URL=`
// falls back to local mode.
const liveBaseURL = process.env.PLAYWRIGHT_BASE_URL || undefined

export default defineConfig({
  testDir: '../tests/frontend/e2e',
  fullyParallel: false,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: 'list',
  use: {
    baseURL: liveBaseURL ?? 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  webServer: liveBaseURL ? undefined : [
    {
      command: 'dotnet run --project ../backend/TradeBlotter.Api',
      url: 'http://localhost:5000/trades',
      reuseExistingServer: true,
      timeout: 240_000,
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: true,
      timeout: 240_000,
    },
  ],
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
