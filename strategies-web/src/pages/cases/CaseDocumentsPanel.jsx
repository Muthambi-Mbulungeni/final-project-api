import { useEffect, useRef, useState } from 'react'
import { ExternalLink, X } from 'lucide-react'
import ErrorNotice from '../../components/ui/ErrorNotice'
import StatusBadge from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/toastContext'
import { apiOpenFile, apiPatch } from '../../lib/api'
import {
  canReview,
  documentFileErrorMessage,
  documentFilePath,
  documentsForReview,
  rejectionCommentError,
  reviewBody,
  reviewErrorMessage,
  reviewPath,
} from '../../lib/documentReview'
import { documentSummary, documentsPath } from '../../lib/documents'
import { formatDate } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'
import CaseConsultantSection from './CaseConsultantSection'
import CaseStageSection from './CaseStageSection'

const NO_REJECTION = { id: null, comment: '', error: null }

function documentName(document) {
  return document.documentTypeName || document.fileName || 'Document'
}

// Loads and reviews one case's documents. Mounted per case (keyed by id), so a newly opened
// case never shows the previous case's documents while its own load.
function DocumentsBody({ caseItem }) {
  const toast = useToast()
  const request = useApi(documentsPath(caseItem.id))
  const documents = documentsForReview(listItems(request.data))
  const summary = documentSummary(documents)

  const [rejecting, setRejecting] = useState(NO_REJECTION)
  const [busyId, setBusyId] = useState(null)
  const [rowError, setRowError] = useState({ id: null, message: null })
  // Reviewed here but not yet reloaded: their buttons are hidden so nothing is submitted twice.
  const [reviewedIds, setReviewedIds] = useState(() => new Set())
  const [openingId, setOpeningId] = useState(null)
  const [fileError, setFileError] = useState({ id: null, message: null })
  const headingRefs = useRef(new Map())
  const commentRef = useRef(null)

  // Staff read the actual file before approving or rejecting it.
  async function openFile(document) {
    setOpeningId(document.id)
    setFileError({ id: null, message: null })
    try {
      const result = await apiOpenFile(documentFilePath(document.id), document.fileName || 'document')
      if (result === 'saved') toast.show(`${documentName(document)} downloaded. Your browser blocked the new tab.`)
    } catch (error) {
      setFileError({ id: document.id, message: documentFileErrorMessage(error) })
    } finally {
      setOpeningId(null)
    }
  }

  function focusHeading(id) {
    requestAnimationFrame(() => headingRefs.current.get(id)?.focus())
  }

  function openReject(id) {
    setRejecting({ id, comment: '', error: null })
    setRowError({ id: null, message: null })
    requestAnimationFrame(() => commentRef.current?.focus())
  }

  function cancelReject(id) {
    setRejecting(NO_REJECTION)
    focusHeading(id)
  }

  async function submitReview(document, status, comment) {
    setBusyId(document.id)
    setRowError({ id: null, message: null })
    try {
      await apiPatch(reviewPath(document.id), reviewBody(status, comment))
      setReviewedIds((current) => new Set(current).add(document.id))
      setRejecting(NO_REJECTION)
      focusHeading(document.id)
      toast.show(
        status === 'Approved'
          ? `${documentName(document)} approved.`
          : `${documentName(document)} rejected. The client is shown your note.`,
      )
      request.reload()
    } catch (error) {
      setRowError({ id: document.id, message: reviewErrorMessage(error) })
    } finally {
      setBusyId(null)
    }
  }

  function confirmReject(document) {
    const error = rejectionCommentError(rejecting.comment)
    if (error) {
      setRejecting((current) => ({ ...current, error }))
      commentRef.current?.focus()
      return
    }
    submitReview(document, 'Rejected', rejecting.comment)
  }

  let content
  if (request.error) {
    const message = request.error.status === 403 ? reviewErrorMessage(request.error) : request.error.message
    content = <ErrorNotice className="mt-3" message={message} onRetry={request.reload} />
  } else if (request.loading && !request.data) {
    content = <p className="mt-3 text-sm text-stone-500">Loading documents…</p>
  } else if (documents.length === 0) {
    content = <p className="mt-3 text-sm text-stone-600">No documents on this case yet.</p>
  } else {
    content = (
      <ul className="mt-1 divide-y divide-stone-200">
        {documents.map((document) => {
          const name = documentName(document)
          const reviewed = reviewedIds.has(document.id)
          const actionable = canReview(document, caseItem.isArchived) && !reviewed
          const busy = busyId === document.id
          const isRejecting = rejecting.id === document.id
          const commentId = `reject-comment-${document.id}`
          const hintId = `reject-hint-${document.id}`
          const errorId = `reject-error-${document.id}`

          return (
            <li key={document.id} className="py-4">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  {/* tabIndex -1: focus lands here after a review; not a control, so no outline. */}
                  <h4
                    ref={(element) => {
                      if (element) headingRefs.current.set(document.id, element)
                      else headingRefs.current.delete(document.id)
                    }}
                    tabIndex={-1}
                    className="text-sm font-medium text-stone-900 outline-none"
                  >
                    {name}
                  </h4>
                  <p className="truncate text-xs text-stone-500">
                    {document.fileName || 'No file name'} · Uploaded {formatDate(document.uploadedAt)}
                  </p>
                  <button
                    type="button"
                    onClick={() => openFile(document)}
                    disabled={openingId === document.id}
                    aria-label={`Open the ${name} file in a new tab`}
                    className="mt-1.5 inline-flex items-center gap-1.5 rounded text-sm font-medium text-brand-700 hover:text-brand-800 hover:underline disabled:cursor-wait disabled:opacity-60"
                  >
                    <ExternalLink className="size-3.5" strokeWidth={2} aria-hidden="true" />
                    {openingId === document.id ? 'Opening…' : 'Open file'}
                  </button>
                </div>
                <StatusBadge type="documentStatus" value={document.status} />
              </div>

              {fileError.id === document.id && (
                <p role="alert" className="mt-2 text-sm text-brand-700">
                  {fileError.message}
                </p>
              )}

              {document.reviewComment && (
                <p className="mt-2 rounded-md bg-stone-50 px-3 py-2 text-sm text-stone-700">
                  <span className="font-medium text-stone-900">Note to client:</span> {document.reviewComment}
                </p>
              )}

              {reviewed && canReview(document, caseItem.isArchived) && (
                <p className="mt-2 text-xs text-stone-500">Saved. Updating…</p>
              )}

              {actionable && !isRejecting && (
                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    onClick={() => submitReview(document, 'Approved')}
                    disabled={busy}
                    aria-label={`Approve ${name}`}
                    className="rounded-md bg-brand-600 px-3 py-1.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-60"
                  >
                    {busy ? 'Saving…' : 'Approve'}
                  </button>
                  <button
                    type="button"
                    onClick={() => openReject(document.id)}
                    disabled={busy}
                    aria-label={`Reject ${name}`}
                    className="rounded-md border border-stone-300 bg-white px-3 py-1.5 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:opacity-60"
                  >
                    Reject
                  </button>
                </div>
              )}

              {actionable && isRejecting && (
                <div className="mt-3 space-y-2">
                  <label htmlFor={commentId} className="block text-sm font-medium text-stone-900">
                    What does the client need to fix?
                  </label>
                  <p id={hintId} className="text-xs text-stone-500">
                    The client is shown this note with the document.
                  </p>
                  <textarea
                    id={commentId}
                    ref={commentRef}
                    rows={3}
                    value={rejecting.comment}
                    onChange={(event) => setRejecting((current) => ({ ...current, comment: event.target.value, error: null }))}
                    aria-invalid={rejecting.error ? true : undefined}
                    aria-describedby={rejecting.error ? `${hintId} ${errorId}` : hintId}
                    className="block w-full rounded-md border border-stone-300 px-3 py-2 text-sm text-stone-900 aria-[invalid=true]:border-brand-600"
                  />
                  {rejecting.error && (
                    <p id={errorId} className="text-sm text-brand-700">
                      {rejecting.error}
                    </p>
                  )}
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => cancelReject(document.id)}
                      disabled={busy}
                      className="rounded-md border border-stone-300 bg-white px-3 py-1.5 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:opacity-60"
                    >
                      Cancel
                    </button>
                    <button
                      type="button"
                      onClick={() => confirmReject(document)}
                      disabled={busy}
                      className="rounded-md bg-brand-600 px-3 py-1.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-60"
                    >
                      {busy ? 'Saving…' : 'Confirm rejection'}
                    </button>
                  </div>
                </div>
              )}

              {rowError.id === document.id && (
                <p role="alert" className="mt-2 text-sm text-brand-700">
                  {rowError.message}
                </p>
              )}
            </li>
          )
        })}
      </ul>
    )
  }

  return (
    <section aria-labelledby="case-docs-heading" className="mt-6">
      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
        <h3 id="case-docs-heading" className="text-sm font-semibold text-stone-900">
          Documents
        </h3>
        {documents.length > 0 && (
          <p className="text-xs text-stone-500">
            {summary.pending} awaiting review · {summary.approved} approved · {summary.rejected} rejected
          </p>
        )}
      </div>
      {content}
    </section>
  )
}

