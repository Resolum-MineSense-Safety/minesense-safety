// Streaming chart of one cabin signal. Its own module so Recharts only loads with the Cabin screen.
import { Area, AreaChart, CartesianGrid, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { STROKE } from '../lib/telemetry'

const timeLabel = (t) => new Date(t).toLocaleTimeString('es-PE', { minute: '2-digit', second: '2-digit' })

/** Streaming area chart of one signal over the buffered window (last 3 minutes). */
export default function StreamChart({ points = [], field, name, unit = '', domain = [0, 'auto'], thresholds = [], tone = 'info', format = (v) => v }) {
  const data = points.map((p) => ({ t: p.t, v: p[field] }))
  const color = STROKE[tone] ?? STROKE.info
  const id = `grad-${field}`
  return (
    <figure className="h-44" aria-label={`${name}: últimos ${Math.round(points.length / 60) || 1} minutos`}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
          <defs>
            <linearGradient id={id} x1="0" x2="0" y1="0" y2="1">
              <stop offset="0%" stopColor={color} stopOpacity={0.28} />
              <stop offset="100%" stopColor={color} stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke="#E8ECEB" vertical={false} />
          <XAxis dataKey="t" tickFormatter={timeLabel} minTickGap={48} tick={{ fontSize: 12, fill: '#5E6D73' }} stroke="#DDE3E1" />
          <YAxis domain={domain} tickFormatter={format} width={44} tick={{ fontSize: 12, fill: '#5E6D73' }} stroke="#DDE3E1" />
          <Tooltip
            labelFormatter={(t) => new Date(t).toLocaleTimeString('es-PE')}
            formatter={(v) => [v == null ? 'sin señal' : `${format(v)}${unit}`, name]}
            contentStyle={{ borderRadius: 10, borderColor: '#DDE3E1', fontSize: 13 }}
          />
          {thresholds.map((line) => (
            <ReferenceLine key={line.value} y={line.value} stroke={line.color} strokeDasharray="4 4"
              label={{ value: line.label, position: 'insideTopRight', fontSize: 11, fill: line.color }} />
          ))}
          <Area type="monotone" dataKey="v" stroke={color} strokeWidth={2} fill={`url(#${id})`} isAnimationActive={false} connectNulls={false} dot={false} />
        </AreaChart>
      </ResponsiveContainer>
    </figure>
  )
}
