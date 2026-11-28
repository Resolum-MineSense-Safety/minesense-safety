import {
  Activity, ArrowRight, BellRing, Camera, CloudUpload, Cpu, HeartPulse, Layers, Radio, RadioTower, Send, Server, Watch, WifiOff,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { telemetry } from '../api/services'
import { LiveBadge, Sparkline } from '../components/live'
import { GATEWAY_STATE, riskOf, useTelemetry } from '../lib/telemetry'
import { Empty, PageHeader, Panel, Pill, Segmented, Spinner, Stat } from '../components/ui'
import { label, time } from '../lib/format'
import { useAsync } from '../lib/hooks'

const PROFILE = { descansado: 'Descansado', cansado: 'Cansado', somnoliento: 'Somnoliento' }

// Edge -> cloud path of one sample (ADR-004): what the presenter walks through on the demo.
function Pipeline({ gateway }) {
  const steps = [
    { icon: Camera, title: 'Sensores', text: 'Cámara IR y wearable', value: '1 Hz' },
    { icon: Cpu, title: 'Gateway de cabina', text: 'Clasifica y advierte en cabina', value: `${gateway?.cabins.streaming ?? 0} activos` },
    { icon: RadioTower, title: 'Broker MQTT', text: gateway?.mqtt.broker ?? 'mosquitto', value: gateway?.mqtt.connected ? 'conectado' : 'sin broker' },
    { icon: Server, title: 'Fatigue Detection', text: `ventana de ${gateway?.assessEverySeconds ?? 30} s`, value: `${gateway?.stats.assessments ?? 0} evaluaciones` },
    { icon: Layers, title: 'Kafka', text: 'FatigueAssessed → AlertIssued', value: 'eventos' },
    { icon: BellRing, title: 'Centro de control', text: 'Flota, alertas, incidentes', value: `${gateway?.stats.alerts ?? 0} alertas` },
  ]
  return (
    <ol className="grid gap-2 sm:grid-cols-3 xl:grid-cols-6" aria-label="Recorrido de una muestra">
      {steps.map((step, index) => (
        <li key={step.title} className="relative flex items-start gap-3 rounded-xl border border-line bg-white p-3">
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-basalt text-haul"><step.icon className="h-4 w-4" aria-hidden="true" /></span>
          <div className="min-w-0">
            <div className="text-sm font-semibold">{step.title}</div>
            <div className="truncate text-[12px] text-basalt-mute">{step.text}</div>
            <div className="numeral mt-0.5 text-sm text-ore">{step.value}</div>
          </div>
          {index < steps.length - 1 && <ArrowRight className="absolute -right-2.5 top-1/2 hidden h-4 w-4 -translate-y-1/2 text-basalt-mute xl:block" aria-hidden="true" />}
        </li>
      ))}
    </ol>
  )
}

function Value({ name, value, unit }) {
  return (
    <div>
      <div className="text-[12px] text-basalt-mute">{name}</div>
      <div className="numeral text-lg leading-tight">{value ?? '—'}<span className="ml-0.5 text-[12px] text-basalt-mute">{value != null ? unit : ''}</span></div>
    </div>
  )
}

function CabinCard({ cabin, onOpen }) {
  const now = cabin.current
  const state = GATEWAY_STATE[cabin.state]
  const risk = riskOf(now)
  const rule = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal', neutral: 'bg-line' }[cabin.state === 'idle' ? 'neutral' : risk]
  return (
    <button type="button" onClick={onOpen} className="flex w-full overflow-hidden rounded-tile border border-line bg-white text-left hover:border-basalt-mute">
      <span className={`w-2 shrink-0 ${rule}`} />
      <div className="min-w-0 flex-1 space-y-3 p-3">
        <div className="flex items-center gap-2">
          <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{cabin.vehicleCode}</span>
          <span className="min-w-0 flex-1 truncate text-[15px] font-semibold">{cabin.fullName}</span>
          <Pill tone={state.tone}>{state.text}</Pill>
        </div>
        {cabin.state === 'idle' ? (
          <p className="text-[13px] text-basalt-mute">{cabin.reason}</p>
        ) : (
          <>
            <div className="grid grid-cols-4 gap-2">
              <Value name="PERCLOS" value={now?.perclos != null ? Math.round(now.perclos * 100) : null} unit="%" />
              <Value name="Pulso" value={now?.heartRate} unit="lpm" />
              <Value name="HRV" value={now?.heartRateVariabilityMs != null ? Math.round(now.heartRateVariabilityMs) : null} unit="ms" />
              <Value name="Batería" value={now?.wearableBattery != null ? Math.round(now.wearableBattery) : null} unit="%" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <div className="text-[11px] text-basalt-mute">PERCLOS · 60 s</div>
                <Sparkline points={cabin.history} field="perclos" min={0} max={0.6} threshold={0.15} tone={risk} />
              </div>
              <div>
                <div className="text-[11px] text-basalt-mute">Pulso · 60 s</div>
                <Sparkline points={cabin.history} field="heartRate" min={45} max={115} tone="info" />
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[12px] text-basalt-mute">
              <span className={`flex items-center gap-1 ${cabin.signals.EyeTracking ? '' : 'font-semibold text-[#B3261E]'}`}><Camera className="h-3.5 w-3.5" aria-hidden="true" />{cabin.signals.EyeTracking ? 'Cámara' : 'Cámara sin señal'}</span>
              <span className={`flex items-center gap-1 ${cabin.signals.HeartRate ? '' : 'font-semibold text-[#B3261E]'}`}><Watch className="h-3.5 w-3.5" aria-hidden="true" />{cabin.signals.HeartRate ? 'Wearable' : 'Wearable sin señal'}</span>
              <span>{cabin.location}</span>
              <span>{PROFILE[cabin.profile]}</span>
              {cabin.state === 'offline' && <span className="flex items-center gap-1 font-semibold text-[#8A5200]"><WifiOff className="h-3.5 w-3.5" aria-hidden="true" />{cabin.edgeBuffered} en buffer</span>}
              {cabin.lastAssessment && <span>Última: {label(cabin.lastAssessment.riskLevel)} {time(cabin.lastAssessment.assessedAt)}</span>}
            </div>
          </>
        )}
      </div>
    </button>
  )
}

export default function Telemetry() {
  const navigate = useNavigate()
  const live = useTelemetry({ interval: 2000 })
  const feed = useAsync(() => telemetry.events(), [], { interval: 3000 })
  const [filter, setFilter] = useState('all')
  const gateway = live.data?.gateway
  const cabins = useMemo(() => (live.data?.cabins ?? []).filter((c) => filter === 'all' || c.state === filter), [live.data, filter])
  const sample = (live.data?.cabins ?? []).find((c) => c.state === 'streaming')

  if (!live.data && !live.error) return <Spinner />
  if (!live.data) return <Empty icon={Radio} title="El gateway IoT no responde. Revisa el contenedor iot-simulator." />

  const all = live.data.cabins
  const count = (state) => all.filter((c) => c.state === state).length

  return (
    <>
      <PageHeader icon={Radio} title="Telemetría IoT">
        <LiveBadge receivedAt={live.receivedAt} paused={live.paused} error={live.error} onToggle={() => live.setPaused((p) => !p)} />
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-6">
        <Stat value={count('streaming')} caption="Cabinas transmitiendo" tone="Normal" icon={Radio} onClick={() => setFilter('streaming')} />
        <Stat value={count('offline')} caption="Sin cobertura" tone={count('offline') ? 'Warning' : 'neutral'} icon={WifiOff} onClick={() => setFilter('offline')} />
        <Stat value={count('idle')} caption="Sin sesión" tone="neutral" icon={Cpu} onClick={() => setFilter('idle')} />
        <Stat value={gateway?.mqtt.published?.toLocaleString('es-PE')} caption="Mensajes MQTT" tone="info" icon={RadioTower} />
        <Stat value={gateway?.stats.assessments} caption="Evaluaciones enviadas" tone="accent" icon={Send} />
        <Stat value={gateway?.stats.batches} caption="Lotes edge sincronizados" tone="info" icon={CloudUpload} onClick={() => navigate('/edge')} />
      </div>

      <section className="mt-5">
        <h2 className="mb-2 font-cond text-xl font-semibold">Recorrido de una muestra</h2>
        <Pipeline gateway={gateway} />
      </section>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1fr_360px]">
        <section className="rounded-tile border border-line bg-white p-4">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <h2 className="font-cond text-2xl font-semibold">Cabinas</h2>
            <Segmented value={filter} onChange={setFilter} options={[
              { value: 'all', label: 'Todas', count: all.length },
              { value: 'streaming', label: 'Transmitiendo', count: count('streaming') },
              { value: 'offline', label: 'Sin cobertura', count: count('offline') },
              { value: 'idle', label: 'Sin sesión', count: count('idle') },
            ]} />
          </div>
          {cabins.length ? (
            <div className="grid gap-3 md:grid-cols-2">
              {cabins.map((cabin) => <CabinCard key={cabin.operatorId} cabin={cabin} onOpen={() => navigate(`/cabina?operatorId=${cabin.operatorId}`)} />)}
            </div>
          ) : (
            <Empty icon={Radio} title="Ninguna cabina en este estado." />
          )}
        </section>

        <div className="space-y-5">
          <Panel title="Eventos de los gateways" icon={Activity} bodyClass="p-2">
            {feed.data?.length ? (
              <ul className="max-h-[420px] space-y-0.5 overflow-y-auto">
                {feed.data.slice(0, 40).map((event) => (
                  <li key={event.id} className="flex gap-2 rounded-lg px-2 py-1.5 text-[13px] leading-snug">
                    <span className="numeral shrink-0 text-basalt-mute">{time(event.at)}</span>
                    <span className={event.severity === 'Critical' ? 'font-semibold text-[#B3261E]' : event.severity === 'Warning' ? 'text-[#8A5200]' : 'text-basalt-soft'}>{event.text}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <Empty icon={Activity} title="Sin eventos todavía." />
            )}
          </Panel>
          <Panel title="Mensaje MQTT de ejemplo" icon={HeartPulse}>
            <div className="mb-2 break-all text-[12px] text-basalt-mute">{gateway?.mqtt.topic}</div>
            <pre className="max-h-64 overflow-auto rounded-lg bg-basalt p-3 text-[12px] leading-relaxed text-white/90">
              {sample ? JSON.stringify({ operatorId: sample.operatorId, ...sample.current }, null, 2) : 'Sin cabinas transmitiendo.'}
            </pre>
            <p className="mt-2 text-[12px] text-basalt-mute">Activo hace {Math.round((gateway?.uptimeSeconds ?? 0) / 60)} min · evaluación cada {gateway?.assessEverySeconds} s · {gateway?.stats.signalChanges} cambios de señal · {gateway?.stats.deviceReports} reportes de batería</p>
          </Panel>
        </div>
      </div>
    </>
  )
}
