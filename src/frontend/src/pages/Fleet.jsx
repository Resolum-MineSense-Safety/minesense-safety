import { AlertTriangle, ArrowLeftRight, Clock, MapPin, MonitorPlay, Plus, RefreshCw, ShieldCheck, Truck, WifiOff } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fatigue, fleet } from '../api/services'
import FleetTile from '../components/FleetTile'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, PageHeader, Pill, Segmented, Select, Spinner, Stat } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { ago, dateTime, label, pct, RISK_ORDER } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const RISKS = ['Normal', 'Warning', 'Critical']
const BAR = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal' }
const RISK_BUTTON = {
  Normal: 'border-risk-normal text-risk-normal hover:bg-risk-normalWash',
  Warning: 'border-risk-warning text-[#A15F00] hover:bg-risk-warningWash',
  Critical: 'border-risk-critical text-risk-critical hover:bg-risk-criticalWash',
}
const RISK_ACTIVE = { Normal: 'bg-risk-normal text-white', Warning: 'bg-risk-warning text-white', Critical: 'bg-risk-critical text-white' }

async function loadFleet(siteId, fleetId) {
  const operators = siteId || fleetId ? await fleet.filtered(siteId || undefined, fleetId || undefined) : await fleet.operators()
  const latest = await Promise.all(operators.map((op) => fatigue.latest(op.operatorId).catch(() => null)))
  return { operators, assessments: Object.fromEntries(operators.map((op, index) => [op.operatorId, latest[index]])) }
}

/** Small vertical bars: one per assessment, height = PERCLOS, color = risk. */
function AssessmentBars({ items }) {
  if (!items.length) return <Empty icon={ShieldCheck} title="Sin evaluaciones de fatiga." />
  return (
    <div className="flex h-24 items-end gap-1 rounded-lg bg-mineral p-2">
      {items.map((item) => (
        <div
          key={item.id}
          title={`${dateTime(item.assessedAt)} · ${pct(item.perclos)} · ${label(item.riskLevel)}`}
          className={`flex-1 rounded-t ${BAR[item.riskLevel] ?? 'bg-line'}`}
          style={{ height: `${Math.max(Math.min(item.perclos, 1) * 100, 6)}%` }}
        />
      ))}
    </div>
  )
}

function OperatorDrawer({ operator, assessment, onClose, onChanged }) {
  const navigate = useNavigate()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { vehicles } = useDirectory()
  const history = useAsync(() => fatigue.byOperator(operator.operatorId), [operator.operatorId])
  const [moving, setMoving] = useState(false)
  const [move, setMove] = useState({ vehicleCode: operator.vehicleCode, location: operator.location })

  const bars = useMemo(
    () => [...(history.data ?? [])].sort((a, b) => new Date(a.assessedAt) - new Date(b.assessedAt)).slice(-16),
    [history.data],
  )

  const setRisk = async (risk) => {
    const result = await run(`risk-${risk}`, () => fleet.setRisk(operator.operatorId, risk), `Riesgo: ${label(risk)}`)
    if (result) onChanged()
  }

  const submitMove = async (event) => {
    event.preventDefault()
    const result = await run('move', () => fleet.move(operator.operatorId, move.vehicleCode, move.location), 'Cabina actualizada')
    if (result) {
      setMoving(false)
      onChanged()
    }
  }

  const vehicleOptions = vehicles.map((v) => ({ value: v.code, label: `${v.code} · ${v.model}` }))
  if (move.vehicleCode && !vehicles.some((v) => v.code === move.vehicleCode))
    vehicleOptions.unshift({ value: move.vehicleCode, label: move.vehicleCode })

  return (
    <Drawer
      open
      title={operator.fullName}
      onClose={onClose}
      footer={
        <Button variant="accent" icon={MonitorPlay} onClick={() => navigate(`/cabina?operatorId=${operator.operatorId}`)}>
          Abrir cabina
        </Button>
      }
    >
      <div className="flex flex-wrap items-center gap-2">
        <Pill value={operator.currentRiskLevel} />
        <Pill value={operator.isStale ? 'Stale' : 'Current'} tone={operator.isStale ? 'Warning' : 'Normal'}>
          {operator.isStale ? 'Dato antiguo' : 'Dato al día'}
        </Pill>
        <Pill value={operator.shift} tone="neutral" dot={false} />
      </div>

      <div className="grid grid-cols-2 gap-2">
        <div className="rounded-lg bg-mineral p-3">
          <div className="flex items-center gap-1.5 text-[13px] text-basalt-mute"><Truck className="h-3.5 w-3.5" />Vehículo</div>
          <div className="mt-1 font-cond text-xl font-semibold">{operator.vehicleCode}</div>
          <div className="truncate text-[13px] text-basalt-mute">{operator.fleet}</div>
        </div>
        <div className="rounded-lg bg-mineral p-3">
          <div className="flex items-center gap-1.5 text-[13px] text-basalt-mute"><MapPin className="h-3.5 w-3.5" />Ubicación</div>
          <div className="mt-1 truncate text-[15px] font-semibold">{operator.location}</div>
          <div className="flex items-center gap-1 text-[13px] text-basalt-mute"><Clock className="h-3.5 w-3.5" />hace {ago(operator.lastUpdatedAt)}</div>
        </div>
      </div>

      <section className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-basalt-soft">Últimas evaluaciones</h3>
          {assessment && <span className="numeral text-sm text-basalt-mute">PERCLOS {pct(assessment.perclos)}</span>}
        </div>
        {history.loading && !history.data ? <Spinner /> : <AssessmentBars items={bars} />}
      </section>

      <section className="space-y-2">
        <h3 className="text-sm font-semibold text-basalt-soft">Fijar riesgo</h3>
        <div className="grid grid-cols-3 gap-2">
          {RISKS.map((risk) => {
            const active = operator.currentRiskLevel === risk
            return (
              <button
                key={risk}
                type="button"
                aria-pressed={active}
                disabled={busy != null || active}
                onClick={() => setRisk(risk)}
                className={`h-11 rounded-full border-2 text-[15px] font-semibold transition-colors disabled:cursor-not-allowed ${
                  active ? `${RISK_ACTIVE[risk]} border-transparent` : `bg-white ${RISK_BUTTON[risk]}`
                } ${busy === `risk-${risk}` ? 'opacity-60' : ''}`}
              >
                {label(risk)}
              </button>
            )
          })}
        </div>
      </section>

      <section className="space-y-2">
        {moving ? (
          <form onSubmit={submitMove} className="space-y-3 rounded-lg border border-line p-3">
            <Field label="Vehículo">
              <Select required options={vehicleOptions} placeholder="Elige un vehículo" value={move.vehicleCode} onChange={(e) => setMove({ ...move, vehicleCode: e.target.value })} />
            </Field>
            <Field label="Ubicación">
              <Input required value={move.location} onChange={(e) => setMove({ ...move, location: e.target.value })} />
            </Field>
            <div className="flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setMoving(false)}>Cancelar</Button>
              <Button type="submit" busy={busy === 'move'}>Guardar</Button>
            </div>
          </form>
        ) : (
          <Button variant="outline" icon={ArrowLeftRight} className="w-full" onClick={() => setMoving(true)}>
            Mover vehículo o ubicación
          </Button>
        )}
      </section>
    </Drawer>
  )
}

