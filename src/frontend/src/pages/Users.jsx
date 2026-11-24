import { Building2, HardHat, KeyRound, Plus, RefreshCw, ShieldCheck, ShieldOff, UserCheck, UserCog, Users as UsersIcon, UserX, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { identity } from '../api/services'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toasts'
import { Button, Drawer, Empty, Field, Input, PageHeader, Panel, Pill, Segmented, Select, Spinner, Stat, Table } from '../components/ui'
import { useDirectory } from '../lib/directory'
import { label } from '../lib/format'
import { useAction, useAsync } from '../lib/hooks'

// UserRole enum of IdentityAccessService.
const ROLES = [
  { value: 'Operator', label: 'Operador', icon: HardHat, tone: 'neutral' },
  { value: 'Supervisor', label: 'Supervisor', icon: UserCheck, tone: 'info' },
  { value: 'SafetyManager', label: 'Jefe de seguridad', icon: ShieldCheck, tone: 'Warning' },
  { value: 'Administrator', label: 'Administrador', icon: KeyRound, tone: 'accent' },
]
const ROLE_TONE = Object.fromEntries(ROLES.map((role) => [role.value, role.tone]))
const MIN_PASSWORD = 8

function Choice({ title, value, onChange, options, disabled }) {
  return (
    <div className="space-y-1">
      <span className="text-sm font-medium text-basalt-soft">{title}</span>
      <div role="radiogroup" aria-label={title} className="flex flex-wrap gap-2">
        {options.map((option) => {
          const Icon = option.icon
          const selected = value === option.value
          return (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={selected}
              disabled={disabled}
              onClick={() => onChange(option.value)}
              className={`inline-flex h-10 items-center gap-2 rounded-full border px-4 text-[15px] font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
                selected ? 'border-basalt bg-basalt text-white' : 'border-line bg-white text-basalt-soft hover:border-basalt-mute'
              }`}
            >
              {Icon && <Icon className="h-4 w-4" />}
              {option.label}
            </button>
          )
        })}
      </div>
    </div>
  )
}

const RolePill = ({ role }) => <Pill tone={ROLE_TONE[role] ?? 'neutral'}>{label(role)}</Pill>
const ActivePill = ({ active }) => <Pill value={active ? 'Active' : 'Inactive'} />

function UserDrawer({ user, me, onClose, onSaved }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const { units, unitName } = useDirectory()
  const [unitId, setUnitId] = useState('')
  const self = user.id === me.id
  const grantable = units.filter((unit) => !user.miningUnitIds.includes(unit.id))

  const act = async (key, fn, message) => {
    const result = await run(key, fn, message)
    if (result !== undefined) onSaved(result)
    return result
  }

  const changeRole = (role) => {
    if (role !== user.role) act('role', () => identity.changeRole(user.id, role), `Rol cambiado a ${label(role).toLowerCase()}.`)
  }

  const grant = async () => {
    const result = await act('grant', () => identity.grantUnit(user.id, unitId), 'Unidad minera asignada.')
    if (result !== undefined) setUnitId('')
  }

  return (
    <Drawer open title={user.fullName} onClose={onClose}>
      <div className="flex items-center gap-3 rounded-xl bg-mineral p-3">
        <span className="grid h-11 w-11 place-items-center rounded-full bg-basalt font-cond text-lg font-semibold text-haul">
          {user.fullName.split(' ').map((part) => part[0]).slice(0, 2).join('')}
        </span>
        <div className="min-w-0 flex-1">
          <div className="truncate font-semibold">{user.username}</div>
          <div className="mt-1 flex flex-wrap gap-1.5"><RolePill role={user.role} /><ActivePill active={user.isActive} /></div>
        </div>
      </div>

      <section className="rounded-xl border border-line p-4">
        <Choice title="Rol" value={user.role} onChange={changeRole} options={ROLES} disabled={busy === 'role' || !user.isActive} />
      </section>

      <section className="space-y-3 rounded-xl border border-line p-4">
        <h3 className="flex items-center gap-2 font-semibold"><Building2 className="h-4 w-4 text-basalt-mute" />Unidades mineras</h3>
        <div className="flex flex-wrap gap-2">
          {user.miningUnitIds.map((id) => (
            <span key={id} className="inline-flex h-8 items-center gap-1 rounded-full bg-ore-wash pl-3 pr-1 text-sm font-semibold text-ore">
              {unitName(id)}
              <button
                type="button"
                aria-label={`Quitar ${unitName(id)}`}
                title="Quitar"
                disabled={busy === `revoke-${id}`}
                onClick={() => act(`revoke-${id}`, () => identity.revokeUnit(user.id, id), 'Unidad minera retirada.')}
                className="grid h-6 w-6 place-items-center rounded-full hover:bg-ore/15 disabled:opacity-50"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </span>
          ))}
          {!user.miningUnitIds.length && <span className="text-sm text-basalt-mute">Sin unidades</span>}
        </div>
        {user.isActive && (
          <div className="flex gap-2">
            <Select value={unitId} onChange={(event) => setUnitId(event.target.value)} aria-label="Unidad minera"
              placeholder={grantable.length ? 'Elige una unidad' : 'Todas asignadas'}
              options={grantable.map((unit) => ({ value: unit.id, label: unit.name }))} />
            <Button icon={Plus} busy={busy === 'grant'} disabled={!unitId} onClick={grant}>Asignar</Button>
          </div>
        )}
      </section>

      {user.isActive && (
        <section className="flex items-center justify-between gap-3 rounded-xl border border-risk-critical/30 bg-risk-criticalWash p-4">
          <span className="text-sm text-risk-critical">{self ? 'No puedes desactivar tu propia cuenta.' : 'Ya no podrá iniciar sesión.'}</span>
          <Button variant="danger" icon={UserX} busy={busy === 'deactivate'} disabled={self}
            onClick={() => act('deactivate', () => identity.deactivate(user.id), 'Usuario desactivado.')}>
            Desactivar
          </Button>
        </section>
      )}
    </Drawer>
  )
}

function NewUserDrawer({ onClose, onSaved }) {
  const toast = useToast()
  const { busy, run } = useAction(toast)
  const [form, setForm] = useState({ username: '', fullName: '', password: '', role: 'Operator' })
  const set = (key) => (event) => setForm({ ...form, [key]: event.target.value })
  const valid = form.username.trim() && form.fullName.trim() && form.password.length >= MIN_PASSWORD

  const submit = async (event) => {
    event.preventDefault()
    if (!valid) return
    const body = { ...form, username: form.username.trim().toLowerCase(), fullName: form.fullName.trim() }
    const result = await run('create', () => identity.createUser(body), 'Usuario creado.')
    if (result !== undefined) onSaved()
  }

  return (
    <Drawer
      open
      title="Nuevo usuario"
      onClose={onClose}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button type="submit" form="user-form" busy={busy === 'create'} disabled={!valid}>Crear</Button>
        </>
      }
    >
      <form id="user-form" className="space-y-4" onSubmit={submit}>
        <Field label="Usuario"><Input value={form.username} onChange={set('username')} autoComplete="off" required /></Field>
        <Field label="Nombre completo"><Input value={form.fullName} onChange={set('fullName')} required /></Field>
        <Field label="Contraseña" hint={`Mínimo ${MIN_PASSWORD} caracteres`}>
          <Input type="password" value={form.password} onChange={set('password')} autoComplete="new-password" minLength={MIN_PASSWORD} required />
        </Field>
        <Choice title="Rol" value={form.role} onChange={(role) => setForm({ ...form, role })} options={ROLES} />
      </form>
    </Drawer>
  )
}

