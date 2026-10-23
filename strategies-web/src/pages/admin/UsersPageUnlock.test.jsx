// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { json } from '../../test/dom'
import ToastProvider from '../../components/ui/ToastProvider'
import { endSession, startSession } from '../../lib/session'
import UsersPage from './UsersPage'

const ME = { id: 'admin-1', fullName: 'System Admin', email: 'admin@nexus.com', role: 'Admin', isActive: true, isLocked: false }
const LOCKED = {
  id: 'cl-1',
  fullName: 'Thandiwe Mokoena',
  email: 'thandiwe@example.com',
  role: 'Client',
  isActive: true,
  isLocked: true,
  lockoutEndsAt: '2026-10-22T20:00:00Z',
}
const SUSPENDED_AND_LOCKED = { ...LOCKED, id: 'cl-2', fullName: 'Sipho Ndlovu', isActive: false }

function stubApi(patch = async () => json({ message: 'User account has been unlocked.' })) {
  const fetchMock = vi.fn(async (url, options = {}) => {
    if (options.method === 'PATCH') return patch(url)
    return json({ totalRecords: 3, totalPages: 1, currentPage: 1, data: [ME, LOCKED, SUSPENDED_AND_LOCKED] })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <UsersPage />
      </ToastProvider>
    </MemoryRouter>,
  )
}

// The table renders a desktop row and a phone card per user, so take the first match.
async function button(name) {
  return (await screen.findAllByRole('button', { name }))[0]
}

describe('UsersPage: locked accounts', () => {
  beforeEach(() => {
    endSession()
    startSession({ accessToken: 'token', refreshToken: 'refresh', role: 'Admin', userId: ME.id, displayName: ME.fullName })
  })

  it('marks a locked account and offers Unlock only for it', async () => {
    stubApi()
    renderPage()

    await button('Unlock Thandiwe Mokoena')
    expect(screen.getAllByText('Locked').length).toBeGreaterThan(0)
    expect(screen.queryByRole('button', { name: 'Unlock System Admin' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Unlock Sipho Ndlovu' })).toBeNull()
  })

  it('shows a suspended account as Suspended, not Locked, and offers Reinstate', async () => {
    stubApi()
    renderPage()

    await button('Reinstate Sipho Ndlovu')
    expect(screen.getAllByText('Suspended').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Locked')).toHaveLength(
      screen.getAllByRole('button', { name: 'Unlock Thandiwe Mokoena' }).length,
    )
  })

  it('asks first, then calls reinstate, clears the badge and shows a toast', async () => {
    const fetchMock = stubApi()
    renderPage()
    fireEvent.click(await button('Unlock Thandiwe Mokoena'))

    const dialog = within(screen.getByRole('dialog', { name: 'Unlock this account?' }))
    expect(fetchMock.mock.calls.filter(([, o]) => o?.method === 'PATCH')).toHaveLength(0)
    fireEvent.click(dialog.getByRole('button', { name: 'Unlock account' }))

    await waitFor(() => expect(screen.queryByRole('button', { name: 'Unlock Thandiwe Mokoena' })).toBeNull())
    const patches = fetchMock.mock.calls.filter(([, o]) => o?.method === 'PATCH')
    expect(patches).toHaveLength(1)
    expect(patches[0][0]).toBe('/api/admin/users/cl-1/reinstate')
    expect(screen.queryByText('Locked')).toBeNull()
    expect(await screen.findByText(/Thandiwe Mokoena: User account has been unlocked\./)).toBeTruthy()
  })

  it('keeps the badge and explains when the API refuses', async () => {
    stubApi(async () => json({ message: 'User account is already active.' }, 409))
    renderPage()
    fireEvent.click(await button('Unlock Thandiwe Mokoena'))

    const dialog = within(screen.getByRole('dialog', { name: 'Unlock this account?' }))
    fireEvent.click(dialog.getByRole('button', { name: 'Unlock account' }))

    expect(await dialog.findByText('User account is already active.')).toBeTruthy()
    expect(screen.getAllByText('Locked').length).toBeGreaterThan(0)
  })
})
