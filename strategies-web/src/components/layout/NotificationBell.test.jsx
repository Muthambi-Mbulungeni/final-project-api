// @vitest-environment jsdom
import { act, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { json } from '../../test/dom'
import { MESSAGES_READ_EVENT } from '../../lib/messages'
import NotificationBell from './NotificationBell'

function notifications(unreadCount) {
  const data = Array.from({ length: unreadCount }, (_, index) => ({
    id: `n${index}`,
    caseId: 'case-1',
    body: `Message ${index}`,
    isRead: false,
    sentAt: '2026-10-20T08:30:00Z',
  }))
  return { unreadCount, totalRecords: data.length, totalPages: 1, currentPage: 1, data }
}

describe('NotificationBell', () => {
  it('reloads the unread count when a message thread is marked read', async () => {
    let unread = 2
    const fetchMock = vi.fn(async () => json(notifications(unread)))
    vi.stubGlobal('fetch', fetchMock)
    render(<NotificationBell />)

    expect(await screen.findByText('2')).toBeTruthy()

    unread = 0
    act(() => window.dispatchEvent(new Event(MESSAGES_READ_EVENT)))

    await waitFor(() => expect(screen.queryByText('2')).toBeNull())
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
