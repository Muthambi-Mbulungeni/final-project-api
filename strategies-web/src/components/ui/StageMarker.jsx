import { Check } from 'lucide-react'

// One step of the five-stage case lifecycle: a check once done, the number ringed in brand red
// while current, a plain number while upcoming. Shared by the client's progress and the staff
// case panel, so both read the lifecycle the same way. Decorative: callers say the state in text.
export default function StageMarker({ index, state }) {
  if (state === 'done') {
    return (
      <span className="relative z-10 flex size-7 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white">
        <Check className="size-4" strokeWidth={2.5} aria-hidden="true" />
      </span>
    )
  }

  if (state === 'current') {
    return (
      <span className="relative z-10 flex size-7 shrink-0 items-center justify-center rounded-full border-2 border-brand-600 bg-white text-xs font-semibold text-brand-700 ring-4 ring-brand-100">
        {index + 1}
      </span>
    )
  }

  return (
    <span className="relative z-10 flex size-7 shrink-0 items-center justify-center rounded-full border border-stone-300 bg-white text-xs font-semibold text-stone-500">
      {index + 1}
    </span>
  )
}
