import { useEffect, useState } from 'react'
import { useFleet } from './hooks/useFleet.js'
import { roleLabels } from './i18n/labels.js'
import AlertsView from './views/AlertsView.jsx'
import AuthView from './views/AuthView.jsx'
import FatigueSimulatorView from './views/FatigueSimulatorView.jsx'
import FleetStatusView from './views/FleetStatusView.jsx'
import IncidentsView from './views/IncidentsView.jsx'

const sections = [
  { id: 'fleet', label: 'Flota' },
  { id: 'alerts', label: 'Alertas' },
  { id: 'incidents', label: 'Incidentes' },
  { id: 'fatigue', label: 'Simulador de fatiga' },
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

const timeFormat = new Intl.DateTimeFormat('es-PE', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })

/** Mine shifts change at 07:00 and 19:00. */
function useShiftClock() {
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 30_000)
    return () => clearInterval(timer)
  }, [])
  const hour = now.getHours()
  return { shift: hour >= 7 && hour < 19 ? 'Turno día' : 'Turno noche', time: timeFormat.format(now) }
}

function ContourMark() {
  return (
    <svg viewBox="0 0 40 40" className="h-9 w-9 shrink-0" aria-hidden="true">
      <path d="M20 4c8 0 15 5 16 13s-5 17-15 18S4 30 4 20 12 4 20 4Z" fill="none" stroke="#7FB3A3" strokeWidth="1.6" />
      <path d="M20 10c5 0 10 3 10 9s-4 11-10 11-10-5-10-10 5-10 10-10Z" fill="none" stroke="#7FB3A3" strokeWidth="1.6" />
      <path d="M20 16c2.5 0 4.5 1.6 4.5 4s-2 4.5-4.5 4.5-4.5-2-4.5-4.3S17.6 16 20 16Z" fill="#E8ECE6" />
    </svg>
  )
}

export default function App() {
  const [active, setActive] = useState('fleet')
  const [user, setUser] = useState(readSession)
  const fleet = useFleet()
  const { shift, time } = useShiftClock()

  const handleSignedIn = (signedIn) => {
    setUser(signedIn)
    writeSession(signedIn)
    setActive('fleet')
  }

  const handleSignOut = () => {
    setUser(null)
    writeSession(null)
  }

  const navButton = (id, text, extra = null) => (
    <button
      type="button"
      onClick={() => setActive(id)}
      aria-current={active === id ? 'page' : undefined}
      className={`flex w-full items-center justify-between gap-2 whitespace-nowrap rounded px-3 py-2 text-left text-base transition-colors ${
        active === id
          ? 'bg-limestone font-semibold text-ink'
          : 'text-limestone/75 hover:bg-white/5 hover:text-limestone'
      }`}
    >
      {text}
      {extra}
    </button>
  )

  return (
    <div className="min-h-screen md:grid md:grid-cols-[15rem_1fr]">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-10 focus:rounded focus:bg-sheet focus:px-3 focus:py-2"
      >
        Saltar al contenido
      </a>

      <aside className="flex flex-col gap-6 bg-ink px-4 py-5 text-limestone md:sticky md:top-0 md:h-screen">
        <div className="flex items-center gap-3">
          <ContourMark />
          <div className="leading-tight">
            <p className="stretch-expanded text-base font-bold">MineSense</p>
            <p className="text-sm text-limestone/65">Centro de control</p>
          </div>
        </div>

        <nav aria-label="Secciones">
          <ul className="flex gap-1 overflow-x-auto [scrollbar-width:none] md:flex-col">
            {sections.map((section) => (
              <li key={section.id} className="shrink-0 md:shrink">
                {navButton(
                  section.id,
                  section.label,
                  section.id === 'fleet' && fleet.critical > 0 ? (
                    <span className="hazard-tape rounded-sm px-1.5 text-xs font-bold text-white" aria-label={`${fleet.critical} en riesgo crítico`}>
                      {fleet.critical}
                    </span>
                  ) : null,
                )}
              </li>
            ))}
          </ul>
        </nav>

        <div className="mt-auto hidden flex-col gap-4 md:flex">
          <div>
            <p className="stretch-expanded text-2xl font-semibold">{time}</p>
            <p className="text-sm text-limestone/65">{shift}</p>
          </div>
          <div className="border-t border-white/10 pt-4">
            {user ? (
              <>
                <p className="font-semibold">{user.username}</p>
                <p className="text-sm text-limestone/65">{roleLabels[user.role] ?? user.role}</p>
                <button type="button" onClick={handleSignOut} className="mt-2 text-sm text-[#9FD0C0] hover:underline">
                  Cerrar sesión
                </button>
              </>
            ) : (
              navButton('access', 'Iniciar sesión')
            )}
          </div>
        </div>
      </aside>

      <main id="main" className="min-w-0 px-5 py-7 md:px-10 md:py-9">
        <div className="mx-auto max-w-6xl">
          {active === 'fleet' && <FleetStatusView fleet={fleet} />}
          {active === 'alerts' && <AlertsView fleet={fleet} />}
          {active === 'incidents' && <IncidentsView fleet={fleet} currentUser={user} />}
          {active === 'fatigue' && <FatigueSimulatorView fleet={fleet} />}
          {active === 'access' && <AuthView onSignedIn={handleSignedIn} />}
          {active !== 'access' && !user && (
            <p className="mt-10 text-sm text-rock md:hidden">
              <button type="button" className="text-malachite underline" onClick={() => setActive('access')}>
                Iniciar sesión
              </button>{' '}
              para registrar acciones a su nombre.
            </p>
          )}
        </div>
      </main>
    </div>
  )
}
