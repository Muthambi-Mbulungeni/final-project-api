import { describe, expect, it } from 'vitest'
import {
  agentReason,
  applyDecision,
  countFromLeadsResponse,
  decisionChoices,
  decisionErrorMessage,
  decisionLabel,
  enquiryMailto,
  isAwaitingReview,
  leadStatusBody,
  leadStatusPath,
  leadsPath,
  percentOf,
  startTimingLabel,
  summariseLeads,
  summaryFromCounts,
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

describe('leadsPath', () => {
  it('asks the API for one page, with optional search and status', () => {
    expect(leadsPath()).toBe('/api/leads?page=1&pageSize=10')
    expect(leadsPath({ page: 2, pageSize: 10, search: '  jo ', status: 'NeedsReview' })).toBe(
      '/api/leads?page=2&pageSize=10&search=jo&status=NeedsReview',
    )
  })
})

describe('countFromLeadsResponse', () => {
  it('reads totalRecords from the API and counts a plain array from the mock', () => {
    expect(countFromLeadsResponse({ data: [], totalRecords: 37 }, 'Qualified')).toBe(37)
    expect(
      countFromLeadsResponse(
        [{ triageStatus: 'Qualified' }, { triageStatus: 1 }, { triageStatus: 'NeedsReview' }],
        'Qualified',
      ),
    ).toBe(2)
    expect(countFromLeadsResponse(null, 'Qualified')).toBeNull()
  })
})

describe('summaryFromCounts', () => {
  it('builds the intake funnel once every status has been counted', () => {
    expect(summaryFromCounts({ Pending: 2, NeedsReview: 3, Qualified: 10, Unqualified: 25 })).toEqual({
      total: 40,
      awaiting: 5,
      qualified: 10,
      unqualified: 25,
    })
    expect(summaryFromCounts({ Pending: 2, NeedsReview: null, Qualified: 10, Unqualified: 25 })).toBeNull()
  })
})

describe('verdictExplanation', () => {
  it('explains each triage outcome, from text or numeric enums', () => {
    expect(verdictExplanation({ triageStatus: 'NeedsReview' }).title).toBe('Waiting for your review')
    expect(verdictExplanation({ triageStatus: 'Pending' }).title).toBe('Not yet screened')
    expect(verdictExplanation({ triageStatus: 1 }).title).toBe('Qualified by the intake agent')
    expect(verdictExplanation({ triageStatus: 'Unqualified' }).title).toBe('Unqualified by the intake agent')
  })

  it('falls back when no verdict is recorded', () => {
    expect(verdictExplanation({}).title).toBe('No verdict recorded')
    expect(verdictExplanation(null).title).toBe('No verdict recorded')
  })

  it('credits a consultant, not the agent, once the lead is reviewed', () => {
    expect(verdictExplanation({ triageStatus: 'Qualified', reviewedById: 'u1' }).title).toBe('Qualified by a consultant')
    expect(verdictExplanation({ triageStatus: 2, reviewedById: 'u1' }).title).toBe('Unqualified by a consultant')
    expect(verdictExplanation({ triageStatus: 'NeedsReview', reviewedById: 'u1' }).title).toBe('Waiting for your review')
  })
})

describe('lead decisions', () => {
  it('sends the status name to the lead status endpoint', () => {
    expect(leadStatusPath('a b')).toBe('/api/leads/a%20b/status')
    expect(leadStatusBody('Unqualified')).toEqual({ status: 'Unqualified' })
  })

  it('offers both decisions before review, so the agent verdict can be confirmed', () => {
    expect(decisionChoices({ triageStatus: 'Qualified' })).toEqual(['Qualified', 'Unqualified'])
    expect(decisionChoices({ triageStatus: 'NeedsReview' })).toEqual(['Qualified', 'Unqualified'])
    expect(decisionLabel({ triageStatus: 'NeedsReview' }, 'Qualified')).toBe('Mark qualified')
  })

  it('offers only the other decision after review, matching numeric enums too', () => {
    expect(decisionChoices({ triageStatus: 1, reviewedById: 'u1' })).toEqual(['Unqualified'])
    expect(decisionChoices({ triageStatus: 'Unqualified', reviewedById: 'u1' })).toEqual(['Qualified'])
    expect(decisionLabel({ reviewedById: 'u1' }, 'Unqualified')).toBe('Change to unqualified')
    expect(decisionChoices(null)).toEqual([])
  })

  it('applies an accepted decision and records the reviewer', () => {
    const lead = { id: 'l1', fullName: 'Naledi', triageStatus: 'NeedsReview', reviewedById: null }
    expect(applyDecision(lead, 'Qualified', 'u9')).toEqual({ ...lead, triageStatus: 'Qualified', reviewedById: 'u9' })
    expect(applyDecision({ ...lead, reviewedById: 'u1' }, 'Qualified', undefined).reviewedById).toBe('u1')
  })

  it('explains failed decisions in plain language', () => {
    expect(decisionErrorMessage({ status: 403 })).toMatch(/isn’t allowed/)
    expect(decisionErrorMessage({ status: 404 })).toMatch(/no longer exists/)
    expect(decisionErrorMessage({ status: 400, message: 'One or more validation errors occurred.' })).toMatch(
      /wasn’t accepted/,
    )
    expect(decisionErrorMessage({ status: 500, message: 'Server error' })).toBe('Server error')
    expect(decisionErrorMessage(null)).toMatch(/could not be saved/)
  })
})

describe('agentReason', () => {
  it('returns the trimmed reason the agent gave', () => {
    expect(agentReason({ triageReason: '  Valid work permit and wants help.\n' })).toBe(
      'Valid work permit and wants help.',
    )
  })

  it('returns null when the agent gave no usable reason', () => {
    expect(agentReason({ triageReason: null })).toBeNull()
    expect(agentReason({ triageReason: '   ' })).toBeNull()
    expect(agentReason({ triageReason: 42 })).toBeNull()
    expect(agentReason({})).toBeNull()
    expect(agentReason(null)).toBeNull()
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
