import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  AlertStatusTag,
  Button,
  Disclosure,
  EmptyState,
  ErrorMessage,
  LoadingState,
  RiskMark,
  Section,
  SelectField,
  SuccessMessage,
} from '../components/ui.jsx'
import { acknowledgeAlert, escalateAlert, getAlertsByOperator } from '../services/alertService.js'
import { openIncident } from '../services/incidentManagementService.js'

const timeFormat = new Intl.DateTimeFormat('es-PE', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })
const dayFormat = new Intl.DateTimeFormat('es-PE', { day: 'numeric', month: 'short' })
const severityOrder = { Critical: 0, Warning: 1 }

/** The Alerts service is queried per operator, so the queue is assembled from the whole fleet. */
async function loadFleetAlerts(operators) {
  const results = await Promise.allSettled(operators.map((op) => getAlertsByOperator(op.operatorId)))
  const alerts = results.flatMap((result) => (result.status === 'fulfilled' && Array.isArray(result.value) ? result.value : []))
  const failed = results.filter((result) => result.status === 'rejected')
  return { alerts, error: failed.length ? failed[0].reason?.message : null }
}

export default function AlertsView({ fleet }) {
  const { operators, byOperatorId, loading: fleetLoading } = fleet
  const [alerts, setAlerts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)
  const [busy, setBusy] = useState(null)
  const [operatorFilter, setOperatorFilter] = useState('')

  const refresh = useCallback(async () => {
    setLoading(true)
    setError(null)
    const result = await loadFleetAlerts(operators)
    setAlerts(result.alerts)
    setError(result.error)
    setLoading(false)
  }, [operators])

  useEffect(() => {
    if (fleetLoading) return
    let cancelled = false
    loadFleetAlerts(operators).then((result) => {
      if (cancelled) return
      setAlerts(result.alerts)
      setError(result.error)
      setLoading(false)
    })
    return () => {
      cancelled = true
    }
  }, [fleetLoading, operators])

  const visible = useMemo(
    () => (operatorFilter ? alerts.filter((a) => a.operatorId === operatorFilter) : alerts),
    [alerts, operatorFilter],
  )
  const pending = useMemo(
    () =>
      visible
        .filter((a) => a.status === 'Issued')
        .sort((a, b) => (severityOrder[a.severity] ?? 2) - (severityOrder[b.severity] ?? 2) || new Date(b.issuedAt) - new Date(a.issuedAt)),
    [visible],
  )
  const handled = useMemo(
    () => visible.filter((a) => a.status !== 'Issued').sort((a, b) => new Date(b.issuedAt) - new Date(a.issuedAt)),
    [visible],
  )

  const replaceAlert = (updated) => setAlerts((prev) => prev.map((a) => (a.id === updated.id ? updated : a)))

  const runAction = async (alert, action) => {
    setBusy(`${alert.id}:${action}`)
    setError(null)
    setNotice(null)
    const name = byOperatorId[alert.operatorId]?.fullName ?? 'el operador'
    try {
      if (action === 'acknowledge') {
        replaceAlert(await acknowledgeAlert(alert.id))
        setNotice(`Alerta de ${name} reconocida.`)
      } else if (action === 'escalate') {
        replaceAlert(await escalateAlert(alert.id))
        setNotice(`Alerta de ${name} escalada al jefe de seguridad.`)
      } else {
        await openIncident({ alertId: alert.id, operatorId: alert.operatorId })
        setNotice(`Incidente abierto para ${name}. Lo encontrará en Incidentes.`)
      }
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(null)
    }
  }

  const criticalPending = pending.filter((a) => a.severity === 'Critical').length
  const isLoading = fleetLoading || loading
  const title = isLoading
    ? 'Revisando alertas de la flota…'
    : pending.length === 0
      ? 'Todas las alertas están atendidas'
      : `${pending.length} ${pending.length === 1 ? 'alerta espera' : 'alertas esperan'} respuesta`

  const operatorOptions = [
    { value: '', label: 'Toda la flota' },
    ...operators.map((op) => ({ value: op.operatorId, label: `${op.fullName} (${op.vehicleCode})` })),
  ]

  return (
    <div className="flex flex-col gap-9">
      <header className="flex flex-col gap-3">
        <h1
          className={`stretch-expanded max-w-4xl text-3xl font-bold md:text-4xl ${criticalPending > 0 ? 'text-signal' : 'text-ink'}`}
        >
          {title}
        </h1>
        <p className="max-w-prose text-rock">
          Una alerta sin reconocer por el operador debe escalarse al jefe de seguridad. Abra un incidente cuando
          haga falta detener el equipo o relevar al operador.
        </p>
      </header>

      <div className="flex flex-col gap-2">
        <ErrorMessage message={error} onDismiss={() => setError(null)} />
        <SuccessMessage message={notice} />
      </div>

      <Section
        title="Por atender"
        description="Primero las críticas, luego las más recientes."
        actions={
          <div className="flex items-end gap-2">
            <SelectField
              label="Operador"
              value={operatorFilter}
              onChange={(event) => setOperatorFilter(event.target.value)}
              options={operatorOptions}
              className="w-64"
            />
            <Button variant="secondary" onClick={refresh} pending={loading && !fleetLoading}>
              Actualizar
            </Button>
          </div>
        }
      >
        {isLoading ? (
          <LoadingState text="Consultando alertas de cada operador…" />
        ) : operators.length === 0 ? (
          <EmptyState title="No hay operadores en monitoreo">
            Las alertas aparecen cuando el agente de cabina detecta fatiga en un operador registrado en la flota.
          </EmptyState>
        ) : pending.length === 0 ? (
          <EmptyState title="Nada pendiente">
            Cuando el agente de cabina detecte fatiga, la alerta aparecerá aquí.
          </EmptyState>
        ) : (
          <ul className="flex flex-col" aria-label="Alertas por atender">
            {pending.map((alert) => (
              <AlertRow
                key={alert.id}
                alert={alert}
                operator={byOperatorId[alert.operatorId]}
                busy={busy}
                onAction={runAction}
              />
            ))}
          </ul>
        )}
      </Section>

      {handled.length > 0 && (
        <Disclosure summary={`Alertas atendidas (${handled.length})`}>
          <ul className="flex flex-col">
            {handled.map((alert) => (
              <AlertRow
                key={alert.id}
                alert={alert}
                operator={byOperatorId[alert.operatorId]}
                busy={busy}
                onAction={runAction}
              />
            ))}
          </ul>
        </Disclosure>
      )}
    </div>
  )
}

