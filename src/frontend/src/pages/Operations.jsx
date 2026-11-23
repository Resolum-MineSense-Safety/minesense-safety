import { Building2, CalendarClock, Factory, MapPin, Moon, Plus, Power, Sun, Truck, UserRound, Users, Wrench } from 'lucide-react'
import { useMemo, useState } from 'react'
import { operations } from '../api/services'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, PageHeader, Panel, Pill, Segmented, Select, Spinner, Stat, Table } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { dateTime, label, today } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

// LocationKind enum of OperationsService (Domain/Model/ValueObjects/LocationKind.cs).
const KINDS = [
  { value: 'Pit', label: 'Tajo' },
  { value: 'Dump', label: 'Botadero' },
  { value: 'Crusher', label: 'Chancadora' },
  { value: 'Ramp', label: 'Rampa' },
  { value: 'Workshop', label: 'Taller' },
]
const KIND_LABEL = Object.fromEntries(KINDS.map((kind) => [kind.value, kind.label]))

const NEW_LABEL = {
  units: 'Nueva unidad',
  fleets: 'Nueva flota',
  vehicles: 'Nuevo vehículo',
  operators: 'Nuevo operador',
  assignments: 'Nueva asignación',
}

/** Row of big pill buttons, one selected. */
function Choice({ title, value, onChange, options }) {
  return (
    <div className="space-y-1">
      <span className="text-sm font-medium text-basalt-soft">{title}</span>
      <div role="radiogroup" aria-label={title} className="flex flex-wrap gap-2">
        {options.map((option) => {
          const Icon = option.icon
          const selected = value === option.value
          return (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={selected}
              onClick={() => onChange(option.value)}
              className={`inline-flex h-10 items-center gap-2 rounded-full border px-4 text-[15px] font-semibold transition-colors ${
                selected ? 'border-basalt bg-basalt text-white' : 'border-line bg-white text-basalt-soft hover:border-basalt-mute'
              }`}
            >
              {Icon && <Icon className="h-4 w-4" />}
              {option.label}
            </button>
          )
        })}
      </div>
    </div>
  )
}

/** Drawer with a form whose submit button lives in the footer. */
function FormDrawer({ id, title, onClose, busy, submitLabel = 'Guardar', disabled, onSubmit, children }) {
  return (
    <Drawer
      open
      title={title}
      onClose={onClose}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button type="submit" form={id} busy={busy} disabled={disabled}>{submitLabel}</Button>
        </>
      }
    >
      <form
        id={id}
        className="space-y-4"
        onSubmit={(event) => {
          event.preventDefault()
          if (!disabled) onSubmit()
        }}
      >
        {children}
      </form>
    </Drawer>
  )
}

function UnitDrawer({ onClose, save, busy }) {
  const [form, setForm] = useState({ businessCode: '', name: '', region: '' })
  const set = (key) => (event) => setForm({ ...form, [key]: event.target.value })
  return (
    <FormDrawer id="unit-form" title="Nueva unidad minera" onClose={onClose} busy={busy === 'unit'}
      disabled={!form.businessCode.trim() || !form.name.trim() || !form.region.trim()}
      onSubmit={() => save('unit', () => operations.createUnit(form), 'Unidad minera registrada.')}>
      <Field label="Código"><Input value={form.businessCode} onChange={set('businessCode')} placeholder="UM-01" required /></Field>
      <Field label="Nombre"><Input value={form.name} onChange={set('name')} required /></Field>
      <Field label="Región"><Input value={form.region} onChange={set('region')} required /></Field>
    </FormDrawer>
  )
}

function LocationDrawer({ unit, onClose, save, busy }) {
  const [name, setName] = useState('')
  const [kind, setKind] = useState('Pit')
  return (
    <FormDrawer id="location-form" title={`Ubicación en ${unit.name}`} onClose={onClose} busy={busy === 'location'}
      disabled={!name.trim()}
      onSubmit={() => save('location', () => operations.addLocation(unit.id, { name, kind }), 'Ubicación agregada.')}>
      <Field label="Nombre"><Input value={name} onChange={(event) => setName(event.target.value)} required /></Field>
      <Choice title="Tipo" value={kind} onChange={setKind} options={KINDS} />
    </FormDrawer>
  )
}

