import { Check } from 'lucide-react'
import EmptyState from '../../components/ui/EmptyState'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import { CASE_STAGES } from '../../config/caseStages'
import { enumOrder } from '../../config/enums'
import { formatDate, shortId } from '../../lib/format'
import { latestCase } from '../../lib/cases'
import { listItems } from '../../lib/listData'
import { getSession } from '../../lib/session'
import { useApi } from '../../lib/useApi'

// Only show details the API actually sent.
function caseDetails(item) {
  if (!item) return []
  const hasConsultant = item.consultantName && item.consultantName !== 'Unassigned'
  return [
    { label: 'Case reference', value: shortId(item.id).toUpperCase(), mono: true },
    { label: 'Opened', value: formatDate(item.createdAt) },
    item.visaCategoryName && item.visaCategoryName !== 'N/A' && { label: 'Visa category', value: item.visaCategoryName },
    hasConsultant && { label: 'Your consultant', value: item.consultantName },
  ].filter(Boolean)
}

function StageMarker({ index, state }) {
  if (state === 'done') {
    return (
      <span className="relative z-10 flex size-7 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white">
        <Check className="size-4" strokeWidth={2.5} aria-hidden="true" />
      </span>
    )
  }

  if (state === 'current') {
    return (
      <span className="relative z-10 flex size-7 shrink-0 items-center justify-center rounded-full border-2 border-brand-600 bg-white text-xs font-semibold text-brand-700 ring-4 ring-brand-100">
        {index + 1}
      </span>
    )
  }

  return (
    <span className="relative z-10 flex size-7 shrink-0 items-center justify-center rounded-full border border-stone-300 bg-white text-xs font-semibold text-stone-500">
      {index + 1}
    </span>
  )
}

function StageLabel({ state }) {
  if (state === 'done') return <span className="sr-only"> (completed)</span>
  if (state === 'current') return <span className="sr-only"> (current stage)</span>
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
        description="See where your application stands and what happens next."
      />

      <Panel title="Application progress" className="mt-8">
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

      <Panel title="What happens next" className="mt-6">
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
    </>
  )
}
