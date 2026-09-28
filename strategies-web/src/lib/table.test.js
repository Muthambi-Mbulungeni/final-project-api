import { describe, expect, it } from 'vitest'
import { matchesFilters, matchesSearch, paginate, sortRows } from './table'

const ROWS = [
  { id: 1, name: 'Amara Okafor', email: 'amara@example.com', status: 'Pending', score: 3 },
  { id: 2, name: 'Liam Chen', email: 'liam@example.com', status: 'Qualified', score: 1 },
  { id: 3, name: 'Fatima Patel', email: 'fatima@example.com', status: 'Pending', score: null },
]

describe('matchesSearch', () => {
  it('matches every row when the search term is empty or only spaces', () => {
    expect(matchesSearch(ROWS[0], '', ['name'])).toBe(true)
    expect(matchesSearch(ROWS[0], '   ', ['name'])).toBe(true)
  })

  it('matches case-insensitively across the given keys', () => {
    expect(matchesSearch(ROWS[2], 'PATEL', ['name', 'email'])).toBe(true)
    expect(matchesSearch(ROWS[1], 'liam@', ['name', 'email'])).toBe(true)
  })

  it('ignores fields that are not in the search keys', () => {
    expect(matchesSearch(ROWS[0], 'amara@', ['name'])).toBe(false)
  })

  it('treats missing values as empty text', () => {
    expect(matchesSearch({ name: null }, 'x', ['name'])).toBe(false)
  })
})

describe('matchesFilters', () => {
  const definitions = [{ key: 'status' }]

  it('passes every row when no filter value is selected', () => {
    expect(matchesFilters(ROWS[1], {}, definitions)).toBe(true)
    expect(matchesFilters(ROWS[1], { status: '' }, definitions)).toBe(true)
  })

  it('keeps only rows that match the selected value', () => {
    expect(matchesFilters(ROWS[0], { status: 'Pending' }, definitions)).toBe(true)
    expect(matchesFilters(ROWS[1], { status: 'Pending' }, definitions)).toBe(false)
  })

  it('uses getValue when a filter provides one', () => {
    const custom = [{ key: 'status', getValue: (row) => row.status.toUpperCase() }]
    expect(matchesFilters(ROWS[0], { status: 'PENDING' }, custom)).toBe(true)
  })
})

describe('sortRows', () => {
  it('returns the rows unchanged when no column or direction is given', () => {
    expect(sortRows(ROWS, null, 'asc')).toBe(ROWS)
    expect(sortRows(ROWS, { key: 'name' }, null)).toBe(ROWS)
  })

  it('sorts text ascending and descending', () => {
    expect(sortRows(ROWS, { key: 'name' }, 'asc').map((row) => row.id)).toEqual([1, 3, 2])
    expect(sortRows(ROWS, { key: 'name' }, 'desc').map((row) => row.id)).toEqual([2, 3, 1])
  })

  it('sorts numbers numerically and keeps empty values last in both directions', () => {
    expect(sortRows(ROWS, { key: 'score' }, 'asc').map((row) => row.id)).toEqual([2, 1, 3])
    expect(sortRows(ROWS, { key: 'score' }, 'desc').map((row) => row.id)).toEqual([1, 2, 3])
  })

  it('uses sortValue when a column provides one', () => {
    const column = { key: 'status', sortValue: (row) => (row.status === 'Qualified' ? 0 : 1) }
    expect(sortRows(ROWS, column, 'asc')[0].id).toBe(2)
  })

  it('does not change the original array', () => {
    const originalOrder = ROWS.map((row) => row.id)
    sortRows(ROWS, { key: 'name' }, 'desc')
    expect(ROWS.map((row) => row.id)).toEqual(originalOrder)
  })
})

describe('paginate', () => {
  const rows = Array.from({ length: 14 }, (_, index) => index + 1)

  it('returns the requested page with correct totals', () => {
    const result = paginate(rows, 2, 10)
    expect(result.items).toEqual([11, 12, 13, 14])
    expect(result.totalCount).toBe(14)
    expect(result.totalPages).toBe(2)
    expect(result.currentPage).toBe(2)
  })

  it('keeps the page number inside the valid range', () => {
    expect(paginate(rows, 99, 10).currentPage).toBe(2)
    expect(paginate(rows, 0, 10).currentPage).toBe(1)
  })

  it('reports a single empty page when there are no rows', () => {
    expect(paginate([], 1, 10)).toEqual({ items: [], totalCount: 0, totalPages: 1, currentPage: 1 })
  })
})
