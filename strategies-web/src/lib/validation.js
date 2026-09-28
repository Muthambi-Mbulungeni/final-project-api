const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

// Returns an error message for an email address, or undefined when it is valid.
export function emailError(value) {
  const email = (value ?? '').trim()
  if (!email) return 'Enter your email address.'
  if (!EMAIL_PATTERN.test(email)) return 'Enter a valid email address, like name@example.com.'
  return undefined
}

// Returns { field: message } for each invalid sign-in field. Empty when valid.
export function validateSignIn({ email, password }) {
  const errors = {}
  const emailMessage = emailError(email)
  if (emailMessage) errors.email = emailMessage
  if (!password) errors.password = 'Enter your password.'
  return errors
}
