import { useCallback, useEffect, useRef, useState } from 'react'

// Staff workspaces show many clients' personal documents, so they sign out when left unattended.
export const STAFF_IDLE_TIMEOUT_MS = 30 * 60 * 1000

// WCAG 2.2.1 (Timing Adjustable): warn before the limit and let the user extend it.
// 60 seconds is three times the minimum of 20 seconds that WCAG asks for.
export const IDLE_WARNING_MS = 60 * 1000

const ACTIVITY_EVENTS = ['pointerdown', 'pointermove', 'keydown', 'wheel', 'touchstart']
const RESET_THROTTLE_MS = 1000

// Calls onWarn(endsAt) `warningMs` before the timeout, then onIdle at the timeout.
// touch() records activity, throttled to once a second so mouse movement stays cheap.
// Once the warning is showing, touch() is ignored: only extend() (a deliberate "Stay signed in")
// restarts the countdown, so a stray mouse movement cannot dismiss the warning unseen.
export function createIdleTimer(timeoutMs, onIdle, { warningMs = 0, onWarn } = {}) {
  let warnTimer = null
  let idleTimer = null
  let lastReset = 0
  let warning = false

  function clear() {
    clearTimeout(warnTimer)
    clearTimeout(idleTimer)
  }

  function reset() {
    clear()
    warning = false
    lastReset = Date.now()
    if (onWarn && warningMs > 0 && warningMs < timeoutMs) {
      warnTimer = setTimeout(() => {
        warning = true
        onWarn(Date.now() + warningMs)
      }, timeoutMs - warningMs)
    }
    idleTimer = setTimeout(onIdle, timeoutMs)
  }

  reset()

  return {
    touch() {
      if (!warning && Date.now() - lastReset >= RESET_THROTTLE_MS) reset()
    },
    extend() {
      reset()
    },
    stop() {
      clear()
    },
  }
}

// While `enabled`, warns before signing out after a period without keyboard, mouse or touch activity.
// Returns warningEndsAt (null when no warning is showing) and extend() for the "Stay signed in" button.
export function useIdleSignOut(enabled, onIdle, { timeoutMs = STAFF_IDLE_TIMEOUT_MS, warningMs = IDLE_WARNING_MS } = {}) {
  const onIdleRef = useRef(onIdle)
  const timerRef = useRef(null)
  const [warningEndsAt, setWarningEndsAt] = useState(null)

  useEffect(() => {
    onIdleRef.current = onIdle
  })

  useEffect(() => {
    if (!enabled) return undefined

    const idle = createIdleTimer(
      timeoutMs,
      () => {
        setWarningEndsAt(null)
        onIdleRef.current()
      },
      { warningMs, onWarn: setWarningEndsAt },
    )
    timerRef.current = idle
    const touch = () => idle.touch()
    ACTIVITY_EVENTS.forEach((type) => window.addEventListener(type, touch, { passive: true }))

    return () => {
      idle.stop()
      timerRef.current = null
      ACTIVITY_EVENTS.forEach((type) => window.removeEventListener(type, touch))
    }
  }, [enabled, timeoutMs, warningMs])

  const extend = useCallback(() => {
    setWarningEndsAt(null)
    timerRef.current?.extend()
  }, [])

  return { warningEndsAt: enabled ? warningEndsAt : null, extend }
}
