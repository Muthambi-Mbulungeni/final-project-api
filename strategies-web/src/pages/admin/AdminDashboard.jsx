import EmptyState from '../../components/ui/EmptyState'
import PageHeader from '../../components/ui/PageHeader'
import Panel from '../../components/ui/Panel'
import StatTile from '../../components/ui/StatTile'

export default function AdminDashboard() {
  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="System overview"
        description="Accounts, case volume and recent activity across the practice."
      />

      <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile label="Active users" />
        <StatTile label="Open cases" />
        <StatTile label="Leads this month" />
        <StatTile label="Suspended accounts" />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <Panel title="Recent activity" className="lg:col-span-2">
          <EmptyState title="No activity to show" message="Audit entries appear here once the API is connected." />
        </Panel>
        <Panel title="Accounts by role">
          <EmptyState title="No accounts loaded" message="Role totals appear here once the API is connected." />
        </Panel>
      </div>
    </>
  )
}
