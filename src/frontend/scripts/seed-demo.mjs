#!/usr/bin/env node
// Loads a demo mine into the 13 microservices through the gateway, using only their public APIs:
// a mining unit with fleets, trucks and operators, cabin devices, users, consents, monitoring
// sessions, fatigue readings that raise alerts and incidents, feedback, trends, reports and an
// Edge batch. Run it once against an empty environment:
//
//   node scripts/seed-demo.mjs                       # through the Vite dev proxy (npm run dev)
//   BASE_URL=http://localhost:8080 node scripts/seed-demo.mjs   # through the docker-compose gateway
//
// DEMO_PASSWORD sets the password of the demo users (supervisor, jefe.seguridad, operador.rosa);
// it is never stored in the repository. ADMIN_PASSWORD defaults to the Development profile value.

const BASE = process.env.BASE_URL ?? 'http://localhost:5173'
const DEMO_PASSWORD = process.env.DEMO_PASSWORD
if (!DEMO_PASSWORD || DEMO_PASSWORD.length < 8) {
  console.error('Set DEMO_PASSWORD (8+ characters) for the demo users, e.g. DEMO_PASSWORD=... node scripts/seed-demo.mjs')
  process.exit(1)
}
const ADMIN = { username: process.env.ADMIN_USER ?? 'admin', password: process.env.ADMIN_PASSWORD ?? 'Admin-Local-2026' }

