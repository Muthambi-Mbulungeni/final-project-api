import { describe, expect, it } from 'vitest'
import { latestCase } from './cases'

describe('latestCase', () => {
  it('returns the most recently opened case', () => {
    const cases = [
      { id: 'old', createdAt: '2026-08-01T09:00:00Z' },
      { id: 'new', createdAt: '2026-09-15T09:00:00Z' },
    ]
    expect(latestCase(cases).id).toBe('new')
  })

  it('returns null when the client has no case', () => {
    expect(latestCase([])).toBeNull()
  })
})
