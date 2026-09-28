import { Link } from 'react-router-dom'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { resolveEnum } from '../../config/enums'
import { formatDate } from '../../lib/format'
import { isAwaitingReview, percentOf, summariseLeads } from '../../lib/leads'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const LIST_LIMIT = 5

function newestFirst(a, b) {
  return String(b.submittedAt).localeCompare(String(a.submittedAt))
}

export default function ConsultantDashboard() {
  const leadsRequest = useApi('/api/leads')
  const casesRequest = useApi(`/api/cases?page=1&pageSize=${LIST_LIMIT}`)

  const leads = listItems(leadsRequest.data)
  const summary = summariseLeads(leads)
  const queue = leads.filter(isAwaitingReview).sort(newestFirst).slice(0, LIST_LIMIT)
  const recentCases = listItems(casesRequest.data).slice(0, LIST_LIMIT)

  const allFailed = Boolean(leadsRequest.error && casesRequest.error)
  const leadsReady = !leadsRequest.loading && !leadsRequest.error

  function retryAll() {
    leadsRequest.reload()
    casesRequest.reload()
  }

  // Every figure is counted from the same lead list shown below, so the tiles and the queue always agree.
  const tiles = [
    { label: 'Enquiries screened', value: summary.total, hint: 'Assessed by the AI intake agent' },
    {
      label: 'Awaiting your review',
      value: summary.awaiting,
      hint: summary.awaiting === 0 ? 'Your queue is clear' : 'Not qualified automatically',
      to: '/consultant/leads',
    },
    { label: 'Qualified', value: summary.qualified, hint: `${percentOf(summary.qualified, summary.total)}% of enquiries` },
    { label: 'Filtered out', value: summary.unqualified, hint: 'Kept out of your queue' },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Today's caseload"
        description="What the intake agent screened, the leads waiting for you, and the latest movement on your cases."
      />

      {allFailed && <ErrorNotice className="mt-8" message={leadsRequest.error.message} onRetry={retryAll} />}

      <section className="mt-8" aria-labelledby="intake-heading">
        <h2 id="intake-heading" className="text-sm font-semibold text-stone-900">
          Intake agent results
        </h2>
        <p className="mt-0.5 text-sm text-stone-500">Every public enquiry is screened before it reaches you.</p>

        <div className="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tiles.map((tile) => (
            <StatTile
              key={tile.label}
              label={tile.label}
              value={leadsReady ? tile.value : null}
              hint={leadsReady ? tile.hint : undefined}
              to={tile.to}
              loading={leadsRequest.loading}
            />
          ))}
        </div>
      </section>

      <div className="mt-8 grid gap-6 lg:grid-cols-2">
        <Panel
          title="Waiting for your review"
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
            emptyMessage="The intake agent has qualified every enquiry so far."
            loadingText="Loading leads…"
            hideError={allFailed}
          >
            <ul className="-my-3 divide-y divide-stone-100">
              {queue.map((lead) => (
                <li key={lead.id} className="flex items-center justify-between gap-4 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-900">{lead.fullName}</p>
                    <p className="truncate text-xs text-stone-500">
                      {resolveEnum('immigrationStatus', lead.immigrationStatus)?.label ?? 'Status not given'}
                      {' · '}Submitted {formatDate(lead.submittedAt)}
                    </p>
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
            hideError={allFailed}
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
