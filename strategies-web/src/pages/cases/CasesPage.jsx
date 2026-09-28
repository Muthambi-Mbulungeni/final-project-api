import { useState } from 'react'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { useApi } from '../../lib/useApi'
import { useDebouncedValue } from '../../lib/useDebouncedValue'

const PAGE_SIZE = 10

// GET /api/cases currently returns { id, clientName, stage } per case,
// and supports page, pageSize and search. No sort or stage filter parameters yet.
const COLUMNS = [
  {
    key: 'clientName',
    header: 'Client',
    render: (item) => <span className="font-medium text-stone-900">{item.clientName ?? '—'}</span>,
  },
  {
    key: 'stage',
    header: 'Stage',
    render: (item) => <StatusBadge type="caseStage" value={item.stage} />,
  },
]

function buildCasesPath(page, search) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) })
  const term = search.trim()
  if (term) params.set('search', term)
  return `/api/cases?${params.toString()}`
}

function CasesPage({ eyebrow, title, description }) {
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const search = useDebouncedValue(searchInput, 400)

  const request = useApi(buildCasesPath(page, search))
  const response = request.data
  const rows = Array.isArray(response?.data) ? response.data : []

  function handleSearchChange(value) {
    setSearchInput(value)
    setPage(1)
  }

  return (
    <>
      <PageHeader
        eyebrow={eyebrow}
        title={title}
        description={description}
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
      />

      <div className="mt-8">
        <DataTable
          caption="Cases"
          columns={COLUMNS}
          rows={rows}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          searchPlaceholder="Search cases"
          searchValue={searchInput}
          onSearchChange={handleSearchChange}
          pagination={{
            page: response?.currentPage ?? page,
            pageSize: PAGE_SIZE,
            totalCount: response?.totalRecords ?? 0,
            totalPages: response?.totalPages ?? 1,
            onPageChange: setPage,
          }}
          emptyTitle="No cases yet"
          emptyMessage="Cases appear here once leads are converted."
        />
      </div>
    </>
  )
}

export function AdminCasesPage() {
  return (
    <CasesPage
      eyebrow="Administration"
      title="Cases"
      description="Every case across the practice and its current stage."
    />
  )
}

export function ConsultantCasesPage() {
  return <CasesPage eyebrow="Consultant" title="My cases" description="Cases and their current stage in the lifecycle." />
}
