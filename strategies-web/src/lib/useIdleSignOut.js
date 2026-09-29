import { useEffect, useRef } from 'react'

// Staff workspaces show many clients' personal documents, so they sign out when left unattended.
export const STAFF_IDLE_TIMEOUT_MS = 30 * 60 * 1000

const ACTIVITY_EVENTS = ['pointerdown', 'pointermove', 'keydown', 'wheel', 'touchstart']
const RESET_THROTTLE_MS = 1000

// Calls onIdle once after `timeoutMs` without activity. touch() records activity.
// Touches less than a second apart are ignored, so mouse movement stays cheap.
export function createIdleTimer(timeoutMs, onIdle) {
  let timer = null
  let lastReset = 0

  function reset() {
    clearTimeout(timer)
    lastReset = Date.now()
    timer = setTimeout(onIdle, timeoutMs)
  }

  reset()

  return {
    touch() {
      if (Date.now() - lastReset >= RESET_THROTTLE_MS) reset()
    },
    stop() {
      clearTimeout(timer)
    },
  }
}

// Runs onIdle after a period without keyboard, mouse or touch activity, while `enabled` is true.
export function useIdleSignOut(enabled, onIdle, timeoutMs = STAFF_IDLE_TIMEOUT_MS) {
  const onIdleRef = useRef(onIdle)

  useEffect(() => {
    onIdleRef.current = onIdle
  })

  useEffect(() => {
    if (!enabled) return undefined

    const idle = createIdleTimer(timeoutMs, () => onIdleRef.current())
    const touch = () => idle.touch()
    ACTIVITY_EVENTS.forEach((type) => window.addEventListener(type, touch, { passive: true }))

    return () => {
      idle.stop()
      ACTIVITY_EVENTS.forEach((type) => window.removeEventListener(type, touch))
    }
  }, [enabled, timeoutMs])
}
