import { useEffect, useState } from 'react'
import {
  Button,
  EmptyState,
  ErrorMessage,
  Guid,
  IncidentStatusBadge,
  LoadingState,
  Panel,
  SelectField,
  SuccessMessage,
  TextArea,
  TextField,
} from '../components/ui.jsx'
import { isGuid, useAsyncAction } from '../hooks/useAsyncAction.js'
import { formatDateTime, incidentStatusLabels } from '../i18n/labels.js'
import {
  assignIncident,
  closeIncident,
  escalateIncident,
  getIncidentsByStatus,
  INCIDENT_STATUSES,
  openIncident,
  registerIncidentAction,
} from '../services/incidentManagementService.js'

const statusOptions = [
  { value: '', label: 'Todos los estados' },
  ...INCIDENT_STATUSES.map((value) => ({ value, label: incidentStatusLabels[value] })),
]

export default function IncidentsView({ currentUser }) {
  const [status, setStatus] = useState('')
  const [incidents, setIncidents] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const load = (activeStatus) => {
    setLoading(true)
    setError(null)
    return getIncidentsByStatus(activeStatus)
      .then((data) => setIncidents(sortIncidents(data)))
      .catch((err) => {
        setIncidents([])
        setError(err.message)
      })
      .finally(() => setLoading(false))
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

  const handleStatusChange = (event) => {
    setStatus(event.target.value)
    load(event.target.value)
  }

  const replaceIncident = (updated) =>
    setIncidents((prev) =>
      status && updated.status !== status
        ? prev.filter((incident) => incident.id !== updated.id)
        : prev.map((incident) => (incident.id === updated.id ? updated : incident)),
    )

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_22rem]">
      <Panel
        title="Incidentes"
        description="Seguimiento de incidentes de fatiga hasta su cierre."
        actions={
          <div className="flex items-end gap-2">
            <SelectField
              label="Estado"
              value={status}
              onChange={handleStatusChange}
              options={statusOptions}
              className="w-48"
            />
            <Button variant="secondary" onClick={() => load(status)} pending={loading}>
              Actualizar
            </Button>
          </div>
        }
      >
        <ErrorMessage message={error} onDismiss={() => setError(null)} />
        {loading ? (
          <LoadingState text="Cargando incidentes…" />
        ) : incidents.length === 0 ? (
          !error && (
            <EmptyState title="No hay incidentes en este estado">
              Los incidentes se abren a partir de una alerta de fatiga.
            </EmptyState>
          )
        ) : (
          <ul className="flex flex-col gap-3" aria-label="Lista de incidentes">
            {incidents.map((incident) => (
              <IncidentCard
                key={incident.id}
                incident={incident}
                currentUser={currentUser}
                onUpdated={replaceIncident}
              />
            ))}
          </ul>
        )}
      </Panel>

      <OpenIncidentForm onOpened={() => load(status)} />
    </div>
  )
}

function sortIncidents(data) {
  const list = Array.isArray(data) ? [...data] : []
  return list.sort((a, b) => new Date(b.openedAt) - new Date(a.openedAt))
}

function OpenIncidentForm({ onOpened }) {
  const [form, setForm] = useState({ alertId: '', operatorId: '' })
  const [success, setSuccess] = useState(null)
  const { run, pending, error, clearError } = useAsyncAction(openIncident)

  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))
  const invalid = (value) => value !== '' && !isGuid(value)

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSuccess(null)
    const created = await run({ alertId: form.alertId.trim(), operatorId: form.operatorId.trim() })
    if (created) {
      setSuccess(`Incidente ${created.id.slice(0, 8)}… abierto.`)
      setForm({ alertId: '', operatorId: '' })
      onOpened()
    }
  }

  return (
    <Panel title="Abrir incidente" description="Registra un incidente a partir de una alerta." className="h-fit">
      <form onSubmit={handleSubmit} className="flex flex-col gap-3">
        <TextField
          label="ID de la alerta"
          value={form.alertId}
          onChange={update('alertId')}
          required
          aria-invalid={invalid(form.alertId) || undefined}
          hint={invalid(form.alertId) ? 'Debe ser un GUID válido.' : undefined}
        />
        <TextField
          label="ID del operador"
          value={form.operatorId}
          onChange={update('operatorId')}
          required
          aria-invalid={invalid(form.operatorId) || undefined}
          hint={invalid(form.operatorId) ? 'Debe ser un GUID válido.' : undefined}
        />
        <ErrorMessage message={error} onDismiss={clearError} />
        <SuccessMessage message={success} />
        <Button type="submit" pending={pending} disabled={invalid(form.alertId) || invalid(form.operatorId)}>
          Abrir incidente
        </Button>
      </form>
    </Panel>
  )
}

