import {
  AlertTriangle,
  ArrowDown,
  ArrowUp,
  CalendarClock,
  ChartColumn,
  Lightbulb,
  Minus,
  Pause,
  Plus,
  RefreshCw,
  Shuffle,
  Siren,
  Users,
} from 'lucide-react'
import { useState } from 'react'
import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { analytics } from '../api/services'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, PageHeader, Panel, Pill, Segmented, Spinner, Stat } from '../components/ui'
import { useAuth } from '../auth/AuthContext'
import { useDirectory } from '../lib/directory'
import { dateTime, daysAgo, label } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const PERIODS = [
  { value: 7, label: '7 días' },
  { value: 30, label: '30 días' },
  { value: 90, label: '90 días' },
]

const DIMENSIONS = [
  { value: 'Shift', label: 'Turno' },
  { value: 'MineSite', label: 'Sitio' },
  { value: 'Location', label: 'Ubicación' },
  { value: 'Fleet', label: 'Flota' },
]

// Backend enum PreventiveMeasureType.
const MEASURE_TYPES = [
  { value: 'PreventivePause', icon: Pause },
  { value: 'OperatorRelief', icon: Users },
  { value: 'ShiftReorganization', icon: Shuffle },
  { value: 'Other', icon: Lightbulb },
]
const MEASURE_ICON = Object.fromEntries(MEASURE_TYPES.map((type) => [type.value, type.icon]))

const SCOPE = [
  { key: 'shift', label: 'Turno' },
  { key: 'mineSite', label: 'Sitio' },
  { key: 'location', label: 'Ubicación' },
  { key: 'fleet', label: 'Flota' },
]

const EMPTY_FORM = { name: '', measureType: 'PreventivePause', description: '', appliedAt: '', shift: '', mineSite: '', location: '', fleet: '' }

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

function localNow() {
  const now = new Date()
  return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}

function TrendsPanel() {
  const [days, setDays] = useState(30)
  const [groupBy, setGroupBy] = useState('Shift')
  const { data, loading, error, reload } = useAsync(
    () => analytics.trends({ from: daysAgo(days), to: new Date().toISOString(), groupBy }),
    [days, groupBy],
  )

  const { unitName } = useDirectory()
  // Observations recorded from Kafka events carry the mining unit id as mine site.
  const groupName = (key) => (groupBy === 'MineSite' && UUID.test(key) ? unitName(key) : label(key))
  const groups = (data?.groups ?? []).map((group) => ({
    name: groupName(group.key),
    alerts: group.alerts - group.criticalAlerts,
    critical: group.criticalAlerts,
  }))
  const critical = (data?.groups ?? []).reduce((sum, group) => sum + group.criticalAlerts, 0)

  return (
    <Panel
      title="Tendencias de fatiga"
      icon={ChartColumn}
      actions={<Segmented value={days} onChange={setDays} options={PERIODS} />}
    >
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <Segmented value={groupBy} onChange={setGroupBy} options={DIMENSIONS} />
        {data && !data.isRepresentative && (
          <Pill tone="Warning" dot={false}>
            <AlertTriangle className="h-3.5 w-3.5" />
            {data.warning ?? 'Muestra mínima no alcanzada'}
          </Pill>
        )}
      </div>

      <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
        <Stat value={data?.totalAlerts} caption="Alertas" tone="Warning" icon={ChartColumn} />
        <Stat value={data ? critical : null} caption="Críticas" tone="Critical" icon={Siren} />
        <Stat value={data?.groups?.length} caption="Grupos" tone="info" />
      </div>

      {!data && loading ? (
        <Spinner />
      ) : error ? (
        <Empty icon={AlertTriangle} title={error.message} action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Reintentar</Button>} />
      ) : groups.length ? (
        <div className="h-72">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={groups} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
              <CartesianGrid vertical={false} stroke="#DDE3E1" />
              <XAxis dataKey="name" tickLine={false} axisLine={false} tick={{ fill: '#6B7A80', fontSize: 13 }} />
              <YAxis allowDecimals={false} tickLine={false} axisLine={false} tick={{ fill: '#6B7A80', fontSize: 13 }} />
              <Tooltip cursor={{ fill: 'rgba(31,42,46,0.04)' }} />
              <Legend iconType="circle" />
              <Bar isAnimationActive={false} dataKey="alerts" name="Advertencia" stackId="a" fill="#E58A00" />
              <Bar isAnimationActive={false} dataKey="critical" name="Crítica" stackId="a" fill="#D6362F" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      ) : (
        <Empty icon={ChartColumn} title="Sin alertas en este periodo." action={<Button variant="outline" onClick={() => setDays(90)}>Ver 90 días</Button>} />
      )}
    </Panel>
  )
}

