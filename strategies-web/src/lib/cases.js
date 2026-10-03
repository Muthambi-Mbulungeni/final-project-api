// The client's most recently opened case. Clients normally have one; if there are more, the newest wins.
export function latestCase(cases) {
  return [...cases].sort((a, b) => String(b.createdAt).localeCompare(String(a.createdAt)))[0] ?? null
}
