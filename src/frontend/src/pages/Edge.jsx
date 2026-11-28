import { Activity, BellRing, CloudUpload, History, Layers, Repeat, ShieldCheck, Wifi, WifiOff } from 'lucide-react'
import { useState } from 'react'
import { devices, edge, telemetry } from '../api/services'
import { LiveBadge } from '../components/live'
import { Drawer, Empty, PageHeader, Panel, Pill, Spinner, Stat } from '../components/ui'
import { useTelemetry } from '../lib/telemetry'
import { dateTime, shortId, time } from '../lib/format'
import { useAsync } from '../lib/hooks'

// Event types are free text in the backend; these are the ones the cabin gateways emit.
const EVENT_TYPES = [
  { type: 'FatigueReadingClassified', label: 'Lectura clasificada en cabina', icon: Activity, tone: 'bg-ore' },
  { type: 'CabinWarningIssued', label: 'Advertencia dada en cabina', icon: BellRing, tone: 'bg-risk-warning' },
  { type: 'FatigueReading', label: 'Lectura de fatiga', icon: Activity, tone: 'bg-ore' },
  { type: 'SignalLost', label: 'Señal perdida', icon: WifiOff, tone: 'bg-risk-critical' },
  { type: 'SignalRestored', label: 'Señal restaurada', icon: Wifi, tone: 'bg-risk-normal' },
  { type: 'AlertRaised', label: 'Alerta emitida', icon: BellRing, tone: 'bg-risk-warning' },
]
const META = Object.fromEntries(EVENT_TYPES.map((item) => [item.type, item]))

const preview = (payload) => JSON.stringify(payload)

function EventIcon({ type }) {
  const meta = META[type]
  const Icon = meta?.icon ?? Layers
  return (
    <span className={`grid h-8 w-8 shrink-0 place-items-center rounded-full text-white ${meta?.tone ?? 'bg-basalt-mute'}`}>
      <Icon className="h-4 w-4" />
    </span>
  )
}

function BatchDrawer({ batchId, deviceCode, onClose }) {
  const { data, loading, error } = useAsync(() => edge.get(batchId), [batchId])
  const events = [...(data?.events ?? [])].sort((a, b) => new Date(a.occurredAt) - new Date(b.occurredAt))
  return (
    <Drawer open title={`Lote ${shortId(batchId)}`} onClose={onClose}>
      {loading && !data ? (
        <Spinner />
      ) : error ? (
        <Empty icon={Layers} title={error.message} />
      ) : (
        data && (
          <>
            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-xl bg-mineral p-3">
                <div className="text-[13px] text-basalt-mute">Estado</div>
                <Pill value={data.status} className="mt-1" />
              </div>
              <div className="rounded-xl bg-mineral p-3">
                <div className="text-[13px] text-basalt-mute">Secuencia</div>
                <div className="numeral text-2xl leading-tight">{data.sequenceNumber}</div>
              </div>
              <div className="rounded-xl bg-mineral p-3">
                <div className="text-[13px] text-basalt-mute">Capturado</div>
                <div className="text-sm font-semibold">{dateTime(data.capturedAt)}</div>
              </div>
              <div className="rounded-xl bg-mineral p-3">
                <div className="text-[13px] text-basalt-mute">Recibido</div>
                <div className="text-sm font-semibold">{dateTime(data.receivedAt)}</div>
              </div>
            </div>
            <div className="text-sm text-basalt-mute">
              {deviceCode(data.deviceId)} · nodo {shortId(data.edgeNodeId)}
            </div>
            <ol className="relative space-y-4 border-l-2 border-line pl-6">
              {events.map((event) => (
                <li key={event.eventId} className="relative">
                  <span className="absolute -left-[41px] top-0"><EventIcon type={event.eventType} /></span>
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-semibold">{META[event.eventType]?.label ?? event.eventType}</span>
                    <span className="numeral text-sm text-basalt-mute">{time(event.occurredAt)}</span>
                  </div>
                  <div className="mt-1 break-all rounded-lg bg-mineral px-2 py-1 text-[13px] text-basalt-soft">{preview(event.payload)}</div>
                </li>
              ))}
            </ol>
          </>
        )
      )}
    </Drawer>
  )
}

