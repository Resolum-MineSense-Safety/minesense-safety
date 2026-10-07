import { useState } from 'react'
import { Button, ErrorMessage, Panel, SelectField, SuccessMessage, TextField } from '../components/ui.jsx'
import { useAsyncAction } from '../hooks/useAsyncAction.js'
import { roleLabels } from '../i18n/labels.js'
import { signIn, signUp, USER_ROLES } from '../services/identityAccessService.js'

const roleOptions = USER_ROLES.map((value) => ({ value, label: roleLabels[value] }))

export default function AuthView({ onSignedIn }) {
  const [mode, setMode] = useState('sign-in')

  return (
    <div className="mx-auto w-full max-w-md">
      <div role="tablist" aria-label="Acceso" className="mb-4 grid grid-cols-2 rounded-md border border-slate-800 p-1">
        {[
          ['sign-in', 'Iniciar sesión'],
          ['sign-up', 'Crear cuenta'],
        ].map(([value, text]) => (
          <button
            key={value}
            type="button"
            role="tab"
            aria-selected={mode === value}
            onClick={() => setMode(value)}
            className={`rounded px-3 py-1.5 text-sm font-medium transition ${
              mode === value ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            {text}
          </button>
        ))}
      </div>
      {mode === 'sign-in' ? (
        <SignInForm onSignedIn={onSignedIn} />
      ) : (
        <SignUpForm onSignedUp={() => setMode('sign-in')} />
      )}
    </div>
  )
}

function SignInForm({ onSignedIn }) {
  const [form, setForm] = useState({ username: '', password: '' })
  const { run, pending, error, clearError } = useAsyncAction(signIn)
  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    const user = await run(form)
    if (user) onSignedIn(user)
  }

  return (
    <Panel title="Iniciar sesión" description="Acceso para supervisores y personal de seguridad.">
      <form onSubmit={handleSubmit} className="flex flex-col gap-3">
        <TextField label="Usuario" value={form.username} onChange={update('username')} autoComplete="username" required />
        <TextField
          label="Contraseña"
          type="password"
          value={form.password}
          onChange={update('password')}
          autoComplete="current-password"
          required
        />
        <ErrorMessage message={error} onDismiss={clearError} />
        <Button type="submit" pending={pending}>
          Ingresar
        </Button>
      </form>
    </Panel>
  )
}

function SignUpForm({ onSignedUp }) {
  const [form, setForm] = useState({ username: '', fullName: '', password: '', role: 'Supervisor' })
  const [success, setSuccess] = useState(null)
  const { run, pending, error, clearError } = useAsyncAction(signUp)
  const update = (field) => (event) => setForm((prev) => ({ ...prev, [field]: event.target.value }))

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSuccess(null)
    const user = await run(form)
    if (user) {
      setSuccess(`Cuenta «${user.username}» creada. Ya puede iniciar sesión.`)
      setTimeout(onSignedUp, 1500)
    }
  }

  return (
    <Panel title="Crear cuenta" description="Registra un nuevo usuario del centro de control.">
      <form onSubmit={handleSubmit} className="flex flex-col gap-3">
        <TextField label="Usuario" value={form.username} onChange={update('username')} autoComplete="username" required />
        <TextField label="Nombre completo" value={form.fullName} onChange={update('fullName')} autoComplete="name" required />
        <TextField
          label="Contraseña"
          type="password"
          value={form.password}
          onChange={update('password')}
          autoComplete="new-password"
          required
        />
        <SelectField label="Rol" value={form.role} onChange={update('role')} options={roleOptions} />
        <ErrorMessage message={error} onDismiss={clearError} />
        <SuccessMessage message={success} />
        <Button type="submit" pending={pending}>
          Crear cuenta
        </Button>
      </form>
    </Panel>
  )
}