function FleetDrawer({ units, onClose, save, busy }) {
  const [form, setForm] = useState({ miningUnitId: units[0]?.id ?? '', name: '' })
  return (
    <FormDrawer id="fleet-form" title="Nueva flota" onClose={onClose} busy={busy === 'fleet'}
      disabled={!form.miningUnitId || !form.name.trim()}
      onSubmit={() => save('fleet', () => operations.createFleet(form), 'Flota registrada.')}>
      <Field label="Unidad minera">
        <Select value={form.miningUnitId} onChange={(event) => setForm({ ...form, miningUnitId: event.target.value })}
          placeholder="Elige una unidad" options={units.map((unit) => ({ value: unit.id, label: unit.name }))} />
      </Field>
      <Field label="Nombre"><Input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required /></Field>
    </FormDrawer>
  )
}

function VehicleDrawer({ fleets, unitName, onClose, save, busy }) {
  const [form, setForm] = useState({ code: '', model: '', fleetId: fleets[0]?.id ?? '' })
  const set = (key) => (event) => setForm({ ...form, [key]: event.target.value })
  return (
    <FormDrawer id="vehicle-form" title="Nuevo vehículo" onClose={onClose} busy={busy === 'vehicle'}
      disabled={!form.code.trim() || !form.model.trim() || !form.fleetId}
      onSubmit={() => save('vehicle', () => operations.createVehicle(form), 'Vehículo registrado.')}>
      <Field label="Código"><Input value={form.code} onChange={set('code')} placeholder="CAT-793-01" required /></Field>
      <Field label="Modelo"><Input value={form.model} onChange={set('model')} required /></Field>
      <Field label="Flota">
        <Select value={form.fleetId} onChange={set('fleetId')} placeholder="Elige una flota"
          options={fleets.map((fleet) => ({ value: fleet.id, label: `${fleet.name} · ${unitName(fleet.miningUnitId)}` }))} />
      </Field>
    </FormDrawer>
  )
}

function OperatorDrawer({ units, onClose, save, busy }) {
  const [form, setForm] = useState({ employeeCode: '', firstName: '', lastName: '', miningUnitId: units[0]?.id ?? '' })
  const set = (key) => (event) => setForm({ ...form, [key]: event.target.value })
  return (
    <FormDrawer id="operator-form" title="Nuevo operador" onClose={onClose} busy={busy === 'operator'}
      disabled={!form.employeeCode.trim() || !form.firstName.trim() || !form.lastName.trim() || !form.miningUnitId}
      onSubmit={() => save('operator', () => operations.createOperator(form), 'Operador registrado.')}>
      <Field label="Código de empleado"><Input value={form.employeeCode} onChange={set('employeeCode')} required /></Field>
      <Field label="Nombres"><Input value={form.firstName} onChange={set('firstName')} required /></Field>
      <Field label="Apellidos"><Input value={form.lastName} onChange={set('lastName')} required /></Field>
      <Field label="Unidad minera">
        <Select value={form.miningUnitId} onChange={set('miningUnitId')} placeholder="Elige una unidad"
          options={units.map((unit) => ({ value: unit.id, label: unit.name }))} />
      </Field>
    </FormDrawer>
  )
}

