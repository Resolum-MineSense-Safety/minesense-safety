import { apiConfig } from './config.js'
import { createClient } from './http.js'

const client = createClient(apiConfig.alerts)
const path = '/api/v1/alerts'

export const ALERT_SEVERITIES = ['Warning', 'Critical']
export const ALERT_STATUSES = ['Issued', 'Acknowledged', 'Escalated']

/** @param {{ operatorId: string, assessmentId: string, severity: string }} data */
export const issueAlert = (data) => client.post(path, data)

export const getAlertById = (alertId) => client.get(`${path}/${alertId}`)

export const getAlertsByOperator = (operatorId) => client.get(path, { operatorId })

export const acknowledgeAlert = (alertId) => client.post(`${path}/${alertId}/acknowledgements`)

export const escalateAlert = (alertId) => client.post(`${path}/${alertId}/escalations`)
