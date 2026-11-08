import { Activity, BellRing, CloudUpload, History, Layers, RefreshCw, Repeat, Send, Trash2, Wifi, WifiOff } from 'lucide-react'
import { useState } from 'react'
import { devices, edge } from '../api/services'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, IconButton, Input, PageHeader, Panel, Pill, Select, Spinner, Stat } from '../components/ui'
import { dateTime, shortId, time } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const HISTORY_KEY = 'minesense.edge.batches'
const MAX_EVENTS = 500

// Event types are free text in the backend; these are the ones a cabin gateway emits in the demo.
const EVENT_TYPES = [
  { type: 'FatigueReading', label: 'Lectura de fatiga', icon: Activity, tone: 'bg-ore', payload: () => ({ perclos: Number((0.1 + Math.random() * 0.4).toFixed(2)), blinkRate: 10 + Math.round(Math.random() * 12) }) },
  { type: 'SignalLost', label: 'Señal perdida', icon: WifiOff, tone: 'bg-risk-critical', payload: () => ({ signal: 'EyeTracking' }) },
  { type: 'SignalRestored', label: 'Señal restaurada', icon: Wifi, tone: 'bg-risk-normal', payload: () => ({ signal: 'EyeTracking' }) },
  { type: 'AlertRaised', label: 'Alerta emitida', icon: BellRing, tone: 'bg-risk-warning', payload: () => ({ severity: Math.random() > 0.5 ? 'Critical' : 'Warning' }) },
]
const META = Object.fromEntries(EVENT_TYPES.map((item) => [item.type, item]))

