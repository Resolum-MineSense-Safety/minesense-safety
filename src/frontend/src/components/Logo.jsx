// MineSense mark: the benches of an open pit seen from above form a watchful eye; each bench sits
// a little lower, so the walls descend toward the pupil, the beacon. Source of public/favicon.svg.
const BENCHES = [
  'M6 29.8C20.3 9.18 43.7 9.18 58 29.8C43.7 50.42 20.3 50.42 6 29.8Z',
  'M11.4 31.7C22.73 16.8 41.27 16.8 52.6 31.7C41.27 46.6 22.73 46.6 11.4 31.7Z',
  'M16.6 33.2C25.07 23.09 38.93 23.09 47.4 33.2C38.93 43.31 25.07 43.31 16.6 33.2Z',
  'M21.4 34.4C27.23 28.28 36.77 28.28 42.6 34.4C36.77 40.52 27.23 40.52 21.4 34.4Z',
]
const INK = '#1F2A2E'
const LIGHT = '#F2B705'

/** variant "plate": basalt eye on the yellow plate. variant "bare": yellow eye straight on a basalt ground. */
export function LogoMark({ size = 40, variant = 'plate', title = 'MineSense Safety', className }) {
  const plate = variant === 'plate'
  const ink = plate ? INK : LIGHT
  const light = plate ? LIGHT : INK
  return (
    <svg viewBox="0 0 64 64" width={size} height={size} role="img" aria-label={title} className={className}>
      {plate && <rect width="64" height="64" rx="15" fill={LIGHT} />}
      {BENCHES.map((d, index) => (
        <path key={d} d={d} fill={index % 2 === 0 ? ink : light} />
      ))}
      <circle cx="32" cy="35.2" r="3" fill={ink} />
    </svg>
  )
}

/** Mark with the condensed wordmark. `tone` follows the ground it sits on. */
export function Logo({ size = 40, tone = 'dark' }) {
  return (
    <span className="flex items-center gap-3">
      <LogoMark size={size} />
      <span className="leading-none">
        <span className={`block font-cond font-semibold ${tone === 'dark' ? 'text-white' : 'text-basalt'}`} style={{ fontSize: size * 0.55 }}>
          MineSense
        </span>
        <span className={`mt-1 block text-xs font-medium ${tone === 'dark' ? 'text-white/50' : 'text-basalt-mute'}`}>Safety</span>
      </span>
    </span>
  )
}
