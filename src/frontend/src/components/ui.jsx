import { useId } from 'react'
import { alertStatusLabels, incidentStatusLabels, riskLevelLabels } from '../i18n/labels.js'

const buttonVariants = {
  primary: 'bg-malachite text-sheet hover:bg-malachite-deep',
  secondary: 'border border-rule bg-sheet text-ink hover:border-rock',
  danger: 'border border-signal/50 bg-sheet text-signal hover:bg-signal-wash',
  quiet: 'text-malachite underline-offset-4 hover:underline',
}

export function Button({ variant = 'primary', pending = false, className = '', children, ...props }) {
  return (
    <button
      type="button"
      {...props}
      disabled={pending || props.disabled}
      aria-busy={pending || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded px-3.5 py-2 text-sm font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-45 ${buttonVariants[variant]} ${className}`}
    >
      {pending && <Spinner className="h-4 w-4" />}
      {children}
    </button>
  )
}

const controlClass =
  'w-full rounded border border-rule bg-sheet px-3 py-2 text-base text-ink placeholder:text-rock/70 focus:border-malachite focus:outline-none focus:ring-2 focus:ring-malachite/25 aria-[invalid=true]:border-signal'

function FieldShell({ id, label, hint, invalid, children, className = '' }) {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label htmlFor={id} className="text-sm font-medium text-ink-soft">
        {label}
      </label>
      {children}
      {hint && (
        <p id={`${id}-hint`} className={`text-xs ${invalid ? 'text-signal' : 'text-rock'}`}>
          {hint}
        </p>
      )}
    </div>
  )
}

export function TextField({ label, hint, className, ...props }) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} invalid={props['aria-invalid']} className={className}>
      <input id={id} aria-describedby={hint ? `${id}-hint` : undefined} className={controlClass} {...props} />
    </FieldShell>
  )
}

export function TextArea({ label, hint, className, ...props }) {
  const id = useId()
  return (
    <FieldShell id={id} label={label} hint={hint} invalid={props['aria-invalid']} className={className}>
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

/** A titled region of a page, separated by a single rule instead of a card. */
export function Section({ title, description, actions, children, className = '' }) {
  return (
    <section className={`border-t border-rule pt-5 ${className}`}>
      {(title || actions) && (
        <header className="mb-4 flex flex-wrap items-end justify-between gap-3">
          <div className="max-w-prose">
            {title && <h2 className="text-lg font-semibold text-ink">{title}</h2>}
            {description && <p className="mt-0.5 text-sm text-rock">{description}</p>}
          </div>
          {actions}
        </header>
      )}
      {children}
    </section>
  )
}

/** Paper surface for forms that sit beside a list. */
export function Sheet({ title, description, children, className = '' }) {
  return (
    <section className={`rounded-md border border-rule bg-sheet p-5 ${className}`}>
      {title && <h2 className="text-lg font-semibold text-ink">{title}</h2>}
      {description && <p className="mb-4 mt-0.5 text-sm text-rock">{description}</p>}
      {children}
    </section>
  )
}

export function Spinner({ className = 'h-5 w-5' }) {
  return (
    <svg className={`animate-spin ${className}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle cx="12" cy="12" r="10" stroke="currentColor" strokeOpacity="0.25" strokeWidth="3" />
      <path d="M22 12a10 10 0 0 0-10-10" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
    </svg>
  )
}

export function LoadingState({ text = 'Cargando…' }) {
  return (
    <div role="status" className="flex items-center gap-2 py-8 text-sm text-rock">
      <Spinner className="h-4 w-4" />
      {text}
    </div>
  )
}

export function EmptyState({ title, children, action }) {
  return (
    <div className="border-l-2 border-rule py-3 pl-4">
      <p className="font-medium text-ink">{title}</p>
      {children && <p className="mt-0.5 max-w-prose text-sm text-rock">{children}</p>}
      {action && <div className="mt-3">{action}</div>}
    </div>
  )
}

export function ErrorMessage({ message, onDismiss }) {
  if (!message) return null
  return (
    <div
      role="alert"
      className="flex items-start justify-between gap-3 border-l-[3px] border-signal bg-signal-wash px-3 py-2 text-sm text-ink"
    >
      <span>{message}</span>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          className="rounded px-1 text-rock hover:text-ink"
          aria-label="Cerrar mensaje de error"
        >
          ✕
        </button>
      )}
    </div>
  )
}

