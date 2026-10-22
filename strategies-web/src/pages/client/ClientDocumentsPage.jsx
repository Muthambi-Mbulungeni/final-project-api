import { useRef, useState } from 'react'
import { AlertCircle, Check, Clock, Mail, Paperclip, Upload } from 'lucide-react'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatusBadge from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/toastContext'
import { apiPost, apiUpload } from '../../lib/api'
import { latestCase } from '../../lib/cases'
import {
  CLIENT_STATUS_LABELS,
  UPLOAD_ACCEPT,
  UPLOAD_PATH,
  documentStatus,
  documentSummary,
  documentsForClient,
  documentsPath,
  replacementBody,
  uploadFileError,
} from '../../lib/documents'
import { formatDate, formatFileSize } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { getSession } from '../../lib/session'
import { useApi } from '../../lib/useApi'

// One visual language per status, shared by the rows, the progress bar and the guide.
// Icons match the case stepper; colour is never the only signal (each also has a text label).
const STATUS_STYLES = {
  Approved: { icon: Check, chip: 'bg-emerald-50 text-emerald-700', bar: 'bg-emerald-500' },
  Pending: { icon: Clock, chip: 'bg-amber-50 text-amber-700', bar: 'bg-amber-400' },
  Rejected: { icon: AlertCircle, chip: 'bg-brand-50 text-brand-700', bar: 'bg-brand-600' },
}
const UNKNOWN_STYLE = { icon: Clock, chip: 'bg-stone-100 text-stone-500', bar: 'bg-stone-300' }

const GUIDE = [
  { status: 'Pending', text: 'Your consultant checks each document you upload.' },
  { status: 'Approved', text: 'Accepted for your application. Nothing more to do.' },
  { status: 'Rejected', text: 'Your consultant explains what to fix, so you can upload a new copy.' },
]

function StatusIcon({ status }) {
  const style = STATUS_STYLES[status] ?? UNKNOWN_STYLE
  const Icon = style.icon
  return (
    <span className={`flex size-8 shrink-0 items-center justify-center rounded-full ${style.chip}`} aria-hidden="true">
      <Icon className="size-4" strokeWidth={2} />
    </span>
  )
}

function legendText(status, count) {
  if (status === 'Rejected') return `${count} ${count === 1 ? 'needs' : 'need'} replacing`
  return `${count} ${CLIENT_STATUS_LABELS[status].toLowerCase()}`
}

// Uploads a new copy of one rejected document: the file first (POST /api/documents/upload), then
// the document record with the same case and type (POST /api/documents). If the second call fails,
// the uploaded file stays on the server unregistered; trying again uploads it afresh.
function ReplaceForm({ document, onReplaced }) {
  const toast = useToast()
  const inputRef = useRef(null)
  const [file, setFile] = useState(null)
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  const name = document.documentTypeName
  const inputId = `replace-file-${document.id}`
  const hintId = `replace-hint-${document.id}`
  const errorId = `replace-error-${document.id}`

  async function handleSubmit(event) {
    event.preventDefault()
    const problem = uploadFileError(file)
    if (problem) {
      setError(problem)
      inputRef.current?.focus()
      return
    }

    setBusy(true)
    setError(null)
    try {
      const uploaded = await apiUpload(UPLOAD_PATH, file)
      await apiPost('/api/documents', replacementBody(document, file, uploaded))
      toast.show(`New copy of ${name} sent. Your consultant will review it.`)
      setFile(null)
      if (inputRef.current) inputRef.current.value = ''
      onReplaced()
    } catch (uploadError) {
      setError(uploadError.message)
      requestAnimationFrame(() => inputRef.current?.focus())
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="mt-4 space-y-2">
      <label htmlFor={inputId} className="block text-sm font-medium text-stone-800">
        Upload a new copy<span className="sr-only"> of {name}</span>
      </label>
      <p id={hintId} className="text-xs text-stone-500">
        PDF, JPG or PNG, up to 5 MB.
      </p>
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        {/* The real file input stays in the page (keyboard, screen readers, form behaviour) but is
            visually hidden; the label below is its visible button and shows its focus ring. That
            label is aria-hidden so the input's accessible name stays "Upload a new copy of …". */}
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <input
            ref={inputRef}
            id={inputId}
            type="file"
            accept={UPLOAD_ACCEPT}
            disabled={busy}
            onChange={(event) => {
              setFile(event.target.files?.[0] ?? null)
              setError(null)
            }}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? `${hintId} ${errorId}` : hintId}
            className="peer sr-only"
          />
          <label
            htmlFor={inputId}
            aria-hidden="true"
            className="inline-flex shrink-0 cursor-pointer items-center gap-2 rounded-md border border-stone-300 bg-white px-3 py-1.5 text-sm font-medium text-stone-800 transition-colors peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-brand-600 peer-disabled:cursor-wait peer-disabled:opacity-60 peer-aria-[invalid=true]:border-brand-600 hover:bg-stone-50"
          >
            <Paperclip className="size-4 text-stone-500" strokeWidth={1.75} />
            {file ? 'Change file' : 'Choose file'}
          </label>
          {/* A long name is shortened; the size always stays visible. */}
          <p className="flex min-w-0 items-baseline gap-1 text-sm text-stone-600" aria-hidden="true">
            {file ? (
              <>
                <span className="truncate font-medium text-stone-900">{file.name}</span>
                <span className="shrink-0 text-stone-500">· {formatFileSize(file.size)}</span>
              </>
            ) : (
              'No file chosen'
            )}
          </p>
        </div>
        <button
          type="submit"
          disabled={busy}
          aria-label={busy ? undefined : `Send new copy of ${name}`}
          className="inline-flex shrink-0 items-center justify-center gap-2 rounded-md bg-brand-600 px-3 py-1.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-60"
        >
          <Upload className="size-4" strokeWidth={1.75} aria-hidden="true" />
          {busy ? 'Uploading…' : 'Send new copy'}
        </button>
      </div>
      {error && (
        <p id={errorId} role="alert" className="text-sm text-brand-700">
          {error}
        </p>
      )}
    </form>
  )
}

