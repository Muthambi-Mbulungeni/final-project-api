// Helpers for the reset password page, reached from the link in the reset email:
// {BaseUrl}/#/reset-password?token=...

export const RESET_PASSWORD_PATH = '/api/auth/reset-password'

const TOO_MANY_ATTEMPTS = 'Too many attempts, please wait a minute and try again.'
const INVALID_LINK = 'This reset link is invalid or has expired.'

// The token from the query string, or '' when the link has none.
export function resetTokenFrom(search) {
  return (new URLSearchParams(search ?? '').get('token') ?? '').trim()
}

// The request body for POST /api/auth/reset-password.
export function resetPasswordBody(token, password) {
  return { token, newPassword: password }
}

export const PASSWORD_MIN_LENGTH = 12
export const PASSWORD_MAX_LENGTH = 64

// Mirrors the API rule (12 to 64 characters, no composition rules). The API also rejects
// common and context-related passwords, so its 400 errors[] are shown as they come.
// Characters are counted as code points, so an emoji or accented letter counts as one.
export function newPasswordError(password) {
  if (!password) return 'Enter a new password.'
  const length = [...password].length
  if (length < PASSWORD_MIN_LENGTH) return `Use at least ${PASSWORD_MIN_LENGTH} characters.`
  if (length > PASSWORD_MAX_LENGTH) return `Use ${PASSWORD_MAX_LENGTH} characters or fewer.`
  return undefined
}

// Returns { field: message } for each invalid field. Empty when valid.
export function validateResetPassword({ password, confirm }) {
  const errors = {}
  const passwordMessage = newPasswordError(password)
  if (passwordMessage) errors.password = passwordMessage
  if (!confirm) errors.confirm = 'Enter the new password again.'
  else if (!passwordMessage && confirm !== password) errors.confirm = 'The passwords do not match.'
  return errors
}

// Turns a failed reset request into what the page should show:
// { kind: 'token' | 'field' | 'form', message }.
// A 400 with field errors is a password problem; any other 400 means the link cannot be used.
// Network failures and gateway errors already carry the shared "cannot be reached" message.
export function resetErrorOutcome(error) {
  if (error?.status === 429) return { kind: 'form', message: TOO_MANY_ATTEMPTS }
  if (error?.status === 400) {
    const fieldErrors = Array.isArray(error.errors) ? error.errors.filter(Boolean) : []
    if (fieldErrors.length) return { kind: 'field', message: fieldErrors.join(' ') }
    return { kind: 'token', message: error.message || INVALID_LINK }
  }
  return { kind: 'form', message: error?.message || 'The password could not be reset. Please try again.' }
}
