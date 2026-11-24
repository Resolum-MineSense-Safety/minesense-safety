// Lighthouse audit of the login and the control center (section 4.4.5): accessibility,
// performance and best practices scores, saved as HTML and JSON under reports/lighthouse.
// Uses the Chromium that Playwright installs, signs in through the form first and keeps the
// session for the second audit.
//   BASE_URL=http://localhost:4173 npm run lighthouse
import { mkdirSync, writeFileSync } from 'node:fs'
import { chromium } from '@playwright/test'
import lighthouse from 'lighthouse'

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:4173'
const USER = process.env.E2E_ADMIN_USER ?? 'admin'
const PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'Admin-Local-2026'
const PORT = 9333
mkdirSync('reports/lighthouse', { recursive: true })

const browser = await chromium.launch({ args: [`--remote-debugging-port=${PORT}`] })
const context = await browser.newContext()
const page = await context.newPage()

async function audit(name, url) {
  const result = await lighthouse(url, {
    port: PORT,
    output: ['html', 'json'],
    logLevel: 'error',
    // No SEO category: the control center is internal and sends noindex on purpose.
    onlyCategories: ['accessibility', 'performance', 'best-practices'],
    formFactor: 'desktop',
    screenEmulation: { mobile: false, width: 1350, height: 940, deviceScaleFactor: 1, disabled: false },
    throttlingMethod: 'simulate',
    disableStorageReset: true,
  })
  const [html, json] = result.report
  writeFileSync(`reports/lighthouse/${name}.html`, html)
  writeFileSync(`reports/lighthouse/${name}.json`, json)
  const scores = Object.fromEntries(Object.entries(result.lhr.categories).map(([k, v]) => [k, Math.round(v.score * 100)]))
  const metrics = result.lhr.audits
  return {
    page: name, url, ...scores,
    fcpMs: Math.round(metrics['first-contentful-paint'].numericValue),
    lcpMs: Math.round(metrics['largest-contentful-paint'].numericValue),
    tbtMs: Math.round(metrics['total-blocking-time'].numericValue),
    cls: Number(metrics['cumulative-layout-shift'].numericValue.toFixed(3)),
  }
}

const summary = []
summary.push(await audit('ingreso', BASE_URL + '/'))

await page.goto(BASE_URL + '/')
await page.getByPlaceholder('Usuario').fill(USER)
await page.getByPlaceholder('Contraseña').fill(PASSWORD)
await page.getByRole('button', { name: 'Entrar' }).click()
await page.getByRole('heading', { level: 1, name: 'Centro de control' }).waitFor()
summary.push(await audit('centro-de-control', BASE_URL + '/'))
summary.push(await audit('alertas', BASE_URL + '/alertas'))

writeFileSync('reports/lighthouse/summary.json', JSON.stringify(summary, null, 2))
console.table(summary)
await browser.close()
