// Helpers for the confirm email page, reached from the link in the confirmation email:
// {BaseUrl}/#/confirm-email?token=...

export const CONFIRM_EMAIL_PATH = '/api/auth/confirm-email'
export const RESEND_CONFIRMATION_PATH = '/api/auth/resend-confirmation'

// The API answers resend-confirmation the same way for every email, so this never says whether one was sent.
export const RESEND_SENT_MESSAGE = "If that email has an account, we've sent a new link."

const TOO_MANY_ATTEMPTS = 'Too many attempts, please wait a minute and try again.'

// The token from the query string, or '' when the link has none.
export function confirmationTokenFrom(search) {
  return (new URLSearchParams(search ?? '').get('token') ?? '').trim()
}

// Turns a failed confirm request into what the page should show: { kind: 'invalid' | 'error', message }.
// Any 400 means the token cannot be used (invalid, expired or already used). Anything else is not the
// link's fault, so the page offers to try again. Network failures already carry the shared message.
export function confirmErrorOutcome(error) {
  if (error?.status === 400) return { kind: 'invalid', message: '' }
  if (error?.status === 429) return { kind: 'error', message: TOO_MANY_ATTEMPTS }
  return { kind: 'error', message: error?.message || 'Your email could not be confirmed. Please try again.' }
}

// The message for a failed resend request.
export function resendErrorMessage(error) {
  if (error?.status === 429) return TOO_MANY_ATTEMPTS
  return error?.message || 'The link could not be sent. Please try again.'
}
