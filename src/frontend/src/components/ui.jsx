import { useId } from 'react'
import { alertStatusLabels, incidentStatusLabels, riskLevelLabels } from '../i18n/labels.js'

const buttonVariants = {
  primary:
    'bg-amber-400 text-slate-950 hover:bg-amber-300 focus-visible:outline-amber-300',
  secondary:
    'border border-slate-600 bg-slate-800 text-slate-100 hover:bg-slate-700 focus-visible:outline-slate-400',
  danger:
    'border border-red-500/60 bg-red-500/10 text-red-200 hover:bg-red-500/20 focus-visible:outline-red-400',
  ghost: 'text-slate-300 hover:bg-slate-800 hover:text-white focus-visible:outline-slate-400',
}

export function Button({ variant = 'primary', pending = false, className = '', children, ...props }) {
  return (
    <button
      type="button"
      {...props}
      disabled={pending || props.disabled}
      aria-busy={pending || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${buttonVariants[variant]} ${className}`}
    >
      {pending && <Spinner className="h-4 w-4" />}
      {children}
    </button>
  )
}

const controlClass =
  'w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 focus:border-amber-400 focus:outline-none focus:ring-1 focus:ring-amber-400'

function FieldShell({ id, label, hint, children, className = '' }) {
  return (
    <div className={`flex flex-col gap-1 ${className}`}>
      <label htmlFor={id} className="text-xs font-medium uppercase tracking-wide text-slate-400">
        {label}
      </label>
      {children}
      {hint && (
        <p id={`${id}-hint`} className="text-xs text-slate-500">
          {hint}
        </p>
      )}
    </div>
  )
}

export function TextField({ label, hint, className, ...props }) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} className={className}>
      <input id={id} aria-describedby={hint ? `${id}-hint` : undefined} className={controlClass} {...props} />
    </FieldShell>
  )
}

export function TextArea({ label, hint, className, ...props }) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} className={className}>
      <textarea
        id={id}
        rows={3}
        aria-describedby={hint ? `${id}-hint` : undefined}
        className={controlClass}
        {...props}
      />
    </FieldShell>
  )
}

/** `options` is a list of `{ value, label }`. */
export function SelectField({ label, hint, options, className, ...props }) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} className={className}>
      <select id={id} aria-describedby={hint ? `${id}-hint` : undefined} className={controlClass} {...props}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </FieldShell>
  )
}

export function Panel({ title, description, actions, children, className = '' }) {
  return (
    <section className={`rounded-lg border border-slate-800 bg-slate-900/70 ${className}`}>
      {(title || actions) && (
        <header className="flex flex-wrap items-start justify-between gap-3 border-b border-slate-800 px-4 py-3">
          <div>
            {title && <h2 className="text-sm font-semibold text-slate-100">{title}</h2>}
            {description && <p className="mt-0.5 text-xs text-slate-400">{description}</p>}
          </div>
          {actions}
        </header>
      )}
      <div className="p-4">{children}</div>
    </section>
  )
}

export function Spinner({ className = 'h-5 w-5' }) {
  return (
    <svg className={`animate-spin ${className}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="10" stroke="currentColor" strokeOpacity="0.25" strokeWidth="4" />
      <path d="M22 12a10 10 0 0 0-10-10" stroke="currentColor" strokeWidth="4" strokeLinecap="round" />
    </svg>
  )
}

export function LoadingState({ text = 'Cargando…' }) {
  return (
    <div role="status" className="flex items-center justify-center gap-2 py-10 text-sm text-slate-400">
      <Spinner />
      {text}
    </div>
  )
}

export function EmptyState({ title, children }) {
  return (
    <div className="rounded-md border border-dashed border-slate-700 px-4 py-10 text-center">
      <p className="text-sm font-medium text-slate-300">{title}</p>
      {children && <p className="mt-1 text-xs text-slate-500">{children}</p>}
    </div>
  )
}

export function ErrorMessage({ message, onDismiss }) {
  if (!message) return null
  return (
    <div
      role="alert"
      className="flex items-start justify-between gap-3 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200"
    >
      <span>{message}</span>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          className="text-red-300 hover:text-white"
          aria-label="Cerrar mensaje de error"
        >
          ×
        </button>
      )}
    </div>
  )
}

export function SuccessMessage({ message }) {
  if (!message) return null
  return (
    <div
      role="status"
      className="rounded-md border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-200"
    >
      {message}
    </div>
  )
}

const badgeTones = {
  green: 'border-emerald-500/40 bg-emerald-500/15 text-emerald-300',
  amber: 'border-amber-500/40 bg-amber-500/15 text-amber-300',
  red: 'border-red-500/50 bg-red-500/15 text-red-300',
  blue: 'border-sky-500/40 bg-sky-500/15 text-sky-300',
  slate: 'border-slate-600 bg-slate-700/40 text-slate-300',
}

export function Badge({ tone = 'slate', children, dot = false }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded border px-2 py-0.5 text-xs font-semibold ${badgeTones[tone]}`}
    >
      {dot && <span className="h-1.5 w-1.5 rounded-full bg-current" aria-hidden="true" />}
      {children}
    </span>
  )
}

const riskTones = { Normal: 'green', Warning: 'amber', Critical: 'red' }

export function RiskBadge({ level }) {
  return (
    <Badge tone={riskTones[level] ?? 'slate'} dot>
      {riskLevelLabels[level] ?? level ?? '—'}
    </Badge>
  )
}

const alertStatusTones = { Issued: 'amber', Acknowledged: 'green', Escalated: 'red' }

export function AlertStatusBadge({ status }) {
  return <Badge tone={alertStatusTones[status] ?? 'slate'}>{alertStatusLabels[status] ?? status}</Badge>
}

const incidentStatusTones = { Pending: 'amber', Assigned: 'blue', Escalated: 'red', Closed: 'slate' }

export function IncidentStatusBadge({ status }) {
  return (
    <Badge tone={incidentStatusTones[status] ?? 'slate'}>{incidentStatusLabels[status] ?? status}</Badge>
  )
}

/** Monospace GUID shown abbreviated, full value available on hover and to screen readers. */
export function Guid({ value }) {
  if (!value) return <span className="text-slate-500">—</span>
  return (
    <code title={value} className="font-mono text-xs text-slate-400">
      <span aria-hidden="true">{String(value).slice(0, 8)}…</span>
      <span className="sr-only">{value}</span>
    </code>
  )
}
