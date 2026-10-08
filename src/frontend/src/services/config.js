const env = import.meta.env

/** Base URL per bounded context; defaults match the Vite dev proxy prefixes. */
export const apiConfig = {
  iam: env.VITE_IAM_API_URL || '/iam',
  fatigue: env.VITE_FATIGUE_API_URL || '/fatigue',
  alerts: env.VITE_ALERTS_API_URL || '/alerts',
  fleet: env.VITE_FLEET_API_URL || '/fleet',
  incidents: env.VITE_INCIDENTS_API_URL || '/incidents',
}
