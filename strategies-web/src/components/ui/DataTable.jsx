import { useMemo, useState } from 'react'
import { ArrowDown, ArrowUp, ChevronRight, ChevronsUpDown, Search } from 'lucide-react'
import EmptyState from './EmptyState'
import Pagination from './Pagination'
import { matchesFilters, matchesSearch, paginate, sortRows } from '../../lib/table'

const CONTROL_CLASSES =
  'block w-full rounded-md border border-stone-300 bg-white text-sm text-stone-900 focus:border-stone-500 focus:ring-2 focus:ring-stone-900/10 focus:outline-none'

function SortIcon({ direction }) {
  if (direction === 'asc') return <ArrowUp className="size-3.5" strokeWidth={2} aria-hidden="true" />
  if (direction === 'desc') return <ArrowDown className="size-3.5" strokeWidth={2} aria-hidden="true" />
  return <ChevronsUpDown className="size-3.5 text-stone-400" strokeWidth={2} aria-hidden="true" />
}

function LoadingRows({ columnCount }) {
  return Array.from({ length: 5 }, (_, rowIndex) => (
    <tr key={rowIndex}>
      {Array.from({ length: columnCount }, (_, cellIndex) => (
        <td key={cellIndex} className="px-4 py-3.5">
          <div className="h-3.5 w-3/4 animate-pulse rounded bg-stone-100" />
        </td>
      ))}
    </tr>
  ))
}

function LoadingCards() {
  return Array.from({ length: 3 }, (_, index) => (
    <li key={index} className="space-y-3 px-4 py-4" aria-hidden="true">
      <div className="h-3.5 w-1/2 animate-pulse rounded bg-stone-100" />
      <div className="grid grid-cols-2 gap-3">
        <div className="h-3 w-3/4 animate-pulse rounded bg-stone-100" />
        <div className="h-3 w-2/3 animate-pulse rounded bg-stone-100" />
      </div>
    </li>
  ))
}

function cellContent(column, row) {
  return column.render ? column.render(row) : row[column.key]
}

function hasContent(content) {
  return content !== null && content !== undefined && content !== false
}

// On a card, a column with a text header becomes a labelled detail.
// Columns without one (row actions) sit along the bottom of the card instead.
function hasCardLabel(column) {
  return column.key !== 'actions' && typeof column.header === 'string' && column.header.trim() !== ''
}

// A row shown as a card on phones. Columns marked `card: false` (e.g. a table-only chevron)
// are left out; a clickable card shows its own chevron beside the heading instead.
function RowCard({ row, columns, onRowClick }) {
  const [primary, ...rest] = columns.filter((column) => column.card !== false)
  const details = rest.filter(hasCardLabel)
  // Actions that render nothing for this row are dropped, so a card never ends in an empty row.
  const trailing = rest
    .filter((column) => !hasCardLabel(column))
    .map((column) => ({ key: column.key, content: cellContent(column, row) }))
    .filter((cell) => hasContent(cell.content))

  return (
    <li
      onClick={onRowClick ? () => onRowClick(row) : undefined}
      className={`px-4 py-4 transition-colors ${onRowClick ? 'cursor-pointer hover:bg-stone-50/70' : ''}`}
    >
      <div className="flex items-start gap-3">
        <div className={`min-w-0 flex-1 text-sm text-stone-900 ${primary.render ? '' : 'font-medium'}`}>
          {cellContent(primary, row)}
        </div>
        {onRowClick && (
          <ChevronRight className="mt-0.5 size-4 shrink-0 text-stone-400" strokeWidth={1.75} aria-hidden="true" />
        )}
      </div>

      {details.length > 0 && (
        <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-3">
          {details.map((column) => (
            <div key={column.key} className="min-w-0">
              <dt className="text-xs text-stone-500">{column.header}</dt>
              <dd className="mt-0.5 text-sm break-words text-stone-700">{cellContent(column, row)}</dd>
            </div>
          ))}
        </dl>
      )}

      {trailing.length > 0 && (
        <div className="mt-3 flex flex-wrap items-center gap-2">
          {trailing.map((cell) => (
            <div key={cell.key}>{cell.content}</div>
          ))}
        </div>
      )}
    </li>
  )
}