export default function Edge() {
  const live = useTelemetry({ interval: 3000 })
  const batches = useAsync(() => telemetry.edgeBatches(), [], { interval: 5000 })
  const { data: deviceList } = useAsync(() => devices.list(), [])
  const [openBatch, setOpenBatch] = useState(null)

  const list = deviceList ?? []
  const deviceCode = (id) => list.find((d) => d.id === id)?.deviceCode ?? shortId(id)
  const received = batches.data ?? []
  const offline = (live.data?.cabins ?? []).filter((c) => c.state === 'offline')
  const events = received.reduce((sum, b) => sum + b.events, 0)
  const warnings = received.reduce((sum, b) => sum + b.warnings, 0)

  if (batches.loading && !batches.data) return <Spinner />

  return (
    <>
      <PageHeader icon={CloudUpload} title="Sincronización edge">
        <LiveBadge receivedAt={live.receivedAt} paused={live.paused} error={live.error} onToggle={() => live.setPaused((p) => !p)} />
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={received.length} caption="Lotes sincronizados" tone="info" icon={History} />
        <Stat value={events} caption="Eventos recuperados" tone="accent" icon={Layers} />
        <Stat value={warnings} caption="Advertencias dadas sin red" tone={warnings ? 'Warning' : 'neutral'} icon={BellRing} />
        <Stat value={offline.length} caption="Cabinas sin cobertura ahora" tone={offline.length ? 'Warning' : 'Normal'} icon={WifiOff} />
      </div>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1fr_380px]">
        <Panel title="Lotes recibidos de los gateways" icon={CloudUpload} bodyClass="p-2">
          {received.length ? (
            <ul className="space-y-1">
              {received.map((item) => (
                <li key={item.batchId}>
                  <button type="button" onClick={() => setOpenBatch(item)} className="flex w-full flex-wrap items-center gap-3 rounded-lg px-2 py-2 text-left hover:bg-mineral">
                    <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{item.vehicleCode}</span>
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-[15px] font-semibold">Lote {shortId(item.batchId)} · secuencia {item.sequenceNumber}</div>
                      <div className="text-[13px] text-basalt-mute">{item.events} eventos · {item.warnings} advertencias · {dateTime(item.sentAt)}</div>
                    </div>
                    <span className="flex items-center gap-1.5 text-[13px]">
                      <Pill tone="Normal" dot={false}>{item.firstStatus} nuevo</Pill>
                      <Pill tone="info" dot={false}><Repeat className="h-3 w-3" aria-hidden="true" />{item.retryStatus} reintento</Pill>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <Empty icon={CloudUpload} title="Todavía ninguna cabina perdió cobertura. Cuando una vuelve a tener red, su lote aparece aquí." />
          )}
        </Panel>

        <div className="space-y-5">
          <Panel title="Cabinas sin cobertura" icon={WifiOff}>
            {offline.length ? (
              <ul className="space-y-2">
                {offline.map((cabin) => (
                  <li key={cabin.operatorId} className="flex items-center gap-3 rounded-xl bg-risk-warningWash p-3">
                    <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{cabin.vehicleCode}</span>
                    <div className="min-w-0 flex-1 text-[14px]">
                      <div className="font-semibold">{cabin.location}</div>
                      <div className="text-[#8A5200]">Detecta y advierte sin red · {cabin.edgeBuffered} eventos en el buffer</div>
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <div className="flex items-center gap-3 text-[15px] text-basalt-soft"><Wifi className="h-5 w-5 text-risk-normal" aria-hidden="true" />Todas las cabinas tienen cobertura.</div>
            )}
          </Panel>
          <Panel title="Cómo protege el edge" icon={ShieldCheck}>
            <ol className="list-decimal space-y-2 pl-5 text-[14px] text-basalt-soft">
              <li>En el tajo o el botadero la cabina pierde la red; el gateway sigue clasificando cada ventana con los umbrales de Fatigue Detection y advierte en la cabina (US13).</li>
              <li>Cada lectura y advertencia queda en el buffer en orden.</li>
              <li>Al volver la red, el gateway sella un lote (BatchId fijo) y lo envía a Edge Synchronization (US14).</li>
              <li>El enlace parpadea y el mismo lote llega dos veces: el servicio responde 200 y no duplica nada.</li>
              <li>Kafka publica el contenido: el historial de fatiga y las alertas dadas en cabina llegan al centro de control con su hora original.</li>
            </ol>
          </Panel>
        </div>
      </div>

      {openBatch && <BatchDrawer batchId={openBatch.batchId} deviceCode={deviceCode} onClose={() => setOpenBatch(null)} />}
    </>
  )
}
