// Validates the RS256 access tokens of Identity & Access with its JWKS, like the .NET services do
// (ADR-007): signature, issuer, audience and expiry. The keys are cached and refreshed on an unknown kid.
import { createPublicKey, verify } from 'node:crypto'

const ISSUER = process.env.AUTH_ISSUER ?? 'minesense-identity-access'
const AUDIENCE = process.env.AUTH_AUDIENCE ?? 'minesense-services'

const b64 = (part) => Buffer.from(part.replace(/-/g, '+').replace(/_/g, '/'), 'base64')

export function createVerifier(jwksUrl) {
  let keys = new Map()
  let loadedAt = 0

  async function load() {
    const response = await fetch(jwksUrl)
    if (!response.ok) throw new Error(`JWKS ${response.status}`)
    const { keys: list = [] } = await response.json()
    keys = new Map(list.map((jwk) => [jwk.kid, createPublicKey({ key: jwk, format: 'jwk' })]))
    loadedAt = Date.now()
  }

  return async function verifyToken(header) {
    const token = header?.startsWith('Bearer ') ? header.slice(7) : null
    if (!token) return null
    const parts = token.split('.')
    if (parts.length !== 3) return null
    let head, claims
    try {
      head = JSON.parse(b64(parts[0]).toString('utf8'))
      claims = JSON.parse(b64(parts[1]).toString('utf8'))
    } catch {
      return null
    }
    if (head.alg !== 'RS256') return null
    if (!keys.has(head.kid) && Date.now() - loadedAt > 10_000) await load().catch(() => {})
    const key = keys.get(head.kid)
    if (!key) return null
    const valid = verify('RSA-SHA256', Buffer.from(`${parts[0]}.${parts[1]}`), key, b64(parts[2]))
    if (!valid) return null
    const now = Date.now() / 1000
    const audiences = Array.isArray(claims.aud) ? claims.aud : [claims.aud]
    if (claims.iss !== ISSUER || !audiences.includes(AUDIENCE)) return null
    if (claims.exp && claims.exp < now - 30) return null
    if (claims.nbf && claims.nbf > now + 30) return null
    return claims
  }
}
