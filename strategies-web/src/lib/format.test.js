import { describe, expect, it } from 'vitest'
import { formatDate } from './format'

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