function Variation({ value }) {
  if (value == null) return <span className="text-sm text-basalt-mute">—</span>
  const Icon = value > 0 ? ArrowUp : value < 0 ? ArrowDown : Minus
  const tone = value > 0 ? 'text-risk-critical' : value < 0 ? 'text-risk-normal' : 'text-basalt-mute'
  return (
    <span className={`inline-flex items-center gap-1 ${tone}`}>
      <Icon className="h-5 w-5" />
      <span className="numeral text-2xl">{Math.abs(Math.round(value))} %</span>
    </span>
  )
}

function Compare({ caption, before, after, variation }) {
  return (
    <div className="rounded-tile border border-line p-3">
      <div className="mb-2 text-sm text-basalt-mute">{caption}</div>
      <div className="flex items-end justify-between gap-3">
        <div className="flex items-end gap-4">
          <div>
            <div className="numeral text-[28px] leading-none text-basalt-mute">{before ?? '—'}</div>
            <div className="mt-1 text-xs text-basalt-mute">Antes</div>
          </div>
          <div>
            <div className="numeral text-[28px] leading-none">{after ?? '—'}</div>
            <div className="mt-1 text-xs text-basalt-mute">Después</div>
          </div>
        </div>
        {variation !== undefined && <Variation value={variation} />}
      </div>
    </div>
  )
}

function variationOf(before, after) {
  if (before == null || after == null || before === 0) return null
  return ((after - before) / before) * 100
}

function EvaluationDrawer({ measure, onClose }) {
  const [windowDays, setWindowDays] = useState(30)
  const { data, loading, error } = useAsync(
    () => (measure ? analytics.evaluate(measure.id, windowDays) : Promise.resolve(null)),
    [measure?.id, windowDays],
  )
  const round = (value) => (value == null ? null : Math.round(value * 10) / 10)

  return (
    <Drawer open={!!measure} title={measure?.name ?? ''} onClose={onClose}>
      {measure && (
        <>
          <div className="flex flex-wrap items-center gap-2">
            <Pill tone="info">{label(measure.measureType)}</Pill>
            <span className="flex items-center gap-1 text-sm text-basalt-mute">
              <CalendarClock className="h-4 w-4" />
              {dateTime(measure.appliedAt)}
            </span>
          </div>
          <Segmented
            value={windowDays}
            onChange={setWindowDays}
            options={PERIODS}
          />
          {loading && !data ? (
            <Spinner />
          ) : error ? (
            <Empty icon={AlertTriangle} title={error.message} />
          ) : data ? (
            <>
              <Pill value={data.isComparable ? 'Normal' : 'Warning'}>
                {data.isComparable ? 'Periodos comparables' : 'No comparable'}
              </Pill>
              <Compare
                caption="Alertas por día"
                before={round(data.before?.alertsPerDay)}
                after={round(data.after?.alertsPerDay)}
                variation={data.alertsPerDayVariationPercent}
              />
              <Compare
                caption="Alertas críticas"
                before={data.before?.criticalAlerts}
                after={data.after?.criticalAlerts}
                variation={variationOf(data.before?.criticalAlerts, data.after?.criticalAlerts)}
              />
              <Compare
                caption="Operadores con alertas repetidas"
                before={data.before?.operatorsWithRepeatedAlerts}
                after={data.after?.operatorsWithRepeatedAlerts}
                variation={data.repeatedAlertsVariationPercent}
              />
              {data.conclusion && <p className="rounded-tile bg-mineral p-3 text-[15px]">{data.conclusion}</p>}
            </>
          ) : null}
        </>
      )}
    </Drawer>
  )
}

