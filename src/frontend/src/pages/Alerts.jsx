import { AlertOctagon, BellRing, CheckCheck, CheckCircle2, CircleHelp, Clock, MessageSquare, RefreshCw, Siren, TrendingUp, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { alerts, feedback } from '../api/services'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, PageHeader, Panel, Pill, Segmented, Spinner, Stat } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { ago, dateTime, label, shortId } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const STATUSES = ['Issued', 'Escalated', 'Acknowledged']
const CLASSES = [
  { value: 'CONFIRMED', icon: CheckCircle2, idle: 'border-risk-normal text-risk-normal hover:bg-risk-normalWash', active: 'bg-risk-normal text-white' },
  { value: 'FALSE_ALARM', icon: XCircle, idle: 'border-risk-critical text-risk-critical hover:bg-risk-criticalWash', active: 'bg-risk-critical text-white' },
  { value: 'INCONCLUSIVE', icon: CircleHelp, idle: 'border-basalt-mute text-basalt-soft hover:bg-basalt/5', active: 'bg-basalt text-white' },
]
const SEVERITY_BAR = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning' }

async function loadAlerts() {
  const [Issued, Escalated, Acknowledged] = await Promise.all(STATUSES.map((status) => alerts.byStatus(status)))
  return { Issued, Escalated, Acknowledged }
}

async function loadFeedback(alertId) {
  try {
    return await feedback.get(alertId)
  } catch (error) {
    if (error.status === 404) return { untracked: true }
    throw error
  }
}

function ClassButtons({ value, onChange, disabled }) {
  return (
    <div className="grid grid-cols-3 gap-2">
      {CLASSES.map(({ value: option, icon: Icon, idle, active }) => (
        <button
          key={option}
          type="button"
          aria-pressed={value === option}
          disabled={disabled}
          onClick={() => onChange(option)}
          className={`flex h-16 flex-col items-center justify-center gap-1 rounded-2xl border-2 text-sm font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
            value === option ? `${active} border-transparent` : `bg-white ${idle}`
          }`}
        >
          <Icon className="h-5 w-5" />
          {label(option)}
        </button>
      ))}
    </div>
  )
}

