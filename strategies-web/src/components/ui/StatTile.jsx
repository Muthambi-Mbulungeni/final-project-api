export default function StatTile({ label, value, hint }) {
  return (
    <div className="rounded-lg border border-stone-200 bg-white px-5 py-4">
      <p className="text-sm text-stone-600">{label}</p>
      <p className="mt-2 text-3xl font-semibold tracking-tight text-stone-900 tabular-nums">{value ?? '—'}</p>
      {hint && <p className="mt-1 text-xs text-stone-500">{hint}</p>}
    </div>
  )
}
