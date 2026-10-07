import { apiConfig } from './config.js'
import { createClient } from './http.js'

const client = createClient(apiConfig.fatigue)
const path = '/api/v1/fatigue-assessments'

/**
 * @param {{ operatorId: string, monitoringSessionId: string, perclos: number,
 *   blinkRatePerMinute: number, heartRateVariabilityMs: number }} data
 */
export const assessFatigue = (data) => client.post(path, data)

export const getAssessmentById = (assessmentId) => client.get(`${path}/${assessmentId}`)

export const getAssessmentsByOperator = (operatorId) => client.get(path, { operatorId })

export const getLatestAssessment = (operatorId) => client.get(`${path}/latest`, { operatorId })
