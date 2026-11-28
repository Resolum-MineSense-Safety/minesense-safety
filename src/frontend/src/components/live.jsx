// Live telemetry of the cabin gateways (fleet IoT simulator, /api/v1/telemetry). Everything here is
// read-only: the values come from the sensors of each cabin, never from a form of the dashboard.
import { Pause, Play, WifiOff } from 'lucide-react'
import { STROKE } from '../lib/telemetry'
import { useEffect, useState } from 'react'

const cx = (...classes) => classes.filter(Boolean).join(' ')

/** "EN VIVO" beacon with the age of the last sample and a pause / resume button. */
export function LiveBadge({ receivedAt, paused, onToggle, error }) {
  const [now, setNow] = useState(() => receivedAt ?? 0)
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(id)
  }, [])
  const age = receivedAt ? Math.max(0, Math.round((Math.max(now, receivedAt) - receivedAt) / 1000)) : null
  const stale = error || age == null || age > 10
  return (
    <div className="inline-flex items-center gap-1 rounded-full border border-line bg-white p-1 pl-3">
      <span className="flex items-center gap-2 text-sm font-semibold" role="status">
        {stale && !paused ? (
          <WifiOff className="h-4 w-4 text-risk-critical" aria-hidden="true" />
        ) : (
          <span className={cx('h-2.5 w-2.5 rounded-full', paused ? 'bg-basalt-mute' : 'bg-risk-normal animate-beacon motion-reduce:animate-none')} aria-hidden="true" />
        )}
        {paused ? 'Pausado' : stale ? 'Sin datos del gateway' : 'En vivo'}
        {!paused && !stale && <span className="numeral font-normal text-basalt-mute">· hace {age} s</span>}
      </span>
      {onToggle && (
        <button
          type="button"
          onClick={onToggle}
          aria-label={paused ? 'Reanudar la vista en vivo' : 'Pausar la vista en vivo'}
          title={paused ? 'Reanudar' : 'Pausar'}
          className="grid h-8 w-8 place-items-center rounded-full text-basalt-soft hover:bg-basalt/5 focus-visible:outline focus-visible:outline-2 focus-visible:outline-ore"
        >
          {paused ? <Play className="h-4 w-4" /> : <Pause className="h-4 w-4" />}
        </button>
      )}
    </div>
  )
}


/** Tiny SVG line of the last samples; cheap enough for one per fleet tile. */
export function Sparkline({ points = [], field, min = 0, max = 1, tone = 'info', height = 32, threshold }) {
  const values = points.map((p) => p[field]).filter((v) => v != null)
  if (values.length < 2) return <div style={{ height }} className="rounded bg-mineral/60" aria-hidden="true" />
  const width = 120
  const y = (v) => height - ((Math.min(Math.max(v, min), max) - min) / (max - min)) * (height - 4) - 2
  const step = width / (values.length - 1)
  const d = values.map((v, i) => `${i ? 'L' : 'M'}${(i * step).toFixed(1)},${y(v).toFixed(1)}`).join(' ')
  return (
    <svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none" className="block w-full" style={{ height }} aria-hidden="true">
      {threshold != null && <line x1="0" x2={width} y1={y(threshold)} y2={y(threshold)} stroke="#E58A00" strokeDasharray="3 3" strokeWidth="1" />}
      <path d={d} fill="none" stroke={STROKE[tone] ?? STROKE.info} strokeWidth="1.8" vectorEffect="non-scaling-stroke" strokeLinejoin="round" />
    </svg>
  )
}

/** One live sensor value with its unit and a text status (never color alone). */
export function Reading({ icon: Icon, name, value, unit, digits = 0, status, tone = 'neutral', source }) {
  const ink = { Critical: 'text-[#B3261E]', Warning: 'text-[#8A5200]', Normal: 'text-[#1E6B48]', neutral: 'text-basalt-mute', info: 'text-[#175E6C]' }[tone]
  return (
    <div className="rounded-xl border border-line bg-white p-3">
      <div className="flex items-center justify-between gap-2 text-sm text-basalt-mute">
        <span className="flex items-center gap-1.5 font-medium">{Icon && <Icon className="h-4 w-4" aria-hidden="true" />}{name}</span>
        {source && <span className="text-[12px]">{source}</span>}
      </div>
      <div className="mt-1 flex items-baseline gap-1">
        <span className="numeral text-[30px] leading-none">{value == null ? '—' : Number(value).toFixed(digits)}</span>
        {unit && value != null && <span className="text-sm text-basalt-mute">{unit}</span>}
      </div>
      {status && <div className={cx('mt-1 text-[13px] font-semibold', ink)}>{status}</div>}
    </div>
  )
}


