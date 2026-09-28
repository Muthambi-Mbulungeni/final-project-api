import { resolveEnum } from '../../config/enums'

const TONE_STYLES = {
  neutral: 'bg-stone-100 text-stone-700 ring-stone-200',
  info: 'bg-sky-50 text-sky-800 ring-sky-200',
  warning: 'bg-amber-50 text-amber-800 ring-amber-200',
  success: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  danger: 'bg-brand-50 text-brand-700 ring-brand-200',
}

export default function StatusBadge({ type, value }) {
  const option = resolveEnum(type, value)
  const tone = option?.tone ?? 'neutral'
  const label = option?.label ?? (value === null || value === undefined ? 'Unknown' : String(value))

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-md px-2 py-0.5 text-xs font-medium whitespace-nowrap ring-1 ring-inset ${TONE_STYLES[tone]}`}
    >
      <span className="size-1.5 rounded-full bg-current opacity-70" aria-hidden="true" />
      {label}
    </span>
  )
}
