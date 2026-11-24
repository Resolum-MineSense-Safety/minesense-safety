// HTTP client for the MineSense API gateway (/api/v1, same origin as the dashboard).
// It adds the Identity & Access access token and rotates it once with the refresh token on a 401.

const SESSION_KEY = 'minesense.session'

export function loadSession() {
  try {
    return JSON.parse(localStorage.getItem(SESSION_KEY)) ?? null
  } catch {
    return null
  }
}

export function saveSession(session) {
  try {
    if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    else localStorage.removeItem(SESSION_KEY)
  } catch {
    // Private mode: the session lives only in memory for this tab.
  }
  listeners.forEach((listener) => listener(session))
}

const listeners = new Set()
export function onSessionChange(listener) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export class ApiError extends Error {
  constructor(status, message, body) {
    super(message)
    this.status = status
    this.body = body
  }
}

function errorMessage(status, body) {
  if (body && typeof body === 'object') {
    if (body.message) return body.message
    if (body.detail) return body.detail
    if (body.title && body.errors) {
      const first = Object.values(body.errors).flat()[0]
      return first ? `${body.title} ${first}` : body.title
    }
    if (body.title) return body.title
  }
  if (typeof body === 'string' && body.trim()) return body
  return {
    401: 'Tu sesión expiró. Inicia sesión otra vez.',
    403: 'Tu rol no tiene permiso para esta acción.',
    404: 'No se encontró el recurso.',
    409: 'El recurso ya existe o cambió mientras lo editabas.',
    422: 'Los datos no cumplen una regla del negocio.',
    503: 'Un servicio dependiente no responde.',
  }[status] ?? `Error ${status}`
}

let refreshing = null
async function refreshTokens() {
  const session = loadSession()
  if (!session?.refreshToken) return false
  refreshing ??= fetch('/api/v1/authentication/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken: session.refreshToken }),
  })
    .then(async (response) => {
      if (!response.ok) return false
      saveSession(toSession(await response.json()))
      return true
    })
    .catch(() => false)
    .finally(() => {
      refreshing = null
    })
  return refreshing
}

export function toSession(auth) {
  return {
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    user: { id: auth.id, username: auth.username, fullName: auth.fullName, role: auth.role, miningUnitIds: auth.miningUnitIds ?? [] },
  }
}

export async function request(method, path, { body, query, raw, retry = true } = {}) {
  const url = new URL(path, window.location.origin)
  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') url.searchParams.set(key, value)
  })
  const session = loadSession()
  const headers = { Accept: raw ? '*/*' : 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (session?.accessToken) headers.Authorization = `Bearer ${session.accessToken}`

  const response = await fetch(url.pathname + url.search, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.status === 401 && retry && session?.refreshToken && !path.includes('/authentication/')) {
    if (await refreshTokens()) return request(method, path, { body, query, raw, retry: false })
    saveSession(null)
  }
  if (raw && response.ok) return response

  const text = await response.text()
  let parsed = text
  try {
    parsed = text ? JSON.parse(text) : null
  } catch {
    // Plain-text body
  }
  if (!response.ok) throw new ApiError(response.status, errorMessage(response.status, parsed), parsed)
  return parsed
}

export const api = {
  get: (path, query) => request('GET', path, { query }),
  post: (path, body, query) => request('POST', path, { body: body ?? {}, query }),
  put: (path, body) => request('PUT', path, { body: body ?? {} }),
  patch: (path, body) => request('PATCH', path, { body: body ?? {} }),
  del: (path) => request('DELETE', path),
  raw: (path) => request('GET', path, { raw: true }),
}
