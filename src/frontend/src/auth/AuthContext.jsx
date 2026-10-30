import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { loadSession, onSessionChange, saveSession, toSession } from '../api/client'
import { identity } from '../api/services'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [session, setSession] = useState(loadSession)
  useEffect(() => onSessionChange(setSession), [])

  const value = useMemo(
    () => ({
      user: session?.user ?? null,
      // Mining unit of the session: the operation scope used by Feedback & Reliability.
      operationId: session?.user?.miningUnitIds?.[0] ?? null,
      async signIn(username, password) {
        const auth = await identity.signIn(username, password)
        saveSession(toSession(auth))
      },
      async signOut() {
        const current = loadSession()
        if (current?.refreshToken) await identity.signOut(current.refreshToken).catch(() => {})
        saveSession(null)
      },
    }),
    [session],
  )
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => useContext(AuthContext)
