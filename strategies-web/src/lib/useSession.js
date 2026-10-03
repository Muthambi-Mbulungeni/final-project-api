import { useEffect, useState, useSyncExternalStore } from 'react'
import { hasRestoreFinished, restoreSession } from './auth'
import { getSession, getSessionVersion, subscribe } from './session'

// The signed-in user. Re-renders when someone signs in or out, or the session expires.
export function useSession() {
  useSyncExternalStore(subscribe, getSessionVersion, getSessionVersion)
  return getSession()
}

// Like useSession, but first tries to restore a session from the refresh cookie.
// `checking` is true while that silent request is in progress.
export function useRestoredSession() {
  const session = useSession()
  const [checking, setChecking] = useState(() => session === null && !hasRestoreFinished())

  useEffect(() => {
    if (!checking) return undefined
    let active = true
    restoreSession().finally(() => {
      if (active) setChecking(false)
    })
    return () => {
      active = false
    }
  }, [checking])

  return { session, checking: checking && session === null }
}
