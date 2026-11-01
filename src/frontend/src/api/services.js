// One module per bounded context. Every call goes through the gateway routes of the Helm chart.
import { api } from './client'

export const identity = {
  signIn: (username, password) => api.post('/api/v1/authentication/sign-in', { username, password }),
  signOut: (refreshToken) => api.post('/api/v1/authentication/sign-out', { refreshToken }),
  me: () => api.get('/api/v1/users/me'),
  users: (role) => api.get('/api/v1/users', { role }),
  createUser: (user) => api.post('/api/v1/users', user),
  changeRole: (userId, role) => api.put(`/api/v1/users/${userId}/role`, { role }),
  deactivate: (userId) => api.post(`/api/v1/users/${userId}/deactivation`),
  grantUnit: (userId, miningUnitId) => api.post(`/api/v1/users/${userId}/mining-units`, { miningUnitId }),
  revokeUnit: (userId, miningUnitId) => api.del(`/api/v1/users/${userId}/mining-units/${miningUnitId}`),
}

export const operations = {
  units: () => api.get('/api/v1/mining-units'),
  createUnit: (unit) => api.post('/api/v1/mining-units', unit),
  addLocation: (unitId, location) => api.post(`/api/v1/mining-units/${unitId}/locations`, location),
  fleets: (miningUnitId) => api.get('/api/v1/fleets', { miningUnitId }),
  createFleet: (fleet) => api.post('/api/v1/fleets', fleet),
  vehicles: (fleetId) => api.get('/api/v1/vehicles', { fleetId }),
  createVehicle: (vehicle) => api.post('/api/v1/vehicles', vehicle),
  updateVehicle: (vehicleId, vehicle) => api.put(`/api/v1/vehicles/${vehicleId}`, vehicle),
  operators: (miningUnitId) => api.get('/api/v1/operators', { miningUnitId }),
  createOperator: (operator) => api.post('/api/v1/operators', operator),
  updateOperator: (operatorId, operator) => api.put(`/api/v1/operators/${operatorId}`, operator),
  assignments: (operatorId) => api.get('/api/v1/operator-assignments', { operatorId }),
  activeAssignment: (operatorId, date) => api.get('/api/v1/operator-assignments/active', { operatorId, date }),
  assign: (assignment) => api.post('/api/v1/operator-assignments', assignment),
  endAssignment: (assignmentId, endDate) => api.post(`/api/v1/operator-assignments/${assignmentId}/end`, { endDate }),
}

export const devices = {
  list: () => api.get('/api/v1/devices'),
  register: (device) => api.post('/api/v1/devices', device),
  assign: (deviceId, assignment) => api.put(`/api/v1/devices/${deviceId}/assignment`, assignment),
  reportStatus: (deviceId, status) => api.post(`/api/v1/devices/${deviceId}/status`, status),
  preShift: (operatorId, batteryThreshold = 20) =>
    api.get('/api/v1/devices/pre-shift-availability', { operatorId, batteryThreshold }),
}

export const monitoring = {
  sessions: (status) => api.get('/api/v1/monitoring-sessions', { status }),
  current: (operatorId) => api.get('/api/v1/monitoring-sessions/current', { operatorId }),
  start: (operatorId, vehicleCode, availableSignals) =>
    api.post('/api/v1/monitoring-sessions', { operatorId, vehicleCode, availableSignals }),
  loseSignal: (sessionId, signal) => api.post(`/api/v1/monitoring-sessions/${sessionId}/signal-losses`, { signal }),
  restoreSignal: (sessionId, signal) =>
    api.post(`/api/v1/monitoring-sessions/${sessionId}/signal-restorations`, { signal }),
  finish: (sessionId) => api.patch(`/api/v1/monitoring-sessions/${sessionId}/finish`),
}

export const fatigue = {
  assess: (reading) => api.post('/api/v1/fatigue-assessments', reading),
  byOperator: (operatorId) => api.get('/api/v1/fatigue-assessments', { operatorId }),
  latest: (operatorId) => api.get('/api/v1/fatigue-assessments/latest', { operatorId }),
  bySession: (sessionId) => api.get(`/api/v1/fatigue-assessments/sessions/${sessionId}`),
}

export const alerts = {
  issue: (alert) => api.post('/api/v1/alerts', alert),
  byOperator: (operatorId) => api.get('/api/v1/alerts', { operatorId }),
  byStatus: (status) => api.get(`/api/v1/alerts/status/${status}`),
  acknowledge: (alertId) => api.post(`/api/v1/alerts/${alertId}/acknowledgements`),
  escalate: (alertId) => api.post(`/api/v1/alerts/${alertId}/escalations`),
  escalateOverdue: () => api.post('/api/v1/alerts/overdue-escalations'),
}

export const fleet = {
  operators: () => api.get('/api/v1/fleet-operators'),
  filtered: (siteId, fleetId) => api.get('/api/v1/fleet-monitoring', { siteId, fleetId }),
  register: (operator) => api.post('/api/v1/fleet-operators', operator),
  setRisk: (operatorId, riskLevel) => api.put(`/api/v1/fleet-operators/${operatorId}/risk-level`, { riskLevel }),
  move: (operatorId, vehicleCode, location) =>
    api.put(`/api/v1/fleet-operators/${operatorId}/vehicle`, { vehicleCode, location }),
}

