import {
  Ban,
  CalendarRange,
  Check,
  CircleCheck,
  CircleX,
  FileCheck,
  History,
  Link2,
  Lock,
  RefreshCw,
  Search,
  ShieldAlert,
  ShieldCheck,
  Undo2,
  UserRound,
} from 'lucide-react'
import { useState } from 'react'
import { privacy } from '../api/services'
import { useToast } from '../components/Toasts'
import { Button, Empty, Field, Input, PageHeader, Panel, Pill, Segmented, Select, Spinner, Table } from '../components/ui'
import { useAuth } from '../auth/AuthContext'
import { useDirectory } from '../lib/directory'
import { dateTime, daysAgo, label, shortId } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

// Spanish text for the purposes of the processing policy catalog (Privacy & Audit, v1.0).
const PURPOSE_HINT = {
  'fatigue-monitoring': 'Monitoreo continuo de la fatiga durante el turno',
  'safety-alerts': 'Advertencias en cabina y escalamiento al supervisor',
  'operational-analytics': 'Tendencias agregadas sin identificar al operador',
}

const TABS = [
  { value: 'consents', label: 'Consentimientos' },
  { value: 'access', label: 'Registro de accesos' },
  { value: 'compliance', label: 'Cumplimiento' },
]

const PERIODS = [
  { value: 7, label: '7 días' },
  { value: 30, label: '30 días' },
  { value: 90, label: '90 días' },
]

// Backend enum ConsentStatus.
const DECISIONS = [
  { value: 'Accepted', icon: CircleCheck, active: 'border-risk-normal bg-risk-normal text-white' },
  { value: 'Declined', icon: CircleX, active: 'border-risk-critical bg-risk-critical text-white' },
  { value: 'Revoked', icon: Undo2, active: 'border-risk-critical bg-risk-critical text-white' },
]

function ConsentState({ consent }) {
  if (consent === null) return <Pill tone="neutral">Sin decisión</Pill>
  return (
    <div className="flex flex-wrap items-center gap-2">
      <Pill value={consent.status} />
      {consent.blocksMonitoring ? (
        <Pill tone="Critical" dot={false}><Ban className="h-3.5 w-3.5" />Monitoreo bloqueado</Pill>
      ) : (
        <Pill tone="Normal" dot={false}><Check className="h-3.5 w-3.5" />Monitoreo permitido</Pill>
      )}
      <Pill tone="neutral" dot={false}>Política {consent.policyVersion}</Pill>
    </div>
  )
}

