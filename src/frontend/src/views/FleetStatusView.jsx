import { useMemo, useState } from 'react'
import {
  Button,
  Disclosure,
  EmptyState,
  ErrorMessage,
  LoadingState,
  RiskMark,
  Section,
  SelectField,
  SuccessMessage,
  TextField,
} from '../components/ui.jsx'
import { byRiskThenName } from '../hooks/useFleet.js'
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
  { value: '', label: 'Todos' },
  ...SHIFTS.map((value) => ({ value, label: shiftLabels[value] })),
]
const riskOptions = [
  { value: '', label: 'Todos' },
  ...RISK_LEVELS.map((value) => ({ value, label: riskLevelLabels[value] })),
]

const plural = (count, one, many) => `${count} ${count === 1 ? one : many}`

function headline(operators, loading, error) {
  if (loading) return { text: 'Leyendo el estado de la flota…', tone: 'text-rock' }
  if (error && operators.length === 0) return { text: 'Sin conexión con el monitoreo de flota', tone: 'text-ink' }
  const critical = operators.filter((o) => o.currentRiskLevel === 'Critical').length
  const warning = operators.filter((o) => o.currentRiskLevel === 'Warning').length
  if (critical > 0)
    return { text: `${plural(critical, 'operador', 'operadores')} en riesgo crítico`, tone: 'text-signal' }
  if (warning > 0)
    return { text: `${plural(warning, 'operador', 'operadores')} con signos de fatiga`, tone: 'text-ochre' }
  if (operators.length === 0) return { text: 'Aún no hay operadores en monitoreo', tone: 'text-ink' }
  return { text: 'Flota sin riesgo de fatiga', tone: 'text-ink' }
}

export default function FleetStatusView({ fleet }) {
  const { operators, loading, error, reload } = fleet
  const [filters, setFilters] = useState(emptyFilters)
  const [filtered, setFiltered] = useState(null) // null = no filter applied
  const [filtering, setFiltering] = useState(false)
  const [filterError, setFilterError] = useState(null)

  const fleets = useMemo(() => new Set(operators.map((o) => o.fleet)).size, [operators])
  const warning = operators.filter((o) => o.currentRiskLevel === 'Warning').length
  const { text, tone } = headline(operators, loading, error)

  const rows = filtered ?? operators
  const updateFilter = (field) => (event) => setFilters((prev) => ({ ...prev, [field]: event.target.value }))

  const applyFilters = async (event) => {
    event.preventDefault()
    if (Object.values(filters).every((value) => value === '')) {
      setFiltered(null)
      return
    }
    setFiltering(true)
    setFilterError(null)
    try {
      const data = await getFleetStatus(filters)
      setFiltered(Array.isArray(data) ? [...data].sort(byRiskThenName) : [])
    } catch (err) {
      setFilterError(err.message)
    } finally {
      setFiltering(false)
    }
  }

  const clearFilters = () => {
    setFilters(emptyFilters)
    setFiltered(null)
    setFilterError(null)
  }

  const refresh = async () => {
    await reload()
    setFiltered(null)
    setFilters(emptyFilters)
  }

  return (
    <div className="flex flex-col gap-9">
      <header className="flex flex-col gap-4">
        <h1 className={`stretch-expanded max-w-4xl text-3xl font-bold md:text-4xl ${tone}`}>{text}</h1>
        {!loading && operators.length > 0 && (
          <p className="max-w-prose text-base text-rock">
            {plural(operators.length, 'operador monitoreado', 'operadores monitoreados')} en{' '}
            {plural(fleets, 'flota', 'flotas')}
            {warning > 0 ? `; ${plural(warning, 'requiere', 'requieren')} vigilancia por fatiga moderada.` : '.'}
          </p>
        )}
        {!loading && operators.length > 0 && <FleetRibbon operators={operators} />}
        <ErrorMessage message={error} />
      </header>

      <Section
        title="Operadores en cabina"
        description="Ordenados del mayor al menor riesgo."
        actions={
          <Button variant="secondary" onClick={refresh} pending={loading}>
            Actualizar
          </Button>
        }
      >
        <form
          onSubmit={applyFilters}
          className="mb-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-[repeat(4,minmax(0,1fr))_auto]"
          aria-label="Filtros de la flota"
        >
          <SelectField label="Turno" value={filters.shift} onChange={updateFilter('shift')} options={shiftOptions} />
          <TextField label="Flota" value={filters.fleet} onChange={updateFilter('fleet')} placeholder="Acarreo Norte" />
          <TextField
            label="Ubicación"
            value={filters.location}
            onChange={updateFilter('location')}
            placeholder="Tajo Principal"
          />
          <SelectField
            label="Riesgo"
            value={filters.riskLevel}
            onChange={updateFilter('riskLevel')}
            options={riskOptions}
          />
          <div className="flex items-end gap-1">
            <Button type="submit" pending={filtering}>
              Filtrar
            </Button>
            {filtered && (
              <Button variant="quiet" onClick={clearFilters}>
                Quitar filtros
              </Button>
            )}
          </div>
        </form>

        <ErrorMessage message={filterError} onDismiss={() => setFilterError(null)} />

        {loading ? (
          <LoadingState text="Cargando operadores…" />
        ) : rows.length === 0 ? (
          <EmptyState title={filtered ? 'Ningún operador cumple esos filtros' : 'No hay operadores en monitoreo'}>
            {filtered
              ? 'Pruebe con menos criterios; todos se aplican a la vez.'
              : 'Registre al primer operador con el formulario de abajo.'}
          </EmptyState>
        ) : (
          <OperatorTable rows={rows} />
        )}
      </Section>

      <Disclosure summary="Registrar un operador en la flota">
        <RegisterOperatorForm onRegistered={refresh} />
      </Disclosure>
    </div>
  )
}