// The one thing the client has to do, shown first and only when there is something to do.
function ActionNeeded({ documents, onReplaced }) {
  const count = documents.length

  return (
    <section aria-labelledby="action-needed-heading" className="overflow-hidden rounded-lg border border-brand-200 bg-white">
      <div className="border-b border-brand-100 bg-brand-50 px-5 py-4 sm:px-6">
        <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">Action needed</p>
        <h2 id="action-needed-heading" className="mt-1 text-base font-semibold text-stone-900">
          {count === 1 ? 'Replace 1 document' : `Replace ${count} documents`}
        </h2>
      </div>

      <ul className="divide-y divide-stone-100 px-5 sm:px-6">
        {documents.map((document) => {
          const comment = document.reviewComment?.trim()
          return (
            <li key={document.id} className="py-4">
              <p className="text-sm font-medium text-stone-900">{document.documentTypeName}</p>
              <p className="mt-0.5 text-xs text-stone-500">Uploaded {formatDate(document.uploadedAt)}</p>
              {comment ? (
                <figure className="mt-3 border-l-2 border-brand-500 pl-3">
                  <blockquote className="text-sm leading-relaxed text-stone-800">{comment}</blockquote>
                  <figcaption className="mt-1 text-xs text-stone-500">Your consultant</figcaption>
                </figure>
              ) : (
                <p className="mt-3 text-sm text-stone-600">Your consultant will contact you about this document.</p>
              )}
              <ReplaceForm document={document} onReplaced={onReplaced} />
            </li>
          )
        })}
      </ul>

      <p className="flex items-start gap-2 border-t border-stone-100 bg-stone-50 px-5 py-3 text-sm text-stone-600 sm:px-6">
        <Upload className="mt-0.5 size-4 shrink-0 text-stone-500" strokeWidth={1.75} aria-hidden="true" />
        Each new copy is reviewed again once it arrives.
      </p>
    </section>
  )
}

function Progress({ summary, total }) {
  const segments = [
    ['Approved', summary.approved],
    ['Pending', summary.pending],
    ['Rejected', summary.rejected],
  ].filter(([, count]) => count > 0)

  return (
    <div>
      <p className="text-sm text-stone-600">
        <span className="mr-1.5 text-2xl font-semibold tracking-tight text-stone-900 tabular-nums">{summary.approved}</span>
        of {total} approved
      </p>
      <div className="mt-3 flex h-2 gap-0.5 overflow-hidden rounded-full bg-stone-100" aria-hidden="true">
        {segments.map(([status, count]) => (
          <span key={status} className={STATUS_STYLES[status].bar} style={{ width: `${(count / total) * 100}%` }} />
        ))}
      </div>
      <ul className="mt-3 flex flex-wrap gap-x-5 gap-y-1.5 text-xs text-stone-600">
        {segments.map(([status, count]) => (
          <li key={status} className="flex items-center gap-1.5">
            <span className={`size-2 rounded-full ${STATUS_STYLES[status].bar}`} aria-hidden="true" />
            {legendText(status, count)}
          </li>
        ))}
      </ul>
    </div>
  )
}

