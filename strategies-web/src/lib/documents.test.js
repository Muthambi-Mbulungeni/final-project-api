import { describe, expect, it } from 'vitest'
import {
  MAX_UPLOAD_BYTES,
  documentStatus,
  documentSummary,
  documentsForClient,
  documentsPath,
  replacementBody,
  uploadFileError,
} from './documents'

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

  it('hides a rejected document once a newer copy of the same type is on the case', () => {
    const documents = [
      { id: 'old-passport', documentTypeId: 'passport', status: 'Rejected', uploadedAt: '2026-09-01T10:00:00Z' },
      { id: 'new-passport', documentTypeId: 'passport', status: 'Pending', uploadedAt: '2026-09-05T10:00:00.1234567Z' },
      { id: 'bank', documentTypeId: 'bank', status: 'Rejected', uploadedAt: '2026-09-02T10:00:00Z' },
    ]
    expect(documentsForClient(documents).map((document) => document.id)).toEqual(['bank', 'new-passport'])
  })

  it('compares upload times as dates, not text, when fractional digits differ', () => {
    // As text ".12Z" sorts after ".1234567Z", which would wrongly make the rejected copy look newer.
    // As dates the new copy (0.1234567 s) is later than the rejected one (0.12 s), so it replaces it.
    const documents = [
      { id: 'rejected', documentTypeId: 'passport', status: 'Rejected', uploadedAt: '2026-09-05T10:00:00.12Z' },
      { id: 'new-copy', documentTypeId: 'passport', status: 'Pending', uploadedAt: '2026-09-05T10:00:00.1234567Z' },
    ]
    expect(documentsForClient(documents).map((document) => document.id)).toEqual(['new-copy'])
  })
})

describe('uploadFileError', () => {
  const file = (name, size = 1024) => ({ name, size })

  it('accepts PDF, JPG and PNG files up to 5 MB, whatever the letter case', () => {
    expect(uploadFileError(file('passport.pdf'))).toBeNull()
    expect(uploadFileError(file('scan.JPEG'))).toBeNull()
    expect(uploadFileError(file('photo.png', MAX_UPLOAD_BYTES))).toBeNull()
  })

  it('explains what is wrong with a missing, empty, oversized or unsupported file', () => {
    expect(uploadFileError(null)).toBe('Choose a file to upload.')
    expect(uploadFileError(file('notes.docx'))).toBe('Choose a PDF, JPG or PNG file.')
    expect(uploadFileError(file('no-extension'))).toBe('Choose a PDF, JPG or PNG file.')
    expect(uploadFileError(file('empty.pdf', 0))).toBe('This file is empty. Choose another file.')
    expect(uploadFileError(file('big.pdf', MAX_UPLOAD_BYTES + 1))).toBe(
      'This file is larger than 5 MB. Choose a smaller file.',
    )
  })
})

describe('replacementBody', () => {
  it('keeps the case and document type, and uses the name and path the upload returned', () => {
    const rejected = { id: 'd1', caseId: 'case-1', documentTypeId: 'passport' }
    const uploaded = { fileName: 'passport-v2.pdf', filePath: '/uploads/abc.pdf' }
    expect(replacementBody(rejected, { name: 'local.pdf' }, uploaded)).toEqual({
      caseId: 'case-1',
      documentTypeId: 'passport',
      fileName: 'passport-v2.pdf',
      filePath: '/uploads/abc.pdf',
    })
  })
})

describe('documentSummary', () => {
  it('counts each status whether the API sends names or numbers', () => {
    const documents = [{ status: 'Approved' }, { status: 1 }, { status: 'Pending' }, { status: 2 }]
    expect(documentSummary(documents)).toEqual({ approved: 2, pending: 1, rejected: 1 })
  })
})
