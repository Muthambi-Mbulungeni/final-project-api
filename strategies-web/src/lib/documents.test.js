import { describe, expect, it } from 'vitest'
import { documentStatus, documentSummary, documentsForClient, documentsPath } from './documents'

describe('documentsPath', () => {
  it('asks for one case’s documents in a single page', () => {
    expect(documentsPath('abc-123')).toBe('/api/documents?caseId=abc-123&page=1&pageSize=50')
  })
})

describe('documentStatus', () => {
  it('reads the status whether the API sends a name or a number', () => {
    expect(documentStatus({ status: 'Rejected' })).toBe('Rejected')
    expect(documentStatus({ status: 1 })).toBe('Approved')
    expect(documentStatus({ status: 'Unexpected' })).toBeNull()
  })
})

describe('documentsForClient', () => {
  it('hides archived documents and lists what needs action first', () => {
    const documents = [
      { id: 'approved', status: 'Approved', uploadedAt: '2026-09-03' },
      { id: 'archived', status: 'Rejected', isArchived: true, uploadedAt: '2026-09-04' },
      { id: 'pending', status: 0, uploadedAt: '2026-09-02' },
      { id: 'rejected', status: 2, uploadedAt: '2026-09-01' },
    ]
    expect(documentsForClient(documents).map((document) => document.id)).toEqual(['rejected', 'pending', 'approved'])
  })
})

describe('documentSummary', () => {
  it('counts each status whether the API sends names or numbers', () => {
    const documents = [{ status: 'Approved' }, { status: 1 }, { status: 'Pending' }, { status: 2 }]
    expect(documentSummary(documents)).toEqual({ approved: 2, pending: 1, rejected: 1 })
  })
})
