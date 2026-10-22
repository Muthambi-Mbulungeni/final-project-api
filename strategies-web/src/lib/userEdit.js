// Editing a user's details as an administrator.
// PATCH /api/admin/users/{id} with { fullName?, email?, role? } (only the fields that changed)
// returns 200 and the updated user { id, fullName, email, role, isActive }.
// 400 { message, errors } is a validation problem. 409 { message, code: "duplicate_email" } means
// another account already uses that email address; any other 409 (last active admin, a consultant
// with active cases) is a rule the change breaks, explained in the message.
// Access (active / suspended) is not edited here: it has its own suspend and reinstate actions.
import { resolveEnum } from '../config/enums'
import { emailError } from './validation'

const FIELDS = ['fullName', 'email', 'role']
export const DUPLICATE_EMAIL_CODE = 'duplicate_email'

export function userEditPath(id) {
  return `/api/admin/users/${encodeURIComponent(id)}`
}

// The role as its API name ("Admin", "Consultant", "Client"), whether the API sent the name or the
// enum index; empty when unknown, so the form asks for one.
export function roleName(role) {
  return resolveEnum('userRole', role)?.value ?? ''
}

export function userEditValues(user) {
  return {
    fullName: user?.fullName ?? '',
    email: user?.email ?? '',
    role: roleName(user?.role),
  }
}

function normalise(field, value) {
  return field === 'role' ? value : String(value ?? '').trim()
}

// The fields whose value differs from the saved user, trimmed, ready to send. An edit that only
// adds spaces is not a change. Empty object when nothing changed.
export function changedUserFields(original, values) {
  const saved = userEditValues(original)
  const changes = {}
  for (const field of FIELDS) {
    const next = normalise(field, values[field])
    if (next !== normalise(field, saved[field])) changes[field] = next
  }
  return changes
}

// Returns { field: message } for each invalid field. Empty when valid.
export function validateUserEdit({ fullName, email, role }) {
  const errors = {}
  if (!String(fullName ?? '').trim()) errors.fullName = 'Enter the user’s full name.'
  if (!String(email ?? '').trim()) errors.email = 'Enter an email address.'
  else if (emailError(email)) errors.email = emailError(email)
  if (!resolveEnum('userRole', role)) errors.role = 'Choose a role.'
  return errors
}

// Turns a failed save into what the dialog shows: { email } for a duplicate address (under the
// email field), or { form: [messages] } above the buttons. Only the API's duplicate_email code
// goes to the email field; other 409s show their message as it is. The 400 errors are plain
// sentences with no field names, so they are listed together rather than guessed onto a field.
export function userEditErrorOutcome(error) {
  if (error?.status === 409 && error.code === DUPLICATE_EMAIL_CODE) {
    return { email: error.message || 'Another account already uses this email address.' }
  }
  const errors = Array.isArray(error?.errors) ? error.errors.filter(Boolean) : []
  if (error?.status === 400 && errors.length) return { form: errors }
  return { form: [error?.message || 'The changes were not saved. Please try again.'] }
}
