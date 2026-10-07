import { useEffect, useMemo, useState } from 'react'
import {
  Button,
  EmptyState,
  ErrorMessage,
  Guid,
  LoadingState,
  Panel,
  RiskBadge,
  SelectField,
  SuccessMessage,
  TextField,
} from '../components/ui.jsx'
import { isGuid, newGuid, useAsyncAction } from '../hooks/useAsyncAction.js'
import { formatDateTime, label, riskLevelLabels, shiftLabels } from '../i18n/labels.js'
import {
  getFleetStatus,
  registerMonitoredOperator,
  RISK_LEVELS,
  SHIFTS,
} from '../services/fleetMonitoringService.js'

const emptyFilters = { shift: '', fleet: '', location: '', riskLevel: '' }

const shiftOptions = [
  { value: '', label: 'Todos los turnos' },
  ...SHIFTS.map((value) => ({ value, label: shiftLabels[value] })),
]
const riskOptions = [
  { value: '', label: 'Todos los niveles' },
  ...RISK_LEVELS.map((value) => ({ value, label: riskLevelLabels[value] })),
]

const riskOrder = { Critical: 0, Warning: 1, Normal: 2 }

const summaryTones = {
  Normal: 'border-emerald-500/30 text-emerald-300',
  Warning: 'border-amber-500/30 text-amber-300',
  Critical: 'border-red-500/40 text-red-300',
}

