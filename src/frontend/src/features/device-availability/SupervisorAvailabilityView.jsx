import { useCallback, useEffect, useState } from 'react'
import { getOperatorAvailability } from './deviceAvailabilityApi'

const REFRESH_INTERVAL_MS = 15000

const MONITORING_STATUS = {
  Active: { label: 'Monitoreo activo', tone: 'bg-emerald-100 text-emerald-800 ring-emerald-300' },
  Partial: { label: 'Monitoreo parcial', tone: 'bg-amber-100 text-amber-800 ring-amber-300' },
  Unavailable: { label: 'Sin monitoreo', tone: 'bg-red-100 text-red-800 ring-red-300' },
}

const DEVICE_STATUS = {
  Available: { label: 'Disponible', tone: 'text-emerald-700' },
  Limited: { label: 'Limitado', tone: 'text-amber-700' },
  Interrupted: { label: 'Interrumpido', tone: 'text-red-700' },
}

function formatDate(value) {
  return value ? new Date(value).toLocaleString('es-PE') : '—'
}

function DeviceTable({ devices }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-slate-200 text-xs uppercase text-slate-500">
          <tr>
            <th className="py-2 pr-4">Código</th>
            <th className="py-2 pr-4">Tipo</th>
            <th className="py-2 pr-4">Estado</th>
            <th className="py-2 pr-4">Batería</th>
            <th className="py-2 pr-4">Componentes</th>
            <th className="py-2">Último reporte</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {devices.map((device) => {
            const status = DEVICE_STATUS[device.status]
            return (
              <tr key={device.id}>
                <td className="py-2 pr-4 font-mono">{device.deviceCode}</td>
                <td className="py-2 pr-4">{device.deviceType}</td>
                <td className={`py-2 pr-4 font-medium ${status?.tone ?? 'text-slate-400'}`}>
                  {status?.label ?? 'Sin reporte'}
                </td>
                <td className="py-2 pr-4">{device.batteryLevel != null ? `${device.batteryLevel}%` : '—'}</td>
                <td className="py-2 pr-4">{device.availableComponents.join(', ') || '—'}</td>
                <td className="py-2">{formatDate(device.reportedAt)}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

function OperatorCard({ availability }) {
  const status = MONITORING_STATUS[availability.status]

  return (
    <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase text-slate-500">Operador</p>
          <p className="font-mono text-sm text-slate-900">{availability.operatorId}</p>
        </div>
        <span className={`rounded-full px-3 py-1 text-sm font-semibold ring-1 ${status?.tone ?? ''}`}>
          {status?.label ?? availability.status}
        </span>
      </div>
      <DeviceTable devices={availability.devices} />
    </section>
  )
}

export default function SupervisorAvailabilityView() {
  const [operators, setOperators] = useState([])
  const [error, setError] = useState(null)
  const [lastUpdated, setLastUpdated] = useState(null)

  const load = useCallback(() => {
    getOperatorAvailability()
      .then((availability) => {
        setOperators(availability)
        setError(null)
        setLastUpdated(new Date())
      })
      .catch((loadError) => setError(loadError.message))
  }, [])

  useEffect(() => {
    load()
    const interval = setInterval(load, REFRESH_INTERVAL_MS)
    return () => clearInterval(interval)
  }, [load])

  const interruptedDevices = operators.flatMap((availability) =>
    availability.devices.filter((device) => device.status === 'Interrupted'))

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8 text-slate-800">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Disponibilidad de dispositivos</h1>
          <p className="text-slate-600">Estado del monitoreo de cada operador según sus dispositivos asignados.</p>
        </div>
        <div className="flex items-center gap-3 text-sm text-slate-500">
          {lastUpdated && <span>Actualizado {lastUpdated.toLocaleTimeString('es-PE')}</span>}
          <button type="button" onClick={load} className="font-medium text-slate-700 hover:text-slate-900">
            Actualizar
          </button>
        </div>
      </header>

      {error && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{error}</p>}

      {interruptedDevices.length > 0 && (
        <div role="alert" className="rounded-md border border-red-300 bg-red-50 p-4 text-sm text-red-800">
          <p className="font-semibold">
            {interruptedDevices.length === 1
              ? '1 dispositivo interrumpido requiere atención'
              : `${interruptedDevices.length} dispositivos interrumpidos requieren atención`}
          </p>
          <p>{interruptedDevices.map((device) => device.deviceCode).join(', ')}</p>
        </div>
      )}

      {!error && operators.length === 0 && (
        <p className="text-sm text-slate-500">No hay dispositivos asignados a operadores.</p>
      )}

      {operators.map((availability) => (
        <OperatorCard key={availability.operatorId} availability={availability} />
      ))}
    </main>
  )
}
