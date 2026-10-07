import { useId, useState } from 'react'
import { Button, EmptyState, ErrorMessage, Guid, Panel, RiskBadge, TextField } from '../components/ui.jsx'
import { isGuid, newGuid, useAsyncAction } from '../hooks/useAsyncAction.js'
import { formatDateTime, riskLevelLabels } from '../i18n/labels.js'
import { assessFatigue } from '../services/fatigueDetectionService.js'

const riskPanels = {
  Normal: 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300',
  Warning: 'border-amber-500/40 bg-amber-500/10 text-amber-300',
  Critical: 'border-red-500/50 bg-red-500/10 text-red-300',
}

const riskAdvice = {
  Normal: 'Sin signos relevantes de fatiga. Continuar el monitoreo.',
  Warning: 'Signos de fatiga moderada. Notificar al operador y vigilar la evolución.',
  Critical: 'Fatiga crítica. Detener el equipo de forma segura y relevar al operador.',
}

const initialForm = {
  operatorId: '',
  monitoringSessionId: '',
  perclos: '0.10',
  blinkRatePerMinute: '15',
  heartRateVariabilityMs: '45',
}

export default function FatigueSimulatorView() {
  const [form, setForm] = useState(initialForm)
  const [history, setHistory] = useState([])
  const { run, pending, error, clearError } = useAsyncAction(assessFatigue)

  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))
  const invalid = (value) => value !== '' && !isGuid(value)

  const handleSubmit = async (event) => {
    event.preventDefault()
    const assessment = await run({
      operatorId: form.operatorId.trim(),
      monitoringSessionId: form.monitoringSessionId.trim(),
      perclos: Number(form.perclos),
      blinkRatePerMinute: Number(form.blinkRatePerMinute),
      heartRateVariabilityMs: Number(form.heartRateVariabilityMs),
    })
    if (assessment) setHistory((prev) => [assessment, ...prev].slice(0, 10))
  }

  const latest = history[0]

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Panel
        title="Simulador de evaluación de fatiga"
        description="Envía señales biométricas al servicio de detección y obtiene el nivel de riesgo."
      >
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex items-end gap-2">
            <TextField
              label="ID del operador"
              className="flex-1"
              value={form.operatorId}
              onChange={update('operatorId')}
              required
              aria-invalid={invalid(form.operatorId) || undefined}
              hint={invalid(form.operatorId) ? 'Debe ser un GUID válido.' : undefined}
            />
            <Button variant="secondary" onClick={() => setForm((p) => ({ ...p, operatorId: newGuid() }))}>
              Generar
            </Button>
          </div>
          <div className="flex items-end gap-2">
            <TextField
              label="ID de la sesión de monitoreo"
              className="flex-1"
              value={form.monitoringSessionId}
              onChange={update('monitoringSessionId')}
              required
              aria-invalid={invalid(form.monitoringSessionId) || undefined}
              hint={invalid(form.monitoringSessionId) ? 'Debe ser un GUID válido.' : undefined}
            />
            <Button
              variant="secondary"
              onClick={() => setForm((p) => ({ ...p, monitoringSessionId: newGuid() }))}
            >
              Generar
            </Button>
          </div>

          <SignalSlider
            label="PERCLOS"
            unit="(0 – 1)"
            hint="Proporción del tiempo con los ojos cerrados (≥ 80 %)."
            min={0}
            max={1}
            step={0.01}
            value={form.perclos}
            onChange={update('perclos')}
          />
          <SignalSlider
            label="Frecuencia de parpadeo"
            unit="(parpadeos/min)"
            min={0}
            max={60}
            step={1}
            value={form.blinkRatePerMinute}
            onChange={update('blinkRatePerMinute')}
          />
          <SignalSlider
            label="Variabilidad de frecuencia cardiaca (HRV)"
            unit="(ms)"
            min={1}
            max={120}
            step={1}
            value={form.heartRateVariabilityMs}
            onChange={update('heartRateVariabilityMs')}
          />

          <ErrorMessage message={error} onDismiss={clearError} />
          <Button
            type="submit"
            pending={pending}
            disabled={invalid(form.operatorId) || invalid(form.monitoringSessionId)}
          >
            Evaluar fatiga
          </Button>
        </form>
      </Panel>

      <div className="flex flex-col gap-6">
        <Panel title="Resultado">
          {latest ? (
            <div
              role="status"
              aria-live="polite"
              className={`rounded-md border px-4 py-5 ${riskPanels[latest.riskLevel] ?? 'border-slate-700'}`}
            >
              <p className="text-xs uppercase tracking-wide text-slate-400">Nivel de riesgo</p>
              <p className="mt-1 text-4xl font-bold">{riskLevelLabels[latest.riskLevel] ?? latest.riskLevel}</p>
              <p className="mt-2 text-sm text-slate-200">{riskAdvice[latest.riskLevel]}</p>
              <dl className="mt-4 grid grid-cols-3 gap-3 text-sm">
                <Metric label="PERCLOS" value={latest.perclos.toFixed(2)} />
                <Metric label="Parpadeo" value={`${latest.blinkRatePerMinute}/min`} />
                <Metric label="HRV" value={`${latest.heartRateVariabilityMs} ms`} />
              </dl>
              <p className="mt-3 text-xs text-slate-400">
                Evaluación <Guid value={latest.id} /> · {formatDateTime(latest.assessedAt)}
              </p>
            </div>
          ) : (
            <EmptyState title="Aún no hay evaluaciones">
              Ajuste las señales y pulse «Evaluar fatiga» para ver el nivel de riesgo.
            </EmptyState>
          )}
        </Panel>

        {history.length > 1 && (
          <Panel title="Evaluaciones de esta sesión">
            <ul className="divide-y divide-slate-800 text-sm">
              {history.slice(1).map((item) => (
                <li key={item.id} className="flex items-center justify-between gap-3 py-2">
                  <span className="font-mono text-xs text-slate-400">
                    P {item.perclos.toFixed(2)} · B {item.blinkRatePerMinute} · HRV {item.heartRateVariabilityMs}
                  </span>
                  <RiskBadge level={item.riskLevel} />
                </li>
              ))}
            </ul>
          </Panel>
        )}
      </div>
    </div>
  )
}

function Metric({ label, value }) {
  return (
    <div>
      <dt className="text-xs text-slate-400">{label}</dt>
      <dd className="font-mono text-slate-100">{value}</dd>
    </div>
  )
}

function SignalSlider({ label, unit, hint, min, max, step, value, onChange }) {
  const id = useId()
  return (
    <div className="flex flex-col gap-1">
      <div className="flex items-baseline justify-between gap-2">
        <label htmlFor={id} className="text-xs font-medium uppercase tracking-wide text-slate-400">
          {label} <span className="normal-case text-slate-500">{unit}</span>
        </label>
        <input
          type="number"
          aria-label={`${label} (valor numérico)`}
          min={min}
          max={max}
          step={step}
          value={value}
          onChange={onChange}
          required
          className="w-24 rounded-md border border-slate-700 bg-slate-950 px-2 py-1 text-right font-mono text-sm text-slate-100 focus:border-amber-400 focus:outline-none focus:ring-1 focus:ring-amber-400"
        />
      </div>
      <input
        id={id}
        type="range"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={onChange}
        aria-describedby={hint ? `${id}-hint` : undefined}
        className="w-full accent-amber-400"
      />
      {hint && (
        <p id={`${id}-hint`} className="text-xs text-slate-500">
          {hint}
        </p>
      )}
    </div>
  )
}