function MeasureDrawer({ open, onClose, onSaved }) {
  const { user } = useAuth()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [form, setForm] = useState(EMPTY_FORM)
  const set = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }))
  const appliedAt = form.appliedAt || localNow()

  const submit = async (event) => {
    event.preventDefault()
    const optional = (value) => (value.trim() ? value.trim() : null)
    const created = await run(
      'save',
      () =>
        analytics.registerMeasure({
          name: form.name.trim(),
          measureType: form.measureType,
          description: optional(form.description),
          appliedAt: new Date(appliedAt).toISOString(),
          registeredBy: user.id,
          shift: optional(form.shift),
          mineSite: optional(form.mineSite),
          location: optional(form.location),
          fleet: optional(form.fleet),
        }),
      'Medida registrada.',
    )
    if (created) {
      setForm(EMPTY_FORM)
      onSaved()
    }
  }

  return (
    <Drawer
      open={open}
      title="Registrar medida"
      onClose={onClose}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button type="submit" form="measure-form" variant="accent" busy={busy === 'save'} disabled={!form.name.trim()}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="measure-form" onSubmit={submit} className="space-y-4">
        <Field label="Nombre">
          <Input value={form.name} onChange={set('name')} required maxLength={120} />
        </Field>
        <div className="space-y-1">
          <span className="text-sm font-medium text-basalt-soft">Tipo</span>
          <div className="grid grid-cols-2 gap-2" role="radiogroup" aria-label="Tipo de medida">
            {MEASURE_TYPES.map(({ value, icon: Icon }) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={form.measureType === value}
                onClick={() => setForm((current) => ({ ...current, measureType: value }))}
                className={`flex h-11 items-center gap-2 rounded-full border px-4 text-sm font-semibold transition-colors ${
                  form.measureType === value ? 'border-basalt bg-basalt text-white' : 'border-line bg-white text-basalt-soft hover:border-basalt-mute'
                }`}
              >
                <Icon className="h-4 w-4" />
                {label(value)}
              </button>
            ))}
          </div>
        </div>
        <Field label="Descripción">
          <textarea className="field min-h-20" value={form.description} onChange={set('description')} maxLength={1000} />
        </Field>
        <Field label="Aplicada el">
          <Input type="datetime-local" value={appliedAt} onChange={set('appliedAt')} required />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          {SCOPE.map((scope) => (
            <Field key={scope.key} label={scope.label} hint="Opcional">
              <Input value={form[scope.key]} onChange={set(scope.key)} />
            </Field>
          ))}
        </div>
      </form>
    </Drawer>
  )
}

function MeasuresPanel() {
  const { data, loading, reload } = useAsync(() => analytics.measures(), [])
  const [creating, setCreating] = useState(false)
  const [selected, setSelected] = useState(null)
  const measures = [...(data ?? [])].sort((a, b) => new Date(b.appliedAt) - new Date(a.appliedAt))

  return (
    <Panel
      title="Medidas preventivas"
      icon={Lightbulb}
      actions={<Button variant="accent" size="sm" icon={Plus} onClick={() => setCreating(true)}>Registrar medida</Button>}
    >
      {!data && loading ? (
        <Spinner />
      ) : measures.length ? (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {measures.map((measure) => {
            const Icon = MEASURE_ICON[measure.measureType] ?? Lightbulb
            const chips = SCOPE.filter((scope) => measure[scope.key])
            return (
              <button
                key={measure.id}
                type="button"
                onClick={() => setSelected(measure)}
                className="flex items-start gap-3 rounded-tile border border-line bg-white p-3 text-left transition-colors hover:border-basalt-mute"
              >
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-ore-wash text-ore">
                  <Icon className="h-5 w-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[15px] font-semibold">{measure.name}</div>
                  <div className="mt-0.5 flex items-center gap-1 text-[13px] text-basalt-mute">
                    <CalendarClock className="h-3.5 w-3.5" />
                    {dateTime(measure.appliedAt)}
                  </div>
                  <div className="mt-2 flex flex-wrap gap-1">
                    {chips.length ? (
                      chips.map((scope) => (
                        <Pill key={scope.key} tone="neutral" dot={false}>{label(measure[scope.key])}</Pill>
                      ))
                    ) : (
                      <Pill tone="info" dot={false}>Toda la operación</Pill>
                    )}
                  </div>
                </div>
              </button>
            )
          })}
        </div>
      ) : (
        <Empty
          icon={Lightbulb}
          title="Aún no hay medidas registradas."
          action={<Button variant="accent" icon={Plus} onClick={() => setCreating(true)}>Registrar medida</Button>}
        />
      )}

      <MeasureDrawer
        open={creating}
        onClose={() => setCreating(false)}
        onSaved={() => {
          setCreating(false)
          reload()
        }}
      />
      <EvaluationDrawer measure={selected} onClose={() => setSelected(null)} />
    </Panel>
  )
}

export default function Analytics() {
  return (
    <>
      <PageHeader icon={ChartColumn} title="Analítica" />
      <div className="space-y-5">
        <TrendsPanel />
        <MeasuresPanel />
      </div>
    </>
  )
}
