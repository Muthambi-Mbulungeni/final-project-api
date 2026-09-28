import { Navigate, Route, Routes } from 'react-router-dom'
import DevRoleSwitcher from './components/dev/DevRoleSwitcher'
import AppShell from './components/layout/AppShell'
import { NAVIGATION } from './config/navigation'
import { getSession, ROLES } from './lib/session'
import AdminDashboard from './pages/admin/AdminDashboard'
import AuditLogPage from './pages/admin/AuditLogPage'
import UsersPage from './pages/admin/UsersPage'
import { AdminCasesPage, ConsultantCasesPage } from './pages/cases/CasesPage'
import ClientDashboard from './pages/client/ClientDashboard'
import ConsultantDashboard from './pages/consultant/ConsultantDashboard'
import LeadTriage from './pages/consultant/LeadTriage'
import EnquiryPage from './pages/EnquiryPage'
import ForgotPassword from './pages/ForgotPassword'
import Login from './pages/Login'
import ModulePlaceholder from './pages/ModulePlaceholder'
import NotFound from './pages/NotFound'
import RequireRole from './routes/RequireRole'

// Pages that have been built. Any navigation item not listed here shows a placeholder.
const MODULE_PAGES = {
  '/admin/users': UsersPage,
  '/admin/cases': AdminCasesPage,
  '/admin/audit-log': AuditLogPage,
  '/consultant/leads': LeadTriage,
  '/consultant/cases': ConsultantCasesPage,
}

function HomeRedirect() {
  const session = getSession()
  return <Navigate to={session ? ROLES[session.role].home : '/login'} replace />
}

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