function UsersAdmin({ me }) {
  const { unitName, reload: reloadDirectory } = useDirectory()
  const { data, loading, reload, setData } = useAsync(() => identity.users(), [])
  const [role, setRole] = useState('all')
  const [selectedId, setSelectedId] = useState(null)
  const [creating, setCreating] = useState(false)

  const all = useMemo(() => data ?? [], [data])
  const rows = useMemo(
    () => all.filter((user) => role === 'all' || user.role === role).sort((a, b) => Number(b.isActive) - Number(a.isActive) || a.fullName.localeCompare(b.fullName)),
    [all, role],
  )
  const selected = all.find((user) => user.id === selectedId)
  const count = (value) => all.filter((user) => user.role === value).length
  const active = all.filter((user) => user.isActive).length

  const replace = (user) => {
    setData(all.map((item) => (item.id === user.id ? user : item)))
    reloadDirectory()
  }

  return (
    <>
      <PageHeader icon={UserCog} title="Usuarios">
        <Button variant="outline" icon={RefreshCw} onClick={reload} busy={loading}>Actualizar</Button>
        <Button icon={Plus} onClick={() => setCreating(true)}>Nuevo usuario</Button>
      </PageHeader>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat value={all.length} caption="Usuarios" tone="info" icon={UsersIcon} onClick={() => setRole('all')} />
        <Stat value={active} caption="Activos" tone="Normal" icon={UserCheck} />
        <Stat value={all.length - active} caption="Desactivados" tone={all.length - active ? 'Critical' : 'neutral'} icon={UserX} />
        <Stat value={count('Administrator')} caption="Administradores" tone="accent" icon={KeyRound} onClick={() => setRole('Administrator')} />
      </div>

      <div className="my-5">
        <Segmented
          value={role}
          onChange={setRole}
          options={[{ value: 'all', label: 'Todos', count: all.length }, ...ROLES.map((r) => ({ value: r.value, label: r.label, count: count(r.value) }))]}
        />
      </div>

      <Panel bodyClass="p-1">
        {!data && loading ? (
          <Spinner />
        ) : (
          <Table
            rows={rows}
            onRowClick={(user) => setSelectedId(user.id)}
            empty={<Empty icon={UsersIcon} title="Ningún usuario con este rol." action={<Button icon={Plus} onClick={() => setCreating(true)}>Nuevo usuario</Button>} />}
            columns={[
              {
                key: 'fullName',
                title: 'Nombre',
                render: (user) => (
                  <button type="button" onClick={(event) => { event.stopPropagation(); setSelectedId(user.id) }} className="text-left hover:underline">
                    <span className={`block font-semibold ${user.isActive ? '' : 'text-basalt-mute line-through'}`}>{user.fullName}</span>
                    <span className="block text-[13px] text-basalt-mute">{user.username}</span>
                  </button>
                ),
              },
              { key: 'role', title: 'Rol', render: (user) => <RolePill role={user.role} /> },
              { key: 'isActive', title: 'Estado', render: (user) => <ActivePill active={user.isActive} /> },
              {
                key: 'units',
                title: 'Unidades',
                className: 'hidden md:table-cell',
                render: (user) => (
                  <div className="flex flex-wrap gap-1">
                    {user.miningUnitIds.map((id) => (
                      <span key={id} className="rounded-full bg-ore-wash px-2 py-0.5 text-[13px] font-semibold text-ore">{unitName(id)}</span>
                    ))}
                    {!user.miningUnitIds.length && <span className="text-basalt-mute">—</span>}
                  </div>
                ),
              },
            ]}
          />
        )}
      </Panel>

      {selected && <UserDrawer key={selected.id} user={selected} me={me} onClose={() => setSelectedId(null)} onSaved={replace} />}
      {creating && (
        <NewUserDrawer
          onClose={() => setCreating(false)}
          onSaved={() => {
            setCreating(false)
            reload()
            reloadDirectory()
          }}
        />
      )}
    </>
  )
}

export default function Users() {
  const { user } = useAuth()
  if (user?.role !== 'Administrator') {
    return (
      <>
        <PageHeader icon={UserCog} title="Usuarios" />
        <Panel>
          <Empty
            icon={ShieldOff}
            title="Solo los administradores gestionan usuarios."
            action={<Link to="/" className="inline-flex h-10 items-center rounded-full bg-basalt px-4 text-[15px] font-semibold text-white hover:bg-basalt-soft">Ir al centro de control</Link>}
          />
        </Panel>
      </>
    )
  }
  return <UsersAdmin me={user} />
}