function DocumentRow({ document }) {
  const status = documentStatus(document)

  return (
    <li className="flex items-start gap-3 py-4 last:pb-0">
      <StatusIcon status={status} />
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1">
          <p className="text-sm font-medium text-stone-900">{document.documentTypeName}</p>
          <StatusBadge type="documentStatus" value={document.status} label={CLIENT_STATUS_LABELS[status]} />
        </div>
        <p className="mt-0.5 text-xs text-stone-500">
          Uploaded {formatDate(document.uploadedAt)}
          <span aria-hidden="true"> · </span>
          <span className="break-words">{document.fileName}</span>
        </p>
      </div>
    </li>
  )
}

function ReviewGuide() {
  return (
    <Panel title="How review works">
      <ul className="space-y-4">
        {GUIDE.map((item) => (
          <li key={item.status} className="flex gap-3">
            <StatusIcon status={item.status} />
            <div>
              <p className="text-sm font-medium text-stone-900">{CLIENT_STATUS_LABELS[item.status]}</p>
              <p className="mt-0.5 text-sm text-stone-600">{item.text}</p>
            </div>
          </li>
        ))}
      </ul>

      <div className="mt-6 flex gap-3 border-t border-stone-100 pt-5">
        <span
          className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-100 text-stone-600"
          aria-hidden="true"
        >
          <Mail className="size-4" strokeWidth={1.75} />
        </span>
        <div>
          <p className="text-sm font-medium text-stone-900">Sending documents</p>
          <p className="mt-0.5 text-sm text-stone-600">
            If a document needs replacing, upload a new copy on this page. Your consultant adds your other
            documents to your case, and they appear here once added.
          </p>
        </div>
      </div>
    </Panel>
  )
}

// Rendered only once the case is known, because the documents request needs its id.
// While reloading after an upload, the previous list stays on screen so the page does not flash.
function DocumentsOverview({ caseId }) {
  const request = useApi(documentsPath(caseId))
  const documents = documentsForClient(listItems(request.data))
  const ready = !request.error && documents.length > 0
  const summaryRef = useRef(null)

  // The replaced document leaves "Action needed", taking its form with it, so focus moves to the summary.
  function handleReplaced() {
    request.reload()
    requestAnimationFrame(() => summaryRef.current?.focus())
  }

  if (!ready) {
    return (
      <Panel title="Your documents" className="mt-8">
        <RequestState
          request={request}
          isEmpty={documents.length === 0}
          emptyTitle="No documents received yet"
          emptyMessage="Documents your consultant adds to your case appear here, with their review."
          loadingText="Loading your documents…"
        >
          {null}
        </RequestState>
      </Panel>
    )
  }

  const summary = documentSummary(documents)
  const total = documents.length
  const toReplace = documents.filter((document) => documentStatus(document) === 'Rejected')

  return (
    <div className="mt-8 grid gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)] lg:items-start">
      <div className="min-w-0 space-y-6">
        {toReplace.length > 0 && <ActionNeeded documents={toReplace} onReplaced={handleReplaced} />}

        <Panel title="Your documents">
          {/* tabIndex -1: focus lands here after an upload; not a control, so no outline. */}
          <div ref={summaryRef} tabIndex={-1} className="outline-none">
            <Progress summary={summary} total={total} />
          </div>
          {summary.approved === total && (
            <p className="mt-4 text-sm text-stone-600">
              All your documents are approved. Nothing more is needed from you right now.
            </p>
          )}
          <ul className="mt-5 divide-y divide-stone-100 border-t border-stone-100">
            {documents.map((document) => (
              <DocumentRow key={document.id} document={document} />
            ))}
          </ul>
        </Panel>
      </div>

      <ReviewGuide />
    </div>
  )
}

// Clients see what needs their attention, their progress, and each document's review status.
// Clients can upload a new copy of a rejected document. A general upload (choosing a type from
// GET /api/documenttypes) is not built yet; consultants add the other documents to the case.
export default function ClientDocumentsPage() {
  const session = getSession()
  const casesRequest = useApi(`/api/cases/client/${session?.id}`)
  const currentCase = latestCase(listItems(casesRequest.data))

  return (
    <>
      <PageHeader
        eyebrow="Your application"
        title="Documents"
        description="See what needs your attention, and which documents your consultant has approved."
      />

      {currentCase ? (
        <DocumentsOverview caseId={currentCase.id} />
      ) : (
        <Panel title="Your documents" className="mt-8">
          <RequestState
            request={casesRequest}
            isEmpty={!currentCase}
            emptyTitle="No active application yet"
            emptyMessage="Your documents appear here once your consultant has opened your case."
            loadingText="Loading your application…"
          >
            {null}
          </RequestState>
        </Panel>
      )}
    </>
  )
}