function ConsentsTab() {
  const { operators } = useDirectory()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const policy = useAsync(() => privacy.purposes(), [])
  const [operatorId, setOperatorId] = useState('')
  const [extra, setExtra] = useState([])
  const [status, setStatus] = useState('Accepted')

  const record = useAsync(async () => {
    if (!operatorId) return null
    const [current, history] = await Promise.all([
      privacy.currentConsent(operatorId).catch((error) => {
        if (error.status === 404) return null
        throw error
      }),
      privacy.consentHistory(operatorId),
    ])
    return { current, history: [...(history ?? [])].sort((a, b) => new Date(b.decidedAt) - new Date(a.decidedAt)) }
  }, [operatorId])

  const purposes = policy.data?.purposes ?? []
  const mandatory = purposes.filter((purpose) => purpose.isMandatory).map((purpose) => purpose.code)
  const selected = [...new Set([...mandatory, ...extra])]
  const toggle = (code) => setExtra((current) => (current.includes(code) ? current.filter((item) => item !== code) : [...current, code]))

  const submit = async () => {
    const saved = await run(
      'consent',
      () => privacy.registerConsent({ operatorId, policyVersion: policy.data.policyVersion, purposes: selected, status }),
      'Decisión registrada.',
    )
    if (saved) record.reload()
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[1fr_380px]">
      <div className="space-y-5">
        <Panel title="Operador" icon={UserRound}>
          <Select
            value={operatorId}
            onChange={(event) => {
              setOperatorId(event.target.value)
              setExtra([])
            }}
            placeholder="Elige un operador"
            options={operators.map((operator) => ({ value: operator.id, label: operator.fullName }))}
          />
          {operatorId && (
            <div className="mt-4">
              {record.loading && !record.data ? <Spinner /> : record.data && <ConsentState consent={record.data.current} />}
            </div>
          )}
        </Panel>

        <Panel title="Historial" icon={History}>
          {!operatorId ? (
            <Empty icon={UserRound} title="Elige un operador para ver sus decisiones." />
          ) : record.loading && !record.data ? (
            <Spinner />
          ) : record.data?.history.length ? (
            <ol className="relative ml-2 space-y-4 border-l-2 border-line pl-5">
              {record.data.history.map((entry) => (
                <li key={entry.id} className="relative">
                  <span
                    className={`absolute -left-[29px] top-1 h-4 w-4 rounded-full border-2 border-white ${
                      entry.status === 'Accepted' ? 'bg-risk-normal' : 'bg-risk-critical'
                    }`}
                  />
                  <div className="flex flex-wrap items-center gap-2">
                    <Pill value={entry.status} />
                    {entry.blocksMonitoring && <Pill tone="Critical" dot={false}><Ban className="h-3.5 w-3.5" />Bloquea</Pill>}
                    <span className="text-[13px] text-basalt-mute">{dateTime(entry.decidedAt)} · {entry.policyVersion}</span>
                  </div>
                  <div className="mt-1.5 flex flex-wrap gap-1">
                    {(entry.purposes ?? []).map((code) => (
                      <Pill key={code} tone="neutral" dot={false}>{code}</Pill>
                    ))}
                  </div>
                </li>
              ))}
            </ol>
          ) : (
            <Empty icon={History} title="Este operador aún no tiene decisiones." />
          )}
        </Panel>
      </div>

      <Panel title="Registrar decisión" icon={ShieldCheck} actions={policy.data && <Pill tone="info" dot={false}>Política {policy.data.policyVersion}</Pill>}>
        {!policy.data && policy.loading ? (
          <Spinner />
        ) : policy.error ? (
          <Empty icon={ShieldAlert} title={policy.error.message} action={<Button variant="outline" icon={RefreshCw} onClick={policy.reload}>Reintentar</Button>} />
        ) : (
          <div className="space-y-4">
            <div className="space-y-2">
              <span className="text-sm font-medium text-basalt-soft">Finalidades</span>
              <div className="space-y-2">
                {purposes.map((purpose) => {
                  const active = selected.includes(purpose.code)
                  return (
                    <button
                      key={purpose.code}
                      type="button"
                      aria-pressed={active}
                      disabled={purpose.isMandatory}
                      onClick={() => toggle(purpose.code)}
                      className={`flex w-full items-start gap-3 rounded-tile border p-3 text-left transition-colors ${
                        active ? 'border-basalt bg-mineral' : 'border-line bg-white hover:border-basalt-mute'
                      } ${purpose.isMandatory ? 'cursor-default' : ''}`}
                    >
                      <span className={`mt-0.5 grid h-5 w-5 shrink-0 place-items-center rounded-full ${active ? 'bg-basalt text-white' : 'border border-line'}`}>
                        {purpose.isMandatory ? <Lock className="h-3 w-3" /> : active && <Check className="h-3 w-3" />}
                      </span>
                      <span className="min-w-0 flex-1">
                        <span className="flex flex-wrap items-center gap-2 text-[15px] font-semibold">
                          {label(purpose.code)}
                          {purpose.isMandatory && <Pill tone="accent" dot={false}>Obligatoria</Pill>}
                        </span>
                        <span className="mt-0.5 block text-[13px] text-basalt-mute">{PURPOSE_HINT[purpose.code] ?? purpose.description}</span>
                      </span>
                    </button>
                  )
                })}
              </div>
            </div>
            <div className="grid grid-cols-3 gap-2" role="radiogroup" aria-label="Decisión">
              {DECISIONS.map(({ value, icon: Icon, active }) => (
                <button
                  key={value}
                  type="button"
                  role="radio"
                  aria-checked={status === value}
                  onClick={() => setStatus(value)}
                  className={`flex h-11 items-center justify-center gap-1.5 rounded-full border text-sm font-semibold transition-colors ${
                    status === value ? active : 'border-line bg-white text-basalt-soft hover:border-basalt-mute'
                  }`}
                >
                  <Icon className="h-4 w-4" />
                  {label(value)}
                </button>
              ))}
            </div>
            <Button variant="accent" size="lg" className="w-full" icon={Check} busy={busy === 'consent'} disabled={!operatorId || !policy.data} onClick={submit}>
              Registrar
            </Button>
          </div>
        )}
      </Panel>
    </div>
  )
}