const EMPTY_FORM = { operatorId: '', vehicleCode: '', shift: 'Day', location: '' }

function RegisterDrawer({ onClose, onDone }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { operators, vehicles, units, fleetName } = useDirectory()
  const [form, setForm] = useState(EMPTY_FORM)

  const operator = operators.find((op) => op.id === form.operatorId)
  const vehicle = vehicles.find((v) => v.code === form.vehicleCode)
  const unit = units.find((u) => u.id === operator?.miningUnitId)
  const locations = unit?.locations ?? []

  const submit = async (event) => {
    event.preventDefault()
    const body = {
      operatorId: form.operatorId,
      fullName: operator?.fullName ?? '',
      vehicleCode: form.vehicleCode,
      fleet: vehicle ? fleetName(vehicle.fleetId) : '',
      shift: form.shift,
      location: form.location,
      miningUnitId: operator?.miningUnitId ?? null,
      fleetId: vehicle?.fleetId ?? null,
    }
    const result = await run('register', () => fleet.register(body), 'Cabina registrada')
    if (result) onDone()
  }

  return (
    <Drawer open title="Registrar cabina" onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <Field label="Operador">
          <Select
            required
            placeholder="Elige un operador"
            options={operators.map((op) => ({ value: op.id, label: `${op.fullName} · ${op.employeeCode}` }))}
            value={form.operatorId}
            onChange={(e) => setForm({ ...form, operatorId: e.target.value, location: '' })}
          />
        </Field>
        <Field label="Vehículo" hint={vehicle ? `Flota ${fleetName(vehicle.fleetId)}` : undefined}>
          <Select
            required
            placeholder="Elige un vehículo"
            options={vehicles.map((v) => ({ value: v.code, label: `${v.code} · ${v.model}` }))}
            value={form.vehicleCode}
            onChange={(e) => setForm({ ...form, vehicleCode: e.target.value })}
          />
        </Field>
        <Field label="Turno">
          <div>
            <Segmented value={form.shift} onChange={(shift) => setForm({ ...form, shift })} options={[{ value: 'Day', label: label('Day') }, { value: 'Night', label: label('Night') }]} />
          </div>
        </Field>
        <Field label="Ubicación">
          {locations.length ? (
            <Select required placeholder="Elige una ubicación" options={locations.map((l) => ({ value: l.name, label: l.name }))} value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} />
          ) : (
            <Input required value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} placeholder="Tajo norte" />
          )}
        </Field>
        <Button type="submit" variant="accent" size="lg" icon={Plus} className="w-full" busy={busy === 'register'} disabled={!operators.length || !vehicles.length}>
          Registrar
        </Button>
        {(!operators.length || !vehicles.length) && <p className="text-center text-sm text-basalt-mute">Primero crea operadores y vehículos en Operaciones.</p>}
      </form>
    </Drawer>
  )
}

