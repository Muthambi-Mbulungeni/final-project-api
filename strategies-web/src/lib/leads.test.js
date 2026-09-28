import { describe, expect, it } from 'vitest'
import { isAwaitingReview, percentOf, summariseLeads } from './leads'

describe('summariseLeads', () => {
  it('counts leads by triage outcome', () => {
    const leads = [
      { triageStatus: 'Pending' },
      { triageStatus: 'NeedsReview' },
      { triageStatus: 'Qualified' },
      { triageStatus: 'Unqualified' },
      { triageStatus: 'Qualified' },
    ]
    expect(summariseLeads(leads)).toEqual({ total: 5, awaiting: 2, qualified: 2, unqualified: 1 })
  })

  it('returns zeros for an empty list', () => {
    expect(summariseLeads([])).toEqual({ total: 0, awaiting: 0, qualified: 0, unqualified: 0 })
  })

  it('counts a lead with an unknown status in the total only', () => {
    expect(summariseLeads([{ triageStatus: 'Something' }])).toEqual({
      total: 1,
      awaiting: 0,
      qualified: 0,
      unqualified: 0,
    })
  })
})

describe('isAwaitingReview', () => {
  it('is true for pending and needs-review leads only', () => {
    expect(isAwaitingReview({ triageStatus: 'Pending' })).toBe(true)
    expect(isAwaitingReview({ triageStatus: 'NeedsReview' })).toBe(true)
    expect(isAwaitingReview({ triageStatus: 'Qualified' })).toBe(false)
    expect(isAwaitingReview(null)).toBe(false)
  })
})

describe('percentOf', () => {
  it('rounds to a whole percentage and handles a zero total', () => {
    expect(percentOf(1, 3)).toBe(33)
    expect(percentOf(2, 2)).toBe(100)
    expect(percentOf(0, 0)).toBe(0)
  })
})
