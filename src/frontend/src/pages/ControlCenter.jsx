import { BatteryWarning, Bell, BellRing, Gauge, RefreshCw, Siren, Truck, Waves } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { alerts, devices, fatigue, fleet, incidents, monitoring } from '../api/services'
import FleetTile from '../components/FleetTile'
import { Button, Empty, PageHeader, Panel, Pill, Segmented, Spinner, Stat } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { ago, RISK_ORDER } from '../lib/format'
import { useAsync } from '../lib/hooks'

async function loadBoard() {
  const [operators, issued, escalated, pending, deviceList, sessions] = await Promise.all([
    fleet.operators(),
    alerts.byStatus('Issued'),
    alerts.byStatus('Escalated'),
    incidents.list('Pending'),
    devices.list(),
    monitoring.sessions(),
  ])
  const latest = await Promise.all(operators.map((op) => fatigue.latest(op.operatorId).catch(() => null)))
  const assessments = Object.fromEntries(operators.map((op, index) => [op.operatorId, latest[index]]))
  return { operators, issued, escalated, pending, deviceList, sessions, assessments }
}

export default function ControlCenter() {
  const navigate = useNavigate()
  const { operatorName } = useDirectory()
  const { data, loading, reload } = useAsync(loadBoard, [], { interval: 10000 })
  const [filter, setFilter] = useState('all')

  const operators = useMemo(
    () =>
      [...(data?.operators ?? [])]
        .filter((op) => filter === 'all' || op.currentRiskLevel === filter)
        .sort((a, b) => RISK_ORDER[a.currentRiskLevel] - RISK_ORDER[b.currentRiskLevel]),
    [data, filter],
  )

  if (!data && loading) return <Spinner />
  const all = data?.operators ?? []
  const count = (risk) => all.filter((op) => op.currentRiskLevel === risk).length
  const lowBattery = (data?.deviceList ?? []).filter((d) => d.status !== 'Available' || (d.batteryLevel ?? 100) < 20).length
  const live = (data?.sessions ?? []).filter((s) => s.status !== 'Stopped').length
  const queue = [...(data?.escalated ?? []), ...(data?.issued ?? [])].slice(0, 6)

  return (
    <>
      <PageHeader icon={Gauge} title="Centro de control">
        <Button variant="outline" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-5">
        <Stat value={count('Critical')} caption="Cabinas en riesgo crítico" tone="Critical" icon={Truck} onClick={() => setFilter('Critical')} />
        <Stat value={(data?.issued.length ?? 0) + (data?.escalated.length ?? 0)} caption="Alertas sin atender" tone="Warning" icon={BellRing} onClick={() => navigate('/alertas')} />
        <Stat value={data?.pending.length} caption="Incidentes pendientes" tone="Warning" icon={Siren} onClick={() => navigate('/incidentes')} />
        <Stat value={live} caption="Cabinas monitoreadas" tone="info" icon={Waves} onClick={() => navigate('/cabina')} />
        <Stat value={lowBattery} caption="Dispositivos con falla" tone={lowBattery ? 'Critical' : 'Normal'} icon={BatteryWarning} onClick={() => navigate('/dispositivos')} />
      </div>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1fr_340px]">
        <section className="contours rounded-tile border border-line bg-white p-4">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <h2 className="font-cond text-2xl font-semibold">Tablero de flota</h2>
            <Segmented
              value={filter}
              onChange={setFilter}
              options={[
                { value: 'all', label: 'Todas', count: all.length },
                { value: 'Critical', label: 'Crítico', count: count('Critical') },
                { value: 'Warning', label: 'Advertencia', count: count('Warning') },
                { value: 'Normal', label: 'Normal', count: count('Normal') },
              ]}
            />
          </div>
          {operators.length ? (
            <div className="grid gap-3 sm:grid-cols-2 2xl:grid-cols-3">
              {operators.map((op) => (
                <FleetTile key={op.id} operator={op} assessment={data.assessments[op.operatorId]} onClick={() => navigate(`/cabina?operatorId=${op.operatorId}`)} />
              ))}
            </div>
          ) : (
            <Empty icon={Truck} title="Ninguna cabina en este estado." />
          )}
        </section>

        <Panel title="Cola de alertas" icon={Bell} actions={<Link to="/alertas" className="text-sm font-semibold text-ore hover:underline">Ver todas</Link>} bodyClass="p-2">
          {queue.length ? (
            <ul className="space-y-1">
              {queue.map((alert) => (
                <li key={alert.id}>
                  <Link to={`/alertas?id=${alert.id}`} className="flex items-center gap-3 rounded-lg px-2 py-2 hover:bg-mineral">
                    <span className={`h-9 w-1.5 rounded-full ${alert.severity === 'Critical' ? 'bg-risk-critical' : 'bg-risk-warning'}`} />
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-[15px] font-semibold">{operatorName(alert.operatorId)}</div>
                      <div className="text-[13px] text-basalt-mute">hace {ago(alert.issuedAt)}</div>
                    </div>
                    <Pill value={alert.status} />
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <Empty icon={Bell} title="Sin alertas pendientes." />
          )}
        </Panel>
      </div>
    </>
  )
}
