import { describe, expect, it } from 'vitest'
import { listItems } from './listData'

describe('listItems', () => {
  it('returns a plain array response as it is', () => {
    expect(listItems([{ id: 1 }, { id: 2 }])).toEqual([{ id: 1 }, { id: 2 }])
  })

  it('returns the data array from a paginated wrapper', () => {
    expect(listItems({ data: [{ id: 1 }], totalRecords: 1, totalPages: 1, currentPage: 1 })).toEqual([{ id: 1 }])
  })

  it('returns an empty list for anything else', () => {
    expect(listItems(null)).toEqual([])
    expect(listItems({ message: 'Validation failed.', errors: [] })).toEqual([])
  })
})
