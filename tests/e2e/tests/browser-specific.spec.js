// Browser-specific checks (section 4.4.1). Each one needs an engine feature or behaviour of a
// single browser, so it skips itself in the others.
import { expect, test } from '@playwright/test'
import { signIn } from '../support/session.js'

test('Chromium · el ingreso carga en menos de 6 s con una red 3G de mina', async ({ page, browserName }) => {
  test.skip(browserName !== 'chromium', 'La emulación de red usa el protocolo DevTools de Chromium')
  const cdp = await page.context().newCDPSession(page)
  // ~1.6 Mbps down, 750 kbps up, 150 ms RTT: the camp Wi-Fi of a remote pit.
  await cdp.send('Network.emulateNetworkConditions', {
    offline: false, latency: 150, downloadThroughput: 200_000, uploadThroughput: 94_000,
  })
  const started = Date.now()
  await page.goto('/', { waitUntil: 'load' })
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible()
  const elapsed = Date.now() - started
  test.info().annotations.push({ type: 'load-ms-3g', description: String(elapsed) })
  expect(elapsed).toBeLessThan(6000)
})

test('Chromium · tras perder la red, el dashboard se recupera al volver la conexión', async ({ page, context, browserName }) => {
  test.skip(browserName !== 'chromium', 'Se valida con la emulación offline de Chromium')
  await signIn(page)
  await context.setOffline(true)
  await page.getByRole('navigation').getByRole('link', { name: 'Alertas', exact: true }).click()
  await context.setOffline(false)
  await page.reload()
  await expect(page.getByRole('heading', { level: 1, name: 'Alertas' })).toBeVisible()
})

test('Firefox · el formulario respeta el orden de foco con teclado (Tab)', async ({ page, browserName }) => {
  test.skip(browserName !== 'firefox', 'Firefox ordena el foco de los controles de forma propia')
  await page.goto('/')
  await page.getByPlaceholder('Usuario').focus()
  await page.keyboard.press('Tab')
  await expect(page.getByPlaceholder('Contraseña')).toBeFocused()
})

test('WebKit · la sesión persiste al recargar (almacenamiento de Safari)', async ({ page, browserName }) => {
  test.skip(browserName !== 'webkit', 'Safari limita el almacenamiento local de forma distinta')
  await signIn(page)
  await page.reload()
  await expect(page.getByRole('heading', { level: 1, name: 'Centro de control' })).toBeVisible()
})
