// Detection chain of one fatigue assessment across the bounded contexts (EP02 -> EP03 -> EP04 ->
// EP05, EP11, EP12). The cabin gateway (fleet IoT simulator, or the Python edge agent) posts the
// assessment; Fleet Monitoring, Alerts, Incident Management, Analytics and Feedback react to the
// Kafka events of ADR-004 on their own. The dashboard never sends readings: it only follows what
// the services created for the assessment it is showing.
import { alerts, feedback, fleet, incidents } from '../api/services'

/**
 * Reads back the chain of an assessment.
 * @returns {Promise<{alert?, incident?, steps: {name: string, ok: boolean|null, detail?: string}[]}>}
 */
export async function traceAssessment(operatorId, assessment) {
  if (!assessment) return { steps: [] }
  const steps = [{ name: 'Lectura del gateway evaluada', ok: true, detail: assessment.origin === 'Edge' ? 'clasificada en cabina sin cobertura' : 'Fatigue Detection' }]
  if (assessment.origin === 'Edge') {
    steps.push({ name: 'Se sincroniza al recuperar cobertura', ok: null, detail: 'lote idempotente de Edge Synchronization' })
    return { steps }
  }

  const current = (await fleet.operators().catch(() => [])).find((item) => item.operatorId === operatorId)
  steps.push({ name: 'Riesgo en flota', ok: current ? current.currentRiskLevel === assessment.riskLevel || null : null, detail: current?.currentRiskLevel })
  if (!assessment.requiresAlert) {
    steps.push({ name: 'Sin alerta: riesgo normal', ok: true })
    return { steps }
  }

  const alert = (await alerts.byOperator(operatorId).catch(() => [])).find((item) => item.assessmentId === assessment.id)
  steps.push({ name: 'Alerta emitida', ok: alert ? true : null, detail: alert ? alert.status : 'en camino' })
  if (!alert) return { steps }

  const tracked = await feedback.get(alert.id).catch(() => null)
  steps.push({ name: 'Feedback habilitado', ok: tracked ? true : null, detail: tracked?.status })

  let incident
  if (alert.severity === 'Critical') {
    incident = (await incidents.list(undefined, operatorId).catch(() => [])).find((item) => item.alertId === alert.id)
    steps.push({ name: 'Incidente abierto', ok: incident ? true : null, detail: incident?.status ?? 'en camino' })
  }
  return { alert, incident, steps }
}
