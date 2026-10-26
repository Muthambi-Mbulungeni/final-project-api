const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const DATE_INPUT_PATTERN = /^\d{4}-\d{2}-\d{2}$/

// Formats a date as YYYY-MM-DD in local time, the format used by <input type="date">.
export function dateInputValue(date) {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

// True when a YYYY-MM-DD value is strictly after today (local time).
export function isFutureDate(value, today = new Date()) {
  if (!DATE_INPUT_PATTERN.test(value ?? '')) return false
  return value > dateInputValue(today)
}

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

// Enquiry step 1: who the person is.
export function validateEnquiryDetails({ fullName, email, immigrationStatus }) {
  const errors = {}
  if ((fullName ?? '').trim().length < 2) errors.fullName = 'Enter your full name.'
  const emailMessage = emailError(email)
  if (emailMessage) errors.email = emailMessage
  if (!immigrationStatus) errors.immigrationStatus = 'Choose the option that best describes you.'
  return errors
}

// Enquiry step 2: their plans and consent.
// The code the API sends back when an enquiry arrives without POPIA consent.
export const CONSENT_REQUIRED_CODE = 'consent_required'
export const CONSENT_REQUIRED_MESSAGE = 'You need to agree before we can assess your enquiry.'

export function validateEnquiryPlans({ desiredStartDate, consent }, today = new Date()) {
  const errors = {}
  if (!desiredStartDate) {
    errors.desiredStartDate = 'Choose the date you would like to start.'
  } else if (!isFutureDate(desiredStartDate, today)) {
    errors.desiredStartDate = 'Choose a date after today.'
  }
  if (!consent) errors.consent = CONSENT_REQUIRED_MESSAGE
  return errors
}

// Field errors for a failed enquiry submission. Only the API's consent_required code is tied to a field
// (the consent checkbox); anything else stays a message above the form. Empty when nothing maps.
export function enquiryServerFieldErrors(error) {
  return error?.code === CONSENT_REQUIRED_CODE ? { consent: CONSENT_REQUIRED_MESSAGE } : {}
}
