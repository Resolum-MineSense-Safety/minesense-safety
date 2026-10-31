// Spanish labels for the enums the services return, plus small formatting helpers.

export const LABELS = {
  // risk / severity
  Normal: 'Normal', Warning: 'Advertencia', Critical: 'Crítico',
  // alerts
  Issued: 'Emitida', Acknowledged: 'Atendida', Escalated: 'Escalada',
  // incidents
  Pending: 'Pendiente', Assigned: 'Asignado', Closed: 'Cerrado',
  // monitoring
  Active: 'Activo', Partial: 'Parcial', Unavailable: 'Sin señal', Stopped: 'Finalizado',
  EyeTracking: 'Cámara', HeartRate: 'Pulso',
  // devices
  Available: 'Disponible', Limited: 'Limitado', Interrupted: 'Interrumpido',
  ProtectionIncomplete: 'Protección incompleta', StartMonitoring: 'Iniciar monitoreo',
  ReviewBatteryBeforeStarting: 'Revisar batería', DoNotStartMonitoring: 'No iniciar',
  camera: 'Cámara', wearable: 'Wearable',
  // operations
  InMaintenance: 'En mantenimiento', Inactive: 'Inactivo', Ended: 'Finalizada', Superseded: 'Reemplazada',
  Day: 'Día', Night: 'Noche',
  // reports
  Requested: 'Solicitado', Rendering: 'Generando', Ready: 'Listo', Empty: 'Vacío', Failed: 'Fallido',
  // consents
  Accepted: 'Aceptado', Declined: 'Rechazado', Revoked: 'Revocado', Granted: 'Permitido', Denied: 'Denegado',
  // feedback
  PENDING: 'Sin revisar', AWAITING_REVIEW: 'Por revisar', REVIEWED: 'Revisada',
  CONFIRMED: 'Confirmada', FALSE_ALARM: 'Falsa alarma', INCONCLUSIVE: 'No concluyente',
  // roles
  Operator: 'Operador', Supervisor: 'Supervisor', SafetyManager: 'Jefe de seguridad', Administrator: 'Administrador',
  // incident actions
  OperatorContact: 'Contacto', PreventivePause: 'Pausa preventiva', OperatorRelief: 'Relevo', Other: 'Otra',
  ShiftReorganization: 'Reorganizar turno',
  // processing purposes (Privacy & Audit)
  'fatigue-monitoring': 'Monitoreo de fatiga', 'safety-alerts': 'Alertas de seguridad',
  'operational-analytics': 'Analítica operativa',
}

export const label = (value) => (value == null ? '—' : LABELS[value] ?? String(value))

export const shortId = (id) => (id ? String(id).slice(0, 8) : '—')

export function time(value) {
  if (!value) return '—'
  return new Date(value).toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit' })
}

export function dateTime(value) {
  if (!value) return '—'
  return new Date(value).toLocaleString('es-PE', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
}

export function ago(value) {
  if (!value) return '—'
  const seconds = Math.round((Date.now() - new Date(value).getTime()) / 1000)
  if (seconds < 60) return `${Math.max(seconds, 0)} s`
  if (seconds < 3600) return `${Math.round(seconds / 60)} min`
  if (seconds < 86400) return `${Math.round(seconds / 3600)} h`
  return `${Math.round(seconds / 86400)} d`
}

export const today = () => new Date().toISOString().slice(0, 10)

export const daysAgo = (days) => new Date(Date.now() - days * 86400000).toISOString()

export const pct = (value) => (value == null ? '—' : `${Math.round(value * 100)} %`)

export const RISK_ORDER = { Critical: 0, Warning: 1, Normal: 2 }
