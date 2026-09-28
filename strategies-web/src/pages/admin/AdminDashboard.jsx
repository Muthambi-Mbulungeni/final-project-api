import { Link } from 'react-router-dom'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import { useApi } from '../../lib/useApi'

const LIST_LIMIT = 5

function getInitials(name = '') {
  return name
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

function pagedItems(request) {
  return Array.isArray(request.data?.data) ? request.data.data.slice(0, LIST_LIMIT) : []
}

export default function AdminDashboard() {
  const stats = useApi('/api/stats/admin')
  const auditLogs = useApi('/api/admin/audit-logs')
  const users = useApi('/api/admin/users')

  const auditEntries = pagedItems(auditLogs)
  const recentUsers = pagedItems(users)

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="System overview"
        description="Accounts, case volume and recent activity across the practice."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
      />

      {stats.error && <ErrorNotice className="mt-8" message={stats.error.message} onRetry={stats.reload} />}

      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        <StatTile label="Total users" value={stats.data?.totalUsers} />
        <StatTile label="Active cases" value={stats.data?.activeCases} />
        <StatTile label="System errors" value={stats.data?.systemErrors} />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <Panel
          title="Recent activity"
          className="lg:col-span-2"
          action={
            <Link to="/admin/audit-log" className="text-sm font-medium text-brand-700 hover:text-brand-800">
              View all
            </Link>
          }
        >
          <RequestState
            request={auditLogs}
            isEmpty={auditEntries.length === 0}
            emptyTitle="No activity recorded"
            emptyMessage="Changes to core records appear here."
            loadingText="Loading activity…"
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {auditEntries.map((entry, index) => (
                <li key={entry.id ?? index} className="py-3 text-sm text-stone-800">
                  {entry.action}
                </li>
              ))}
            </ul>
          </RequestState>
        </Panel>

        <Panel
          title="Recent users"
          action={
            <Link to="/admin/users" className="text-sm font-medium text-brand-700 hover:text-brand-800">
              View all
            </Link>
          }
        >
          <RequestState
            request={users}
            isEmpty={recentUsers.length === 0}
            emptyTitle="No users found"
            emptyMessage="Registered accounts appear here."
            loadingText="Loading users…"
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {recentUsers.map((user, index) => {
                const displayName = user.name ?? user.fullName ?? 'Unnamed user'
                return (
                  <li key={user.id ?? index} className="flex items-center gap-3 py-3">
                    <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-100 text-xs font-semibold text-stone-700">
                      {getInitials(displayName)}
                    </span>
                    <span className="truncate text-sm font-medium text-stone-900">{displayName}</span>
                  </li>
                )
              })}
            </ul>
          </RequestState>
        </Panel>
      </div>
    </>
  )
}
