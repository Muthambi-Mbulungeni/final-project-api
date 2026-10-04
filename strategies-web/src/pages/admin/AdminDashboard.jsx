import { Link } from 'react-router-dom'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { actionLabel } from '../../lib/audit'
import { formatDateTime, getInitials } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'
import { useLeadSummary } from '../../lib/useLeadSummary'

const LIST_LIMIT = 5

function hasValue(value) {
  return value !== undefined && value !== null
}

// The total from a list response: the paginated wrapper's totalRecords, or the length of a plain array.
function totalOf(request) {
  if (request.loading || request.error || !request.data) return null
  if (typeof request.data.totalRecords === 'number') return request.data.totalRecords
  return listItems(request.data).length
}

export default function AdminDashboard() {
  const users = useApi(`/api/admin/users?page=1&pageSize=${LIST_LIMIT}`)
  const cases = useApi('/api/cases?page=1&pageSize=1')
  const auditLogs = useApi('/api/admin/audit-logs')
  const leadSummary = useLeadSummary()

  // When every request fails (e.g. the API is down), show one banner instead of several.
  const requests = [users, cases, auditLogs, leadSummary]
  const allFailed = requests.every((request) => request.error)

  function retryAll() {
    requests.forEach((request) => request.reload())
  }

  const auditEntries = listItems(auditLogs.data).slice(0, LIST_LIMIT)
  const recentUsers = listItems(users.data).slice(0, LIST_LIMIT)
  const summary = leadSummary.summary

  const userTotal = totalOf(users)
  const caseTotal = totalOf(cases)

  // Figures come from the same list endpoints the admin pages use, so they match what those pages show.
  // On a phone the tiles sit two-up, so the third spans the full width rather than leaving a gap.
  const tiles = [
    {
      label: 'User accounts',
      value: userTotal,
      hint: hasValue(userTotal) ? 'Administrators, consultants and clients' : undefined,
      to: '/admin/users',
      loading: users.loading,
    },
    {
      label: 'Cases',
      value: caseTotal,
      hint: hasValue(caseTotal) ? 'Across all five lifecycle stages' : undefined,
      to: '/admin/cases',
      loading: cases.loading,
    },
    {
      label: 'Enquiries received',
      value: summary?.total ?? null,
      hint: summary ? `${summary.awaiting} awaiting consultant review` : undefined,
      loading: leadSummary.loading && !summary,
      className: 'col-span-2 sm:col-span-1',
    },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="System overview"
        description="Accounts, case volume and recent activity across the practice."
      />

      {allFailed && <ErrorNotice className="mt-8" message={users.error.message} onRetry={retryAll} />}

      <div className="mt-8 grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4">
        {tiles.map((tile) => (
          <StatTile
            key={tile.label}
            label={tile.label}
            value={tile.value}
            hint={tile.hint}
            to={tile.to}
            loading={tile.loading}
            className={tile.className}
          />
        ))}
      </div>

      {/* min-w-0 on each panel stops long, truncated text from widening the grid past the screen. */}
      <div className="mt-8 grid gap-6 *:min-w-0 lg:grid-cols-3">
        <Panel
          title="Recent activity"
          className="lg:col-span-2"
          action={
            <Link to="/admin/audit-log" className="text-sm font-medium whitespace-nowrap text-brand-700 hover:text-brand-800">
              View audit log
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
                    <p className="truncate text-sm font-medium text-stone-900">{actionLabel(entry.action)}</p>
                    {entry.entityType && <p className="truncate text-xs text-stone-500">{entry.entityType} record</p>}
                  </div>
                  <span className="shrink-0 text-xs text-stone-500 tabular-nums">{formatDateTime(entry.createdAt)}</span>
                </li>
              ))}
            </ul>
          </RequestState>
        </Panel>

        <Panel
          title="Recent users"
          action={
            <Link to="/admin/users" className="text-sm font-medium whitespace-nowrap text-brand-700 hover:text-brand-800">
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
                      {user.isActive === false && <span className="text-xs font-medium text-brand-700">Suspended</span>}
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
