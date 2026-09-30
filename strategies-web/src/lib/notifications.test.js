import { describe, expect, it } from 'vitest'
import { isUnread, notificationDate, notificationText, unreadCountLabel, unreadTotal } from './notifications'

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

describe('notification fields', () => {
  it('reads the text and date from the API message shape', () => {
    const message = { id: 'a', body: 'Your documents were approved.', sentAt: '2026-09-30T08:00:00Z' }
    expect(notificationText(message)).toBe('Your documents were approved.')
    expect(notificationDate(message)).toBe('2026-09-30T08:00:00Z')
  })

  it('still reads the older mock shape', () => {
    const legacy = { id: 'b', message: 'Case updated.', createdAt: '2026-09-28T10:00:00Z' }
    expect(notificationText(legacy)).toBe('Case updated.')
    expect(notificationDate(legacy)).toBe('2026-09-28T10:00:00Z')
  })
})

describe('unreadTotal', () => {
  const list = [
    { id: 'a', isRead: false },
    { id: 'b', isRead: false },
    { id: 'c', isRead: true },
  ]

  it('uses the API unread count across all pages, minus those marked read here', () => {
    expect(unreadTotal(12, list, new Set(['a']))).toBe(11)
    expect(unreadTotal(0, list, new Set(['a']))).toBe(0)
  })

  it('counts the loaded list when the API sends no unread count', () => {
    expect(unreadTotal(undefined, list, new Set(['a']))).toBe(1)
  })
})
