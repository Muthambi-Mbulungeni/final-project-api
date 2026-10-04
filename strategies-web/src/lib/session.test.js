import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  endSession,
  getAccessToken,
  getSession,
  getStoredRefreshToken,
  mayRestoreSession,
  roleKeyFromApi,
  startDevSession,
  startSession,
  subscribe,
} from './session'

function createStorage() {
  const values = new Map()
  return {
    getItem: (key) => (values.has(key) ? values.get(key) : null),
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
    contents: () => [...values.values()].join(' '),
  }
}

const AUTH = {
  accessToken: 'access-123',
  tokenExpiry: '2026-09-30T12:00:00Z',
  role: 'Consultant',
  displayName: 'Pieter van Wyk',
  userId: 'c1',
}

describe('roleKeyFromApi', () => {
  it('maps the API role names to workspaces and rejects anything else', () => {
    expect(roleKeyFromApi('Admin')).toBe('admin')
    expect(roleKeyFromApi('consultant')).toBe('consultant')
    expect(roleKeyFromApi(' CLIENT ')).toBe('client')
    expect(roleKeyFromApi('Superuser')).toBeNull()
    expect(roleKeyFromApi(undefined)).toBeNull()
  })
})

describe('sessions', () => {
  let local
  let session

  beforeEach(() => {
    local = createStorage()
    session = createStorage()
    vi.stubGlobal('localStorage', local)
    vi.stubGlobal('sessionStorage', session)
  })

  afterEach(() => {
    endSession()
    vi.unstubAllEnvs()
    vi.unstubAllGlobals()
  })

  it('keeps the user and access token in memory, never in storage', () => {
    expect(startSession(AUTH)).toEqual({ id: 'c1', name: 'Pieter van Wyk', role: 'consultant', source: 'api' })
    expect(getAccessToken()).toBe('access-123')
    expect(local.contents()).not.toContain('access-123')
    expect(session.contents()).not.toContain('access-123')
    expect(getStoredRefreshToken()).toBeNull()
  })

  it('stores the refresh token in sessionStorage only when the API sends it in the body', () => {
    startSession({ ...AUTH, refreshToken: 'refresh-1' })
    expect(getStoredRefreshToken()).toBe('refresh-1')

    endSession()
    expect(getStoredRefreshToken()).toBeNull()
  })

  it('knows whether a session may be restored, without storing anything secret', () => {
    expect(mayRestoreSession()).toBe(false)

    startSession(AUTH)
    expect(mayRestoreSession()).toBe(true)
    expect(local.contents()).toBe('1')

    endSession()
    expect(mayRestoreSession()).toBe(false)

    startSession({ ...AUTH, refreshToken: 'refresh-1' })
    expect(mayRestoreSession()).toBe(true)
    expect(local.contents()).toBe('')
  })

  it('rejects a response without a token or with an unknown role', () => {
    expect(startSession({ ...AUTH, accessToken: '' })).toBeNull()
    expect(startSession({ ...AUTH, role: 'Superuser' })).toBeNull()
    expect(startSession(null)).toBeNull()
    expect(getSession()).toBeNull()
  })

  it('prefers a real session over a dev session and clears both on sign-out', () => {
    startDevSession('admin')
    expect(getSession()).toMatchObject({ role: 'admin', source: 'dev' })

    startSession(AUTH)
    expect(getSession()).toMatchObject({ role: 'consultant', source: 'api' })

    endSession()
    expect(getSession()).toBeNull()
  })

  it('notifies subscribers when someone signs in or out', () => {
    const listener = vi.fn()
    const unsubscribe = subscribe(listener)

    startSession(AUTH)
    endSession()
    unsubscribe()
    startSession(AUTH)

    expect(listener).toHaveBeenCalledTimes(2)
  })

  it('ignores dev sessions in production builds', () => {
    startDevSession('admin')
    vi.stubEnv('DEV', false)
    expect(getSession()).toBeNull()
  })
})
