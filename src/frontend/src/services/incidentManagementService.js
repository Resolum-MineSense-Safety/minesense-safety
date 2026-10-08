import { apiConfig } from './config.js'
import { createClient } from './http.js'

const client = createClient(apiConfig.incidents)
const path = '/api/v1/incidents'

export const INCIDENT_STATUSES = ['Pending', 'Assigned', 'Escalated', 'Closed']

/** @param {{ alertId: string, operatorId: string }} data */
export const openIncident = (data) => client.post(path, data)

export const getIncidentsByStatus = (status) => client.get(path, { status })

export const getIncidentById = (incidentId) => client.get(`${path}/${incidentId}`)

export const assignIncident = (incidentId, supervisorId) =>
  client.post(`${path}/${incidentId}/assignment`, { supervisorId })

/** @param {{ supervisorId: string, description: string, outcome: string }} data */
export const registerIncidentAction = (incidentId, data) =>
  client.post(`${path}/${incidentId}/actions`, data)

export const escalateIncident = (incidentId) => client.post(`${path}/${incidentId}/escalation`)

export const closeIncident = (incidentId, resolution) =>
  client.post(`${path}/${incidentId}/closure`, { resolution })
