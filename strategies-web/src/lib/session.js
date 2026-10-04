// Who is signed in.
//
// Real sessions come from the API's AuthResponse. The access token and user details live in
// memory only. The refresh token lives in an httpOnly cookie that the browser sends by itself,
// so no script on the page can read it (POPIA: we hold passports and ID documents).
// Fallback: if the API returns the refresh token in the response body instead, it is kept in
// sessionStorage (this tab only, cleared when the tab closes).
//
// Development builds also offer a dev session (the "Dev" switcher) so each workspace can be
// opened without an account. Production builds ignore it completely.

const DEV_SESSION_KEY = 'smp.devSession'
const REFRESH_TOKEN_KEY = 'smp.refreshToken'
// Not a secret: only remembers that a sign-in used the cookie, so a restore is worth trying.
const COOKIE_HINT_KEY = 'smp.cookieSession'

export const ROLES = {
  admin: { key: 'admin', label: 'Administrator', home: '/admin' },
  consultant: { key: 'consultant', label: 'Consultant', home: '/consultant' },
  client: { key: 'client', label: 'Client', home: '/client' },
}

// Fixed demo IDs so endpoints that need a user ID can be called with a dev session.
const DEMO_USERS = {
  admin: { id: 'a1000000-0000-4000-8000-000000000001', name: 'Thandi Mokoena', email: 'admin@example.com' },
  consultant: { id: 'c1000000-0000-4000-8000-000000000001', name: 'Pieter van Wyk', email: 'consultant@example.com' },
  client: { id: 'e1000000-0000-4000-8000-000000000001', name: 'Amara Okafor', email: 'client@example.com' },
}

let current = null
let version = 0
const listeners = new Set()

function readItem(storageName, key) {
  try {
    return globalThis[storageName]?.getItem(key) ?? null
  } catch {
    return null
  }
}

function writeItem(storageName, key, value) {
  try {
    globalThis[storageName]?.setItem(key, value)
  } catch {
    // Storage unavailable (e.g. private mode). The session still works for this page.
  }
}

function removeItem(storageName, key) {
  try {
    globalThis[storageName]?.removeItem(key)
  } catch {
    // Storage unavailable. Nothing to remove.
  }
}

function notify() {
  version += 1
  listeners.forEach((listener) => listener())
}

// Lets the app re-render when someone signs in or out (for useSyncExternalStore).
export function subscribe(listener) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function getSessionVersion() {
  return version
}

// Maps the API's role ("Admin", "Consultant", "Client") to a workspace key.
export function roleKeyFromApi(role) {
  if (typeof role !== 'string') return null
  const key = role.trim().toLowerCase()
  return ROLES[key] ? key : null
}

// Starts (or renews, after a refresh) a real session from an AuthResponse.
// Returns the signed-in user, or null if the response is not usable.
export function startSession(auth) {
  const role = roleKeyFromApi(auth?.role)
  if (!auth?.accessToken || !role || !auth.userId) return null

  current = {
    id: String(auth.userId),
    name: auth.displayName || '',
    role,
    accessToken: auth.accessToken,
    tokenExpiry: auth.tokenExpiry ?? null,
  }

  // Fallback B: only when the API put the refresh token in the body.
  if (auth.refreshToken) writeItem('sessionStorage', REFRESH_TOKEN_KEY, auth.refreshToken)
  else writeItem('localStorage', COOKIE_HINT_KEY, '1')

  removeItem('localStorage', DEV_SESSION_KEY)
  notify()
  return getSession()
}

export function getAccessToken() {
  return current?.accessToken ?? null
}

export function getStoredRefreshToken() {
  return readItem('sessionStorage', REFRESH_TOKEN_KEY)
}

// True when this browser may still hold a session to restore: a refresh token in this tab, or an
// earlier sign-in that used the cookie. A first-time visitor has neither, so no request is made.
export function mayRestoreSession() {
  return Boolean(getStoredRefreshToken() || readItem('localStorage', COOKIE_HINT_KEY))
}

function devSession() {
  if (!import.meta.env.DEV) return null
  const raw = readItem('localStorage', DEV_SESSION_KEY)
  if (!raw) return null

  try {
    const session = JSON.parse(raw)
    if (!ROLES[session?.role]) return null
    return { ...DEMO_USERS[session.role], ...session, source: 'dev' }
  } catch {
    return null
  }
}

// The signed-in user: { id, name, role, source }, or null. A real session wins over a dev session.
export function getSession() {
  if (current) return { id: current.id, name: current.name, role: current.role, source: 'api' }
  return devSession()
}

export function startDevSession(role) {
  if (!ROLES[role]) return null
  current = null
  removeItem('sessionStorage', REFRESH_TOKEN_KEY)
  removeItem('localStorage', COOKIE_HINT_KEY)
  writeItem('localStorage', DEV_SESSION_KEY, JSON.stringify({ role, ...DEMO_USERS[role] }))
  notify()
  return getSession()
}

// Clears everything on this device. Revoking the refresh token on the server is done by the caller.
export function endSession() {
  current = null
  removeItem('sessionStorage', REFRESH_TOKEN_KEY)
  removeItem('localStorage', COOKIE_HINT_KEY)
  removeItem('localStorage', DEV_SESSION_KEY)
  notify()
}
