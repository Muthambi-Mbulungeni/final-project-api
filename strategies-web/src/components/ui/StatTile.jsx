import { Link } from 'react-router-dom'

const BASE_CLASSES = 'block h-full rounded-lg border border-stone-200 bg-white px-5 py-4'

// A single statistic. Optional icon, optional link to the related page,
// and a shimmer placeholder while the value is loading.
export default function StatTile({ label, value, hint, icon: Icon, to, loading = false }) {
  const content = (
    <>
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm text-stone-600">{label}</p>
        {Icon && (
          <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-stone-100 text-stone-500 transition-colors group-hover:bg-brand-50 group-hover:text-brand-600">
            <Icon className="size-4" strokeWidth={1.75} aria-hidden="true" />
          </span>
        )}
      </div>

      {loading ? (
        <>
          <div className="mt-2 h-9 w-16 animate-pulse rounded bg-stone-100" aria-hidden="true" />
          <span className="sr-only">Loading</span>
        </>
      ) : (
        <p className="mt-1 text-3xl font-semibold tracking-tight text-stone-900 tabular-nums">{value ?? '—'}</p>
      )}

      {hint && <p className="mt-1 text-xs text-stone-500">{hint}</p>}
    </>
  )

  if (to) {
    return (
      <Link
        to={to}
        className={`group ${BASE_CLASSES} transition-all hover:-translate-y-px hover:border-stone-300 hover:shadow-sm hover:shadow-stone-900/5`}
      >
        {content}
      </Link>
    )
  }

  return <div className={BASE_CLASSES}>{content}</div>
}
