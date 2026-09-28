import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { enumFilter, enumOrder, resolveEnum } from '../../config/enums'
import { formatDate } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const COLUMNS = [
  {
    key: 'fullName',
    header: 'Lead',
    sortable: true,
    render: (lead) => (
      <div>
        <p className="font-medium text-stone-900">{lead.fullName}</p>
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
    header: 'Triage',
    sortable: true,
    sortValue: (lead) => enumOrder('triageStatus', lead.triageStatus),
    render: (lead) => <StatusBadge type="triageStatus" value={lead.triageStatus} />,
  },
]

const FILTERS = [
  enumFilter('triageStatus', 'triageStatus', 'Triage'),
  enumFilter('immigrationStatus', 'immigrationStatus', 'Immigration status'),
]

const SEARCH_KEYS = ['fullName', 'email']
const INITIAL_SORT = { key: 'submittedAt', direction: 'desc' }

export default function LeadTriage() {
  const { data, loading, error, reload } = useApi('/api/leads')
  const leads = listItems(data)

  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Lead triage"
        description="Qualify new enquiries captured by the intake agent and convert them into cases."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
      />

      <div className="mt-8">
        <DataTable
          caption="Leads"
          columns={COLUMNS}
          rows={leads}
          loading={loading}
          error={error?.message}
          onRetry={reload}
          searchKeys={SEARCH_KEYS}
          searchPlaceholder="Search by name or email"
          filters={FILTERS}
          initialSort={INITIAL_SORT}
          emptyTitle="No leads yet"
          emptyMessage="New enquiries from the intake agent appear here."
        />
      </div>
    </>
  )
}
