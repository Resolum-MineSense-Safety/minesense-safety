import {
  Activity, BatteryMedium, Camera, CheckCircle2, CircleDashed, CircleSlash, Eye, Gauge, HeartPulse, Play, Radio,
  ShieldAlert, Square, Timer, Waves, WifiOff, XCircle,
} from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { devices, fatigue, fleet, monitoring, privacy, telemetry } from '../api/services'
import { useToast } from '../components/Toasts'
import { LiveBadge, Reading } from '../components/live'
import StreamChart from '../components/StreamChart'
import { GATEWAY_STATE, riskOf, useTelemetry } from '../lib/telemetry'
import { Button, Empty, PageHeader, Panel, Pill, Ring, Spinner } from '../components/ui'
import { traceAssessment } from '../lib/flow'
import { label, pct, time } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const SIGNALS = [
  { key: 'EyeTracking', icon: Camera, device: 'Cámara infrarroja' },
  { key: 'HeartRate', icon: HeartPulse, device: 'Wearable de muñeca' },
]
const BAND = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal' }
const RISK_TEXT = { Critical: 'Riesgo crítico', Warning: 'Advertencia', Normal: 'Dentro de rango', neutral: 'Sin señal' }

function Stage({ step, title, done, children }) {
  return (
    <Panel
      title={
        <span className="flex items-center gap-2">
          <span className={`grid h-6 w-6 place-items-center rounded-full font-cond text-sm ${done ? 'bg-risk-normal text-white' : 'bg-basalt text-haul'}`}>{step}</span>
          {title}
        </span>
      }
    >
      {children}
    </Panel>
  )
}

function LiveReadings({ live }) {
  const now = live?.current
  const risk = riskOf(now)
  const battery = now?.wearableBattery
  return (
    <div className="grid grid-cols-2 gap-2">
      <Reading icon={Eye} name="PERCLOS" source="cámara" value={now?.perclos != null ? now.perclos * 100 : null} unit="%"
        tone={risk} status={now?.eyesClosed ? 'Ojos cerrados: microsueño' : RISK_TEXT[risk]} />
      <Reading icon={Activity} name="Parpadeos" source="cámara" value={now?.blinkRatePerMinute} unit="/min"
        tone={now?.blinkRatePerMinute == null ? 'neutral' : now.blinkRatePerMinute >= 25 ? 'Warning' : 'Normal'}
        status={now?.blinkRatePerMinute == null ? 'Sin señal' : now.blinkRatePerMinute >= 25 ? 'Elevado' : 'Normal'} />
      <Reading icon={HeartPulse} name="Frecuencia" source="wearable" value={now?.heartRate} unit="lpm"
        tone={now?.heartRate == null ? 'neutral' : 'Normal'} status={now?.heartRate == null ? 'Sin señal' : 'Pulso estable'} />
      <Reading icon={Waves} name="HRV" source="wearable" value={now?.heartRateVariabilityMs} unit="ms"
        tone={now?.heartRateVariabilityMs == null ? 'neutral' : now.heartRateVariabilityMs < 30 ? 'Warning' : 'Normal'}
        status={now?.heartRateVariabilityMs == null ? 'Sin señal' : now.heartRateVariabilityMs < 30 ? 'Baja: fatiga' : 'Normal'} />
      <Reading icon={Gauge} name="Velocidad" source="camión" value={now?.speedKmh} unit="km/h" tone="info" status={live?.location} />
      <Reading icon={BatteryMedium} name="Batería" source="wearable" value={battery} unit="%"
        tone={battery == null ? 'neutral' : battery < 20 ? 'Critical' : battery < 50 ? 'Warning' : 'Normal'}
        status={battery == null ? 'Sin dato' : battery < 20 ? 'Batería baja' : 'Carga suficiente'} />
    </div>
  )
}

function ChainStep({ step }) {
  const Icon = step.ok === true ? CheckCircle2 : step.ok === false ? XCircle : CircleDashed
  const tone = step.ok === true ? 'text-risk-normal' : step.ok === false ? 'text-risk-critical' : 'text-basalt-mute'
  return (
    <li className="flex items-center gap-2 text-[15px]">
      <Icon className={`h-4 w-4 shrink-0 ${tone}`} aria-hidden="true" />
      <span className="flex-1">{step.name}</span>
      {step.detail && <span className="truncate text-[13px] text-basalt-mute">{label(step.detail)}</span>}
    </li>
  )
}

