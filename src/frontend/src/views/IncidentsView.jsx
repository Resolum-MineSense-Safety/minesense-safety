import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  Disclosure,
  EmptyState,
  ErrorMessage,
  Guid,
  IncidentStatusTag,
  LoadingState,
  Section,
  SelectField,
  SuccessMessage,
  TextArea,
  TextField,
} from '../components/ui.jsx'
import { isGuid, useAsyncAction } from '../hooks/useAsyncAction.js'
import { formatDateTime } from '../i18n/labels.js'
import {
  assignIncident,
  closeIncident,
  escalateIncident,
  getIncidentsByStatus,
  openIncident,
  registerIncidentAction,
} from '../services/incidentManagementService.js'

const views = [
  { id: 'open', label: 'Abiertos', match: (i) => i.status !== 'Closed' },
  { id: 'closed', label: 'Cerrados', match: (i) => i.status === 'Closed' },
  { id: 'all', label: 'Todos', match: () => true },
]

const code = (id) => `INC-${String(id).slice(0, 6).toUpperCase()}`

export default function IncidentsView({ fleet, currentUser }) {
  const { byOperatorId, operators } = fleet
  const [incidents, setIncidents] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [view, setView] = useState('open')

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getIncidentsByStatus('')
      setIncidents(sortIncidents(data))
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelled = false
    getIncidentsByStatus('')
      .then((data) => !cancelled && setIncidents(sortIncidents(data)))
      .catch((err) => !cancelled && setError(err.message))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  const replaceIncident = (updated) =>
    setIncidents((prev) => prev.map((incident) => (incident.id === updated.id ? updated : incident)))

  const open = incidents.filter((i) => i.status !== 'Closed')
  const escalated = open.filter((i) => i.status === 'Escalated').length
  const shown = useMemo(() => incidents.filter(views.find((v) => v.id === view).match), [incidents, view])

  const title = loading
    ? 'Cargando incidentes…'
    : open.length === 0
      ? 'Sin incidentes abiertos'
      : `${open.length} ${open.length === 1 ? 'incidente abierto' : 'incidentes abiertos'}`

  return (
    <div className="flex flex-col gap-9">
      <header className="flex flex-col gap-3">
        <h1 className={`stretch-expanded text-3xl font-bold md:text-4xl ${escalated > 0 ? 'text-signal' : 'text-ink'}`}>
          {title}
        </h1>
        <p className="max-w-prose text-rock">
          Cada incidente se asigna a un supervisor, registra las acciones tomadas en cabina y se cierra con una
          resolución.
          {escalated > 0 && ` ${escalated} ${escalated === 1 ? 'está escalado' : 'están escalados'} y necesita${escalated === 1 ? '' : 'n'} un responsable.`}
        </p>
      </header>

      <Section
        title="Seguimiento"
        actions={
          <div className="flex items-center gap-3">
            <div role="radiogroup" aria-label="Mostrar incidentes" className="flex rounded border border-rule bg-sheet p-0.5">
              {views.map((option) => (
                <button
                  key={option.id}
                  type="button"
                  role="radio"
                  aria-checked={view === option.id}
                  onClick={() => setView(option.id)}
                  className={`rounded-sm px-3 py-1.5 text-sm font-medium ${
                    view === option.id ? 'bg-ink text-sheet' : 'text-rock hover:text-ink'
                  }`}
                >
                  {option.label}
                </button>
              ))}
            </div>
            <Button variant="secondary" onClick={load} pending={loading}>
              Actualizar
            </Button>
          </div>
        }
      >
        <ErrorMessage message={error} onDismiss={() => setError(null)} />
        {loading ? (
          <LoadingState text="Cargando incidentes…" />
        ) : shown.length === 0 ? (
          <EmptyState title={view === 'closed' ? 'Aún no se cerró ningún incidente' : 'No hay incidentes por atender'}>
            Los incidentes se abren desde una alerta, en la sección Alertas.
          </EmptyState>
        ) : (
          <ul className="flex flex-col" aria-label="Incidentes">
            {shown.map((incident) => (
              <IncidentItem
                key={incident.id}
                incident={incident}
                operator={byOperatorId[incident.operatorId]}
                currentUser={currentUser}
                onUpdated={replaceIncident}
              />
            ))}
          </ul>
        )}
      </Section>

      <Disclosure summary="Abrir un incidente manualmente">
        <OpenIncidentForm operators={operators} onOpened={load} />
      </Disclosure>
    </div>
  )
}

