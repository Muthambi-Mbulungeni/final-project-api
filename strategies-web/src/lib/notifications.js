// A notification is unread if the API says so and it has not been marked read in this session.
export function isUnread(notification, readIds) {
  return !notification.isRead && !readIds.has(notification.id)
}

// Text for the bell badge: empty when nothing is unread, "9+" above nine.
export function unreadCountLabel(count) {
  if (count <= 0) return ''
  return count > 9 ? '9+' : String(count)
}
