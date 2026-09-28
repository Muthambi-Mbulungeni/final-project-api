import { describe, expect, it } from 'vitest'
import { signInErrorMessage } from './authMessages'

describe('signInErrorMessage', () => {
  it('gives one message for wrong details without saying which field was wrong', () => {
    expect(signInErrorMessage({ status: 401 })).toBe('The email address or password is incorrect.')
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