function sortIncidents(data) {
  const list = Array.isArray(data) ? [...data] : []
  return list.sort((a, b) => new Date(b.openedAt) - new Date(a.openedAt))
}

/** Pending, assigned, closed: escalation is a detour that sends the incident back to be assigned. */
function Lifecycle({ incident }) {
  const steps = [
    { id: 'opened', label: 'Abierto', done: true },
    { id: 'assigned', label: incident.status === 'Escalated' ? 'Escalado' : 'Asignado', done: incident.status !== 'Pending' },
    { id: 'closed', label: 'Cerrado', done: incident.status === 'Closed' },
  ]
  return (
    <ol className="flex items-center gap-2 text-xs" aria-label="Etapa del incidente">
      {steps.map((step, index) => {
        const isEscalated = step.id === 'assigned' && incident.status === 'Escalated'
        return (
          <li key={step.id} className="flex items-center gap-2">
            {index > 0 && <span className={`h-px w-6 ${step.done ? 'bg-ink' : 'bg-rule'}`} aria-hidden="true" />}
            <span
              className={`inline-flex items-center gap-1.5 ${
                isEscalated ? 'font-semibold text-signal' : step.done ? 'font-semibold text-ink' : 'text-rock'
              }`}
            >
              <span
                className={`inline-block h-2 w-2 rounded-full ${
                  isEscalated ? 'bg-signal' : step.done ? 'bg-ink' : 'border border-rock'
                }`}
                aria-hidden="true"
              />
              {step.label}
              <span className="sr-only">{step.done ? '(completado)' : '(pendiente)'}</span>
            </span>
          </li>
        )
      })}
    </ol>
  )
}

