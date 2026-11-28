// Live telemetry of the cabin gateways (fleet IoT simulator, /api/v1/telemetry): shared helpers.
import { Radio, WifiOff } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { telemetry } from '../api/services'

/** Same thresholds as FatigueRiskPolicy: colors a live point before the cloud assesses it. */
export function riskOf(point) {
  if (!point || point.perclos == null) return 'neutral'
  const { perclos, blinkRatePerMinute: blink = 0, heartRateVariabilityMs: hrv = 60 } = point
  if (perclos >= 0.4 || (perclos >= 0.25 && hrv != null && hrv < 20)) return 'Critical'
  if (perclos >= 0.15 || blink >= 25 || (hrv != null && hrv < 30)) return 'Warning'
  return 'Normal'
}

/**
 * Polls the gateway while the tab is visible. `pause()` freezes the view (the gateway keeps
 * transmitting), as the streaming-chart guideline asks for a pause control.
 */
export function useTelemetry({ operatorId, interval = 2000, history = 'short' } = {}) {
  const [state, setState] = useState({ data: undefined, error: null, receivedAt: null })
  const [paused, setPaused] = useState(false)
  const timer = useRef(null)

  const load = useCallback(async () => {
    try {
      const data = operatorId ? await telemetry.cabin(operatorId) : await telemetry.cabins(history)
      setState({ data, error: null, receivedAt: Date.now() })
    } catch (error) {
      setState((current) => ({ ...current, error }))
    }
  }, [operatorId, history])

  useEffect(() => {
    if (paused) return undefined
    load()
    const tick = () => {
      if (document.visibilityState === 'visible') load()
    }
    timer.current = setInterval(tick, interval)
    return () => clearInterval(timer.current)
  }, [load, interval, paused])

  return { ...state, paused, setPaused, reload: load }
}

/** Line colors of the live charts (risk palette of tailwind.config.js). */
export const STROKE = { Critical: '#D6362F', Warning: '#E58A00', Normal: '#2E9E6B', neutral: '#5E6D73', info: '#1F7A8C' }

/** Human label of a cabin gateway state. */
export const GATEWAY_STATE = {
  streaming: { text: 'Transmitiendo', tone: 'Normal', icon: Radio },
  offline: { text: 'Sin cobertura', tone: 'Warning', icon: WifiOff },
  idle: { text: 'Sin sesión', tone: 'neutral', icon: Radio },
}
