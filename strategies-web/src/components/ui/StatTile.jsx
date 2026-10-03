import { Link } from 'react-router-dom'
import { ArrowRight } from 'lucide-react'

const BASE_CLASSES = 'flex h-full min-w-0 flex-col rounded-lg border border-stone-200 bg-white px-4 py-3 sm:px-5 sm:py-4'

// A single figure with an optional line of context underneath.
// Pass `to` to make the whole tile a link to the related page.
// Sized to sit two-up on a phone, so a dashboard's figures fit on the first screen.
export default function StatTile({ label, value, hint, to, loading = false, className = '' }) {
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
          <div className="mt-2 h-8 w-14 animate-pulse rounded bg-stone-100 sm:h-9 sm:w-16" aria-hidden="true" />
          <span className="sr-only">Loading</span>
        </>
      ) : (
        <p className="mt-1.5 text-2xl font-semibold tracking-tight text-stone-900 tabular-nums sm:mt-2 sm:text-3xl">
          {value ?? '—'}
        </p>
      )}

      {hint && !loading && <p className="mt-auto pt-2 text-xs text-stone-500">{hint}</p>}
    </>
  )

  if (to) {
    return (
      <Link
        to={to}
        className={`group ${BASE_CLASSES} transition-colors hover:border-stone-300 hover:bg-stone-50/60 ${className}`}
      >
        {content}
      </Link>
    )
  }

  return <div className={`${BASE_CLASSES} ${className}`}>{content}</div>
}
