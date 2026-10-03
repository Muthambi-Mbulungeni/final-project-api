import { documentStatus } from './documents'

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
