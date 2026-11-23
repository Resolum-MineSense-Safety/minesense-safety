import { AlertTriangle, ChevronLeft, ChevronRight, ClipboardCheck, Home, Inbox, Lock, RefreshCw, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { feedback } from '../api/services'
import { Button, Empty, IconButton, PageHeader, Panel, Pill, Segmented, Spinner, Stat } from '../components/ui'
import { useAuth } from '../auth/AuthContext'
import { useDirectory } from '../lib/directory'
import { dateTime, daysAgo, label, pct } from '../lib/format'
import { useAsync } from '../lib/hooks'

const PERIODS = [
  { value: 7, label: '7 días' },
  { value: 30, label: '30 días' },
  { value: 90, label: '90 días' },
]

const PAGE_SIZE = 20

const period = (days) => ({ from: daysAgo(days), to: new Date().toISOString() })

function Forbidden() {
  const navigate = useNavigate()
  return (
    <Empty
      icon={Lock}
      title="Tu rol no ve la fiabilidad."
      action={<Button variant="outline" icon={Home} onClick={() => navigate('/')}>Ir al centro de control</Button>}
    />
  )
}

function ReliabilityPanel({ operationId, days }) {
  const { data, loading, error, reload } = useAsync(
    () => feedback.reliability({ operationId, ...period(days) }),
    [operationId, days],
  )

  if (!data && loading) return <Panel><Spinner /></Panel>
  if (error?.status === 403) return <Panel><Forbidden /></Panel>
  if (error) {
    return (
      <Panel>
        <Empty icon={AlertTriangle} title={error.message} action={<Button variant="outline" icon={RefreshCw} onClick={reload}>Reintentar</Button>} />
      </Panel>
    )
  }

  const slices = [
    { name: 'Confirmadas', value: data.confirmed, color: '#2E9E6B' },
    { name: 'Falsas alarmas', value: data.falseAlarms, color: '#D6362F' },
    { name: 'No concluyentes', value: data.inconclusive, color: '#6B7A80' },
    { name: 'Sin evaluar', value: data.pendingAlerts, color: '#DDE3E1' },
  ]
  const hasData = slices.some((slice) => slice.value > 0)

  return (
    <Panel
      title="Fiabilidad de alertas"
      icon={ShieldCheck}
      actions={
        data.insufficientSample ? (
          <Pill tone="Warning" dot={false}>
            <AlertTriangle className="h-3.5 w-3.5" />
            Muestra insuficiente · mín. {data.minimumSampleSize}
          </Pill>
        ) : (
          <Pill value="Normal">Muestra válida</Pill>
        )
      }
    >
      <div className="grid items-center gap-5 lg:grid-cols-[280px_1fr]">
        <div className="relative mx-auto h-64 w-64">
          {hasData ? (
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie isAnimationActive={false} data={slices} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="100%" paddingAngle={2} stroke="none">
                  {slices.map((slice) => (
                    <Cell key={slice.name} fill={slice.color} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div className="h-full w-full rounded-full border-[22px] border-line" />
          )}
          <div className="pointer-events-none absolute inset-0 grid place-items-center text-center">
            <div>
              <div className="numeral text-[40px] leading-none">{data.totalAlerts}</div>
              <div className="text-sm text-basalt-mute">alertas</div>
            </div>
          </div>
        </div>

        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
            <Stat value={pct(data.confirmedProportion)} caption={`Confirmadas · ${data.confirmed}`} tone="Normal" />
            <Stat value={pct(data.falseAlarmProportion)} caption={`Falsas alarmas · ${data.falseAlarms}`} tone="Critical" />
            <Stat value={pct(data.inconclusiveProportion)} caption={`No concluyentes · ${data.inconclusive}`} tone="neutral" />
            <Stat value={data.pendingAlerts} caption="Sin evaluar" tone="Warning" />
          </div>
          <div className="flex flex-wrap gap-3 text-sm text-basalt-mute">
            {slices.map((slice) => (
              <span key={slice.name} className="flex items-center gap-1.5">
                <span className="h-3 w-3 rounded-full" style={{ background: slice.color }} />
                {slice.name}
              </span>
            ))}
            <span className="ml-auto">Muestra evaluada: <span className="numeral text-basalt">{data.sampleSize}</span></span>
          </div>
        </div>
      </div>
    </Panel>
  )
}

function QueuePanel({ operationId, days }) {
  const { operatorName } = useDirectory()
  const [status, setStatus] = useState('AWAITING_REVIEW')
  const [page, setPage] = useState(1)
  const { data, loading, error } = useAsync(
    () => feedback.queue({ operationId, ...period(days), status, page, pageSize: PAGE_SIZE }),
    [operationId, days, status, page],
  )
  const pages = Math.max(1, Math.ceil((data?.total ?? 0) / PAGE_SIZE))
  const changeStatus = (value) => {
    setStatus(value)
    setPage(1)
  }

  return (
    <Panel
      title="Cola de revisión"
      icon={ClipboardCheck}
      actions={
        <Segmented
          value={status}
          onChange={changeStatus}
          options={['PENDING', 'AWAITING_REVIEW', 'REVIEWED'].map((value) => ({ value, label: label(value), count: value === status ? data?.total : undefined }))}
        />
      }
      bodyClass="p-2"
    >
      {!data && loading ? (
        <Spinner />
      ) : error?.status === 403 ? (
        <Forbidden />
      ) : error ? (
        <Empty icon={AlertTriangle} title={error.message} />
      ) : data?.items?.length ? (
        <>
          <ul className="space-y-1">
            {data.items.map((item) => (
              <li key={item.alertId}>
                <Link to={`/alertas?id=${item.alertId}`} className="flex items-center gap-3 rounded-lg px-2 py-2 hover:bg-mineral">
                  <span
                    className={`h-9 w-1.5 shrink-0 rounded-full ${
                      item.effectiveClassification === 'CONFIRMED'
                        ? 'bg-risk-normal'
                        : item.effectiveClassification === 'FALSE_ALARM'
                          ? 'bg-risk-critical'
                          : item.effectiveClassification
                            ? 'bg-basalt-mute'
                            : 'bg-line'
                    }`}
                  />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-[15px] font-semibold">{operatorName(item.operatorId)}</div>
                    <div className="text-[13px] text-basalt-mute">{dateTime(item.issuedAt)}</div>
                  </div>
                  {item.effectiveClassification && <Pill value={item.effectiveClassification} />}
                  <Pill value={item.status} />
                  <ChevronRight className="h-4 w-4 text-basalt-mute" />
                </Link>
              </li>
            ))}
          </ul>
          {pages > 1 && (
            <div className="flex items-center justify-end gap-2 px-2 pt-2">
              <IconButton icon={ChevronLeft} label="Página anterior" disabled={page <= 1} onClick={() => setPage(page - 1)} />
              <span className="numeral text-sm">{page} / {pages}</span>
              <IconButton icon={ChevronRight} label="Página siguiente" disabled={page >= pages} onClick={() => setPage(page + 1)} />
            </div>
          )}
        </>
      ) : (
        <Empty
          icon={Inbox}
          title="Nada en esta cola."
          action={status !== 'PENDING' && <Button variant="outline" onClick={() => changeStatus('PENDING')}>Ver sin revisar</Button>}
        />
      )}
    </Panel>
  )
}

export default function Reliability() {
  const { operationId } = useAuth()
  const navigate = useNavigate()
  const [days, setDays] = useState(30)

  if (!operationId) {
    return (
      <>
        <PageHeader icon={ShieldCheck} title="Fiabilidad" />
        <Panel>
          <Empty
            icon={Lock}
            title="Tu usuario no tiene una unidad minera asignada."
            action={<Button variant="outline" icon={Home} onClick={() => navigate('/')}>Ir al centro de control</Button>}
          />
        </Panel>
      </>
    )
  }

  return (
    <>
      <PageHeader icon={ShieldCheck} title="Fiabilidad">
        <Segmented value={days} onChange={setDays} options={PERIODS} />
      </PageHeader>
      <div className="space-y-5">
        <ReliabilityPanel operationId={operationId} days={days} />
        <QueuePanel operationId={operationId} days={days} />
      </div>
    </>
  )
}