function AssignDrawer({ operators, vehicles, operatorId, onClose, save, busy }) {
  const [form, setForm] = useState({ operatorId: operatorId ?? '', vehicleId: '', shift: 'Day', validFrom: today(), validTo: '' })
  const set = (key) => (event) => setForm({ ...form, [key]: event.target.value })
  const submit = () =>
    save('assign', () => operations.assign({ ...form, validTo: form.validTo || null }), 'Asignación registrada.')
  return (
    <FormDrawer id="assign-form" title="Nueva asignación" onClose={onClose} busy={busy === 'assign'} submitLabel="Asignar"
      disabled={!form.operatorId || !form.vehicleId || !form.validFrom} onSubmit={submit}>
      <Field label="Operador">
        <Select value={form.operatorId} onChange={set('operatorId')} placeholder="Elige un operador"
          options={operators.map((op) => ({ value: op.id, label: `${op.fullName} · ${op.employeeCode}` }))} />
      </Field>
      <Field label="Vehículo">
        <Select value={form.vehicleId} onChange={set('vehicleId')} placeholder="Elige un vehículo"
          options={vehicles.map((v) => ({ value: v.id, label: `${v.code} · ${v.model}${v.status === 'Active' ? '' : ' (en mantenimiento)'}` }))} />
      </Field>
      <Choice title="Turno" value={form.shift} onChange={(shift) => setForm({ ...form, shift })}
        options={[{ value: 'Day', label: 'Día', icon: Sun }, { value: 'Night', label: 'Noche', icon: Moon }]} />
      <div className="grid grid-cols-2 gap-3">
        <Field label="Desde"><Input type="date" value={form.validFrom} onChange={set('validFrom')} required /></Field>
        <Field label="Hasta" hint="Opcional"><Input type="date" value={form.validTo} min={form.validFrom} onChange={set('validTo')} /></Field>
      </div>
    </FormDrawer>
  )
}

function Plate({ code }) {
  return <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{code}</span>
}

function UnitsTab({ units, fleets, onAddLocation, onNew }) {
  if (!units.length) return <Empty icon={Building2} title="Aún no hay unidades mineras." action={<Button icon={Plus} onClick={onNew}>Nueva unidad</Button>} />
  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
      {units.map((unit) => (
        <article key={unit.id} className="flex flex-col gap-3 rounded-tile border border-line bg-white p-4">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <div className="truncate text-[17px] font-semibold">{unit.name}</div>
              <div className="mt-1 flex items-center gap-1 text-[13px] text-basalt-mute"><MapPin className="h-3.5 w-3.5" />{unit.region}</div>
            </div>
            <Pill tone="accent" dot={false}>{unit.businessCode}</Pill>
          </div>
          <div className="flex flex-wrap gap-1.5">
            {(unit.locations ?? []).map((location) => (
              <span key={`${location.kind}-${location.name}`} className="inline-flex items-center gap-1 rounded-full bg-mineral px-2.5 py-0.5 text-[13px]">
                <span className="font-semibold">{location.name}</span>
                <span className="text-basalt-mute">{KIND_LABEL[location.kind] ?? location.kind}</span>
              </span>
            ))}
            {!unit.locations?.length && <span className="text-[13px] text-basalt-mute">Sin ubicaciones</span>}
          </div>
          <div className="mt-auto flex items-center justify-between gap-2 border-t border-line pt-3">
            <span className="flex items-center gap-1.5 text-sm text-basalt-mute">
              <Truck className="h-4 w-4" />
              <span className="numeral">{fleets.filter((fleet) => fleet.miningUnitId === unit.id).length}</span> flotas
            </span>
            <Button size="sm" variant="outline" icon={Plus} onClick={() => onAddLocation(unit)}>Ubicación</Button>
          </div>
        </article>
      ))}
    </div>
  )
}

function FleetsTab({ units, fleets, vehicles, onNew }) {
  if (!fleets.length) return <Empty icon={Truck} title="Aún no hay flotas." action={<Button icon={Plus} onClick={onNew} disabled={!units.length}>Nueva flota</Button>} />
  return (
    <div className="space-y-4">
      {units.map((unit) => {
        const own = fleets.filter((fleet) => fleet.miningUnitId === unit.id)
        if (!own.length) return null
        return (
          <Panel key={unit.id} title={unit.name} icon={Building2} actions={<Pill tone="accent" dot={false}>{unit.businessCode}</Pill>}>
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              {own.map((fleet) => {
                const list = vehicles.filter((v) => v.fleetId === fleet.id)
                const active = list.filter((v) => v.status === 'Active').length
                return (
                  <div key={fleet.id} className="rounded-xl border border-line p-3">
                    <div className="truncate text-[15px] font-semibold">{fleet.name}</div>
                    <div className="mt-2 flex items-end gap-1">
                      <span className="numeral text-[28px] leading-none">{list.length}</span>
                      <span className="pb-0.5 text-sm text-basalt-mute">vehículos</span>
                    </div>
                    <div className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-line" aria-label={`${active} de ${list.length} activos`}>
                      <span className="bg-risk-normal" style={{ width: list.length ? `${(active / list.length) * 100}%` : 0 }} />
                      <span className="bg-risk-warning" style={{ width: list.length ? `${((list.length - active) / list.length) * 100}%` : 0 }} />
                    </div>
                  </div>
                )
              })}
            </div>
          </Panel>
        )
      })}
    </div>
  )
}

