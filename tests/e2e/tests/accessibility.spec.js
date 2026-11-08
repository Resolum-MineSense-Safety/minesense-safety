// Accessibility (section 4.4.5, NFR04): axe-core with the WCAG 2.1 A/AA rules on the login and on
// the main pages. Serious and critical violations fail the test; every result is saved under
// reports/axe for the report.
import { mkdirSync, writeFileSync } from 'node:fs'
import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { signIn } from '../support/session.js'

const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']
const PAGES = [
  ['Centro de control', null],
  ['Flota', 'Flota'],
  ['Alertas', 'Alertas'],
  ['Incidentes', 'Incidentes'],
  ['Operaciones', 'Operaciones'],
  ['Reportes', 'Reportes'],
]

function save(name, browser, results) {
  mkdirSync('reports/axe', { recursive: true })
  const summary = results.violations.map((v) => ({
    id: v.id, impact: v.impact, help: v.help, nodes: v.nodes.length,
    targets: v.nodes.slice(0, 3).map((n) => n.target.join(' ')),
  }))
  writeFileSync(`reports/axe/${browser}-${name.replace(/\s+/g, '-').toLowerCase()}.json`,
    JSON.stringify({ page: name, passes: results.passes.length, violations: summary }, null, 2))
  return summary
}

const blocking = (summary) => summary.filter((v) => v.impact === 'serious' || v.impact === 'critical')

test('@a11y ingreso sin violaciones graves de WCAG 2.1 AA', async ({ page, browserName }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible()
  const results = await new AxeBuilder({ page }).withTags(TAGS).analyze()
  expect(blocking(save('ingreso', browserName, results))).toEqual([])
})

for (const [name, menu] of PAGES) {
  test(`@a11y ${name} sin violaciones graves de WCAG 2.1 AA`, async ({ page, browserName }) => {
    await signIn(page)
    if (menu) await page.getByRole('navigation').getByRole('link', { name: menu, exact: true }).click()
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await page.waitForLoadState('networkidle')
    const results = await new AxeBuilder({ page }).withTags(TAGS).analyze()
    expect(blocking(save(name, browserName, results))).toEqual([])
  })
}
