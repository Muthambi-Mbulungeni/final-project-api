import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import EmptyState from '../../components/ui/EmptyState'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StageMarker from '../../components/ui/StageMarker'
import { CASE_STAGES } from '../../config/caseStages'
import { enumOrder } from '../../config/enums'
import { formatDate, shortId } from '../../lib/format'
import { assignedConsultantName, latestCase } from '../../lib/cases'
import { documentsPath, documentsToReplace, replaceActionLabel } from '../../lib/documents'
import { listItems } from '../../lib/listData'
import { getSession } from '../../lib/session'
import { useApi } from '../../lib/useApi'
import CaseMessages from '../cases/CaseMessages'

// Only show details the API actually sent.
function caseDetails(item) {
  if (!item) return []
  const hasConsultant = item.consultantName && item.consultantName !== 'Unassigned'
  return [
    { label: 'Case reference', value: shortId(item.id), mono: true },
    { label: 'Opened', value: formatDate(item.createdAt) },
    item.visaCategoryName && item.visaCategoryName !== 'N/A' && { label: 'Visa category', value: item.visaCategoryName },
    hasConsultant && { label: 'Your consultant', value: item.consultantName },
  ].filter(Boolean)
}

// The one thing the client has to do, surfaced on their home screen. The Documents page holds
// the details and the upload form; this card only points there. It stays out of the way while
// loading or if the request fails (the Documents page explains errors), and when nothing is needed.
// The button names the document when there is only one, and the note names the consultant
// when the case has one, so the client knows exactly what to do and who asked.
function DocumentsAttention({ caseId, consultantName }) {
  const request = useApi(documentsPath(caseId))
  const toReplace = request.error ? [] : documentsToReplace(listItems(request.data))
  if (toReplace.length === 0) return null

  const count = toReplace.length
  const names = toReplace.map((document) => document.documentTypeName).filter(Boolean)
  const asker = consultantName ?? 'Your consultant'

  return (
    <section
      aria-labelledby="attention-heading"
      className="flex flex-col gap-4 rounded-lg border border-brand-200 bg-brand-50 px-5 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-6"
    >
      <div className="min-w-0">
        <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">Action needed</p>
        <h2 id="attention-heading" className="mt-1 text-base font-semibold text-stone-900">
          {count === 1 ? 'Replace 1 document' : `Replace ${count} documents`}
        </h2>
        {names.length > 0 && (
          <p className="mt-0.5 text-sm text-stone-600">
            {names.length === 1
              ? `${asker} asked for a new copy of your ${names[0].toLowerCase()}.`
              : `${asker} asked for new copies of: ${names.join(', ')}.`}
          </p>
        )}
      </div>
      <Link
        to="/client/documents"
        className="inline-flex shrink-0 items-center justify-center gap-2 rounded-md bg-brand-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700"
      >
        {replaceActionLabel(toReplace)}
        <ArrowRight className="size-4" strokeWidth={2} aria-hidden="true" />
      </Link>
    </section>
  )
}

// The current stage is named in visible text too, so it never relies on the red ring alone.
function StageLabel({ state }) {
  if (state === 'done') return <span className="sr-only"> (completed)</span>
  if (state === 'current') return <span className="block text-xs font-medium text-brand-700">Current stage</span>
  return null
}