function AlertRow({ alert, operator, busy, onAction }) {
  const isBusy = busy !== null
  const issued = alert.status === 'Issued'
  return (
    <li className="grid gap-x-6 gap-y-3 border-b border-rule py-4 sm:grid-cols-[5.5rem_1fr_auto] sm:items-center">
      <div>
        <p className="stretch-expanded text-xl font-semibold">{timeFormat.format(new Date(alert.issuedAt))}</p>
        <p className="text-xs text-rock">{dayFormat.format(new Date(alert.issuedAt))}</p>
      </div>
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
          <p className="font-semibold">{operator?.fullName ?? 'Operador no registrado en la flota'}</p>
          <RiskMark level={alert.severity} />
          {!issued && <AlertStatusTag status={alert.status} />}
        </div>
        <p className="stretch-condensed text-sm text-rock">
          {operator ? `${operator.vehicleCode}, ${operator.fleet}, ${operator.location}` : alert.operatorId}
          {alert.acknowledgedAt && `. Reconocida a las ${timeFormat.format(new Date(alert.acknowledgedAt))}`}
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        {issued && (
          <>
            <Button
              variant="secondary"
              onClick={() => onAction(alert, 'acknowledge')}
              pending={busy === `${alert.id}:acknowledge`}
              disabled={isBusy}
            >
              Reconocer
            </Button>
            <Button
              variant="danger"
              onClick={() => onAction(alert, 'escalate')}
              pending={busy === `${alert.id}:escalate`}
              disabled={isBusy}
            >
              Escalar
            </Button>
          </>
        )}
        <Button
          variant="quiet"
          onClick={() => onAction(alert, 'incident')}
          pending={busy === `${alert.id}:incident`}
          disabled={isBusy}
        >
          Abrir incidente
        </Button>
      </div>
    </li>
  )
}
