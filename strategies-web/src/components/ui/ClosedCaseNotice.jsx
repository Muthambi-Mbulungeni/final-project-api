import { Archive } from 'lucide-react'

// Shown to a client whose case has been archived. Same shape as the "Action needed" cards, in the
// neutral stone palette because there is nothing left for the client to do.
export default function ClosedCaseNotice({ headingId, children }) {
  return (
    <section
      aria-labelledby={headingId}
      className="flex items-start gap-4 rounded-lg border border-stone-200 bg-stone-50 px-5 py-4 sm:px-6"
    >
      <span
        className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-200 text-stone-700"
        aria-hidden="true"
      >
        <Archive className="size-4" strokeWidth={1.75} />
      </span>
      <div className="min-w-0">
        <p className="text-xs font-semibold uppercase tracking-[0.08em] text-stone-600">Case closed</p>
        <h2 id={headingId} className="mt-1 text-base font-semibold text-stone-900">
          This case is archived
        </h2>
        <p className="mt-0.5 text-sm text-stone-600">{children}</p>
      </div>
    </section>
  )
}
