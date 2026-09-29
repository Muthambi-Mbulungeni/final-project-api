import { useEffect } from 'react'
import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import DevRoleSwitcher from './components/dev/DevRoleSwitcher'
import AppShell from './components/layout/AppShell'
import SessionCheck from './components/layout/SessionCheck'
import { NAVIGATION } from './config/navigation'
import { onSessionExpired } from './lib/api'
import { ROLES } from './lib/session'
import { useRestoredSession } from './lib/useSession'
import AdminDashboard from './pages/admin/AdminDashboard'
import ClientDashboard from './pages/client/ClientDashboard'
import ConsultantDashboard from './pages/consultant/ConsultantDashboard'
import EnquiryPage from './pages/EnquiryPage'
import ForgotPassword from './pages/ForgotPassword'
import Login from './pages/Login'
import ModulePlaceholder from './pages/ModulePlaceholder'
import NotFound from './pages/NotFound'
import { MODULE_PAGES } from './routes/modulePages'
import RequireRole from './routes/RequireRole'

function HomeRedirect() {
  const { session, checking } = useRestoredSession()
  if (checking) return <SessionCheck />
  return <Navigate to={session ? ROLES[session.role].home : '/login'} replace />
}

// Built pages get their component. Unbuilt ones are hidden from the sidebar,
// and anyone typing their URL sees a neutral placeholder instead of a 404.
function moduleRoutes(role) {
  return NAVIGATION[role]
    .filter((item) => !item.end)
    .map((item) => {
      const Page = MODULE_PAGES[item.to]
      return (
        <Route
          key={item.to}
          path={item.to.split('/').pop()}
          element={Page ? <Page /> : <ModulePlaceholder title={item.label} description={item.description} />}
        />
      )
    })
}

export default function App() {
  const navigate = useNavigate()

  // When a session can no longer be renewed, go to sign-in and come back to this page afterwards.
  useEffect(() => {
    onSessionExpired(() => {
      navigate('/login', {
        replace: true,
        state: { reason: 'expired', from: `${window.location.pathname}${window.location.search}` },
      })
    })
    return () => onSessionExpired(null)
  }, [navigate])

  return (
    <>
      <Routes>
        <Route path="/" element={<HomeRedirect />} />
        <Route path="/login" element={<Login />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/enquire" element={<EnquiryPage />} />

        <Route element={<RequireRole role="admin" />}>
          <Route path="/admin" element={<AppShell role="admin" />}>
            <Route index element={<AdminDashboard />} />
            {moduleRoutes('admin')}
          </Route>
        </Route>

        <Route element={<RequireRole role="consultant" />}>
          <Route path="/consultant" element={<AppShell role="consultant" />}>
            <Route index element={<ConsultantDashboard />} />
            {moduleRoutes('consultant')}
          </Route>
        </Route>

        <Route element={<RequireRole role="client" />}>
          <Route path="/client" element={<AppShell role="client" />}>
            <Route index element={<ClientDashboard />} />
            {moduleRoutes('client')}
          </Route>
        </Route>

        <Route path="*" element={<NotFound />} />
      </Routes>

      <DevRoleSwitcher />
    </>
  )
}
