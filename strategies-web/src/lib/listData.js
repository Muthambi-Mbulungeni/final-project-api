// Returns the list of items from an API response.
// Handles both shapes the API uses: a plain array (e.g. /api/leads today)
// and the paginated wrapper { data, totalRecords, totalPages, currentPage }.
export function listItems(body) {
  if (Array.isArray(body)) return body
  if (Array.isArray(body?.data)) return body.data
  return []
}
