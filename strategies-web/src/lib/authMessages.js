// Turns a failed sign-in request into a message for the person signing in.
// Deliberately never reveals whether the email or the password was wrong.
export const ACCOUNT_LOCKED_CODE = 'account_locked'

// "about 15 minutes", "about 1 minute", or "a few minutes" when the API gave no time.
export function waitPhrase(seconds) {
  if (!Number.isFinite(seconds) || seconds <= 0) return 'a few minutes'
  const minutes = Math.max(1, Math.ceil(seconds / 60))
  return `about ${minutes} ${minutes === 1 ? 'minute' : 'minutes'}`
}

export function signInErrorMessage(error) {
  if (error?.code === ACCOUNT_LOCKED_CODE) {
    return `This account is locked after too many failed sign-in attempts. Try again in ${waitPhrase(error.retryAfter)}, or reset your password.`
  }

  switch (error?.status) {
    case 400:
      return 'The email address or password is incorrect.'
    // The API answers an unconfirmed account exactly like a wrong password, so both are mentioned.
    case 401:
      return "The email address or password is incorrect. If you've just registered, confirm your email first."
    case 403:
      return 'This account is not active. Contact your consultant or the practice administrator.'
    case 404:
    case 405:
    case 501:
      return 'Sign-in is temporarily unavailable. Please try again later.'
    case 429:
      return 'Too many sign-in attempts. Wait a minute and try again.'
    default:
      return error?.message || 'Sign-in failed. Please try again.'
  }
}
