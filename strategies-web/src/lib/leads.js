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

// Once a consultant has decided, the status is theirs, not the agent's.
const REVIEWED_VERDICTS = {
  Qualified: {
    title: 'Qualified by a consultant',
    body: 'A consultant reviewed this enquiry and confirmed it can proceed.',
  },
  Unqualified: {
    title: 'Unqualified by a consultant',
    body: 'A consultant reviewed this enquiry and decided it cannot proceed.',
  },
}

const NO_VERDICT = {
  title: 'No verdict recorded',
  body: 'No triage outcome has been recorded for this enquiry yet.',
}

export function isReviewed(lead) {
  return Boolean(lead?.reviewedById)
}

export function verdictExplanation(lead) {
  const value = triageValue(lead)
  if (isReviewed(lead) && REVIEWED_VERDICTS[value]) return REVIEWED_VERDICTS[value]
  return VERDICTS[value] ?? NO_VERDICT
}

// PATCH /api/leads/{id}/status takes { status } as the enum name and records the reviewer.
// The API turns a missing or unknown status into Pending or a raw 400, so only these two
// decisions are ever sent: Pending and NeedsReview would mark a lead reviewed yet leave it queued.
const DECISIONS = ['Qualified', 'Unqualified']

export function leadStatusPath(leadId) {
  return `/api/leads/${encodeURIComponent(leadId)}/status`
}

export function leadStatusBody(status) {
  return { status }
}

// Before review both are offered: confirming the agent's verdict also counts as a review.
// After review only the other decision is offered, so a click always changes something.
export function decisionChoices(lead) {
  if (!lead) return []
  if (!isReviewed(lead)) return DECISIONS
  return DECISIONS.filter((status) => status !== triageValue(lead))
}

export function decisionLabel(lead, status) {
  const label = resolveEnum('triageStatus', status)?.label.toLowerCase() ?? status
  return `${isReviewed(lead) ? 'Change to' : 'Mark'} ${label}`
}

// The lead as the panel should show it once the API has accepted the decision.
export function applyDecision(lead, status, reviewerId) {
  return { ...lead, triageStatus: status, reviewedById: reviewerId ?? lead.reviewedById }
}

// The API's 400 is the framework's raw validation body, so it is replaced with plain language.
export function decisionErrorMessage(error) {
  if (error?.status === 403) return 'Your account isn’t allowed to review leads.'
  if (error?.status === 404) return 'This lead no longer exists. Close the panel and refresh the list.'
  if (error?.status === 400) return 'That decision wasn’t accepted. Refresh the page and try again.'
  return error?.message || 'The decision could not be saved. Please try again.'
}

// The intake agent's own explanation, saved when the enquiry arrived. Null when the
// agent gave none (AI unavailable, unparseable reply, or older seeded leads).
export function agentReason(lead) {
  const reason = lead?.triageReason
  if (typeof reason !== 'string') return null
  const trimmed = reason.trim()
  return trimmed || null
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
