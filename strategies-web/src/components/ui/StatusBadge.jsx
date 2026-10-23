import { resolveEnum } from '../../config/enums'

const TONE_STYLES = {
  neutral: 'bg-stone-100 text-stone-700 ring-stone-200',
  info: 'bg-sky-50 text-sky-800 ring-sky-200',
  warning: 'bg-amber-50 text-amber-800 ring-amber-200',
  success: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  danger: 'bg-brand-50 text-brand-700 ring-brand-200',
}

// `label` lets a screen word a status for its audience (e.g. "Needs replacing" for clients)
// while keeping the tone, so the colour still means the same thing everywhere.
// `tone` is for statuses that are not a backend enum, such as an account being active or suspended.
export default function StatusBadge({ type, value, label: labelOverride, tone: toneOverride }) {
  const option = resolveEnum(type, value)
  const tone = toneOverride ?? option?.tone ?? 'neutral'
  const label = labelOverride ?? option?.label ?? (value === null || value === undefined ? 'Unknown' : String(value))

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-md px-2 py-0.5 text-xs font-medium whitespace-nowrap ring-1 ring-inset ${TONE_STYLES[tone]}`}
    >
      <span className="size-1.5 rounded-full bg-current opacity-70" aria-hidden="true" />
      {label}
    </span>
  )
}
