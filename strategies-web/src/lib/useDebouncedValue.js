import { useEffect, useState } from 'react'

// Returns the value only after it has stopped changing for `delay` milliseconds.
// Used to avoid sending a search request on every keystroke.
export function useDebouncedValue(value, delay = 400) {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])

  return debounced
}
