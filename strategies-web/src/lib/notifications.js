// Notifications are case messages: { id, caseId, body, isRead, sentAt } from the API.
// Older mock data used { message, createdAt }, so both shapes are accepted.

// A notification is unread if the API says so and it has not been marked read in this session.
export function isUnread(notification, readIds) {
  return !notification.isRead && !readIds.has(notification.id)
}

// Text for the bell badge: empty when nothing is unread, "9+" above nine.
export function unreadCountLabel(count) {
  if (count <= 0) return ''
  return count > 9 ? '9+' : String(count)
}

export function notificationText(notification) {
  return notification?.body ?? notification?.message ?? ''
}

export function notificationDate(notification) {
  return notification?.sentAt ?? notification?.createdAt ?? null
}

// Unread total for the badge. Prefers the API's unreadCount, which covers every page,
// minus the ones marked read in this session. Without it, counts the loaded list.
export function unreadTotal(serverCount, notifications, readIds) {
  if (typeof serverCount === 'number') {
    const markedReadHere = notifications.filter((item) => !item.isRead && readIds.has(item.id)).length
    return Math.max(0, serverCount - markedReadHere)
  }
  return notifications.filter((item) => isUnread(item, readIds)).length
}
