// Development-only session.
// Lets the team preview each role's workspace before real authentication exists.
// Replaced by JWT-based auth once the security layer is merged.

const STORAGE_KEY = 'smp.devSession'

export const ROLES = {
  admin: { key: 'admin', label: 'Administrator', home: '/admin' },
  consultant: { key: 'consultant', label: 'Consultant', home: '/consultant' },
  client: { key: 'client', label: 'Client', home: '/client' },
}

// Fixed demo IDs so endpoints that need a user ID (e.g. /api/stats/client/{clientId}) can be called.
const DEMO_USERS = {
  admin: { id: 'a1000000-0000-4000-8000-000000000001', name: 'Thandi Mokoena', email: 'admin@example.com' },
  consultant: { id: 'c1000000-0000-4000-8000-000000000001', name: 'Pieter van Wyk', email: 'consultant@example.com' },
  client: { id: 'e1000000-0000-4000-8000-000000000001', name: 'Amara Okafor', email: 'client@example.com' },
}

export function getSession() {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const session = JSON.parse(raw)
    if (!ROLES[session?.role]) return null
    // Fills in fields (such as id) missing from sessions saved by an older version.
    return { ...DEMO_USERS[session.role], ...session }
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
