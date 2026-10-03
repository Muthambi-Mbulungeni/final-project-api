const dateFormatter = new Intl.DateTimeFormat('en-ZA', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

const dateTimeFormatter = new Intl.DateTimeFormat('en-ZA', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

// .NET sends up to 7 fractional-second digits (e.g. 08:25:13.5513727Z).
// JavaScript only guarantees parsing of 3, so extra digits are trimmed first.
export function toDate(value) {
  if (!value) return null
  const normalised = typeof value === 'string' ? value.replace(/(\.\d{3})\d+/, '$1') : value
  const date = new Date(normalised)
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatDate(value) {
  const date = toDate(value)
  return date ? dateFormatter.format(date) : '—'
}

export function formatDateTime(value) {
  const date = toDate(value)
  return date ? dateTimeFormatter.format(date) : '—'
}

// Up to two initials from a person's name, e.g. "Thandi Mokoena" -> "TM".
export function getInitials(name) {
  return String(name ?? '')
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

// First 8 characters of an ID (e.g. a GUID) for compact display.
export function shortId(id) {
  if (id === null || id === undefined || id === '') return '—'
  return String(id).slice(0, 8)
}
