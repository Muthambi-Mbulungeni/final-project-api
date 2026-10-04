import { resolveEnum } from '../config/enums'

// How each review status is described to clients. Staff screens keep the API's own wording.
export const CLIENT_STATUS_LABELS = {
  Pending: 'Awaiting review',
  Approved: 'Approved',
  Rejected: 'Needs replacing',
}

// One case's documents. The API pages them, but a case holds a handful, so one page of 50 covers it.
export function documentsPath(caseId) {
  const params = new URLSearchParams({ caseId: String(caseId), page: '1', pageSize: '50' })
  return `/api/documents?${params.toString()}`
}

// The API may send the status as its enum name or its number.
export function documentStatus(document) {
  return resolveEnum('documentStatus', document.status)?.value ?? null
}

// What the client should act on first: documents to replace, then those awaiting review, then approved.
const CLIENT_ORDER = { Rejected: 0, Pending: 1, Approved: 2 }

export function documentsForClient(documents) {
  return documents
    .filter((document) => !document.isArchived)
    .sort(
      (a, b) =>
        (CLIENT_ORDER[documentStatus(a)] ?? 3) - (CLIENT_ORDER[documentStatus(b)] ?? 3) ||
        String(b.uploadedAt).localeCompare(String(a.uploadedAt)),
    )
}

export function documentSummary(documents) {
  const counts = { approved: 0, pending: 0, rejected: 0 }
  for (const document of documents) {
    const status = documentStatus(document)
    if (status === 'Approved') counts.approved += 1
    else if (status === 'Pending') counts.pending += 1
    else if (status === 'Rejected') counts.rejected += 1
  }
  return counts
}
