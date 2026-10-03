import { Navigate, Outlet, useLocation } from 'react-router-dom'
import SessionCheck from '../components/layout/SessionCheck'
import { ROLES } from '../lib/session'
import { useRestoredSession } from '../lib/useSession'

// Opens a role's workspace only for that role. This is for the user's benefit:
// the API checks the token and role on every request itself.
export default function RequireRole({ role }) {
  const { session, checking } = useRestoredSession()
  const location = useLocation()

  if (checking) return <SessionCheck />

  if (!session) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />
  }

  if (session.role !== role) {
    return <Navigate to={ROLES[session.role].home} replace />
  }

  return <Outlet />
}
