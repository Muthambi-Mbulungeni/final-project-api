import { countFromLeadsResponse, leadsPath, summaryFromCounts } from './leads'
import { useApi } from './useApi'

// The intake funnel, counted by the API: one tiny request per verdict (?status=X&pageSize=1),
// each reading totalRecords. Accurate however many leads exist, and allowed for consultants.
export function useLeadSummary() {
  const pending = useApi(leadsPath({ pageSize: 1, status: 'Pending' }))
  const needsReview = useApi(leadsPath({ pageSize: 1, status: 'NeedsReview' }))
  const qualified = useApi(leadsPath({ pageSize: 1, status: 'Qualified' }))
  const unqualified = useApi(leadsPath({ pageSize: 1, status: 'Unqualified' }))
  const requests = [pending, needsReview, qualified, unqualified]

  const summary = summaryFromCounts({
    Pending: countFromLeadsResponse(pending.data, 'Pending'),
    NeedsReview: countFromLeadsResponse(needsReview.data, 'NeedsReview'),
    Qualified: countFromLeadsResponse(qualified.data, 'Qualified'),
    Unqualified: countFromLeadsResponse(unqualified.data, 'Unqualified'),
  })

  return {
    summary,
    loading: requests.some((request) => request.loading),
    error: requests.find((request) => request.error)?.error ?? null,
    reload: () => requests.forEach((request) => request.reload()),
  }
}
