import { Link } from 'react-router-dom'
import EmptyState from '../../components/ui/EmptyState'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { CASE_STAGES } from '../../config/caseStages'
import { resolveEnum } from '../../config/enums'
import { formatDate } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const QUEUE_STATUSES = ['Pending', 'NeedsReview']
const QUEUE_LIMIT = 5

function isInQueue(lead) {
  return QUEUE_STATUSES.includes(resolveEnum('triageStatus', lead.triageStatus)?.value)
}

function hasValue(value) {
  return value !== undefined && value !== null
}

export default function ConsultantDashboard() {
  const stats = useApi('/api/stats/consultant')
  const leadsRequest = useApi('/api/leads')

  const queue = listItems(leadsRequest.data)
    .filter(isInQueue)
    .sort((a, b) => String(b.submittedAt).localeCompare(String(a.submittedAt)))
    .slice(0, QUEUE_LIMIT)

  // documentsAwaitingReview is in frontend-contracts.md but not yet returned by the API,
  // so its tile only appears once the field is present.
  const tiles = [
    { label: 'Pending leads', value: stats.data?.pendingLeads },
    { label: 'Active cases', value: stats.data?.activeCases },
    { label: 'Documents awaiting review', value: stats.data?.documentsAwaitingReview, optional: true },
  ].filter((tile) => !tile.optional || hasValue(tile.value))

  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Today's caseload"
        description="New leads to qualify and cases moving through each stage."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
      />

      {stats.error && <ErrorNotice className="mt-8" message={stats.error.message} onRetry={stats.reload} />}

      <div className={`mt-8 grid gap-4 ${tiles.length > 2 ? 'sm:grid-cols-3' : 'sm:grid-cols-2'}`}>
        {tiles.map((tile) => (
          <StatTile key={tile.label} label={tile.label} value={tile.value} />
        ))}
      </div>

      <Panel title="Case pipeline" className="mt-6">
        <ol className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          {CASE_STAGES.map((stage, index) => (
            <li key={stage.key} className="rounded-md border border-stone-200 bg-stone-50 px-4 py-3">
              <p className="text-xs font-medium text-stone-500">Stage {index + 1}</p>
              <p className="mt-0.5 text-sm font-semibold text-stone-900">{stage.label}</p>
              <p className="mt-3 text-2xl font-semibold text-stone-900 tabular-nums">—</p>
            </li>
          ))}
        </ol>
      </Panel>

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

        <Panel title="Documents awaiting review">
          <EmptyState title="No documents loaded" message="Client uploads needing your approval appear here." />
        </Panel>
      </div>
    </>
  )
}
