// Development-only session.
// Lets the team preview each role's workspace before real authentication exists.
// Replaced by JWT-based auth once the security layer is merged.

const STORAGE_KEY = 'smp.devSession'

export const ROLES = {
  admin: { key: 'admin', label: 'Administrator', home: '/admin' },
  consultant: { key: 'consultant', label: 'Consultant', home: '/consultant' },
  client: { key: 'client', label: 'Client', home: '/client' },
}

const DEMO_USERS = {
  admin: { name: 'Thandi Mokoena', email: 'admin@example.com' },
  consultant: { name: 'Pieter van Wyk', email: 'consultant@example.com' },
  client: { name: 'Amara Okafor', email: 'client@example.com' },
}

export function getSession() {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const session = JSON.parse(raw)
    return ROLES[session?.role] ? session : null
  } catch {
    return null
  }
}

export function startDevSession(role) {
  const session = { role, ...DEMO_USERS[role] }
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    // Storage unavailable; the session will not persist across reloads.
  }
  return session
}

export function endSession() {
  try {
    window.localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Storage unavailable; nothing to clear.
  }
}
