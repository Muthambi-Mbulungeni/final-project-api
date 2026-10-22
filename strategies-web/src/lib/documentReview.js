import { documentStatus } from './documents'
import { formatDate } from './format'

// PATCH /api/documents/{id}/status takes { status: 'Approved' | 'Rejected', reviewComment }.
// The API answers 400 when a rejection has no comment, and 403 (empty body) when a consultant
// reviews a case that is not assigned to them.

export function reviewPath(documentId) {
  return `/api/documents/${encodeURIComponent(documentId)}/status`
}

export function reviewBody(status, comment) {
  const trimmed = typeof comment === 'string' ? comment.trim() : ''
  return { status, reviewComment: trimmed || null }
}

// Same rule as the API, checked first so the reviewer gets the message without a round trip.
export function rejectionCommentError(comment) {
  return typeof comment === 'string' && comment.trim()
    ? null
    : 'Explain what the client needs to fix. They are shown this note.'
}

export function reviewErrorMessage(error) {
  if (error?.status === 403) return 'This case isn’t assigned to you, so you can’t review its documents.'
  if (error?.status === 404) return 'This document no longer exists. Close the panel and open the case again.'
  return error?.message || 'The review could not be saved. Please try again.'
}

// How many documents on this case still need a staff decision: the same rule as the
// Approve / Reject buttons, so the summary and the buttons can never disagree.
export function awaitingDecisionCount(documents, caseArchived) {
  return documents.filter((document) => canReview(document, caseArchived)).length
}

// GET /api/documents/{id}/file streams the stored file. Clients only reach their own cases and
// consultants only their assigned ones (403 otherwise). The API answers 404 both for an unknown
// document and for a record whose file is not on the server (e.g. demo rows with no upload).
export function documentFilePath(documentId) {
  return `/api/documents/${encodeURIComponent(documentId)}/file`
}

export function documentFileErrorMessage(error) {
  if (error?.status === 403) return 'This case isn’t assigned to you, so you can’t open its files.'
  if (error?.status === 404) return 'This file isn’t stored on the server, so it can’t be opened. Ask the client for a new copy.'
  return error?.message || 'The file could not be opened. Please try again.'
}

// Only pending documents on open cases can be reviewed: re-reviewing is not a defined workflow.
export function canReview(document, caseArchived) {
  return !caseArchived && !document.isArchived && documentStatus(document) === 'Pending'
}

// What the reviewer acts on first: pending, then rejected, then approved; newest first within each.
const REVIEW_ORDER = { Pending: 0, Rejected: 1, Approved: 2 }

export function documentsForReview(documents) {
  return documents
    .filter((document) => !document.isArchived)
    .sort(
      (a, b) =>
        (REVIEW_ORDER[documentStatus(a)] ?? 3) - (REVIEW_ORDER[documentStatus(b)] ?? 3) ||
        String(b.uploadedAt).localeCompare(String(a.uploadedAt)),
    )
}

// "Approved by Nomvula Zulu · 22 Oct 2026" for the staff panel, from reviewedByName and reviewedAt.
// Nothing for a pending document or a decision with no recorded reviewer (the badge shows the status).
const DECISION_VERBS = { Approved: 'Approved', Rejected: 'Rejected' }

export function reviewDecisionLine(document) {
  const verb = DECISION_VERBS[documentStatus(document)]
  const name = typeof document?.reviewedByName === 'string' ? document.reviewedByName.trim() : ''
  if (!verb || !name) return null
  return document.reviewedAt ? `${verb} by ${name} · ${formatDate(document.reviewedAt)}` : `${verb} by ${name}`
}