function IncidentItem({ incident, operator, currentUser, onUpdated }) {
  const [mode, setMode] = useState(null) // 'assign' | 'action' | 'close'
  const escalate = useAsyncAction(escalateIncident)
  const actions = incident.actions ?? []
  const closed = incident.status === 'Closed'
  const canAssign = incident.status === 'Pending' || incident.status === 'Escalated'
  const assigned = incident.status === 'Assigned'

  const handleEscalate = async () => {
    const updated = await escalate.run(incident.id)
    if (updated) onUpdated(updated)
  }

  const handleDone = (updated) => {
    setMode(null)
    onUpdated(updated)
  }

  const toggle = (next) => setMode((current) => (current === next ? null : next))

  return (
    <li className="border-b border-rule py-5">
      <div className="grid gap-4 lg:grid-cols-[1fr_auto]">
        <div className="flex min-w-0 flex-col gap-2">
          <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <h3 className="text-lg font-semibold">{operator?.fullName ?? 'Operador no registrado en la flota'}</h3>
            <span className="stretch-condensed text-sm text-rock" title={incident.id}>
              {code(incident.id)}
            </span>
            <IncidentStatusTag status={incident.status} />
          </div>
          <p className="stretch-condensed text-sm text-rock">
            {operator ? `${operator.vehicleCode}, ${operator.fleet}. ` : ''}Abierto el {formatDateTime(incident.openedAt)}
            {incident.assignedAt && `; asignado el ${formatDateTime(incident.assignedAt)}`}
            {incident.closedAt && `; cerrado el ${formatDateTime(incident.closedAt)}`}.
          </p>
          <Lifecycle incident={incident} />
        </div>

        {!closed && (
          <div className="flex flex-wrap items-start gap-2 lg:justify-end">
            {canAssign && (
              <Button onClick={() => toggle('assign')} aria-expanded={mode === 'assign'}>
                Asignar responsable
              </Button>
            )}
            {assigned && (
              <>
                <Button onClick={() => toggle('action')} aria-expanded={mode === 'action'}>
                  Registrar acción
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => toggle('close')}
                  aria-expanded={mode === 'close'}
                  disabled={actions.length === 0}
                  title={actions.length === 0 ? 'Registre al menos una acción antes de cerrar.' : undefined}
                >
                  Cerrar incidente
                </Button>
              </>
            )}
            {incident.status !== 'Escalated' && (
              <Button variant="danger" onClick={handleEscalate} pending={escalate.pending}>
                Escalar
              </Button>
            )}
          </div>
        )}
      </div>

      {actions.length > 0 && (
        <ol className="mt-4 flex flex-col gap-3 border-l-2 border-rule pl-4" aria-label="Acciones registradas">
          {actions.map((action, index) => (
            <li key={`${action.registeredAt}-${index}`}>
              <p className="text-base">{action.description}</p>
              <p className="text-sm text-rock">
                Resultado: {action.outcome}. {formatDateTime(action.registeredAt)}, supervisor <Guid value={action.supervisorId} />
              </p>
            </li>
          ))}
        </ol>
      )}

      {incident.resolution && (
        <p className="mt-4 max-w-prose border-l-2 border-malachite pl-4">
          <span className="font-semibold">Resolución.</span> {incident.resolution}
        </p>
      )}

      <div className="mt-3">
        <ErrorMessage message={escalate.error} onDismiss={escalate.clearError} />
      </div>

      {mode === 'assign' && (
        <AssignForm incidentId={incident.id} currentUser={currentUser} onDone={handleDone} onCancel={() => setMode(null)} />
      )}
      {mode === 'action' && (
        <ActionForm incidentId={incident.id} currentUser={currentUser} onDone={handleDone} onCancel={() => setMode(null)} />
      )}
      {mode === 'close' && <CloseForm incidentId={incident.id} onDone={handleDone} onCancel={() => setMode(null)} />}
    </li>
  )
}

function InlineForm({ title, onSubmit, onCancel, pending, error, clearError, submitLabel, disabled, children }) {
  return (
    <form
      onSubmit={onSubmit}
      aria-label={title}
      className="mt-4 flex max-w-2xl flex-col gap-3 rounded-md border border-rule bg-sheet p-4"
    >
      <p className="font-semibold">{title}</p>
      {children}
      <ErrorMessage message={error} onDismiss={clearError} />
      <div className="flex gap-2">
        <Button type="submit" pending={pending} disabled={disabled}>
          {submitLabel}
        </Button>
        <Button variant="quiet" onClick={onCancel}>
          Cancelar
        </Button>
      </div>
    </form>
  )
}

function SupervisorField({ currentUser, value, onChange }) {
  const invalid = value !== '' && !isGuid(value)
  if (currentUser && value === currentUser.id) {
    return (
      <p className="text-sm text-rock">
        Responsable: <span className="font-semibold text-ink">{currentUser.username}</span> (sesión actual).{' '}
        <button type="button" className="text-malachite underline" onClick={() => onChange('')}>
          Elegir a otra persona
        </button>
      </p>
    )
  }
  return (
    <TextField
      label="Identificador del supervisor"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      required
      aria-invalid={invalid || undefined}
      hint={invalid ? 'El identificador no tiene el formato correcto.' : 'Inicie sesión para usar su propia cuenta.'}
    />
  )
}

function AssignForm({ incidentId, currentUser, onDone, onCancel }) {
  const [supervisorId, setSupervisorId] = useState(currentUser?.id ?? '')
  const { run, pending, error, clearError } = useAsyncAction(assignIncident)

  const handleSubmit = async (event) => {
    event.preventDefault()
    const updated = await run(incidentId, supervisorId.trim())
    if (updated) onDone(updated)
  }

  return (
    <InlineForm
      title="Asignar responsable"
      onSubmit={handleSubmit}
      onCancel={onCancel}
      pending={pending}
      error={error}
      clearError={clearError}
      submitLabel="Asignar"
      disabled={!isGuid(supervisorId)}
    >
      <SupervisorField currentUser={currentUser} value={supervisorId} onChange={setSupervisorId} />
    </InlineForm>
  )
}

