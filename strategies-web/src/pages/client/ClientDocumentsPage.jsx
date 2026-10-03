import { AlertCircle, Check, Clock, Mail } from 'lucide-react'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatusBadge from '../../components/ui/StatusBadge'
import { latestCase } from '../../lib/cases'
import {
  CLIENT_STATUS_LABELS,
  documentStatus,
  documentSummary,
  documentsForClient,
  documentsPath,
} from '../../lib/documents'
import { formatDate } from '../../lib/format'
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
  { status: 'Rejected', text: 'Your consultant explains what to fix, so you can send a new copy.' },
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

// The one thing the client has to do, shown first and only when there is something to do.
function ActionNeeded({ documents }) {
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
            </li>
          )
        })}
      </ul>

      <p className="flex items-start gap-2 border-t border-stone-100 bg-stone-50 px-5 py-3 text-sm text-stone-600 sm:px-6">
        <Mail className="mt-0.5 size-4 shrink-0 text-stone-500" strokeWidth={1.75} aria-hidden="true" />
        Send a new copy to your consultant, who adds it to your case. It is reviewed again once received.
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
            Send documents to your consultant, who adds them to your case. They appear here once added.
          </p>
        </div>
      </div>
    </Panel>
  )
}

// Rendered only once the case is known, because the documents request needs its id.
function DocumentsOverview({ caseId }) {
  const request = useApi(documentsPath(caseId))
  const documents = documentsForClient(listItems(request.data))
  const ready = !request.loading && !request.error && documents.length > 0

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
        {toReplace.length > 0 && <ActionNeeded documents={toReplace} />}

        <Panel title="Your documents">
          <Progress summary={summary} total={total} />
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
// Clients cannot upload documents yet (the API has no file upload), so this page has no upload control.
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
