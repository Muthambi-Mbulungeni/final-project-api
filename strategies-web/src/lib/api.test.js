import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { apiGet, apiPost, filenameFromDisposition, onSessionExpired, refreshSession } from './api'
import { endSession, getAccessToken, getSession, startSession } from './session'

function createStorage() {
  const values = new Map()
  return {
    getItem: (key) => (values.has(key) ? values.get(key) : null),
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
  }
}

function json(body, status = 200) {
  return new Response(body === null ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function authHeader(options) {
  return options?.headers?.Authorization
}

const AUTH = {
  accessToken: 'old-token',
  tokenExpiry: '2026-09-30T12:00:00Z',
  role: 'Consultant',
  displayName: 'Pieter van Wyk',
  userId: 'c1',
}

const UNAUTHORIZED = { message: 'Unauthorized access.' }

describe('filenameFromDisposition', () => {
  it('reads the filename the API currently sends for the cases export', () => {
    const header = "attachment; filename=cases.csv; filename*=UTF-8''cases.csv"
    expect(filenameFromDisposition(header, 'fallback.csv')).toBe('cases.csv')
  })

  it('decodes UTF-8 filenames and reads quoted plain filenames', () => {
    expect(filenameFromDisposition("attachment; filename*=UTF-8''case%20report.csv", 'x')).toBe('case report.csv')
    expect(filenameFromDisposition('attachment; filename="leads 2026.csv"', 'x')).toBe('leads 2026.csv')
  })

  it('uses the fallback when there is no usable header', () => {
    expect(filenameFromDisposition(null, 'fallback.csv')).toBe('fallback.csv')
    expect(filenameFromDisposition('attachment', 'fallback.csv')).toBe('fallback.csv')
  })
})

describe('authorised requests', () => {
  let fetchMock

  beforeEach(() => {
    vi.stubGlobal('sessionStorage', createStorage())
    vi.stubGlobal('localStorage', createStorage())
    fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    endSession()
    onSessionExpired(null)
    vi.unstubAllGlobals()
  })

  it('sends the access token as a Bearer header', async () => {
    startSession(AUTH)
    fetchMock.mockImplementation(async () => json([]))

    await apiGet('/api/leads')

    expect(authHeader(fetchMock.mock.calls[0][1])).toBe('Bearer old-token')
  })

  it('refreshes once and retries when the access token has expired', async () => {
    startSession(AUTH)
    fetchMock.mockImplementation(async (path, options) => {
      if (path === '/api/auth/refresh') return json({ ...AUTH, accessToken: 'new-token' })
      return authHeader(options) === 'Bearer new-token' ? json({ ok: true }) : json(UNAUTHORIZED, 401)
    })

    await expect(apiGet('/api/leads')).resolves.toEqual({ ok: true })

    expect(fetchMock.mock.calls.map(([path]) => path)).toEqual(['/api/leads', '/api/auth/refresh', '/api/leads'])
    expect(getAccessToken()).toBe('new-token')
  })

  it('shares one refresh between requests that fail at the same time', async () => {
    startSession(AUTH)
    fetchMock.mockImplementation(async (path, options) => {
      if (path === '/api/auth/refresh') {
        await new Promise((resolve) => setTimeout(resolve, 5))
        return json({ ...AUTH, accessToken: 'new-token' })
      }
      return authHeader(options) === 'Bearer new-token' ? json({ ok: path }) : json(UNAUTHORIZED, 401)
    })

    const paths = ['/api/leads', '/api/cases', '/api/admin/users', '/api/notifications']
    const results = await Promise.all(paths.map((path) => apiGet(path)))

    expect(results).toEqual(paths.map((path) => ({ ok: path })))
    expect(fetchMock.mock.calls.filter(([path]) => path === '/api/auth/refresh')).toHaveLength(1)
  })

  it('ends the session once when the refresh fails', async () => {
    startSession(AUTH)
    const expired = vi.fn()
    onSessionExpired(expired)
    fetchMock.mockImplementation(async () => json(UNAUTHORIZED, 401))

    const results = await Promise.allSettled([apiGet('/api/leads'), apiGet('/api/cases')])

    expect(results.every((result) => result.status === 'rejected' && result.reason.status === 401)).toBe(true)
    expect(getSession()).toBeNull()
    expect(expired).toHaveBeenCalledTimes(1)
  })

  it('does not refresh for signed-out visitors or sign-in requests', async () => {
    fetchMock.mockImplementation(async () => json(UNAUTHORIZED, 401))

    await expect(apiGet('/api/leads')).rejects.toMatchObject({ status: 401 })
    startSession(AUTH)
    await expect(apiPost('/api/auth/login', { email: 'a@b.co', password: 'secret1' })).rejects.toMatchObject({
      status: 401,
    })

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('sends a refresh token only when the API gave one in the body (fallback mode)', async () => {
    fetchMock.mockImplementation(async () => json({ ...AUTH, accessToken: 'new-token' }))

    // Cookie mode: the refresh token never reaches JavaScript, so nothing is sent in the body.
    startSession(AUTH)
    await refreshSession()
    expect(fetchMock.mock.calls[0][1].body).toBeUndefined()

    // Fallback mode: the API returned the refresh token in the body at sign-in.
    fetchMock.mockClear()
    startSession({ ...AUTH, refreshToken: 'refresh-1' })
    await refreshSession()
    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({ refreshToken: 'refresh-1' })
  })
})

describe('error messages', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('says the server had a problem, not that it was unreachable, when it answers 500', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('<html>stack trace</html>', { status: 500 })))

    await expect(apiGet('/api/cases')).rejects.toMatchObject({
      status: 500,
      message: 'Something went wrong on our side. Please try again in a moment.',
    })
  })

  it('says the server could not be reached when there is no response at all', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        throw new TypeError('Failed to fetch')
      }),
    )

    await expect(apiGet('/api/cases')).rejects.toMatchObject({
      status: 0,
      message: expect.stringContaining('could not be reached'),
    })
  })
})

describe('gateway errors', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('says the server could not be reached when the gateway answers 502, 503 or 504', async () => {
    for (const status of [502, 503, 504]) {
      vi.stubGlobal('fetch', vi.fn(async () => new Response('{"message":"The API is not reachable."}', { status })))
      await expect(apiGet('/api/cases')).rejects.toMatchObject({
        status,
        message: expect.stringContaining('could not be reached'),
      })
    }
  })
})
