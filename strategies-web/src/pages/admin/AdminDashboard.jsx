import { ArrowRight, Clock } from 'lucide-react'
import { Link } from 'react-router-dom'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { actionLabel, activityContext } from '../../lib/audit'
import {
  STAFF_LOOKUP_PATH,
  WORKLOAD_CASES_PATH,
  WORKLOAD_TARGET,
  consultantChoices,
  consultantWorkload,
  workloadStatus,
} from '../../lib/cases'
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

// Active cases per consultant against the client's target of about 10 each, so an administrator
// can see at a glance who has room before assigning. Each bar is backed by words and figures.
function WorkloadPanel({ users, cases, hideError }) {
  const { consultants, complete: allUsersLoaded } = consultantChoices(users.data)
  const { rows, unassigned, complete: allCasesLoaded } = consultantWorkload(consultants, cases.data)
  const request = {
    loading: users.loading || cases.loading,
    error: users.error ?? cases.error,
    data: users.data && cases.data,
    reload: () => {
      users.reload()
      cases.reload()
    },
  }

  return (
    <Panel
      title="Consultant workload"
      className="lg:col-span-2"
      action={<span className="text-xs text-stone-500">Target: {WORKLOAD_TARGET} active cases each</span>}
    >
      <RequestState
        request={request}
        isEmpty={rows.length === 0}
        emptyTitle="No active consultants"
        emptyMessage="Consultants appear here once their accounts are active."
        loadingText="Loading workload…"
        hideError={hideError}
      >
        <ul className="-my-3 divide-y divide-stone-100">
          {rows.map((row) => {
            const share = row.active === null ? 0 : Math.min(row.active / WORKLOAD_TARGET, 1)
            const full = row.active !== null && row.active >= WORKLOAD_TARGET
            return (
              <li
                key={row.id}
                className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-4 gap-y-2 py-3 sm:grid-cols-[minmax(0,13rem)_minmax(0,1fr)_4.5rem]"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <span
                    className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-100 text-xs font-semibold text-stone-700"
                    aria-hidden="true"
                  >
                    {getInitials(row.name)}
                  </span>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-900">{row.name}</p>
                    <p className={`truncate text-xs ${full ? 'font-medium text-brand-700' : 'text-stone-500'}`}>
                      {workloadStatus(row.active)}
                    </p>
                  </div>
                </div>
                <div className="col-span-2 row-start-2 h-1.5 overflow-hidden rounded-full bg-stone-100 sm:col-span-1 sm:row-start-auto" aria-hidden="true">
                  <div
                    className={`h-full rounded-full ${full ? 'bg-brand-600' : 'bg-stone-700'}`}
                    style={{ width: `${share * 100}%` }}
                  />
                </div>
                <p className="text-right text-sm text-stone-500 tabular-nums">
                  <span className="text-base font-semibold text-stone-900">{row.active ?? '—'}</span> / {WORKLOAD_TARGET}
                </p>
              </li>
            )
          })}
        </ul>

        {(unassigned > 0 || !allUsersLoaded || !allCasesLoaded) && (
          <div className="mt-4 space-y-1 border-t border-stone-100 pt-4">
            {unassigned > 0 && (
              <p className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 text-sm text-stone-700">
                <span>
                  <span className="font-semibold text-stone-900">{unassigned}</span>{' '}
                  {unassigned === 1 ? 'active case has' : 'active cases have'} no consultant yet.
                </span>
                <Link
                  to="/admin/cases"
                  className="inline-flex items-center gap-1 font-medium text-brand-700 hover:text-brand-800"
                >
                  Assign in Cases
                  <ArrowRight className="size-3.5" strokeWidth={2} aria-hidden="true" />
                </Link>
              </p>
            )}
            {(!allUsersLoaded || !allCasesLoaded) && (
              <p className="text-xs text-stone-500">Based on the first 100 accounts and cases.</p>
            )}
          </div>
        )}
      </RequestState>
    </Panel>
  )
}

// One clear next step when enquiries are waiting: the same amber as the "Needs review" status.
function AwaitingReviewNotice({ count }) {
  return (
    <div className="flex flex-col gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <p className="flex items-start gap-2 text-sm text-amber-900">
        <Clock className="mt-0.5 size-4 shrink-0 text-amber-700" strokeWidth={2} aria-hidden="true" />
        <span>
          <span className="font-semibold">
            {count} {count === 1 ? 'enquiry is' : 'enquiries are'}
          </span>{' '}
          waiting for a consultant’s review.
        </span>
      </p>
      <Link
        to="/admin/leads"
        className="inline-flex shrink-0 items-center gap-1 text-sm font-semibold text-amber-900 hover:underline"
      >
        Review enquiries
        <ArrowRight className="size-4" strokeWidth={2} aria-hidden="true" />
      </Link>
    </div>
  )
}

export default function AdminDashboard() {
  // One page of accounts and one of cases feed the totals, the users list and the workload card.
  const users = useApi(STAFF_LOOKUP_PATH)
  const cases = useApi(WORKLOAD_CASES_PATH)
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
  // Three tiles cannot sit evenly two-up, so on a phone they stack as compact rows instead.
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
    },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="System overview"
        description="Enquiries waiting for review, consultant workload and recent activity across the practice."
      />

      {allFailed && <ErrorNotice className="mt-8" message={users.error.message} onRetry={retryAll} />}

      <div className="mt-8 space-y-4">
        {summary?.awaiting > 0 && <AwaitingReviewNotice count={summary.awaiting} />}
        <div className="grid gap-3 sm:grid-cols-3 sm:gap-4">
          {tiles.map((tile) => (
            <StatTile
              key={tile.label}
              label={tile.label}
              value={tile.value}
              hint={tile.hint}
              to={tile.to}
              loading={tile.loading}
              compactOnPhone
            />
          ))}
        </div>
      </div>

      {/* min-w-0 on each panel stops long, truncated text from widening the grid past the screen.
          Workload and users share a row; the activity feed runs full width beneath them. The source
          order matches that visual order, so keyboard and screen reader users meet them the same way. */}
      <div className="mt-8 grid gap-6 *:min-w-0 lg:grid-cols-3">
        <WorkloadPanel users={users} cases={cases} hideError={allFailed} />

        {/* The API lists users by name and sends no sign-up date, so this is not "recent". */}
        <Panel
          title="Users"
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

        <Panel
          title="Recent activity"
          className="lg:col-span-3"
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
              {auditEntries.map((entry, index) => {
                const context = activityContext(entry)
                return (
                  <li key={entry.id ?? index} className="flex items-center justify-between gap-4 py-3">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-stone-900">{actionLabel(entry.action)}</p>
                      {context && <p className="truncate text-xs text-stone-500">{context}</p>}
                    </div>
                    <span className="shrink-0 text-xs text-stone-500 tabular-nums">
                      {formatDateTime(entry.createdAt)}
                    </span>
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