export default function Fleet() {
  const { units, fleets } = useDirectory()
  const [siteId, setSiteId] = useState('')
  const [fleetId, setFleetId] = useState('')
  const [risk, setRisk] = useState('all')
  const [selectedId, setSelectedId] = useState(null)
  const [registering, setRegistering] = useState(false)
  const { data, loading, reload } = useAsync(() => loadFleet(siteId, fleetId), [siteId, fleetId], { interval: 10000 })

  const all = useMemo(() => data?.operators ?? [], [data])
  const shown = useMemo(
    () =>
      all
        .filter((op) => risk === 'all' || op.currentRiskLevel === risk)
        .sort((a, b) => (RISK_ORDER[a.currentRiskLevel] ?? 3) - (RISK_ORDER[b.currentRiskLevel] ?? 3)),
    [all, risk],
  )
  const count = (level) => all.filter((op) => op.currentRiskLevel === level).length
  const stale = all.filter((op) => op.isStale).length
  const selected = all.find((op) => op.operatorId === selectedId)
  const fleetOptions = fleets.filter((f) => !siteId || f.miningUnitId === siteId).map((f) => ({ value: f.id, label: f.name }))

  return (
    <>
      <PageHeader icon={Truck} title="Flota">
        <Button variant="outline" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>
        <Button variant="accent" icon={Plus} onClick={() => setRegistering(true)}>Registrar cabina</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={data ? all.length : null} caption="Cabinas" tone="info" icon={Truck} onClick={() => setRisk('all')} />
        <Stat value={data ? count('Critical') : null} caption="En riesgo crítico" tone="Critical" icon={AlertTriangle} onClick={() => setRisk('Critical')} />
        <Stat value={data ? count('Warning') : null} caption="En advertencia" tone="Warning" icon={AlertTriangle} onClick={() => setRisk('Warning')} />
        <Stat value={data ? stale : null} caption="Con dato antiguo" tone={stale ? 'Warning' : 'Normal'} icon={WifiOff} />
      </div>

      <section className="contours mt-5 rounded-tile border border-line bg-white p-4">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex w-full flex-wrap gap-2 sm:w-auto">
            <div className="w-full sm:w-48">
              <Select aria-label="Unidad minera" placeholder="Todas las unidades" options={units.map((u) => ({ value: u.id, label: u.name }))} value={siteId} onChange={(e) => { setSiteId(e.target.value); setFleetId('') }} />
            </div>
            <div className="w-full sm:w-48">
              <Select aria-label="Flota" placeholder="Todas las flotas" options={fleetOptions} value={fleetId} onChange={(e) => setFleetId(e.target.value)} />
            </div>
          </div>
          <Segmented
            value={risk}
            onChange={setRisk}
            options={[
              { value: 'all', label: 'Todas', count: all.length },
              { value: 'Critical', label: label('Critical'), count: count('Critical') },
              { value: 'Warning', label: label('Warning'), count: count('Warning') },
              { value: 'Normal', label: label('Normal'), count: count('Normal') },
            ]}
          />
        </div>

        {!data && loading ? (
          <Spinner />
        ) : shown.length ? (
          <div className="grid gap-3 sm:grid-cols-2 2xl:grid-cols-3">
            {shown.map((op) => (
              <FleetTile key={op.id} operator={op} assessment={data.assessments[op.operatorId]} selected={op.operatorId === selectedId} onClick={() => setSelectedId(op.operatorId)} />
            ))}
          </div>
        ) : (
          <Empty
            icon={Truck}
            title={all.length ? 'Ninguna cabina en este estado.' : 'Aún no hay cabinas en la flota.'}
            action={all.length ? <Button variant="outline" onClick={() => setRisk('all')}>Ver todas</Button> : <Button variant="accent" icon={Plus} onClick={() => setRegistering(true)}>Registrar cabina</Button>}
          />
        )}
      </section>

      {selected && (
        <OperatorDrawer key={selected.operatorId} operator={selected} assessment={data.assessments[selected.operatorId]} onClose={() => setSelectedId(null)} onChanged={reload} />
      )}
      {registering && (
        <RegisterDrawer
          onClose={() => setRegistering(false)}
          onDone={() => {
            setRegistering(false)
            reload()
          }}
        />
      )}
    </>
  )
}
