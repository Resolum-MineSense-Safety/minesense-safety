import { CalendarRange, Check, Database, Download, FileSpreadsheet, FileText, Files, Plus, RefreshCw, Send } from 'lucide-react'
import { useState } from 'react'
import { reporting } from '../api/services'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, PageHeader, Panel, Pill, Segmented, Select, Spinner } from '../components/ui'
import { useAuth } from '../auth/AuthContext'
import { useDirectory } from '../lib/directory'
import { dateTime, daysAgo, shortId } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

const PERIODS = [
  { value: 7, label: '7 días' },
  { value: 30, label: '30 días' },
  { value: 90, label: '90 días' },
]

const DATA_SOURCES = [
  { value: 'fatigue-assessments', label: 'Evaluaciones de fatiga' },
  { value: 'alerts', label: 'Alertas' },
  { value: 'incidents', label: 'Incidentes' },
  { value: 'analytics', label: 'Analítica' },
]
const SOURCE_LABEL = Object.fromEntries(DATA_SOURCES.map((source) => [source.value, source.label]))

const FORMATS = [
  { value: 'Pdf', label: 'PDF', icon: FileText },
  { value: 'Csv', label: 'CSV', icon: FileSpreadsheet },
]
const FORMAT_ICON = { Pdf: FileText, Csv: FileSpreadsheet }

const dateOnly = (value) =>
  value ? new Date(value).toLocaleDateString('es-PE', { day: '2-digit', month: 'short' }) : '—'

function fileNameFrom(disposition) {
  if (!disposition) return null
  const encoded = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(disposition)
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim().replace(/^"|"$/g, ''))
    } catch {
      // fall through to the plain filename
    }
  }
  const plain = /filename="?([^";]+)"?/i.exec(disposition)
  return plain ? plain[1].trim() : null
}

function ChipToggle({ active, onClick, children }) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={`inline-flex h-9 items-center gap-1.5 rounded-full border px-3 text-sm font-semibold transition-colors ${
        active ? 'border-basalt bg-basalt text-white' : 'border-line bg-white text-basalt-soft hover:border-basalt-mute'
      }`}
    >
      {active && <Check className="h-4 w-4" />}
      {children}
    </button>
  )
}

function DefineDrawer({ open, onClose, onSaved }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [form, setForm] = useState({ code: '', name: '', version: '1.0', dataSources: ['alerts'] })
  const set = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }))
  const toggle = (value) =>
    setForm((current) => ({
      ...current,
      dataSources: current.dataSources.includes(value)
        ? current.dataSources.filter((item) => item !== value)
        : [...current.dataSources, value],
    }))
  const valid = form.code.trim() && form.name.trim() && form.version.trim() && form.dataSources.length

  const submit = async (event) => {
    event.preventDefault()
    const created = await run(
      'define',
      async () => {
        try {
          return await reporting.define({
            code: form.code.trim(),
            name: form.name.trim(),
            version: form.version.trim(),
            dataSources: form.dataSources,
          })
        } catch (error) {
          if (error.status === 409) throw new Error('Ese indicador ya existe en esa versión.', { cause: error })
          throw error
        }
      },
      'Indicador definido.',
    )
    if (created) {
      setForm({ code: '', name: '', version: '1.0', dataSources: ['alerts'] })
      onSaved()
    }
  }

  return (
    <Drawer
      open={open}
      title="Nuevo indicador"
      onClose={onClose}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button type="submit" form="define-form" variant="accent" busy={busy === 'define'} disabled={!valid}>Guardar</Button>
        </>
      }
    >
      <form id="define-form" onSubmit={submit} className="space-y-4">
        <div className="grid grid-cols-[1fr_110px] gap-3">
          <Field label="Código">
            <Input value={form.code} onChange={set('code')} required placeholder="fatigue-weekly" />
          </Field>
          <Field label="Versión">
            <Input value={form.version} onChange={set('version')} required />
          </Field>
        </div>
        <Field label="Nombre">
          <Input value={form.name} onChange={set('name')} required />
        </Field>
        <div className="space-y-1">
          <span className="text-sm font-medium text-basalt-soft">Fuentes de datos</span>
          <div className="flex flex-wrap gap-2">
            {DATA_SOURCES.map((source) => (
              <ChipToggle key={source.value} active={form.dataSources.includes(source.value)} onClick={() => toggle(source.value)}>
                {source.label}
              </ChipToggle>
            ))}
          </div>
        </div>
      </form>
    </Drawer>
  )
}

