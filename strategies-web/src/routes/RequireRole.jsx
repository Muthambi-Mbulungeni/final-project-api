import { Navigate, Outlet } from 'react-router-dom'
import { getSession, ROLES } from '../lib/session'

export default function RequireRole({ role }) {
  const session = getSession()

  if (!session) {
    return <Navigate to="/login" replace />
  }

  if (session.role !== role) {
    return <Navigate to={ROLES[session.role].home} replace />
  }

  return <Outlet />
}
