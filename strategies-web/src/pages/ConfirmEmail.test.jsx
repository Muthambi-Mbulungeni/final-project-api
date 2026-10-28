// @vitest-environment jsdom
import { StrictMode } from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { deferred, json } from '../test/dom'
import ConfirmEmail from './ConfirmEmail'

const SENT = "If that email has an account, we've sent a new link."

function stubFetch(handler) {
  const fetchMock = vi.fn(async (url, options = {}) => handler(String(url), options))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function callsTo(fetchMock, path) {
  return fetchMock.mock.calls.filter(([url]) => String(url).endsWith(path))
}

function renderAt(entry, { strict = false } = {}) {
  const tree = (
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route path="/confirm-email" element={<ConfirmEmail />} />
        <Route path="/login" element={<p>Sign-in page</p>} />
      </Routes>
    </MemoryRouter>
  )
  return render(strict ? <StrictMode>{tree}</StrictMode> : tree)
}

describe('ConfirmEmail', () => {
  it('confirms a valid token once, even under StrictMode, and focuses the result', async () => {
    const pending = deferred()
    const fetchMock = stubFetch(async () => {
      await pending.promise
      return json({ message: 'Email confirmed. You can now sign in.' })
    })
    renderAt('/confirm-email?token=good.token', { strict: true })

    expect(screen.getByRole('status').textContent).toContain('Confirming your email…')
    pending.resolve()

    const heading = await screen.findByRole('heading', { name: 'Email confirmed' })
    expect(document.activeElement).toBe(heading)
    expect(callsTo(fetchMock, '/api/auth/confirm-email')).toHaveLength(1)
    const [, options] = callsTo(fetchMock, '/api/auth/confirm-email')[0]
    expect(options.method).toBe('POST')
    expect(JSON.parse(options.body)).toEqual({ token: 'good.token' })

    fireEvent.click(screen.getByRole('link', { name: 'Sign in' }))
    expect(await screen.findByText('Sign-in page')).toBeTruthy()
  })

  it('shows the invalid state with a resend form when the API answers 400', async () => {
    const fetchMock = stubFetch(async (url) => {
      if (url.endsWith('/api/auth/confirm-email')) {
        return json({ message: 'Invalid or expired confirmation token.', errors: [] }, 400)
      }
      return json({ message: 'If an account with that email needs confirming, a new confirmation link has been sent.' })
    })
    renderAt('/confirm-email?token=used.token')

    const heading = await screen.findByRole('heading', { name: 'This link is invalid or has expired' })
    expect(document.activeElement).toBe(heading)

    fireEvent.change(screen.getByLabelText('Email address'), { target: { value: ' new.client@example.com ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Send a new link' }))

    await waitFor(() => expect(screen.getAllByRole('status').some((node) => node.textContent === SENT)).toBe(true))
    const resendCalls = callsTo(fetchMock, '/api/auth/resend-confirmation')
    expect(resendCalls).toHaveLength(1)
    expect(JSON.parse(resendCalls[0][1].body)).toEqual({ email: 'new.client@example.com' })
  })

  it('does not call the API when the link has no token', async () => {
    const fetchMock = stubFetch(async () => json({}))
    renderAt('/confirm-email')

    expect(await screen.findByRole('heading', { name: 'This link is invalid or has expired' })).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('checks the email before resending', async () => {
    const fetchMock = stubFetch(async () => json({}))
    renderAt('/confirm-email')

    fireEvent.change(screen.getByLabelText('Email address'), { target: { value: 'not-an-email' } })
    fireEvent.click(screen.getByRole('button', { name: 'Send a new link' }))

    expect(await screen.findByText('Enter a valid email address, like name@example.com.')).toBeTruthy()
    expect(document.activeElement).toBe(screen.getByLabelText('Email address'))
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('offers to try again when the failure is not the link', async () => {
    let attempt = 0
    const fetchMock = stubFetch(async () => {
      attempt += 1
      return attempt === 1 ? json({ message: 'Too many requests.' }, 429) : json({ message: 'Email confirmed.' })
    })
    renderAt('/confirm-email?token=good.token')

    expect(await screen.findByRole('heading', { name: "We couldn't confirm your email" })).toBeTruthy()
    expect(screen.getByRole('alert').textContent).toContain('Too many attempts, please wait a minute and try again.')

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('heading', { name: 'Email confirmed' })).toBeTruthy()
    expect(callsTo(fetchMock, '/api/auth/confirm-email')).toHaveLength(2)
  })
})
