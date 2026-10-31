// Detection chain of one cabin reading across the bounded contexts (EP02 -> EP03 -> EP04 -> EP05,
// EP11, EP12). With VITE_EVENT_DRIVEN=true the backend runs it through the Kafka events of ADR-004
// and the dashboard only posts the assessment and follows what the services created. Without it
// (local demo without Kafka) the dashboard calls each service itself.
import { alerts, analytics, fatigue, feedback, fleet, incidents } from '../api/services'

export const EVENT_DRIVEN = import.meta.env.VITE_EVENT_DRIVEN === 'true'

const POLL_TIMEOUT_MS = 8000
const POLL_INTERVAL_MS = 500

/**
 * @returns {Promise<{assessment, alert?, incident?, steps: {name: string, ok: boolean}[]}>}
 */
export async function processReading({ operator, session, reading, operationId, context }) {
  const steps = []
  const step = async (name, fn) => {
    try {
      const result = await fn()
      steps.push({ name, ok: true })
      return result
    } catch (error) {
      steps.push({ name, ok: false, error: error.message })
      return undefined
    }
  }

  const assessment = await fatigue.assess({
    operatorId: operator.operatorId,
    monitoringSessionId: session.id,
    ...reading,
  })
  steps.push({ name: 'Fatiga evaluada', ok: true })

  if (EVENT_DRIVEN) return followBackend({ operator, assessment, operationId, step, steps })

  await step('Riesgo en flota', () => fleet.setRisk(operator.operatorId, assessment.riskLevel))
  if (!assessment.requiresAlert) return { assessment, steps }

  const alert = await step('Alerta emitida', () =>
    alerts.issue({ operatorId: operator.operatorId, assessmentId: assessment.id, severity: assessment.riskLevel }))
  if (!alert) return { assessment, steps }

  await step('Tendencia registrada', () =>
    analytics.recordEvent({
      operatorId: operator.operatorId,
      alertId: alert.id,
      severity: alert.severity,
      occurredAt: alert.issuedAt,
      shift: context.shift,
      mineSite: context.mineSite,
      location: operator.location,
      fleet: operator.fleet,
    }))
  if (operationId) await step('Feedback habilitado', () => feedback.track(alert.id, operationId))

  let incident
  if (alert.severity === 'Critical')
    incident = await step('Incidente abierto', () =>
      incidents.open({ operatorId: operator.operatorId, alertId: alert.id, severity: alert.severity }))

  return { assessment, alert, incident, steps }
}

/**
 * Event-driven mode: Fleet Monitoring, Alerts, Incident Management, Analytics and Feedback react to
 * FatigueAssessed / AlertIssued on their own. The steps show what could be read back in a few
 * seconds; Analytics has no per-alert read, so its step is not shown.
 */
async function followBackend({ operator, assessment, operationId, step, steps }) {
  const { operatorId } = operator
  const [, alert] = await Promise.all([
    step('Riesgo en flota', () =>
      waitFor(async () => {
        const current = (await fleet.operators()).find((item) => item.operatorId === operatorId)
        return current?.currentRiskLevel === assessment.riskLevel &&
          new Date(current.lastUpdatedAt) >= new Date(assessment.assessedAt)
          ? current
          : undefined
      })),
    assessment.requiresAlert
      ? step('Alerta emitida', () =>
          waitFor(async () => (await alerts.byOperator(operatorId)).find((item) => item.assessmentId === assessment.id)))
      : undefined,
  ])
  if (!alert) return { assessment, steps: ordered(steps) }

  const [incident] = await Promise.all([
    alert.severity === 'Critical'
      ? step('Incidente abierto', () =>
          waitFor(async () => (await incidents.list(undefined, operatorId)).find((item) => item.alertId === alert.id)))
      : undefined,
    operationId ? step('Feedback habilitado', () => waitFor(() => feedback.get(alert.id))) : undefined,
  ])
  return { assessment, alert, incident, steps: ordered(steps) }
}

/** Keeps the steps in the order of the chain, whatever request finished first. */
const ORDER = ['Fatiga evaluada', 'Riesgo en flota', 'Alerta emitida', 'Feedback habilitado', 'Incidente abierto']
const ordered = (steps) => [...steps].sort((a, b) => ORDER.indexOf(a.name) - ORDER.indexOf(b.name))

/** Polls until `read` returns a value; errors (e.g. 404 while the event is in flight) count as "not yet". */
async function waitFor(read) {
  const deadline = Date.now() + POLL_TIMEOUT_MS
  for (;;) {
    try {
      const value = await read()
      if (value) return value
    } catch {
      // not created yet
    }
    if (Date.now() >= deadline) throw new Error('El backend no lo registró a tiempo')
    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS))
  }
}

/** Preset readings: values that land in each risk band of the Fatigue Detection policy. */
export const PRESETS = {
  Normal: { perclos: 0.08, blinkRatePerMinute: 14, heartRateVariabilityMs: 52 },
  Warning: { perclos: 0.19, blinkRatePerMinute: 22, heartRateVariabilityMs: 34 },
  Critical: { perclos: 0.46, blinkRatePerMinute: 29, heartRateVariabilityMs: 17 },
}