function FeedbackBlock({ alertId }) {
  const { user, operationId } = useAuth()
  const { userName } = useDirectory()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { data, loading, error, reload, setData } = useAsync(() => loadFeedback(alertId), [alertId])
  const [report, setReport] = useState({ classification: '', reason: '' })
  const [review, setReview] = useState({ classification: '', observation: '' })

  const canReport = user?.role === 'Operator' || user?.role === 'Supervisor'
  const canReview = user?.role === 'Supervisor'

  if (loading && !data) return <Spinner />
  if (error) return <Empty icon={MessageSquare} title="No se pudo cargar el feedback." action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Reintentar</Button>} />

  if (data?.untracked) {
    if (!operationId) return <Empty icon={MessageSquare} title="Tu sesión no tiene una unidad minera asignada para habilitar feedback." />
    return (
      <Empty
        icon={MessageSquare}
        title="Esta alerta aún no recibe feedback."
        action={
          <Button
            variant="accent"
            icon={MessageSquare}
            busy={busy === 'track'}
            onClick={async () => {
              const result = await run('track', () => feedback.track(alertId, operationId), 'Feedback habilitado')
              if (result) setData(result)
              else reload()
            }}
          >
            Habilitar feedback
          </Button>
        }
      />
    )
  }

  const alreadyReported = (data?.feedback ?? []).some((entry) => entry.reportedBy === user?.id)
  const needsReason = report.classification === 'FALSE_ALARM'

  const sendReport = async (event) => {
    event.preventDefault()
    const result = await run('report', () => feedback.report(alertId, report.classification, report.reason.trim() || null, user.id), 'Feedback enviado')
    if (result) {
      setData(result)
      setReport({ classification: '', reason: '' })
    }
  }

  const sendReview = async (event) => {
    event.preventDefault()
    const result = await run('review', () => feedback.review(alertId, review.classification, review.observation.trim(), user.id), 'Revisión guardada')
    if (result) {
      setData(result)
      setReview({ classification: '', observation: '' })
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Pill value={data.status} />
        {data.effectiveClassification && <Pill value={data.effectiveClassification} dot={false} />}
      </div>

      {data.feedback?.length > 0 && (
        <ul className="space-y-2">
          {data.feedback.map((entry) => (
            <li key={`${entry.reportedBy}-${entry.reportedAt}`} className="rounded-lg bg-mineral p-3">
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-sm font-semibold">{userName(entry.reportedBy)}</span>
                <Pill value={entry.classification} dot={false} />
              </div>
              {entry.reason && <p className="mt-1 text-sm text-basalt-soft">{entry.reason}</p>}
              <div className="mt-1 text-xs text-basalt-mute">{dateTime(entry.reportedAt)}</div>
            </li>
          ))}
        </ul>
      )}

      {data.reviews?.length > 0 && (
        <ul className="space-y-2">
          {data.reviews.map((item) => (
            <li key={`${item.reviewedBy}-${item.reviewedAt}`} className="rounded-lg border-l-4 border-ore bg-ore-wash p-3">
              <div className="flex items-center justify-between gap-2">
                <span className="flex items-center gap-1.5 truncate text-sm font-semibold"><CheckCheck className="h-4 w-4 text-ore" />{userName(item.reviewedBy)}</span>
                <Pill value={item.classification} dot={false} />
              </div>
              <p className="mt-1 text-sm text-basalt-soft">{item.observation}</p>
              <div className="mt-1 text-xs text-basalt-mute">{dateTime(item.reviewedAt)}</div>
            </li>
          ))}
        </ul>
      )}

      {!data.feedback?.length && !data.reviews?.length && <p className="text-sm text-basalt-mute">Sin reportes todavía.</p>}

      {canReport && !alreadyReported && (
        <form onSubmit={sendReport} className="space-y-3">
          <h4 className="text-sm font-semibold text-basalt-soft">¿Cómo fue esta alerta?</h4>
          <ClassButtons value={report.classification} onChange={(classification) => setReport({ ...report, classification })} disabled={busy === 'report'} />
          {report.classification && (
            <>
              <Field label={needsReason ? 'Motivo' : 'Motivo (opcional)'}>
                <textarea className="field min-h-20" required={needsReason} maxLength={1000} value={report.reason} onChange={(e) => setReport({ ...report, reason: e.target.value })} />
              </Field>
              <Button type="submit" className="w-full" busy={busy === 'report'} disabled={needsReason && !report.reason.trim()}>Enviar feedback</Button>
            </>
          )}
        </form>
      )}

      {canReview && (
        <form onSubmit={sendReview} className="space-y-3 rounded-lg border border-line p-3">
          <h4 className="text-sm font-semibold text-basalt-soft">Revisión del supervisor</h4>
          <ClassButtons value={review.classification} onChange={(classification) => setReview({ ...review, classification })} disabled={busy === 'review'} />
          <Field label="Observación">
            <textarea className="field min-h-20" required maxLength={1000} value={review.observation} onChange={(e) => setReview({ ...review, observation: e.target.value })} />
          </Field>
          <Button type="submit" className="w-full" busy={busy === 'review'} disabled={!review.classification || !review.observation.trim()}>Guardar revisión</Button>
        </form>
      )}
    </div>
  )
}

function AlertRow({ alert, busy, onOpen, onAcknowledge, onEscalate }) {
  const { operatorName } = useDirectory()
  return (
    <li className="flex items-center gap-3 rounded-xl border border-line bg-white p-2 pr-3 hover:border-basalt-mute">
      <button type="button" onClick={onOpen} className="flex min-w-0 flex-1 items-center gap-3 rounded-lg text-left">
        <span className={`h-11 w-1.5 shrink-0 rounded-full ${SEVERITY_BAR[alert.severity] ?? 'bg-line'} ${alert.severity === 'Critical' && alert.status !== 'Acknowledged' ? 'animate-beacon' : ''}`} />
        <div className="min-w-0 flex-1">
          <div className="truncate text-[15px] font-semibold">{operatorName(alert.operatorId)}</div>
          <div className="flex items-center gap-1 text-[13px] text-basalt-mute"><Clock className="h-3.5 w-3.5" />hace {ago(alert.issuedAt)}</div>
        </div>
        <Pill value={alert.status} className="hidden sm:inline-flex" />
      </button>
      {alert.status === 'Issued' && (
        <div className="flex shrink-0 gap-2">
          <Button size="sm" variant="ok" icon={CheckCheck} busy={busy === `ack-${alert.id}`} disabled={busy != null} onClick={onAcknowledge}>Atender</Button>
          <Button size="sm" variant="outline" icon={TrendingUp} busy={busy === `esc-${alert.id}`} disabled={busy != null} onClick={onEscalate}>Escalar</Button>
        </div>
      )}
    </li>
  )
}

