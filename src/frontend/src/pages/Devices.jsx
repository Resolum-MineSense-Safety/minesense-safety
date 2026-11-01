import { BatteryLow, BatteryWarning, Camera, CheckCircle2, ClipboardCheck, Clock, Cpu, Link2, Plus, RefreshCw, Truck, UserRound, Watch } from 'lucide-react'
import { useMemo, useState } from 'react'
import { devices } from '../api/services'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, Meter, PageHeader, Panel, Pill, Select, Spinner, Stat } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { ago, label } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const LOW_BATTERY = 20
const GROUPS = [
  { key: 'camera', title: 'Cámaras', icon: Camera },
  { key: 'wearable', title: 'Wearables', icon: Watch },
  { key: 'other', title: 'Otros', icon: Cpu },
]
const TYPE_ICON = { camera: Camera, wearable: Watch }
// DeviceStatus enum of DeviceManagementService.
const STATUSES = [
  { value: 'Available', label: 'Disponible', dot: 'bg-risk-normal' },
  { value: 'Limited', label: 'Limitado', dot: 'bg-risk-warning' },
  { value: 'Interrupted', label: 'Interrumpido', dot: 'bg-risk-critical' },
]
// Components are free text in the backend; these are suggestions per device type.
const COMPONENTS = {
  camera: ['lente', 'infrarrojo', 'procesador', 'batería'],
  wearable: ['pulso', 'bluetooth', 'correa', 'batería'],
  other: ['sensor', 'batería', 'conectividad'],
}
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

const typeKey = (type) => {
  const key = String(type ?? '').toLowerCase()
  return key === 'camera' || key === 'wearable' ? key : 'other'
}

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
              {option.dot && <span className={`h-2 w-2 rounded-full ${option.dot}`} />}
              {option.label}
            </button>
          )
        })}
      </div>
    </div>
  )
}