function VehiclesTab({ vehicles, fleets, fleetName, busy, onToggle, onNew }) {
  if (!vehicles.length) return <Empty icon={Truck} title="Aún no hay vehículos." action={<Button icon={Plus} onClick={onNew} disabled={!fleets.length}>Nuevo vehículo</Button>} />
  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
      {vehicles.map((vehicle) => {
        const active = vehicle.status === 'Active'
        return (
          <article key={vehicle.id} className="flex items-stretch overflow-hidden rounded-tile border border-line bg-white">
            <span className={`w-2 shrink-0 ${active ? 'bg-risk-normal' : 'bg-risk-warning'}`} />
            <div className="flex min-w-0 flex-1 flex-col gap-2 p-3">
              <div className="flex items-center justify-between gap-2">
                <Plate code={vehicle.code} />
                <Pill value={vehicle.status} />
              </div>
              <div className="truncate text-[15px] font-semibold">{vehicle.model}</div>
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-[13px] text-basalt-mute">{fleetName(vehicle.fleetId)}</span>
                <Button size="sm" variant="outline" icon={active ? Wrench : Power} busy={busy === `vehicle-${vehicle.id}`} onClick={() => onToggle(vehicle)}>
                  {active ? 'A mantenimiento' : 'Activar'}
                </Button>
              </div>
            </div>
          </article>
        )
      })}
    </div>
  )
}

function OperatorsTab({ operators, units, unitName, busy, onToggle, onNew }) {
  return (
    <Panel bodyClass="p-1">
      <Table
        rows={operators}
        empty={<Empty icon={UserRound} title="Aún no hay operadores." action={<Button icon={Plus} onClick={onNew} disabled={!units.length}>Nuevo operador</Button>} />}
        columns={[
          { key: 'employeeCode', title: 'Código', render: (op) => <Pill tone="neutral" dot={false}>{op.employeeCode}</Pill> },
          { key: 'fullName', title: 'Nombre', render: (op) => <span className="font-semibold">{op.fullName}</span> },
          { key: 'unit', title: 'Unidad', className: 'hidden md:table-cell', render: (op) => unitName(op.miningUnitId) },
          { key: 'status', title: 'Estado', render: (op) => <Pill value={op.status} /> },
          {
            key: 'action',
            title: <span className="sr-only">Acción</span>,
            className: 'text-right',
            render: (op) => (
              <Button size="sm" variant={op.status === 'Active' ? 'outline' : 'ok'} icon={Power} busy={busy === `operator-${op.id}`} onClick={() => onToggle(op)}>
                {op.status === 'Active' ? 'Desactivar' : 'Activar'}
              </Button>
            ),
          },
        ]}
      />
    </Panel>
  )
}

async function loadAssignments(operatorId) {
  if (!operatorId) return null
  const [history, active] = await Promise.all([
    operations.assignments(operatorId),
    operations.activeAssignment(operatorId, today()).catch((error) => {
      if (error.status === 404) return null
      throw error
    }),
  ])
  return { history: [...history].sort((a, b) => String(b.validFrom).localeCompare(String(a.validFrom))), active }
}

