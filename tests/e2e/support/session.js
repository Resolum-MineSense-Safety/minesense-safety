import { expect } from '@playwright/test'

export const ADMIN = {
  username: process.env.E2E_ADMIN_USER ?? 'admin',
  password: process.env.E2E_ADMIN_PASSWORD ?? 'Admin-Local-2026',
}

/** Signs in through the login form, as a supervisor would. */
export async function signIn(page, account = ADMIN) {
  await page.goto('/')
  await page.getByPlaceholder('Usuario').fill(account.username)
  await page.getByPlaceholder('Contraseña').fill(account.password)
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Centro de control' })).toBeVisible()
}

/** Gets an access token straight from Identity & Access, for API set-up steps. */
export async function apiToken(request, account = ADMIN) {
  const response = await request.post('/api/v1/authentication/sign-in', {
    data: { username: account.username, password: account.password },
  })
  expect(response.ok()).toBeTruthy()
  return (await response.json()).accessToken
}
