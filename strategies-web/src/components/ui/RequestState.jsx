import EmptyState from './EmptyState'
import ErrorNotice from './ErrorNotice'

// Renders the error, loading or empty state for a useApi request,
// and the children only once data is available.
// hideError shows a quiet "Unavailable" instead of the full error,
// for when the page already shows one shared error banner.
export default function RequestState({
  request,
  isEmpty,
  emptyTitle,
  emptyMessage,
  loadingText = 'Loading…',
  hideError = false,
  children,
}) {
  if (request.error) {
    if (hideError) {
      return <p className="py-6 text-center text-sm text-stone-500">Unavailable</p>
    }
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
