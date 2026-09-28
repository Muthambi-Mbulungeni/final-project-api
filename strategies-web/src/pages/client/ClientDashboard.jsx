import { Briefcase, Check, MessageSquare } from 'lucide-react'
import EmptyState from '../../components/ui/EmptyState'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import RequestState from '../../components/ui/RequestState'
import StatTile from '../../components/ui/StatTile'
import { CASE_STAGES } from '../../config/caseStages'
import { enumOrder } from '../../config/enums'
import { formatDate, shortId } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { getSession } from '../../lib/session'
import { useApi } from '../../lib/useApi'

// The client's most recently opened case.
function latestCase(cases) {
  return [...cases].sort((a, b) => String(b.createdAt).localeCompare(String(a.createdAt)))[0] ?? null
}

function StageMarker({ index, state }) {
  if (state === 'done') {
    return (
      <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white">
        <Check className="size-4" strokeWidth={2.5} aria-hidden="true" />
      </span>
    )
  }

  if (state === 'current') {
    return (
      <span className="flex size-7 shrink-0 items-center justify-center rounded-full border-2 border-brand-600 bg-white text-xs font-semibold text-brand-700 ring-4 ring-brand-100">
        {index + 1}
      </span>
    )
  }

  return (
    <span className="flex size-7 shrink-0 items-center justify-center rounded-full border border-stone-300 bg-white text-xs font-semibold text-stone-400">
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
  const stats = useApi(`/api/stats/client/${session?.id}`)
  const casesRequest = useApi(`/api/cases/client/${session?.id}`)

  const currentCase = latestCase(listItems(casesRequest.data))
  const currentIndex = currentCase ? enumOrder('caseStage', currentCase.stage) : null
  const currentStage = currentIndex === null ? null : CASE_STAGES[currentIndex]

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

      {stats.error && <ErrorNotice className="mt-8" message={stats.error.message} onRetry={stats.reload} />}

      <div className="mt-8 grid gap-4 sm:grid-cols-2">
        <StatTile
          label="Active applications"
          value={stats.data?.activeApplications}
          icon={Briefcase}
          loading={stats.loading}
        />
        <StatTile
          label="Unread messages"
          value={stats.data?.unreadMessages}
          icon={MessageSquare}
          loading={stats.loading}
        />
      </div>

      <Panel title="Application progress" className="mt-6">
        <RequestState
          request={casesRequest}
          isEmpty={!currentCase}
          emptyTitle="No active application yet"
          emptyMessage="Your consultant will open your case once your enquiry has been reviewed."
          loadingText="Loading your application…"
        >
          <ol className="grid gap-5 md:grid-cols-5 md:gap-4">
            {CASE_STAGES.map((stage, index) => {
              const state = stateFor(index)
              return (
                <li
                  key={stage.key}
                  className="flex gap-3 md:flex-col md:gap-2"
                  aria-current={state === 'current' ? 'step' : undefined}
                >
                  <StageMarker index={index} state={state} />
                  <div>
                    <p
                      className={`text-sm ${
                        state === 'current'
                          ? 'font-semibold text-stone-900'
                          : state === 'done'
                            ? 'font-medium text-stone-700'
                            : 'font-medium text-stone-400'
                      }`}
                    >
                      {stage.label}
                      <StageLabel state={state} />
                    </p>
                    <p className={`mt-0.5 text-xs ${state === 'upcoming' ? 'text-stone-400' : 'text-stone-500'}`}>
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
          <div className="grid gap-6 sm:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">
                Stage {currentIndex + 1} of {CASE_STAGES.length}
              </p>
              <p className="mt-1 text-base font-semibold text-stone-900">{currentStage.label}</p>
              <p className="mt-1 text-sm text-stone-600">{currentStage.clientHint}</p>
            </div>
            <dl className="grid grid-cols-2 gap-4 sm:grid-cols-1">
              <div>
                <dt className="text-xs text-stone-500">Case reference</dt>
                <dd className="mt-0.5 font-mono text-sm font-semibold tracking-wider text-stone-900">
                  {shortId(currentCase.id).toUpperCase()}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-stone-500">Opened</dt>
                <dd className="mt-0.5 text-sm font-medium text-stone-900 tabular-nums">
                  {formatDate(currentCase.createdAt)}
                </dd>
              </div>
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
