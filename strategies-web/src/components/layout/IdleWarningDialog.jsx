import { useEffect, useState } from 'react'
import ConfirmDialog from '../ui/ConfirmDialog'

// Visible timer ticks every second; screen readers hear it only at about 30 and 10 seconds,
// because announcing every second would drown out everything else.
function Countdown({ endsAt, totalSeconds }) {
  const [seconds, setSeconds] = useState(totalSeconds)

  useEffect(() => {
    const id = setInterval(() => {
      setSeconds(Math.max(0, Math.ceil((endsAt - Date.now()) / 1000)))
    }, 1000)
    return () => clearInterval(id)
  }, [endsAt])

  const minutes = Math.floor(seconds / 60)
  const rest = String(seconds % 60).padStart(2, '0')
  const announcement = seconds <= 10 ? 'About 10 seconds left.' : seconds <= 30 ? 'About 30 seconds left.' : ''

  return (
    <>
      <span className="font-semibold tabular-nums text-stone-900">
        {minutes}:{rest}
      </span>
      <span className="sr-only" aria-live="polite">
        {announcement}
      </span>
    </>
  )
}

// Warning shown before an idle staff session ends (WCAG 2.2.1 Timing Adjustable).
// "Stay signed in" sits in the cancel slot, so it gets focus on open and Escape also keeps the session.
export default function IdleWarningDialog({ endsAt, warningMs, onStay, onSignOut }) {
  const open = endsAt !== null

  return (
    <ConfirmDialog
      open={open}
      title="Are you still there?"
      message={
        open ? (
          <>
            To protect clients’ personal information, staff sessions end after a period without activity.
            You’ll be signed out in <Countdown key={endsAt} endsAt={endsAt} totalSeconds={Math.round(warningMs / 1000)} />.
          </>
        ) : (
          ''
        )
      }
      cancelLabel="Stay signed in"
      confirmLabel="Sign out now"
      onCancel={onStay}
      onConfirm={onSignOut}
    />
  )
}