export function SuccessMessage({ message }) {
  if (!message) return null
  return (
    <div role="status" className="border-l-[3px] border-malachite bg-malachite-wash px-3 py-2 text-sm text-ink">
      {message}
    </div>
  )
}

/**
 * Risk is told by shape as well as colour (circle, diamond, taped square) so it
 * reads for colour-blind supervisors and on washed-out control-room screens.
 */
export function RiskMark({ level, size = 'sm' }) {
  const text = riskLevelLabels[level] ?? level ?? '—'
  const big = size === 'lg'
  if (level === 'Critical') {
    return (
      <span className={`inline-flex items-center gap-2 font-semibold text-signal ${big ? 'text-lg' : 'text-sm'}`}>
        <span className={`hazard-tape pulse-critical inline-block rounded-sm ${big ? 'h-4 w-4' : 'h-3 w-3'}`} aria-hidden="true" />
        {text}
      </span>
    )
  }
  if (level === 'Warning') {
    return (
      <span className={`inline-flex items-center gap-2 font-semibold text-ochre ${big ? 'text-lg' : 'text-sm'}`}>
        <span className={`inline-block rotate-45 bg-ochre-bright ${big ? 'h-3 w-3' : 'h-2 w-2'}`} aria-hidden="true" />
        {text}
      </span>
    )
  }
  return (
    <span className={`inline-flex items-center gap-2 text-rock ${big ? 'text-lg' : 'text-sm'}`}>
      <span className={`inline-block rounded-full border-2 border-rock/60 ${big ? 'h-3.5 w-3.5' : 'h-2.5 w-2.5'}`} aria-hidden="true" />
      {text}
    </span>
  )
}

const statusTones = {
  attention: 'bg-ochre-wash text-ochre',
  danger: 'bg-signal-wash text-signal',
  progress: 'bg-malachite-wash text-malachite-deep',
  done: 'bg-limestone text-rock',
}

function StatusTag({ tone, children }) {
  return (
    <span className={`inline-flex items-center rounded-sm px-1.5 py-0.5 text-xs font-semibold ${statusTones[tone]}`}>
      {children}
    </span>
  )
}

const alertStatusTones = { Issued: 'attention', Acknowledged: 'done', Escalated: 'danger' }

export function AlertStatusTag({ status }) {
  return <StatusTag tone={alertStatusTones[status] ?? 'done'}>{alertStatusLabels[status] ?? status}</StatusTag>
}

const incidentStatusTones = { Pending: 'attention', Assigned: 'progress', Escalated: 'danger', Closed: 'done' }

export function IncidentStatusTag({ status }) {
  return (
    <StatusTag tone={incidentStatusTones[status] ?? 'done'}>{incidentStatusLabels[status] ?? status}</StatusTag>
  )
}

/** GUID shown abbreviated; the full value is in the tooltip and for screen readers. */
export function Guid({ value }) {
  if (!value) return <span className="text-rock">—</span>
  return (
    <span title={value} className="stretch-condensed text-xs text-rock">
      <span aria-hidden="true">{String(value).slice(0, 8)}</span>
      <span className="sr-only">{value}</span>
    </span>
  )
}

/** Collapsible block for secondary forms, so the main list keeps the attention. */
export function Disclosure({ summary, children, defaultOpen = false }) {
  return (
    <details open={defaultOpen} className="group rounded-md border border-rule bg-sheet">
      <summary className="flex cursor-pointer list-none items-center justify-between gap-3 px-4 py-3 font-semibold text-ink [&::-webkit-details-marker]:hidden">
        {summary}
        <span aria-hidden="true" className="text-rock transition-transform group-open:rotate-45">
          +
        </span>
      </summary>
      <div className="border-t border-rule px-4 pb-4 pt-4">{children}</div>
    </details>
  )
}
