import { useCallback, useEffect, useMemo, useState } from 'react'
import { getFleetStatus } from '../services/fleetMonitoringService.js'

const riskOrder = { Critical: 0, Warning: 1, Normal: 2 }

export const byRiskThenName = (a, b) =>
  (riskOrder[a.currentRiskLevel] ?? 3) - (riskOrder[b.currentRiskLevel] ?? 3) ||
  String(a.fullName).localeCompare(String(b.fullName), 'es')

/**
 * Loads the monitored operators once and shares them across views, so alerts and
 * incidents can show an operator's name and vehicle instead of a raw id.
 */
export function useFleet() {
  const [operators, setOperators] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const reload = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getFleetStatus({})
      setOperators(Array.isArray(data) ? [...data].sort(byRiskThenName) : [])
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    getFleetStatus({})
      .then((data) => !cancelled && setOperators(Array.isArray(data) ? [...data].sort(byRiskThenName) : []))
      .catch((err) => !cancelled && setError(err.message))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  const byOperatorId = useMemo(
    () => Object.fromEntries(operators.map((operator) => [operator.operatorId, operator])),
    [operators],
  )

  const critical = useMemo(
    () => operators.filter((operator) => operator.currentRiskLevel === 'Critical').length,
    [operators],
  )

  return { operators, byOperatorId, critical, loading, error, reload }
}
