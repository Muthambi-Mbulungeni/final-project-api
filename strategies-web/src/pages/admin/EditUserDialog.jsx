import { useState } from 'react'
import FormDialog from '../../components/ui/FormDialog'
import { inputClasses } from '../../components/ui/fieldStyles'
import TextField from '../../components/ui/TextField'
import { ENUMS } from '../../config/enums'
import { apiPatch } from '../../lib/api'
import { changedUserFields, userEditErrorOutcome, userEditPath, userEditValues, validateUserEdit } from '../../lib/userEdit'

// Edits one user's name, email and role. Mount it per user (key by id) while open, so it always
// starts from that user's saved values. Save stays off until something actually changed, and only
// the changed fields are sent. Administrators cannot change their own role (no self-demotion).
export default function EditUserDialog({ user, isSelf, onSaved, onClose }) {
  const [values, setValues] = useState(() => userEditValues(user))
  const [errors, setErrors] = useState({})
  const [formError, setFormError] = useState(null)
  const [busy, setBusy] = useState(false)
  const changes = changedUserFields(user, values)
  if (isSelf) delete changes.role
  const unchanged = Object.keys(changes).length === 0

  function update(field, value) {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
    setFormError(null)
  }

  async function save() {
    const invalid = validateUserEdit(values)
    if (Object.keys(invalid).length) {
      setErrors(invalid)
      document.getElementById(`edit-user-${Object.keys(invalid)[0]}`)?.focus()
      return
    }

    setBusy(true)
    setFormError(null)
    try {
      const updated = await apiPatch(userEditPath(user.id), changes)
      onSaved(updated ?? { ...user, ...changes })
    } catch (error) {
      const outcome = userEditErrorOutcome(error)
      if (outcome.email) {
        setErrors((current) => ({ ...current, email: outcome.email }))
        document.getElementById('edit-user-email')?.focus()
      } else {
        setFormError(outcome.form)
      }
      setBusy(false)
    }
  }

  const roleHintId = 'edit-user-role-hint'
  const roleErrorId = 'edit-user-role-error'
  const roleDescribedBy = [isSelf ? roleHintId : null, errors.role ? roleErrorId : null].filter(Boolean).join(' ')

  return (
    <FormDialog
      open
      title="Edit user"
      description="Change this person’s name, sign-in email or role. Access is changed with Suspend and Reinstate."
      submitLabel="Save changes"
      busy={busy}
      submitDisabled={unchanged}
      error={formError}
      onSubmit={save}
      onCancel={onClose}
    >
      <TextField
        id="edit-user-fullName"
        label="Full name"
        autoComplete="off"
        value={values.fullName}
        onChange={(event) => update('fullName', event.target.value)}
        error={errors.fullName}
      />
      <TextField
        id="edit-user-email"
        label="Email address"
        type="email"
        autoComplete="off"
        spellCheck={false}
        value={values.email}
        onChange={(event) => update('email', event.target.value)}
        error={errors.email}
      />
      <div>
        <label htmlFor="edit-user-role" className="block text-sm font-medium text-stone-800">
          Role
        </label>
        <select
          id="edit-user-role"
          value={values.role}
          onChange={(event) => update('role', event.target.value)}
          disabled={isSelf}
          aria-invalid={Boolean(errors.role)}
          aria-describedby={roleDescribedBy || undefined}
          className={`mt-1.5 ${inputClasses(Boolean(errors.role))} disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-500`}
        >
          <option value="">Choose a role</option>
          {ENUMS.userRole.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        {isSelf && (
          <p id={roleHintId} className="mt-1.5 text-xs text-stone-500">
            You cannot change your own role. Ask another administrator.
          </p>
        )}
        {errors.role && (
          <p id={roleErrorId} className="mt-1.5 text-sm text-brand-700">
            {errors.role}
          </p>
        )}
      </div>
    </FormDialog>
  )
}
