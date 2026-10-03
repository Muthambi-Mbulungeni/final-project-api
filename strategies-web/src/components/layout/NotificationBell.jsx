import { useEffect, useRef, useState } from 'react'
import { Bell } from 'lucide-react'
import RequestState from '../ui/RequestState'
import { apiPatch } from '../../lib/api'
import { formatDateTime } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { isUnread, notificationDate, notificationText, unreadCountLabel, unreadTotal } from '../../lib/notifications'
import { useApi } from '../../lib/useApi'

// On a phone the panel is a full-width sheet under the header (the bell is not at the
// screen edge, so a dropdown anchored to it would run off the left side).
// From the sm breakpoint up it is a dropdown anchored to the bell.
const PANEL_CLASSES =
  'fixed inset-x-3 top-[4.5rem] z-30 rounded-lg border border-stone-200 bg-white shadow-lg shadow-stone-900/10 ' +
  'sm:absolute sm:inset-x-auto sm:top-full sm:right-0 sm:mt-2 sm:w-80 sm:shadow-md sm:shadow-stone-900/5'

export default function NotificationBell() {
  const [open, setOpen] = useState(false)
  const [readIds, setReadIds] = useState(() => new Set())
  const [actionError, setActionError] = useState('')
  const containerRef = useRef(null)
  const buttonRef = useRef(null)

  const request = useApi('/api/notifications')
  const notifications = listItems(request.data)
  const unread = notifications.filter((notification) => isUnread(notification, readIds))
  const unreadCount = unreadTotal(request.data?.unreadCount, notifications, readIds)
  const countLabel = unreadCountLabel(unreadCount)

  useEffect(() => {
    if (!open) return undefined

    function handlePointerDown(event) {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setOpen(false)
      }
    }

    // Escape closes the panel and puts focus back on the bell, so keyboard users are not lost.
    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        setOpen(false)
        buttonRef.current?.focus()
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  function toggleOpen() {
    setOpen((current) => !current)
    setActionError('')
  }

  // Marks notifications read straight away, then undoes the change if the API request fails.
  async function markRead(ids) {
    if (ids.length === 0) return
    setActionError('')
    setReadIds((previous) => new Set([...previous, ...ids]))

    try {
      await Promise.all(ids.map((id) => apiPatch(`/api/notifications/${encodeURIComponent(id)}/read`)))
    } catch (error) {
      setReadIds((previous) => {
        const next = new Set(previous)
        ids.forEach((id) => next.delete(id))
        return next
      })
      setActionError(`Could not update notifications. ${error.message}`)
    }
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        ref={buttonRef}
        type="button"
        onClick={toggleOpen}
        aria-haspopup="true"
        aria-expanded={open}
        aria-label={unreadCount > 0 ? `Notifications, ${unreadCount} unread` : 'Notifications'}
        className="relative rounded-md p-2 text-stone-500 hover:bg-stone-100 hover:text-stone-900"
      >
        <Bell className="size-5" strokeWidth={1.75} aria-hidden="true" />
        {countLabel && (
          <span className="absolute top-1 right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand-600 px-1 text-[10px] leading-none font-semibold text-white">
            {countLabel}
          </span>
        )}
      </button>

      {open && (
        <div role="dialog" aria-label="Notifications" className={PANEL_CLASSES}>
          <div className="flex items-center justify-between gap-4 border-b border-stone-200 px-4 py-3">
            <p className="text-sm font-semibold text-stone-900">Notifications</p>
            {unread.length > 0 && (
              <button
                type="button"
                onClick={() => markRead(unread.map((notification) => notification.id))}
                className="text-xs font-medium whitespace-nowrap text-brand-700 hover:text-brand-800"
              >
                Mark all as read
              </button>
            )}
          </div>

          {actionError && (
            <p role="alert" className="border-b border-stone-200 bg-brand-50 px-4 py-2 text-xs text-brand-800">
              {actionError}
            </p>
          )}

          <div className="max-h-[min(20rem,calc(100dvh-9rem))] overflow-y-auto px-4">
            <RequestState
              request={request}
              isEmpty={notifications.length === 0}
              emptyTitle="You're all caught up"
              emptyMessage="Messages about your cases appear here."
              loadingText="Loading notifications…"
            >
              <ul className="divide-y divide-stone-100">
                {notifications.map((notification, index) => {
                  const unreadItem = isUnread(notification, readIds)
                  return (
                    <li key={notification.id ?? index}>
                      <button
                        type="button"
                        onClick={() => unreadItem && markRead([notification.id])}
                        disabled={!unreadItem}
                        title={unreadItem ? 'Mark as read' : undefined}
                        className="flex w-full items-start gap-3 py-3 text-left disabled:cursor-default"
                      >
                        <span
                          className={`mt-1.5 size-2 shrink-0 rounded-full ${unreadItem ? 'bg-brand-600' : 'bg-transparent'}`}
                          aria-hidden="true"
                        />
                        <span className="min-w-0 flex-1">
                          <span className={`block text-sm ${unreadItem ? 'font-medium text-stone-900' : 'text-stone-600'}`}>
                            {notificationText(notification) || 'New message on your case'}
                          </span>
                          <span className="mt-0.5 block text-xs text-stone-500 tabular-nums">
                            {formatDateTime(notificationDate(notification))}
                          </span>
                        </span>
                      </button>
                    </li>
                  )
                })}
              </ul>
            </RequestState>
          </div>
        </div>
      )}
    </div>
  )
}
