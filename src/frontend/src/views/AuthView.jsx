import { useState } from 'react'
import { Button, ErrorMessage, SelectField, SuccessMessage, TextField } from '../components/ui.jsx'
import { useAsyncAction } from '../hooks/useAsyncAction.js'
import { roleLabels } from '../i18n/labels.js'
import { signIn, signUp, USER_ROLES } from '../services/identityAccessService.js'

const roleOptions = USER_ROLES.map((value) => ({ value, label: roleLabels[value] }))

export default function AuthView({ onSignedIn }) {
  const [mode, setMode] = useState('sign-in')

  return (
    <div className="grid gap-10 lg:grid-cols-[1fr_24rem]">
      <header className="flex flex-col gap-3">
        <h1 className="stretch-expanded text-3xl font-bold md:text-4xl">
          {mode === 'sign-in' ? 'Ingrese con su cuenta del centro de control' : 'Cree una cuenta para el centro de control'}
        </h1>
        <p className="max-w-prose text-rock">
          Con la sesión iniciada, las asignaciones y las acciones de cada incidente quedan registradas a su nombre.
        </p>
      </header>

      <div className="rounded-md border border-rule bg-sheet p-5">
        {mode === 'sign-in' ? (
          <SignInForm onSignedIn={onSignedIn} />
        ) : (
          <SignUpForm onSignedUp={() => setMode('sign-in')} />
        )}
        <p className="mt-5 border-t border-rule pt-4 text-sm text-rock">
          {mode === 'sign-in' ? '¿Todavía no tiene cuenta? ' : '¿Ya tiene cuenta? '}
          <button
            type="button"
            className="font-semibold text-malachite underline-offset-4 hover:underline"
            onClick={() => setMode(mode === 'sign-in' ? 'sign-up' : 'sign-in')}
          >
            {mode === 'sign-in' ? 'Crear una cuenta' : 'Iniciar sesión'}
          </button>
        </p>
      </div>
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
    <form onSubmit={handleSubmit} className="flex flex-col gap-4" aria-label="Iniciar sesión">
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
        Iniciar sesión
      </Button>
    </form>
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
    <form onSubmit={handleSubmit} className="flex flex-col gap-4" aria-label="Crear cuenta">
      <TextField label="Usuario" value={form.username} onChange={update('username')} autoComplete="username" required />
      <TextField label="Nombre completo" value={form.fullName} onChange={update('fullName')} autoComplete="name" required />
      <TextField
        label="Contraseña"
        type="password"
        value={form.password}
        onChange={update('password')}
        autoComplete="new-password"
        hint="Al menos 8 caracteres."
        required
      />
      <SelectField label="Rol" value={form.role} onChange={update('role')} options={roleOptions} />
      <ErrorMessage message={error} onDismiss={clearError} />
      <SuccessMessage message={success} />
      <Button type="submit" pending={pending}>
        Crear cuenta
      </Button>
    </form>
  )
}
