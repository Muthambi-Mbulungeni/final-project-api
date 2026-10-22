import { resolveEnum } from '../config/enums'
import { formatDate } from './format'

// Who wrote a review note, e.g. "Nomvula Zulu, 22 Oct 2026". The API sends reviewedByName and
// reviewedAt; rows reviewed before it did (or with no reviewer) fall back to "Your consultant".
export function reviewerSignature(document) {
  const name = typeof document?.reviewedByName === 'string' ? document.reviewedByName.trim() : ''
  if (!name) return 'Your consultant'
  return document.reviewedAt ? `${name}, ${formatDate(document.reviewedAt)}` : name
}

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

// A rejected document counts as replaced once a newer document of the same type is on the case.
// Only staff can archive documents, so the client's old copy stays in the API; it is hidden from
// the client here, and staff still see it. Limit: if a case ever holds two separate documents of
// one type, a newer one also hides an older rejected one.
// Compared as dates: the API's timestamps vary in their number of fractional digits.
function uploadedTime(document) {
  const time = Date.parse(document.uploadedAt)
  return Number.isNaN(time) ? 0 : time
}

function isReplaced(document, documents) {
  if (documentStatus(document) !== 'Rejected' || !document.documentTypeId) return false
  return documents.some(
    (other) =>
      other !== document &&
      other.documentTypeId === document.documentTypeId &&
      uploadedTime(other) > uploadedTime(document),
  )
}

export function documentsForClient(documents) {
  const current = documents.filter((document) => !document.isArchived)
  return current
    .filter((document) => !isReplaced(document, current))
    .sort(
      (a, b) =>
        (CLIENT_ORDER[documentStatus(a)] ?? 3) - (CLIENT_ORDER[documentStatus(b)] ?? 3) ||
        String(b.uploadedAt).localeCompare(String(a.uploadedAt)),
    )
}

// What the client still has to replace: rejected documents with no newer copy (same rule as above).
export function documentsToReplace(documents) {
  return documentsForClient(documents).filter((document) => documentStatus(document) === 'Rejected')
}

// The client dashboard's button: names the document when there is exactly one to replace.
export function replaceActionLabel(documents) {
  const names = documents.map((document) => document.documentTypeName).filter(Boolean)
  if (documents.length === 1 && names.length === 1) return `Replace ${names[0].toLowerCase()}`
  return 'Go to documents'
}

// POST /api/documents/upload accepts PDF, JPG and PNG files up to 5 MB. Checked here first so the
// client hears straight away; the API checks again and its message is shown if it disagrees.
export const UPLOAD_PATH = '/api/documents/upload'
export const UPLOAD_ACCEPT = '.pdf,.jpg,.jpeg,.png'
export const MAX_UPLOAD_BYTES = 5 * 1024 * 1024
const UPLOAD_EXTENSIONS = UPLOAD_ACCEPT.split(',')

export function uploadFileError(file) {
  if (!file) return 'Choose a file to upload.'
  const name = String(file.name ?? '').toLowerCase()
  const extension = name.includes('.') ? name.slice(name.lastIndexOf('.')) : ''
  if (!UPLOAD_EXTENSIONS.includes(extension)) return 'Choose a PDF, JPG or PNG file.'
  if (file.size === 0) return 'This file is empty. Choose another file.'
  if (file.size > MAX_UPLOAD_BYTES) return 'This file is larger than 5 MB. Choose a smaller file.'
  return null
}

// POST /api/documents body for a new copy: same case and document type as the copy it replaces,
// with the file name and path the upload returned.
export function replacementBody(document, file, uploaded) {
  return {
    caseId: document.caseId,
    documentTypeId: document.documentTypeId,
    fileName: uploaded?.fileName || file.name,
    filePath: uploaded?.filePath,
  }
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
