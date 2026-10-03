import { useRef, useState } from 'react'
import { useLocation, useSearchParams } from 'react-router-dom'
import { X } from 'lucide-react'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import { formatDateTime, shortId } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const PAGE_SIZE = 20
const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

// GET /api/admin/audit-logs?page&pageSize&userId returns
// { data: [{ id, userId, action, entityType, entityId, createdAt }], totalRecords, totalPages, currentPage },
// always newest first. userId is the only filter the API supports, so it is the only one offered.
// The filter lives in the URL (?userId=), so Back, bookmarks and the Users page's "Activity" link all work.
function buildAuditPath(page, userId) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) })
  if (userId) params.set('userId', userId)
  return `/api/admin/audit-logs?${params.toString()}`
}

function IdText({ value }) {
  return (
    <span className="font-mono text-xs text-stone-600" title={value ?? undefined}>
      {shortId(value)}
    </span>
  )
}

export default function AuditLogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const location = useLocation()
  const filterRowRef = useRef(null)

  // Only a well-formed id is sent; anything else would be rejected by the API.
  const rawUserId = searchParams.get('userId')
  const userId = rawUserId && GUID_PATTERN.test(rawUserId) ? rawUserId : null
  // Present when arriving from the Users page; a shared link falls back to the short id.
  const userName = userId ? location.state?.userName : null

  // The page number belongs to one filter: changing the filter (including via Back) starts at page 1.
  const [paging, setPaging] = useState({ userId: null, page: 1 })
  const page = paging.userId === userId ? paging.page : 1

  const request = useApi(buildAuditPath(page, userId))
  const response = request.data

  function filterByUser(id) {
    setSearchParams({ userId: id })
  }

  function clearUserFilter() {
    setSearchParams({})
    // The clear button disappears, so keep focus in place rather than dropping it to the page.
    filterRowRef.current?.focus()
  }

  const columns = [
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
      render: (entry) =>
        !entry.userId ? (
          '—'
        ) : userId ? (
          <IdText value={entry.userId} />
        ) : (
          <button
            type="button"
            onClick={() => filterByUser(entry.userId)}
            title="Show only this user's actions"
            aria-label={`${shortId(entry.userId)}: show only this user's actions`}
            className="rounded font-mono text-xs text-brand-700 underline-offset-2 hover:text-brand-800 hover:underline"
          >
            {shortId(entry.userId)}
          </button>
        ),
    },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Audit log"
        description="A record of who changed what, and when, across core records."
      />

      {/* tabIndex -1 lets focus land here after the filter is cleared; no outline because it is not a control. */}
      <div
        ref={filterRowRef}
        tabIndex={-1}
        className="mt-8 flex flex-wrap items-center gap-x-3 gap-y-2 text-sm text-stone-600 outline-none"
      >
        {userId ? (
          <span className="inline-flex items-center gap-2">
            Showing actions by
            <span className="inline-flex items-center gap-1 rounded-md bg-stone-100 py-0.5 pr-1 pl-2 font-medium text-stone-900">
              {userName ?? <span className="font-mono text-xs">{shortId(userId)}</span>}
              <button
                type="button"
                onClick={clearUserFilter}
                aria-label="Clear user filter and show everyone's actions"
                className="rounded p-0.5 text-stone-500 transition-colors hover:bg-stone-200 hover:text-stone-900"
              >
                <X className="size-3.5" strokeWidth={2} aria-hidden="true" />
              </button>
            </span>
          </span>
        ) : (
          <span>Select an ID under “Performed by” to see only that user’s actions.</span>
        )}
        <span className="text-xs text-stone-500">Newest first</span>
      </div>

      <div className="mt-3">
        <DataTable
          caption={userName ? `Audit log for ${userName}` : 'Audit log'}
          columns={columns}
          rows={listItems(response)}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          pagination={{
            page: response?.currentPage ?? page,
            pageSize: PAGE_SIZE,
            totalCount: response?.totalRecords ?? 0,
            totalPages: response?.totalPages ?? 1,
            onPageChange: (next) => setPaging({ userId, page: next }),
          }}
          emptyTitle={userId ? 'No activity for this user' : 'No activity recorded'}
          emptyMessage={userId ? 'This user has not changed any core records yet.' : 'Changes to core records appear here.'}
        />
      </div>
    </>
  )
}