function AssignmentsTab({ operators, vehicle, operatorId, setOperatorId, busy, run, onNew, reloadKey }) {
  const { data, loading, reload } = useAsync(() => loadAssignments(operatorId), [operatorId, reloadKey])
  const end = async (assignment) => {
    const result = await run(`end-${assignment.id}`, () => operations.endAssignment(assignment.id, today()), 'Asignación finalizada.')
    if (result !== undefined) reload()
  }
  const plate = (vehicleId) => vehicle(vehicleId)?.code ?? '—'

  if (!operators.length) return <Empty icon={UserRound} title="Primero registra operadores." />
  return (
    <div className="grid gap-5 lg:grid-cols-[320px_1fr]">
      <Panel title="Operador" icon={UserRound}>
        <Select value={operatorId} onChange={(event) => setOperatorId(event.target.value)} placeholder="Elige un operador"
          options={operators.map((op) => ({ value: op.id, label: op.fullName }))} aria-label="Operador" />
        {data?.active ? (
          <div className="mt-4 rounded-xl bg-risk-normalWash p-4">
            <div className="flex items-center justify-between">
              <span className="text-sm font-semibold text-risk-normal">Hoy</span>
              {data.active.shift === 'Night' ? <Moon className="h-5 w-5 text-risk-normal" /> : <Sun className="h-5 w-5 text-risk-normal" />}
            </div>
            <div className="mt-2"><Plate code={plate(data.active.vehicleId)} /></div>
            <div className="mt-2 text-sm">{label(data.active.shift)} · desde {data.active.validFrom}</div>
          </div>
        ) : (
          operatorId && !loading && (
            <div className="mt-4 rounded-xl bg-mineral p-4 text-sm text-basalt-mute">Sin asignación hoy</div>
          )
        )}
        <Button className="mt-4 w-full" icon={Plus} onClick={onNew}>Nueva asignación</Button>
      </Panel>

      <Panel title="Historial" icon={CalendarClock} bodyClass="p-2">
        {!operatorId ? (
          <Empty icon={CalendarClock} title="Elige un operador para ver sus asignaciones." />
        ) : loading && !data ? (
          <Spinner />
        ) : data?.history.length ? (
          <ul className="space-y-1">
            {data.history.map((assignment) => (
              <li key={assignment.id} className="flex flex-wrap items-center gap-3 rounded-lg px-2 py-2 hover:bg-mineral">
                <span className={`h-9 w-1.5 rounded-full ${assignment.status === 'Active' ? 'bg-risk-normal' : 'bg-line'}`} />
                <Plate code={plate(assignment.vehicleId)} />
                <span className="flex items-center gap-1 text-sm">
                  {assignment.shift === 'Night' ? <Moon className="h-4 w-4 text-basalt-mute" /> : <Sun className="h-4 w-4 text-basalt-mute" />}
                  {label(assignment.shift)}
                </span>
                <span className="numeral text-sm text-basalt-mute">{assignment.validFrom} – {assignment.validTo ?? '…'}</span>
                <span className="ml-auto flex items-center gap-2">
                  <Pill value={assignment.status} />
                  {assignment.status === 'Active' && (
                    <Button size="sm" variant="outline" busy={busy === `end-${assignment.id}`} onClick={() => end(assignment)}>Finalizar</Button>
                  )}
                </span>
                <span className="sr-only">Creada {dateTime(assignment.createdAt)}</span>
              </li>
            ))}
          </ul>
        ) : (
          <Empty icon={CalendarClock} title="Sin asignaciones." action={<Button icon={Plus} onClick={onNew}>Nueva asignación</Button>} />
        )}
      </Panel>
    </div>
  )
}

