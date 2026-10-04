import { Link } from 'react-router-dom'
import { ArrowRight } from 'lucide-react'

const BASE_CLASSES = 'h-full min-w-0 rounded-lg border border-stone-200 bg-white px-4 py-3 sm:px-5 sm:py-4'
const STACKED = 'flex flex-col'
// Below sm: label and hint on the left, the figure on the right, in one short row.
const COMPACT = 'grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-4 sm:flex sm:flex-col sm:items-stretch'
const COMPACT_FIGURE = 'max-sm:col-start-2 max-sm:row-span-2 max-sm:row-start-1 max-sm:mt-0'

// A single figure with an optional line of context underneath.
// Pass `to` to make the whole tile a link to the related page.
// Sized to sit two-up on a phone, so a dashboard's figures fit on the first screen. Pass
// `compactOnPhone` when the tiles cannot pair up evenly (e.g. three): each becomes one short row.
export default function StatTile({ label, value, hint, to, loading = false, className = '', compactOnPhone = false }) {
  const figure = compactOnPhone ? COMPACT_FIGURE : ''

  const content = (
    <>
      <p className="flex items-center gap-1.5 text-sm font-medium text-stone-600">
        {label}
        {to && (
          <ArrowRight
            className="size-3.5 shrink-0 -translate-x-1 text-stone-400 opacity-0 transition-all group-hover:translate-x-0 group-hover:opacity-100"
            strokeWidth={2}
            aria-hidden="true"
          />
        )}
      </p>

      {loading ? (
        <>
          <div className={`mt-2 h-8 w-14 animate-pulse rounded bg-stone-100 sm:h-9 sm:w-16 ${figure}`} aria-hidden="true" />
          <span className="sr-only">Loading</span>
        </>
      ) : (
        <p
          className={`mt-1.5 text-2xl font-semibold tracking-tight text-stone-900 tabular-nums sm:mt-2 sm:text-3xl ${figure}`}
        >
          {value ?? '—'}
        </p>
      )}

      {hint && !loading && (
        <p className={`mt-auto pt-2 text-xs text-stone-500 ${compactOnPhone ? 'max-sm:pt-0.5' : ''}`}>{hint}</p>
      )}
    </>
  )

  const layout = `${BASE_CLASSES} ${compactOnPhone ? COMPACT : STACKED}`

  if (to) {
    return (
      <Link to={to} className={`group ${layout} transition-colors hover:border-stone-300 hover:bg-stone-50/60 ${className}`}>
        {content}
      </Link>
    )
  }

  return <div className={`${layout} ${className}`}>{content}</div>
}
