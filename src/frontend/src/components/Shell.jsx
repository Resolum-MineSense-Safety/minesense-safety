import {
  BarChart3, Bell, Building2, Cpu, FileText, Gauge, LogOut, Radio, Server, ShieldCheck, Siren,
  ThumbsUp, Truck, Users, Waves,
} from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { Logo, LogoMark } from './Logo'
import { label } from '../lib/format'

const NAV = [
  {
    group: 'Turno',
    items: [
      { to: '/', icon: Gauge, text: 'Centro de control', end: true },
      { to: '/flota', icon: Truck, text: 'Flota' },
      { to: '/cabina', icon: Waves, text: 'Cabina' },
      { to: '/alertas', icon: Bell, text: 'Alertas' },
      { to: '/incidentes', icon: Siren, text: 'Incidentes' },
    ],
  },
  {
    group: 'Mina',
    items: [
      { to: '/operaciones', icon: Building2, text: 'Operaciones' },
      { to: '/dispositivos', icon: Cpu, text: 'Dispositivos' },
      { to: '/edge', icon: Radio, text: 'Edge' },
    ],
  },
  {
    group: 'Análisis',
    items: [
      { to: '/analitica', icon: BarChart3, text: 'Tendencias' },
      { to: '/fiabilidad', icon: ThumbsUp, text: 'Fiabilidad' },
      { to: '/reportes', icon: FileText, text: 'Reportes' },
    ],
  },
  {
    group: 'Gobierno',
    items: [
      { to: '/privacidad', icon: ShieldCheck, text: 'Privacidad' },
      { to: '/usuarios', icon: Users, text: 'Usuarios', roles: ['Administrator'] },
      { to: '/servicios', icon: Server, text: 'Servicios' },
    ],
  },
]

export default function Shell() {
  const { user, signOut } = useAuth()
  return (
    <div className="flex min-h-svh">
      {/* The outer rail stretches with the page; the inner column stays in view while scrolling. */}
      <aside className="hidden w-60 shrink-0 bg-basalt text-white md:block">
       <div className="sticky top-0 flex h-svh flex-col">
        <div className="px-5 py-5">
          <Logo size={40} />
        </div>
        <nav className="flex-1 space-y-5 overflow-y-auto px-3 pb-4">
          {NAV.map((section) => (
            <div key={section.group}>
              <div className="px-3 pb-1.5 text-xs font-medium text-white/40">{section.group}</div>
              {section.items
                .filter((item) => !item.roles || item.roles.includes(user?.role))
                .map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.end}
                    className={({ isActive }) =>
                      `flex items-center gap-3 rounded-lg px-3 py-2 text-[15px] font-medium transition-colors ${
                        isActive ? 'bg-haul text-basalt' : 'text-white/75 hover:bg-white/10 hover:text-white'
                      }`
                    }
                  >
                    <item.icon className="h-[18px] w-[18px]" />
                    {item.text}
                  </NavLink>
                ))}
            </div>
          ))}
        </nav>
        <div className="flex items-center gap-3 border-t border-white/10 px-4 py-4">
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-white/10 font-cond text-lg font-semibold">
            {user?.fullName?.[0] ?? '?'}
          </span>
          <div className="min-w-0 flex-1 leading-tight">
            <div className="truncate text-sm font-semibold">{user?.fullName}</div>
            <div className="text-xs text-white/50">{label(user?.role)}</div>
          </div>
          <button type="button" onClick={signOut} aria-label="Cerrar sesión" title="Cerrar sesión" className="rounded-full p-2 text-white/60 hover:bg-white/10 hover:text-white">
            <LogOut className="h-4 w-4" />
          </button>
        </div>
       </div>
      </aside>

      <div className="min-w-0 flex-1">
        {/* Compact top navigation for small screens */}
        <header className="sticky top-0 z-30 flex items-center gap-2 overflow-x-auto bg-basalt px-3 py-2 md:hidden">
          <LogoMark size={36} />
          {NAV.flatMap((section) => section.items)
            .filter((item) => !item.roles || item.roles.includes(user?.role))
            .map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                aria-label={item.text}
                className={({ isActive }) => `grid h-9 w-9 shrink-0 place-items-center rounded-lg ${isActive ? 'bg-haul text-basalt' : 'text-white/70'}`}
              >
                <item.icon className="h-[18px] w-[18px]" />
              </NavLink>
            ))}
        </header>
        <main className="mx-auto max-w-[1400px] px-4 py-6 md:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