function AccessTab() {
  const { userName } = useDirectory()
  const [decision, setDecision] = useState('')
  const [typeDraft, setTypeDraft] = useState('')
  const [resourceType, setResourceType] = useState('')
  const { data, loading, error, reload } = useAsync(
    () => privacy.accessLog({ page: 1, pageSize: 50, decision: decision || undefined, resourceType: resourceType || undefined }),
    [decision, resourceType],
  )

  const columns = [
    { key: 'accessedAt', title: 'Hora', render: (row) => <span className="whitespace-nowrap">{dateTime(row.accessedAt)}</span> },
    { key: 'userId', title: 'Usuario', render: (row) => <span className="font-semibold">{userName(row.userId)}</span> },
    {
      key: 'resource',
      title: 'Recurso',
      render: (row) => (
        <span className="flex items-center gap-1.5 whitespace-nowrap">
          <Pill tone="info" dot={false}>{row.resourceType}</Pill>
          <span className="text-[13px] text-basalt-mute">{shortId(row.resourceId)}</span>
        </span>
      ),
    },
    {
      key: 'fields',
      title: 'Campos',
      render: (row) => (
        <div className="flex flex-wrap gap-1">
          {(row.fields ?? []).map((field) => (
            <Pill key={field} tone="neutral" dot={false}>{field}</Pill>
          ))}
        </div>
      ),
    },
    { key: 'purpose', title: 'Finalidad' },
    { key: 'decision', title: 'Decisión', render: (row) => <Pill value={row.decision} /> },
    {
      key: 'hash',
      title: 'Cadena',
      render: (row) => (
        <span className="relative flex items-center">
          <span className="absolute -bottom-4 -top-4 left-[13px] w-0.5 bg-line" aria-hidden="true" />
          <span
            title={row.hash}
            className="relative inline-flex items-center gap-1 rounded-full border border-line bg-white px-2 py-0.5 text-[13px] font-semibold text-basalt-soft"
          >
            <Link2 className="h-3.5 w-3.5 text-ore" />
            {row.hash ? row.hash.slice(0, 10) : '—'}
          </span>
        </span>
      ),
    },
  ]

  return (
    <Panel
      title="Registro de accesos"
      icon={History}
      actions={<Button variant="outline" size="sm" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>}
    >
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <Segmented
          value={decision}
          onChange={setDecision}
          options={[
            { value: '', label: 'Todos' },
            { value: 'Granted', label: label('Granted') },
            { value: 'Denied', label: label('Denied') },
          ]}
        />
        <form
          className="flex items-center gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            setResourceType(typeDraft.trim())
          }}
        >
          <div className="w-48">
            <Input value={typeDraft} onChange={(event) => setTypeDraft(event.target.value)} placeholder="Tipo de recurso" aria-label="Tipo de recurso" />
          </div>
          <Button type="submit" variant="outline" size="sm" icon={Search}>Filtrar</Button>
        </form>
      </div>
      {!data && loading ? (
        <Spinner />
      ) : error ? (
        <Empty icon={ShieldAlert} title={error.message} action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Reintentar</Button>} />
      ) : (
        <Table
          columns={columns}
          rows={data ?? []}
          empty={
            <Empty
              icon={History}
              title="Sin accesos registrados con este filtro."
              action={
                (decision || resourceType) && (
                  <Button
                    variant="outline"
                    onClick={() => {
                      setDecision('')
                      setResourceType('')
                      setTypeDraft('')
                    }}
                  >
                    Quitar filtros
                  </Button>
                )
              }
            />
          }
        />
      )}
    </Panel>
  )
}

