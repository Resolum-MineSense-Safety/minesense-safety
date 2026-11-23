import { X, Loader2 } from 'lucide-react'
import { useEffect } from 'react'
import { label } from '../lib/format'

const cx = (...classes) => classes.filter(Boolean).join(' ')

const BUTTON = {
  primary: 'bg-basalt text-white hover:bg-basalt-soft',
  accent: 'bg-haul text-basalt hover:bg-[#E5AC00]',
  ghost: 'bg-transparent text-basalt hover:bg-basalt/5',
  outline: 'border border-line bg-white text-basalt hover:border-basalt-mute',
  danger: 'bg-risk-critical text-white hover:bg-[#BC2C26]',
  ok: 'bg-risk-normal text-white hover:bg-[#268A5C]',
}

export function Button({ variant = 'primary', icon: Icon, busy, size = 'md', className, children, ...props }) {
  return (
    <button
      type="button"
      className={cx(
        'inline-flex items-center justify-center gap-2 rounded-full font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50',
        size === 'sm' ? 'h-8 px-3 text-sm' : size === 'lg' ? 'h-12 px-6 text-base' : 'h-10 px-4 text-[15px]',
        BUTTON[variant],
        className,
      )}
      disabled={busy || props.disabled}
      {...props}
    >
      {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : Icon && <Icon className="h-4 w-4" />}
      {children}
    </button>
  )
}

export function IconButton({ icon: Icon, label: text, className, ...props }) {
  return (
    <button
      type="button"
      title={text}
      aria-label={text}
      className={cx('grid h-9 w-9 place-items-center rounded-full text-basalt-soft hover:bg-basalt/5', className)}
      {...props}
    >
      <Icon className="h-[18px] w-[18px]" />
    </button>
  )
}

const TONES = {
  Critical: 'bg-risk-criticalWash text-risk-critical',
  Warning: 'bg-risk-warningWash text-[#A15F00]',
  Normal: 'bg-risk-normalWash text-risk-normal',
  neutral: 'bg-basalt/5 text-basalt-soft',
  info: 'bg-ore-wash text-ore',
  accent: 'bg-haul-wash text-[#8A6700]',
}

// Status value -> tone, so every screen colors the same state the same way.
const STATUS_TONE = {
  Critical: 'Critical', Escalated: 'Critical', Interrupted: 'Critical', ProtectionIncomplete: 'Critical',
  Failed: 'Critical', Denied: 'Critical', Declined: 'Critical', Revoked: 'Critical', FALSE_ALARM: 'Critical',
  Unavailable: 'Critical', Inactive: 'Critical',
  Warning: 'Warning', Issued: 'Warning', Pending: 'Warning', Limited: 'Warning', Partial: 'Warning',
  AWAITING_REVIEW: 'Warning', Rendering: 'Warning', Requested: 'Warning', InMaintenance: 'Warning',
  Normal: 'Normal', Acknowledged: 'Normal', Closed: 'Normal', Available: 'Normal', Active: 'Normal', Ready: 'Normal',
  Accepted: 'Normal', Granted: 'Normal', CONFIRMED: 'Normal', REVIEWED: 'Normal',
  Assigned: 'info', Stopped: 'neutral', PENDING: 'neutral', INCONCLUSIVE: 'neutral',
}

export function Pill({ value, tone, children, dot = true, className }) {
  const resolved = tone ?? STATUS_TONE[value] ?? 'neutral'
  return (
    <span className={cx('inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-0.5 text-[13px] font-semibold', TONES[resolved], className)}>
      {dot && <span className={cx('h-1.5 w-1.5 rounded-full bg-current', value === 'Critical' && 'animate-beacon')} />}
      {children ?? label(value)}
    </span>
  )
}

export function Panel({ title, icon: Icon, actions, className, bodyClass, children }) {
  return (
    <section className={cx('rounded-tile border border-line bg-white', className)}>
      {(title || actions) && (
        <header className="flex items-center justify-between gap-3 border-b border-line px-4 py-3">
          <h2 className="flex items-center gap-2 text-[15px] font-semibold">
            {Icon && <Icon className="h-4 w-4 text-basalt-mute" />}
            {title}
          </h2>
          <div className="flex items-center gap-2">{actions}</div>
        </header>
      )}
      <div className={cx('p-4', bodyClass)}>{children}</div>
    </section>
  )
}

/** Large figure, small label. Tone colors the left rule, never the number. */
export function Stat({ value, caption, tone = 'neutral', icon: Icon, onClick }) {
  const rule = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal', info: 'bg-ore', accent: 'bg-haul', neutral: 'bg-line' }[tone]
  const Tag = onClick ? 'button' : 'div'
  return (
    <Tag onClick={onClick} className={cx('relative flex items-center gap-3 overflow-hidden rounded-tile border border-line bg-white py-3 pl-5 pr-4 text-left', onClick && 'hover:border-basalt-mute')}>
      <span className={cx('absolute inset-y-0 left-0 w-1.5', rule)} />
      <div className="min-w-0 flex-1">
        <div className="numeral text-[34px] leading-none">{value ?? '—'}</div>
        <div className="mt-1 truncate text-sm text-basalt-mute">{caption}</div>
      </div>
      {Icon && <Icon className="h-6 w-6 shrink-0 text-basalt-mute/60" />}
    </Tag>
  )
}

