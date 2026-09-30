import { useEffect, useRef } from 'react'
import { Mail, X } from 'lucide-react'
import StatusBadge from '../../components/ui/StatusBadge'
import { resolveEnum } from '../../config/enums'
import { formatDate, formatDateTime } from '../../lib/format'
import { enquiryMailto, enquiryReference, startTimingLabel, verdictExplanation } from '../../lib/leads'

function Detail({ label, children, hint }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-stone-500">{label}</dt>
      <dd className="mt-0.5 text-sm font-medium break-words text-stone-900">{children}</dd>
      {hint && <dd className="mt-0.5 text-xs text-stone-500">{hint}</dd>}
    </div>
  )
}

// Side panel with one lead's full enquiry and what the intake agent decided.
// Built on the native <dialog> element (like ConfirmDialog): focus is moved into the
// panel, Escape closes it, and focus returns to the row that opened it.
// Full screen on phones; the footer buttons share the width and clear the home-bar area.
export default function LeadDetailPanel({ lead, onClose }) {
  const dialogRef = useRef(null)
  const open = lead !== null

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  function handleCancel(event) {
    event.preventDefault()
    onClose()
  }

  // A click on the backdrop lands on the <dialog> element itself, not on its content.
  function handleBackdropClick(event) {
    if (event.target === dialogRef.current) onClose()
  }

  const verdict = lead ? verdictExplanation(lead) : null
  const mailto = lead ? enquiryMailto(lead) : null
  const reference = enquiryReference(lead)
  const firstName = String(lead?.fullName ?? '').trim().split(' ')[0]
  const needsAssistance = lead?.needsAssistance

  return (
    <dialog
      ref={dialogRef}
      onCancel={handleCancel}
      onClick={handleBackdropClick}
      aria-labelledby="lead-panel-title"
      className="my-0 mr-0 ml-auto h-dvh max-h-none w-full max-w-md border-stone-200 bg-white p-0 text-stone-900 shadow-xl shadow-stone-900/10 backdrop:bg-stone-900/40 motion-safe:animate-panel-in sm:border-l"
    >
      {lead && (
        <div className="flex h-full flex-col">
          <header className="flex items-start justify-between gap-4 border-b border-stone-200 px-5 py-5 sm:px-6">
            <div className="min-w-0">
              <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">Lead</p>
              <h2 id="lead-panel-title" className="mt-1 truncate text-lg font-semibold tracking-tight">
                {lead.fullName}
              </h2>
              <p className="truncate text-sm text-stone-500">{lead.email}</p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="-mr-1.5 rounded-md p-1.5 text-stone-500 hover:bg-stone-100 hover:text-stone-900"
              aria-label="Close lead details"
            >
              <X className="size-5" strokeWidth={1.75} aria-hidden="true" />
            </button>
          </header>

          <div className="flex-1 space-y-6 overflow-y-auto px-5 py-5 sm:px-6">
            <section aria-labelledby="lead-verdict-heading">
              <h3 id="lead-verdict-heading" className="text-sm font-semibold text-stone-900">
                Intake agent verdict
              </h3>
              <div className="mt-3 rounded-md border border-stone-200 bg-stone-50 p-4">
                <StatusBadge type="triageStatus" value={lead.triageStatus} />
                <p className="mt-3 text-sm font-medium text-stone-900">{verdict.title}</p>
                <p className="mt-1 text-sm leading-relaxed text-stone-600">{verdict.body}</p>
              </div>
            </section>

            <section aria-labelledby="lead-details-heading">
              <h3 id="lead-details-heading" className="text-sm font-semibold text-stone-900">
                Enquiry details
              </h3>
              <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-5">
                <Detail label="Immigration status">
                  {resolveEnum('immigrationStatus', lead.immigrationStatus)?.label ?? 'Not given'}
                </Detail>
                <Detail label="Needs help with application">
                  {needsAssistance === undefined ? 'Not given' : needsAssistance ? 'Yes' : 'No'}
                </Detail>
                <Detail label="Desired start" hint={startTimingLabel(lead.desiredStartDate)}>
                  {formatDate(lead.desiredStartDate)}
                </Detail>
                <Detail label="Submitted">{formatDateTime(lead.submittedAt)}</Detail>
                <Detail label="Reference" hint="Matches the client’s confirmation">
                  <span className="font-mono tracking-wider">{reference ?? '—'}</span>
                </Detail>
                <Detail label="Consultant review">{lead.reviewedById ? 'Reviewed' : 'Not yet reviewed'}</Detail>
              </dl>
            </section>
          </div>

          <footer
            className={`grid gap-2 border-t border-stone-200 bg-stone-50 px-5 pt-4 pb-[max(1rem,env(safe-area-inset-bottom))] sm:flex sm:justify-end sm:px-6 ${
              mailto ? 'grid-cols-2' : 'grid-cols-1'
            }`}
          >
            <button
              type="button"
              onClick={onClose}
              className="rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50"
            >
              Close
            </button>
            {mailto && (
              <a
                href={mailto}
                className="inline-flex items-center justify-center gap-2 rounded-md bg-brand-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700"
              >
                <Mail className="size-4" strokeWidth={1.75} aria-hidden="true" />
                {firstName ? `Email ${firstName}` : 'Email lead'}
              </a>
            )}
          </footer>
        </div>
      )}
    </dialog>
  )
}
