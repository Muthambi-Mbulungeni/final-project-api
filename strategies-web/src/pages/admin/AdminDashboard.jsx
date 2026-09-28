import { Link } from 'react-router-dom'
import { Briefcase, Inbox, ShieldAlert, Users } from 'lucide-react'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { formatDate, getInitials } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const LIST_LIMIT = 5

function hasValue(value) {
  return value !== undefined && value !== null
}

export default function AdminDashboard() {
  const stats = useApi('/api/stats/admin')
  const auditLogs = useApi('/api/admin/audit-logs')
  const users = useApi('/api/admin/users')

  // When every request fails (e.g. the API is down), show one banner instead of three.
  const requests = [stats, auditLogs, users]
  const allFailed = requests.every((request) => request.error)

  function retryAll() {
    requests.forEach((request) => request.reload())
  }

  const auditEntries = listItems(auditLogs.data).slice(0, LIST_LIMIT)
  const recentUsers = listItems(users.data).slice(0, LIST_LIMIT)

  // pendingLeads is in frontend-contracts.md but not yet returned by the API,
  // so its tile only appears once the field is present.
  const tiles = [
    { label: 'Total users', value: stats.data?.totalUsers, icon: Users, to: '/admin/users' },
    { label: 'Active cases', value: stats.data?.activeCases, icon: Briefcase, to: '/admin/cases' },
    { label: 'Pending leads', value: stats.data?.pendingLeads, icon: Inbox, to: '/admin/leads', optional: true },
    { label: 'System errors', value: stats.data?.systemErrors, icon: ShieldAlert },
  ].filter((tile) => !tile.optional || hasValue(tile.value))

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

      {stats.error && (
        <ErrorNotice
          className="mt-8"
          message={stats.error.message}
          onRetry={allFailed ? retryAll : stats.reload}
        />
      )}

      <div className={`mt-8 grid gap-4 ${tiles.length > 3 ? 'sm:grid-cols-2 xl:grid-cols-4' : 'sm:grid-cols-3'}`}>
        {tiles.map((tile) => (
          <StatTile
            key={tile.label}
            label={tile.label}
            value={tile.value}
            icon={tile.icon}
            to={tile.to}
            loading={stats.loading}
          />
        ))}
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
            hideError={allFailed}
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {auditEntries.map((entry, index) => (
                <li key={entry.id ?? index} className="flex items-center justify-between gap-4 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-900">{entry.action}</p>
                    {entry.entityType && <p className="text-xs text-stone-500">{entry.entityType}</p>}
                  </div>
                  <span className="shrink-0 text-xs text-stone-500 tabular-nums">{formatDate(entry.createdAt)}</span>
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
            hideError={allFailed}
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {recentUsers.map((user, index) => {
                const displayName = user.fullName ?? user.name ?? 'Unnamed user'
                return (
                  <li key={user.id ?? index} className="flex items-center gap-3 py-3">
                    <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-100 text-xs font-semibold text-stone-700">
                      {getInitials(displayName)}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium text-stone-900">{displayName}</p>
                      {user.email && <p className="truncate text-xs text-stone-500">{user.email}</p>}
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      {hasValue(user.role) && <StatusBadge type="userRole" value={user.role} />}
                      {user.isActive === false && <span className="text-xs font-medium text-brand-700">Inactive</span>}
                    </div>
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
