import EmptyState from './EmptyState'
import ErrorNotice from './ErrorNotice'

// Renders the error, loading or empty state for a useApi request,
// and the children only once data is available.
export default function RequestState({ request, isEmpty, emptyTitle, emptyMessage, loadingText = 'Loading…', children }) {
  if (request.error) {
    return <ErrorNotice message={request.error.message} onRetry={request.reload} />
  }

  if (request.loading) {
    return <p className="py-6 text-center text-sm text-stone-500">{loadingText}</p>
  }

  if (isEmpty) {
    return <EmptyState title={emptyTitle} message={emptyMessage} />
  }

  return children
}
