import { describe, expect, it } from 'vitest'
import { signInErrorMessage } from './authMessages'

describe('signInErrorMessage', () => {
  it('gives one message for wrong details without saying which field was wrong', () => {
    expect(signInErrorMessage({ status: 401 })).toBe(
      "The email address or password is incorrect. If you've just registered, confirm your email first.",
    )
    expect(signInErrorMessage({ status: 400 })).toBe('The email address or password is incorrect.')
  })

  it('explains that a suspended or inactive account cannot sign in', () => {
    expect(signInErrorMessage({ status: 403 })).toMatch(/not active/)
  })

  it('reports sign-in as unavailable when the endpoint does not exist', () => {
    expect(signInErrorMessage({ status: 404 })).toMatch(/temporarily unavailable/)
  })

  it('asks the person to wait after too many attempts', () => {
    expect(signInErrorMessage({ status: 429 })).toMatch(/Too many sign-in attempts/)
  })

  it('falls back to the error message, then to a generic message', () => {
    expect(signInErrorMessage({ status: 0, message: 'The server could not be reached.' })).toBe(
      'The server could not be reached.',
    )
    expect(signInErrorMessage(null)).toBe('Sign-in failed. Please try again.')
  })
})

describe('signInErrorMessage for a locked account', () => {
  const locked = (retryAfter) => ({ status: 429, code: 'account_locked', retryAfter })

  it('says how long to wait, in minutes rounded up, and offers a password reset', () => {
    expect(signInErrorMessage(locked(900))).toBe(
      'This account is locked after too many failed sign-in attempts. Try again in about 15 minutes, or reset your password.',
    )
    expect(signInErrorMessage(locked(61))).toMatch(/about 2 minutes/)
  })

  it('uses the singular for the last minute', () => {
    expect(signInErrorMessage(locked(30))).toMatch(/about 1 minute,/)
  })

  it('still gives a sensible message when the wait time is missing', () => {
    expect(signInErrorMessage(locked(null))).toMatch(/Try again in a few minutes/)
  })

  it('does not confuse it with the per-address limit, which has no code', () => {
    expect(signInErrorMessage({ status: 429, code: null })).toBe('Too many sign-in attempts. Wait a minute and try again.')
  })
})
