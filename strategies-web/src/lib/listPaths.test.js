import { describe, expect, it } from 'vitest'
import { casesPath, usersPath } from './listPaths'

describe('usersPath', () => {
  it('asks for one page of users, with an optional trimmed search', () => {
    expect(usersPath()).toBe('/api/admin/users?page=1&pageSize=10')
    expect(usersPath({ page: 3, pageSize: 10, search: '  thandi ' })).toBe(
      '/api/admin/users?page=3&pageSize=10&search=thandi',
    )
  })
})

describe('casesPath', () => {
  it('asks for one page of cases, with optional search and lifecycle stage', () => {
    expect(casesPath()).toBe('/api/cases?page=1&pageSize=10')
    expect(casesPath({ page: 2, pageSize: 10, search: 'visa', stage: 'UnderReview' })).toBe(
      '/api/cases?page=2&pageSize=10&search=visa&stage=UnderReview',
    )
  })
})