export const incidents = {
  list: (status, operatorId) => api.get('/api/v1/incidents', { status, operatorId }),
  get: (incidentId) => api.get(`/api/v1/incidents/${incidentId}`),
  open: (incident) => api.post('/api/v1/incidents', incident),
  assign: (incidentId, assigneeId) => api.patch(`/api/v1/incidents/${incidentId}/assignment`, { assigneeId }),
  addAction: (incidentId, action) => api.post(`/api/v1/incidents/${incidentId}/actions`, action),
  close: (incidentId, resolution) => api.patch(`/api/v1/incidents/${incidentId}/closure`, { resolution }),
  escalation: (incidentId) => api.get(`/api/v1/incidents/${incidentId}/escalation`),
  history: (incidentId) => api.get(`/api/v1/incidents/${incidentId}/history`),
  escalateOverdue: () => api.post('/api/v1/incidents/overdue-escalations'),
}

export const privacy = {
  purposes: () => api.get('/api/v1/processing-purposes'),
  registerConsent: (consent) => api.post('/api/v1/consents', consent),
  currentConsent: (operatorId) => api.get(`/api/v1/consents/operator/${operatorId}/current`),
  consentHistory: (operatorId) => api.get(`/api/v1/consents/operator/${operatorId}/history`),
  logAccess: (entry) => api.post('/api/v1/access-log/entries', entry),
  accessLog: (query) => api.get('/api/v1/access-log', query),
  incidentTrail: (incidentId) => api.get(`/api/v1/audit/incidents/${incidentId}/history`),
  requestComplianceReport: (report) => api.post('/api/v1/audit/compliance-reports', report),
  complianceReport: (reportId) => api.get(`/api/v1/audit/compliance-reports/${reportId}`),
}

export const reporting = {
  definitions: (onlyActive = true) => api.get('/api/v1/report-definitions', { onlyActive }),
  define: (definition) => api.post('/api/v1/report-definitions', definition),
  request: (job) => api.post('/api/v1/reports/safety', job),
  jobs: (query) => api.get('/api/v1/reports', query),
  download: (jobId) => api.raw(`/api/v1/reports/${jobId}/download`),
}

export const analytics = {
  recordEvent: (event) => api.post('/api/v1/analytics/fatigue-events', event),
  trends: (query) => api.get('/api/v1/analytics/fatigue-trends', query),
  measures: () => api.get('/api/v1/analytics/preventive-measures'),
  registerMeasure: (measure) => api.post('/api/v1/analytics/preventive-measures', measure),
  evaluate: (measureId, windowDays = 30) =>
    api.get(`/api/v1/analytics/preventive-measures/${measureId}/evaluation`, { windowDays }),
}

export const feedback = {
  track: (alertId, operationId) => api.put(`/api/v1/feedback-alerts/${alertId}`, { operationId }),
  queue: (query) => api.get('/api/v1/feedback-alerts', query),
  get: (alertId) => api.get(`/api/v1/alerts/${alertId}/feedback`),
  report: (alertId, classification, reason, reportedBy) =>
    api.post(`/api/v1/alerts/${alertId}/feedback`, { classification, reason, reportedBy }),
  review: (alertId, classification, observation, reviewedBy) =>
    api.post(`/api/v1/alerts/${alertId}/feedback/reviews`, { classification, observation, reviewedBy }),
  reliability: (query) => api.get('/api/v1/analytics/alert-reliability', query),
}

export const edge = {
  submit: (batch) => api.post('/api/v1/synchronization-batches', batch),
  get: (batchId) => api.get(`/api/v1/synchronization-batches/${batchId}`),
}

/** One cheap read per microservice, used by the service status page. */
export const PROBES = [
  { key: 'identity', name: 'Identity & Access', run: () => identity.me() },
  { key: 'operations', name: 'Operations', run: () => operations.units() },
  { key: 'devices', name: 'Device Management', run: () => devices.list() },
  { key: 'monitoring', name: 'Monitoring', run: () => monitoring.sessions() },
  { key: 'fatigue', name: 'Fatigue Detection', run: () => fatigue.byOperator('00000000-0000-0000-0000-000000000001') },
  { key: 'alerts', name: 'Alerts', run: () => alerts.byStatus('Issued') },
  { key: 'fleet', name: 'Fleet Monitoring', run: () => fleet.operators() },
  { key: 'incidents', name: 'Incident Management', run: () => incidents.list() },
  { key: 'privacy', name: 'Privacy & Audit', run: () => privacy.purposes() },
  { key: 'reporting', name: 'Reporting', run: () => reporting.definitions() },
  { key: 'analytics', name: 'Analytics', run: () => analytics.measures() },
  { key: 'feedback', name: 'Feedback & Reliability', run: () => feedback.get('00000000-0000-0000-0000-000000000001').catch((e) => { if (e.status === 404) return null; throw e }) },
  { key: 'edge', name: 'Edge Synchronization', run: () => edge.get('00000000-0000-0000-0000-000000000001').catch((e) => { if (e.status === 404) return null; throw e }) },
]
