import { Briefcase, MessageSquare } from 'lucide-react'
import EmptyState from '../../components/ui/EmptyState'
import ErrorNotice from '../../components/ui/ErrorNotice'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import StatTile from '../../components/ui/StatTile'
import { CASE_STAGES } from '../../config/caseStages'
import { getSession } from '../../lib/session'
import { useApi } from '../../lib/useApi'

export default function ClientDashboard() {
  const session = getSession()
  const firstName = session?.name?.split(' ')[0] ?? 'there'
  const stats = useApi(`/api/stats/client/${session?.id}`)

  return (
    <>
      <PageHeader
        eyebrow="Your application"
        title={`Welcome back, ${firstName}`}
        description="See where your application stands and what we need from you next."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
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
          to="/client/messages"
          loading={stats.loading}
        />
      </div>

      <Panel title="Application progress" className="mt-6">
        <ol className="grid gap-5 md:grid-cols-5 md:gap-4">
          {CASE_STAGES.map((stage, index) => (
            <li key={stage.key} className="flex gap-3 md:flex-col md:gap-2">
              <span className="flex size-7 shrink-0 items-center justify-center rounded-full border border-stone-300 bg-white text-xs font-semibold text-stone-500">
                {index + 1}
              </span>
              <div>
                <p className="text-sm font-medium text-stone-900">{stage.label}</p>
                <p className="mt-0.5 text-xs text-stone-500">{stage.clientHint}</p>
              </div>
            </li>
          ))}
        </ol>
      </Panel>

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <Panel title="Outstanding documents">
          <EmptyState title="Checklist not loaded" message="Documents required for your visa category appear here." />
        </Panel>
        <Panel title="Latest messages">
          <EmptyState title="No messages loaded" message="Updates from your consultant appear here." />
        </Panel>
      </div>
    </>
  )
}
