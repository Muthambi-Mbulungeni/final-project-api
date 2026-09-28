import { useCallback, useEffect, useState } from 'react'
import { apiGet } from './api'

// Loads JSON from an API path and exposes { data, loading, error, reload }.
// Requests are cancelled if the component unmounts before they finish.
export function useApi(path) {
  const [state, setState] = useState({ data: null, error: null, loading: true })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    apiGet(path, { signal: controller.signal })
      .then((data) => setState({ data, error: null, loading: false }))
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setState({ data: null, error, loading: false })
        }
      })

    return () => controller.abort()
  }, [path, attempt])

  const reload = useCallback(() => {
    setState((previous) => ({ ...previous, error: null, loading: true }))
    setAttempt((count) => count + 1)
  }, [])

  return { ...state, reload }
}