export default function Operations() {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { units, fleets, vehicles, operators, reload, unitName, fleetName, vehicle } = useDirectory()
  const [tab, setTab] = useState('units')
  const [drawer, setDrawer] = useState(null)
  const [locationUnit, setLocationUnit] = useState(null)
  const [operatorId, setOperatorId] = useState('')
  const [assignVersion, setAssignVersion] = useState(0)

  const activeVehicles = useMemo(() => vehicles.filter((v) => v.status === 'Active').length, [vehicles])
  const activeOperators = useMemo(() => operators.filter((op) => op.status === 'Active').length, [operators])

  const close = () => {
    setDrawer(null)
    setLocationUnit(null)
  }

  const save = async (key, fn, message) => {
    const result = await run(key, fn, message)
    if (result === undefined) return
    await reload()
    if (key === 'assign') setAssignVersion((version) => version + 1)
    close()
  }

  const toggleVehicle = async (v) => {
    const status = v.status === 'Active' ? 'InMaintenance' : 'Active'
    const result = await run(`vehicle-${v.id}`, () => operations.updateVehicle(v.id, { model: v.model, status, fleetId: v.fleetId }), `${v.code}: ${label(status).toLowerCase()}.`)
    if (result !== undefined) reload()
  }

  const toggleOperator = async (op) => {
    const status = op.status === 'Active' ? 'Inactive' : 'Active'
    const body = { firstName: op.firstName, lastName: op.lastName, miningUnitId: op.miningUnitId, status }
    const result = await run(`operator-${op.id}`, () => operations.updateOperator(op.id, body), `${op.fullName}: ${label(status).toLowerCase()}.`)
    if (result !== undefined) reload()
  }

  const openNew = (kind = tab) => setDrawer(kind)
  const needsParent = (tab === 'fleets' || tab === 'operators') && !units.length ? true : tab === 'vehicles' && !fleets.length

  return (
    <>
      <PageHeader icon={Factory} title="Operaciones">
        <Button icon={Plus} onClick={() => openNew()} disabled={needsParent}>{NEW_LABEL[tab]}</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={units.length} caption="Unidades mineras" tone="accent" icon={Building2} onClick={() => setTab('units')} />
        <Stat value={fleets.length} caption="Flotas" tone="info" icon={Truck} onClick={() => setTab('fleets')} />
        <Stat value={`${activeVehicles}/${vehicles.length}`} caption="Vehículos activos" tone={activeVehicles < vehicles.length ? 'Warning' : 'Normal'} icon={Wrench} onClick={() => setTab('vehicles')} />
        <Stat value={`${activeOperators}/${operators.length}`} caption="Operadores activos" tone="Normal" icon={Users} onClick={() => setTab('operators')} />
      </div>

      <div className="my-5">
        <Segmented
          value={tab}
          onChange={setTab}
          options={[
            { value: 'units', label: 'Unidades mineras', count: units.length },
            { value: 'fleets', label: 'Flotas', count: fleets.length },
            { value: 'vehicles', label: 'Vehículos', count: vehicles.length },
            { value: 'operators', label: 'Operadores', count: operators.length },
            { value: 'assignments', label: 'Asignaciones' },
          ]}
        />
      </div>

      {tab === 'units' && (
        <UnitsTab units={units} fleets={fleets} onNew={() => openNew('units')} onAddLocation={(unit) => { setLocationUnit(unit); setDrawer('location') }} />
      )}
      {tab === 'fleets' && <FleetsTab units={units} fleets={fleets} vehicles={vehicles} onNew={() => openNew('fleets')} />}
      {tab === 'vehicles' && (
        <VehiclesTab vehicles={vehicles} fleets={fleets} fleetName={fleetName} busy={busy} onToggle={toggleVehicle} onNew={() => openNew('vehicles')} />
      )}
      {tab === 'operators' && (
        <OperatorsTab operators={operators} units={units} unitName={unitName} busy={busy} onToggle={toggleOperator} onNew={() => openNew('operators')} />
      )}
      {tab === 'assignments' && (
        <AssignmentsTab operators={operators} vehicle={vehicle} operatorId={operatorId} setOperatorId={setOperatorId}
          busy={busy} run={run} onNew={() => openNew('assignments')} reloadKey={assignVersion} />
      )}

      {drawer === 'units' && <UnitDrawer onClose={close} save={save} busy={busy} />}
      {drawer === 'location' && locationUnit && <LocationDrawer unit={locationUnit} onClose={close} save={save} busy={busy} />}
      {drawer === 'fleets' && <FleetDrawer units={units} onClose={close} save={save} busy={busy} />}
      {drawer === 'vehicles' && <VehicleDrawer fleets={fleets} unitName={unitName} onClose={close} save={save} busy={busy} />}
      {drawer === 'operators' && <OperatorDrawer units={units} onClose={close} save={save} busy={busy} />}
      {drawer === 'assignments' && (
        <AssignDrawer operators={operators} vehicles={vehicles} operatorId={operatorId} onClose={close} save={save} busy={busy} />
      )}
    </>
  )
}