function IncidentCard({ incident, currentUser, onUpdated }) {
  const [mode, setMode] = useState(null) // 'assign' | 'action' | 'close'
  const closed = incident.status === 'Closed'
  const actions = incident.actions ?? []

  const escalate = useAsyncAction(escalateIncident)

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
    <li className="rounded-md border border-slate-800 bg-slate-950/40 px-4 py-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <IncidentStatusBadge status={incident.status} />
          <span className="font-mono text-sm text-slate-200" title={incident.id}>
            INC-{incident.id.slice(0, 8).toUpperCase()}
          </span>
          <span className="text-xs text-slate-400">Abierto {formatDateTime(incident.openedAt)}</span>
        </div>
        {!closed && (
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => toggle('assign')} aria-expanded={mode === 'assign'}>
              Asignar
            </Button>
            <Button variant="secondary" onClick={() => toggle('action')} aria-expanded={mode === 'action'}>
              Registrar acción
            </Button>
            <Button
              variant="danger"
              onClick={handleEscalate}
              pending={escalate.pending}
              disabled={incident.status === 'Escalated'}
            >
              Escalar
            </Button>
            <Button variant="ghost" onClick={() => toggle('close')} aria-expanded={mode === 'close'}>
              Cerrar
            </Button>
          </div>
        )}
      </div>

      <dl className="mt-2 grid gap-x-6 gap-y-1 text-xs text-slate-400 sm:grid-cols-2 lg:grid-cols-4">
        <div className="flex gap-1">
          <dt>Alerta:</dt>
          <dd><Guid value={incident.alertId} /></dd>
        </div>
        <div className="flex gap-1">
          <dt>Operador:</dt>
          <dd><Guid value={incident.operatorId} /></dd>
        </div>
        <div className="flex gap-1">
          <dt>Supervisor:</dt>
          <dd><Guid value={incident.assignedSupervisorId} /></dd>
        </div>
        <div className="flex gap-1">
          <dt>{closed ? 'Cerrado:' : 'Asignado:'}</dt>
          <dd>{formatDateTime(closed ? incident.closedAt : incident.assignedAt)}</dd>
        </div>
      </dl>

      {incident.resolution && (
        <p className="mt-2 text-sm text-slate-300">
          <span className="text-slate-400">Resolución: </span>
          {incident.resolution}
        </p>
      )}

      {actions.length > 0 && (
        <details className="mt-2 text-sm">
          <summary className="cursor-pointer text-xs text-slate-400 hover:text-slate-200">
            Acciones registradas ({actions.length})
          </summary>
          <ol className="mt-2 flex flex-col gap-2 border-l border-slate-700 pl-3">
            {actions.map((action, index) => (
              <li key={`${action.registeredAt}-${index}`}>
                <p className="text-slate-200">{action.description}</p>
                <p className="text-xs text-slate-400">
                  Resultado: {action.outcome} · {formatDateTime(action.registeredAt)} · Supervisor{' '}
                  <Guid value={action.supervisorId} />
                </p>
              </li>
            ))}
          </ol>
        </details>
      )}

      <div className="mt-2">
        <ErrorMessage message={escalate.error} onDismiss={escalate.clearError} />
      </div>

      {mode === 'assign' && (
        <AssignForm incidentId={incident.id} defaultSupervisorId={currentUser?.id} onDone={handleDone} />
      )}
      {mode === 'action' && (
        <ActionForm incidentId={incident.id} defaultSupervisorId={currentUser?.id} onDone={handleDone} />
      )}
      {mode === 'close' && <CloseForm incidentId={incident.id} onDone={handleDone} />}
    </li>
  )
}

