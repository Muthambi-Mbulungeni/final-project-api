import { useMemo, useState } from 'react'
import { ChevronRight } from 'lucide-react'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { enumFilter, enumOrder, resolveEnum } from '../../config/enums'
import { formatDate } from '../../lib/format'
import { applyDecision, leadsPath } from '../../lib/leads'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'
import { useDebouncedValue } from '../../lib/useDebouncedValue'
import { useLeadSummary } from '../../lib/useLeadSummary'
import { useSession } from '../../lib/useSession'
import LeadDetailPanel from './LeadDetailPanel'

const PAGE_SIZE = 10

// The lead's name is a real button, so keyboard and screen-reader users can open the details too.
// Columns are only sortable when the whole list is loaded (mock API). With the real API each
// page comes from the server newest first, and sorting one page would be misleading.
function buildColumns(onOpen, sortable) {
  return [
    {
      key: 'fullName',
      header: 'Lead',
      sortable,
      render: (lead) => (
        <div>
          <button
            type="button"
            onClick={() => onOpen(lead)}
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
      sortable,
      sortValue: (lead) => enumOrder('immigrationStatus', lead.immigrationStatus),
      render: (lead) => resolveEnum('immigrationStatus', lead.immigrationStatus)?.label ?? '—',
    },
    {
      key: 'needsAssistance',
      header: 'Needs assistance',
      sortable,
      render: (lead) => (lead.needsAssistance === undefined ? '—' : lead.needsAssistance ? 'Yes' : 'No'),
    },
    {
      key: 'desiredStartDate',
      header: 'Desired start',
      sortable,
      className: 'whitespace-nowrap tabular-nums',
      render: (lead) => formatDate(lead.desiredStartDate),
    },
    {
      key: 'submittedAt',
      header: 'Submitted',
      sortable,
      className: 'whitespace-nowrap tabular-nums',
      render: (lead) => formatDate(lead.submittedAt),
    },
    {
      key: 'triageStatus',
      header: 'Verdict',
      sortable,
      sortValue: (lead) => enumOrder('triageStatus', lead.triageStatus),
      render: (lead) => <StatusBadge type="triageStatus" value={lead.triageStatus} />,
    },
    {
      key: 'open',
      card: false,
      header: <span className="sr-only">Details</span>,
      align: 'right',
      render: () => <ChevronRight className="inline size-4 text-stone-400" strokeWidth={1.75} aria-hidden="true" />,
    },
  ]
}

const FILTERS = [
  enumFilter('triageStatus', 'triageStatus', 'Verdict'),
  enumFilter('immigrationStatus', 'immigrationStatus', 'Immigration status'),
]

const SEARCH_KEYS = ['fullName', 'email']
const INITIAL_SORT = { key: 'submittedAt', direction: 'desc' }

function TriageSummary({ summary, loading }) {
  const items = [
    { label: 'Screened by the intake agent', value: summary?.total },
    { label: 'Awaiting review', value: summary?.awaiting },
    { label: 'Qualified', value: summary?.qualified },
    { label: 'Unqualified', value: summary?.unqualified },
  ]

  return (
    <dl className="grid grid-cols-2 rounded-lg border border-stone-200 bg-white sm:grid-cols-4 sm:divide-x sm:divide-stone-200">
      {items.map((item) => (
        <div key={item.label} className="px-5 py-3">
          <dt className="text-xs text-stone-500">{item.label}</dt>
          <dd className="mt-0.5 text-xl font-semibold tracking-tight text-stone-900 tabular-nums">
            {loading ? <span className="inline-block h-6 w-8 animate-pulse rounded bg-stone-100" /> : (item.value ?? '—')}
          </dd>
        </div>
      ))}
    </dl>
  )
}

function LeadTriage({ eyebrow, title, description }) {
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [status, setStatus] = useState('')
  const [selectedLead, setSelectedLead] = useState(null)
  const search = useDebouncedValue(searchInput, 400)
  const session = useSession()

  const request = useApi(leadsPath({ page, pageSize: PAGE_SIZE, search, status }))
  const leadSummary = useLeadSummary()

  const response = request.data
  const leads = listItems(response)
  // The real API pages, searches and filters by verdict on the server. The mock API returns
  // every lead as a plain array; then the table does all of that in the browser.
  const serverMode = !Array.isArray(response)
  const columns = useMemo(() => buildColumns(setSelectedLead, !serverMode), [serverMode])

  // Called after the API accepted the decision (200). The panel keeps its own copy of the lead,
  // so it stays open even when the decision moves the lead out of the filtered list.
  function handleDecided(leadId, status) {
    setSelectedLead((current) => (current?.id === leadId ? applyDecision(current, status, session?.id) : current))
    request.reload()
    leadSummary.reload()
  }

  function handleSearchChange(value) {
    setSearchInput(value)
    setPage(1)
  }

  function handleFiltersChange(filters) {
    if (!serverMode) return
    setStatus(filters.triageStatus ?? '')
    setPage(1)
  }

  return (
    <>
      <PageHeader eyebrow={eyebrow} title={title} description={description} />

      {!leadSummary.error && (
        <div className="mt-8">
          <TriageSummary summary={leadSummary.summary} loading={leadSummary.loading && !leadSummary.summary} />
        </div>
      )}

      <div className={leadSummary.error ? 'mt-8' : 'mt-4'}>
        <DataTable
          caption="Leads"
          columns={columns}
          rows={leads}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          onRowClick={setSelectedLead}
          searchKeys={SEARCH_KEYS}
          searchPlaceholder="Search by name or email"
          searchValue={serverMode ? searchInput : undefined}
          onSearchChange={serverMode ? handleSearchChange : undefined}
          filters={FILTERS}
          onFiltersChange={handleFiltersChange}
          initialSort={INITIAL_SORT}
          pagination={
            serverMode
              ? {
                  page: response?.currentPage ?? page,
                  pageSize: PAGE_SIZE,
                  totalCount: response?.totalRecords ?? 0,
                  totalPages: response?.totalPages ?? 1,
                  onPageChange: setPage,
                }
              : null
          }
          actions={serverMode ? <span className="text-xs text-stone-500">Newest first</span> : undefined}
          emptyTitle="No leads yet"
          emptyMessage="New enquiries appear here once the intake agent has screened them."
        />
      </div>

      <LeadDetailPanel lead={selectedLead} onClose={() => setSelectedLead(null)} onDecided={handleDecided} />
    </>
  )
}

// GET /api/leads and PATCH /api/leads/{id}/status allow Admin and Consultant and treat both alike,
// so the two pages differ only in their wording.
export function AdminLeadsPage() {
  return (
    <LeadTriage
      eyebrow="Administration"
      title="Leads"
      description="Every public enquiry and the AI intake agent's verdict on it. Select a lead to see the full enquiry."
    />
  )
}

export function ConsultantLeadsPage() {
  return (
    <LeadTriage
      eyebrow="Consultant"
      title="Lead triage"
      description="Every public enquiry is screened by the AI intake agent. Leads it could not qualify on its own wait here for your review. Select a lead to see the full enquiry."
    />
  )
}
