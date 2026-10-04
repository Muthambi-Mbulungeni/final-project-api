import { useState } from 'react'
import { useToast } from '../../components/ui/toastContext'
import { resolveEnum } from '../../config/enums'
import { apiPatch } from '../../lib/api'
import {
  decisionChoices,
  decisionErrorMessage,
  decisionLabel,
  isReviewed,
  leadStatusBody,
  leadStatusPath,
} from '../../lib/leads'

// Staff confirm or override the intake agent's verdict. A decision can be changed later,
// so there is no confirm step. Mounted per lead (keyed by id), so state never leaks between leads.
export default function LeadDecisionSection({ lead, onDecided }) {
  const toast = useToast()
  const [saving, setSaving] = useState(null)
  const [error, setError] = useState(null)
  const choices = decisionChoices(lead)

  async function decide(status) {
    setSaving(status)
    setError(null)
    try {
      await apiPatch(leadStatusPath(lead.id), leadStatusBody(status))
      const label = resolveEnum('triageStatus', status)?.label.toLowerCase() ?? status
      onDecided(status)
      toast.show(`${lead.fullName} marked ${label}.`)
    } catch (requestError) {
      setError(decisionErrorMessage(requestError))
    } finally {
      setSaving(null)
    }
  }

  if (choices.length === 0) return null

  return (
    <section aria-labelledby="lead-decision-heading">
      <h3 id="lead-decision-heading" className="text-sm font-semibold text-stone-900">
        Your decision
      </h3>
      <p id="lead-decision-hint" className="mt-1 text-sm text-stone-600">
        {isReviewed(lead)
          ? 'This lead has been reviewed. You can still change the decision.'
          : 'Confirm the agent’s verdict or override it. You can change this later.'}
      </p>
      <div className="mt-3 flex flex-col gap-2 sm:flex-row">
        {choices.map((status) => (
          <button
            key={status}
            type="button"
            onClick={() => decide(status)}
            disabled={saving !== null}
            aria-describedby="lead-decision-hint"
            className="rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:cursor-wait disabled:opacity-60 sm:flex-1"
          >
            {saving === status ? 'Saving…' : decisionLabel(lead, status)}
          </button>
        ))}
      </div>
      {error && (
        <p role="alert" className="mt-2 text-sm text-brand-700">
          {error}
        </p>
      )}
    </section>
  )
}
