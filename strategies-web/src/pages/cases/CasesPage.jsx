import { useState } from 'react'
import { Download } from 'lucide-react'
import DataTable from '../../components/ui/DataTable'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/toastContext'
import { enumFilter } from '../../config/enums'
import { apiDownload } from '../../lib/api'
import { formatDate } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { casesPath } from '../../lib/listPaths'
import { useApi } from '../../lib/useApi'
import { useDebouncedValue } from '../../lib/useDebouncedValue'

const PAGE_SIZE = 10
const EXPORT_PATH = '/api/cases/export?format=csv'

// GET /api/cases returns { data: [{ id, clientName, visaCategoryName, stage, isArchived, createdAt, lastUpdated }],
// totalRecords, totalPages, currentPage } and supports page, pageSize, search and stage.
const COLUMNS = [
  {
    key: 'clientName',
    header: 'Client',
    render: (item) => (
      <div className="flex items-center gap-2">
        <span className="font-medium text-stone-900">{item.clientName ?? '—'}</span>
        {item.isArchived && (
          <span className="rounded-md bg-stone-100 px-1.5 py-0.5 text-xs font-medium text-stone-600">Archived</span>
        )}
      </div>
    ),
  },
  {
    key: 'visaCategoryName',
    header: 'Visa category',
    render: (item) => item.visaCategoryName ?? '—',
  },
  {
    key: 'stage',
    header: 'Stage',
    render: (item) => <StatusBadge type="caseStage" value={item.stage} />,
  },
  {
    key: 'createdAt',
    header: 'Opened',
    className: 'whitespace-nowrap tabular-nums',
    render: (item) => formatDate(item.createdAt),
  },
  {
    key: 'lastUpdated',
    header: 'Last updated',
    className: 'whitespace-nowrap tabular-nums',
    render: (item) => formatDate(item.lastUpdated),
  },
]

// The stage filter is sent to the API, which filters across every page.
const FILTERS = [enumFilter('caseStage', 'stage', 'Stage')]

function CasesPage({ eyebrow, title, description }) {
  const toast = useToast()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [stage, setStage] = useState('')
  const [exportState, setExportState] = useState({ busy: false, error: null })
  const search = useDebouncedValue(searchInput, 400)

  const request = useApi(casesPath({ page, pageSize: PAGE_SIZE, search, stage }))
  const response = request.data
  const rows = listItems(response)

  function handleSearchChange(value) {
    setSearchInput(value)
    setPage(1)
  }

  function handleFiltersChange(filters) {
    setStage(filters.stage ?? '')
    setPage(1)
  }

  async function handleExport() {
    setExportState({ busy: true, error: null })
    try {
      const filename = await apiDownload(EXPORT_PATH, 'cases.csv')
      setExportState({ busy: false, error: null })
      toast.show(`Exported ${filename}`)
    } catch (error) {
      setExportState({ busy: false, error: error.message })
    }
  }

  const exportButton = (
    <button
      type="button"
      onClick={handleExport}
      disabled={exportState.busy}
      className="inline-flex items-center gap-2 rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:cursor-wait disabled:opacity-60"
    >
      <Download className="size-4 text-stone-500" strokeWidth={1.75} aria-hidden="true" />
      {exportState.busy ? 'Exporting…' : 'Export CSV'}
    </button>
  )

  return (
    <>
      <PageHeader eyebrow={eyebrow} title={title} description={description} />

      {exportState.error && (
        <ErrorNotice
          className="mt-8"
          message={`The export could not be created. ${exportState.error}`}
          onRetry={handleExport}
        />
      )}

      <div className="mt-8">
        <DataTable
          caption="Cases"
          columns={COLUMNS}
          rows={rows}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          searchPlaceholder="Search by client or visa"
          searchValue={searchInput}
          onSearchChange={handleSearchChange}
          filters={FILTERS}
          onFiltersChange={handleFiltersChange}
          actions={exportButton}
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