export default function FleetStatusView() {
  const [filters, setFilters] = useState(emptyFilters)
  const [operators, setOperators] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const load = (activeFilters) => {
    setLoading(true)
    setError(null)
    return getFleetStatus(activeFilters)
      .then((data) => setOperators(Array.isArray(data) ? data : []))
      .catch((err) => {
        setOperators([])
        setError(err.message)
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    let cancelled = false
    getFleetStatus(emptyFilters)
      .then((data) => !cancelled && setOperators(Array.isArray(data) ? data : []))
      .catch((err) => !cancelled && setError(err.message))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  const sorted = useMemo(
    () =>
      [...operators].sort(
        (a, b) =>
          (riskOrder[a.currentRiskLevel] ?? 3) - (riskOrder[b.currentRiskLevel] ?? 3) ||
          String(a.fullName).localeCompare(String(b.fullName)),
      ),
    [operators],
  )

  const counts = useMemo(
    () =>
      RISK_LEVELS.reduce(
        (acc, level) => ({ ...acc, [level]: operators.filter((o) => o.currentRiskLevel === level).length }),
        {},
      ),
    [operators],
  )

  const updateFilter = (field) => (event) => setFilters((prev) => ({ ...prev, [field]: event.target.value }))

  const handleFilter = (event) => {
    event.preventDefault()
    load(filters)
  }

  const handleClear = () => {
    setFilters(emptyFilters)
    load(emptyFilters)
  }

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_22rem]">
      <div className="flex min-w-0 flex-col gap-6">
        <div className="grid grid-cols-3 gap-3" aria-label="Resumen de riesgo de la flota">
          {['Critical', 'Warning', 'Normal'].map((level) => (
            <div key={level} className={`rounded-lg border bg-slate-900/70 px-4 py-3 ${summaryTones[level]}`}>
              <p className="text-xs uppercase tracking-wide text-slate-400">{riskLevelLabels[level]}</p>
              <p className="mt-1 font-mono text-3xl font-semibold">{loading ? '–' : counts[level]}</p>
            </div>
          ))}
        </div>

        <Panel
          title="Estado de la flota"
          description="Operadores monitoreados ordenados por nivel de riesgo."
          actions={
            <Button variant="secondary" onClick={() => load(filters)} pending={loading}>
              Actualizar
            </Button>
          }
        >
          <form
            onSubmit={handleFilter}
            className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-5"
            aria-label="Filtros de la flota"
          >
            <SelectField label="Turno" value={filters.shift} onChange={updateFilter('shift')} options={shiftOptions} />
            <TextField label="Flota" value={filters.fleet} onChange={updateFilter('fleet')} placeholder="Ej. CAT-797" />
            <TextField
              label="Ubicación"
              value={filters.location}
              onChange={updateFilter('location')}
              placeholder="Ej. Tajo Norte"
            />
            <SelectField
              label="Nivel de riesgo"
              value={filters.riskLevel}
              onChange={updateFilter('riskLevel')}
              options={riskOptions}
            />
            <div className="flex items-end gap-2">
              <Button type="submit" className="flex-1">
                Filtrar
              </Button>
              <Button variant="ghost" onClick={handleClear}>
                Limpiar
              </Button>
            </div>
          </form>

          <ErrorMessage message={error} onDismiss={() => setError(null)} />

          {loading ? (
            <LoadingState text="Cargando estado de la flota…" />
          ) : sorted.length === 0 ? (
            !error && (
              <EmptyState title="No hay operadores que coincidan">
                Ajuste los filtros o registre un operador para iniciar el monitoreo.
              </EmptyState>
            )
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[46rem] text-left text-sm">
                <caption className="sr-only">Operadores monitoreados</caption>
                <thead className="border-b border-slate-800 text-xs uppercase tracking-wide text-slate-400">
                  <tr>
                    <th scope="col" className="py-2 pr-3 font-medium">Operador</th>
                    <th scope="col" className="py-2 pr-3 font-medium">Vehículo</th>
                    <th scope="col" className="py-2 pr-3 font-medium">Flota</th>
                    <th scope="col" className="py-2 pr-3 font-medium">Turno</th>
                    <th scope="col" className="py-2 pr-3 font-medium">Ubicación</th>
                    <th scope="col" className="py-2 pr-3 font-medium">Riesgo</th>
                    <th scope="col" className="py-2 font-medium">Actualizado</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/80">
                  {sorted.map((op) => (
                    <tr
                      key={op.id}
                      className={op.currentRiskLevel === 'Critical' ? 'bg-red-500/5' : undefined}
                    >
                      <td className="py-2.5 pr-3">
                        <div className="font-medium text-slate-100">{op.fullName}</div>
                        <Guid value={op.operatorId} />
                      </td>
                      <td className="py-2.5 pr-3 font-mono text-slate-200">{op.vehicleCode}</td>
                      <td className="py-2.5 pr-3 text-slate-300">{op.fleet}</td>
                      <td className="py-2.5 pr-3 text-slate-300">{label(shiftLabels, op.shift)}</td>
                      <td className="py-2.5 pr-3 text-slate-300">{op.location}</td>
                      <td className="py-2.5 pr-3">
                        <RiskBadge level={op.currentRiskLevel} />
                      </td>
                      <td className="py-2.5 text-xs text-slate-400">{formatDateTime(op.lastUpdatedAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Panel>
      </div>

      <RegisterOperatorForm onRegistered={() => load(filters)} />
    </div>
  )
}

const emptyOperator = { operatorId: '', fullName: '', vehicleCode: '', fleet: '', shift: 'Day', location: '' }

function RegisterOperatorForm({ onRegistered }) {
  const [form, setForm] = useState(emptyOperator)
  const [success, setSuccess] = useState(null)
  const { run, pending, error, clearError } = useAsyncAction(registerMonitoredOperator)

  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSuccess(null)
    const created = await run({ ...form, operatorId: form.operatorId.trim() })
    if (created) {
      setSuccess(`Operador ${created.fullName} registrado en ${created.vehicleCode}.`)
      setForm(emptyOperator)
      onRegistered()
    }
  }

  const invalidId = form.operatorId !== '' && !isGuid(form.operatorId)

  return (
    <Panel title="Registrar operador" description="Incorpora un operador al monitoreo de la flota." className="h-fit">
      <form onSubmit={handleSubmit} className="flex flex-col gap-3">
        <div className="flex items-end gap-2">
          <TextField
            label="ID del operador"
            className="flex-1"
            value={form.operatorId}
            onChange={update('operatorId')}
            placeholder="GUID del usuario"
            required
            aria-invalid={invalidId || undefined}
            hint={invalidId ? 'Debe ser un GUID válido.' : undefined}
          />
          <Button variant="secondary" onClick={() => setForm((prev) => ({ ...prev, operatorId: newGuid() }))}>
            Generar
          </Button>
        </div>
        <TextField label="Nombre completo" value={form.fullName} onChange={update('fullName')} required />
        <div className="grid grid-cols-2 gap-3">
          <TextField label="Código de vehículo" value={form.vehicleCode} onChange={update('vehicleCode')} required />
          <TextField label="Flota" value={form.fleet} onChange={update('fleet')} required />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <SelectField
            label="Turno"
            value={form.shift}
            onChange={update('shift')}
            options={SHIFTS.map((value) => ({ value, label: shiftLabels[value] }))}
          />
          <TextField label="Ubicación" value={form.location} onChange={update('location')} required />
        </div>
        <ErrorMessage message={error} onDismiss={clearError} />
        <SuccessMessage message={success} />
        <Button type="submit" pending={pending} disabled={invalidId}>
          Registrar operador
        </Button>
      </form>
    </Panel>
  )
}
