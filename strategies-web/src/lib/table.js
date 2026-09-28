// Pure helpers behind DataTable: search, filter, sort and paginate.
// Kept free of React so they can be unit tested directly.

export function matchesSearch(row, term, keys) {
  const needle = (term ?? '').trim().toLowerCase()
  if (!needle) return true
  return keys.some((key) => String(row[key] ?? '').toLowerCase().includes(needle))
}

export function matchesFilters(row, activeFilters, filterDefs) {
  return filterDefs.every((def) => {
    const selected = activeFilters[def.key]
    if (selected === undefined || selected === '') return true
    const value = def.getValue ? def.getValue(row) : row[def.key]
    return String(value) === String(selected)
  })
}

function compareValues(a, b) {
  if (typeof a === 'number' && typeof b === 'number') return a - b
  if (typeof a === 'boolean' && typeof b === 'boolean') return Number(a) - Number(b)
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' })
}

export function sortRows(rows, column, direction) {
  if (!column || !direction) return rows
  const getValue = column.sortValue ?? ((row) => row[column.key])
  const factor = direction === 'desc' ? -1 : 1

  return [...rows].sort((rowA, rowB) => {
    const a = getValue(rowA)
    const b = getValue(rowB)
    if (a == null && b == null) return 0
    if (a == null) return 1
    if (b == null) return -1
    return compareValues(a, b) * factor
  })
}

export function paginate(rows, page, pageSize) {
  const totalCount = rows.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const currentPage = Math.min(Math.max(1, page), totalPages)
  const start = (currentPage - 1) * pageSize

  return {
    items: rows.slice(start, start + pageSize),
    totalCount,
    totalPages,
    currentPage,
  }
}