function DefinitionsPanel({ definitions, loading, onCreate }) {
  return (
    <Panel
      title="Indicadores"
      icon={Database}
      actions={<Button variant="accent" size="sm" icon={Plus} onClick={onCreate}>Nuevo indicador</Button>}
    >
      {!definitions && loading ? (
        <Spinner />
      ) : definitions?.length ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {definitions.map((definition) => (
            <div key={definition.id} className="rounded-tile border border-line p-3">
              <div className="flex items-center gap-2">
                <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{definition.code}</span>
                <Pill tone="neutral" dot={false}>{definition.version}</Pill>
                {!definition.isActive && <Pill value="Inactive" />}
              </div>
              <div className="mt-1.5 text-[15px] font-semibold">{definition.name}</div>
              <div className="mt-2 flex flex-wrap gap-1">
                {(definition.dataSources ?? []).map((source) => (
                  <Pill key={source} tone="info" dot={false}>{SOURCE_LABEL[source] ?? source}</Pill>
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <Empty icon={Database} title="Sin indicadores definidos." action={<Button variant="accent" icon={Plus} onClick={onCreate}>Nuevo indicador</Button>} />
      )}
    </Panel>
  )
}

function RequestPanel({ definitions, onRequested }) {
  const { user } = useAuth()
  const { units, fleets } = useDirectory()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [code, setCode] = useState('')
  const [format, setFormat] = useState('Pdf')
  const [days, setDays] = useState(30)
  const [siteId, setSiteId] = useState('')
  const [fleetId, setFleetId] = useState('')
  const definitionCode = code || definitions?.[0]?.code || ''

  const submit = async () => {
    const job = await run(
      'request',
      () =>
        reporting.request({
          from: daysAgo(days),
          to: new Date().toISOString(),
          siteId: siteId || null,
          fleetId: fleetId || null,
          definitionCode,
          format,
          requestedBy: user.id,
        }),
      'Reporte solicitado.',
    )
    if (job) onRequested()
  }

  return (
    <Panel title="Solicitar reporte" icon={Send}>
      <div className="space-y-4">
        <Field label="Indicador">
          <Select
            value={definitionCode}
            onChange={(event) => setCode(event.target.value)}
            options={Object.values(
              // A job always uses the latest version of the indicator, so offer each code once.
              Object.fromEntries(
                [...(definitions ?? [])]
                  .sort((a, b) => a.version.localeCompare(b.version, undefined, { numeric: true }))
                  .map((definition) => [definition.code, definition]),
              ),
            ).map((definition) => ({ value: definition.code, label: `${definition.name} · ${definition.version}` }))}
            placeholder={definitions?.length ? undefined : 'Sin indicadores'}
          />
        </Field>
        <div className="space-y-1">
          <span className="text-sm font-medium text-basalt-soft">Formato</span>
          <div className="grid grid-cols-2 gap-2" role="radiogroup" aria-label="Formato">
            {FORMATS.map(({ value, label: text, icon: Icon }) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={format === value}
                onClick={() => setFormat(value)}
                className={`flex h-12 items-center justify-center gap-2 rounded-full border text-[15px] font-semibold transition-colors ${
                  format === value ? 'border-basalt bg-basalt text-white' : 'border-line bg-white text-basalt-soft hover:border-basalt-mute'
                }`}
              >
                <Icon className="h-5 w-5" />
                {text}
              </button>
            ))}
          </div>
        </div>
        <div className="space-y-1">
          <span className="flex items-center gap-1.5 text-sm font-medium text-basalt-soft"><CalendarRange className="h-4 w-4" />Periodo</span>
          <Segmented value={days} onChange={setDays} options={PERIODS} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Unidad minera">
            <Select value={siteId} onChange={(event) => setSiteId(event.target.value)} placeholder="Todas" options={units.map((unit) => ({ value: unit.id, label: unit.name }))} />
          </Field>
          <Field label="Flota">
            <Select value={fleetId} onChange={(event) => setFleetId(event.target.value)} placeholder="Todas" options={fleets.map((item) => ({ value: item.id, label: item.name }))} />
          </Field>
        </div>
        <Button variant="accent" size="lg" icon={Send} className="w-full" busy={busy === 'request'} disabled={!definitionCode} onClick={submit}>
          Solicitar
        </Button>
      </div>
    </Panel>
  )
}

function JobsPanel({ jobs, loading, reload }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { unitName, fleetName } = useDirectory()

  const download = (job) =>
    run(job.id, async () => {
      const res = await reporting.download(job.id)
      const blob = await res.blob()
      const name = fileNameFrom(res.headers.get('Content-Disposition')) ?? `${job.definitionCode}.${job.format.toLowerCase()}`
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = name
      document.body.appendChild(anchor)
      anchor.click()
      anchor.remove()
      setTimeout(() => URL.revokeObjectURL(url), 1000)
      const checksum = res.headers.get('X-Checksum')
      toast.ok(checksum ? `Descargado · checksum ${checksum.slice(0, 12)}` : 'Descargado.')
    })

  return (
    <Panel
      title="Reportes generados"
      icon={Files}
      actions={<Button variant="outline" size="sm" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>}
      bodyClass="p-2"
    >
      {!jobs && loading ? (
        <Spinner />
      ) : jobs?.length ? (
        <ul className="space-y-1">
          {jobs.map((job) => {
            const Icon = FORMAT_ICON[job.format] ?? FileText
            return (
              <li key={job.id} className="flex flex-wrap items-center gap-3 rounded-lg px-2 py-2 hover:bg-mineral">
                <span className={`grid h-10 w-10 shrink-0 place-items-center rounded-xl ${job.format === 'Csv' ? 'bg-risk-normalWash text-risk-normal' : 'bg-risk-criticalWash text-risk-critical'}`}>
                  <Icon className="h-5 w-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="truncate text-[15px] font-semibold">{job.definitionCode}</span>
                    {job.definitionVersion && <span className="text-[13px] text-basalt-mute">{job.definitionVersion}</span>}
                  </div>
                  <div className="flex flex-wrap items-center gap-x-3 text-[13px] text-basalt-mute">
                    <span className="flex items-center gap-1"><CalendarRange className="h-3.5 w-3.5" />{dateOnly(job.from)} – {dateOnly(job.to)}</span>
                    {job.siteId && <span>{unitName(job.siteId)}</span>}
                    {job.fleetId && <span>{fleetName(job.fleetId)}</span>}
                    <span>{job.completedAt ? dateTime(job.completedAt) : dateTime(job.requestedAt)}</span>
                  </div>
                  {job.failureReason && <div className="text-[13px] text-risk-critical">{job.failureReason}</div>}
                </div>
                <Pill value={job.status} />
                <Button
                  variant={job.status === 'Ready' ? 'primary' : 'outline'}
                  size="sm"
                  icon={Download}
                  busy={busy === job.id}
                  disabled={job.status !== 'Ready'}
                  onClick={() => download(job)}
                  aria-label={`Descargar ${job.definitionCode} ${shortId(job.id)}`}
                >
                  Descargar
                </Button>
              </li>
            )
          })}
        </ul>
      ) : (
        <Empty icon={Files} title="Aún no hay reportes." action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Actualizar</Button>} />
      )}
    </Panel>
  )
}

export default function Reports() {
  const definitions = useAsync(() => reporting.definitions(), [])
  const jobs = useAsync(() => reporting.jobs({ page: 1, pageSize: 50 }), [], { interval: 15000 })
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader icon={FileText} title="Reportes" />
      <div className="grid gap-5 xl:grid-cols-[1fr_380px]">
        <div className="space-y-5">
          <JobsPanel jobs={jobs.data} loading={jobs.loading} reload={jobs.reload} />
          <DefinitionsPanel definitions={definitions.data} loading={definitions.loading} onCreate={() => setCreating(true)} />
        </div>
        <div>
          <RequestPanel definitions={definitions.data} onRequested={jobs.reload} />
        </div>
      </div>
      <DefineDrawer
        open={creating}
        onClose={() => setCreating(false)}
        onSaved={() => {
          setCreating(false)
          definitions.reload()
        }}
      />
    </>
  )
}
