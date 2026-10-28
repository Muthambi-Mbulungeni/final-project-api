// Signing in and out, and restoring a session when the page loads.
// Token storage and renewal live in session.js and api.js.

import { ApiError, apiPost, refreshSession } from './api'
import {
  endSession,
  getAccessToken,
  getSession,
  getStoredRefreshToken,
  mayRestoreSession,
  ROLES,
  startSession,
} from './session'

let restoreAttempt = null
let restoreFinished = false

// Signs in and starts a session. Throws an ApiError the login page can explain.
export async function signIn(email, password) {
  const auth = await apiPost('/api/auth/login', { email, password })
  const user = startSession(auth)
  if (!user) throw new ApiError('Sign-in returned an unexpected response. Please try again.', { status: 502 })
  return user
}

// Revokes the refresh token on the server, then clears the session on this device.
// Signing out always succeeds locally, even if the server cannot be reached.
export async function signOut() {
  const refreshToken = getStoredRefreshToken()
  try {
    if (getAccessToken() || refreshToken) {
      await apiPost('/api/auth/revoke', refreshToken ? { refreshToken } : undefined)
    }
  } catch {
    // Best effort: the refresh token also expires on its own.
  } finally {
    endSession()
  }
}

// On page load or in a new tab, silently asks for a new access token.
// The browser sends the httpOnly refresh cookie by itself. Tried once per page load, and only
// when there may be a session: a first-time visitor's request could only fail (a console error).
// A failed restore clears what is left, so a stale session costs at most one request.
export function restoreSession() {
  if (getSession()) return Promise.resolve(true)
  if (!restoreAttempt) {
    const attempt = mayRestoreSession() ? refreshSession() : Promise.resolve(false)
    restoreAttempt = attempt
      .catch(() => false)
      .then((restored) => {
        if (!restored) endSession()
        return restored
      })
      .finally(() => {
        restoreFinished = true
      })
  }
  return restoreAttempt
}

export function hasRestoreFinished() {
  return restoreFinished
}

// Where to go after signing in: back to the page the user wanted, but only if it is inside
// their own workspace and on this site. Anything else goes to their workspace home.
export function destinationFor(user, from) {
  const home = ROLES[user?.role]?.home ?? '/login'
  if (typeof from !== 'string' || !from.startsWith('/') || from.startsWith('//')) return home
  const insideHome = from === home || from.startsWith(`${home}/`) || from.startsWith(`${home}?`)
  return insideHome ? from : home
}

const NOTICES = {
  expired: 'Your session has expired. Please sign in again.',
  idle: 'You were signed out after 30 minutes without activity, to protect client information.',
  'signed-out': 'You have signed out.',
  'password-reset': 'Your password has been reset. Sign in with your new password.',
  'email-confirmed': 'Your email is confirmed. Sign in to continue.',
}

// Explains why the person is on the sign-in page, when there is a reason.
export function signInNotice(reason) {
  return NOTICES[reason] ?? null
}
