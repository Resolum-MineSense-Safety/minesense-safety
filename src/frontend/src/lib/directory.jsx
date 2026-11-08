import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { identity, operations } from '../api/services'
import { useAuth } from '../auth/AuthContext'
import { shortId } from './format'

// Names for the ids that every screen shows: operators, vehicles, fleets, mining units and users.
const DirectoryContext = createContext(null)

export function DirectoryProvider({ children }) {
  const { user } = useAuth()
  const [data, setData] = useState({ units: [], fleets: [], vehicles: [], operators: [], users: [] })

  const load = useCallback(async () => {
    if (!user) return null
    const [units, fleets, vehicles, operators] = await Promise.all([
      operations.units().catch(() => []),
      operations.fleets().catch(() => []),
      operations.vehicles().catch(() => []),
      operations.operators().catch(() => []),
    ])
    const users = user.role === 'Administrator' ? await identity.users().catch(() => []) : []
    return { units, fleets, vehicles, operators, users }
  }, [user])

  const reload = useCallback(async () => {
    const next = await load()
    if (next) setData(next)
  }, [load])

  useEffect(() => {
    let active = true
    load().then((next) => active && next && setData(next))
    return () => {
      active = false
    }
  }, [load])

  const value = useMemo(() => {
    const byId = (list) => Object.fromEntries(list.map((item) => [item.id, item]))
    const operatorsById = byId(data.operators)
    const usersById = byId(data.users)
    const vehiclesById = byId(data.vehicles)
    const fleetsById = byId(data.fleets)
    const unitsById = byId(data.units)
    return {
      ...data,
      reload,
      operatorName: (id) => operatorsById[id]?.fullName ?? usersById[id]?.fullName ?? shortId(id),
      userName: (id) =>
        id === user?.id ? user.fullName : usersById[id]?.fullName ?? operatorsById[id]?.fullName ?? shortId(id),
      vehicle: (id) => vehiclesById[id],
      fleetName: (id) => fleetsById[id]?.name ?? shortId(id),
      unitName: (id) => unitsById[id]?.name ?? shortId(id),
    }
  }, [data, reload, user])

  return <DirectoryContext.Provider value={value}>{children}</DirectoryContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export const useDirectory = () => useContext(DirectoryContext)
