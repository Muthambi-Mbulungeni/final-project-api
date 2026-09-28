import { useEffect } from 'react'

export const APP_NAME = 'Strategies Migration Services'

// Sets the browser tab title, e.g. "Sign in · Strategies Migration Services".
export function useDocumentTitle(title) {
  useEffect(() => {
    document.title = title ? `${title} · ${APP_NAME}` : APP_NAME
  }, [title])
}
