import { useId, useMemo, useState } from 'react'
import { Button, ErrorMessage, RiskMark, SelectField } from '../components/ui.jsx'
import { newGuid, useAsyncAction } from '../hooks/useAsyncAction.js'
import { formatDateTime } from '../i18n/labels.js'
import { assessFatigue } from '../services/fatigueDetectionService.js'

/**
 * Mirrors FatigueRiskPolicy in the Fatigue Detection service, only to preview the result
 * while the sliders move; the service's answer is the one that counts.
 */
function previewRisk({ perclos, blink, hrv }) {
  if (perclos >= 0.4 || (perclos >= 0.25 && hrv < 20)) return 'Critical'
  if (perclos >= 0.15 || blink >= 25 || hrv < 30) return 'Warning'
  return 'Normal'
}

const presets = [
  { id: 'alert', label: 'Atento', values: { perclos: 0.08, blink: 16, hrv: 48 } },
  { id: 'drowsy', label: 'Somnoliento', values: { perclos: 0.22, blink: 27, hrv: 28 } },
  { id: 'microsleep', label: 'Microsueño', values: { perclos: 0.55, blink: 8, hrv: 17 } },
]

const advice = {
  Normal: 'Sin signos relevantes de fatiga. El monitoreo continúa.',
  Warning: 'Fatiga moderada: se avisa al operador en cabina y se vigila la evolución.',
  Critical: 'Fatiga crítica: detener el equipo en un lugar seguro y relevar al operador.',
}

const numberFormat = (digits) => new Intl.NumberFormat('es-PE', { minimumFractionDigits: digits, maximumFractionDigits: digits })

