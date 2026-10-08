// Spanish (Peru) display labels for backend enum values.
// The backend contract stays in English; translation happens only in the UI.

export const riskLevelLabels = {
  Normal: 'Normal',
  Warning: 'Advertencia',
  Critical: 'Crítico',
}

export const shiftLabels = {
  Day: 'Día',
  Night: 'Noche',
}

export const alertStatusLabels = {
  Issued: 'Emitida',
  Acknowledged: 'Reconocida',
  Escalated: 'Escalada',
}

export const incidentStatusLabels = {
  Pending: 'Pendiente',
  Assigned: 'Asignado',
  Escalated: 'Escalado',
  Closed: 'Cerrado',
}

export const roleLabels = {
  Operator: 'Operador',
  Supervisor: 'Supervisor',
  SafetyManager: 'Jefe de seguridad',
  Administrator: 'Administrador',
}

export const label = (dictionary, value) => dictionary[value] ?? value ?? '—'

const dateTimeFormat = new Intl.DateTimeFormat('es-PE', {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

export function formatDateTime(value) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : dateTimeFormat.format(date)
}

export const shortId = (id) => (id ? String(id).slice(0, 8) : '—')
