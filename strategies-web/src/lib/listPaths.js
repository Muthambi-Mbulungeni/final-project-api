// API paths for the admin lists. Paging, search and filters all happen on the server.

export function usersPath({ page = 1, pageSize = 10, search = '' } = {}) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  const term = search.trim()
  if (term) params.set('search', term)
  return `/api/admin/users?${params.toString()}`
}

export function casesPath({ page = 1, pageSize = 10, search = '', stage = '' } = {}) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  const term = search.trim()
  if (term) params.set('search', term)
  if (stage) params.set('stage', stage)
  return `/api/cases?${params.toString()}`
}
