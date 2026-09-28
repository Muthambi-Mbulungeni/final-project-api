import { Link } from 'react-router-dom'
import EmptyState from '../../components/ui/EmptyState'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import StatTile from '../../components/ui/StatTile'
import StatusBadge from '../../components/ui/StatusBadge'
import { CASE_STAGES } from '../../config/caseStages'
import { resolveEnum } from '../../config/enums'
import { SAMPLE_LEADS } from '../../data/sampleLeads'
import { formatDate } from '../../lib/format'

const QUEUE_STATUSES = ['Pending', 'NeedsReview']
const QUEUE_LIMIT = 5

function triageValue(lead) {
  return resolveEnum('triageStatus', lead.triageStatus)?.value
}

function countByTriage(leads, status) {
  return leads.filter((lead) => triageValue(lead) === status).length
}

export default function ConsultantDashboard() {
  const leads = SAMPLE_LEADS

  const queue = leads
    .filter((lead) => QUEUE_STATUSES.includes(triageValue(lead)))
    .sort((a, b) => String(b.submittedAt).localeCompare(String(a.submittedAt)))
    .slice(0, QUEUE_LIMIT)

  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Today's caseload"
        description="New leads to qualify, documents to review and cases moving through each stage."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Sample data
          </span>
        }
      />

      <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile label="Pending leads" value={countByTriage(leads, 'Pending')} hint="Triage status: Pending" />
        <StatTile label="Leads needing review" value={countByTriage(leads, 'NeedsReview')} hint="Triage status: Needs review" />
        <StatTile label="Active cases" hint="Available once case data is connected" />
        <StatTile label="Documents to review" hint="Available once document data is connected" />
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
          {queue.length === 0 ? (
            <EmptyState title="Queue is clear" message="There are no leads waiting for triage." />
          ) : (
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
          )}
        </Panel>

        <Panel title="Documents awaiting review">
          <EmptyState title="No documents loaded" message="Client uploads needing your approval appear here." />
        </Panel>
      </div>
    </>
  )
}
