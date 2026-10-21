import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { destinationFor, restoreSession, signIn, signInNotice, signOut } from './auth'
import { endSession, getSession, startSession } from './session'

function createStorage() {
  const values = new Map()
  return {
    getItem: (key) => (values.has(key) ? values.get(key) : null),
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
  }
}

function json(body, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

const AUTH = {
  accessToken: 'access-123',
  tokenExpiry: '2026-09-30T12:00:00Z',
  role: 'Consultant',
  displayName: 'Pieter van Wyk',
  userId: 'c1',
}

describe('signing in, out and restoring', () => {
  let fetchMock

  beforeEach(() => {
    vi.stubGlobal('sessionStorage', createStorage())
    vi.stubGlobal('localStorage', createStorage())
    fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    endSession()
    vi.unstubAllGlobals()
  })

  it('starts a session from the login response', async () => {
    fetchMock.mockImplementation(async () => json(AUTH))

    await expect(signIn('pieter@example.com', 'secret12')).resolves.toMatchObject({
      role: 'consultant',
      source: 'api',
    })

    const [path, options] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/auth/login')
    expect(JSON.parse(options.body)).toEqual({ email: 'pieter@example.com', password: 'secret12' })
  })

  it('refuses a login response it cannot use', async () => {
    fetchMock.mockImplementation(async () => json({ ...AUTH, role: 'Superuser' }))

    await expect(signIn('a@b.co', 'secret12')).rejects.toMatchObject({ status: 502 })
    expect(getSession()).toBeNull()
  })

  it('revokes on sign-out, and signs out locally even if the server cannot be reached', async () => {
    startSession(AUTH)
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    await signOut()

    const [path, options] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/auth/revoke')
    expect(options.headers.Authorization).toBe('Bearer access-123')
    expect(options.body).toBeUndefined()
    expect(getSession()).toBeNull()
  })

  it('sends the refresh token on sign-out in fallback mode', async () => {
    startSession({ ...AUTH, refreshToken: 'refresh-1' })
    fetchMock.mockImplementation(async () => new Response(null, { status: 204 }))

    await signOut()

    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({ refreshToken: 'refresh-1' })
  })

  it('restores a session from the refresh cookie, trying only once per page load', async () => {
    fetchMock.mockImplementation(async () => json(AUTH))
    localStorage.setItem('smp.cookieSession', '1')

    await expect(restoreSession()).resolves.toBe(true)
    expect(getSession()).toMatchObject({ role: 'consultant', source: 'api' })

    endSession()
    await restoreSession()
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0][0]).toBe('/api/auth/refresh')
  })
})

describe('destinationFor', () => {
  it('returns to the page the user wanted, but only inside their own workspace on this site', () => {
    const consultant = { role: 'consultant' }
    expect(destinationFor(consultant, '/consultant/leads')).toBe('/consultant/leads')
    expect(destinationFor(consultant, '/consultant/cases?page=2')).toBe('/consultant/cases?page=2')
    expect(destinationFor(consultant, '/admin/users')).toBe('/consultant')
    expect(destinationFor(consultant, '/consultants-elsewhere')).toBe('/consultant')
    expect(destinationFor(consultant, '//evil.example')).toBe('/consultant')
    expect(destinationFor(consultant, 'https://evil.example')).toBe('/consultant')
    expect(destinationFor(consultant, undefined)).toBe('/consultant')
  })
})

describe('signInNotice', () => {
  it('explains why the person is back at sign-in', () => {
    expect(signInNotice('expired')).toMatch(/expired/)
    expect(signInNotice('idle')).toMatch(/30 minutes/)
    expect(signInNotice('signed-out')).toBe('You have signed out.')
    expect(signInNotice('password-reset')).toBe('Your password has been reset. Sign in with your new password.')
    expect(signInNotice('something else')).toBeNull()
  })
})
