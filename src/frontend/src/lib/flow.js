// Detection chain of one cabin reading across the bounded contexts (EP02 -> EP03 -> EP04 -> EP05,
// EP11, EP12). It runs from the dashboard until the Kafka events of ADR-004 connect the services.
import { alerts, analytics, fatigue, feedback, fleet, incidents } from '../api/services'

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

/** Preset readings: values that land in each risk band of the Fatigue Detection policy. */
export const PRESETS = {
  Normal: { perclos: 0.08, blinkRatePerMinute: 14, heartRateVariabilityMs: 52 },
  Warning: { perclos: 0.19, blinkRatePerMinute: 22, heartRateVariabilityMs: 34 },
  Critical: { perclos: 0.46, blinkRatePerMinute: 29, heartRateVariabilityMs: 17 },
}