let token = null
async function call(method, path, body, { allow = [], as } = {}) {
  const response = await fetch(BASE + path, {
    method,
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${as ?? token}` },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  const text = await response.text()
  const data = text ? JSON.parse(text) : null
  if (!response.ok && !allow.includes(response.status))
    throw new Error(`${method} ${path} -> ${response.status} ${text.slice(0, 300)}`)
  return data
}
const post = (path, body, options) => call('POST', path, body ?? {}, options)
const put = (path, body, options) => call('PUT', path, body ?? {}, options)
const patch = (path, body, options) => call('PATCH', path, body ?? {}, options)
const get = (path, options) => call('GET', path, undefined, options)

const log = (text) => console.log(`  ✓ ${text}`)
const today = new Date().toISOString().slice(0, 10)
const daysAgo = (days, hour = 10) => {
  const date = new Date(Date.now() - days * 86400000)
  date.setUTCHours(hour, 0, 0, 0)
  return date.toISOString()
}
const uuid = () => crypto.randomUUID()

async function signIn(username, password) {
  const auth = await post('/api/v1/authentication/sign-in', { username, password }, { as: '' })
  return auth
}

async function main() {
  console.log(`Seeding MineSense demo data at ${BASE}`)
  const admin = await signIn(ADMIN.username, ADMIN.password)
  token = admin.accessToken

  // ---------- Operations (EP08) ----------
  const unit = await post('/api/v1/mining-units', { businessCode: 'UM-TR', name: 'Tambo Real', region: 'Arequipa' })
  for (const [name, kind] of [['Tajo Norte', 'Pit'], ['Botadero Este', 'Dump'], ['Chancadora 1', 'Crusher'], ['Rampa 3', 'Ramp'], ['Taller Central', 'Workshop']])
    await post(`/api/v1/mining-units/${unit.id}/locations`, { name, kind })
  const fleets = [
    await post('/api/v1/fleets', { miningUnitId: unit.id, name: 'Acarreo Norte' }),
    await post('/api/v1/fleets', { miningUnitId: unit.id, name: 'Acarreo Sur' }),
  ]
  const trucks = []
  for (let i = 0; i < 8; i++) {
    const fleet = fleets[i < 4 ? 0 : 1]
    trucks.push(await post('/api/v1/vehicles', { code: `HT-${101 + i}`, model: i % 2 ? 'Komatsu 930E' : 'CAT 793F', fleetId: fleet.id }))
  }
  const names = [
    ['Rosa', 'Quispe'], ['Julio', 'Mamani'], ['Carmen', 'Huanca'], ['Luis', 'Condori'],
    ['Patricia', 'Flores'], ['Miguel', 'Ccama'], ['Elena', 'Torres'], ['Raúl', 'Apaza'],
  ]
  const operators = []
  for (const [index, [firstName, lastName]] of names.entries())
    operators.push(await post('/api/v1/operators', { employeeCode: `OP-${2001 + index}`, firstName, lastName, miningUnitId: unit.id }))
  for (const [index, operator] of operators.entries())
    await post('/api/v1/operator-assignments', {
      operatorId: operator.id, vehicleId: trucks[index].id, shift: index < 6 ? 'Day' : 'Night', validFrom: today,
    })
  await put(`/api/v1/vehicles/${trucks[7].id}`, { model: trucks[7].model, status: 'InMaintenance', fleetId: fleets[1].id })
  log(`Operations: 1 unidad, 2 flotas, ${trucks.length} camiones, ${operators.length} operadores`)

  // ---------- Identity & Access (EP08) ----------
  const people = [
    { username: 'supervisor', fullName: 'Ana Villanueva', password: DEMO_PASSWORD, role: 'Supervisor' },
    { username: 'jefe.seguridad', fullName: 'Carlos Medina', password: DEMO_PASSWORD, role: 'SafetyManager' },
    { username: 'operador.rosa', fullName: 'Rosa Quispe', password: DEMO_PASSWORD, role: 'Operator' },
  ]
  const users = {}
  for (const person of people) {
    const created = await post('/api/v1/users', person, { allow: [409] })
    const id = created?.id ?? (await get('/api/v1/users')).find((u) => u.username === person.username).id
    await post(`/api/v1/users/${id}/mining-units`, { miningUnitId: unit.id }, { allow: [409, 422] })
    users[person.role] = id
  }
  await post(`/api/v1/users/${admin.id}/mining-units`, { miningUnitId: unit.id }, { allow: [409, 422] })
  token = (await signIn(ADMIN.username, ADMIN.password)).accessToken // new token carries the mining_unit scope
  log('Identity: supervisor, jefe de seguridad y operador con alcance a Tambo Real')

  // ---------- Device Management (EP06) ----------
  const now = new Date().toISOString()
  for (const [index, operator] of operators.entries()) {
    const kinds = index === 5 ? ['camera'] : ['camera', 'wearable'] // Miguel has no wearable: pre-shift fails
    for (const type of kinds) {
      const device = await post('/api/v1/devices', { deviceCode: `${type === 'camera' ? 'CAM' : 'WR'}-${101 + index}`, deviceType: type, organizationId: unit.id })
      await put(`/api/v1/devices/${device.id}/assignment`, { operatorId: operator.id, vehicleId: type === 'camera' ? trucks[index].id : null })
      const battery = index === 4 && type === 'wearable' ? 14 : 55 + ((index * 7) % 40)
      await post(`/api/v1/devices/${device.id}/status`, {
        status: index === 7 && type === 'camera' ? 'Limited' : 'Available',
        batteryLevel: battery,
        reportedAt: now,
        availableComponents: type === 'camera' ? ['infrared', 'lens'] : ['ppg', 'vibration'],
      })
    }
  }
  log('Devices: cámara y wearable por cabina (uno sin wearable, uno con batería baja)')

  // ---------- Fleet Monitoring (EP04) ----------
  const locations = ['Tajo Norte', 'Rampa 3', 'Botadero Este', 'Tajo Norte', 'Chancadora 1', 'Rampa 3', 'Botadero Este', 'Taller Central']
  const cabins = []
  for (const [index, operator] of operators.entries()) {
    const fleet = fleets[index < 4 ? 0 : 1]
    const cabin = {
      operatorId: operator.id, fullName: operator.fullName, vehicleCode: trucks[index].code, fleet: fleet.name,
      shift: index < 6 ? 'Day' : 'Night', location: locations[index], miningUnitId: unit.id, fleetId: fleet.id,
    }
    await post('/api/v1/fleet-operators', cabin)
    cabins.push(cabin)
  }
  log('Fleet Monitoring: 8 cabinas en el tablero')

  // ---------- Privacy & Audit (EP09) ----------
  for (const [index, operator] of operators.entries())
    await post('/api/v1/consents', {
      operatorId: operator.id, policyVersion: 'v1.0',
      purposes: index === 7 ? ['fatigue-monitoring'] : ['fatigue-monitoring', 'safety-alerts', 'operational-analytics'],
      status: index === 7 ? 'Declined' : 'Accepted',
    })
  log('Privacy: consentimientos (uno rechazado)')

  // ---------- Monitoring + Fatigue + Alerts + Fleet + Analytics + Feedback + Incidents ----------
  const readings = {
    Normal: { perclos: 0.07, blinkRatePerMinute: 13, heartRateVariabilityMs: 55 },
    Warning: { perclos: 0.21, blinkRatePerMinute: 24, heartRateVariabilityMs: 33 },
    Critical: { perclos: 0.47, blinkRatePerMinute: 30, heartRateVariabilityMs: 16 },
  }
  const plan = ['Critical', 'Warning', 'Normal', 'Critical', 'Warning', null, 'Normal', null]
  const raised = []
  for (const [index, cabin] of cabins.entries()) {
    const level = plan[index]
    if (!level) continue
    const session = await post('/api/v1/monitoring-sessions', { operatorId: cabin.operatorId, vehicleCode: cabin.vehicleCode, availableSignals: ['EyeTracking', 'HeartRate'] })
    if (index === 4) await post(`/api/v1/monitoring-sessions/${session.id}/signal-losses`, { signal: 'HeartRate' })
    for (const step of level === 'Normal' ? ['Normal', 'Normal'] : ['Normal', level]) {
      const assessment = await post('/api/v1/fatigue-assessments', { operatorId: cabin.operatorId, monitoringSessionId: session.id, ...readings[step] })
      await put(`/api/v1/fleet-operators/${cabin.operatorId}/risk-level`, { riskLevel: assessment.riskLevel })
      if (!assessment.requiresAlert) continue
      const alert = await post('/api/v1/alerts', { operatorId: cabin.operatorId, assessmentId: assessment.id, severity: assessment.riskLevel })
      await post('/api/v1/analytics/fatigue-events', {
        operatorId: cabin.operatorId, alertId: alert.id, severity: alert.severity, occurredAt: alert.issuedAt,
        shift: cabin.shift, mineSite: unit.name, location: cabin.location, fleet: cabin.fleet,
      })
      await put(`/api/v1/feedback-alerts/${alert.id}`, { operationId: unit.id })
      const incident = alert.severity === 'Critical'
        ? await post('/api/v1/incidents', { operatorId: cabin.operatorId, alertId: alert.id, severity: alert.severity })
        : null
      raised.push({ alert, incident, cabin })
    }
  }
  log(`Detección: ${raised.length} alertas, ${raised.filter((r) => r.incident).length} incidentes`)

  // Supervisor work: acknowledge, escalate, handle incidents, feedback.
  const supervisor = await signIn('supervisor', DEMO_PASSWORD)
  const asSupervisor = { as: supervisor.accessToken }
  const warnings = raised.filter((r) => r.alert.severity === 'Warning')
  if (warnings[0]) await post(`/api/v1/alerts/${warnings[0].alert.id}/acknowledgements`)
  if (warnings[1]) await post(`/api/v1/alerts/${warnings[1].alert.id}/escalations`)
  const critical = raised.filter((r) => r.incident)
  if (critical[0]) {
    const id = critical[0].incident.id
    await patch(`/api/v1/incidents/${id}/assignment`, { assigneeId: supervisor.id })
    await post(`/api/v1/incidents/${id}/actions`, {
      actionType: 'OperatorContact', description: 'Llamada por radio a la cabina', result: 'Operador responde y detiene el camión',
      performedBy: supervisor.id, performedAt: new Date().toISOString(),
    })
    await post(`/api/v1/incidents/${id}/actions`, {
      actionType: 'OperatorRelief', description: 'Relevo con operador de reserva', result: 'Relevo completado',
      performedBy: supervisor.id, performedAt: new Date().toISOString(),
    })
    await patch(`/api/v1/incidents/${id}/closure`, { resolution: 'Operador relevado y derivado a descanso' })
    await post('/api/v1/access-log/entries', {
      userId: supervisor.id, resourceType: 'Incident', resourceId: id, fields: ['status', 'actions'], purpose: 'incident-review', decision: 'Granted',
    })
  }
  if (critical[1]) await patch(`/api/v1/incidents/${critical[1].incident.id}/assignment`, { assigneeId: supervisor.id })

  const classifications = ['CONFIRMED', 'FALSE_ALARM', 'CONFIRMED', 'INCONCLUSIVE']
  for (const [index, item] of raised.slice(0, 4).entries()) {
    const classification = classifications[index]
    await post(`/api/v1/alerts/${item.alert.id}/feedback`, {
      classification, reason: classification === 'FALSE_ALARM' ? 'Polvo sobre la cámara' : null, reportedBy: supervisor.id,
    }, asSupervisor)
    if (index < 2)
      await post(`/api/v1/alerts/${item.alert.id}/feedback/reviews`, {
        classification: 'CONFIRMED', observation: 'Revisado con el video de cabina', reviewedBy: supervisor.id,
      }, asSupervisor)
  }
  log('Supervisor: alertas atendidas, incidente cerrado y feedback registrado')

  // Access log entries of the supervisor and one denied attempt.
  for (const cabin of cabins.slice(0, 4))
    await post('/api/v1/access-log/entries', {
      userId: supervisor.id, resourceType: 'FatigueAssessment', resourceId: cabin.operatorId, fields: ['perclos', 'riskLevel'], purpose: 'shift-supervision', decision: 'Granted',
    })
  await post('/api/v1/access-log/entries', {
    userId: users.Operator, resourceType: 'FatigueAssessment', resourceId: cabins[1].operatorId, fields: ['perclos'], purpose: 'shift-supervision', decision: 'Denied',
  })

  // ---------- Analytics history (EP11): 60 days of observations and one preventive measure ----------
  const shifts = ['Day', 'Night']
  let history = 0
  for (let day = 2; day <= 60; day += 1) {
    const count = day > 30 ? 2 : 1 // fewer alerts after the measure applied 30 days ago
    for (let i = 0; i < count; i++) {
      const cabin = cabins[(day + i) % cabins.length]
      await post('/api/v1/analytics/fatigue-events', {
        operatorId: cabin.operatorId, alertId: uuid(), severity: (day + i) % 4 === 0 ? 'Critical' : 'Warning',
        occurredAt: daysAgo(day, (day * 5 + i * 7) % 24), shift: shifts[(day + i) % 2], mineSite: unit.name,
        location: locations[(day + i) % locations.length], fleet: cabin.fleet,
      })
      history++
    }
  }
  await post('/api/v1/analytics/preventive-measures', {
    name: 'Pausa activa cada 2 horas', measureType: 'PreventivePause', description: 'Pausa de 10 minutos en el turno noche',
    appliedAt: daysAgo(30), registeredBy: users.SafetyManager, shift: 'Night', mineSite: unit.name, location: null, fleet: null,
  })
  log(`Analytics: ${history} observaciones históricas y 1 medida preventiva`)

  // ---------- Reporting (EP10) and compliance request (EP09) ----------
  await post('/api/v1/report-definitions', { code: 'fatiga-por-turno', name: 'Alertas de fatiga por turno', version: 'v1.0', dataSources: ['alerts', 'fatigue-assessments'] }, { allow: [409] })
  await post('/api/v1/report-definitions', { code: 'fatiga-por-turno', name: 'Alertas de fatiga por turno', version: 'v1.1', dataSources: ['alerts', 'fatigue-assessments', 'incidents'] }, { allow: [409] })
  await post('/api/v1/report-definitions', { code: 'incidentes-criticos', name: 'Incidentes críticos atendidos', version: 'v1.0', dataSources: ['incidents'] }, { allow: [409] })
  for (const format of ['Pdf', 'Csv'])
    await post('/api/v1/reports/safety', {
      from: daysAgo(14), to: new Date().toISOString(), siteId: unit.name, fleetId: fleets[0].name,
      definitionCode: 'fatiga-por-turno', format, requestedBy: users.SafetyManager,
    })
  await post('/api/v1/audit/compliance-reports', { from: daysAgo(30), to: new Date().toISOString(), siteId: unit.name, fleetId: null, requestedBy: users.SafetyManager })
  log('Reporting: 3 indicadores y 2 reportes listos para descargar')

  // ---------- Edge Synchronization (EP07) ----------
  const batchId = uuid()
  const batch = {
    batchId, edgeNodeId: uuid(), deviceId: uuid(), sequenceNumber: 1, capturedAt: daysAgo(0, 6),
    events: [
      { eventId: uuid(), occurredAt: daysAgo(0, 5), eventType: 'FatigueReading', payload: { perclos: 0.18, vehicle: 'HT-103' } },
      { eventId: uuid(), occurredAt: daysAgo(0, 5), eventType: 'SignalLost', payload: { signal: 'HeartRate' } },
      { eventId: uuid(), occurredAt: daysAgo(0, 6), eventType: 'SignalRestored', payload: { signal: 'HeartRate' } },
    ],
  }
  await post('/api/v1/synchronization-batches', batch)
  await post('/api/v1/synchronization-batches', batch) // retry: idempotent 200
  log(`Edge: lote ${batchId.slice(0, 8)} sincronizado (y reintentado)`)

  console.log('\nListo. Inicia sesión como supervisor / Supervisor-2026 o admin / Admin-Local-2026.')
}

main().catch((error) => {
  console.error(`\n✗ ${error.message}`)
  process.exit(1)
})
