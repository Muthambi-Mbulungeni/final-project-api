import { useMemo, useState } from 'react'
import { ChevronRight } from 'lucide-react'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { enumFilter, enumOrder, resolveEnum } from '../../config/enums'
import { formatDate } from '../../lib/format'
import { summariseLeads } from '../../lib/leads'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'
import LeadDetailPanel from './LeadDetailPanel'

// The lead's name is a real button, so keyboard and screen-reader users can open the details too.
function buildColumns(onOpen) {
  return [
    {
      key: 'fullName',
      header: 'Lead',
      sortable: true,
      render: (lead) => (
        <div>
          <button
            type="button"
            onClick={() => onOpen(lead.id)}
            aria-haspopup="dialog"
            className="text-left font-medium text-stone-900 hover:text-brand-700 hover:underline"
          >
            {lead.fullName}
          </button>
          <p className="text-xs text-stone-500">{lead.email}</p>
        </div>
      ),
    },
    {
      key: 'immigrationStatus',
      header: 'Immigration status',
      sortable: true,
      sortValue: (lead) => enumOrder('immigrationStatus', lead.immigrationStatus),
      render: (lead) => resolveEnum('immigrationStatus', lead.immigrationStatus)?.label ?? '—',
    },
    {
      key: 'needsAssistance',
      header: 'Needs assistance',
      sortable: true,
      render: (lead) => (lead.needsAssistance === undefined ? '—' : lead.needsAssistance ? 'Yes' : 'No'),
    },
    {
      key: 'desiredStartDate',
      header: 'Desired start',
      sortable: true,
      className: 'whitespace-nowrap tabular-nums',
      render: (lead) => formatDate(lead.desiredStartDate),
    },
    {
      key: 'submittedAt',
      header: 'Submitted',
      sortable: true,
      className: 'whitespace-nowrap tabular-nums',
      render: (lead) => formatDate(lead.submittedAt),
    },
    {
      key: 'triageStatus',
      header: 'Agent verdict',
      sortable: true,
      sortValue: (lead) => enumOrder('triageStatus', lead.triageStatus),
      render: (lead) => <StatusBadge type="triageStatus" value={lead.triageStatus} />,
    },
    {
      key: 'open',
      header: <span className="sr-only">Details</span>,
      align: 'right',
      render: () => <ChevronRight className="inline size-4 text-stone-400" strokeWidth={1.75} aria-hidden="true" />,
    },
  ]
}

const FILTERS = [
  enumFilter('triageStatus', 'triageStatus', 'Agent verdict'),
  enumFilter('immigrationStatus', 'immigrationStatus', 'Immigration status'),
]

const SEARCH_KEYS = ['fullName', 'email']
const INITIAL_SORT = { key: 'submittedAt', direction: 'desc' }

function TriageSummary({ summary, loading }) {
  const items = [
    { label: 'Screened by the intake agent', value: summary.total },
    { label: 'Awaiting review', value: summary.awaiting },
    { label: 'Qualified', value: summary.qualified },
    { label: 'Filtered out', value: summary.unqualified },
  ]

  return (
    <dl className="grid grid-cols-2 rounded-lg border border-stone-200 bg-white sm:grid-cols-4 sm:divide-x sm:divide-stone-200">
      {items.map((item) => (
        <div key={item.label} className="px-5 py-3">
          <dt className="text-xs text-stone-500">{item.label}</dt>
          <dd className="mt-0.5 text-xl font-semibold tracking-tight text-stone-900 tabular-nums">
            {loading ? <span className="inline-block h-6 w-8 animate-pulse rounded bg-stone-100" /> : item.value}
          </dd>
        </div>
      ))}
    </dl>
  )
}

export default function LeadTriage() {
  const { data, loading, error, reload } = useApi('/api/leads')
  const [selectedId, setSelectedId] = useState(null)
  const columns = useMemo(() => buildColumns(setSelectedId), [])

  const leads = listItems(data)
  const summary = summariseLeads(leads)
  // Looked up by ID so the panel always shows the latest data after a reload.
  const selectedLead = leads.find((lead) => lead.id === selectedId) ?? null

  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Lead triage"
        description="Every public enquiry is screened by the AI intake agent. Leads it could not qualify on its own wait here for your review. Select a lead to see the full enquiry."
      />

      {!error && (
        <div className="mt-8">
          <TriageSummary summary={summary} loading={loading} />
        </div>
      )}

      <div className={error ? 'mt-8' : 'mt-4'}>
        <DataTable
          caption="Leads"
          columns={columns}
          rows={leads}
          loading={loading}
          error={error?.message}
          onRetry={reload}
          onRowClick={(lead) => setSelectedId(lead.id)}
          searchKeys={SEARCH_KEYS}
          searchPlaceholder="Search by name or email"
          filters={FILTERS}
          initialSort={INITIAL_SORT}
          emptyTitle="No leads yet"
          emptyMessage="New enquiries appear here once the intake agent has screened them."
        />
      </div>

      <LeadDetailPanel lead={selectedLead} onClose={() => setSelectedId(null)} />
    </>
  )
}