/** One segment per operator, worst risk first: the shape of the shift at a glance. */
function FleetRibbon({ operators }) {
  const segment = {
    Critical: 'hazard-tape',
    Warning: 'bg-ochre-bright',
    Normal: 'bg-rule',
  }
  return (
    <figure className="flex flex-col gap-2">
      <div className="flex h-9 gap-[3px]" role="img" aria-label="Franja de riesgo de la flota, un segmento por operador">
        {operators.map((operator) => (
          <span
            key={operator.id}
            title={`${operator.fullName}, ${operator.vehicleCode}: ${riskLevelLabels[operator.currentRiskLevel] ?? ''}`}
            className={`min-w-[6px] flex-1 first:rounded-l last:rounded-r ${segment[operator.currentRiskLevel] ?? 'bg-rule'}`}
          />
        ))}
      </div>
      <figcaption className="text-xs text-rock">Cada franja es un operador en cabina, del mayor al menor riesgo.</figcaption>
    </figure>
  )
}

function OperatorTable({ rows }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[44rem] border-collapse text-left">
        <caption className="sr-only">Operadores monitoreados</caption>
        <thead>
          <tr className="border-b border-ink/70 text-sm text-rock">
            <th scope="col" className="py-2 pl-3 pr-4 font-medium">Operador</th>
            <th scope="col" className="py-2 pr-4 font-medium">Riesgo</th>
            <th scope="col" className="py-2 pr-4 font-medium">Equipo</th>
            <th scope="col" className="py-2 pr-4 font-medium">Flota</th>
            <th scope="col" className="py-2 pr-4 font-medium">Ubicación</th>
            <th scope="col" className="py-2 pr-4 font-medium">Turno</th>
            <th scope="col" className="py-2 font-medium">Última lectura</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((op) => (
            <tr
              key={op.id}
              className={`border-b border-rule ${
                op.currentRiskLevel === 'Critical'
                  ? 'bg-signal-wash/60 shadow-[inset_3px_0_0_theme(colors.signal.DEFAULT)]'
                  : op.currentRiskLevel === 'Warning'
                    ? 'shadow-[inset_3px_0_0_theme(colors.ochre.bright)]'
                    : ''
              }`}
            >
              <td className="py-3 pl-3 pr-4 font-semibold text-ink">{op.fullName}</td>
              <td className="py-3 pr-4">
                <RiskMark level={op.currentRiskLevel} />
              </td>
              <td className="stretch-condensed py-3 pr-4 text-base font-medium">{op.vehicleCode}</td>
              <td className="stretch-condensed py-3 pr-4 text-base">{op.fleet}</td>
              <td className="stretch-condensed py-3 pr-4 text-base">{op.location}</td>
              <td className="stretch-condensed py-3 pr-4 text-base">{label(shiftLabels, op.shift)}</td>
              <td className="stretch-condensed py-3 text-sm text-rock">{formatDateTime(op.lastUpdatedAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
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
      setSuccess(`${created.fullName} quedó registrado en el equipo ${created.vehicleCode}.`)
      setForm(emptyOperator)
      onRegistered()
    }
  }

  const invalidId = form.operatorId !== '' && !isGuid(form.operatorId)

  return (
    <form onSubmit={handleSubmit} className="grid gap-4 md:grid-cols-2">
      <div className="flex items-end gap-2 md:col-span-2">
        <TextField
          label="Identificador del operador"
          className="flex-1"
          value={form.operatorId}
          onChange={update('operatorId')}
          placeholder="El mismo identificador de su cuenta de usuario"
          required
          aria-invalid={invalidId || undefined}
          hint={invalidId ? 'El identificador no tiene el formato correcto.' : undefined}
        />
        <Button variant="secondary" onClick={() => setForm((prev) => ({ ...prev, operatorId: newGuid() }))}>
          Generar uno nuevo
        </Button>
      </div>
      <TextField label="Nombre completo" value={form.fullName} onChange={update('fullName')} required />
      <TextField
        label="Equipo asignado"
        value={form.vehicleCode}
        onChange={update('vehicleCode')}
        placeholder="CAT-797F-12"
        required
      />
      <TextField label="Flota" value={form.fleet} onChange={update('fleet')} placeholder="Acarreo Norte" required />
      <TextField
        label="Ubicación"
        value={form.location}
        onChange={update('location')}
        placeholder="Tajo Principal"
        required
      />
      <SelectField
        label="Turno"
        value={form.shift}
        onChange={update('shift')}
        options={SHIFTS.map((value) => ({ value, label: shiftLabels[value] }))}
      />
      <div className="flex flex-col justify-end gap-2">
        <ErrorMessage message={error} onDismiss={clearError} />
        <SuccessMessage message={success} />
        <Button type="submit" pending={pending} disabled={invalidId}>
          Registrar operador
        </Button>
      </div>
    </form>
  )
}
