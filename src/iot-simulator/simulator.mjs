#!/usr/bin/env node
// MineSense fleet IoT simulator: one virtual cabin gateway per truck of the demo mine.
//
// Each cabin samples its sensors once per second (eye-tracking camera: PERCLOS and blink rate;
// wearable: heart rate and HRV), publishes the raw telemetry over MQTT (ADR-004, edge -> cloud),
// and every ASSESS_SECONDS sends the window to Fatigue Detection, which starts the Kafka chain
// (Fleet risk -> Alert -> Analytics / Feedback -> Incident). It also reports the devices' battery
// to Device Management, loses and restores signals in Monitoring, moves the trucks in Fleet
// Monitoring and, when a cabin drives out of coverage, buffers its readings and warnings and
// sends them later as an idempotent Edge Synchronization batch (US13/US14).
//
// The values are random walks, never typed by a person: the dashboard only reads them through
// GET /api/v1/telemetry/* (this process), behind the same gateway and Identity & Access tokens.
import { spawn } from 'node:child_process'
import { randomUUID } from 'node:crypto'
import http from 'node:http'
import { fileURLToPath } from 'node:url'
import { createVerifier } from './jwt.mjs'
import { MqttPublisher } from './mqtt.mjs'

const env = (name, fallback) => process.env[name] ?? fallback
const BASE = env('GATEWAY_URL', 'http://gateway:8080')
const PORT = Number(env('PORT', '8080'))
const ADMIN = { username: env('SIM_USERNAME', 'admin'), password: env('SIM_PASSWORD', 'Admin-Local-2026') }
const ASSESS_SECONDS = Number(env('ASSESS_SECONDS', '30'))
const INTENSITY = Number(env('SIM_INTENSITY', '1')) // multiplies the fatigue drift
const AUTO_SEED = env('AUTO_SEED', 'true') === 'true'
const HISTORY = 180 // seconds of telemetry kept per cabin

const log = {
  info: (text) => console.log(`${new Date().toISOString()} INFO  ${text}`),
  warn: (text) => console.log(`${new Date().toISOString()} WARN  ${text}`),
  error: (text) => console.log(`${new Date().toISOString()} ERROR ${text}`),
}

// ---------------------------------------------------------------- random helpers
const rand = (min, max) => min + Math.random() * (max - min)
const gauss = () => {
  let u = 0
  while (u === 0) u = Math.random()
  return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * Math.random())
}
const clamp = (value, min, max) => Math.min(max, Math.max(min, value))
const round = (value, digits = 2) => (value == null ? null : Number(value.toFixed(digits)))
const chance = (probability) => Math.random() < probability
const pick = (list) => list[Math.floor(Math.random() * list.length)]

// Same thresholds as FatigueRiskPolicy (Fatigue Detection) and the Python edge agent.
export function classify({ perclos, blinkRatePerMinute, heartRateVariabilityMs }) {
  if (perclos >= 0.4 || (perclos >= 0.25 && heartRateVariabilityMs < 20)) return 'Critical'
  if (perclos >= 0.15 || blinkRatePerMinute >= 25 || heartRateVariabilityMs < 30) return 'Warning'
  return 'Normal'
}

// ---------------------------------------------------------------- gateway client
let token = null
let signedInAt = 0
async function signIn() {
  const response = await fetch(`${BASE}/api/v1/authentication/sign-in`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(ADMIN),
  })
  if (!response.ok) throw new Error(`sign-in ${response.status}`)
  token = (await response.json()).accessToken
  signedInAt = Date.now()
}

const stats = { requests: 0, failures: 0, assessments: 0, deviceReports: 0, signalChanges: 0, moves: 0, batches: 0, alerts: 0 }

