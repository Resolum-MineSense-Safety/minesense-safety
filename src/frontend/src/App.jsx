import { lazy } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import Shell from './components/Shell'
import { ToastProvider } from './components/Toasts'
import { DirectoryProvider } from './lib/directory'
import ControlCenter from './pages/ControlCenter'
import Login from './pages/Login'

// Each module loads on first visit (code splitting): the login and the control center only
// download what they show (Lighthouse, section 4.4.5).
const Alerts = lazy(() => import('./pages/Alerts'))
const Analytics = lazy(() => import('./pages/Analytics'))
const Cabin = lazy(() => import('./pages/Cabin'))
const Devices = lazy(() => import('./pages/Devices'))
const Edge = lazy(() => import('./pages/Edge'))
const Fleet = lazy(() => import('./pages/Fleet'))
const Incidents = lazy(() => import('./pages/Incidents'))
const Operations = lazy(() => import('./pages/Operations'))
const Privacy = lazy(() => import('./pages/Privacy'))
const Reliability = lazy(() => import('./pages/Reliability'))
const Reports = lazy(() => import('./pages/Reports'))
const Services = lazy(() => import('./pages/Services'))
const Users = lazy(() => import('./pages/Users'))

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
