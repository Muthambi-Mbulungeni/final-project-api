// Turns a failed sign-in request into a message for the person signing in.
// Deliberately never reveals whether the email or the password was wrong.
export function signInErrorMessage(error) {
  switch (error?.status) {
    case 400:
    case 401:
      return 'The email address or password is incorrect.'
    case 403:
      return 'This account is not active. Contact your consultant or the practice administrator.'
    case 404:
    case 405:
    case 501:
      return 'Sign-in is temporarily unavailable. Please try again later.'
    case 423:
      return error?.message || 'This account is temporarily locked due to multiple failed sign-in attempts. Please try again later.'
    case 429:
      return 'Too many sign-in attempts. Wait a minute and try again.'
    default:
      return error?.message || 'Sign-in failed. Please try again.'
  }
}
