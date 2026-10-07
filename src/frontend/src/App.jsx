import { useState } from 'react'
import { Button } from './components/ui.jsx'
import { roleLabels } from './i18n/labels.js'
import AlertsView from './views/AlertsView.jsx'
import AuthView from './views/AuthView.jsx'
import FatigueSimulatorView from './views/FatigueSimulatorView.jsx'
import FleetStatusView from './views/FleetStatusView.jsx'
import IncidentsView from './views/IncidentsView.jsx'

const sections = [
  { id: 'fleet', label: 'Estado de flota' },
  { id: 'alerts', label: 'Alertas' },
  { id: 'incidents', label: 'Incidentes' },
  { id: 'fatigue', label: 'Simulador de fatiga' },
  { id: 'access', label: 'Acceso' },
]

const SESSION_KEY = 'minesense.session'

function readSession() {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

function writeSession(user) {
  try {
    if (user) sessionStorage.setItem(SESSION_KEY, JSON.stringify(user))
    else sessionStorage.removeItem(SESSION_KEY)
  } catch {
    // Storage may be unavailable (private mode); the session simply won't persist.
  }
}

export default function App() {
  const [active, setActive] = useState('fleet')
  const [user, setUser] = useState(readSession)

  const handleSignedIn = (signedIn) => {
    setUser(signedIn)
    writeSession(signedIn)
    setActive('fleet')
  }

  const handleSignOut = () => {
    setUser(null)
    writeSession(null)
  }

  const current = sections.find((section) => section.id === active)

  return (
    <div className="flex min-h-screen flex-col">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-10 focus:rounded focus:bg-amber-400 focus:px-3 focus:py-2 focus:text-slate-950"
      >
        Saltar al contenido
      </a>
      <header className="border-b border-slate-800 bg-slate-900">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4 py-3">
          <div className="flex items-center gap-3">
            <img src="/favicon.svg" alt="" className="h-8 w-8" />
            <div>
              <p className="text-sm font-bold tracking-wide text-white">MineSense Safety</p>
              <p className="text-xs text-slate-400">Centro de control de fatiga</p>
            </div>
          </div>
          <div className="flex items-center gap-3 text-sm">
            {user ? (
              <>
                <span className="text-slate-300">
                  <span className="font-medium text-white">{user.username}</span>
                  <span className="text-slate-500"> · {roleLabels[user.role] ?? user.role}</span>
                </span>
                <Button variant="ghost" onClick={handleSignOut}>
                  Cerrar sesión
                </Button>
              </>
            ) : (
              <Button variant="secondary" onClick={() => setActive('access')}>
                Iniciar sesión
              </Button>
            )}
          </div>
        </div>
        <nav aria-label="Secciones" className="mx-auto max-w-7xl overflow-x-auto px-4">
          <ul className="flex gap-1">
            {sections.map((section) => (
              <li key={section.id}>
                <button
                  type="button"
                  onClick={() => setActive(section.id)}
                  aria-current={active === section.id ? 'page' : undefined}
                  className={`whitespace-nowrap border-b-2 px-3 py-2.5 text-sm font-medium transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-amber-300 ${
                    active === section.id
                      ? 'border-amber-400 text-white'
                      : 'border-transparent text-slate-400 hover:text-slate-200'
                  }`}
                >
                  {section.label}
                </button>
              </li>
            ))}
          </ul>
        </nav>
      </header>

      <main id="main" className="mx-auto w-full max-w-7xl flex-1 px-4 py-6">
        <h1 className="mb-5 text-lg font-semibold text-white">{current.label}</h1>
        {active === 'fleet' && <FleetStatusView />}
        {active === 'alerts' && <AlertsView />}
        {active === 'incidents' && <IncidentsView currentUser={user} />}
        {active === 'fatigue' && <FatigueSimulatorView />}
        {active === 'access' && <AuthView onSignedIn={handleSignedIn} />}
      </main>

      <footer className="border-t border-slate-800 px-4 py-3 text-center text-xs text-slate-500">
        MineSense Safety · Prevención de fatiga en operadores de maquinaria pesada
      </footer>
    </div>
  )
}
