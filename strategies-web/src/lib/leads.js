import { resolveEnum } from '../config/enums'
import { shortId, toDate } from './format'

// Leads the intake agent could not qualify on its own wait for a consultant.
const AWAITING_REVIEW = ['Pending', 'NeedsReview']
const DAY_IN_MS = 24 * 60 * 60 * 1000

export function triageValue(lead) {
  return resolveEnum('triageStatus', lead?.triageStatus)?.value ?? null
}

export function isAwaitingReview(lead) {
  return AWAITING_REVIEW.includes(triageValue(lead))
}

// Counts leads by triage outcome in a loaded list. Accepts the enum as text or as its number.
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

// One page of leads. The API pages, searches (name or email) and filters by status itself,
// always newest first.
export function leadsPath({ page = 1, pageSize = 10, search = '', status = '' } = {}) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  const term = search.trim()
  if (term) params.set('search', term)
  if (status) params.set('status', status)
  return `/api/leads?${params.toString()}`
}

// How many leads have a status. The API answers a one-row page with totalRecords.
// The mock API returns every lead as a plain array, so those are counted here instead.
export function countFromLeadsResponse(body, status) {
  if (Array.isArray(body)) return body.filter((lead) => triageValue(lead) === status).length
  if (typeof body?.totalRecords === 'number') return body.totalRecords
  return null
}

// The intake funnel from one count per status. The four statuses cover every lead,
// so their sum is the total. Returns null until every count is known.
export function summaryFromCounts({ Pending, NeedsReview, Qualified, Unqualified }) {
  const counts = [Pending, NeedsReview, Qualified, Unqualified]
  if (counts.some((count) => typeof count !== 'number')) return null
  return {
    total: Pending + NeedsReview + Qualified + Unqualified,
    awaiting: Pending + NeedsReview,
    qualified: Qualified,
    unqualified: Unqualified,
  }
}

// What each triage outcome means in the consultant's workflow.
// The API marks a lead Qualified when the agent succeeds and NeedsReview otherwise,
// including when the AI service is unavailable (the fallback path).
const VERDICTS = {
  NeedsReview: {
    title: 'Waiting for your review',
    body: 'The intake agent could not qualify this enquiry automatically, or was unavailable when it arrived, so it has been routed to a consultant.',
  },
  Pending: {
    title: 'Not yet screened',
    body: 'The intake agent has not recorded a verdict for this enquiry yet, so it is waiting for a consultant.',
  },
  Qualified: {
    title: 'Qualified by the intake agent',
    body: 'The agent assessed this enquiry as complete and eligible to proceed. It does not need to wait in the review queue.',
  },
  Unqualified: {
    title: 'Filtered out by the intake agent',
    body: 'The agent assessed this enquiry as not viable, so it was kept out of the review queue.',
  },
}

const NO_VERDICT = {
  title: 'No verdict recorded',
  body: 'No triage outcome has been recorded for this enquiry yet.',
}

export function verdictExplanation(lead) {
  return VERDICTS[triageValue(lead)] ?? NO_VERDICT
}

// How far away the desired start date is, in plain words.
export function startTimingLabel(value, now = new Date()) {
  const date = toDate(value)
  if (!date) return null

  const start = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const days = Math.round((start - today) / DAY_IN_MS)

  if (days < 0) return 'Date has passed'
  if (days === 0) return 'Today'
  if (days === 1) return 'Tomorrow'
  if (days < 14) return `In ${days} days`
  if (days < 60) return `In ${Math.round(days / 7)} weeks`
  return `In about ${Math.round(days / 30)} months`
}

// The same reference the public sees on their enquiry confirmation.
export function enquiryReference(lead) {
  return lead?.id ? shortId(lead.id).toUpperCase() : null
}

// A mailto link that opens the consultant's email app, addressed to the lead.
export function enquiryMailto(lead) {
  if (!lead?.email) return null

  const reference = enquiryReference(lead)
  const subject = reference
    ? `Your enquiry with Strategies Migration Services (ref ${reference})`
    : 'Your enquiry with Strategies Migration Services'
  const firstName = String(lead.fullName ?? '').trim().split(' ')[0]
  const body = firstName ? `Dear ${firstName},\n\n` : ''

  return `mailto:${lead.email}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`
}
