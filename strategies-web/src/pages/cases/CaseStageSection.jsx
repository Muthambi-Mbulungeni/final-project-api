import { useRef, useState } from 'react'
import { Check } from 'lucide-react'
import { useToast } from '../../components/ui/toastContext'
import { ENUMS, enumOrder, resolveEnum } from '../../config/enums'
import { apiPatch } from '../../lib/api'
import {
  canChangeStage,
  stageActionLabel,
  stageBody,
  stageChangeError,
  stageErrorMessage,
  stagePath,
} from '../../lib/cases'

const NO_ERRORS = { field: null, request: null }

// Where the case sits in the five-stage lifecycle. Built only from the case's current stage:
// the API has no stage history endpoint, so no dates or past moves are shown.
function StageSteps({ stage }) {
  const current = enumOrder('caseStage', stage)

  return (
    <ol className="mt-3 space-y-2">
      {ENUMS.caseStage.map((option, index) => {
        const done = current !== null && index < current
        const isCurrent = index === current
        return (
          <li key={option.value} aria-current={isCurrent ? 'step' : undefined} className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className={`flex size-5 shrink-0 items-center justify-center rounded-full border ${
                done
                  ? 'border-brand-600 bg-brand-600 text-white'
                  : isCurrent
                    ? 'border-brand-600 bg-white'
                    : 'border-stone-300 bg-white'
              }`}
            >
              {done && <Check className="size-3" strokeWidth={2.5} />}
              {isCurrent && <span className="size-2 rounded-full bg-brand-600" />}
            </span>
            <span className={`text-sm ${isCurrent ? 'font-medium text-stone-900' : 'text-stone-600'}`}>
              {option.label}
              {done && <span className="sr-only"> (completed)</span>}
              {isCurrent && <span className="sr-only"> (current stage)</span>}
            </span>
          </li>
        )
      })}
    </ol>
  )
}

// Shows the case's stage and lets staff move it. The API decides who may: a consultant who
// is not assigned to the case gets its 403 explained. Mounted per case (keyed by id).
export default function CaseStageSection({ caseItem, onStageChanged }) {
  const toast = useToast()
  const [nextStage, setNextStage] = useState('')
  const [errors, setErrors] = useState(NO_ERRORS)
  const [busy, setBusy] = useState(false)
  const headingRef = useRef(null)
  const selectRef = useRef(null)
  const editable = canChangeStage(caseItem)
  const currentValue = resolveEnum('caseStage', caseItem.stage)?.value

  async function handleSubmit(event) {
    event.preventDefault()
    const fieldError = stageChangeError(caseItem.stage, nextStage)
    if (fieldError) {
      setErrors({ field: fieldError, request: null })
      selectRef.current?.focus()
      return
    }

    setBusy(true)
    setErrors(NO_ERRORS)
    try {
      await apiPatch(stagePath(caseItem.id), stageBody(nextStage))
      const label = resolveEnum('caseStage', nextStage)?.label ?? nextStage
      onStageChanged(caseItem.id, nextStage)
      setNextStage('')
      requestAnimationFrame(() => headingRef.current?.focus())
      toast.show(`Case moved to ${label.toLowerCase()}.`)
    } catch (error) {
      setErrors({ field: null, request: stageErrorMessage(error) })
    } finally {
      setBusy(false)
    }
  }

  return (
    <section aria-labelledby="case-stage-heading" className="border-b border-stone-200 pb-6">
      {/* tabIndex -1: focus lands here after a stage change; not a control, so no outline. */}
      <h3 id="case-stage-heading" ref={headingRef} tabIndex={-1} className="text-sm font-semibold text-stone-900 outline-none">
        Stage
      </h3>
      <StageSteps stage={caseItem.stage} />

      {editable && (
        <form noValidate onSubmit={handleSubmit} className="mt-5 space-y-2">
          <label htmlFor="case-stage-select" className="block text-sm font-medium text-stone-900">
            Move this case to
          </label>
          <div className="flex flex-col gap-2 sm:flex-row">
            <select
              id="case-stage-select"
              ref={selectRef}
              value={nextStage}
              onChange={(event) => {
                setNextStage(event.target.value)
                setErrors(NO_ERRORS)
              }}
              disabled={busy}
              aria-invalid={errors.field ? true : undefined}
              aria-describedby={errors.field ? 'case-stage-error' : undefined}
              className="block w-full min-w-0 rounded-md border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 aria-[invalid=true]:border-brand-600 sm:flex-1"
            >
              <option value="">Choose a stage</option>
              {ENUMS.caseStage.map((option) => (
                <option key={option.value} value={option.value} disabled={option.value === currentValue}>
                  {option.value === currentValue ? `${option.label} (current)` : option.label}
                </option>
              ))}
            </select>
            <button
              type="submit"
              disabled={busy}
              className="shrink-0 rounded-md bg-brand-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-60"
            >
              {busy ? 'Saving…' : stageActionLabel(caseItem.stage, nextStage)}
            </button>
          </div>
          {errors.field && (
            <p id="case-stage-error" className="text-sm text-brand-700">
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
