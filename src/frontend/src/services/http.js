/**
 * Error raised for any non-2xx response. `message` carries the backend
 * business-rule message (HTTP 422 `{ message }`) when available.
 */
export class ApiError extends Error {
  constructor(message, status) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

const fallbackMessages = {
  400: 'La solicitud no es válida. Revise los datos ingresados.',
  401: 'Credenciales inválidas o sesión expirada.',
  403: 'No tiene permisos para realizar esta acción.',
  404: 'El recurso solicitado no existe.',
  422: 'La operación no cumple las reglas de negocio.',
  500: 'Error interno del servicio.',
  502: 'El servicio no está disponible.',
  503: 'El servicio no está disponible.',
  504: 'El servicio no respondió a tiempo.',
}

async function readBody(response) {
  const text = await response.text()
  if (!text) return null
  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

function extractMessage(body, status) {
  if (body && typeof body === 'object') {
    if (typeof body.message === 'string') return body.message
    if (body.errors && typeof body.errors === 'object') {
      const first = Object.values(body.errors).flat()[0]
      if (first) return String(first)
    }
    if (typeof body.title === 'string') return body.title
  }
  return fallbackMessages[status] ?? `Error inesperado (HTTP ${status}).`
}

function buildUrl(base, path, query) {
  const url = `${base}${path}`
  if (!query) return url
  const params = new URLSearchParams()
  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      params.append(key, value)
    }
  })
  const qs = params.toString()
  return qs ? `${url}?${qs}` : url
}

/**
 * Builds a small JSON client bound to a service base URL.
 */
export function createClient(baseUrl) {
  const base = String(baseUrl ?? '').replace(/\/$/, '')

  async function request(path, { method = 'GET', body, query } = {}) {
    const hasBody = body !== undefined
    let response
    try {
      response = await fetch(buildUrl(base, path, query), {
        method,
        headers: {
          Accept: 'application/json',
          ...(hasBody ? { 'Content-Type': 'application/json' } : {}),
        },
        body: hasBody ? JSON.stringify(body) : undefined,
      })
    } catch {
      throw new ApiError(
        'No se pudo conectar con el servicio. Verifique que esté en ejecución.',
        0,
      )
    }

    const data = await readBody(response)
    if (!response.ok) {
      throw new ApiError(extractMessage(data, response.status), response.status)
    }
    return data
  }

  return {
    get: (path, query) => request(path, { query }),
    post: (path, body) => request(path, { method: 'POST', body }),
    put: (path, body) => request(path, { method: 'PUT', body }),
  }
}
