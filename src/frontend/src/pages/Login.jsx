import { Activity, LogIn } from 'lucide-react'
import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { Button } from '../components/ui'

// Demo accounts created by scripts/seed-demo.mjs (admin comes from the Development profile).
const DEMO = [
  { username: 'admin', password: 'Admin-Local-2026', role: 'Administrador' },
  { username: 'supervisor', password: 'Supervisor-2026', role: 'Supervisor' },
  { username: 'jefe.seguridad', password: 'Seguridad-2026', role: 'Jefe de seguridad' },
]

export default function Login() {
  const { signIn } = useAuth()
  const [form, setForm] = useState({ username: '', password: '' })
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await signIn(form.username.trim(), form.password)
    } catch (err) {
      setError(err.status === 401 ? 'Usuario o contraseña incorrectos.' : err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="grid min-h-svh md:grid-cols-[1.1fr_1fr]">
      <section className="relative hidden overflow-hidden bg-basalt p-12 text-white md:flex md:flex-col md:justify-between">
        <div className="flex items-center gap-3">
          <span className="grid h-12 w-12 place-items-center rounded-2xl bg-haul">
            <Activity className="h-6 w-6 text-basalt" strokeWidth={2.5} />
          </span>
          <span className="font-cond text-2xl font-semibold">MineSense Safety</span>
        </div>

        {/* Three cab beacons: the states the control center watches */}
        <div className="space-y-5">
          {[
            { color: 'bg-risk-normal', width: 'w-[62%]' },
            { color: 'bg-risk-warning', width: 'w-[44%]' },
            { color: 'bg-risk-critical animate-beacon', width: 'w-[28%]' },
          ].map((bar) => (
            <div key={bar.color} className="flex items-center gap-4">
              <span className={`h-4 w-4 rounded-full ${bar.color}`} />
              <span className={`h-3 rounded-full bg-white/10 ${bar.width}`} />
            </div>
          ))}
          <h1 className="max-w-md pt-6 font-cond text-5xl font-semibold leading-[1.05]">Cada cabina, a la vista.</h1>
        </div>

        <span className="pointer-events-none absolute -bottom-24 -right-24 h-80 w-80 rounded-full border-[40px] border-haul/10" />
      </section>

      <section className="flex items-center justify-center p-6">
        <form onSubmit={submit} className="w-full max-w-sm space-y-5">
          <h2 className="font-cond text-3xl font-semibold">Iniciar sesión</h2>
          <input
            className="field h-12"
            placeholder="Usuario"
            autoComplete="username"
            value={form.username}
            onChange={(event) => setForm({ ...form, username: event.target.value })}
          />
          <input
            className="field h-12"
            type="password"
            placeholder="Contraseña"
            autoComplete="current-password"
            value={form.password}
            onChange={(event) => setForm({ ...form, password: event.target.value })}
          />
          {error && <p className="rounded-lg bg-risk-criticalWash px-3 py-2 text-sm font-medium text-risk-critical">{error}</p>}
          <Button type="submit" size="lg" icon={LogIn} busy={busy} className="w-full" disabled={!form.username || !form.password}>
            Entrar
          </Button>

          <div className="space-y-2 pt-4">
            <div className="text-sm text-basalt-mute">Cuentas de demostración</div>
            <div className="flex flex-wrap gap-2">
              {DEMO.map((account) => (
                <button
                  key={account.username}
                  type="button"
                  onClick={() => setForm({ username: account.username, password: account.password })}
                  className="rounded-full border border-line bg-white px-3 py-1.5 text-sm font-medium hover:border-basalt-mute"
                >
                  {account.role}
                </button>
              ))}
            </div>
          </div>
        </form>
      </section>
    </div>
  )
}
