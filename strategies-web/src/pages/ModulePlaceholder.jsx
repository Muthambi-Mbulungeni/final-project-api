import EmptyState from '../components/ui/EmptyState'
import PageHeader from '../components/ui/PageHeader'

export default function ModulePlaceholder({ title, description }) {
  return (
    <>
      <PageHeader title={title} description={description} />
      <div className="mt-8 rounded-lg border border-dashed border-stone-300 bg-white">
        <EmptyState
          title="Not connected yet"
          message="This module will display live data once its API endpoint is available."
        />
      </div>
    </>
  )
}
