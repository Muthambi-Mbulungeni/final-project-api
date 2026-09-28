export default function ErrorNotice({ message, onRetry, className = '' }) {
  return (
    <div
      role="alert"
      className={`flex flex-col gap-3 rounded-lg border border-brand-200 bg-brand-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between ${className}`}
    >
      <p className="text-sm text-brand-800">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="shrink-0 self-start rounded-md border border-brand-200 bg-white px-3 py-1.5 text-sm font-medium text-brand-800 transition-colors hover:bg-brand-100 sm:self-auto"
        >
          Try again
        </button>
      )}
    </div>
  )
}
