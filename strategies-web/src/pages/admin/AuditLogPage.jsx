import { useState } from 'react'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import { formatDateTime, shortId } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const PAGE_SIZE = 20

// Matches GET /api/admin/audit-logs in frontend-contracts.md:
// { data: [{ id, userId, action, entityType, entityId, createdAt }], totalRecords, totalPages, currentPage }.
// Supports page and pageSize. The API returns userId only, not the user's name.
function buildAuditPath(page) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) })
  return `/api/admin/audit-logs?${params.toString()}`
}

function IdText({ value }) {
  return (
    <span className="font-mono text-xs text-stone-600" title={value ?? undefined}>
      {shortId(value)}
    </span>
  )
}

const COLUMNS = [
  {
    key: 'createdAt',
    header: 'When',
    className: 'whitespace-nowrap tabular-nums',
    render: (entry) => formatDateTime(entry.createdAt),
  },
  {
    key: 'action',
    header: 'Action',
    render: (entry) => <span className="font-medium text-stone-900">{entry.action ?? '—'}</span>,
  },
  {
    key: 'entityType',
    header: 'Record',
    render: (entry) => (
      <div>
        <p className="text-stone-800">{entry.entityType ?? '—'}</p>
        <IdText value={entry.entityId} />
      </div>
    ),
  },
  {
    key: 'userId',
    header: 'Performed by',
    render: (entry) => <IdText value={entry.userId} />,
  },
]

export default function AuditLogPage() {
  const [page, setPage] = useState(1)
  const request = useApi(buildAuditPath(page))
  const response = request.data

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Audit log"
        description="A record of who changed what, and when, across core records."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
      />

      <div className="mt-8">
        <DataTable
          caption="Audit log"
          columns={COLUMNS}
          rows={listItems(response)}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          pagination={{
            page: response?.currentPage ?? page,
            pageSize: PAGE_SIZE,
            totalCount: response?.totalRecords ?? 0,
            totalPages: response?.totalPages ?? 1,
            onPageChange: setPage,
          }}
          emptyTitle="No activity recorded"
          emptyMessage="Changes to core records appear here."
        />
      </div>
    </>
  )
}