export default function FatigueSimulatorView({ fleet }) {
  const [signals, setSignals] = useState(presets[0].values)
  const [operatorId, setOperatorId] = useState('')
  const [sessionId] = useState(newGuid)
  const [history, setHistory] = useState([])
  const { run, pending, error, clearError } = useAsyncAction(assessFatigue)

  const preview = useMemo(() => previewRisk(signals), [signals])
  const set = (field) => (value) => setSignals((prev) => ({ ...prev, [field]: value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    const assessment = await run({
      operatorId: operatorId || newGuid(),
      monitoringSessionId: sessionId,
      perclos: signals.perclos,
      blinkRatePerMinute: signals.blink,
      heartRateVariabilityMs: signals.hrv,
    })
    if (assessment) setHistory((prev) => [assessment, ...prev].slice(0, 8))
  }

  const latest = history[0]
  const operatorName = (id) => fleet.byOperatorId[id]?.fullName ?? 'Operador de prueba'

  return (
    <div className="flex flex-col gap-9">
      <header className="flex flex-col gap-3">
        <h1 className="stretch-expanded text-3xl font-bold md:text-4xl">Simulador de fatiga</h1>
        <p className="max-w-prose text-rock">
          Reproduzca las lecturas que envía el agente de cabina y compruebe cómo las clasifica el servicio de
          detección. Las marcas de cada escala son los umbrales que usa el servicio.
        </p>
      </header>

      <div className="grid gap-10 lg:grid-cols-[1fr_20rem]">
        <form onSubmit={handleSubmit} className="flex flex-col gap-7" aria-label="Lecturas biométricas">
          <div className="flex flex-wrap items-end gap-3">
            <SelectField
              label="Operador evaluado"
              value={operatorId}
              onChange={(event) => setOperatorId(event.target.value)}
              options={[
                { value: '', label: 'Operador de prueba (sin registrar)' },
                ...fleet.operators.map((op) => ({ value: op.operatorId, label: `${op.fullName} (${op.vehicleCode})` })),
              ]}
              className="min-w-[16rem] flex-1"
            />
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium text-ink-soft" id="presets-label">
                Cargar un caso
              </span>
              <div className="flex gap-1" role="group" aria-labelledby="presets-label">
                {presets.map((preset) => (
                  <Button key={preset.id} variant="secondary" onClick={() => setSignals(preset.values)}>
                    {preset.label}
                  </Button>
                ))}
              </div>
            </div>
          </div>

          <SignalScale
            label="Ojos cerrados (PERCLOS)"
            hint="Proporción del tiempo con los párpados cerrados en la última ventana de lectura."
            min={0}
            max={1}
            step={0.01}
            digits={2}
            value={signals.perclos}
            onChange={set('perclos')}
            zones={[
              { from: 0, to: 0.15, level: 'Normal' },
              { from: 0.15, to: 0.4, level: 'Warning' },
              { from: 0.4, to: 1, level: 'Critical' },
            ]}
            marks={[0.15, 0.25, 0.4]}
          />
          <SignalScale
            label="Parpadeos por minuto"
            hint="Un parpadeo muy frecuente también indica fatiga."
            min={0}
            max={60}
            step={1}
            digits={0}
            value={signals.blink}
            onChange={set('blink')}
            zones={[
              { from: 0, to: 25, level: 'Normal' },
              { from: 25, to: 60, level: 'Warning' },
            ]}
            marks={[25]}
          />
          <SignalScale
            label="Variabilidad cardiaca (ms)"
            hint="Por debajo de 20 ms, junto con PERCLOS de 0,25 o más, la fatiga es crítica."
            min={1}
            max={120}
            step={1}
            digits={0}
            value={signals.hrv}
            onChange={set('hrv')}
            zones={[
              { from: 1, to: 20, level: 'Critical' },
              { from: 20, to: 30, level: 'Warning' },
              { from: 30, to: 120, level: 'Normal' },
            ]}
            marks={[20, 30]}
          />

          <ErrorMessage message={error} onDismiss={clearError} />
          <div>
            <Button type="submit" pending={pending}>
              Enviar al servicio de detección
            </Button>
          </div>
        </form>

        <aside className="flex flex-col gap-6 lg:sticky lg:top-9 lg:self-start">
          <div className="rounded-md border border-rule bg-sheet p-5" aria-live="polite">
            <p className="text-sm text-rock">Con estas lecturas</p>
            <div className="mt-2">
              <RiskMark level={preview} size="lg" />
            </div>
            <p className="mt-2 text-sm">{advice[preview]}</p>
          </div>

          {latest && (
            <div role="status" className="border-l-[3px] border-ink pl-4">
              <p className="text-sm text-rock">Última respuesta del servicio</p>
              <div className="mt-1">
                <RiskMark level={latest.riskLevel} />
              </div>
              <p className="stretch-condensed mt-1 text-sm text-rock">
                {operatorName(latest.operatorId)}, {formatDateTime(latest.assessedAt)}
              </p>
            </div>
          )}

          {history.length > 1 && (
            <section>
              <h2 className="mb-2 font-semibold">Evaluaciones anteriores</h2>
              <ul className="flex flex-col">
                {history.slice(1).map((item) => (
                  <li key={item.id} className="flex items-center justify-between gap-3 border-b border-rule py-2">
                    <span className="stretch-condensed text-sm text-rock">
                      PERCLOS {numberFormat(2).format(item.perclos)}, {item.blinkRatePerMinute}/min,{' '}
                      {item.heartRateVariabilityMs} ms
                    </span>
                    <RiskMark level={item.riskLevel} />
                  </li>
                ))}
              </ul>
            </section>
          )}
        </aside>
      </div>
    </div>
  )
}

const zoneClass = { Normal: 'bg-rule', Warning: 'bg-ochre-bright', Critical: 'hazard-tape' }

function SignalScale({ label, hint, min, max, step, digits, value, onChange, zones, marks }) {
  const id = useId()
  const pct = (v) => ((v - min) / (max - min)) * 100
  const format = numberFormat(digits)
  const parse = (raw) => {
    const number = Number(String(raw).replace(',', '.'))
    return Number.isNaN(number) ? value : Math.min(max, Math.max(min, number))
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-end justify-between gap-4">
        <div>
          <label htmlFor={id} className="font-semibold">
            {label}
          </label>
          <p id={`${id}-hint`} className="text-sm text-rock">
            {hint}
          </p>
        </div>
        <input
          type="number"
          aria-label={`${label}, valor exacto`}
          min={min}
          max={max}
          step={step}
          value={value}
          onChange={(event) => onChange(parse(event.target.value))}
          className="stretch-expanded w-24 rounded border border-rule bg-sheet px-2 py-1 text-right text-xl font-semibold focus:border-malachite focus:outline-none focus:ring-2 focus:ring-malachite/25"
        />
      </div>
      <input
        id={id}
        type="range"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        aria-describedby={`${id}-hint`}
        aria-valuetext={format.format(value)}
        className="w-full accent-malachite"
      />
      <div className="relative" aria-hidden="true">
        <div className="flex h-2 gap-[2px] overflow-hidden rounded-sm">
          {zones.map((zone) => (
            <span key={zone.from} className={zoneClass[zone.level]} style={{ width: `${pct(zone.to) - pct(zone.from)}%` }} />
          ))}
        </div>
        <div className="relative mt-1 h-4">
          {marks.map((mark) => (
            <span
              key={mark}
              className="stretch-condensed absolute -translate-x-1/2 text-xs text-rock"
              style={{ left: `${pct(mark)}%` }}
            >
              {format.format(mark)}
            </span>
          ))}
        </div>
      </div>
    </div>
  )
}
