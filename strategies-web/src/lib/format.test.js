import { describe, expect, it } from 'vitest'
import { formatDate, formatDateTime, getInitials, shortId } from './format'

describe('formatDate', () => {
  it('returns a dash for empty or invalid values', () => {
    expect(formatDate(null)).toBe('—')
    expect(formatDate('')).toBe('—')
    expect(formatDate('not a date')).toBe('—')
  })

  it('handles .NET timestamps with 7 fractional-second digits', () => {
    const fromDotNet = formatDate('2026-11-28T08:25:13.5513727Z')
    expect(fromDotNet).not.toBe('—')
    expect(fromDotNet).toBe(formatDate('2026-11-28T08:25:13.551Z'))
  })
})

describe('formatDateTime', () => {
  it('returns a dash for empty or invalid values', () => {
    expect(formatDateTime(undefined)).toBe('—')
    expect(formatDateTime('not a date')).toBe('—')
  })

  it('includes the time and handles .NET timestamps', () => {
    const fromDotNet = formatDateTime('2026-09-28T09:15:00.1234567Z')
    expect(fromDotNet).not.toBe('—')
    expect(fromDotNet).toBe(formatDateTime('2026-09-28T09:15:00.123Z'))
    expect(fromDotNet).not.toBe(formatDate('2026-09-28T09:15:00.123Z'))
  })
})

describe('getInitials', () => {
  it('returns up to two uppercase initials', () => {
    expect(getInitials('Thandi Mokoena')).toBe('TM')
    expect(getInitials('pieter van wyk')).toBe('PV')
    expect(getInitials('Riaad')).toBe('R')
  })

  it('returns an empty string when there is no name', () => {
    expect(getInitials('')).toBe('')
    expect(getInitials(null)).toBe('')
    expect(getInitials(undefined)).toBe('')
  })
})

describe('shortId', () => {
  it('returns the first 8 characters of an ID', () => {
    expect(shortId('33333333-3333-3333-3333-333333333333')).toBe('33333333')
    expect(shortId('6adcbe68-1f2e-4c3d-8b9a-0123456789ab')).toBe('6ADCBE68')
  })

  it('returns a dash when there is no ID', () => {
    expect(shortId(null)).toBe('—')
    expect(shortId('')).toBe('—')
  })
})