export default function Cabin() {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [params, setParams] = useSearchParams()
  const [chain, setChain] = useState(null)

  const roster = useAsync(() => fleet.operators(), [])
  const operatorId = params.get('operatorId') ?? roster.data?.[0]?.operatorId
  const operator = useMemo(() => roster.data?.find((op) => op.operatorId === operatorId), [roster.data, operatorId])

  const cab = useAsync(async () => {
    if (!operatorId) return null
    const [preShift, session, history, consent] = await Promise.all([
      devices.preShift(operatorId, 20).catch(() => null),
      monitoring.current(operatorId).catch(() => null),
      fatigue.byOperator(operatorId).catch(() => []),
      privacy.currentConsent(operatorId).catch(() => null),
    ])
    return { preShift, session, history, consent }
  }, [operatorId], { interval: 10000 })

  const live = useTelemetry({ operatorId, interval: 1500 })
  const feed = useAsync(() => (operatorId ? telemetry.events(operatorId) : Promise.resolve([])), [operatorId], { interval: 5000 })
  const gateway = live.data

  // Follow the detection chain of the newest assessment the gateway sent.
  const latest = gateway?.lastAssessment
  const latestKey = latest?.id ?? latest?.assessedAt
  useEffect(() => {
    if (!latest) return undefined
    let cancelled = false
    const follow = async (attempt = 0) => {
      const result = await traceAssessment(operatorId, latest)
      if (cancelled) return
      setChain({ assessment: latest, ...result })
      if (attempt < 4 && result.steps.some((s) => s.ok === null)) setTimeout(() => follow(attempt + 1), 1500)
    }
    follow()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [operatorId, latestKey])

  if (roster.loading && !roster.data) return <Spinner />
  if (!roster.data?.length) return <Empty icon={Waves} title="No hay cabinas registradas en Fleet Monitoring." />

  const session = cab.data?.session
  const open = session && session.status !== 'Stopped'
  const preShift = cab.data?.preShift
  const blocked = cab.data?.consent?.blocksMonitoring
  const state = GATEWAY_STATE[gateway?.state ?? 'idle']
  const points = gateway?.history ?? []

  async function start() {
    await run('start', () => monitoring.start(operatorId, operator.vehicleCode, ['EyeTracking', 'HeartRate']), 'Monitoreo iniciado: el gateway empieza a transmitir')
    cab.reload()
    live.reload()
  }
  async function finish() {
    await run('finish', () => monitoring.finish(session.sessionId), 'Monitoreo finalizado')
    cab.reload()
    live.reload()
  }

  const history = [...(cab.data?.history ?? [])].slice(0, 30).reverse()

  return (
    <>
      <PageHeader icon={Waves} title="Cabina">
        <LiveBadge receivedAt={live.receivedAt} paused={live.paused} error={live.error} onToggle={() => live.setPaused((p) => !p)} />
        <select
          className="field w-auto min-w-[16rem]"
          value={operatorId ?? ''}
          onChange={(event) => {
            setChain(null)
            setParams({ operatorId: event.target.value })
          }}
          aria-label="Cabina"
        >
          {roster.data.map((op) => (
            <option key={op.operatorId} value={op.operatorId}>
              {op.vehicleCode} · {op.fullName}
            </option>
          ))}
        </select>
      </PageHeader>

      {operator && (
        <div className="mb-5 flex flex-wrap items-center gap-3 rounded-tile border border-line bg-white px-4 py-3">
          <span className="rounded-md bg-basalt px-2 py-1 font-cond text-lg font-semibold text-haul">{operator.vehicleCode}</span>
          <span className="text-lg font-semibold">{operator.fullName}</span>
          <Pill value={operator.currentRiskLevel} />
          <span className="text-sm text-basalt-mute">{operator.fleet} · {gateway?.location ?? operator.location} · {label(operator.shift)}</span>
          <Pill tone={state.tone}>Gateway: {state.text}</Pill>
          {blocked && <Pill tone="Critical">Consentimiento bloquea el monitoreo</Pill>}
        </div>
      )}

      <div className="grid gap-5 xl:grid-cols-3">
        <Stage step={1} title="Pre-turno" done={preShift?.recommendation === 'StartMonitoring'}>
          {preShift ? (
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                {preShift.recommendation === 'StartMonitoring' ? (
                  <CheckCircle2 className="h-10 w-10 text-risk-normal" />
                ) : (
                  <ShieldAlert className={`h-10 w-10 ${preShift.status === 'Warning' ? 'text-risk-warning' : 'text-risk-critical'}`} />
                )}
                <div>
                  <div className="text-lg font-semibold">{label(preShift.recommendation)}</div>
                  <Pill value={preShift.status} />
                </div>
              </div>
              <div className="flex flex-wrap gap-2">
                {preShift.evaluatedDevices.map((device) => (
                  <span key={device.id} className="flex items-center gap-2 rounded-full border border-line px-3 py-1 text-sm">
                    {device.deviceType === 'camera' ? <Camera className="h-4 w-4" /> : <HeartPulse className="h-4 w-4" />}
                    {device.deviceCode}
                    <span className="numeral">{device.batteryLevel ?? '—'}%</span>
                  </span>
                ))}
                {preShift.missingDeviceTypes.map((type) => (
                  <Pill key={type} tone="Critical">Falta {label(type).toLowerCase()}</Pill>
                ))}
              </div>
              <p className="text-[13px] text-basalt-mute">Cada dispositivo reporta su batería al gateway; no se ingresa a mano.</p>
            </div>
          ) : (
            <Empty icon={CircleSlash} title="Device Management no respondió." />
          )}
        </Stage>

        <Stage step={2} title="Monitoreo" done={open}>
          {open ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <Pill value={session.status} />
                <span className="text-sm text-basalt-mute">desde {time(session.startedAt)}</span>
              </div>
              <ul className="grid grid-cols-2 gap-3" aria-label="Señales de la cabina">
                {SIGNALS.map(({ key, icon: Icon, device }) => {
                  const on = !(session.missingSignals ?? []).includes(key)
                  return (
                    <li
                      key={key}
                      className={`flex flex-col items-center gap-1 rounded-tile border-2 py-3 text-center font-semibold ${
                        on ? 'border-risk-normal bg-risk-normalWash text-[#1E6B48]' : 'border-dashed border-risk-critical bg-risk-criticalWash text-[#B3261E]'
                      }`}
                    >
                      <Icon className="h-7 w-7" aria-hidden="true" />
                      {label(key)}
                      <span className="text-xs font-medium">{on ? 'Con señal' : 'Sin señal'}</span>
                      <span className="text-[11px] font-normal opacity-80">{device}</span>
                    </li>
                  )
                })}
              </ul>
              <p className="text-sm text-basalt-mute">{session.message}</p>
              <Button variant="outline" icon={Square} onClick={finish} busy={busy === 'finish'} className="w-full">Finalizar monitoreo</Button>
            </div>
          ) : (
            <div className="space-y-3">
              <Empty icon={Play} title={gateway?.reason ?? 'Sin sesión abierta.'} />
              <Button variant="accent" icon={Play} onClick={start} busy={busy === 'start'} className="w-full" disabled={!operator || blocked}>
                Iniciar monitoreo
              </Button>
            </div>
          )}
        </Stage>

        <Stage step={3} title="Gateway IoT de la cabina" done={gateway?.state === 'streaming'}>
          {!gateway || gateway.state === 'idle' ? (
            <Empty icon={Radio} title={live.error ? 'El gateway IoT no responde.' : 'El gateway transmite cuando hay una sesión abierta.'} />
          ) : (
            <div className="space-y-3">
              {gateway.state === 'offline' && (
                <div className="flex items-start gap-2 rounded-xl bg-risk-warningWash p-3 text-[14px] text-[#8A5200]">
                  <WifiOff className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                  <span>Sin cobertura en {gateway.location}. La cabina sigue detectando y advirtiendo; {gateway.edgeBuffered} eventos esperan en el buffer edge.</span>
                </div>
              )}
              <LiveReadings live={gateway} />
              <div className="flex items-center justify-between text-[13px] text-basalt-mute">
                <span className="flex items-center gap-1"><Timer className="h-3.5 w-3.5" aria-hidden="true" />Próxima evaluación en {gateway.nextAssessmentInSeconds ?? '—'} s</span>
                <span>1 muestra/s · MQTT</span>
              </div>
            </div>
          )}
        </Stage>
      </div>

      <Panel title="Telemetría en vivo · últimos 3 minutos" icon={Activity} className="mt-5"
        actions={<span className="hidden text-[13px] text-basalt-mute sm:inline">Medido por los sensores; no se edita desde la vista</span>}>
        {points.length > 1 ? (
          <div className="grid gap-4 lg:grid-cols-3">
            <div>
              <div className="mb-1 text-sm font-semibold">PERCLOS (cámara)</div>
              <StreamChart points={points} field="perclos" name="PERCLOS" domain={[0, 0.7]} tone={riskOf(gateway?.current)}
                format={(v) => `${Math.round(v * 100)}%`}
                thresholds={[{ value: 0.15, label: 'Advertencia', color: '#E58A00' }, { value: 0.4, label: 'Crítico', color: '#D6362F' }]} />
            </div>
            <div>
              <div className="mb-1 text-sm font-semibold">Frecuencia cardíaca (wearable)</div>
              <StreamChart points={points} field="heartRate" name="Frecuencia" unit=" lpm" domain={[40, 120]} tone="info" format={(v) => Math.round(v)} />
            </div>
            <div>
              <div className="mb-1 text-sm font-semibold">Variabilidad cardíaca (HRV)</div>
              <StreamChart points={points} field="heartRateVariabilityMs" name="HRV" unit=" ms" domain={[0, 90]} tone="Normal"
                format={(v) => Math.round(v)} thresholds={[{ value: 30, label: 'Advertencia', color: '#E58A00' }, { value: 20, label: 'Crítico', color: '#D6362F' }]} />
            </div>
          </div>
        ) : (
          <Empty icon={Activity} title="Esperando muestras del gateway de la cabina." />
        )}
      </Panel>

      <div className="mt-5 grid gap-5 lg:grid-cols-[380px_1fr]">
        <Panel title="Última evaluación y su cadena" icon={Radio}>
          {chain?.assessment ? (
            <div className="space-y-4">
              <div className="flex items-center gap-4">
                <Ring value={chain.assessment.perclos} size={96} stroke={10} tone={chain.assessment.riskLevel}>
                  <span className="numeral text-2xl">{Math.round(chain.assessment.perclos * 100)}</span>
                </Ring>
                <div className="space-y-1">
                  <Pill value={chain.assessment.riskLevel} />
                  <div className="text-sm text-basalt-mute">{time(chain.assessment.assessedAt)} · {chain.assessment.origin === 'Edge' ? 'en cabina' : 'en la nube'}</div>
                  <div className="text-[13px] text-basalt-mute">
                    Parpadeos {Math.round(chain.assessment.blinkRatePerMinute)}/min · HRV {Math.round(chain.assessment.heartRateVariabilityMs)} ms
                  </div>
                </div>
              </div>
              <ul className="space-y-1.5">{chain.steps.map((s) => <ChainStep key={s.name} step={s} />)}</ul>
              {chain.alert && <Link to={`/alertas?id=${chain.alert.id}`} className="text-sm font-semibold text-ore hover:underline">Ver la alerta</Link>}
            </div>
          ) : (
            <Empty icon={Timer} title="El gateway envía una evaluación cada 30 segundos." />
          )}
        </Panel>

        <div className="grid gap-5">
          <Panel title="Evaluaciones de la sesión" icon={Waves}>
            {history.length ? (
              <>
                <div className="flex h-36 items-end gap-1.5" role="img" aria-label={`Últimas ${history.length} evaluaciones de fatiga`}>
                  {history.map((a) => (
                    <div key={a.id} className="relative flex h-full max-w-[28px] flex-1 items-end" title={`${label(a.riskLevel)} · PERCLOS ${pct(a.perclos)} · ${time(a.assessedAt)}${a.origin === 'Edge' ? ' · edge' : ''}`}>
                      <div className={`w-full rounded-t ${BAND[a.riskLevel]} ${a.origin === 'Edge' ? 'opacity-60' : ''}`} style={{ height: `${Math.min(Math.max(a.perclos * 140, 4), 100)}%` }} />
                    </div>
                  ))}
                </div>
                <div className="mt-2 flex flex-wrap gap-3 text-[12px] text-basalt-mute">
                  {['Normal', 'Warning', 'Critical'].map((r) => <span key={r} className="flex items-center gap-1"><span className={`h-2 w-3 rounded-sm ${BAND[r]}`} />{label(r)}</span>)}
                  <span>Atenuada: sincronizada desde el edge</span>
                </div>
              </>
            ) : (
              <Empty icon={Waves} title="Todavía no hay evaluaciones." />
            )}
          </Panel>
          <Panel title="Eventos del gateway" icon={Radio} bodyClass="p-2">
            {feed.data?.length ? (
              <ul className="max-h-56 space-y-0.5 overflow-y-auto">
                {feed.data.slice(0, 20).map((event) => (
                  <li key={event.id} className="flex items-start gap-3 rounded-lg px-2 py-1.5 text-[14px]">
                    <span className="numeral w-12 shrink-0 text-basalt-mute">{time(event.at)}</span>
                    <span>{event.text}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <Empty icon={Radio} title="Sin eventos todavía." />
            )}
          </Panel>
        </div>
      </div>
    </>
  )
}
