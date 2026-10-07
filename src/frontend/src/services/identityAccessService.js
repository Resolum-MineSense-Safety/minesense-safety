import { apiConfig } from './config.js'
import { createClient } from './http.js'

const client = createClient(apiConfig.iam)

export const USER_ROLES = ['Operator', 'Supervisor', 'SafetyManager', 'Administrator']

/** @param {{ username: string, fullName: string, password: string, role: string }} data */
export const signUp = (data) => client.post('/api/v1/authentication/sign-up', data)

/**
 * @param {{ username: string, password: string }} data
 * @returns {Promise<{ id: string, username: string, role: string }>}
 */
export const signIn = (data) => client.post('/api/v1/authentication/sign-in', data)

export const getUsers = (role) => client.get('/api/v1/users', { role })

export const getUserById = (userId) => client.get(`/api/v1/users/${userId}`)
