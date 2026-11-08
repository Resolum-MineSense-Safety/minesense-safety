// Screenshots for the report (chapter IV): dashboard pages after the accessibility fixes, the
// Playwright and Lighthouse HTML reports and the Prometheus /metrics endpoint of a service.
//   BASE_URL=http://localhost:4173 OUT=<folder> node scripts/capture-evidence.mjs
import { mkdirSync } from 'node:fs'
import { resolve } from 'node:path'
import { pathToFileURL } from 'node:url'
import { chromium } from '@playwright/test'

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:4173'
const OUT = process.env.OUT ?? 'reports/evidence'
const METRICS_URL = process.env.METRICS_URL ?? 'http://localhost:5233/metrics'
mkdirSync(OUT, { recursive: true })

const browser = await chromium.launch()
const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1 })
const page = await context.newPage()

await page.goto(BASE_URL + '/')
await page.screenshot({ path: `${OUT}/01-ingreso.png` })
await page.getByPlaceholder('Usuario').fill(process.env.E2E_ADMIN_USER ?? 'admin')
await page.getByPlaceholder('Contraseña').fill(process.env.E2E_ADMIN_PASSWORD ?? 'Admin-Local-2026')
await page.getByRole('button', { name: 'Entrar' }).click()
await page.getByRole('heading', { level: 1, name: 'Centro de control' }).waitFor()
await page.waitForLoadState('networkidle')
await page.screenshot({ path: `${OUT}/02-centro-de-control.png` })
for (const [menu, file] of [['Alertas', '03-alertas'], ['Flota', '04-flota']]) {
  await page.getByRole('navigation').getByRole('link', { name: menu, exact: true }).click()
  await page.getByRole('heading', { level: 1 }).waitFor()
  await page.waitForLoadState('networkidle')
  await page.screenshot({ path: `${OUT}/${file}.png` })
}

const report = await context.newPage()
await report.goto(pathToFileURL(resolve('playwright-report/index.html')).href)
await report.waitForTimeout(1500)
await report.screenshot({ path: `${OUT}/05-playwright-report.png` })

for (const name of ['ingreso', 'centro-de-control']) {
  await report.goto(pathToFileURL(resolve(`reports/lighthouse/${name}.html`)).href)
  await report.waitForTimeout(2500)
  await report.screenshot({ path: `${OUT}/06-lighthouse-${name}.png` })
}

await report.goto(METRICS_URL)
await report.screenshot({ path: `${OUT}/07-metrics-alert-service.png` })

await browser.close()
console.log(`Capturas en ${OUT}`)
