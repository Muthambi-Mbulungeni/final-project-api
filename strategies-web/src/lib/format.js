const dateFormatter = new Intl.DateTimeFormat('en-ZA', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

// .NET sends up to 7 fractional-second digits (e.g. 08:25:13.5513727Z).
// JavaScript only guarantees parsing of 3, so extra digits are trimmed first.
function normaliseTimestamp(value) {
  return typeof value === 'string' ? value.replace(/(\.\d{3})\d+/, '$1') : value
}

export function formatDate(value) {
  if (!value) return '—'
  const date = new Date(normaliseTimestamp(value))
  return Number.isNaN(date.getTime()) ? '—' : dateFormatter.format(date)
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
