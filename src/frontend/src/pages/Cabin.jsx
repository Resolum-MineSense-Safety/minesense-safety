import { Camera, CheckCircle2, CircleSlash, HeartPulse, Play, Send, ShieldAlert, Square, Waves, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { devices, fatigue, fleet, monitoring, privacy } from '../api/services'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toasts'
import { Button, Empty, PageHeader, Panel, Pill, Ring, Spinner } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { label, time } from '../lib/format'
import { PRESETS, processReading } from '../lib/flow'
import { useAction, useAsync } from '../lib/hooks'

const SIGNALS = [
  { key: 'EyeTracking', icon: Camera },
  { key: 'HeartRate', icon: HeartPulse },
]
const BAND = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal' }

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

function Slider({ name, value, min, max, step, onChange, unit }) {
  return (
    <label className="block">
      <div className="flex items-baseline justify-between">
        <span className="text-sm font-medium text-basalt-soft">{name}</span>
        <span className="numeral text-xl">{value}{unit}</span>
      </div>
      <input type="range" className="w-full accent-[#1F2A2E]" min={min} max={max} step={step} value={value} onChange={(e) => onChange(Number(e.target.value))} />
    </label>
  )
}

export default function Cabin() {
  const toast = useToast()
  const { operationId } = useAuth()
  const { unitName } = useDirectory()
  const { busy, run } = useAction(toast)
  const [params, setParams] = useSearchParams()
  const [reading, setReading] = useState(PRESETS.Normal)
  const [outcome, setOutcome] = useState(null)

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
  }, [operatorId])

  if (roster.loading && !roster.data) return <Spinner />
  if (!roster.data?.length)
    return <Empty icon={Waves} title="No hay cabinas registradas en Fleet Monitoring." />

  const session = cab.data?.session
  const live = session && session.status !== 'Stopped'
  const preShift = cab.data?.preShift
  const blocked = cab.data?.consent?.blocksMonitoring

  async function start() {
    await run('start', () => monitoring.start(operatorId, operator.vehicleCode, ['EyeTracking', 'HeartRate']), 'Monitoreo iniciado')
    cab.reload()
  }
  async function toggleSignal(signal) {
    const missing = session.missingSignals.includes(signal)
    await run(signal, () => (missing ? monitoring.restoreSignal(session.sessionId, signal) : monitoring.loseSignal(session.sessionId, signal)),
      missing ? `${label(signal)} restaurada` : `${label(signal)} perdida`)
    cab.reload()
  }
  async function finish() {
    await run('finish', () => monitoring.finish(session.sessionId), 'Monitoreo finalizado')
    cab.reload()
  }
  async function send() {
    const result = await run('send', () =>
      processReading({
        operator,
        session: { id: session.sessionId },
        reading,
        operationId,
        context: { shift: operator.shift, mineSite: unitName(operator.miningUnitId) },
      }))
    if (result) {
      setOutcome(result)
      cab.reload()
      roster.reload()
    }
  }

  const history = [...(cab.data?.history ?? [])].slice(0, 24).reverse()

  return (
    <>
      <PageHeader icon={Waves} title="Cabina">
        <select
          className="field w-auto min-w-[16rem]"
          value={operatorId ?? ''}
          onChange={(event) => {
            setOutcome(null)
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
          <span className="text-sm text-basalt-mute">{operator.fleet} · {operator.location} · {label(operator.shift)}</span>
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
            </div>
          ) : (
            <Empty icon={CircleSlash} title="Device Management no respondió." />
          )}
        </Stage>

        <Stage step={2} title="Monitoreo" done={live}>
          {live ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <Pill value={session.status} />
                <span className="text-sm text-basalt-mute">desde {time(session.startedAt)}</span>
              </div>
              <div className="grid grid-cols-2 gap-3">
                {SIGNALS.map(({ key, icon: Icon }) => {
                  const on = session.availableSignals.includes(key)
                  return (
                    <button
                      key={key}
                      type="button"
                      onClick={() => toggleSignal(key)}
                      disabled={busy === key}
                      className={`flex flex-col items-center gap-2 rounded-tile border-2 py-4 font-semibold transition-colors ${
                        on ? 'border-risk-normal bg-risk-normalWash text-risk-normal' : 'border-dashed border-risk-critical bg-risk-criticalWash text-risk-critical'
                      }`}
                    >
                      <Icon className="h-7 w-7" />
                      {label(key)}
                      <span className="text-xs font-medium">{on ? 'Con señal' : 'Sin señal'}</span>
                    </button>
                  )
                })}
              </div>
              <p className="text-sm text-basalt-mute">{session.message}</p>
              <Button variant="outline" icon={Square} onClick={finish} busy={busy === 'finish'} className="w-full">Finalizar monitoreo</Button>
            </div>
          ) : (
            <div className="space-y-3">
              <Empty icon={Play} title="Sin sesión abierta." />
              <Button variant="accent" icon={Play} onClick={start} busy={busy === 'start'} className="w-full" disabled={!operator}>
                Iniciar monitoreo
              </Button>
            </div>
          )}
        </Stage>

        <Stage step={3} title="Lectura de fatiga" done={!!outcome}>
          <div className="space-y-4">
            <div className="grid grid-cols-3 gap-2">
              {Object.keys(PRESETS).map((risk) => (
                <button
                  key={risk}
                  type="button"
                  onClick={() => setReading(PRESETS[risk])}
                  className="flex flex-col items-center gap-1.5 rounded-xl border border-line py-2 text-sm font-semibold hover:border-basalt-mute"
                >
                  <span className={`h-2 w-8 rounded-full ${BAND[risk]}`} />
                  {label(risk)}
                </button>
              ))}
            </div>
            <Slider name="PERCLOS" value={reading.perclos} min={0} max={1} step={0.01} onChange={(v) => setReading({ ...reading, perclos: v })} />
            <Slider name="Parpadeos" unit="/min" value={reading.blinkRatePerMinute} min={0} max={60} step={1} onChange={(v) => setReading({ ...reading, blinkRatePerMinute: v })} />
            <Slider name="HRV" unit=" ms" value={reading.heartRateVariabilityMs} min={5} max={120} step={1} onChange={(v) => setReading({ ...reading, heartRateVariabilityMs: v })} />
            <Button icon={Send} onClick={send} busy={busy === 'send'} className="w-full" disabled={!live}>
              Enviar lectura
            </Button>
          </div>
        </Stage>
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-[360px_1fr]">
        <Panel title="Resultado">
          {outcome ? (
            <div className="space-y-4">
              <div className="flex items-center gap-4">
                <Ring value={outcome.assessment.perclos} size={96} stroke={10} tone={outcome.assessment.riskLevel}>
                  <span className="numeral text-2xl">{Math.round(outcome.assessment.perclos * 100)}</span>
                </Ring>
                <div className="space-y-1">
                  <Pill value={outcome.assessment.riskLevel} />
                  <div className="text-sm text-basalt-mute">{outcome.assessment.requiresAlert ? 'Requiere alerta' : 'Sin alerta'}</div>
                </div>
              </div>
              <ul className="space-y-1.5">
                {outcome.steps.map((s) => (
                  <li key={s.name} className="flex items-center gap-2 text-[15px]">
                    {s.ok ? <CheckCircle2 className="h-4 w-4 text-risk-normal" /> : <XCircle className="h-4 w-4 text-risk-critical" />}
                    {s.name}
                    {!s.ok && <span className="truncate text-xs text-risk-critical">{s.error}</span>}
                  </li>
                ))}
              </ul>
            </div>
          ) : (
            <Empty icon={Send} title="Envía una lectura para ver la cadena de detección." />
          )}
        </Panel>

        <Panel title="Últimas evaluaciones">
          {history.length ? (
            <div className="flex h-40 items-end gap-1.5">
              {history.map((a) => (
                <div key={a.id} className="group relative flex h-full max-w-[36px] flex-1 items-end" title={`${label(a.riskLevel)} · PERCLOS ${a.perclos} · ${time(a.assessedAt)}`}>
                  <div className={`w-full rounded-t ${BAND[a.riskLevel]}`} style={{ height: `${Math.max(a.perclos * 100, 4)}%` }} />
                </div>
              ))}
            </div>
          ) : (
            <Empty icon={Waves} title="Todavía no hay evaluaciones." />
          )}
        </Panel>
      </div>
    </>
  )
}
