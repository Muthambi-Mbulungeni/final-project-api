import { describe, expect, it } from 'vitest'
import { findCurrentItem, NAVIGATION } from './navigation'

const admin = NAVIGATION.admin

describe('findCurrentItem', () => {
  it('matches the overview only on its exact path', () => {
    expect(findCurrentItem(admin, '/admin')?.label).toBe('Overview')
  })

  it('matches a section page and its sub-pages', () => {
    expect(findCurrentItem(admin, '/admin/users')?.label).toBe('Users')
    expect(findCurrentItem(admin, '/admin/users/123')?.label).toBe('Users')
  })

  it('ignores a trailing slash', () => {
    expect(findCurrentItem(admin, '/admin/cases/')?.label).toBe('Cases')
    expect(findCurrentItem(admin, '/admin/')?.label).toBe('Overview')
  })

  it('returns null for paths outside the navigation', () => {
    expect(findCurrentItem(admin, '/admin/unknown')).toBeNull()
    expect(findCurrentItem(admin, '/consultant')).toBeNull()
  })
})