function InlineForm({ title, onSubmit, pending, error, clearError, submitLabel, disabled, children }) {
  return (
    <form
      onSubmit={onSubmit}
      aria-label={title}
      className="mt-3 flex flex-col gap-3 rounded-md border border-slate-800 bg-slate-900 p-3"
    >
      <p className="text-xs font-semibold uppercase tracking-wide text-amber-300">{title}</p>
      {children}
      <ErrorMessage message={error} onDismiss={clearError} />
      <div>
        <Button type="submit" pending={pending} disabled={disabled}>
          {submitLabel}
        </Button>
      </div>
    </form>
  )
}

function AssignForm({ incidentId, defaultSupervisorId, onDone }) {
  const [supervisorId, setSupervisorId] = useState(defaultSupervisorId ?? '')
  const { run, pending, error, clearError } = useAsyncAction(assignIncident)
  const invalid = supervisorId !== '' && !isGuid(supervisorId)

  const handleSubmit = async (event) => {
    event.preventDefault()
    const updated = await run(incidentId, supervisorId.trim())
    if (updated) onDone(updated)
  }

  return (
    <InlineForm
      title="Asignar supervisor"
      onSubmit={handleSubmit}
      pending={pending}
      error={error}
      clearError={clearError}
      submitLabel="Asignar"
      disabled={invalid}
    >
      <TextField
        label="ID del supervisor"
        value={supervisorId}
        onChange={(event) => setSupervisorId(event.target.value)}
        required
        aria-invalid={invalid || undefined}
        hint={invalid ? 'Debe ser un GUID válido.' : 'Por defecto, el usuario con sesión iniciada.'}
      />
    </InlineForm>
  )
}

function ActionForm({ incidentId, defaultSupervisorId, onDone }) {
  const [form, setForm] = useState({ supervisorId: defaultSupervisorId ?? '', description: '', outcome: '' })
  const { run, pending, error, clearError } = useAsyncAction(registerIncidentAction)
  const invalid = form.supervisorId !== '' && !isGuid(form.supervisorId)
  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    const updated = await run(incidentId, { ...form, supervisorId: form.supervisorId.trim() })
    if (updated) onDone(updated)
  }

  return (
    <InlineForm
      title="Registrar acción correctiva"
      onSubmit={handleSubmit}
      pending={pending}
      error={error}
      clearError={clearError}
      submitLabel="Registrar acción"
      disabled={invalid}
    >
      <TextField
        label="ID del supervisor"
        value={form.supervisorId}
        onChange={update('supervisorId')}
        required
        aria-invalid={invalid || undefined}
        hint={invalid ? 'Debe ser un GUID válido.' : undefined}
      />
      <TextArea
        label="Descripción"
        value={form.description}
        onChange={update('description')}
        placeholder="Ej. Se detuvo el equipo y se relevó al operador."
        required
      />
      <TextField
        label="Resultado"
        value={form.outcome}
        onChange={update('outcome')}
        placeholder="Ej. Operador en descanso"
        required
      />
    </InlineForm>
  )
}

function CloseForm({ incidentId, onDone }) {
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
      pending={pending}
      error={error}
      clearError={clearError}
      submitLabel="Confirmar cierre"
    >
      <TextArea
        label="Resolución"
        value={resolution}
        onChange={(event) => setResolution(event.target.value)}
        required
      />
    </InlineForm>
  )
}
