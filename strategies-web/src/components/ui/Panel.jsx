export default function Panel({ title, action, children, className = '' }) {
  return (
    <section className={`rounded-lg border border-stone-200 bg-white ${className}`}>
      <header className="flex items-center justify-between border-b border-stone-200 px-5 py-3.5">
        <h2 className="text-sm font-semibold text-stone-900">{title}</h2>
        {action}
      </header>
      <div className="p-5">{children}</div>
    </section>
  )
}
