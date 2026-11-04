// EP03 core business, end to end through the browser: an alert issued for a fatigue
// assessment reaches the supervisor, who acknowledges it (US05, US06). Browser-agnostic.
import { randomUUID } from 'node:crypto'
import { expect, test } from '@playwright/test'
import { apiToken, signIn } from '../support/session.js'

async function count(page, caption) {
  const text = await page.getByRole('button', { name: new RegExp(caption) }).first().innerText()
  return Number.parseInt(text.match(/\d+/)?.[0] ?? '0', 10)
}

test('el supervisor atiende una alerta nueva y pasa a Atendidas', async ({ page, request }) => {
  // Arrange: Alerts issues a warning for a new assessment (as Fatigue Detection would)
  const token = await apiToken(request)
  const operatorId = randomUUID()
  const issued = await request.post('/api/v1/alerts', {
    headers: { Authorization: `Bearer ${token}` },
    data: { operatorId, assessmentId: randomUUID(), severity: 'Warning' },
  })
  expect(issued.status()).toBe(201)

  await signIn(page)
  await page.getByRole('navigation').getByRole('link', { name: 'Alertas', exact: true }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Alertas' })).toBeVisible()
  // An operator outside the directory shows by the first 8 characters of its id.
  const row = page.getByRole('listitem').or(page.locator('div.rounded-xl, div.rounded-lg'))
    .filter({ hasText: operatorId.slice(0, 8) }).filter({ has: page.getByRole('button', { name: 'Atender', exact: true }) }).last()
  await expect(row).toBeVisible()
  const acknowledgedBefore = await count(page, 'Atendidas')

  // Act
  await row.getByRole('button', { name: 'Atender', exact: true }).click()

  // Assert
  await expect(page.getByText('Alerta atendida')).toBeVisible()
  await expect.poll(() => count(page, 'Atendidas')).toBe(acknowledgedBefore + 1)
  await expect(page.getByText(operatorId.slice(0, 8))).toHaveCount(0)
})
