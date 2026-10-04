// GET /api/admin/audit-logs sends userName: the user's full name, or "System/Unknown"
// when no user matches the row's userId. Only a real name is shown; callers fall back to the short id.
const UNKNOWN_ACTOR = 'System/Unknown'

export function actorName(entry) {
  const name = typeof entry?.userName === 'string' ? entry.userName.trim() : ''
  return name && name !== UNKNOWN_ACTOR ? name : null
}

// Names that more than one user on this page shares. Only those need the short id beside the
// name to tell the people apart; elsewhere the full id is in the tooltip and the user filter.
export function sharedNames(entries = []) {
  const idsByName = new Map()
  for (const entry of entries) {
    const name = actorName(entry)
    if (!name || !entry.userId) continue
    if (!idsByName.has(name)) idsByName.set(name, new Set())
    idsByName.get(name).add(entry.userId)
  }
  return new Set([...idsByName].filter(([, ids]) => ids.size > 1).map(([name]) => name))
}

// One line of context under an activity: who did it, and to what kind of record.
export function activityContext(entry) {
  const name = actorName(entry)
  const record = typeof entry?.entityType === 'string' && entry.entityType.trim() ? entry.entityType.trim() : null
  if (name && record) return `${name} · ${record}`
  return name ?? (record ? `${record} record` : null)
}

// Some API endpoints log code names ("UpdateLeadStatus") while others and the seed data log
// sentences ("Advanced case stage"). Known code names get the matching sentence.
const ACTION_LABELS = {
  CreateCase: 'Created case',
  UpdateCaseStage: 'Changed case stage',
  AssignConsultant: 'Assigned consultant',
  UpdateLeadStatus: 'Reviewed lead',
  ArchiveDocument: 'Archived document',
}

// Any other code name is split into words ("SuspendUser" -> "Suspend user"); sentences pass through.
export function actionLabel(action) {
  const text = typeof action === 'string' ? action.trim() : ''
  if (!text) return '—'
  if (ACTION_LABELS[text]) return ACTION_LABELS[text]
  if (!/^[A-Z][A-Za-z]+$/.test(text)) return text
  const words = text.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  return words.charAt(0).toUpperCase() + words.slice(1)
}
