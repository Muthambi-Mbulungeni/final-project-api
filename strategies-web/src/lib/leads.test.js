import { describe, expect, it } from 'vitest'
import {
  enquiryMailto,
  isAwaitingReview,
  percentOf,
  startTimingLabel,
  summariseLeads,
  verdictExplanation,
} from './leads'

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

describe('verdictExplanation', () => {
  it('explains each triage outcome, from text or numeric enums', () => {
    expect(verdictExplanation({ triageStatus: 'Pending' }).title).toBe('Waiting for your review')
    expect(verdictExplanation({ triageStatus: 1 }).title).toBe('Qualified by the intake agent')
    expect(verdictExplanation({ triageStatus: 'Unqualified' }).title).toBe('Filtered out by the intake agent')
    expect(verdictExplanation({ triageStatus: 'NeedsReview' }).title).toBe('Flagged for review')
  })

  it('falls back when no verdict is recorded', () => {
    expect(verdictExplanation({}).title).toBe('No verdict recorded')
    expect(verdictExplanation(null).title).toBe('No verdict recorded')
  })
})

describe('startTimingLabel', () => {
  const now = new Date(2026, 8, 28, 10, 0)

  it('describes how far away the start date is', () => {
    expect(startTimingLabel(new Date(2026, 8, 28, 18, 0), now)).toBe('Today')
    expect(startTimingLabel(new Date(2026, 8, 29), now)).toBe('Tomorrow')
    expect(startTimingLabel(new Date(2026, 9, 5), now)).toBe('In 7 days')
    expect(startTimingLabel(new Date(2026, 9, 28), now)).toBe('In 4 weeks')
    expect(startTimingLabel(new Date(2026, 10, 28), now)).toBe('In about 2 months')
    expect(startTimingLabel(new Date(2026, 8, 20), now)).toBe('Date has passed')
  })

  it('returns null for a missing or invalid date', () => {
    expect(startTimingLabel(null, now)).toBeNull()
    expect(startTimingLabel('not a date', now)).toBeNull()
  })
})

describe('enquiryMailto', () => {
  it('addresses the lead and quotes their enquiry reference', () => {
    const link = enquiryMailto({
      id: 'f3f68726-5bcf-440d-979b-19d4d07b51dc',
      fullName: 'John Doe',
      email: 'john.doe@example.com',
    })
    expect(link.startsWith('mailto:john.doe@example.com?subject=')).toBe(true)
    expect(decodeURIComponent(link)).toContain('(ref F3F68726)')
    expect(decodeURIComponent(link)).toContain('Dear John,')
  })

  it('returns null when the lead has no email address', () => {
    expect(enquiryMailto({ fullName: 'No Email' })).toBeNull()
    expect(enquiryMailto(null)).toBeNull()
  })
})
