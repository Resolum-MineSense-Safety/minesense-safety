import { apiConfig } from './config.js'
import { createClient } from './http.js'

const client = createClient(apiConfig.fleet)
const path = '/api/v1/fleet-operators'

export const SHIFTS = ['Day', 'Night']
export const RISK_LEVELS = ['Normal', 'Warning', 'Critical']

/** @param {{ shift?: string, fleet?: string, location?: string, riskLevel?: string }} filters */
export const getFleetStatus = (filters = {}) => client.get(path, filters)

export const getMonitoredOperator = (operatorId) => client.get(`${path}/${operatorId}`)

/**
 * @param {{ operatorId: string, fullName: string, vehicleCode: string,
 *   fleet: string, shift: string, location: string }} data
 */
export const registerMonitoredOperator = (data) => client.post(path, data)

export const updateOperatorRiskLevel = (operatorId, riskLevel) =>
  client.put(`${path}/${operatorId}/risk-level`, { riskLevel })

export const reassignOperatorVehicle = (operatorId, vehicleCode, location) =>
  client.put(`${path}/${operatorId}/vehicle`, { vehicleCode, location })
