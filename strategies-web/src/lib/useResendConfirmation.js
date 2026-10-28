import { useCallback, useState } from 'react'
import { apiPost } from './api'
import { RESEND_CONFIRMATION_PATH, RESEND_SENT_MESSAGE, resendErrorMessage } from './confirmEmail'

// Sends a new confirmation email. `message` is the neutral sent text, `error` a request failure;
// `reset` clears both, e.g. when the email being resent to changes.
export function useResendConfirmation() {
  const [sending, setSending] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const send = useCallback(async (email) => {
    setSending(true)
    setMessage('')
    setError('')
    try {
      await apiPost(RESEND_CONFIRMATION_PATH, { email: email.trim() })
      setMessage(RESEND_SENT_MESSAGE)
    } catch (failure) {
      setError(resendErrorMessage(failure))
    } finally {
      setSending(false)
    }
  }, [])

  const reset = useCallback(() => {
    setMessage('')
    setError('')
  }, [])

  return { send, sending, message, error, reset }
}