function ActionForm({ incidentId, currentUser, onDone, onCancel }) {
  const [form, setForm] = useState({ supervisorId: currentUser?.id ?? '', description: '', outcome: '' })
  const { run, pending, error, clearError } = useAsyncAction(registerIncidentAction)
  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    const updated = await run(incidentId, { ...form, supervisorId: form.supervisorId.trim() })
    if (updated) onDone(updated)
  }

  return (
    <InlineForm
      title="Registrar acción en cabina"
      onSubmit={handleSubmit}
      onCancel={onCancel}
      pending={pending}
      error={error}
      clearError={clearError}
      submitLabel="Registrar acción"
      disabled={!isGuid(form.supervisorId)}
    >
      <SupervisorField
        currentUser={currentUser}
        value={form.supervisorId}
        onChange={(value) => setForm((prev) => ({ ...prev, supervisorId: value }))}
      />
      <TextArea
        label="Qué se hizo"
        value={form.description}
        onChange={update('description')}
        placeholder="Se detuvo el camión en la bahía de seguridad y se relevó al operador."
        required
      />
      <TextField
        label="Resultado"
        value={form.outcome}
        onChange={update('outcome')}
        placeholder="Operador en descanso supervisado"
        required
      />
    </InlineForm>
  )
}

function CloseForm({ incidentId, onDone, onCancel }) {
  const [resolution, setResolution] = useState('')
  const { run, pending, error, clearError } = useAsyncAction(closeIncident)

  const handleSubmit = async (event) => {
    event.preventDefault()
    const updated = await run(incidentId, resolution)
    if (updated) onDone(updated)
  }

  return (
    <InlineForm
      title="Cerrar incidente"
      onSubmit={handleSubmit}
      onCancel={onCancel}
      pending={pending}
      error={error}
      clearError={clearError}
      submitLabel="Cerrar incidente"
    >
      <TextArea
        label="Resolución"
        value={resolution}
        onChange={(event) => setResolution(event.target.value)}
        placeholder="El operador retomó la jornada tras el descanso y la evaluación médica."
        required
      />
    </InlineForm>
  )
}

function OpenIncidentForm({ operators, onOpened }) {
  const [form, setForm] = useState({ alertId: '', operatorId: '' })
  const [success, setSuccess] = useState(null)
  const { run, pending, error, clearError } = useAsyncAction(openIncident)
  const invalidAlert = form.alertId !== '' && !isGuid(form.alertId)

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSuccess(null)
    const created = await run({ alertId: form.alertId.trim(), operatorId: form.operatorId })
    if (created) {
      setSuccess(`Incidente ${code(created.id)} abierto.`)
      setForm({ alertId: '', operatorId: '' })
      onOpened()
    }
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-4 md:grid-cols-2">
      <SelectField
        label="Operador"
        value={form.operatorId}
        onChange={(event) => setForm((prev) => ({ ...prev, operatorId: event.target.value }))}
        options={[
          { value: '', label: 'Seleccione un operador' },
          ...operators.map((op) => ({ value: op.operatorId, label: `${op.fullName} (${op.vehicleCode})` })),
        ]}
        required
      />
      <TextField
        label="Identificador de la alerta"
        value={form.alertId}
        onChange={(event) => setForm((prev) => ({ ...prev, alertId: event.target.value }))}
        required
        aria-invalid={invalidAlert || undefined}
        hint={invalidAlert ? 'El identificador no tiene el formato correcto.' : 'Lo encuentra en el detalle de la alerta.'}
      />
      <div className="flex flex-col gap-2 md:col-span-2">
        <ErrorMessage message={error} onDismiss={clearError} />
        <SuccessMessage message={success} />
        <div>
          <Button type="submit" pending={pending} disabled={invalidAlert || !form.operatorId}>
            Abrir incidente
          </Button>
        </div>
      </div>
    </form>
  )
}
