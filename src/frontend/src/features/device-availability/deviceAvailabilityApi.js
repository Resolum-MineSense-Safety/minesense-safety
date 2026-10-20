const DEVICES_URL = '/api/v1/devices'

async function request(url) {
  const response = await fetch(url, { headers: { Accept: 'application/json' } })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(body?.message ?? `La solicitud falló (${response.status}).`)
  }
  return response.json()
}

export function getOperatorAvailability() {
  return request(`${DEVICES_URL}/operator-availability`)
}
