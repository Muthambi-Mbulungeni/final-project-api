// GET /api/admin/audit-logs sends userName: the user's full name, or "System/Unknown"
// when no user matches the row's userId. Only a real name is shown; callers fall back to the short id.
const UNKNOWN_ACTOR = 'System/Unknown'

export function actorName(entry) {
  const name = typeof entry?.userName === 'string' ? entry.userName.trim() : ''
  return name && name !== UNKNOWN_ACTOR ? name : null
}
