import { describe, expect, it } from 'vitest'
import { emailError, validateSignIn } from './validation'

describe('emailError', () => {
  it('asks for an email when it is empty or only spaces', () => {
    expect(emailError('')).toBe('Enter your email address.')
    expect(emailError('   ')).toBe('Enter your email address.')
  })

  it('rejects addresses without a valid shape', () => {
    expect(emailError('amara')).toMatch(/valid email/)
    expect(emailError('amara@example')).toMatch(/valid email/)
  })

  it('accepts a valid address, ignoring surrounding spaces', () => {
    expect(emailError(' amara@example.com ')).toBeUndefined()
  })
})

describe('validateSignIn', () => {
  it('reports both fields when the form is empty', () => {
    expect(validateSignIn({ email: '', password: '' })).toEqual({
      email: 'Enter your email address.',
      password: 'Enter your password.',
    })
  })

  it('returns no errors for a complete form', () => {
    expect(validateSignIn({ email: 'amara@example.com', password: 'secret' })).toEqual({})
  })
})