// Side panel for one case: its stage (staff can move it), its consultant (administrators can
// assign one; pass onAssigned only for them) and its documents, with approve / reject for pending ones.
// Same native <dialog> pattern as LeadDetailPanel: focus moves in, Escape closes,
// focus returns to the client name that opened it. Full screen on phones.
export default function CaseDocumentsPanel({ caseItem, onClose, onStageChanged, onAssigned }) {
  const dialogRef = useRef(null)
  const open = caseItem !== null

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

  return (
    <dialog
      ref={dialogRef}
      onCancel={handleCancel}
      onClick={handleBackdropClick}
      aria-labelledby="case-docs-title"
      className="my-0 mr-0 ml-auto h-dvh max-h-none w-full max-w-md border-stone-200 bg-white p-0 text-stone-900 shadow-xl shadow-stone-900/10 backdrop:bg-stone-900/40 motion-safe:animate-panel-in sm:border-l"
    >
      {caseItem && (
        <div className="flex h-full flex-col">
          <header className="flex items-start justify-between gap-4 border-b border-stone-200 px-5 py-5 sm:px-6">
            <div className="min-w-0">
              <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">Case</p>
              <h2 id="case-docs-title" className="mt-1 truncate text-lg font-semibold tracking-tight">
                {caseItem.clientName ?? 'Client'}
              </h2>
              <div className="mt-1 flex flex-wrap items-center gap-2 text-sm text-stone-500">
                <span>{caseItem.visaCategoryName ?? '—'}</span>
                <StatusBadge type="caseStage" value={caseItem.stage} />
              </div>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="-mr-1.5 rounded-md p-1.5 text-stone-500 hover:bg-stone-100 hover:text-stone-900"
              aria-label="Close case"
            >
              <X className="size-5" strokeWidth={1.75} aria-hidden="true" />
            </button>
          </header>

          <div className="flex-1 overflow-y-auto px-5 py-5 sm:px-6">
            {caseItem.isArchived && (
              <p className="mb-4 rounded-md border border-stone-200 bg-stone-50 px-3 py-2 text-sm text-stone-700">
                This case is archived, so its stage and documents are read-only.
              </p>
            )}
            <CaseStageSection key={`stage-${caseItem.id}`} caseItem={caseItem} onStageChanged={onStageChanged} />
            {onAssigned && (
              <CaseConsultantSection key={`consultant-${caseItem.id}`} caseItem={caseItem} onAssigned={onAssigned} />
            )}
            <DocumentsBody key={caseItem.id} caseItem={caseItem} />
          </div>

          <footer className="grid grid-cols-1 border-t border-stone-200 bg-stone-50 px-5 pt-4 pb-[max(1rem,env(safe-area-inset-bottom))] sm:flex sm:justify-end sm:px-6">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50"
            >
              Close
            </button>
          </footer>
        </div>
      )}
    </dialog>
  )
}
