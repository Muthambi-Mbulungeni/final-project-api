// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { json } from '../test/dom'
import { endSession } from '../lib/session'
import Login from './Login'

const RESEND_PATH = '/api/auth/resend-confirmation'
const SENT = "If that email has an account, we've sent a new link."

function stubFetch(loginResponse) {
  const fetchMock = vi.fn(async (url) => {
    if (String(url).endsWith('/api/auth/login')) return loginResponse()
    return json({ message: 'If an account with that email needs confirming, a new confirmation link has been sent.' })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function signInAs(email) {
  render(
    <MemoryRouter>
      <Login />
    </MemoryRouter>,
  )
  fireEvent.change(screen.getByLabelText('Email address'), { target: { value: email } })
  fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'a-password-123' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sign in' }))
}

describe('Login: unconfirmed accounts', () => {
  beforeEach(() => endSession())

  it('offers to resend the confirmation email after a 401, to the typed email', async () => {
    const fetchMock = stubFetch(() => json({ message: 'Invalid email or password.', errors: [] }, 401))
    signInAs('new.client@example.com')

    const alert = await screen.findByRole('alert')
    expect(alert.textContent).toBe(
      "The email address or password is incorrect. If you've just registered, confirm your email first.",
    )

    fireEvent.click(screen.getByRole('button', { name: 'Resend confirmation email' }))
    await waitFor(() => expect(screen.getAllByRole('status').some((node) => node.textContent === SENT)).toBe(true))

    const resendCalls = fetchMock.mock.calls.filter(([url]) => String(url).endsWith(RESEND_PATH))
    expect(resendCalls).toHaveLength(1)
    expect(JSON.parse(resendCalls[0][1].body)).toEqual({ email: 'new.client@example.com' })
  })

  it('hides the resend option again when the email is edited', async () => {
    stubFetch(() => json({ message: 'Invalid email or password.', errors: [] }, 401))
    signInAs('new.client@example.com')

    await screen.findByRole('button', { name: 'Resend confirmation email' })
    fireEvent.change(screen.getByLabelText('Email address'), { target: { value: 'other@example.com' } })
    expect(screen.queryByRole('button', { name: 'Resend confirmation email' })).toBeNull()
  })

  it('does not offer a resend for a locked account', async () => {
    stubFetch(() => json({ message: 'Too many failed sign-in attempts.', code: 'account_locked', errors: [] }, 429))
    signInAs('client@nexus.com')

    expect((await screen.findByRole('alert')).textContent).toMatch(/locked/)
    expect(screen.queryByRole('button', { name: 'Resend confirmation email' })).toBeNull()
  })
})
