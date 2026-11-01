import {
  CheckCircle2, ChevronDown, Clock, Coffee, Eye, Hand, MoreHorizontal, Phone, RefreshCw, Siren, Timer, TrendingUp, UserCheck, Users,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { incidents, privacy } from '../api/services'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, PageHeader, Panel, Pill, Segmented, Spinner, Stat } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { ago, dateTime, label } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const STATUSES = ['Pending', 'Assigned', 'Closed']
const SEVERITY_BAR = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning' }
const ACTION_TYPES = [
  { value: 'OperatorContact', icon: Phone },
  { value: 'PreventivePause', icon: Coffee },
  { value: 'OperatorRelief', icon: Users },
  { value: 'Other', icon: MoreHorizontal },
]
const EVENT = {
  Opened: { text: 'Abierto', dot: 'bg-risk-warning' },
  Assigned: { text: 'Asignado', dot: 'bg-ore' },
  ActionRecorded: { text: 'Acción registrada', dot: 'bg-basalt' },
  Escalated: { text: 'Escalado', dot: 'bg-risk-critical' },
  Closed: { text: 'Cerrado', dot: 'bg-risk-normal' },
}
const ROLE_LABEL = { ShiftManager: 'Jefe de turno', SafetyManager: 'Jefe de seguridad' }
const ESCALATION_LABEL = { NotApplicable: 'No aplica', NotRequired: 'Sin escalar', Escalated: 'Escalado' }

function duration(seconds) {
  if (seconds == null) return '—'
  if (seconds < 60) return `${Math.round(seconds)} s`
  if (seconds < 3600) return `${Math.round(seconds / 60)} min`
  return `${(seconds / 3600).toFixed(1)} h`
}

async function loadTray() {
  const [Pending, Assigned, Closed] = await Promise.all(STATUSES.map((status) => incidents.list(status)))
  return { Pending, Assigned, Closed }
}

async function loadIncident(id) {
  const [incident, escalation, history, trail] = await Promise.all([
    incidents.get(id),
    incidents.escalation(id).catch(() => null),
    incidents.history(id).catch(() => []),
    privacy.incidentTrail(id).catch(() => null),
  ])
  return { incident, escalation, history, trail }
}

/** Escalation level 0–2 as two small dots. */
function LevelDots({ level = 0 }) {
  return (
    <span className="flex items-center gap-1" title={`Nivel de escalamiento ${level}`} aria-label={`Nivel de escalamiento ${level}`}>
      {[1, 2].map((step) => (
        <span key={step} className={`h-2.5 w-2.5 rounded-full ${level >= step ? 'bg-risk-critical' : 'bg-line'}`} />
      ))}
    </span>
  )
}

function IncidentRow({ incident, onOpen }) {
  const { operatorName, userName } = useDirectory()
  return (
    <li>
      <button type="button" onClick={onOpen} className="flex w-full items-center gap-3 rounded-xl border border-line bg-white p-2 pr-3 text-left hover:border-basalt-mute">
        <span className={`h-11 w-1.5 shrink-0 rounded-full ${SEVERITY_BAR[incident.severity] ?? 'bg-line'}`} />
        <div className="min-w-0 flex-1">
          <div className="truncate text-[15px] font-semibold">{operatorName(incident.operatorId)}</div>
          <div className="flex flex-wrap items-center gap-x-3 text-[13px] text-basalt-mute">
            <span className="flex items-center gap-1"><Clock className="h-3.5 w-3.5" />hace {ago(incident.openedAt)}</span>
            {incident.attentionTimeSeconds != null && <span className="flex items-center gap-1"><Timer className="h-3.5 w-3.5" />{duration(incident.attentionTimeSeconds)}</span>}
          </div>
        </div>
        <LevelDots level={incident.escalationLevel} />
        <span className="hidden min-w-0 max-w-[10rem] items-center gap-1 truncate text-[13px] text-basalt-mute sm:flex">
          <UserCheck className="h-3.5 w-3.5 shrink-0" />
          <span className="truncate">{incident.assigneeId ? userName(incident.assigneeId) : 'Sin asignar'}</span>
        </span>
        <Pill value={incident.status} className="hidden md:inline-flex" />
      </button>
    </li>
  )
}