function readHistory() {
  try {
    const parsed = JSON.parse(localStorage.getItem(HISTORY_KEY))
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function writeHistory(list) {
  try {
    localStorage.setItem(HISTORY_KEY, JSON.stringify(list))
  } catch {
    // Private mode: the history lives only in memory.
  }
}

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
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { data: deviceList, loading: loadingDevices } = useAsync(() => devices.list(), [])
  const [deviceId, setDeviceId] = useState('')
  const [edgeNodeId, setEdgeNodeId] = useState(() => crypto.randomUUID())
  const [batchId, setBatchId] = useState(() => crypto.randomUUID())
  const [sequence, setSequence] = useState(1)
  const [events, setEvents] = useState([])
  const [sent, setSent] = useState(null)
  const [result, setResult] = useState(null)
  const [history, setHistory] = useState(readHistory)
  const [openBatch, setOpenBatch] = useState(null)

  const list = deviceList ?? []
  const deviceCode = (id) => list.find((d) => d.id === id)?.deviceCode ?? shortId(id)

  const addEvent = (type) => {
    if (events.length >= MAX_EVENTS) return
    setEvents((current) => [
      ...current,
      { eventId: crypto.randomUUID(), occurredAt: new Date().toISOString(), eventType: type, payload: META[type].payload() },
    ])
  }

  const submit = async (body) => {
    const isNew = !history.some((item) => item.batchId === body.batchId)
    const response = await run('submit', () => edge.submit(body))
    if (response === undefined) return
    setSent(body)
    setResult({ batch: response, isNew })
    if (isNew) {
      const next = [{ batchId: body.batchId, deviceId: body.deviceId, events: body.events.length, submittedAt: new Date().toISOString() }, ...history].slice(0, 30)
      setHistory(next)
      writeHistory(next)
      toast.ok('Lote aceptado.')
    } else {
      toast.ok('Lote repetido: el servidor devolvió el original.')
    }
  }

  const sendNew = () =>
    submit({ batchId, edgeNodeId, deviceId, sequenceNumber: Number(sequence), capturedAt: new Date().toISOString(), events })

  const startNext = () => {
    setBatchId(crypto.randomUUID())
    setSequence((value) => Number(value) + 1)
    setEvents([])
    setSent(null)
    setResult(null)
  }

  const clearHistory = () => {
    setHistory([])
    writeHistory([])
  }

  const counts = EVENT_TYPES.map((item) => ({ ...item, count: events.filter((e) => e.eventType === item.type).length }))
  const alreadySent = sent?.batchId === batchId
  const canSend = deviceId && events.length > 0 && Number(sequence) >= 0 && !alreadySent

  return (
    <>
      <PageHeader icon={CloudUpload} title="Sincronización edge" />

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={history.length} caption="Lotes enviados" tone="info" icon={History} />
        <Stat value={events.length} caption="Eventos en el lote" tone="accent" icon={Layers} />
        <Stat value={sequence} caption="Secuencia" tone="neutral" icon={Repeat} />
        <Stat value={list.length} caption="Dispositivos" tone="neutral" icon={Activity} />
      </div>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1fr_360px]">
        <div className="space-y-5">
          <Panel title="Gateway de cabina" icon={Wifi}>
            <div className="grid gap-4 md:grid-cols-2">
              <Field label="Dispositivo">
                <Select value={deviceId} onChange={(event) => setDeviceId(event.target.value)} disabled={loadingDevices && !deviceList}
                  placeholder={list.length ? 'Elige un dispositivo' : 'Sin dispositivos'}
                  options={list.map((d) => ({ value: d.id, label: `${d.deviceCode} · ${d.deviceType}` }))} />
              </Field>
              <Field label="Secuencia">
                <Input type="number" min={0} value={sequence} onChange={(event) => setSequence(event.target.value)} />
              </Field>
              <div className="space-y-1 md:col-span-2">
                <span className="text-sm font-medium text-basalt-soft">Nodo edge</span>
                <div className="flex items-center gap-2">
                  <span className="flex-1 truncate rounded-lg border border-line bg-mineral px-3 py-2 text-[15px]">{edgeNodeId}</span>
                  <IconButton icon={RefreshCw} label="Generar otro nodo" onClick={() => setEdgeNodeId(crypto.randomUUID())} />
                </div>
              </div>
            </div>
          </Panel>

          <Panel title="Eventos" icon={Layers} actions={events.length > 0 && <Button size="sm" variant="ghost" icon={Trash2} onClick={() => setEvents([])} disabled={alreadySent}>Vaciar</Button>}>
            <div className="grid grid-cols-2 gap-2 md:grid-cols-4">
              {counts.map((item) => {
                const Icon = item.icon
                return (
                  <button
                    key={item.type}
                    type="button"
                    onClick={() => addEvent(item.type)}
                    disabled={alreadySent}
                    className="relative flex flex-col items-center gap-2 rounded-xl border border-line bg-white px-2 py-3 text-center transition-colors hover:border-basalt-mute disabled:opacity-50"
                  >
                    <span className={`grid h-10 w-10 place-items-center rounded-full text-white ${item.tone}`}><Icon className="h-5 w-5" /></span>
                    <span className="text-sm font-semibold">{item.label}</span>
                    {item.count > 0 && <span className="numeral absolute right-2 top-2 rounded-full bg-basalt px-1.5 text-xs text-white">{item.count}</span>}
                  </button>
                )
              })}
            </div>
            {events.length ? (
              <ul className="mt-4 space-y-1">
                {events.map((event) => (
                  <li key={event.eventId} className="flex items-center gap-3 rounded-lg px-2 py-1.5 hover:bg-mineral">
                    <EventIcon type={event.eventType} />
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-semibold">{META[event.eventType].label}</div>
                      <div className="truncate text-[13px] text-basalt-mute">{preview(event.payload)}</div>
                    </div>
                    <span className="numeral text-sm text-basalt-mute">{time(event.occurredAt)}</span>
                    <IconButton icon={Trash2} label="Quitar evento" disabled={alreadySent}
                      onClick={() => setEvents((current) => current.filter((e) => e.eventId !== event.eventId))} />
                  </li>
                ))}
              </ul>
            ) : (
              <Empty icon={Layers} title="Agrega eventos con los botones." />
            )}
          </Panel>

          <div className="flex flex-wrap items-center gap-2">
            <Button size="lg" variant="accent" icon={Send} busy={busy === 'submit' && !alreadySent} disabled={!canSend} onClick={sendNew}>Enviar lote</Button>
            {alreadySent && (
              <>
                <Button size="lg" variant="outline" icon={Repeat} busy={busy === 'submit'} onClick={() => submit(sent)}>Reenviar mismo lote</Button>
                <Button size="lg" variant="ghost" onClick={startNext}>Nuevo lote</Button>
              </>
            )}
          </div>

          {result && (
            <div className={`flex flex-wrap items-center gap-4 rounded-tile border p-4 ${result.isNew ? 'border-risk-normal bg-risk-normalWash' : 'border-ore bg-ore-wash'}`}>
              <span className={`numeral grid h-14 w-14 place-items-center rounded-full text-lg text-white ${result.isNew ? 'bg-risk-normal' : 'bg-ore'}`}>
                {result.isNew ? 202 : 200}
              </span>
              <div className="min-w-0 flex-1">
                <div className="font-cond text-2xl font-semibold">{result.isNew ? 'Lote nuevo' : 'Lote repetido'}</div>
                <div className="text-sm text-basalt-soft">
                  {result.batch.events?.length ?? 0} eventos · recibido {dateTime(result.batch.receivedAt)}
                </div>
              </div>
              <Pill value={result.batch.status} />
            </div>
          )}
        </div>

        <Panel title="Historial" icon={History} bodyClass="p-2"
          actions={history.length > 0 && <Button size="sm" variant="ghost" onClick={clearHistory}>Limpiar</Button>}>
          {history.length ? (
            <ul className="space-y-1">
              {history.map((item) => (
                <li key={item.batchId}>
                  <button type="button" onClick={() => setOpenBatch(item.batchId)} className="flex w-full items-center gap-3 rounded-lg px-2 py-2 text-left hover:bg-mineral">
                    <span className="h-9 w-1.5 rounded-full bg-ore" />
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-[15px] font-semibold">Lote {shortId(item.batchId)}</div>
                      <div className="text-[13px] text-basalt-mute">{deviceCode(item.deviceId)} · {dateTime(item.submittedAt)}</div>
                    </div>
                    <span className="numeral rounded-full bg-basalt/5 px-2 text-sm">{item.events}</span>
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <Empty icon={History} title="Aún no envías lotes." />
          )}
        </Panel>
      </div>

      {openBatch && <BatchDrawer batchId={openBatch} deviceCode={deviceCode} onClose={() => setOpenBatch(null)} />}
    </>
  )
}
