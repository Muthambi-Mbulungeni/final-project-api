import { describe, expect, it } from 'vitest'
import { isUnread, unreadCountLabel } from './notifications'

describe('isUnread', () => {
  it('treats a notification the API marks as not read as unread', () => {
    expect(isUnread({ id: 'a', isRead: false }, new Set())).toBe(true)
  })

  it('treats a notification marked read in this session as read', () => {
    expect(isUnread({ id: 'a', isRead: false }, new Set(['a']))).toBe(false)
  })

  it('treats a notification the API marks as read as read', () => {
    expect(isUnread({ id: 'a', isRead: true }, new Set())).toBe(false)
  })
})

describe('unreadCountLabel', () => {
  it('returns nothing when there are no unread notifications', () => {
    expect(unreadCountLabel(0)).toBe('')
  })

  it('shows the exact count up to nine and 9+ above that', () => {
    expect(unreadCountLabel(1)).toBe('1')
    expect(unreadCountLabel(9)).toBe('9')
    expect(unreadCountLabel(12)).toBe('9+')
  })
})
