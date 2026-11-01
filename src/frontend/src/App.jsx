import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import Shell from './components/Shell'
import { ToastProvider } from './components/Toasts'
import { DirectoryProvider } from './lib/directory'
import Alerts from './pages/Alerts'
import Analytics from './pages/Analytics'
import Cabin from './pages/Cabin'
import ControlCenter from './pages/ControlCenter'
import Devices from './pages/Devices'
import Edge from './pages/Edge'
import Fleet from './pages/Fleet'
import Incidents from './pages/Incidents'
import Login from './pages/Login'
import Operations from './pages/Operations'
import Privacy from './pages/Privacy'
import Reliability from './pages/Reliability'
import Reports from './pages/Reports'
import Services from './pages/Services'
import Users from './pages/Users'

function Routed() {
  const { user } = useAuth()
  if (!user) return <Login />
  return (
    <DirectoryProvider>
      <Routes>
        <Route element={<Shell />}>
          <Route index element={<ControlCenter />} />
          <Route path="flota" element={<Fleet />} />
          <Route path="cabina" element={<Cabin />} />
          <Route path="alertas" element={<Alerts />} />
          <Route path="incidentes" element={<Incidents />} />
          <Route path="operaciones" element={<Operations />} />
          <Route path="dispositivos" element={<Devices />} />
          <Route path="edge" element={<Edge />} />
          <Route path="analitica" element={<Analytics />} />
          <Route path="fiabilidad" element={<Reliability />} />
          <Route path="reportes" element={<Reports />} />
          <Route path="privacidad" element={<Privacy />} />
          <Route path="usuarios" element={<Users />} />
          <Route path="servicios" element={<Services />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </DirectoryProvider>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <Routed />
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}