export default function ClientDashboard() {
  const session = getSession()
  const firstName = session?.name?.split(' ')[0] ?? 'there'
  const casesRequest = useApi(`/api/cases/client/${session?.id}`)

  const currentCase = latestCase(listItems(casesRequest.data))
  const currentIndex = currentCase ? enumOrder('caseStage', currentCase.stage) : null
  const currentStage = currentIndex === null ? null : CASE_STAGES[currentIndex]
  const nextStage = currentIndex === null ? null : CASE_STAGES[currentIndex + 1]
  const details = caseDetails(currentCase)
  const visa = currentCase?.visaCategoryName && currentCase.visaCategoryName !== 'N/A' ? currentCase.visaCategoryName : null
  const consultantName = assignedConsultantName(currentCase)

  function stateFor(index) {
    if (currentIndex === null) return 'upcoming'
    if (index < currentIndex) return 'done'
    if (index === currentIndex) return 'current'
    return 'upcoming'
  }

  return (
    <>
      <PageHeader
        eyebrow="Your application"
        title={`Welcome back, ${firstName}`}
        description={
          visa ? `Here’s the latest on your ${visa} application.` : 'See where your application stands and what happens next.'
        }
      />

      {/* The attention card renders nothing when there is nothing to do, so the spacing stays even. */}
      <div className="mt-8 space-y-6">
        {currentCase?.id && (
          <DocumentsAttention key={currentCase.id} caseId={currentCase.id} consultantName={consultantName} />
        )}

        <Panel title="Application progress">
          <RequestState
            request={casesRequest}
            isEmpty={!currentCase}
            emptyTitle="No active application yet"
            emptyMessage="Your consultant will open your case once your enquiry has been reviewed."
            loadingText="Loading your application…"
          >
            {/* Vertical on phones, horizontal from md up. The connector after each stage turns
                brand red once that stage is complete, in both layouts. */}
            <ol className="grid gap-5 md:grid-cols-5 md:gap-4">
              {CASE_STAGES.map((stage, index) => {
                const state = stateFor(index)
                const isLast = index === CASE_STAGES.length - 1
                const connectorColour = currentIndex !== null && index < currentIndex ? 'bg-brand-600' : 'bg-stone-200'
                return (
                  <li
                    key={stage.key}
                    className="relative flex gap-3 md:flex-col md:gap-2"
                    aria-current={state === 'current' ? 'step' : undefined}
                  >
                    {!isLast && (
                      <>
                        <span
                          className={`absolute top-7 -bottom-5 left-[13px] w-0.5 rounded-full md:hidden ${connectorColour}`}
                          aria-hidden="true"
                        />
                        <span
                          className={`absolute top-[13px] left-9 -right-2 hidden h-0.5 rounded-full md:block ${connectorColour}`}
                          aria-hidden="true"
                        />
                      </>
                    )}
                    <StageMarker index={index} state={state} />
                    <div className="min-w-0 pt-1 md:pt-0">
                      <p
                        className={`text-sm ${
                          state === 'current'
                            ? 'font-semibold text-stone-900'
                            : state === 'done'
                              ? 'font-medium text-stone-700'
                              : 'font-medium text-stone-500'
                        }`}
                      >
                        {stage.label}
                        <StageLabel state={state} />
                      </p>
                      <p className={`mt-0.5 text-xs ${state === 'upcoming' ? 'text-stone-500' : 'text-stone-600'}`}>
                        {stage.clientHint}
                      </p>
                    </div>
                  </li>
                )
              })}
            </ol>
          </RequestState>
        </Panel>

        <Panel title="What happens next">
          {currentCase && currentStage ? (
            <div className="grid gap-6 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">
                  Stage {currentIndex + 1} of {CASE_STAGES.length}
                </p>
                <p className="mt-1 text-base font-semibold text-stone-900">{currentStage.label}</p>
                <p className="mt-1 text-sm text-stone-600">{currentStage.clientHint}</p>
                {nextStage && (
                  <p className="mt-4 text-sm text-stone-600">
                    <span className="font-medium text-stone-900">Next:</span> {nextStage.label.toLowerCase()}.{' '}
                    {nextStage.clientHint}
                  </p>
                )}
              </div>
              <dl className="grid grid-cols-2 gap-4 self-start rounded-md bg-stone-50 p-4">
                {details.map((detail) => (
                  <div key={detail.label} className="min-w-0">
                    <dt className="text-xs text-stone-500">{detail.label}</dt>
                    <dd
                      className={`mt-0.5 text-sm font-semibold break-words text-stone-900 ${
                        detail.mono ? 'font-mono tracking-wider' : 'tabular-nums'
                      }`}
                    >
                      {detail.value}
                    </dd>
                  </div>
                ))}
              </dl>
            </div>
          ) : (
            <EmptyState
              title="Nothing to show yet"
              message="Once your case is open, the next step and your case reference appear here."
            />
          )}
        </Panel>

        {currentCase?.id && (
          <Panel title="Messages">
            <CaseMessages
              key={currentCase.id}
              caseId={currentCase.id}
              archived={currentCase.isArchived}
              emptyMessage={`Ask ${consultantName ?? 'your consultant'} a question here. Replies appear in this thread.`}
            />
          </Panel>
        )}
      </div>
    </>
  )
}
