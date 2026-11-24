// US18 / QAS 04: only authenticated users reach the control center. Browser-agnostic.
import { expect, test } from '@playwright/test'
import { ADMIN, signIn } from '../support/session.js'

test.describe('Autenticación', () => {
  test('un usuario válido entra al centro de control', async ({ page }) => {
    await signIn(page)
    await expect(page.getByRole('navigation')).toBeVisible()
  })

  test('una contraseña incorrecta muestra el error y no entra', async ({ page }) => {
    await page.goto('/')
    await page.getByPlaceholder('Usuario').fill(ADMIN.username)
    await page.getByPlaceholder('Contraseña').fill('contrasena-equivocada')
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page.getByText('Usuario o contraseña incorrectos.')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Centro de control' })).toHaveCount(0)
  })

  test('cerrar sesión vuelve al formulario de ingreso', async ({ page }) => {
    await signIn(page)
    await page.getByRole('button', { name: 'Cerrar sesión' }).click()
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible()
  })

  test('la API rechaza peticiones sin token (401)', async ({ request }) => {
    const response = await request.get('/api/v1/alerts/status/Issued')
    expect(response.status()).toBe(401)
  })
})
