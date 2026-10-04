import { enumOrder, resolveEnum } from '../config/enums'
import { listItems } from './listData'

// The client's most recently opened case. Clients normally have one; if there are more, the newest wins.
export function latestCase(cases) {
  return [...cases].sort((a, b) => String(b.createdAt).localeCompare(String(a.createdAt)))[0] ?? null
}

// PATCH /api/cases/{id}/stage takes { stage } as the stage name and answers 403 when a consultant
// moves a case that is not assigned to them. It also accepts notes but does not store them, so none
// are sent. Any stage is accepted, so a case can be moved back to correct a mistake.
export function stagePath(caseId) {
  return `/api/cases/${encodeURIComponent(caseId)}/stage`
}

export function stageBody(stage) {
  return { stage }
}

// The API accepts stage changes on archived cases; the portal keeps archived cases read-only.
export function canChangeStage(caseItem) {
  return Boolean(caseItem) && !caseItem.isArchived
}

// Checked before the request so a missing or unchanged stage never reaches the API.
export function stageChangeError(currentStage, nextStage) {
  const next = resolveEnum('caseStage', nextStage)
  if (!next) return 'Choose the stage to move this case to.'
  if (next.value === resolveEnum('caseStage', currentStage)?.value) {
    return 'The case is already at this stage. Choose a different one.'
  }
  return null
}

// The button names the move, so a step backwards is never made by accident.
export function stageActionLabel(currentStage, nextStage) {
  const next = resolveEnum('caseStage', nextStage)
  if (!next) return 'Update stage'
  const current = enumOrder('caseStage', currentStage)
  const isBack = current !== null && enumOrder('caseStage', nextStage) < current
  return `${isBack ? 'Move back to' : 'Move to'} ${next.label.toLowerCase()}`
}

// PATCH /api/cases/{id}/assign (Admin only) takes { consultantId }. The API stores any id it is
// given without checking it belongs to an active consultant, so the portal only ever offers those.
export function assignPath(caseId) {
  return `/api/cases/${encodeURIComponent(caseId)}/assign`
}

export function assignBody(consultantId) {
  return { consultantId }
}

// GET /api/admin/users has no role filter, so one page of accounts is loaded and the active
// consultants are picked from it. `complete` is false when the API holds more accounts than
// that page, so the screen can say some may be missing instead of silently hiding them.
export const STAFF_PAGE_SIZE = 100
export const STAFF_LOOKUP_PATH = `/api/admin/users?page=1&pageSize=${STAFF_PAGE_SIZE}`

export function consultantChoices(response) {
  const users = listItems(response)
  const consultants = users
    .filter((user) => user?.id && resolveEnum('userRole', user.role)?.value === 'Consultant' && user.isActive !== false)
    .sort((a, b) => String(a.fullName).localeCompare(String(b.fullName)))
  const total = response?.totalRecords
  return { consultants, complete: typeof total !== 'number' || total <= users.length }
}

// The case list sends the consultant's name, not their id. The current consultant is only
// recognised when exactly one active consultant has that name; otherwise none is marked.
export function currentConsultantId(consultants, consultantName) {
  const matches = consultants.filter((consultant) => consultant.fullName === consultantName)
  return matches.length === 1 ? matches[0].id : null
}

export function assignedConsultantName(caseItem) {
  const name = typeof caseItem?.consultantName === 'string' ? caseItem.consultantName.trim() : ''
  return name && name !== 'Unassigned' ? name : null
}

export function assignErrorMessage(error) {
  if (error?.status === 403) return 'Only administrators can assign consultants.'
  if (error?.status === 404) return 'This case no longer exists. Close the panel and refresh the list.'
  if (error?.status === 400) return 'That consultant wasn’t accepted. Refresh the page and try again.'
  return error?.message || 'The consultant could not be assigned. Please try again.'
}

// A 400 here is the framework's raw validation body, so it is replaced with plain language.
export function stageErrorMessage(error) {
  if (error?.status === 403) return 'This case isn’t assigned to you, so you can’t change its stage.'
  if (error?.status === 404) return 'This case no longer exists. Close the panel and refresh the list.'
  if (error?.status === 400) return 'That stage wasn’t accepted. Refresh the page and try again.'
  return error?.message || 'The stage could not be saved. Please try again.'
}
