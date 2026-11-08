// Steps of NFR04 (playwright-bdd). The same axe rules as tests/accessibility.spec.js.
import AxeBuilder from '@axe-core/playwright'
import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'
import { ADMIN, signIn } from '../../support/session.js'

const { Given, When, Then } = createBdd()
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']
let results

Given('el supervisor inició sesión en el dashboard', async ({ page }) => {
  await signIn(page)
})

Given('el supervisor abre la página de ingreso', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible()
})

When('abre el módulo {string}', async ({ page }, module) => {
  if (module !== 'Centro de control')
    await page.getByRole('navigation').getByRole('link', { name: module, exact: true }).click()
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
  await page.waitForLoadState('networkidle')
  results = await new AxeBuilder({ page }).withTags(TAGS).analyze()
})

Then('axe no encuentra violaciones de impacto {string} ni {string}', async ({ $testInfo }, first, second) => {
  const blocking = results.violations.filter((v) => v.impact === first || v.impact === second)
  await $testInfo.attach('axe-violations', { body: JSON.stringify(results.violations, null, 2), contentType: 'application/json' })
  expect(blocking.map((v) => `${v.id} (${v.nodes.length})`)).toEqual([])
})

Then('el contraste de todo el texto es de al menos 4.5 a 1', () => {
  expect(results.violations.find((v) => v.id === 'color-contrast')).toBeUndefined()
})

When('completa el formulario con el teclado', async ({ page }, table) => {
  const values = { Usuario: ADMIN.username, Contraseña: ADMIN.password }
  await page.getByRole('textbox', { name: 'Usuario' }).focus()
  for (const row of table.hashes()) {
    await page.keyboard.type(values[row.campo])
    await page.keyboard.press(row.tecla)
  }
})

Then('entra al {string} sin usar el mouse', async ({ page }, title) => {
  await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible()
})

Then('estos controles se encuentran por su nombre accesible', async ({ page }, table) => {
  for (const row of table.hashes())
    await expect(page.getByRole(row.rol, { name: row.nombre, exact: true })).toBeVisible()
})
