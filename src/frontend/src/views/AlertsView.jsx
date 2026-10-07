import { useState } from 'react'
import {
  AlertStatusBadge,
  Button,
  EmptyState,
  ErrorMessage,
  Guid,
  LoadingState,
  Panel,
  RiskBadge,
  SuccessMessage,
  TextField,
} from '../components/ui.jsx'
import { isGuid } from '../hooks/useAsyncAction.js'
import { formatDateTime } from '../i18n/labels.js'
import { acknowledgeAlert, escalateAlert, getAlertsByOperator } from '../services/alertService.js'
import { openIncident } from '../services/incidentManagementService.js'

export default function AlertsView() {
  const [operatorId, setOperatorId] = useState('')
  const [searchedId, setSearchedId] = useState(null)
  const [alerts, setAlerts] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)
  const [busy, setBusy] = useState(null) // `${alertId}:${action}` currently running

  const search = async (id) => {
    setLoading(true)
    setError(null)
    setNotice(null)
    try {
      const data = await getAlertsByOperator(id)
      const list = Array.isArray(data) ? data : []
      list.sort((a, b) => new Date(b.issuedAt) - new Date(a.issuedAt))
      setAlerts(list)
      setSearchedId(id)
    } catch (err) {
      setAlerts([])
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    search(operatorId.trim())
  }

  const replaceAlert = (updated) =>
    setAlerts((prev) => prev.map((alert) => (alert.id === updated.id ? updated : alert)))

  const runAction = async (alert, action) => {
    setBusy(`${alert.id}:${action}`)
    setError(null)
    setNotice(null)
    try {
      if (action === 'acknowledge') {
        replaceAlert(await acknowledgeAlert(alert.id))
        setNotice('Alerta reconocida.')
      } else if (action === 'escalate') {
        replaceAlert(await escalateAlert(alert.id))
        setNotice('Alerta escalada al jefe de seguridad.')
      } else if (action === 'incident') {
        const incident = await openIncident({ alertId: alert.id, operatorId: alert.operatorId })
        setNotice(`Incidente ${incident.id.slice(0, 8)}… abierto. Gestiónelo en la sección Incidentes.`)
      }
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(null)
    }
  }

  const invalidId = operatorId !== '' && !isGuid(operatorId)

  return (
    <Panel title="Alertas por operador" description="Consulte, reconozca o escale las alertas de fatiga emitidas.">
      <form onSubmit={handleSubmit} className="mb-4 flex flex-wrap items-end gap-3" aria-label="Buscar alertas">
        <TextField
          label="ID del operador"
          className="min-w-[18rem] flex-1"
          value={operatorId}
          onChange={(event) => setOperatorId(event.target.value)}
          placeholder="00000000-0000-0000-0000-000000000000"
          required
          aria-invalid={invalidId || undefined}
          hint={invalidId ? 'Debe ser un GUID válido.' : undefined}
        />
        <Button type="submit" pending={loading} disabled={invalidId || operatorId === ''}>
          Buscar alertas
        </Button>
      </form>

      <div className="mb-4 flex flex-col gap-2">
        <ErrorMessage message={error} onDismiss={() => setError(null)} />
        <SuccessMessage message={notice} />
      </div>

      {loading ? (
        <LoadingState text="Cargando alertas…" />
      ) : searchedId === null ? (
        <EmptyState title="Ingrese el ID de un operador">
          Las alertas se listan de la más reciente a la más antigua.
        </EmptyState>
      ) : alerts.length === 0 ? (
        <EmptyState title="Sin alertas registradas">El operador no tiene alertas de fatiga.</EmptyState>
      ) : (
        <ul className="flex flex-col gap-3" aria-label="Alertas del operador">
          {alerts.map((alert) => (
            <li
              key={alert.id}
              className={`rounded-md border px-4 py-3 ${
                alert.severity === 'Critical' && alert.status !== 'Acknowledged'
                  ? 'border-red-500/50 bg-red-500/5'
                  : 'border-slate-800 bg-slate-950/40'
              }`}
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-wrap items-center gap-2">
                  <RiskBadge level={alert.severity} />
                  <AlertStatusBadge status={alert.status} />
                  <span className="text-xs text-slate-400">Emitida {formatDateTime(alert.issuedAt)}</span>
                  {alert.acknowledgedAt && (
                    <span className="text-xs text-slate-400">
                      · Reconocida {formatDateTime(alert.acknowledgedAt)}
                    </span>
                  )}
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button
                    variant="secondary"
                    onClick={() => runAction(alert, 'acknowledge')}
                    pending={busy === `${alert.id}:acknowledge`}
                    disabled={busy !== null || alert.status !== 'Issued'}
                  >
                    Reconocer
                  </Button>
                  <Button
                    variant="danger"
                    onClick={() => runAction(alert, 'escalate')}
                    pending={busy === `${alert.id}:escalate`}
                    disabled={busy !== null || alert.status === 'Escalated'}
                  >
                    Escalar
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={() => runAction(alert, 'incident')}
                    pending={busy === `${alert.id}:incident`}
                    disabled={busy !== null}
                  >
                    Abrir incidente
                  </Button>
                </div>
              </div>
              <dl className="mt-2 flex flex-wrap gap-x-6 gap-y-1 text-xs text-slate-400">
                <div className="flex gap-1">
                  <dt>Alerta:</dt>
                  <dd><Guid value={alert.id} /></dd>
                </div>
                <div className="flex gap-1">
                  <dt>Evaluación:</dt>
                  <dd><Guid value={alert.assessmentId} /></dd>
                </div>
              </dl>
            </li>
          ))}
        </ul>
      )}
    </Panel>
  )
}