// Two modes:
// - Client mode (default): the table searches, filters, sorts and paginates `rows` itself.
// - Server mode: pass `pagination` (and optionally `onSearchChange`/`searchValue`) and the
//   API does the paging and searching; `rows` is treated as the current page only.
//   In server mode, only offer sorting or filters the API itself supports, and pass
//   `onFiltersChange` so the page can send the selected filters to the API.
// Optional `onRowClick(row)` makes each row clickable. For keyboard users, also render a
// real button inside the row (e.g. on the name) that does the same thing.
//
// Layout: from the md breakpoint up the rows are a table; below it each row is a card
// (first column as the heading, the rest as labelled details), so nothing sits off-screen
// on a phone. The switch is CSS only; the hidden layout is display:none, so screen
// readers only meet one of them. Set `card: false` on a column to keep it table-only.
//
// Loading: placeholders appear only on the first load. When the page or search changes,
// the current rows stay in place (dimmed) until the new ones arrive, so the list never grows
// and shrinks under the cursor.
export default function DataTable({
  columns,
  rows = [],
  getRowId = (row) => row.id,
  searchKeys = [],
  searchPlaceholder = 'Search',
  searchValue,
  onSearchChange,
  filters = [],
  onFiltersChange,
  initialSort = null,
  pageSize = 10,
  pagination = null,
  loading = false,
  error = null,
  onRetry,
  onRowClick,
  emptyTitle = 'Nothing to show',
  emptyMessage,
  actions,
  caption,
}) {
  const isServerSearch = typeof onSearchChange === 'function'
  const isServerPaged = pagination !== null

  const [localSearch, setLocalSearch] = useState('')
  const [activeFilters, setActiveFilters] = useState({})
  const [sort, setSort] = useState(initialSort)
  const [localPage, setLocalPage] = useState(1)

  const search = isServerSearch ? (searchValue ?? '') : localSearch
  const showSearch = isServerSearch || searchKeys.length > 0
  const sortableColumns = columns.filter((column) => column.sortable)

  const sortColumn = columns.find((column) => column.key === sort?.key)
  const sortDirection = sort?.direction

  const processedRows = useMemo(() => {
    const filtered = rows.filter(
      (row) =>
        (isServerSearch || matchesSearch(row, search, searchKeys)) && matchesFilters(row, activeFilters, filters),
    )
    return sortRows(filtered, sortColumn, sortDirection)
  }, [rows, isServerSearch, search, searchKeys, activeFilters, filters, sortColumn, sortDirection])

  const { items, totalCount, totalPages, currentPage } = isServerPaged
    ? {
        items: processedRows,
        totalCount: pagination.totalCount,
        totalPages: Math.max(1, pagination.totalPages),
        currentPage: pagination.page,
      }
    : paginate(processedRows, localPage, pageSize)

  const showPlaceholders = loading && items.length === 0
  const refreshing = loading && items.length > 0

  const hasActiveQuery =
    search.trim() !== '' || Object.values(activeFilters).some((value) => value !== '' && value !== undefined)

  function resetLocalPage() {
    if (!isServerPaged) setLocalPage(1)
  }

  function changePage(nextPage) {
    if (isServerPaged) pagination.onPageChange(nextPage)
    else setLocalPage(nextPage)
  }

  function handleSearch(event) {
    const value = event.target.value
    if (isServerSearch) onSearchChange(value)
    else setLocalSearch(value)
    resetLocalPage()
  }

  function handleFilter(key, value) {
    const next = { ...activeFilters, [key]: value }
    setActiveFilters(next)
    onFiltersChange?.(next)
    resetLocalPage()
  }

  function handleSort(key) {
    setSort((previous) => {
      if (previous?.key !== key) return { key, direction: 'asc' }
      if (previous.direction === 'asc') return { key, direction: 'desc' }
      return null
    })
    resetLocalPage()
  }

  // Cards have no column headers to click, so phones sort from a select instead.
  function handleSortSelect(event) {
    const [key, direction] = event.target.value.split(':')
    setSort(key ? { key, direction } : null)
    resetLocalPage()
  }

  function clearQuery() {
    if (isServerSearch) onSearchChange('')
    else setLocalSearch('')
    setActiveFilters({})
    onFiltersChange?.({})
    resetLocalPage()
  }

  const showPagination = !error && totalCount > 0 && (isServerPaged || !loading)
  const showToolbar = showSearch || filters.length > 0 || sortableColumns.length > 0 || actions

  const emptyContent = hasActiveQuery ? (
    <EmptyState title="No matching results" message="Try a different search term or clear the filters." />
  ) : (
    <EmptyState title={emptyTitle} message={emptyMessage} />
  )

  return (
    <div className="rounded-lg border border-stone-200 bg-white">
      {showToolbar && (
        <div className="flex flex-col gap-3 border-b border-stone-200 p-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-1 flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
            {showSearch && (
              <label className="relative block sm:w-72">
                <span className="sr-only">{searchPlaceholder}</span>
                <Search
                  className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400"
                  strokeWidth={1.75}
                  aria-hidden="true"
                />
                <input
                  type="search"
                  autoComplete="off"
                  value={search}
                  onChange={handleSearch}
                  placeholder={searchPlaceholder}
                  className={`${CONTROL_CLASSES} py-2 pr-3 pl-9 placeholder:text-stone-400`}
                />
              </label>
            )}

            {filters.map((filter) => (
              <label key={filter.key} className="block">
                <span className="sr-only">{filter.label}</span>
                <select
                  value={activeFilters[filter.key] ?? ''}
                  onChange={(event) => handleFilter(filter.key, event.target.value)}
                  className={`${CONTROL_CLASSES} py-2 pr-8 pl-3 sm:w-auto`}
                >
                  <option value="">{filter.label}: All</option>
                  {filter.options.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
            ))}

            {sortableColumns.length > 0 && (
              <label className="block md:hidden">
                <span className="sr-only">Sort by</span>
                <select
                  value={sort ? `${sort.key}:${sort.direction}` : ''}
                  onChange={handleSortSelect}
                  className={`${CONTROL_CLASSES} py-2 pr-8 pl-3 sm:w-auto`}
                >
                  <option value="">Sort: Default</option>
                  {sortableColumns.flatMap((column) => [
                    <option key={`${column.key}:asc`} value={`${column.key}:asc`}>
                      {`Sort: ${column.header}, ascending`}
                    </option>,
                    <option key={`${column.key}:desc`} value={`${column.key}:desc`}>
                      {`Sort: ${column.header}, descending`}
                    </option>,
                  ])}
                </select>
              </label>
            )}

            {hasActiveQuery && (
              <button
                type="button"
                onClick={clearQuery}
                className="self-start text-sm font-medium text-stone-600 hover:text-stone-900 sm:self-center"
              >
                Clear
              </button>
            )}
          </div>

          {actions && <div className="flex shrink-0 items-center gap-2">{actions}</div>}
        </div>
      )}

      {error ? (
        <div role="alert" className="px-6 py-12 text-center">
          <p className="text-sm font-medium text-stone-900">This list could not be loaded</p>
          <p className="mt-1 text-sm text-stone-500">
            {typeof error === 'string' ? error : 'The server did not respond. Try again in a moment.'}
          </p>
          {onRetry && (
            <button
              type="button"
              onClick={onRetry}
              className="mt-4 rounded-md border border-stone-300 bg-white px-3 py-1.5 text-sm font-medium text-stone-800 hover:bg-stone-50"
            >
              Try again
            </button>
          )}
        </div>
      ) : (
        <>
          <ul
            aria-label={caption}
            aria-busy={loading || undefined}
            className={`divide-y divide-stone-100 transition-opacity duration-150 md:hidden ${refreshing ? 'opacity-50' : ''}`}
          >
            {showPlaceholders ? (
              <LoadingCards />
            ) : items.length === 0 ? (
              <li>{emptyContent}</li>
            ) : (
              items.map((row) => <RowCard key={getRowId(row)} row={row} columns={columns} onRowClick={onRowClick} />)
            )}
          </ul>

          <div className="hidden overflow-x-auto md:block">
            <table className="min-w-full text-left text-sm">
              {caption && <caption className="sr-only">{caption}</caption>}
              <thead className="bg-stone-50 text-xs text-stone-500">
                <tr>
                  {columns.map((column) => {
                    const isSorted = sort?.key === column.key
                    const ariaSort = isSorted ? (sortDirection === 'asc' ? 'ascending' : 'descending') : 'none'

                    return (
                      <th
                        key={column.key}
                        scope="col"
                        aria-sort={column.sortable ? ariaSort : undefined}
                        className={`px-4 py-2.5 font-medium whitespace-nowrap ${column.align === 'right' ? 'text-right' : ''}`}
                      >
                        {column.sortable ? (
                          <button
                            type="button"
                            onClick={() => handleSort(column.key)}
                            className={`inline-flex items-center gap-1 transition-colors hover:text-stone-900 ${
                              isSorted ? 'text-stone-900' : ''
                            }`}
                          >
                            {column.header}
                            <SortIcon direction={isSorted ? sortDirection : null} />
                          </button>
                        ) : (
                          column.header
                        )}
                      </th>
                    )
                  })}
                </tr>
              </thead>

              <tbody
                aria-busy={loading || undefined}
                className={`divide-y divide-stone-100 transition-opacity duration-150 ${refreshing ? 'opacity-50' : ''}`}
              >
                {showPlaceholders ? (
                  <LoadingRows columnCount={columns.length} />
                ) : items.length === 0 ? (
                  <tr>
                    <td colSpan={columns.length}>{emptyContent}</td>
                  </tr>
                ) : (
                  items.map((row) => (
                    <tr
                      key={getRowId(row)}
                      onClick={onRowClick ? () => onRowClick(row) : undefined}
                      className={`transition-colors hover:bg-stone-50/70 ${onRowClick ? 'cursor-pointer' : ''}`}
                    >
                      {columns.map((column) => (
                        <td
                          key={column.key}
                          className={`px-4 py-3 align-middle text-stone-700 ${column.align === 'right' ? 'text-right' : ''} ${
                            column.className ?? ''
                          }`}
                        >
                          {cellContent(column, row)}
                        </td>
                      ))}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </>
      )}

      {showPagination && (
        <Pagination
          page={currentPage}
          totalPages={totalPages}
          totalCount={totalCount}
          pageSize={isServerPaged ? pagination.pageSize : pageSize}
          itemCount={isServerPaged ? items.length : undefined}
          onPageChange={changePage}
        />
      )}
    </div>
  )
}
