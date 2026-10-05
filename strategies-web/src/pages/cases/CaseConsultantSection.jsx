import { useRef, useState } from 'react'
import ErrorNotice from '../../components/ui/ErrorNotice'
import { useToast } from '../../components/ui/toastContext'
import { apiPatch } from '../../lib/api'
import { getInitials } from '../../lib/format'
import {
  STAFF_LOOKUP_PATH,
  assignBody,
  assignErrorMessage,
  assignPath,
  assignedConsultantName,
  canChangeStage,
  consultantChoices,
  currentConsultantId,
} from '../../lib/cases'
import { useApi } from '../../lib/useApi'

const NO_ERRORS = { field: null, request: null }

// Admin only (PATCH /api/cases/{id}/assign is [Authorize(Roles = "Admin")]): shows who handles
// the case and lets an administrator assign or reassign it. Only active consultants are offered.
// Archived cases stay read-only, like their stage. Mounted per case (keyed by id).
export default function CaseConsultantSection({ caseItem, onAssigned }) {
  const toast = useToast()
  const staff = useApi(STAFF_LOOKUP_PATH)
  const [choice, setChoice] = useState('')
  const [errors, setErrors] = useState(NO_ERRORS)
  const [busy, setBusy] = useState(false)
  const headingRef = useRef(null)
  const selectRef = useRef(null)

  const assignedName = assignedConsultantName(caseItem)
  const editable = canChangeStage(caseItem)
  const { consultants, complete } = consultantChoices(staff.data)
  const currentId = currentConsultantId(consultants, assignedName)

  async function handleSubmit(event) {
    event.preventDefault()
    const chosen = consultants.find((consultant) => consultant.id === choice)
    if (!chosen) {
      setErrors({ field: 'Choose the consultant to assign.', request: null })
      selectRef.current?.focus()
      return
    }

    setBusy(true)
    setErrors(NO_ERRORS)
    try {
      await apiPatch(assignPath(caseItem.id), assignBody(chosen.id))
      onAssigned(caseItem.id, chosen.fullName)
      setChoice('')
      requestAnimationFrame(() => headingRef.current?.focus())
      toast.show(`Case assigned to ${chosen.fullName}.`)
    } catch (error) {
      setErrors({ field: null, request: assignErrorMessage(error) })
    } finally {
      setBusy(false)
    }
  }

  return (
    <section aria-labelledby="case-consultant-heading" className="border-b border-stone-200 py-6">
      {/* tabIndex -1: focus lands here after assigning; not a control, so no outline. */}
      <h3
        id="case-consultant-heading"
        ref={headingRef}
        tabIndex={-1}
        className="text-sm font-semibold text-stone-900 outline-none"
      >
        Consultant
      </h3>
      {assignedName ? (
        <p className="mt-3 flex items-center gap-3 text-sm font-medium text-stone-900">
          <span
            className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-100 text-xs font-semibold text-stone-700"
            aria-hidden="true"
          >
            {getInitials(assignedName)}
          </span>
          {assignedName}
        </p>
      ) : (
        <p className="mt-3 flex items-center gap-3 text-sm font-medium text-brand-700">
          <span className="size-8 shrink-0 rounded-full border border-dashed border-brand-200" aria-hidden="true" />
          Not assigned yet
        </p>
      )}

      {editable && staff.error && (
        <ErrorNotice
          className="mt-3"
          message={`Consultants could not be loaded. ${staff.error.message}`}
          onRetry={staff.reload}
        />
      )}

      {editable && !staff.error && (
        <form noValidate onSubmit={handleSubmit} className="mt-4 space-y-2">
          <label htmlFor="case-consultant-select" className="block text-sm font-medium text-stone-900">
            {assignedName ? 'Reassign to' : 'Assign to'}
          </label>
          <div className="flex flex-col gap-2 sm:flex-row">
            <select
              id="case-consultant-select"
              ref={selectRef}
              value={choice}
              onChange={(event) => {
                setChoice(event.target.value)
                setErrors(NO_ERRORS)
              }}
              disabled={busy || staff.loading}
              aria-invalid={errors.field ? true : undefined}
              aria-describedby={errors.field ? 'case-consultant-error' : undefined}
              className="block w-full min-w-0 rounded-md border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 aria-[invalid=true]:border-brand-600 sm:flex-1"
            >
              <option value="">{staff.loading ? 'Loading consultants…' : 'Choose a consultant'}</option>
              {consultants.map((consultant) => (
                <option key={consultant.id} value={consultant.id} disabled={consultant.id === currentId}>
                  {consultant.id === currentId ? `${consultant.fullName} (current)` : consultant.fullName}
                </option>
              ))}
            </select>
            <button
              type="submit"
              disabled={busy || staff.loading}
              className="shrink-0 rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-semibold text-stone-900 transition-colors hover:bg-stone-50 disabled:cursor-wait disabled:opacity-60"
            >
              {busy ? 'Saving…' : assignedName ? 'Reassign' : 'Assign'}
            </button>
          </div>
          {!staff.loading && consultants.length === 0 && (
            <p className="text-sm text-stone-600">There are no active consultants to assign.</p>
          )}
          {!complete && (
            <p className="text-xs text-stone-500">
              Only the first 100 accounts were checked, so some consultants may be missing.
            </p>
          )}
          {errors.field && (
            <p id="case-consultant-error" className="text-sm text-brand-700">
              {errors.field}
            </p>
          )}
          {errors.request && (
            <p role="alert" className="text-sm text-brand-700">
              {errors.request}
            </p>
          )}
        </form>
      )}
    </section>
  )
}
