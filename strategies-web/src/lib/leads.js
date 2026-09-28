import { resolveEnum } from '../config/enums'

// Leads the intake agent could not qualify on its own wait for a consultant.
const AWAITING_REVIEW = ['Pending', 'NeedsReview']

export function triageValue(lead) {
  return resolveEnum('triageStatus', lead?.triageStatus)?.value ?? null
}

export function isAwaitingReview(lead) {
  return AWAITING_REVIEW.includes(triageValue(lead))
}

// Counts leads by triage outcome. Accepts the enum as text or as its number.
export function summariseLeads(leads = []) {
  const summary = { total: leads.length, awaiting: 0, qualified: 0, unqualified: 0 }

  for (const lead of leads) {
    const value = triageValue(lead)
    if (AWAITING_REVIEW.includes(value)) summary.awaiting += 1
    else if (value === 'Qualified') summary.qualified += 1
    else if (value === 'Unqualified') summary.unqualified += 1
  }

  return summary
}

export function percentOf(part, total) {
  if (!total) return 0
  return Math.round((part / total) * 100)
}
