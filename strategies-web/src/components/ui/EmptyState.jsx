export default function EmptyState({ title, message }) {
  return (
    <div className="py-8 text-center">
      <p className="text-sm font-medium text-stone-900">{title}</p>
      {message && <p className="mt-1 text-sm text-stone-500">{message}</p>}
    </div>
  )
}