export default function Alerts() {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { operatorName } = useDirectory()
  const [params, setParams] = useSearchParams()
  const [tab, setTab] = useState('Issued')
  const { data, loading, reload } = useAsync(loadAlerts, [], { interval: 10000 })

  const selectedId = params.get('id')
  const selected = useMemo(
    () => (selectedId && data ? STATUSES.flatMap((status) => data[status]).find((alert) => alert.id === selectedId) : null),
    [data, selectedId],
  )
  const list = data?.[tab] ?? []
  const critical = data ? [...data.Issued, ...data.Escalated].filter((a) => a.severity === 'Critical').length : null

  const open = (id) => setParams({ id })
  const close = () => setParams({})

  const acknowledge = async (alert) => {
    if (await run(`ack-${alert.id}`, () => alerts.acknowledge(alert.id), 'Alerta atendida')) reload()
  }
  const escalate = async (alert) => {
    if (await run(`esc-${alert.id}`, () => alerts.escalate(alert.id), 'Alerta escalada')) reload()
  }
  const escalateOverdue = async () => {
    const result = await run('overdue', () => alerts.escalateOverdue(), (items) =>
      items?.length ? `${items.length} alertas escaladas` : 'Ninguna alerta vencida')
    if (result) reload()
  }

  return (
    <>
      <PageHeader icon={BellRing} title="Alertas">
        <Button variant="outline" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>
        <Button variant="danger" icon={Siren} onClick={escalateOverdue} busy={busy === 'overdue'}>Escalar vencidas</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={data?.Issued.length} caption="Emitidas sin atender" tone="Warning" icon={BellRing} onClick={() => setTab('Issued')} />
        <Stat value={data?.Escalated.length} caption="Escaladas" tone="Critical" icon={TrendingUp} onClick={() => setTab('Escalated')} />
        <Stat value={critical} caption="Críticas abiertas" tone="Critical" icon={AlertOctagon} />
        <Stat value={data?.Acknowledged.length} caption="Atendidas" tone="Normal" icon={CheckCheck} onClick={() => setTab('Acknowledged')} />
      </div>

      <Panel
        className="mt-5"
        title={
          <Segmented
            value={tab}
            onChange={setTab}
            options={STATUSES.map((status) => ({ value: status, label: `${label(status)}s`, count: data?.[status].length }))}
          />
        }
        bodyClass="p-3"
      >
        {!data && loading ? (
          <Spinner />
        ) : list.length ? (
          <ul className="space-y-2">
            {list.map((alert) => (
              <AlertRow key={alert.id} alert={alert} busy={busy} onOpen={() => open(alert.id)} onAcknowledge={() => acknowledge(alert)} onEscalate={() => escalate(alert)} />
            ))}
          </ul>
        ) : (
          <Empty
            icon={CheckCheck}
            title={tab === 'Acknowledged' ? 'Aún no hay alertas atendidas.' : 'Nada pendiente aquí.'}
            action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Actualizar</Button>}
          />
        )}
      </Panel>

      <Drawer
        open={Boolean(selectedId)}
        title={selected ? operatorName(selected.operatorId) : 'Alerta'}
        onClose={close}
        footer={
          selected?.status === 'Issued' && (
            <>
              <Button variant="outline" icon={TrendingUp} busy={busy === `esc-${selected.id}`} disabled={busy != null} onClick={() => escalate(selected)}>Escalar</Button>
              <Button variant="ok" icon={CheckCheck} busy={busy === `ack-${selected.id}`} disabled={busy != null} onClick={() => acknowledge(selected)}>Atender</Button>
            </>
          )
        }
      >
        {!data ? (
          <Spinner />
        ) : !selected ? (
          <Empty icon={BellRing} title="No encontramos esta alerta." action={<Button variant="outline" onClick={close}>Volver</Button>} />
        ) : (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <Pill value={selected.severity} />
              <Pill value={selected.status} />
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-lg bg-mineral p-3">
                <div className="text-[13px] text-basalt-mute">Emitida</div>
                <div className="mt-1 text-[15px] font-semibold">{dateTime(selected.issuedAt)}</div>
                <div className="text-[13px] text-basalt-mute">hace {ago(selected.issuedAt)}</div>
              </div>
              <div className="rounded-lg bg-mineral p-3">
                <div className="text-[13px] text-basalt-mute">Atendida</div>
                <div className="mt-1 text-[15px] font-semibold">{dateTime(selected.acknowledgedAt)}</div>
                <div className="text-[13px] text-basalt-mute">Evaluación {shortId(selected.assessmentId)}</div>
              </div>
            </div>
            <section className="space-y-3 border-t border-line pt-4">
              <h3 className="flex items-center gap-2 text-sm font-semibold text-basalt-soft"><MessageSquare className="h-4 w-4" />Feedback</h3>
              <FeedbackBlock key={selected.id} alertId={selected.id} />
            </section>
          </>
        )}
      </Drawer>
    </>
  )
}