async function call(method, path, body, { allow = [], retry = true } = {}) {
  if (!token || Date.now() - signedInAt > 10 * 60 * 1000) await signIn()
  stats.requests++
  const response = await fetch(BASE + path, {
    method,
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (response.status === 401 && retry) {
    await signIn()
    return call(method, path, body, { allow, retry: false })
  }
  const text = await response.text()
  let data = null
  try {
    data = text ? JSON.parse(text) : null
  } catch {
    data = text
  }
  if (!response.ok && !allow.includes(response.status)) {
    stats.failures++
    throw new Error(`${method} ${path} -> ${response.status} ${String(text).slice(0, 200)}`)
  }
  return { status: response.status, data }
}
const get = async (path) => (await call('GET', path)).data
const post = async (path, body, options) => (await call('POST', path, body ?? {}, options)).data
const put = async (path, body, options) => (await call('PUT', path, body ?? {}, options)).data

// ---------------------------------------------------------------- IoT event feed
const feed = []
function emit(kind, cabin, text, extra = {}) {
  feed.unshift({ id: randomUUID(), at: new Date().toISOString(), kind, operatorId: cabin?.operatorId, vehicleCode: cabin?.vehicleCode, text, ...extra })
  feed.length = Math.min(feed.length, 80)
}

const edgeBatches = []

// ---------------------------------------------------------------- one cabin gateway
const ROUTE = ['Tajo Norte', 'Rampa 3', 'Chancadora 1', 'Rampa 3', 'Botadero Este', 'Rampa 3']
// Deep in the pit the radio link drops: those stretches are where a cabin can lose coverage.
const LOW_COVERAGE = new Set(['Tajo Norte', 'Botadero Este'])

class Cabin {
  constructor(operator, index) {
    Object.assign(this, {
      operatorId: operator.operatorId,
      fullName: operator.fullName,
      vehicleCode: operator.vehicleCode,
      fleet: operator.fleet,
      shift: operator.shift,
      location: operator.location,
      miningUnitId: operator.miningUnitId,
    })
    // Two "tired" profiles per fleet make the demo show warnings and the odd critical event.
    this.profile = index % 4 === 0 ? 'cansado' : index % 4 === 3 ? 'somnoliento' : 'descansado'
    // Tired operators start the shift already somewhat fatigued, so the demo shows alerts early.
    this.fatigue = this.profile === 'descansado' ? rand(0.02, 0.12) : rand(0.18, 0.3)
    this.drift = ({ descansado: rand(0.00008, 0.00025), cansado: rand(0.0006, 0.0009), somnoliento: rand(0.0012, 0.0016) })[this.profile] * INTENSITY
    this.heartBase = rand(68, 84)
    this.speed = rand(18, 32)
    this.routeIndex = Math.max(0, ROUTE.indexOf(this.location))
    this.nextMoveAt = Date.now() + rand(60, 150) * 1000
    this.history = []
    this.session = null
    this.state = 'idle'
    this.reason = 'Sin sesión de monitoreo'
    this.signals = { EyeTracking: true, HeartRate: true }
    this.signalBackAt = {}
    this.devices = []
    this.wearableBattery = rand(55, 95)
    this.cameraBattery = rand(80, 100)
    this.microsleep = 0
    this.relieveAt = null
    this.lastAssessment = null
    this.lastAssessAt = Date.now() - rand(0, ASSESS_SECONDS) * 1000
    this.lastDeviceReportAt = 0
    this.coverage = true
    this.coverageBackAt = null
    this.edgeBuffer = []
    this.edgeNodeId = randomUUID()
    this.sequence = 0
    this.busy = false
  }

  get streaming() {
    return this.state === 'streaming' || this.state === 'offline'
  }

  sample(now) {
    // Fatigue drifts up while driving; an active pause or a relief brings it down.
    this.fatigue = clamp(this.fatigue + this.drift + gauss() * 0.002, 0, 1)
    if (this.fatigue > 0.3 && chance(0.0015)) {
      this.fatigue = clamp(this.fatigue - rand(0.12, 0.25), 0.03, 1)
      emit('pause', this, `${this.vehicleCode}: pausa activa del operador`)
    }
    if (this.relieveAt && now >= this.relieveAt) {
      this.fatigue = rand(0.03, 0.08)
      this.relieveAt = null
      emit('relief', this, `${this.vehicleCode}: operador relevado, fatiga reiniciada`)
    }
    // Microsleeps: a few seconds with the eyes closed, more likely the more tired the operator is.
    if (this.microsleep > 0) this.microsleep--
    else if (this.fatigue > 0.45 && chance(0.004 * this.fatigue)) {
      this.microsleep = Math.round(rand(2, 5))
      emit('microsleep', this, `${this.vehicleCode}: microsueño detectado por la cámara (${this.microsleep} s)`)
    }

    const f = this.fatigue
    const eyes = this.signals.EyeTracking
    const wrist = this.signals.HeartRate
    const perclos = eyes ? clamp(0.04 + 0.55 * f ** 1.5 + gauss() * 0.012 + (this.microsleep ? rand(0.25, 0.45) : 0), 0, 1) : null
    const blink = eyes ? clamp(11 + 19 * f + gauss() * 1.4, 2, 45) : null
    const heartRate = wrist ? clamp(this.heartBase - 7 * f + 3 * Math.sin(now / 9000) + gauss() * 1.6, 48, 130) : null
    const hrv = wrist ? clamp(62 - 46 * f + gauss() * 2.5, 8, 110) : null

    // Truck speed: random walk, slow on the ramp, stopped at the crusher and dump while loading.
    const stopZone = ['Chancadora 1', 'Botadero Este'].includes(this.location) && Math.sin(now / 20000) > 0.4
    this.speed = stopZone ? Math.max(0, this.speed - 4) : clamp(this.speed + gauss() * 1.5, this.location === 'Rampa 3' ? 8 : 15, this.location === 'Rampa 3' ? 18 : 42)

    this.wearableBattery = clamp(this.wearableBattery - rand(0.004, 0.009), 0, 100)
    this.cameraBattery = clamp(this.cameraBattery + 0.01, 0, 100) // charged by the truck

    const point = {
      t: new Date(now).toISOString(),
      perclos: round(perclos, 3),
      blinkRatePerMinute: round(blink, 1),
      heartRate: round(heartRate, 0),
      heartRateVariabilityMs: round(hrv, 1),
      speedKmh: round(this.speed, 1),
      eyesClosed: !!this.microsleep,
      wearableBattery: round(this.wearableBattery, 1),
      coverage: this.coverage,
    }
    this.history.push(point)
    if (this.history.length > HISTORY) this.history.shift()
    return point
  }

  window() {
    const last = this.history.slice(-ASSESS_SECONDS)
    const mean = (key) => {
      const values = last.map((p) => p[key]).filter((v) => v != null)
      return values.length ? values.reduce((a, b) => a + b, 0) / values.length : null
    }
    return { perclos: mean('perclos'), blinkRatePerMinute: mean('blinkRatePerMinute'), heartRateVariabilityMs: mean('heartRateVariabilityMs') }
  }

  snapshot(full = true) {
    return {
      operatorId: this.operatorId,
      fullName: this.fullName,
      vehicleCode: this.vehicleCode,
      fleet: this.fleet,
      shift: this.shift,
      location: this.location,
      profile: this.profile,
      state: this.state,
      reason: this.reason,
      sessionId: this.session?.id ?? null,
      signals: this.signals,
      coverage: this.coverage,
      edgeBuffered: this.edgeBuffer.length,
      microsleep: this.microsleep > 0,
      fatigueIndex: round(this.fatigue, 3),
      current: this.history.at(-1) ?? null,
      lastAssessment: this.lastAssessment,
      nextAssessmentInSeconds: this.streaming ? Math.max(0, Math.round(ASSESS_SECONDS - (Date.now() - this.lastAssessAt) / 1000)) : null,
      devices: this.devices.map((d) => ({ id: d.id, deviceCode: d.deviceCode, deviceType: d.deviceType })),
      history: full ? this.history : this.history.slice(-60),
    }
  }
}

// ---------------------------------------------------------------- fleet orchestration
const cabins = new Map()
const startedAt = Date.now()
let mqtt
let ready = false
let unitName = 'mina'

async function waitForGateway() {
  for (let attempt = 1; ; attempt++) {
    try {
      await signIn()
      await get('/api/v1/mining-units')
      return
    } catch (error) {
      if (attempt % 6 === 1) log.info(`Esperando al gateway y a Identity & Access (${error.message})`)
      await new Promise((resolve) => setTimeout(resolve, 5000))
    }
  }
}

async function seedIfEmpty() {
  const units = await get('/api/v1/mining-units')
  if (units?.length || !AUTO_SEED) return
  if (!process.env.DEMO_PASSWORD) {
    log.warn('Entorno vacío y sin DEMO_PASSWORD: no se cargan datos de demostración')
    return
  }
  log.info('Entorno vacío: cargando la mina de demostración (seed-demo.mjs)')
  const script = fileURLToPath(new URL('./seed-demo.mjs', import.meta.url))
  await new Promise((resolve, reject) => {
    const child = spawn(process.execPath, [script], { env: { ...process.env, BASE_URL: BASE }, stdio: 'inherit' })
    child.on('exit', (code) => (code === 0 ? resolve() : reject(new Error(`seed-demo terminó con ${code}`))))
  })
}

async function refreshFleet() {
  const [operators, sessions, deviceList, units] = await Promise.all([
    get('/api/v1/fleet-operators'),
    get('/api/v1/monitoring-sessions').catch(() => []),
    get('/api/v1/devices').catch(() => []),
    get('/api/v1/mining-units').catch(() => []),
  ])
  if (units?.[0]?.name) unitName = units[0].name
  operators.forEach((operator, index) => {
    if (!cabins.has(operator.operatorId)) cabins.set(operator.operatorId, new Cabin(operator, index))
    const cabin = cabins.get(operator.operatorId)
    cabin.location = operator.location ?? cabin.location
    cabin.devices = deviceList.filter((d) => d.assignedOperatorId === operator.operatorId)
    const camera = cabin.devices.find((d) => d.deviceType === 'camera')
    const wearable = cabin.devices.find((d) => d.deviceType === 'wearable')
    if (!cabin.seededBattery) {
      if (wearable?.batteryLevel != null) cabin.wearableBattery = wearable.batteryLevel
      if (camera?.batteryLevel != null) cabin.cameraBattery = camera.batteryLevel
      cabin.seededBattery = true
    }
    const open = sessions.find((s) => s.operatorId === operator.operatorId && s.status !== 'Stopped')
    cabin.session = open ? { id: open.sessionId ?? open.id, ...open } : null
    if (open) {
      cabin.signals = {
        EyeTracking: !(open.missingSignals ?? []).includes('EyeTracking'),
        HeartRate: !(open.missingSignals ?? []).includes('HeartRate'),
      }
      // A signal lost before this gateway started (e.g. by the seed) comes back after a while.
      for (const signal of ['EyeTracking', 'HeartRate'])
        if (!cabin.signals[signal] && !cabin.signalBackAt[signal]) cabin.signalBackAt[signal] = Date.now() + rand(20, 60) * 1000
      if (cabin.state === 'idle') emit('session', cabin, `${cabin.vehicleCode}: gateway transmitiendo (sesión ${String(cabin.session.id).slice(0, 8)})`)
      cabin.state = cabin.coverage ? 'streaming' : 'offline'
      cabin.reason = cabin.coverage ? 'Transmitiendo a 1 Hz' : 'Sin cobertura: guardando en el buffer edge'
    } else {
      if (cabin.state !== 'idle') emit('session', cabin, `${cabin.vehicleCode}: sesión cerrada, el gateway deja de transmitir`)
      cabin.state = 'idle'
    }
  })
}

// At start-up every cabin that passes the pre-shift check and has consent starts its session.
async function openSessions() {
  for (const cabin of cabins.values()) {
    if (cabin.session) continue
    try {
      const consent = await get(`/api/v1/consents/operator/${cabin.operatorId}/current`).catch(() => null)
      if (consent?.blocksMonitoring || consent?.status === 'Declined') {
        cabin.reason = 'El operador no dio su consentimiento: no se monitorea'
        continue
      }
      const check = await get(`/api/v1/devices/pre-shift-availability?operatorId=${cabin.operatorId}&batteryThreshold=20`).catch(() => null)
      if (check && check.recommendation !== 'StartMonitoring') {
        cabin.reason = `Pre-turno no aprobado: ${(check.missingDeviceTypes ?? []).length ? `falta ${check.missingDeviceTypes.join(', ')}` : check.status}`
        continue
      }
      const session = await post('/api/v1/monitoring-sessions', { operatorId: cabin.operatorId, vehicleCode: cabin.vehicleCode, availableSignals: ['EyeTracking', 'HeartRate'] }, { allow: [409] })
      if (session?.id || session?.sessionId) log.info(`${cabin.vehicleCode}: sesión de monitoreo abierta`)
    } catch (error) {
      log.warn(`${cabin.vehicleCode}: no se pudo abrir la sesión (${error.message})`)
    }
  }
  await refreshFleet()
}

async function assess(cabin) {
  const window = cabin.window()
  if (window.perclos == null) {
    emit('skip', cabin, `${cabin.vehicleCode}: sin señal ocular, no se evalúa esta ventana`)
    return
  }
  // Without the wearable the cabin sends the last HRV it measured (the backend needs a value > 0).
  if (window.heartRateVariabilityMs == null) window.heartRateVariabilityMs = cabin.lastAssessment?.heartRateVariabilityMs ?? 45
  if (window.blinkRatePerMinute == null) window.blinkRatePerMinute = 14
  const reading = {
    perclos: round(window.perclos, 3),
    blinkRatePerMinute: round(window.blinkRatePerMinute, 1),
    heartRateVariabilityMs: round(window.heartRateVariabilityMs, 1),
  }
  const capturedAt = new Date().toISOString()
  const localRisk = classify(reading)

  if (!cabin.coverage) {
    // Offline: classified in the cabin, warned on the cabin display, buffered for the batch.
    const readingEventId = randomUUID()
    if (localRisk !== 'Normal')
      cabin.edgeBuffer.push({
        eventId: randomUUID(), occurredAt: capturedAt, eventType: 'CabinWarningIssued',
        payload: { operatorId: cabin.operatorId, monitoringSessionId: cabin.session.id, severity: localRisk, message: localRisk === 'Critical' ? 'Detén el camión de forma segura' : 'Toma una pausa activa', deliveredChannels: ['display', 'buzzer'], unavailableChannels: [], issuedOffline: true, readingEventId },
      })
    cabin.edgeBuffer.push({
      eventId: readingEventId, occurredAt: capturedAt, eventType: 'FatigueReadingClassified',
      payload: { operatorId: cabin.operatorId, monitoringSessionId: cabin.session.id, ...reading, riskLevel: localRisk, classifiedOffline: true },
    })
    cabin.lastAssessment = { ...reading, riskLevel: localRisk, assessedAt: capturedAt, origin: 'Edge', requiresAlert: localRisk !== 'Normal' }
    if (localRisk !== 'Normal') emit('cabin-warning', cabin, `${cabin.vehicleCode}: advertencia ${localRisk === 'Critical' ? 'crítica' : 'de fatiga'} en cabina sin cobertura (se sincroniza al volver)`, { severity: localRisk })
    return
  }

  const assessment = await post('/api/v1/fatigue-assessments', { operatorId: cabin.operatorId, monitoringSessionId: cabin.session.id, ...reading })
  stats.assessments++
  cabin.lastAssessment = { ...reading, id: assessment.id, riskLevel: assessment.riskLevel, assessedAt: assessment.assessedAt ?? capturedAt, origin: 'Cloud', requiresAlert: assessment.requiresAlert }
  if (assessment.requiresAlert) {
    stats.alerts++
    emit('alert', cabin, `${cabin.vehicleCode}: PERCLOS ${(reading.perclos * 100).toFixed(0)} % → riesgo ${assessment.riskLevel === 'Critical' ? 'crítico' : 'de advertencia'}, alerta en camino`, { severity: assessment.riskLevel })
    // The operator reacts: after a warning the wearable vibrates and often they take a pause;
    // after a critical alert the supervisor relieves them within one or two minutes.
    if (assessment.riskLevel === 'Critical') cabin.relieveAt = Date.now() + rand(60, 120) * 1000
    else if (chance(0.5)) cabin.fatigue = clamp(cabin.fatigue - rand(0.05, 0.1), 0.03, 1)
  }
}

async function reportDevices(cabin) {
  for (const device of cabin.devices) {
    const camera = device.deviceType === 'camera'
    const battery = camera ? cabin.cameraBattery : cabin.wearableBattery
    const signal = camera ? cabin.signals.EyeTracking : cabin.signals.HeartRate
    const status = !signal ? 'Interrupted' : battery < 15 ? 'Limited' : 'Available'
    await post(`/api/v1/devices/${device.id}/status`, {
      status,
      batteryLevel: Math.round(battery),
      reportedAt: new Date().toISOString(),
      availableComponents: camera ? (signal ? ['infrared', 'lens'] : ['lens']) : (signal ? ['ppg', 'vibration'] : ['vibration']),
    })
    stats.deviceReports++
  }
  if (cabin.wearableBattery < 6) {
    cabin.wearableBattery = rand(96, 100)
    emit('battery', cabin, `${cabin.vehicleCode}: wearable cambiado por uno cargado`)
  }
}

async function toggleSignals(cabin, now) {
  for (const signal of ['HeartRate', 'EyeTracking']) {
    const on = cabin.signals[signal]
    if (on && chance(signal === 'HeartRate' ? 0.0005 : 0.0002)) {
      await post(`/api/v1/monitoring-sessions/${cabin.session.id}/signal-losses`, { signal }, { allow: [409, 422] })
      cabin.signals[signal] = false
      cabin.signalBackAt[signal] = now + rand(signal === 'HeartRate' ? 15 : 8, signal === 'HeartRate' ? 40 : 20) * 1000
      stats.signalChanges++
      emit('signal-lost', cabin, `${cabin.vehicleCode}: se perdió la señal de ${signal === 'HeartRate' ? 'frecuencia cardíaca (wearable)' : 'la cámara'}`)
    } else if (!on && cabin.signalBackAt[signal] && now >= cabin.signalBackAt[signal]) {
      await post(`/api/v1/monitoring-sessions/${cabin.session.id}/signal-restorations`, { signal }, { allow: [409, 422] })
      cabin.signals[signal] = true
      delete cabin.signalBackAt[signal]
      stats.signalChanges++
      emit('signal-restored', cabin, `${cabin.vehicleCode}: señal de ${signal === 'HeartRate' ? 'frecuencia cardíaca' : 'cámara'} restaurada`)
    }
  }
}

async function move(cabin, now) {
  if (now < cabin.nextMoveAt) return
  cabin.routeIndex = (cabin.routeIndex + 1) % ROUTE.length
  cabin.location = ROUTE[cabin.routeIndex]
  cabin.nextMoveAt = now + rand(70, 160) * 1000
  await put(`/api/v1/fleet-operators/${cabin.operatorId}/vehicle`, { vehicleCode: cabin.vehicleCode, location: cabin.location })
  stats.moves++
  emit('move', cabin, `${cabin.vehicleCode} llegó a ${cabin.location}`)
}

let lastOutageAt = Date.now()
async function coverage(cabin, now) {
  if (cabin.coverage) {
    // At most one cabin out of coverage at a time, and only in the low-coverage stretches.
    const someoneOffline = [...cabins.values()].some((c) => !c.coverage)
    if (!someoneOffline && LOW_COVERAGE.has(cabin.location) && now - lastOutageAt > 150_000 && chance(0.01)) {
      cabin.coverage = false
      cabin.coverageBackAt = now + rand(50, 90) * 1000
      cabin.state = 'offline'
      cabin.reason = 'Sin cobertura: guardando en el buffer edge'
      lastOutageAt = now
      emit('offline', cabin, `${cabin.vehicleCode}: sin cobertura en ${cabin.location}, el gateway sigue detectando y guarda los eventos`)
    }
    return
  }
  if (now < cabin.coverageBackAt) return
  cabin.coverage = true
  cabin.state = 'streaming'
  cabin.reason = 'Transmitiendo a 1 Hz'
  if (!cabin.edgeBuffer.length) return
  const camera = cabin.devices.find((d) => d.deviceType === 'camera')
  const batch = {
    batchId: randomUUID(), edgeNodeId: cabin.edgeNodeId, deviceId: camera?.id ?? randomUUID(),
    sequenceNumber: ++cabin.sequence, capturedAt: new Date().toISOString(), events: cabin.edgeBuffer.splice(0),
  }
  const first = await call('POST', '/api/v1/synchronization-batches', batch)
  // The link flickers when it comes back: the same sealed batch is sent again and must be a no-op.
  const again = await call('POST', '/api/v1/synchronization-batches', batch)
  stats.batches++
  edgeBatches.unshift({
    batchId: batch.batchId, operatorId: cabin.operatorId, vehicleCode: cabin.vehicleCode, deviceId: batch.deviceId,
    edgeNodeId: batch.edgeNodeId, sequenceNumber: batch.sequenceNumber, events: batch.events.length,
    warnings: batch.events.filter((e) => e.eventType === 'CabinWarningIssued').length,
    sentAt: batch.capturedAt, firstStatus: first.status, retryStatus: again.status,
  })
  edgeBatches.length = Math.min(edgeBatches.length, 40)
  emit('online', cabin, `${cabin.vehicleCode}: cobertura recuperada, lote ${batch.batchId.slice(0, 8)} con ${batch.events.length} eventos sincronizado (reintento ${again.status}: sin duplicados)`)
}

async function tickCabin(cabin, now) {
  if (!cabin.streaming) return
  const point = cabin.sample(now)
  if (cabin.coverage)
    mqtt?.publish(`minesense/${unitName.toLowerCase().replace(/\s+/g, '-')}/cabins/${cabin.vehicleCode}/telemetry`, { operatorId: cabin.operatorId, ...point })
  // The sensors keep sampling while the previous second's calls to the cloud are still running.
  if (cabin.busy) return
  cabin.busy = true
  try {
    await toggleSignals(cabin, now)
    await coverage(cabin, now)
    if (now - cabin.lastAssessAt >= ASSESS_SECONDS * 1000) {
      cabin.lastAssessAt = now
      await assess(cabin)
    }
    if (cabin.coverage) {
      if (now - cabin.lastDeviceReportAt >= 30_000) {
        cabin.lastDeviceReportAt = now
        await reportDevices(cabin)
      }
      await move(cabin, now)
    }
  } catch (error) {
    log.warn(`${cabin.vehicleCode}: ${error.message}`)
  } finally {
    cabin.busy = false
  }
}

// ---------------------------------------------------------------- HTTP API for the dashboard
const verifyToken = createVerifier(`${BASE}/.well-known/jwks.json`)

function gatewayStatus() {
  const all = [...cabins.values()]
  return {
    startedAt: new Date(startedAt).toISOString(),
    uptimeSeconds: Math.round((Date.now() - startedAt) / 1000),
    sampleRateHz: 1,
    assessEverySeconds: ASSESS_SECONDS,
    mqtt: { connected: !!mqtt?.connected, broker: mqtt?.host ? `${mqtt.host}:${mqtt.port}` : null, published: mqtt?.published ?? 0, topic: `minesense/${unitName.toLowerCase().replace(/\s+/g, '-')}/cabins/+/telemetry` },
    cabins: { total: all.length, streaming: all.filter((c) => c.state === 'streaming').length, offline: all.filter((c) => c.state === 'offline').length, idle: all.filter((c) => c.state === 'idle').length },
    stats,
    ready,
  }
}

function send(res, status, body) {
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' })
  res.end(JSON.stringify(body))
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, 'http://local')
  if (url.pathname === '/health/live') return send(res, 200, { status: 'Healthy' })
  if (url.pathname === '/health/ready') return send(res, ready ? 200 : 503, { status: ready ? 'Healthy' : 'Starting' })
  if (req.method !== 'GET' || !url.pathname.startsWith('/api/v1/telemetry')) return send(res, 404, { message: 'No encontrado' })
  const claims = await verifyToken(req.headers.authorization)
  if (!claims) return send(res, 401, { message: 'Tu sesión expiró. Inicia sesión otra vez.' })

  const path = url.pathname.replace(/\/+$/, '')
  if (path === '/api/v1/telemetry/status') return send(res, 200, gatewayStatus())
  if (path === '/api/v1/telemetry/cabins')
    return send(res, 200, { generatedAt: new Date().toISOString(), gateway: gatewayStatus(), cabins: [...cabins.values()].map((c) => c.snapshot(url.searchParams.get('history') !== 'short')) })
  const one = path.match(/^\/api\/v1\/telemetry\/cabins\/([0-9a-f-]{36})$/i)
  if (one) {
    const cabin = cabins.get(one[1])
    return cabin ? send(res, 200, cabin.snapshot()) : send(res, 404, { message: 'Esa cabina no tiene gateway IoT.' })
  }
  if (path === '/api/v1/telemetry/events') {
    const operatorId = url.searchParams.get('operatorId')
    return send(res, 200, operatorId ? feed.filter((e) => e.operatorId === operatorId) : feed)
  }
  if (path === '/api/v1/telemetry/edge-batches') return send(res, 200, edgeBatches)
  return send(res, 404, { message: 'No encontrado' })
})

