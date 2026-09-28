import { describe, expect, it } from 'vitest'
import {
  emailError,
  isFutureDate,
  validateEnquiryDetails,
  validateEnquiryPlans,
  validateSignIn,
} from './validation'

const TODAY = new Date(2026, 8, 28) // 28 September 2026, local time

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

describe('isFutureDate', () => {
  it('accepts a date after today', () => {
    expect(isFutureDate('2026-09-29', TODAY)).toBe(true)
  })

  it('rejects today and earlier dates', () => {
    expect(isFutureDate('2026-09-28', TODAY)).toBe(false)
    expect(isFutureDate('2026-01-01', TODAY)).toBe(false)
  })

  it('rejects values that are not YYYY-MM-DD dates', () => {
    expect(isFutureDate('', TODAY)).toBe(false)
    expect(isFutureDate('28/10/2026', TODAY)).toBe(false)
  })
})

describe('validateEnquiryDetails', () => {
  it('reports every missing field', () => {
    expect(validateEnquiryDetails({ fullName: ' ', email: '', immigrationStatus: '' })).toEqual({
      fullName: 'Enter your full name.',
      email: 'Enter your email address.',
      immigrationStatus: 'Choose the option that best describes you.',
    })
  })

  it('returns no errors for complete details', () => {
    expect(
      validateEnquiryDetails({ fullName: 'Amara Okafor', email: 'amara@example.com', immigrationStatus: 'Foreigner' }),
    ).toEqual({})
  })
})

describe('validateEnquiryPlans', () => {
  it('asks for a start date and consent when both are missing', () => {
    expect(validateEnquiryPlans({ desiredStartDate: '', consent: false }, TODAY)).toEqual({
      desiredStartDate: 'Choose the date you would like to start.',
      consent: 'You need to agree before we can assess your enquiry.',
    })
  })

  it('rejects a start date that is not in the future', () => {
    expect(validateEnquiryPlans({ desiredStartDate: '2026-09-28', consent: true }, TODAY)).toEqual({
      desiredStartDate: 'Choose a date after today.',
    })
  })

  it('returns no errors for a future date with consent', () => {
    expect(validateEnquiryPlans({ desiredStartDate: '2026-11-01', consent: true }, TODAY)).toEqual({})
  })
})
