// A titled card. The title truncates before the action does, so a long title can never
// push the action link off a narrow screen.
export default function Panel({ title, action, children, className = '' }) {
  return (
    <section className={`rounded-lg border border-stone-200 bg-white ${className}`}>
      <header className="flex items-center justify-between gap-4 border-b border-stone-200 px-5 py-3.5">
        <h2 className="min-w-0 truncate text-sm font-semibold text-stone-900">{title}</h2>
        {action && <div className="shrink-0">{action}</div>}
      </header>
      <div className="p-5">{children}</div>
    </section>
  )
}
