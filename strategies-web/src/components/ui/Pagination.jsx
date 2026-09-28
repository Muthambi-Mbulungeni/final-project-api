import { ChevronLeft, ChevronRight } from 'lucide-react'

const BUTTON_CLASSES =
  'inline-flex items-center gap-1 rounded-md border border-stone-300 bg-white px-2.5 py-1.5 text-sm font-medium text-stone-700 transition-colors hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-50'

export default function Pagination({ page, totalPages, totalCount, pageSize, onPageChange }) {
  const first = (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, totalCount)

  return (
    <nav
      className="flex flex-col gap-3 border-t border-stone-200 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
      aria-label="Pagination"
    >
      <p className="text-sm text-stone-600">
        Showing{' '}
        <span className="font-medium text-stone-900 tabular-nums">
          {first}–{last}
        </span>{' '}
        of <span className="font-medium text-stone-900 tabular-nums">{totalCount}</span>
      </p>

      <div className="flex items-center gap-2">
        <span className="mr-1 text-sm text-stone-600 tabular-nums">
          Page {page} of {totalPages}
        </span>
        <button
          type="button"
          className={BUTTON_CLASSES}
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          <ChevronLeft className="size-4" strokeWidth={1.75} aria-hidden="true" />
          Previous
        </button>
        <button
          type="button"
          className={BUTTON_CLASSES}
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          Next
          <ChevronRight className="size-4" strokeWidth={1.75} aria-hidden="true" />
        </button>
      </div>
    </nav>
  )
}