// ---------------------------------------------------------------- main
async function main() {
  server.listen(PORT, () => log.info(`API de telemetría en :${PORT}`))
  mqtt = new MqttPublisher({ host: env('MQTT_HOST', 'mosquitto'), port: Number(env('MQTT_PORT', '1883')), clientId: `minesense-fleet-sim-${process.pid}`, log })
  mqtt.start()

  await waitForGateway()
  await seedIfEmpty()
  await refreshFleet()
  await openSessions()
  ready = true
  log.info(`${cabins.size} cabinas · ${[...cabins.values()].filter((c) => c.streaming).length} transmitiendo`)

  setInterval(() => {
    const now = Date.now()
    for (const cabin of cabins.values()) tickCabin(cabin, now)
  }, 1000)
  setInterval(() => refreshFleet().catch((error) => log.warn(`refresco de flota: ${error.message}`)), 15_000)
  setInterval(() => {
    const s = gatewayStatus()
    log.info(`transmitiendo ${s.cabins.streaming}, sin cobertura ${s.cabins.offline} · evaluaciones ${stats.assessments} · alertas ${stats.alerts} · MQTT ${s.mqtt.published} · lotes ${stats.batches}`)
  }, 60_000)
}

if (process.argv[1] === fileURLToPath(import.meta.url))
  main().catch((error) => {
    log.error(error.stack ?? error.message)
    process.exit(1)
  })
