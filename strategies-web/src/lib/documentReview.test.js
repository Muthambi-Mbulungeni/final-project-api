import { describe, expect, it } from 'vitest'
import {
  canReview,
  documentsForReview,
  rejectionCommentError,
  reviewBody,
  reviewErrorMessage,
  reviewPath,
} from './documentReview'

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
