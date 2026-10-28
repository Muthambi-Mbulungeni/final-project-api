import { describe, expect, it } from 'vitest'
import { confirmErrorOutcome, confirmationTokenFrom, resendErrorMessage } from './confirmEmail'

describe('confirmationTokenFrom', () => {
  it('reads and trims the token, or gives an empty string', () => {
    expect(confirmationTokenFrom('?token=%20abc.def%20')).toBe('abc.def')
    expect(confirmationTokenFrom('?token=')).toBe('')
    expect(confirmationTokenFrom('')).toBe('')
    expect(confirmationTokenFrom(undefined)).toBe('')
  })
})

describe('confirmErrorOutcome', () => {
  it('treats any 400 as a link that cannot be used', () => {
    expect(confirmErrorOutcome({ status: 400, message: 'Invalid or expired confirmation token.' }).kind).toBe('invalid')
  })

  it('offers a retry for rate limits and other failures', () => {
    expect(confirmErrorOutcome({ status: 429 })).toEqual({
      kind: 'error',
      message: 'Too many attempts, please wait a minute and try again.',
    })
    expect(confirmErrorOutcome({ status: 0, message: 'The service cannot be reached.' })).toEqual({
      kind: 'error',
      message: 'The service cannot be reached.',
    })
    expect(confirmErrorOutcome({ status: 500 }).kind).toBe('error')
  })
})

describe('resendErrorMessage', () => {
  it('explains a rate limit and passes other messages through', () => {
    expect(resendErrorMessage({ status: 429 })).toMatch(/wait a minute/)
    expect(resendErrorMessage({ status: 0, message: 'The service cannot be reached.' })).toBe(
      'The service cannot be reached.',
    )
    expect(resendErrorMessage({})).toBe('The link could not be sent. Please try again.')
  })
})
