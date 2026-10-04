import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

function createStorage() {
  const values = new Map()
  return {
    getItem: (key) => (values.has(key) ? values.get(key) : null),
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
  }
}

// restoreSession runs once per page load, so each test loads fresh modules (a new "page").
async function loadPage() {
  vi.resetModules()
  return import('./auth')
}

describe('restoring a session on page load', () => {
  let fetchMock

  beforeEach(() => {
    vi.stubGlobal('sessionStorage', createStorage())
    vi.stubGlobal('localStorage', createStorage())
    fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('makes no request for a visitor who has never signed in', async () => {
    const { restoreSession, hasRestoreFinished } = await loadPage()

    await expect(restoreSession()).resolves.toBe(false)
    expect(fetchMock).not.toHaveBeenCalled()
    expect(hasRestoreFinished()).toBe(true)
  })

  it('tries once with a refresh token kept in this tab', async () => {
    sessionStorage.setItem('smp.refreshToken', 'refresh-1')
    fetchMock.mockImplementation(async () => new Response(null, { status: 401 }))
    const { restoreSession } = await loadPage()

    await expect(restoreSession()).resolves.toBe(false)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(sessionStorage.getItem('smp.refreshToken')).toBeNull()
  })

  it('forgets a stale cookie sign-in, so the next page load makes no request', async () => {
    localStorage.setItem('smp.cookieSession', '1')
    fetchMock.mockImplementation(async () => new Response(null, { status: 401 }))

    await expect((await loadPage()).restoreSession()).resolves.toBe(false)
    await expect((await loadPage()).restoreSession()).resolves.toBe(false)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
