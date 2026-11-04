// Shared helpers of the k6 scenarios (section 4.4.3): sign-in, the critical detection -> alert
// path and the supervisor reads, all through the single entry point (ALB, gateway or Vite proxy).
import http from 'k6/http'
import { check, group } from 'k6'
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js'

export const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080'
const USER = __ENV.K6_USER || 'admin'
const PASSWORD = __ENV.K6_PASSWORD || 'Admin-Local-2026'

/** setup(): one token for the whole test, as the dashboard keeps one per session. */
export function signIn() {
  const response = http.post(`${BASE_URL}/api/v1/authentication/sign-in`,
    JSON.stringify({ username: USER, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'sign-in' } })
  check(response, { 'sign-in 200': (r) => r.status === 200 })
  return { token: response.json('accessToken') }
}

const headers = (token) => ({ 'Content-Type': 'application/json', Authorization: `Bearer ${token}` })

// Drowsy signals (PERCLOS 0.45): Fatigue Detection classifies them as Critical (US03/US04).
const SIGNALS = [
  { perclos: 0.05, blinkRatePerMinute: 15, heartRateVariabilityMs: 60 },
  { perclos: 0.2, blinkRatePerMinute: 18, heartRateVariabilityMs: 45 },
  { perclos: 0.45, blinkRatePerMinute: 8, heartRateVariabilityMs: 18 },
]

/**
 * The safety-critical path of QAS 02 (SLO-2, P95 <= 500 ms): assess the cabin signals and, when
 * the assessment requires it, issue the alert. Tagged path=critical for the thresholds.
 */
export function criticalPath(token, operatorId) {
  group('detección y alerta', () => {
    const signals = SIGNALS[Math.floor(Math.random() * SIGNALS.length)]
    const assessed = http.post(`${BASE_URL}/api/v1/fatigue-assessments`,
      JSON.stringify({ operatorId, monitoringSessionId: uuidv4(), ...signals }),
      { headers: headers(token), tags: { name: 'assess', path: 'critical' } })
    check(assessed, { 'assess 201': (r) => r.status === 201 })
    if (assessed.status !== 201 || !assessed.json('requiresAlert')) return
    const severity = assessed.json('riskLevel') === 'Critical' ? 'Critical' : 'Warning'
    const alert = http.post(`${BASE_URL}/api/v1/alerts`,
      JSON.stringify({ operatorId, assessmentId: assessed.json('id'), severity }),
      { headers: headers(token), tags: { name: 'issue-alert', path: 'critical' } })
    check(alert, { 'alert 201': (r) => r.status === 201 })
  })
}

/** What the control center polls (SLO-3, P95 <= 2 s). */
export function supervisorReads(token, operatorId) {
  group('lecturas del supervisor', () => {
    const responses = http.batch([
      ['GET', `${BASE_URL}/api/v1/alerts/status/Issued`, null, { headers: headers(token), tags: { name: 'alerts-issued', path: 'read' } }],
      ['GET', `${BASE_URL}/api/v1/alerts?operatorId=${operatorId}`, null, { headers: headers(token), tags: { name: 'alerts-by-operator', path: 'read' } }],
      ['GET', `${BASE_URL}/api/v1/incidents`, null, { headers: headers(token), tags: { name: 'incidents', path: 'read' } }],
    ])
    check(responses[0], { 'reads 200': (r) => r.status === 200 })
  })
}

export { uuidv4 }
