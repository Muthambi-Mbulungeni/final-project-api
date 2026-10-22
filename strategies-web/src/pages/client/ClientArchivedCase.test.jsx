// @vitest-environment jsdom
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { json } from '../../test/dom'
import ToastProvider from '../../components/ui/ToastProvider'
import { endSession, startSession } from '../../lib/session'
import ClientDashboard from './ClientDashboard'
import ClientDocumentsPage from './ClientDocumentsPage'

const CLIENT_ID = 'client-1'

function caseItem(isArchived) {
  return {
    id: 'case-1',
    stage: 'UnderReview',
    isArchived,
    createdAt: '2026-09-01T08:00:00Z',
    consultantName: 'Nomvula Zulu',
    visaCategoryName: 'Critical Skills',
  }
}

const REJECTED = {
  id: 'doc-1',
  documentTypeId: 'type-1',
  documentTypeName: 'Medical report',
  fileName: 'medical.pdf',
  status: 'Rejected',
  reviewComment: 'The scan is cut off.',
  uploadedAt: '2026-09-10T08:00:00Z',
  isArchived: false,
}

// Routes the requests each page makes: the client's cases, the case's documents and its messages.
function stubApi(isArchived) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url) => {
      if (String(url).includes('/api/cases/client/')) return json({ data: [caseItem(isArchived)] })
      if (String(url).includes('/api/documents')) return json({ data: [REJECTED] })
      return json({ data: [], totalRecords: 0, totalPages: 1, currentPage: 1 })
    }),
  )
}

function renderPage(element) {
  return render(
    <MemoryRouter>
      <ToastProvider>{element}</ToastProvider>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  endSession()
  startSession({ accessToken: 'token', refreshToken: 'refresh', role: 'Client', userId: CLIENT_ID, displayName: 'Thandiwe Mokoena' })
})

describe('client dashboard: archived case', () => {
  it('says the case is closed and offers no document prompt', async () => {
    stubApi(true)
    renderPage(<ClientDashboard />)

    expect(await screen.findByRole('heading', { name: 'This case is archived' })).toBeTruthy()
    expect(screen.getByText('Where it ended')).toBeTruthy()
    expect(screen.queryByText('Action needed')).toBeNull()
    expect(screen.queryByText('What happens next')).toBeNull()
    expect(screen.queryByRole('link', { name: /replace|go to documents/i })).toBeNull()
  })

  it('still shows the document prompt on an open case', async () => {
    stubApi(false)
    renderPage(<ClientDashboard />)

    expect(await screen.findByText('Action needed')).toBeTruthy()
    expect(screen.getByText('What happens next')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'This case is archived' })).toBeNull()
  })
})

describe('client documents page: archived case', () => {
  it('keeps the history readable but removes the upload forms', async () => {
    stubApi(true)
    renderPage(<ClientDocumentsPage />)

    expect(await screen.findByRole('heading', { name: 'This case is archived' })).toBeTruthy()
    expect(screen.getByText('Medical report')).toBeTruthy()
    expect(screen.getByText('1 not accepted')).toBeTruthy()
    expect(screen.queryByText('Action needed')).toBeNull()
    expect(screen.queryByLabelText(/upload a new copy/i)).toBeNull()
    expect(screen.queryByRole('button', { name: /send new copy/i })).toBeNull()
  })

  it('offers the upload form on an open case', async () => {
    stubApi(false)
    renderPage(<ClientDocumentsPage />)

    expect(await screen.findByText('Action needed')).toBeTruthy()
    expect(screen.getByLabelText(/upload a new copy/i)).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'This case is archived' })).toBeNull()
  })
})
