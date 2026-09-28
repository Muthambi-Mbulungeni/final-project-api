import EmptyState from '../../components/ui/EmptyState'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import StatTile from '../../components/ui/StatTile'
import { CASE_STAGES } from '../../config/caseStages'

export default function ConsultantDashboard() {
  return (
    <>
      <PageHeader
        eyebrow="Consultant"
        title="Today's caseload"
        description="New leads to qualify, documents to review and cases moving through each stage."
      />

      <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile label="Leads awaiting triage" />
        <StatTile label="Active cases" />
        <StatTile label="Documents to review" />
        <StatTile label="Decisions this month" />
      </div>

      <Panel title="Case pipeline" className="mt-6">
        <ol className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          {CASE_STAGES.map((stage, index) => (
            <li key={stage.key} className="rounded-md border border-stone-200 bg-stone-50 px-4 py-3">
              <p className="text-xs font-medium text-stone-500">Stage {index + 1}</p>
              <p className="mt-0.5 text-sm font-semibold text-stone-900">{stage.label}</p>
              <p className="mt-3 text-2xl font-semibold text-stone-900 tabular-nums">—</p>
            </li>
          ))}
        </ol>
      </Panel>

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <Panel title="Lead triage queue">
          <EmptyState title="No leads loaded" message="New enquiries from the intake agent appear here." />
        </Panel>
        <Panel title="Documents awaiting review">
          <EmptyState title="No documents loaded" message="Client uploads needing your approval appear here." />
        </Panel>
      </div>
    </>
  )
}
