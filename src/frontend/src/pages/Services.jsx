import {
  Activity,
  BellRing,
  Building2,
  ChartColumn,
  CloudCog,
  FileText,
  KeyRound,
  RefreshCw,
  ScanEye,
  Server,
  ShieldCheck,
  Siren,
  Smartphone,
  ThumbsUp,
  Timer,
  Truck,
  Waves,
} from 'lucide-react'
import { useState } from 'react'
import { PROBES } from '../api/services'
import { Button, PageHeader, Spinner, Stat } from '../components/ui'
import { time } from '../lib/format'
import { useAsync } from '../lib/hooks'

const ICONS = {
  identity: KeyRound,
  operations: Building2,
  devices: Smartphone,
  monitoring: Waves,
  fatigue: ScanEye,
  alerts: BellRing,
  fleet: Truck,
  incidents: Siren,
  privacy: ShieldCheck,
  reporting: FileText,
  analytics: ChartColumn,
  feedback: ThumbsUp,
  edge: CloudCog,
}

async function probe(entry) {
  const started = performance.now()
  try {
    await entry.run()
    return { key: entry.key, ok: true, ms: Math.round(performance.now() - started), at: new Date().toISOString() }
  } catch (error) {
    return {
      key: entry.key,
      ok: false,
      code: error.status ?? null,
      message: error.message,
      ms: Math.round(performance.now() - started),
      at: new Date().toISOString(),
    }
  }
}

async function probeAll() {
  const results = await Promise.all(PROBES.map(probe))
  return Object.fromEntries(results.map((result) => [result.key, result]))
}

function latencyTone(ms) {
  if (ms < 300) return 'bg-risk-normal'
  if (ms < 1000) return 'bg-risk-warning'
  return 'bg-risk-critical'
}

function ServiceTile({ entry, result, checking, onCheck }) {
  const Icon = ICONS[entry.key] ?? Server
  const pending = !result || checking
  const ok = result?.ok
  return (
    <div
      className={`relative flex flex-col overflow-hidden rounded-tile border bg-white ${
        pending ? 'border-line' : ok ? 'border-line' : 'border-risk-critical/40 bg-risk-criticalWash'
      }`}
    >
      <span className={`absolute inset-x-0 top-0 h-1.5 ${pending ? 'bg-line' : ok ? 'bg-risk-normal' : 'bg-risk-critical'}`} />
      <div className="flex items-start gap-3 p-4 pt-5">
        <span className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-mineral">
          <Icon className="h-5 w-5 text-basalt-soft" />
        </span>
        <div className="min-w-0 flex-1">
          <div className="truncate text-[15px] font-semibold">{entry.name}</div>
          <div className="text-[13px] text-basalt-mute">{result ? time(result.at) : '—'}</div>
        </div>
        <span
          role="img"
          aria-label={pending ? 'Comprobando' : ok ? 'Responde' : `Error ${result.code ?? ''}`}
          className={`mt-1 h-5 w-5 shrink-0 rounded-full ring-4 ${
            pending ? 'animate-beacon bg-line ring-line/40' : ok ? 'bg-risk-normal ring-risk-normalWash' : 'bg-risk-critical ring-risk-critical/20'
          }`}
        />
      </div>
      <div className="flex items-end justify-between gap-3 px-4 pb-4">
        <div>
          {ok || pending ? (
            <div className="flex items-baseline gap-1">
              <span className="numeral text-[34px] leading-none">{result && !checking ? result.ms : '—'}</span>
              <span className="text-sm text-basalt-mute">ms</span>
            </div>
          ) : (
            <div className="flex items-baseline gap-2" title={result.message}>
              <span className="numeral text-[34px] leading-none text-risk-critical">{result.code ?? 'Sin red'}</span>
              <span className="text-sm text-basalt-mute">{result.ms} ms</span>
            </div>
          )}
          {result && !checking && (
            <div className="mt-2 h-1.5 w-24 overflow-hidden rounded-full bg-line">
              <div className={`h-full ${ok ? latencyTone(result.ms) : 'bg-risk-critical'}`} style={{ width: `${Math.min(100, Math.max(6, result.ms / 15))}%` }} />
            </div>
          )}
        </div>
        <Button variant="outline" size="sm" icon={RefreshCw} busy={checking} onClick={onCheck} aria-label={`Comprobar ${entry.name}`}>
          Comprobar
        </Button>
      </div>
    </div>
  )
}

export default function Services() {
  const { data, loading, reload, setData } = useAsync(probeAll, [], { interval: 15000 })
  const [checking, setChecking] = useState(null)

  const checkOne = async (entry) => {
    setChecking(entry.key)
    const result = await probe(entry)
    setData({ ...(data ?? {}), [entry.key]: result })
    setChecking(null)
  }

  const results = Object.values(data ?? {})
  const up = results.filter((result) => result.ok).length
  const okResults = results.filter((result) => result.ok)
  const average = okResults.length ? Math.round(okResults.reduce((sum, result) => sum + result.ms, 0) / okResults.length) : null
  const slowest = results.length ? Math.max(...results.map((result) => result.ms)) : null

  return (
    <>
      <PageHeader icon={Activity} title="Estado de servicios">
        <Button variant="accent" icon={RefreshCw} busy={loading} onClick={reload}>Comprobar todo</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={data ? `${up}/${PROBES.length}` : null} caption="Servicios en línea" tone={!data ? 'neutral' : up === PROBES.length ? 'Normal' : 'Critical'} icon={Server} />
        <Stat value={data ? PROBES.length - up : null} caption="Con error" tone={data && up < PROBES.length ? 'Critical' : 'Normal'} icon={Siren} />
        <Stat value={average != null ? `${average} ms` : null} caption="Latencia media" tone="info" icon={Timer} />
        <Stat value={slowest != null ? `${slowest} ms` : null} caption="Más lento" tone="Warning" icon={Timer} />
      </div>

      {!data && loading ? (
        <Spinner />
      ) : (
        <div className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4">
          {PROBES.map((entry) => (
            <ServiceTile
              key={entry.key}
              entry={entry}
              result={data?.[entry.key]}
              checking={checking === entry.key || (loading && !data?.[entry.key])}
              onCheck={() => checkOne(entry)}
            />
          ))}
        </div>
      )}
      <p className="mt-4 text-[13px] text-basalt-mute">Cada servicio se consulta a través del gateway y se repite cada 15 s.</p>
    </>
  )
}