export function Drawer({ open, title, onClose, children, footer }) {
  useEffect(() => {
    if (!open) return undefined
    const onKey = (event) => event.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])
  if (!open) return null
  return (
    <div className="fixed inset-0 z-40 flex justify-end">
      <button type="button" aria-label="Cerrar" className="absolute inset-0 bg-basalt/30" onClick={onClose} />
      <aside role="dialog" aria-modal="true" aria-label={title} className="relative flex h-full w-full max-w-md animate-slidein flex-col bg-white shadow-2xl">
        <header className="flex items-center justify-between border-b border-line px-5 py-4">
          <h2 className="text-lg font-semibold">{title}</h2>
          <IconButton icon={X} label="Cerrar" onClick={onClose} />
        </header>
        <div className="flex-1 space-y-4 overflow-y-auto px-5 py-4">{children}</div>
        {footer && <footer className="flex justify-end gap-2 border-t border-line px-5 py-3">{footer}</footer>}
      </aside>
    </div>
  )
}

export function Field({ label: text, children, hint }) {
  return (
    <label className="block space-y-1">
      <span className="text-sm font-medium text-basalt-soft">{text}</span>
      {children}
      {hint && <span className="block text-xs text-basalt-mute">{hint}</span>}
    </label>
  )
}

export function Input(props) {
  return <input className="field" {...props} />
}

export function Select({ options, placeholder, ...props }) {
  return (
    <select className="field" {...props}>
      {placeholder && <option value="">{placeholder}</option>}
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  )
}

/** Segmented control: a row of pill buttons, one selected. */
export function Segmented({ value, onChange, options }) {
  return (
    <div className="inline-flex flex-wrap gap-1 rounded-full border border-line bg-white p-1">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          onClick={() => onChange(option.value)}
          className={cx(
            'flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-semibold transition-colors',
            value === option.value ? 'bg-basalt text-white' : 'text-basalt-soft hover:bg-basalt/5',
          )}
        >
          {option.label}
          {option.count != null && (
            <span className={cx('numeral rounded-full px-1.5 text-xs', value === option.value ? 'bg-white/20' : 'bg-basalt/5')}>
              {option.count}
            </span>
          )}
        </button>
      ))}
    </div>
  )
}

export function Empty({ icon: Icon, title, action }) {
  return (
    <div className="flex flex-col items-center gap-3 py-10 text-center">
      {Icon && (
        <span className="grid h-14 w-14 place-items-center rounded-full bg-mineral">
          <Icon className="h-6 w-6 text-basalt-mute" />
        </span>
      )}
      <p className="text-[15px] text-basalt-mute">{title}</p>
      {action}
    </div>
  )
}

export function Spinner() {
  return (
    <div className="grid place-items-center py-10">
      <Loader2 className="h-6 w-6 animate-spin text-basalt-mute" />
    </div>
  )
}

/** Ring gauge for PERCLOS and other 0–1 ratios. */
export function Ring({ value = 0, size = 64, stroke = 7, tone = 'Normal', children }) {
  const radius = (size - stroke) / 2
  const circumference = 2 * Math.PI * radius
  const color = { Critical: '#D6362F', Warning: '#E58A00', Normal: '#2E9E6B', info: '#1F7A8C' }[tone] ?? '#6B7A80'
  return (
    <div className="relative grid place-items-center" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={radius} stroke="#E8ECEB" strokeWidth={stroke} fill="none" />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke={color}
          strokeWidth={stroke}
          fill="none"
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={circumference * (1 - Math.min(Math.max(value, 0), 1))}
        />
      </svg>
      <div className="absolute inset-0 grid place-items-center">{children}</div>
    </div>
  )
}

/** Battery-style horizontal meter (0–100). */
export function Meter({ value, threshold = 20 }) {
  if (value == null) return <span className="text-sm text-basalt-mute">—</span>
  const tone = value < threshold ? 'bg-risk-critical' : value < 50 ? 'bg-risk-warning' : 'bg-risk-normal'
  return (
    <div className="flex items-center gap-2">
      <div className="relative h-3 w-16 rounded-sm border border-basalt-mute/50 p-px">
        <div className={cx('h-full rounded-[1px]', tone)} style={{ width: `${value}%` }} />
        <span className="absolute -right-1 top-1/2 h-1.5 w-0.5 -translate-y-1/2 rounded-r bg-basalt-mute/50" />
      </div>
      <span className="numeral text-sm">{Math.round(value)}%</span>
    </div>
  )
}

export function Table({ columns, rows, onRowClick, empty }) {
  if (!rows?.length) return empty ?? null
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-[14px]">
        <thead>
          <tr className="border-b border-line text-[13px] text-basalt-mute">
            {columns.map((column) => (
              <th key={column.key} className={cx('px-3 py-2 font-medium', column.className)}>
                {column.title}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr
              key={row.id ?? row.key ?? index}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={cx('border-b border-line/70 last:border-0', onRowClick && 'cursor-pointer hover:bg-mineral/70')}
            >
              {columns.map((column) => (
                <td key={column.key} className={cx('px-3 py-2.5', column.className)}>
                  {column.render ? column.render(row) : row[column.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function PageHeader({ icon: Icon, title, children }) {
  return (
    <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
      <h1 className="flex items-center gap-3 font-cond text-[30px] font-semibold leading-none">
        {Icon && (
          <span className="grid h-10 w-10 place-items-center rounded-xl bg-haul">
            <Icon className="h-5 w-5 text-basalt" />
          </span>
        )}
        {title}
      </h1>
      <div className="flex flex-wrap items-center gap-2">{children}</div>
    </div>
  )
}
