import { describe, expect, it } from 'vitest'
import {
  newPasswordError,
  resetErrorOutcome,
  resetPasswordBody,
  resetTokenFrom,
  validateResetPassword,
} from './resetPassword'

describe('resetTokenFrom', () => {
  it('reads the token from the query string', () => {
    expect(resetTokenFrom('?token=abc.def.ghi')).toBe('abc.def.ghi')
  })

  it('returns an empty token when the link has none, so the page shows no form', () => {
    expect(resetTokenFrom('')).toBe('')
    expect(resetTokenFrom('?token=')).toBe('')
    expect(resetTokenFrom('?token=%20%20')).toBe('')
    expect(resetTokenFrom(undefined)).toBe('')
  })
})

describe('resetPasswordBody', () => {
  it('sends the token and the new password only', () => {
    expect(resetPasswordBody('t0ken', 'N3w-pass')).toEqual({ token: 't0ken', newPassword: 'N3w-pass' })
  })
})

describe('newPasswordError', () => {
  it('requires a new password', () => {
    expect(newPasswordError('')).toBe('Enter a new password.')
  })

  it('accepts 12 to 64 characters, like the API', () => {
    expect(newPasswordError('a'.repeat(11))).toBe('Use at least 12 characters.')
    expect(newPasswordError('a'.repeat(12))).toBeUndefined()
    expect(newPasswordError('a'.repeat(64))).toBeUndefined()
    expect(newPasswordError('a'.repeat(65))).toBe('Use 64 characters or fewer.')
  })

  it('accepts a short phrase with spaces and no composition rules', () => {
    expect(newPasswordError('correct horse')).toBeUndefined()
  })

  it('counts an emoji or accented letter as one character', () => {
    // '😀' is two UTF-16 units, so .length would count these one too high.
    expect(newPasswordError(`${'a'.repeat(11)}😀`)).toBeUndefined()
    expect(newPasswordError(`${'a'.repeat(10)}😀`)).toBe('Use at least 12 characters.')
    expect(newPasswordError(`${'a'.repeat(63)}😀`)).toBeUndefined()
    expect(newPasswordError('é'.repeat(12))).toBeUndefined()
  })
})

describe('validateResetPassword', () => {
  const valid = 'a long pass phrase'

  it('asks for both fields', () => {
    expect(validateResetPassword({ password: '', confirm: '' })).toEqual({
      password: 'Enter a new password.',
      confirm: 'Enter the new password again.',
    })
  })

  it('reports passwords that do not match', () => {
    expect(validateResetPassword({ password: valid, confirm: `${valid}!` })).toEqual({
      confirm: 'The passwords do not match.',
    })
  })

  it('reports a short password once, without a mismatch on top', () => {
    expect(validateResetPassword({ password: 'short', confirm: 'other' })).toEqual({
      password: 'Use at least 12 characters.',
    })
  })

  it('is empty when both fields match', () => {
    expect(validateResetPassword({ password: valid, confirm: valid })).toEqual({})
  })
})

describe('resetErrorOutcome', () => {
  it('treats a 400 without field errors as an invalid or expired link', () => {
    expect(resetErrorOutcome({ status: 400, message: 'Invalid or expired reset token.', errors: [] })).toEqual({
      kind: 'token',
      message: 'Invalid or expired reset token.',
    })
    expect(resetErrorOutcome({ status: 400 })).toEqual({
      kind: 'token',
      message: 'This reset link is invalid or has expired.',
    })
  })

  it('shows validation errors under the password field', () => {
    const error = { status: 400, message: 'Validation failed.', errors: ['Too short.', 'Add a number.'] }
    expect(resetErrorOutcome(error)).toEqual({ kind: 'field', message: 'Too short. Add a number.' })
  })

  it('asks the person to wait after too many attempts', () => {
    expect(resetErrorOutcome({ status: 429, message: 'ignored' })).toEqual({
      kind: 'form',
      message: 'Too many attempts, please wait a minute and try again.',
    })
  })

  it('passes on the shared message when the server cannot be reached', () => {
    const unreachable = 'We cannot reach the service right now. Please try again shortly.'
    expect(resetErrorOutcome({ status: 0, message: unreachable })).toEqual({ kind: 'form', message: unreachable })
    expect(resetErrorOutcome({ status: 502, message: unreachable })).toEqual({ kind: 'form', message: unreachable })
    expect(resetErrorOutcome(null).kind).toBe('form')
  })
})