function ComplianceTab() {
  const { user } = useAuth()
  const { units, fleets, unitName, fleetName, userName } = useDirectory()
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [days, setDays] = useState(30)
  const [siteId, setSiteId] = useState('')
  const [fleetId, setFleetId] = useState('')
  const [lookup, setLookup] = useState('')
  const [reports, setReports] = useState([])

  const keep = (report) => setReports((current) => [report, ...current.filter((item) => item.id !== report.id)])

  const request = async () => {
    const report = await run(
      'request',
      () =>
        privacy.requestComplianceReport({
          from: daysAgo(days),
          to: new Date().toISOString(),
          siteId: siteId || null,
          fleetId: fleetId || null,
          requestedBy: user.id,
        }),
      'Reporte de cumplimiento solicitado.',
    )
    if (report) keep(report)
  }

  const find = async (event) => {
    event.preventDefault()
    const id = lookup.trim()
    if (!id) return
    const report = await run('lookup', () => privacy.complianceReport(id))
    if (report) {
      keep(report)
      setLookup('')
    }
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[380px_1fr]">
      <div className="space-y-5">
        <Panel title="Solicitar" icon={FileCheck}>
          <div className="space-y-4">
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
            <Button variant="accent" size="lg" className="w-full" icon={FileCheck} busy={busy === 'request'} onClick={request}>
              Solicitar
            </Button>
          </div>
        </Panel>
        <Panel title="Buscar por id" icon={Search}>
          <form onSubmit={find} className="flex gap-2">
            <Input value={lookup} onChange={(event) => setLookup(event.target.value)} placeholder="Id del reporte" aria-label="Id del reporte" />
            <Button type="submit" variant="outline" icon={Search} busy={busy === 'lookup'} disabled={!lookup.trim()}>Buscar</Button>
          </form>
        </Panel>
      </div>

      <Panel title="Reportes de esta sesión" icon={FileCheck} bodyClass="p-2">
        {reports.length ? (
          <ul className="space-y-1">
            {reports.map((report) => (
              <li key={report.id} className="flex flex-wrap items-center gap-3 rounded-lg px-2 py-2">
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-ore-wash text-ore">
                  <FileCheck className="h-5 w-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2 text-[15px] font-semibold">
                    <span>{dateTime(report.from)} – {dateTime(report.to)}</span>
                  </div>
                  <div className="flex flex-wrap gap-x-3 text-[13px] text-basalt-mute">
                    <span title={report.id}>#{shortId(report.id)}</span>
                    {report.siteId && <span>{unitName(report.siteId)}</span>}
                    {report.fleetId && <span>{fleetName(report.fleetId)}</span>}
                    <span>{userName(report.requestedBy)}</span>
                  </div>
                </div>
                <Pill value={report.status} />
                <Button
                  variant="ghost"
                  size="sm"
                  icon={RefreshCw}
                  busy={busy === report.id}
                  onClick={async () => {
                    const fresh = await run(report.id, () => privacy.complianceReport(report.id))
                    if (fresh) keep(fresh)
                  }}
                >
                  Actualizar
                </Button>
              </li>
            ))}
          </ul>
        ) : (
          <Empty icon={FileCheck} title="Aún no solicitas reportes en esta sesión." action={<Button variant="accent" icon={FileCheck} busy={busy === 'request'} onClick={request}>Solicitar</Button>} />
        )}
      </Panel>
    </div>
  )
}

export default function Privacy() {
  const [tab, setTab] = useState('consents')
  return (
    <>
      <PageHeader icon={ShieldCheck} title="Privacidad y auditoría">
        <Segmented value={tab} onChange={setTab} options={TABS} />
      </PageHeader>
      {tab === 'consents' && <ConsentsTab />}
      {tab === 'access' && <AccessTab />}
      {tab === 'compliance' && <ComplianceTab />}
    </>
  )
}
