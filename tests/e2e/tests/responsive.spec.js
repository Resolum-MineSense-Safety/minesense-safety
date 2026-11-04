// Tablet in landscape (iPad, WebKit): the supervisor checks the board from the pit.
import { expect, test } from '@playwright/test'
import { signIn } from '../support/session.js'

test('el centro de control cabe en una tablet sin scroll horizontal', async ({ page }) => {
  await signIn(page)
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
  expect(overflow).toBeLessThanOrEqual(1)
  await expect(page.getByRole('heading', { name: 'Tablero de flota' })).toBeVisible()
})
