import { useCallback, useEffect, useState } from 'react'
import { apiGet } from './api'

// Loads JSON from an API path and exposes { data, loading, error, reload }.
// A new request starts whenever the path changes (e.g. a new page or search term).
// The previous data stays available while the next request loads.
// Requests are cancelled if the path changes or the component unmounts first.
export function useApi(path) {
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState({ key: null, data: null, error: null })
  const key = `${path}#${attempt}`

  useEffect(() => {
    const controller = new AbortController()

    apiGet(path, { signal: controller.signal })
      .then((data) => setResult({ key, data, error: null }))
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setResult({ key, data: null, error })
        }
      })

    return () => controller.abort()
  }, [key, path])

  const reload = useCallback(() => setAttempt((count) => count + 1), [])

  const isCurrent = result.key === key

  return {
    data: result.data,
    error: isCurrent ? result.error : null,
    loading: !isCurrent,
    reload,
  }
}
