import { Link } from 'react-router-dom'
import { Briefcase, FolderOpen, Inbox } from 'lucide-react'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { resolveEnum } from '../../config/enums'
import { formatDate } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const QUEUE_STATUSES = ['Pending', 'NeedsReview']
const LIST_LIMIT = 5

function isInQueue(lead) {
  return QUEUE_STATUSES.includes(resolveEnum('triageStatus', lead.triageStatus)?.value)
}

function hasValue(value) {
  return value !== undefined && value !== null
}

export default function ConsultantDashboard() {
  const stats = useApi('/api/stats/consultant')
  const leadsRequest = useApi('/api/leads')
  const casesRequest = useApi(`/api/cases?page=1&pageSize=${LIST_LIMIT}`)

  const queue = listItems(leadsRequest.data)
    .filter(isInQueue)
    .sort((a, b) => String(b.submittedAt).localeCompare(String(a.submittedAt)))
    .slice(0, LIST_LIMIT)

  const recentCases = listItems(casesRequest.data).slice(0, LIST_LIMIT)

  // documentsAwaitingReview is in frontend-contracts.md but not yet returned by the API,
  // so its tile only appears once the field is present.
  const tiles = [
    { label: 'Pending leads', value: stats.data?.pendingLeads, icon: Inbox, to: '/consultant/leads' },
    { label: 'Active cases', value: stats.data?.activeCases, icon: Briefcase, to: '/consultant/cases' },
    {
      label: 'Documents awaiting review',
      value: stats.data?.documentsAwaitingReview,
      icon: FolderOpen,
      optional: true,
    },
  ].filter((tile) => !tile.optional || hasValue(tile.value))

  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Today's caseload"
        description="New leads to qualify and the latest movement on your cases."
      />

      {stats.error && <ErrorNotice className="mt-8" message={stats.error.message} onRetry={stats.reload} />}

      <div className={`mt-8 grid gap-4 ${tiles.length > 2 ? 'sm:grid-cols-3' : 'sm:grid-cols-2'}`}>
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

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <Panel
          title="Lead triage queue"
          action={
            <Link to="/consultant/leads" className="text-sm font-medium text-brand-700 hover:text-brand-800">
              View all
            </Link>
          }
        >
          <RequestState
            request={leadsRequest}
            isEmpty={queue.length === 0}
            emptyTitle="Queue is clear"
            emptyMessage="There are no leads waiting for triage."
            loadingText="Loading leads…"
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {queue.map((lead) => (
                <li key={lead.id} className="flex items-center justify-between gap-4 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-900">{lead.fullName}</p>
                    <p className="text-xs text-stone-500">Submitted {formatDate(lead.submittedAt)}</p>
                  </div>
                  <StatusBadge type="triageStatus" value={lead.triageStatus} />
                </li>
              ))}
            </ul>
          </RequestState>
        </Panel>

        <Panel
          title="Recent cases"
          action={
            <Link to="/consultant/cases" className="text-sm font-medium text-brand-700 hover:text-brand-800">
              View all
            </Link>
          }
        >
          <RequestState
            request={casesRequest}
            isEmpty={recentCases.length === 0}
            emptyTitle="No cases yet"
            emptyMessage="Cases appear here once leads are converted."
            loadingText="Loading cases…"
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {recentCases.map((item, index) => (
                <li key={item.id ?? index} className="flex items-center justify-between gap-4 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-900">{item.clientName ?? 'Unassigned'}</p>
                    <p className="truncate text-xs text-stone-500">
                      {item.visaCategoryName ?? 'Visa category not set'}
                      {item.lastUpdated ? ` · Updated ${formatDate(item.lastUpdated)}` : ''}
                    </p>
                  </div>
                  <StatusBadge type="caseStage" value={item.stage} />
                </li>
              ))}
            </ul>
          </RequestState>
        </Panel>
      </div>
    </>
  )
}
