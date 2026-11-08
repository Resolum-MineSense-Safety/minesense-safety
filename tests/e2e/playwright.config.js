// E2E and browser tests of the Supervision Dashboard (section 4.4.1) and accessibility (4.4.5).
//   BASE_URL=http://localhost:8080 npx playwright test   # docker compose gateway
//   BASE_URL=http://localhost:5173 npx playwright test   # Vite dev server + start-native.sh
// Browser-agnostic specs run on Chromium, Firefox and WebKit; browser-specific specs pick their
// engine with test.skip (see tests/browser-specific.spec.js).
import { defineConfig, devices } from '@playwright/test'
import { defineBddConfig } from 'playwright-bdd'

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:5173'

// Gherkin acceptance tests of NFR04 (section 4.8): features/*.feature -> .features-gen (bddgen).
const bddTestDir = defineBddConfig({
  features: 'features/*.feature',
  steps: 'features/steps/*.js',
  outputDir: '.features-gen',
})

export default defineConfig({
  testDir: './tests',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : 1,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['json', { outputFile: 'reports/playwright-results.json' }],
    ['junit', { outputFile: 'reports/playwright-junit.xml' }],
  ],
  use: {
    baseURL: BASE_URL,
    locale: 'es-PE',
    timezoneId: 'America/Lima',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
    // Supervisors also check the board from a tablet in the pit (browser-specific, WebKit/iPad).
    { name: 'tablet-webkit', use: { ...devices['iPad (gen 7) landscape'] }, testMatch: /responsive\.spec\.js/ },
    { name: 'bdd-chromium', testDir: bddTestDir, use: { ...devices['Desktop Chrome'] } },
    { name: 'bdd-firefox', testDir: bddTestDir, use: { ...devices['Desktop Firefox'] } },
  ],
})
