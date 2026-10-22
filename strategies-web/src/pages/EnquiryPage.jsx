import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import { inputClasses } from '../components/ui/fieldStyles'
import TextField from '../components/ui/TextField'
import { enumOrder } from '../config/enums'
import { apiPost } from '../lib/api'
import { shortId } from '../lib/format'
import { useDocumentTitle } from '../lib/useDocumentTitle'
import { dateInputValue, enquiryServerFieldErrors, validateEnquiryDetails, validateEnquiryPlans } from '../lib/validation'

// Values match the backend ImmigrationStatus enum. Descriptions are written for the public.
const STATUS_OPTIONS = [
  { value: 'Citizen', label: 'South African citizen', description: 'You hold South African citizenship.' },
  { value: 'PermanentResident', label: 'Permanent resident', description: 'You hold South African permanent residence.' },
  { value: 'TemporaryResident', label: 'Temporary resident', description: 'You are in South Africa on a visa or permit.' },
  { value: 'Foreigner', label: 'Foreign national', description: 'You do not currently hold South African status.' },
]

const STEP_TITLES = { 1: 'About you', 2: 'Your plans' }

// Fields in on-screen order per step: after a failed submit, focus goes to the first one with an error.
const STEP_FIELDS = {
  1: ['fullName', 'email', 'immigrationStatus'],
  2: ['desiredStartDate', 'consent'],
}

const INITIAL_VALUES = {
  fullName: '',
  email: '',
  immigrationStatus: '',
  desiredStartDate: '',
  needsAssistance: false,
  consent: false,
}

function tomorrowInputValue() {
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  return dateInputValue(tomorrow)
}

function submissionErrorMessage(error) {
  if (error.status === 429) return 'Too many enquiries have been sent from this connection. Please wait a minute and try again.'
  return error.status >= 400 && error.status < 500 ? error.message : 'We could not send your enquiry just now. Please try again in a moment.'
}

function ProgressIndicator({ step }) {
  return (
    <div className="mt-6">
      <div className="flex items-center justify-between text-xs font-medium text-stone-500">
        <span>Step {step} of 2</span>
        <span>{STEP_TITLES[step]}</span>
      </div>
      <div className="mt-2 grid grid-cols-2 gap-1.5" aria-hidden="true">
        <span className="h-1 rounded-full bg-brand-600" />
        <span className={`h-1 rounded-full transition-colors ${step >= 2 ? 'bg-brand-600' : 'bg-stone-200'}`} />
      </div>
    </div>
  )
}

function FieldError({ id, message }) {
  if (!message) return null
  return (
    <p id={id} className="mt-1.5 text-sm text-brand-700">
      {message}
    </p>
  )
}

