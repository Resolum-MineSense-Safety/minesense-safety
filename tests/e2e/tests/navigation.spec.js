// Every module of the dashboard opens and loads its data. Browser-agnostic.
import { expect, test } from '@playwright/test'
import { signIn } from '../support/session.js'

const PAGES = [
  ['Flota', 'Flota'],
  ['Cabina', 'Cabina'],
  ['Alertas', 'Alertas'],
  ['Incidentes', 'Incidentes'],
  ['Operaciones', 'Operaciones'],
  ['Dispositivos', 'Dispositivos'],
  ['Edge', 'Sincronización edge'],
  ['Tendencias', 'Analítica'],
  ['Fiabilidad', 'Fiabilidad'],
  ['Reportes', 'Reportes'],
  ['Privacidad', 'Privacidad y auditoría'],
  ['Usuarios', 'Usuarios'],
  ['Servicios', 'Estado de servicios'],
]

test.describe('Navegación del dashboard', () => {
  test.beforeEach(async ({ page }) => signIn(page))

  for (const [menu, title] of PAGES) {
    test(`abre ${menu}`, async ({ page }) => {
      const errors = []
      page.on('pageerror', (error) => errors.push(error.message))
      await page.getByRole('navigation').getByRole('link', { name: menu, exact: true }).click()
      await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible()
      expect(errors, 'sin errores de JavaScript').toEqual([])
    })
  }
})
