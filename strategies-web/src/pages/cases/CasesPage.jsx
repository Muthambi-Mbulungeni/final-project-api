import { useMemo, useState } from 'react'
import { ChevronRight, Download } from 'lucide-react'
import DataTable from '../../components/ui/DataTable'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/toastContext'
import { enumFilter } from '../../config/enums'
import { apiDownload } from '../../lib/api'
import { assignedConsultantName } from '../../lib/cases'
import { formatDate } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { casesPath } from '../../lib/listPaths'
import { useApi } from '../../lib/useApi'
import { useDebouncedValue } from '../../lib/useDebouncedValue'
import CaseDocumentsPanel from './CaseDocumentsPanel'

const PAGE_SIZE = 10
const EXPORT_PATH = '/api/cases/export?format=csv'

// GET /api/cases returns { data: [{ id, clientName, consultantName, visaCategoryName, stage, isArchived,
// createdAt, lastUpdated }], totalRecords, totalPages, currentPage } and supports page, pageSize, search and stage.
// The client's name is a real button that opens the case panel, so keyboard users can reach it too.
// Administrators also see who handles each case; consultants only see their own cases, so it is left out.
function buildColumns(onOpen, showConsultant) {
  const consultantColumn = {
    key: 'consultantName',
    header: 'Consultant',
    className: 'whitespace-nowrap',
    render: (item) => assignedConsultantName(item) ?? <span className="text-brand-700">Unassigned</span>,
  }

  return [
    {
      key: 'clientName',
      header: 'Client',
      render: (item) => (
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => onOpen(item)}
            aria-haspopup="dialog"
            aria-label={`${item.clientName ?? 'Client'}, open case`}
            className="text-left font-medium whitespace-nowrap text-stone-900 hover:text-brand-700 hover:underline"
          >
            {item.clientName ?? '—'}
          </button>
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
    ...(showConsultant ? [consultantColumn] : []),
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
    {
      key: 'open',
      card: false,
      header: <span className="sr-only">Open case</span>,
      align: 'right',
      render: () => <ChevronRight className="inline size-4 text-stone-400" strokeWidth={1.75} aria-hidden="true" />,
    },
  ]
}

// The stage filter is sent to the API, which filters across every page.
const FILTERS = [enumFilter('caseStage', 'stage', 'Stage')]

function CasesPage({ eyebrow, title, description, canAssign = false }) {
  const toast = useToast()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [stage, setStage] = useState('')
  // The opened case is kept here, not looked up in the rows: after a stage change under a
  // stage filter, the case leaves the list but its panel must stay open.
  const [selectedCase, setSelectedCase] = useState(null)
  const [exportState, setExportState] = useState({ busy: false, error: null })
  const search = useDebouncedValue(searchInput, 400)

  const request = useApi(casesPath({ page, pageSize: PAGE_SIZE, search, stage }))
  const response = request.data
  const rows = listItems(response)
  const columns = useMemo(() => buildColumns(setSelectedCase, canAssign), [canAssign])

  // Called after the API confirmed the change (200), so the panel shows the saved stage; the list reloads.
  function handleStageChanged(caseId, newStage) {
    setSelectedCase((current) => (current?.id === caseId ? { ...current, stage: newStage } : current))
    request.reload()
  }

  // Same for a new consultant: the panel shows the saved name and the list reloads.
  function handleAssigned(caseId, consultantName) {
    setSelectedCase((current) => (current?.id === caseId ? { ...current, consultantName } : current))
    request.reload()
  }

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
          columns={columns}
          rows={rows}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          onRowClick={setSelectedCase}
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

      <CaseDocumentsPanel
        caseItem={selectedCase}
        onClose={() => setSelectedCase(null)}
        onStageChanged={handleStageChanged}
        onAssigned={canAssign ? handleAssigned : undefined}
      />
    </>
  )
}

export function AdminCasesPage() {
  return (
    <CasesPage
      eyebrow="Administration"
      title="Cases"
      description="Every case across the practice, who handles it and its current stage. Select a client to assign a consultant, update the stage or review documents."
      canAssign
    />
  )
}

export function ConsultantCasesPage() {
  return (
    <CasesPage
      eyebrow="Consultant"
      title="My cases"
      description="Cases assigned to you and their current stage. Select a client to update the stage or review documents."
    />
  )
}
