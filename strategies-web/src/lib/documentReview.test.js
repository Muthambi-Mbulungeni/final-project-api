import { describe, expect, it } from 'vitest'
import { formatDate } from './format'
import {
  awaitingDecisionCount,
  reviewDecisionLine,
  canReview,
  documentFileErrorMessage,
  documentFilePath,
  documentsForReview,
  rejectionCommentError,
  reviewBody,
  reviewErrorMessage,
  reviewPath,
} from './documentReview'

describe('awaitingDecisionCount', () => {
  it('counts pending documents on an open case, like the review buttons', () => {
    const documents = [
      { status: 'Pending' },
      { status: 0 },
      { status: 'Pending', isArchived: true },
      { status: 'Approved' },
      { status: 'Rejected' },
    ]
    expect(awaitingDecisionCount(documents, false)).toBe(2)
    expect(awaitingDecisionCount(documents, true)).toBe(0)
    expect(awaitingDecisionCount([], false)).toBe(0)
  })
})

describe('opening a document file', () => {
  it('builds the file path for one document', () => {
    expect(documentFilePath('a b')).toBe('/api/documents/a%20b/file')
  })

  it('explains why a file could not be opened', () => {
    expect(documentFileErrorMessage({ status: 404, message: 'Physical file not found.' })).toMatch(
      /isn’t stored on the server/,
    )
    expect(documentFileErrorMessage({ status: 403 })).toMatch(/isn’t assigned to you/)
    expect(documentFileErrorMessage({ status: 500, message: 'Server error' })).toBe('Server error')
    expect(documentFileErrorMessage(null)).toMatch(/could not be opened/)
  })
})

describe('reviewPath and reviewBody', () => {
  it('builds the status path for one document', () => {
    expect(reviewPath('abc')).toBe('/api/documents/abc/status')
  })

  it('sends approvals without a comment', () => {
    expect(reviewBody('Approved')).toEqual({ status: 'Approved', reviewComment: null })
  })

  it('trims the rejection comment', () => {
    expect(reviewBody('Rejected', '  Blurred scan ')).toEqual({ status: 'Rejected', reviewComment: 'Blurred scan' })
  })
})

describe('rejectionCommentError', () => {
  it('requires a non-blank comment', () => {
    expect(rejectionCommentError('   ')).toMatch(/client/i)
    expect(rejectionCommentError(undefined)).not.toBeNull()
    expect(rejectionCommentError('Blurred scan')).toBeNull()
  })
})

describe('reviewErrorMessage', () => {
  it('explains a 403 as a case that is not assigned', () => {
    expect(reviewErrorMessage({ status: 403, message: 'The request failed with status 403.' })).toMatch(/assigned to you/)
  })

  it('uses the API message, or a fallback', () => {
    const message = 'A review comment is required when rejecting a document.'
    expect(reviewErrorMessage({ status: 400, message })).toBe(message)
    expect(reviewErrorMessage({})).toMatch(/could not be saved/)
  })
})

describe('canReview', () => {
  it('allows only pending documents on open cases', () => {
    expect(canReview({ status: 'Pending' }, false)).toBe(true)
    expect(canReview({ status: 0 }, false)).toBe(true)
    expect(canReview({ status: 'Approved' }, false)).toBe(false)
    expect(canReview({ status: 'Pending' }, true)).toBe(false)
    expect(canReview({ status: 'Pending', isArchived: true }, false)).toBe(false)
  })
})

describe('documentsForReview', () => {
  it('puts pending first, hides archived, newest first within a status', () => {
    const documents = [
      { id: 'a', status: 'Approved', uploadedAt: '2026-09-01' },
      { id: 'b', status: 'Pending', uploadedAt: '2026-09-02' },
      { id: 'c', status: 'Rejected', uploadedAt: '2026-09-03' },
      { id: 'd', status: 'Pending', uploadedAt: '2026-09-05' },
      { id: 'e', status: 'Pending', uploadedAt: '2026-09-06', isArchived: true },
    ]
    expect(documentsForReview(documents).map((document) => document.id)).toEqual(['d', 'b', 'c', 'a'])
  })
})

describe('reviewDecisionLine', () => {
  const reviewedAt = '2026-10-22T10:00:00Z'

  it('says who approved or rejected a document, and when', () => {
    expect(reviewDecisionLine({ status: 'Approved', reviewedByName: 'Nomvula Zulu', reviewedAt })).toBe(
      `Approved by Nomvula Zulu · ${formatDate(reviewedAt)}`,
    )
    expect(reviewDecisionLine({ status: 2, reviewedByName: 'System Admin', reviewedAt })).toMatch(
      /^Rejected by System Admin · 22 Oct 2026$/,
    )
  })

  it('leaves the date out when it is missing', () => {
    expect(reviewDecisionLine({ status: 'Approved', reviewedByName: 'Nomvula Zulu' })).toBe('Approved by Nomvula Zulu')
  })

  it('shows nothing for a pending document or when no reviewer is recorded', () => {
    expect(reviewDecisionLine({ status: 'Pending', reviewedByName: 'Nomvula Zulu', reviewedAt })).toBeNull()
    expect(reviewDecisionLine({ status: 'Rejected', reviewedByName: null, reviewedAt })).toBeNull()
    expect(reviewDecisionLine({ status: 'Approved' })).toBeNull()
  })
})
