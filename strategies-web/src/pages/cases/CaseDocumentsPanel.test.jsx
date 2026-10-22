// @vitest-environment jsdom
import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { json } from '../../test/dom'
import ToastProvider from '../../components/ui/ToastProvider'
import { endSession, startSession } from '../../lib/session'
import CaseDocumentsPanel from './CaseDocumentsPanel'

const LONG_NAME = 'police-clearance-certificate-lerato-mahlangu-certified-copy-2026.pdf'

const CASE = {
  id: 'case-1',
  clientName: 'Lerato Mahlangu',
  consultantName: 'Nomvula Zulu',
  visaCategoryName: 'Critical Skills Work Visa',
  stage: 'DocumentGathering',
  isArchived: false,
}

function stubApi(documents) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url) => {
      if (String(url).includes('/messages')) return json({ totalRecords: 0, totalPages: 1, currentPage: 1, data: [] })
      return json({ totalRecords: documents.length, totalPages: 1, currentPage: 1, data: documents })
    }),
  )
}

function renderPanel() {
  return render(
    <ToastProvider>
      <CaseDocumentsPanel caseItem={CASE} onClose={() => {}} onStageChanged={() => {}} />
    </ToastProvider>,
  )
}

describe('CaseDocumentsPanel document file line', () => {
  beforeEach(() => {
    endSession()
    startSession({ accessToken: 'token', refreshToken: 'refresh', role: 'Consultant', userId: 'consultant-1', displayName: 'Nomvula Zulu' })
  })

  it('cuts a long file name short with the full name on hover, and keeps the upload date whole', async () => {
    stubApi([
      {
        id: 'doc-1',
        caseId: CASE.id,
        documentTypeName: 'Police clearance',
        fileName: LONG_NAME,
        status: 'Pending',
        uploadedAt: '2026-10-22T08:30:00Z',
      },
    ])
    renderPanel()

    const name = await screen.findByText(LONG_NAME)
    expect(name.className).toContain('truncate')
    expect(name.getAttribute('title')).toBe(LONG_NAME)

    const date = screen.getByText(/Uploaded/)
    expect(date).not.toBe(name)
    expect(date.textContent).not.toContain(LONG_NAME)
    expect(date.closest('.truncate')).toBeNull()
    expect(date.className).toContain('shrink-0')
  })

  it('shows "No file name" without a tooltip when the file has no name', async () => {
    stubApi([{ id: 'doc-2', caseId: CASE.id, documentTypeName: 'Passport', fileName: '', status: 'Approved', uploadedAt: '2026-10-22T08:30:00Z' }])
    renderPanel()

    const name = await screen.findByText('No file name')
    expect(name.hasAttribute('title')).toBe(false)
  })
})