function IncidentDrawer({ incidentId, onClose, onChanged }) {
  const { user } = useAuth()
  const { operatorName, userName } = useDirectory()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { data, loading, error, reload } = useAsync(() => loadIncident(incidentId), [incidentId])
  const [action, setAction] = useState({ actionType: '', description: '', result: '' })
  const [resolution, setResolution] = useState('')

  const refresh = () => {
    reload()
    onChanged()
  }

  const incident = data?.incident
  const closed = incident?.status === 'Closed'
  const actions = incident?.actions ?? []

  const take = async () => {
    if (await run('assign', () => incidents.assign(incidentId, user.id), 'Incidente asignado a ti')) refresh()
  }

  const addAction = async (event) => {
    event.preventDefault()
    const body = { ...action, description: action.description.trim(), result: action.result.trim(), performedBy: user.id, performedAt: new Date().toISOString() }
    if (await run('action', () => incidents.addAction(incidentId, body), 'Acción registrada')) {
      setAction({ actionType: '', description: '', result: '' })
      refresh()
    }
  }

  const close = async (event) => {
    event.preventDefault()
    if (await run('close', () => incidents.close(incidentId, resolution.trim()), 'Incidente cerrado')) {
      setResolution('')
      refresh()
    }
  }

  const footer = incident && !closed && !incident.assigneeId && (
    <Button variant="accent" icon={Hand} busy={busy === 'assign'} onClick={take}>Tomar incidente</Button>
  )

  return (
    <Drawer open title={incident ? operatorName(incident.operatorId) : 'Incidente'} onClose={onClose} footer={footer}>
      {loading && !data ? (
        <Spinner />
      ) : error || !incident ? (
        <Empty icon={Siren} title="No se pudo cargar el incidente." action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Reintentar</Button>} />
      ) : (
        <>
          <div className="flex flex-wrap items-center gap-2">
            <Pill value={incident.severity} />
            <Pill value={incident.status} />
            {incident.assigneeId === user?.id && <Pill tone="info" value="mine">Asignado a ti</Pill>}
          </div>

          <div className="grid grid-cols-3 gap-2">
            <div className="rounded-lg bg-mineral p-3">
              <div className="text-[13px] text-basalt-mute">Abierto</div>
              <div className="numeral mt-1 text-lg">{ago(incident.openedAt)}</div>
            </div>
            <div className="rounded-lg bg-mineral p-3">
              <div className="text-[13px] text-basalt-mute">Atención</div>
              <div className="numeral mt-1 text-lg">{duration(incident.attentionTimeSeconds)}</div>
            </div>
            <div className="rounded-lg bg-mineral p-3">
              <div className="text-[13px] text-basalt-mute">Acciones</div>
              <div className="numeral mt-1 text-lg">{actions.length}</div>
            </div>
          </div>

          <div className="flex items-center gap-2 rounded-lg border border-line p-3 text-sm">
            <UserCheck className="h-4 w-4 text-basalt-mute" />
            <span className="text-basalt-mute">Responsable</span>
            <span className="ml-auto truncate font-semibold">{incident.assigneeId ? userName(incident.assigneeId) : 'Sin asignar'}</span>
          </div>

          {data.escalation && (
            <section className="space-y-2 rounded-lg border border-line p-3">
              <div className="flex items-center gap-2">
                <TrendingUp className="h-4 w-4 text-basalt-mute" />
                <span className="text-sm font-semibold text-basalt-soft">Escalamiento</span>
                <span className="ml-auto flex items-center gap-2">
                  <LevelDots level={data.escalation.level} />
                  <span className="numeral text-sm">{data.escalation.level}</span>
                </span>
              </div>
              <div className="flex flex-wrap gap-1.5">
                <Pill tone={data.escalation.status === 'Escalated' ? 'Critical' : 'neutral'} value={data.escalation.status}>
                  {ESCALATION_LABEL[data.escalation.status] ?? data.escalation.status}
                </Pill>
                {[...(data.escalation.notifiedRoles ?? [])].map((role) => (
                  <Pill key={role} tone="info" dot={false} value={role}>{ROLE_LABEL[role] ?? label(role)}</Pill>
                ))}
              </div>
            </section>
          )}

          {actions.length > 0 && (
            <section className="space-y-2">
              <h3 className="text-sm font-semibold text-basalt-soft">Acciones registradas</h3>
              <ul className="space-y-2">
                {actions.map((item) => (
                  <li key={item.id} className="rounded-lg bg-mineral p-3">
                    <div className="flex items-center justify-between gap-2">
                      <Pill tone="accent" dot={false} value={item.actionType} />
                      <span className="text-xs text-basalt-mute">{dateTime(item.performedAt)}</span>
                    </div>
                    <p className="mt-1 text-sm">{item.description}</p>
                    <p className="text-sm text-basalt-mute">{item.result}</p>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {!closed && (
            <form onSubmit={addAction} className="space-y-3 rounded-lg border border-line p-3">
              <h3 className="text-sm font-semibold text-basalt-soft">Registrar acción</h3>
              <div className="grid grid-cols-2 gap-2">
                {ACTION_TYPES.map(({ value, icon: Icon }) => (
                  <button
                    key={value}
                    type="button"
                    aria-pressed={action.actionType === value}
                    onClick={() => setAction({ ...action, actionType: value })}
                    className={`flex h-11 items-center justify-center gap-2 rounded-full border text-sm font-semibold transition-colors ${
                      action.actionType === value ? 'border-basalt bg-basalt text-white' : 'border-line bg-white text-basalt hover:border-basalt-mute'
                    }`}
                  >
                    <Icon className="h-4 w-4" />
                    {label(value)}
                  </button>
                ))}
              </div>
              <Field label="Qué se hizo">
                <Input required value={action.description} onChange={(e) => setAction({ ...action, description: e.target.value })} />
              </Field>
              <Field label="Resultado">
                <Input required value={action.result} onChange={(e) => setAction({ ...action, result: e.target.value })} />
              </Field>
              <Button type="submit" className="w-full" busy={busy === 'action'} disabled={!action.actionType || !action.description.trim() || !action.result.trim()}>
                Guardar acción
              </Button>
            </form>
          )}

          {!closed && (
            <form onSubmit={close} className="space-y-3 rounded-lg border border-line p-3">
              <h3 className="text-sm font-semibold text-basalt-soft">Cerrar incidente</h3>
              <Field label="Resolución" hint={actions.length ? undefined : 'Registra al menos una acción antes de cerrar.'}>
                <textarea className="field min-h-20" required value={resolution} onChange={(e) => setResolution(e.target.value)} />
              </Field>
              <Button type="submit" variant="ok" icon={CheckCircle2} className="w-full" busy={busy === 'close'} disabled={!actions.length || !resolution.trim()}>
                Cerrar incidente
              </Button>
            </form>
          )}

          {closed && incident.resolution && (
            <div className="rounded-lg border-l-4 border-risk-normal bg-risk-normalWash p-3 text-sm">{incident.resolution}</div>
          )}

          <section className="space-y-2">
            <h3 className="text-sm font-semibold text-basalt-soft">Historial</h3>
            {data.history.length ? (
              <ol className="relative ml-1.5 space-y-3 border-l-2 border-line pl-4">
                {data.history.map((event, index) => (
                  <li key={`${event.type}-${event.occurredAt}-${index}`} className="relative">
                    <span className={`absolute -left-[23px] top-1 h-3 w-3 rounded-full ring-2 ring-white ${EVENT[event.type]?.dot ?? 'bg-basalt-mute'}`} />
                    <div className="flex flex-wrap items-baseline justify-between gap-x-2">
                      <span className="text-sm font-semibold">{EVENT[event.type]?.text ?? event.type}</span>
                      <span className="text-xs text-basalt-mute">{dateTime(event.occurredAt)}</span>
                    </div>
                    {event.actorId && <div className="text-[13px] text-basalt-mute">{userName(event.actorId)}</div>}
                  </li>
                ))}
              </ol>
            ) : (
              <p className="text-sm text-basalt-mute">Sin eventos.</p>
            )}
          </section>

          <details className="group rounded-lg border border-line">
            <summary className="flex cursor-pointer list-none items-center gap-2 p-3 text-sm font-semibold text-basalt-soft">
              <Eye className="h-4 w-4 text-basalt-mute" />
              Accesos registrados
              <span className="numeral ml-auto rounded-full bg-basalt/5 px-2 text-xs">{data.trail?.length ?? '—'}</span>
              <ChevronDown className="h-4 w-4 transition-transform group-open:rotate-180" />
            </summary>
            <div className="border-t border-line p-3">
              {data.trail?.length ? (
                <ul className="space-y-2">
                  {data.trail.map((entry, index) => (
                    <li key={`${entry.occurredAt}-${index}`} className="flex items-center gap-2 text-sm">
                      <Pill value={entry.kind === 'AccessGranted' ? 'Granted' : 'Denied'} />
                      <span className="min-w-0 flex-1 truncate">{userName(entry.actor)}</span>
                      <span className="text-xs text-basalt-mute">{dateTime(entry.occurredAt)}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="text-sm text-basalt-mute">{data.trail ? 'Sin accesos registrados.' : 'Auditoría no disponible.'}</p>
              )}
            </div>
          </details>
        </>
      )}
    </Drawer>
  )
}

export default function Incidents() {
  const { user } = useAuth()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [tab, setTab] = useState('Pending')
  const [selectedId, setSelectedId] = useState(null)
  const { data, loading, reload } = useAsync(loadTray, [], { interval: 10000 })

  const list = data?.[tab] ?? []
  const open = useMemo(() => (data ? [...data.Pending, ...data.Assigned] : []), [data])
  const escalated = open.filter((incident) => incident.escalationLevel > 0).length
  const attention = useMemo(() => {
    const times = (data?.Closed ?? []).map((incident) => incident.attentionTimeSeconds).filter((value) => value != null)
    return times.length ? duration(times.reduce((sum, value) => sum + value, 0) / times.length) : '—'
  }, [data])

  const openIncident = (incident) => {
    setSelectedId(incident.id)
    privacy
      .logAccess({ userId: user.id, resourceType: 'Incident', resourceId: incident.id, fields: ['status', 'actions'], purpose: 'incident-review', decision: 'Granted' })
      .catch(() => {})
  }

  const escalateOverdue = async () => {
    const result = await run('overdue', () => incidents.escalateOverdue(), (items) =>
      items?.length ? `${items.length} incidentes escalados` : 'Ningún incidente vencido')
    if (result) reload()
  }

  return (
    <>
      <PageHeader icon={Siren} title="Incidentes">
        <Button variant="outline" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>
        <Button variant="danger" icon={TrendingUp} onClick={escalateOverdue} busy={busy === 'overdue'}>Escalar vencidos</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={data?.Pending.length} caption="Pendientes" tone="Warning" icon={Siren} onClick={() => setTab('Pending')} />
        <Stat value={data ? escalated : null} caption="Escalados abiertos" tone="Critical" icon={TrendingUp} />
        <Stat value={data?.Assigned.length} caption="En atención" tone="info" icon={UserCheck} onClick={() => setTab('Assigned')} />
        <Stat value={data ? attention : null} caption="Atención promedio" tone="Normal" icon={Timer} onClick={() => setTab('Closed')} />
      </div>

      <Panel
        className="mt-5"
        title={<Segmented value={tab} onChange={setTab} options={STATUSES.map((status) => ({ value: status, label: `${label(status)}s`, count: data?.[status].length }))} />}
        bodyClass="p-3"
      >
        {!data && loading ? (
          <Spinner />
        ) : list.length ? (
          <ul className="space-y-2">
            {list.map((incident) => (
              <IncidentRow key={incident.id} incident={incident} onOpen={() => openIncident(incident)} />
            ))}
          </ul>
        ) : (
          <Empty
            icon={CheckCircle2}
            title={tab === 'Closed' ? 'Aún no hay incidentes cerrados.' : 'Ningún incidente en esta bandeja.'}
            action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Actualizar</Button>}
          />
        )}
      </Panel>

      {selectedId && <IncidentDrawer key={selectedId} incidentId={selectedId} onClose={() => setSelectedId(null)} onChanged={reload} />}
    </>
  )
}