function Chip({ children, tone = 'bg-mineral text-basalt-soft' }) {
  return <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-[13px] font-medium ${tone}`}>{children}</span>
}

function DeviceTile({ device, operatorName, vehicle, onClick }) {
  const Icon = TYPE_ICON[typeKey(device.deviceType)] ?? Cpu
  const beacon = { Available: 'bg-risk-normal', Limited: 'bg-risk-warning', Interrupted: 'bg-risk-critical' }[device.status] ?? 'bg-line'
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex w-full items-stretch overflow-hidden rounded-tile border border-line bg-white text-left transition-colors hover:border-basalt-mute"
    >
      <span className={`w-2 shrink-0 ${beacon}`} />
      <div className="flex min-w-0 flex-1 flex-col gap-2 p-3">
        <div className="flex items-center justify-between gap-2">
          <span className="flex min-w-0 items-center gap-2">
            <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-mineral"><Icon className="h-4 w-4" /></span>
            <span className="truncate text-[15px] font-semibold">{device.deviceCode}</span>
          </span>
          {device.status ? <Pill value={device.status} /> : <Pill tone="neutral">Sin reporte</Pill>}
        </div>
        <div className="flex items-center justify-between gap-2">
          <Meter value={device.batteryLevel} threshold={LOW_BATTERY} />
          <span className="flex items-center gap-1 text-[13px] text-basalt-mute"><Clock className="h-3.5 w-3.5" />{ago(device.reportedAt)}</span>
        </div>
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[13px] text-basalt-mute">
          {device.assignedOperatorId && <span className="flex items-center gap-1"><UserRound className="h-3.5 w-3.5" />{operatorName(device.assignedOperatorId)}</span>}
          {device.assignedVehicleId && <span className="flex items-center gap-1"><Truck className="h-3.5 w-3.5" />{vehicle(device.assignedVehicleId)?.code ?? '—'}</span>}
          {!device.assignedOperatorId && !device.assignedVehicleId && <span>Sin asignar</span>}
        </div>
        {device.availableComponents?.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {device.availableComponents.map((component) => <Chip key={component}>{component}</Chip>)}
          </div>
        )}
      </div>
    </button>
  )
}

function DeviceDrawer({ device, onClose, onSaved }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { operators, vehicles, operatorName, vehicle } = useDirectory()
  const [operatorId, setOperatorId] = useState(device.assignedOperatorId ?? '')
  const [vehicleId, setVehicleId] = useState(device.assignedVehicleId ?? '')
  const [status, setStatus] = useState(device.status ?? 'Available')
  const [battery, setBattery] = useState(Math.round(device.batteryLevel ?? 100))
  const [components, setComponents] = useState(device.availableComponents ?? [])
  const [extra, setExtra] = useState('')
  const suggestions = [...new Set([...COMPONENTS[typeKey(device.deviceType)], ...(device.availableComponents ?? []), ...components])]
  const Icon = TYPE_ICON[typeKey(device.deviceType)] ?? Cpu

  const toggle = (component) =>
    setComponents((current) => (current.includes(component) ? current.filter((c) => c !== component) : [...current, component]))

  const assign = async () => {
    const result = await run('assign', () => devices.assign(device.id, { operatorId: operatorId || null, vehicleId: vehicleId || null }), 'Asignación guardada.')
    if (result !== undefined) onSaved(result)
  }

  const report = async () => {
    const body = { status, batteryLevel: battery, reportedAt: new Date().toISOString(), availableComponents: components }
    const result = await run('report', () => devices.reportStatus(device.id, body), 'Estado reportado.')
    if (result !== undefined) onSaved(result)
  }

  const addExtra = () => {
    const value = extra.trim()
    if (value && !components.includes(value)) setComponents([...components, value])
    setExtra('')
  }

  return (
    <Drawer open title={device.deviceCode} onClose={onClose}>
      <div className="flex items-center gap-3 rounded-xl bg-mineral p-3">
        <span className="grid h-11 w-11 place-items-center rounded-xl bg-white"><Icon className="h-5 w-5" /></span>
        <div className="min-w-0 flex-1">
          <div className="text-sm text-basalt-mute">{label(typeKey(device.deviceType) === 'other' ? device.deviceType : typeKey(device.deviceType))}</div>
          <Meter value={device.batteryLevel} threshold={LOW_BATTERY} />
        </div>
        {device.status ? <Pill value={device.status} /> : <Pill tone="neutral">Sin reporte</Pill>}
      </div>

      <section className="space-y-3 rounded-xl border border-line p-4">
        <h3 className="flex items-center gap-2 font-semibold"><Link2 className="h-4 w-4 text-basalt-mute" />Asignación</h3>
        <Field label="Operador">
          <Select value={operatorId} onChange={(event) => setOperatorId(event.target.value)} placeholder="Sin operador"
            options={operators.map((op) => ({ value: op.id, label: op.fullName }))} />
        </Field>
        <Field label="Vehículo">
          <Select value={vehicleId} onChange={(event) => setVehicleId(event.target.value)} placeholder="Sin vehículo"
            options={vehicles.map((v) => ({ value: v.id, label: `${v.code} · ${v.model}` }))} />
        </Field>
        <div className="flex items-center justify-between gap-2">
          <span className="truncate text-[13px] text-basalt-mute">
            {device.assignedOperatorId || device.assignedVehicleId
              ? `Ahora: ${[device.assignedOperatorId && operatorName(device.assignedOperatorId), device.assignedVehicleId && vehicle(device.assignedVehicleId)?.code].filter(Boolean).join(' · ')}`
              : 'Ahora: sin asignar'}
          </span>
          <Button size="sm" icon={Link2} busy={busy === 'assign'} disabled={!operatorId && !vehicleId} onClick={assign}>Asignar</Button>
        </div>
      </section>

      <section className="space-y-4 rounded-xl border border-line p-4">
        <h3 className="flex items-center gap-2 font-semibold"><RefreshCw className="h-4 w-4 text-basalt-mute" />Reportar estado</h3>
        <Choice title="Estado" value={status} onChange={setStatus} options={STATUSES} />
        <div className="space-y-1">
          <div className="flex items-center justify-between">
            <label htmlFor="battery" className="text-sm font-medium text-basalt-soft">Batería</label>
            <span className="numeral text-lg">{battery}%</span>
          </div>
          <input id="battery" type="range" min={0} max={100} value={battery} onChange={(event) => setBattery(Number(event.target.value))} className="w-full accent-basalt" />
        </div>
        <div className="space-y-2">
          <span className="text-sm font-medium text-basalt-soft">Componentes</span>
          <div className="flex flex-wrap gap-2">
            {suggestions.map((component) => {
              const on = components.includes(component)
              return (
                <button
                  key={component}
                  type="button"
                  aria-pressed={on}
                  onClick={() => toggle(component)}
                  className={`inline-flex h-8 items-center gap-1 rounded-full border px-3 text-sm font-semibold ${
                    on ? 'border-risk-normal bg-risk-normalWash text-risk-normal' : 'border-line bg-white text-basalt-mute hover:border-basalt-mute'
                  }`}
                >
                  {on && <CheckCircle2 className="h-3.5 w-3.5" />}
                  {component}
                </button>
              )
            })}
          </div>
          <div className="flex gap-2">
            <Input value={extra} onChange={(event) => setExtra(event.target.value)} placeholder="Otro componente" aria-label="Otro componente"
              onKeyDown={(event) => { if (event.key === 'Enter') { event.preventDefault(); addExtra() } }} />
            <Button variant="outline" icon={Plus} onClick={addExtra} disabled={!extra.trim()}>Añadir</Button>
          </div>
        </div>
        <Button className="w-full" icon={RefreshCw} busy={busy === 'report'} onClick={report}>Reportar ahora</Button>
      </section>
    </Drawer>
  )
}

function RegisterDrawer({ onClose, onSaved }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { units } = useDirectory()
  const [form, setForm] = useState({ deviceCode: '', deviceType: 'camera', organizationId: units[0]?.id ?? '' })
  const valid = form.deviceCode.trim() && GUID.test(form.organizationId.trim())

  const submit = async (event) => {
    event.preventDefault()
    if (!valid) return
    const body = { ...form, deviceCode: form.deviceCode.trim(), organizationId: form.organizationId.trim() }
    const result = await run('register', () => devices.register(body), 'Dispositivo registrado.')
    if (result !== undefined) onSaved()
  }

  return (
    <Drawer
      open
      title="Registrar dispositivo"
      onClose={onClose}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button type="submit" form="device-form" busy={busy === 'register'} disabled={!valid}>Registrar</Button>
        </>
      }
    >
      <form id="device-form" className="space-y-4" onSubmit={submit}>
        <Field label="Código"><Input value={form.deviceCode} onChange={(event) => setForm({ ...form, deviceCode: event.target.value })} placeholder="CAM-001" required /></Field>
        <Choice title="Tipo" value={form.deviceType} onChange={(deviceType) => setForm({ ...form, deviceType })}
          options={[{ value: 'camera', label: 'Cámara', icon: Camera }, { value: 'wearable', label: 'Wearable', icon: Watch }]} />
        {units.length ? (
          <Field label="Organización">
            <Select value={form.organizationId} onChange={(event) => setForm({ ...form, organizationId: event.target.value })}
              options={units.map((unit) => ({ value: unit.id, label: unit.name }))} />
          </Field>
        ) : (
          <Field label="Organización" hint="Identificador Guid de la unidad minera">
            <Input value={form.organizationId} onChange={(event) => setForm({ ...form, organizationId: event.target.value })} required />
          </Field>
        )}
      </form>
    </Drawer>
  )
}

function PreShiftPanel() {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { operators } = useDirectory()
  const [operatorId, setOperatorId] = useState('')
  const [threshold, setThreshold] = useState(LOW_BATTERY)
  const [result, setResult] = useState(null)

  const check = async () => {
    const response = await run('preshift', () => devices.preShift(operatorId, threshold))
    if (response !== undefined) setResult(response)
  }

  const wash = { Available: 'bg-risk-normalWash', Warning: 'bg-risk-warningWash', ProtectionIncomplete: 'bg-risk-criticalWash' }

  return (
    <Panel title="Chequeo pre-turno" icon={ClipboardCheck}>
      <div className="space-y-4">
        <Select value={operatorId} onChange={(event) => { setOperatorId(event.target.value); setResult(null) }} placeholder="Elige un operador"
          options={operators.map((op) => ({ value: op.id, label: op.fullName }))} aria-label="Operador" />
        <div className="space-y-1">
          <div className="flex items-center justify-between">
            <label htmlFor="threshold" className="text-sm font-medium text-basalt-soft">Batería mínima</label>
            <span className="numeral text-lg">{threshold}%</span>
          </div>
          <input id="threshold" type="range" min={0} max={100} value={threshold} onChange={(event) => setThreshold(Number(event.target.value))} className="w-full accent-basalt" />
        </div>
        <Button className="w-full" size="lg" icon={ClipboardCheck} disabled={!operatorId} busy={busy === 'preshift'} onClick={check}>Evaluar</Button>

        {result && (
          <div className={`space-y-3 rounded-xl p-4 ${wash[result.status] ?? 'bg-mineral'}`}>
            <Pill value={result.status} className="px-4 py-1.5 text-base" />
            <div className="font-cond text-2xl font-semibold">{label(result.recommendation)}</div>
            <div className="flex items-center gap-2 text-sm">
              <span className="numeral text-xl">{result.evaluatedDevices?.length ?? 0}</span> dispositivos evaluados
            </div>
            {result.evaluatedDevices?.length > 0 && (
              <ul className="space-y-1">
                {result.evaluatedDevices.map((d) => {
                  const Icon = TYPE_ICON[typeKey(d.deviceType)] ?? Cpu
                  return (
                    <li key={d.id} className="flex items-center gap-2 rounded-lg bg-white/70 px-2 py-1.5">
                      <Icon className="h-4 w-4 text-basalt-mute" />
                      <span className="flex-1 truncate text-sm font-semibold">{d.deviceCode}</span>
                      <Meter value={d.batteryLevel} threshold={threshold} />
                    </li>
                  )
                })}
              </ul>
            )}
            {result.missingDeviceTypes?.length > 0 && (
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="text-sm">Faltan</span>
                {result.missingDeviceTypes.map((type) => <Chip key={type} tone="bg-risk-critical text-white">{label(type)}</Chip>)}
              </div>
            )}
          </div>
        )}
      </div>
    </Panel>
  )
}

export default function Devices() {
  const { operatorName, vehicle } = useDirectory()
  const { data, loading, reload, setData } = useAsync(() => devices.list(), [], { interval: 15000 })
  const [selectedId, setSelectedId] = useState(null)
  const [registering, setRegistering] = useState(false)

  const list = useMemo(() => data ?? [], [data])
  const groups = useMemo(
    () => GROUPS.map((group) => ({ ...group, items: list.filter((d) => typeKey(d.deviceType) === group.key) })).filter((group) => group.items.length),
    [list],
  )
  const selected = list.find((d) => d.id === selectedId)

  const available = list.filter((d) => d.status === 'Available').length
  const failing = list.filter((d) => d.status === 'Limited' || d.status === 'Interrupted').length
  const low = list.filter((d) => d.batteryLevel != null && d.batteryLevel < LOW_BATTERY).length

  const replace = (device) => setData(list.map((d) => (d.id === device.id ? device : d)))

  return (
    <>
      <PageHeader icon={Cpu} title="Dispositivos">
        <Button variant="outline" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>
        <Button icon={Plus} onClick={() => setRegistering(true)}>Registrar dispositivo</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={list.length} caption="Dispositivos" tone="info" icon={Cpu} />
        <Stat value={available} caption="Disponibles" tone="Normal" icon={CheckCircle2} />
        <Stat value={failing} caption="Limitados o interrumpidos" tone={failing ? 'Warning' : 'Normal'} icon={BatteryWarning} />
        <Stat value={low} caption="Batería baja" tone={low ? 'Critical' : 'Normal'} icon={BatteryLow} />
      </div>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1fr_360px]">
        <div className="space-y-5">
          {!data && loading ? (
            <Spinner />
          ) : groups.length ? (
            groups.map((group) => {
              const Icon = group.icon
              return (
                <section key={group.key}>
                  <h2 className="mb-3 flex items-center gap-2 font-cond text-2xl font-semibold">
                    <Icon className="h-5 w-5 text-basalt-mute" />
                    {group.title}
                    <span className="numeral rounded-full bg-basalt/5 px-2 text-sm">{group.items.length}</span>
                  </h2>
                  <div className="grid gap-3 sm:grid-cols-2 2xl:grid-cols-3">
                    {group.items.map((device) => (
                      <DeviceTile key={device.id} device={device} operatorName={operatorName} vehicle={vehicle} onClick={() => setSelectedId(device.id)} />
                    ))}
                  </div>
                </section>
              )
            })
          ) : (
            <Panel>
              <Empty icon={Cpu} title="Aún no hay dispositivos." action={<Button icon={Plus} onClick={() => setRegistering(true)}>Registrar dispositivo</Button>} />
            </Panel>
          )}
        </div>
        <div>
          <PreShiftPanel />
        </div>
      </div>

      {selected && <DeviceDrawer key={selected.id} device={selected} onClose={() => setSelectedId(null)} onSaved={replace} />}
      {registering && (
        <RegisterDrawer
          onClose={() => setRegistering(false)}
          onSaved={() => {
            setRegistering(false)
            reload()
          }}
        />
      )}
    </>
  )
}