export default function EnquiryPage() {
  useDocumentTitle('Start an enquiry')
  const [step, setStep] = useState(1)
  const [values, setValues] = useState(INITIAL_VALUES)
  const [errors, setErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [serverErrors, setServerErrors] = useState([])
  const [submitting, setSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(null)
  const stepHeadingRef = useRef(null)
  const successHeadingRef = useRef(null)
  const formRef = useRef(null)
  const isFirstRender = useRef(true)
  // Name of the field to focus once the errors from a failed submit have rendered.
  const pendingFocusRef = useRef(null)

  // Move focus to the step heading when the step changes, so screen readers announce it.
  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false
      return
    }
    stepHeadingRef.current?.focus()
  }, [step])

  // Focus the first invalid field after its error message and aria-describedby are in the DOM,
  // so a screen reader announces the field together with what is wrong (WCAG 3.3.1).
  useEffect(() => {
    const name = pendingFocusRef.current
    if (!name) return
    pendingFocusRef.current = null
    const target = formRef.current?.elements.namedItem(name)
    // A radio group comes back as a list of its inputs rather than an element: focus its first option.
    const field = target && !target.tagName ? target[0] : target
    field?.focus()
  }, [errors])

  // The submit button disappears on success, so move focus to the confirmation instead of losing it.
  useEffect(() => {
    if (submitted) successHeadingRef.current?.focus()
  }, [submitted])

  function updateValue(name, value) {
    setValues((previous) => ({ ...previous, [name]: value }))
    setFormError('')
    setServerErrors([])
    if (errors[name]) {
      setErrors((previous) => ({ ...previous, [name]: undefined }))
    }
  }

  function handleTextChange(event) {
    updateValue(event.target.name, event.target.value)
  }

  function showErrors(found, fields) {
    pendingFocusRef.current = fields.find((name) => found[name]) ?? null
    setErrors(found)
  }

  function handleContinue(event) {
    event.preventDefault()
    const found = validateEnquiryDetails(values)
    showErrors(found, STEP_FIELDS[1])
    if (Object.keys(found).length === 0) setStep(2)
  }

  function handleBack() {
    setErrors({})
    setFormError('')
    setServerErrors([])
    setStep(1)
  }

  async function handleSubmit(event) {
    event.preventDefault()
    const found = validateEnquiryPlans(values)
    showErrors(found, STEP_FIELDS[2])
    if (Object.keys(found).length > 0) return

    setSubmitting(true)
    setFormError('')
    setServerErrors([])

    try {
      const result = await apiPost('/api/leads', {
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        // Sent as the enum's number, which the API accepts with or without JsonStringEnumConverter.
        immigrationStatus: enumOrder('immigrationStatus', values.immigrationStatus),
        needsAssistance: values.needsAssistance,
        desiredStartDate: `${values.desiredStartDate}T00:00:00Z`,
        consent: values.consent,
      })
      setSubmitted({
        firstName: values.fullName.trim().split(' ')[0],
        email: values.email.trim(),
        reference: result?.leadId ? shortId(result.leadId) : null,
      })
    } catch (error) {
      // The API can still refuse for missing consent: show that under the checkbox, not above the form.
      const fieldErrors = enquiryServerFieldErrors(error)
      if (Object.keys(fieldErrors).length > 0) {
        showErrors(fieldErrors, STEP_FIELDS[2])
        return
      }
      setFormError(submissionErrorMessage(error))
      setServerErrors(Array.isArray(error.errors) ? error.errors : [])
    } finally {
      setSubmitting(false)
    }
  }

  function startAnother() {
    setValues(INITIAL_VALUES)
    setErrors({})
    setSubmitted(null)
    setStep(1)
  }

  if (submitted) {
    return (
      <AuthLayout wide>
        {/* tabIndex -1 so focus can move here on success; no outline because a heading is not a control. */}
        <h1
          ref={successHeadingRef}
          tabIndex={-1}
          className="text-center text-xl font-semibold tracking-tight text-stone-900 focus:outline-none"
        >
          Enquiry received
        </h1>
        <p className="mt-3 text-center text-sm leading-relaxed text-stone-600">
          Thank you, {submitted.firstName}. A consultant will review your enquiry and contact you at{' '}
          <span className="font-medium text-stone-900">{submitted.email}</span>.
        </p>
        {submitted.reference && (
          <div className="mx-auto mt-6 max-w-xs rounded-lg border border-stone-200 bg-stone-50 px-4 py-3 text-center">
            <p className="text-xs font-medium uppercase tracking-[0.08em] text-stone-500">Your reference</p>
            <p className="mt-1 font-mono text-lg font-semibold tracking-wider text-stone-900">{submitted.reference}</p>
          </div>
        )}
        <div className="mt-8 flex flex-col items-center gap-3">
          <button
            type="button"
            onClick={startAnother}
            className="text-sm font-medium text-brand-700 hover:text-brand-800"
          >
            Submit another enquiry
          </button>
          <Link to="/login" className="text-sm font-medium text-stone-600 hover:text-stone-900">
            Go to sign in
          </Link>
        </div>
      </AuthLayout>
    )
  }

  const formAlert = (formError || serverErrors.length > 0) && (
    <div role="alert" className="rounded-md border border-brand-200 bg-brand-50 px-3 py-2.5 text-sm text-brand-800">
      {formError && <p>{formError}</p>}
      {serverErrors.length > 0 && (
        <ul className="mt-1 list-disc space-y-0.5 pl-5">
          {serverErrors.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
    </div>
  )

  return (
    <AuthLayout wide>
      <h1 className="text-center text-xl font-semibold tracking-tight text-stone-900">Start your enquiry</h1>
      <p className="mt-1.5 text-center text-sm text-stone-600">
        Tell us about your situation and a consultant will be in touch.
      </p>

      <ProgressIndicator step={step} />

      {step === 1 ? (
        <form ref={formRef} noValidate onSubmit={handleContinue} className="mt-8 space-y-6">
          <h2 ref={stepHeadingRef} tabIndex={-1} className="text-base font-semibold text-stone-900 focus:outline-none">
            About you
          </h2>

          <TextField
            id="fullName"
            label="Full name"
            autoComplete="name"
            value={values.fullName}
            onChange={handleTextChange}
            error={errors.fullName}
          />

          <TextField
            id="email"
            label="Email address"
            type="email"
            autoComplete="email"
            value={values.email}
            onChange={handleTextChange}
            error={errors.email}
          />

          <fieldset aria-describedby={errors.immigrationStatus ? 'immigrationStatus-error' : undefined}>
            <legend className="text-sm font-medium text-stone-800">Your current status in South Africa</legend>
            <div className="mt-2 grid gap-2 sm:grid-cols-2">
              {STATUS_OPTIONS.map((option) => {
                const selected = values.immigrationStatus === option.value
                return (
                  <label
                    key={option.value}
                    className={`relative flex cursor-pointer flex-col rounded-lg border px-3.5 py-3 transition-colors has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand-600 ${
                      selected
                        ? 'border-brand-600 bg-brand-50/50 ring-1 ring-brand-600'
                        : 'border-stone-300 hover:border-stone-400 hover:bg-stone-50'
                    }`}
                  >
                    <input
                      type="radio"
                      name="immigrationStatus"
                      value={option.value}
                      checked={selected}
                      onChange={() => updateValue('immigrationStatus', option.value)}
                      className="sr-only"
                    />
                    <span className="text-sm font-medium text-stone-900">{option.label}</span>
                    <span className="mt-0.5 text-xs text-stone-500">{option.description}</span>
                  </label>
                )
              })}
            </div>
            <FieldError id="immigrationStatus-error" message={errors.immigrationStatus} />
          </fieldset>

          <button
            type="submit"
            className="w-full rounded-md bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700"
          >
            Continue
          </button>
        </form>
      ) : (
        <form ref={formRef} noValidate onSubmit={handleSubmit} className="mt-8 space-y-6">
          <h2 ref={stepHeadingRef} tabIndex={-1} className="text-base font-semibold text-stone-900 focus:outline-none">
            Your plans
          </h2>

          {formAlert}

          <div>
            <label htmlFor="desiredStartDate" className="block text-sm font-medium text-stone-800">
              When would you like to start?
            </label>
            <input
              id="desiredStartDate"
              name="desiredStartDate"
              type="date"
              min={tomorrowInputValue()}
              value={values.desiredStartDate}
              onChange={handleTextChange}
              aria-invalid={Boolean(errors.desiredStartDate)}
              aria-describedby={errors.desiredStartDate ? 'desiredStartDate-error' : undefined}
              className={`mt-1.5 ${inputClasses(Boolean(errors.desiredStartDate))}`}
            />
            <FieldError id="desiredStartDate-error" message={errors.desiredStartDate} />
          </div>

          <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-stone-200 px-3.5 py-3 hover:bg-stone-50">
            <input
              type="checkbox"
              checked={values.needsAssistance}
              onChange={(event) => updateValue('needsAssistance', event.target.checked)}
              className="mt-0.5 size-4 shrink-0 accent-brand-600"
            />
            <span>
              <span className="block text-sm font-medium text-stone-900">I need help preparing my application</span>
              <span className="mt-0.5 block text-xs text-stone-500">
                A consultant can guide you through the documents and forms.
              </span>
            </span>
          </label>

          <div>
            <label className="flex cursor-pointer items-start gap-3">
              <input
                id="consent"
                name="consent"
                type="checkbox"
                checked={values.consent}
                onChange={(event) => updateValue('consent', event.target.checked)}
                aria-invalid={Boolean(errors.consent)}
                aria-describedby={errors.consent ? 'consent-error' : undefined}
                className="mt-0.5 size-4 shrink-0 accent-brand-600"
              />
              <span className="text-sm text-stone-700">
                I agree that Strategies Migration Services may process the personal information in this form to
                assess my enquiry, in line with POPIA.{' '}
                <a
                  href="/privacy"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="font-medium text-brand-700 underline hover:text-brand-800"
                >
                  Read the privacy notice
                  <span className="sr-only"> (opens in a new tab)</span>
                </a>
              </span>
            </label>
            <FieldError id="consent-error" message={errors.consent} />
          </div>

          <div className="flex flex-col-reverse gap-3 sm:flex-row">
            <button
              type="button"
              onClick={handleBack}
              disabled={submitting}
              className="rounded-md border border-stone-300 bg-white px-4 py-2.5 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:opacity-60 sm:w-auto"
            >
              Back
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 rounded-md bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-70"
            >
              {submitting ? 'Submitting…' : 'Submit enquiry'}
            </button>
          </div>
        </form>
      )}

      <div className="mt-8 text-center">
        <Link
          to="/login"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-stone-600 hover:text-stone-900"
        >
          <ArrowLeft className="size-4" strokeWidth={1.75} aria-hidden="true" />
          Back to sign in
        </Link>
      </div>
    </AuthLayout>
  )
}
